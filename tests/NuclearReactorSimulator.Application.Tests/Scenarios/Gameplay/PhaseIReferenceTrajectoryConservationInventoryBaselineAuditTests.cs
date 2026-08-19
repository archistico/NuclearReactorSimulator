using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios.Recording;
using NuclearReactorSimulator.Application.Scenarios.Training;
using NuclearReactorSimulator.Domain.Plant;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay;

/// <summary>
/// I.3 establishes the first Phase-I versioned 300-second current-v2 reference trajectory together with
/// consolidated conservation/inventory observations and tolerance budgets. It is observational only.
/// </summary>
public sealed class PhaseIReferenceTrajectoryConservationInventoryBaselineAuditTests
{
    private const int StepsPerSecond = 100;
    private const int ReferenceSeconds = 300;
    private const int FinalWindowSeconds = 60;
    private const int ReferenceSteps = ReferenceSeconds * StepsPerSecond;
    private const double MaximumMassClosureResidualKilograms = 1e-6d;
    private const double MaximumEnergyClosureResidualJoules = 1e-2d;
    private const double MaximumBalanceMassRateResidualKilogramsPerSecond = 1e-8d;
    private const double MaximumBalancePowerResidualWatts = 1e-3d;
    private static readonly UTF8Encoding Utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);
    private const string LongAuditOptInEnvironmentVariable = "NRS_I3_LONG_AUDIT";

    [Fact]
    public void FrozenI2Evidence_ProvesAuditCiBaselineBeforeReferenceBaseline()
    {
        AssertFrozenEvidence(
            "I2_ValidatedAuditConsolidationCiBaselineSummary.txt",
            "59597F9D2B3E00E985488298F66DF1A17CE0A7B6245A58BC103A0C44A6FCA68B",
            "phase-i-audit-consolidation-passes=True",
            "i2-audit-passes=True",
            "phase-i-ci-baseline-established=True",
            "runtime-behavior-changed=False");

        AssertFrozenEvidence(
            "I2_ValidatedAuditTierManifest.csv",
            "074007E5D9714FEC8E10C3C4AF46C5B7F1EF0363C54F9C6CA9C81B956714A071",
            "gameplay-long,SCHEDULED-LONG",
            "operational-envelope,SCHEDULED-LONG",
            "reference-plant-scale,SCHEDULED-LONG",
            "H24-post-H28,HISTORICAL-FROZEN");

        AssertFrozenEvidence(
            "I2_ValidatedLegacyModeRetirementReadiness.csv",
            "6815B4B4A92A5AE0194CCBB482D720EFD2B4388557E718E5116684230477736A",
            "DeterministicHybridSemiImplicit,HISTORICAL-FROZEN-CANDIDATE,False,False,True,False",
            "FourNodeBranchContinuityShadowIntegrated,HISTORICAL-FROZEN-CANDIDATE,False,False,True,False");
    }

    [Fact]
    public void ReferenceTrajectoryContract_IsExactVersionedAndBaselineEstablishing()
    {
        var path = Path.Combine(FindRepositoryRoot(), "eng", "phase-i-reference-trajectory-contract.csv");
        Assert.True(File.Exists(path), "Phase-I reference trajectory contract is missing.");
        var lines = File.ReadAllLines(path);
        Assert.Equal(2, lines.Length);
        Assert.Equal(
            "trajectory_id,schema_version,exact_initial_condition,production_policy,simulated_seconds,logical_steps,sample_stride_steps,final_window_seconds,reference_role,budget_derivation,baseline_status",
            lines[0]);

        var fields = lines[1].Split(',');
        Assert.Equal(11, fields.Length);
        Assert.Equal("phase-i-desktop-v2-healthy-300s-v1", fields[0]);
        Assert.Equal("1", fields[1]);
        Assert.Equal("integrated-operations-desktop-stable@2", fields[2]);
        Assert.Equal("ExplicitCommittedState", fields[3]);
        Assert.Equal("300", fields[4]);
        Assert.Equal("30000", fields[5]);
        Assert.Equal("100", fields[6]);
        Assert.Equal("60", fields[7]);
        Assert.Equal("AUTHORITATIVE-DEFAULT-REFERENCE", fields[8]);
        Assert.Equal("final-window-mean-plus-2x-observed-deviation-with-absolute-floor", fields[9]);
        Assert.Equal("CANDIDATE-TO-FREEZE-AFTER-I3", fields[10]);
    }

    [Fact(Explicit = true)]
    [Trait("Category", "PhaseIReferenceTrajectoryConservationInventoryBaselineAudit")]
    public void DesktopV2HealthyReferenceTrajectory_EstablishesThreeHundredSecondConservationInventoryAndToleranceEvidence()
    {
        if (!string.Equals(
                Environment.GetEnvironmentVariable(LongAuditOptInEnvironmentVariable),
                "1",
                StringComparison.Ordinal))
        {
            return;
        }

        ResetReportDirectory();
        Assert.Equal("integrated-operations-desktop-stable", DesktopSustainedGenerationInitialConditionFactory.Reference.InitialConditionId);
        Assert.Equal(2, DesktopSustainedGenerationInitialConditionFactory.Reference.Version);
        var engine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            new DesktopSustainedGenerationInitialConditionFactory().CreateRuntimeEngine());
        Assert.Equal(
            HydraulicNumericalCouplingMode.ExplicitCommittedState,
            engine.CurrentState.PlantDefinition.PlantDefinition.HydraulicNumericalCoupling.Mode);
        var coordinator = new ControlRoomRuntimeCoordinator(engine);
        var samples = new List<ReferenceTrajectorySample>(ReferenceSeconds + 1)
        {
            Capture(engine, coordinator.Current),
        };

        coordinator.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.Run));
        for (var second = 1; second <= ReferenceSeconds; second++)
        {
            AdvanceCheckpoint(coordinator, StepsPerSecond);
            var sample = Capture(engine, coordinator.Current);
            samples.Add(sample);

            if (second % 30 == 0)
            {
                File.AppendAllText(
                    Path.Combine(ReportDirectory(), "00-progress.txt"),
                    $"{DateTimeOffset.UtcNow:O} simulated-seconds={second}; logical-step={sample.LogicalStep}{Environment.NewLine}",
                    Utf8WithoutBom);
            }
        }

        Assert.Equal(ReferenceSeconds + 1, samples.Count);
        Assert.Equal(ReferenceSteps, samples[^1].LogicalStep);

        var maxMassClosure = samples.Max(static sample => Math.Abs(sample.MassClosureResidualKilograms));
        var maxEnergyClosure = samples.Max(static sample => Math.Abs(sample.EnergyClosureResidualJoules));
        var maxBalanceMassRate = samples.Max(static sample => Math.Abs(sample.BalanceMassRateResidualKilogramsPerSecond));
        var maxBalancePower = samples.Max(static sample => Math.Abs(sample.BalancePowerResidualWatts));

        Assert.InRange(maxMassClosure, 0d, MaximumMassClosureResidualKilograms);
        Assert.InRange(maxEnergyClosure, 0d, MaximumEnergyClosureResidualJoules);
        Assert.InRange(maxBalanceMassRate, 0d, MaximumBalanceMassRateResidualKilogramsPerSecond);
        Assert.InRange(maxBalancePower, 0d, MaximumBalancePowerResidualWatts);

        var finalWindow = samples.Where(static sample => sample.SimulatedSeconds >= ReferenceSeconds - FinalWindowSeconds).ToArray();
        Assert.Equal(FinalWindowSeconds + 1, finalWindow.Length);

        var slopes = BuildInventorySlopes(finalWindow);
        Assert.Equal(7, slopes.Count);
        Assert.All(slopes, static slope => Assert.True(double.IsFinite(slope.SlopePerSecond), $"Non-finite slope for {slope.MetricId}."));

        var budgets = BuildToleranceBudgets(finalWindow, slopes);
        Assert.Equal(19, budgets.Count);
        Assert.All(budgets, static budget =>
        {
            Assert.True(double.IsFinite(budget.Target));
            Assert.True(double.IsFinite(budget.AbsoluteTolerance));
            Assert.True(budget.AbsoluteTolerance > 0d);
        });

        var operatingSamples = samples.Skip(1).ToArray();
        Assert.Equal(ReferenceSeconds, operatingSamples.Length);

        var healthViolations = operatingSamples.Where(static sample => !IsHealthy(sample)).ToArray();
        var shaftFloorViolations = operatingSamples.Where(static sample => sample.ShaftPowerMegawatts <= 4.5d).ToArray();
        var shaftDropEpisodes = BuildShaftDropEpisodes(operatingSamples);

        var trajectoryFingerprint = ComputeTrajectoryFingerprint(samples);
        var finalPresentationFingerprint = samples[^1].PresentationFingerprint;
        var passes = healthViolations.Length == 0
            && maxMassClosure <= MaximumMassClosureResidualKilograms
            && maxEnergyClosure <= MaximumEnergyClosureResidualJoules
            && maxBalanceMassRate <= MaximumBalanceMassRateResidualKilogramsPerSecond
            && maxBalancePower <= MaximumBalancePowerResidualWatts
            && slopes.All(static slope => double.IsFinite(slope.SlopePerSecond))
            && budgets.All(static budget => double.IsFinite(budget.Target) && budget.AbsoluteTolerance > 0d);
        WriteArtifacts(
            samples,
            slopes,
            budgets,
            healthViolations,
            shaftFloorViolations,
            shaftDropEpisodes,
            maxMassClosure,
            maxEnergyClosure,
            maxBalanceMassRate,
            maxBalancePower,
            trajectoryFingerprint,
            finalPresentationFingerprint,
            passes);

        Assert.True(
            passes,
            BuildHealthFailureDiagnostic(healthViolations, shaftFloorViolations, shaftDropEpisodes));
    }

    private static ReferenceTrajectorySample Capture(IntegratedAutomaticOperationRuntimeEngine engine, ControlRoomSnapshot presentation)
    {
        var fullPlant = engine.LatestCanonicalSnapshot.Control.ProtectedControl.FullPlant;
        var plant = fullPlant.CandidatePlant;
        var heatBalance = fullPlant.HeatBalance;
        var thermofluid = fullPlant.IntegratedCycle.ThermofluidAudit;
        var turbine = fullPlant.IntegratedCycle.TurbineExpansion;
        var admissionTrain = Assert.Single(turbine.MainSteamNetwork.AdmissionTrains);
        var condenser = Assert.Single(fullPlant.IntegratedCycle.Condenser.Condensers);
        var train = Assert.Single(fullPlant.IntegratedCycle.CondensateFeedwater.Trains);
        var drum = Assert.Single(fullPlant.IntegratedCycle.PrimaryCircuit.SteamDrums.Drums);
        var steamLine = Assert.Single(fullPlant.IntegratedCycle.TurbineExpansion.MainSteamNetwork.SteamLines);
        var generator = Assert.Single(presentation.Electrical.Generators);
        var rotor = Assert.Single(presentation.TurbineSecondary.Rotors);

        var exhaust = plant.GetFluidNode(condenser.SteamSpaceNodeId);
        var hotwell = plant.GetFluidNode(condenser.HotwellNodeId);
        var feedwater = plant.GetFluidNode(train.FeedwaterInventoryNodeId);
        var drumInventory = plant.GetFluidNode(drum.InventoryNodeId);
        var header = plant.GetFluidNode(steamLine.HeaderNodeId);
        var totalFluidMass = plant.FluidNodes.Sum(static node => node.Mass.Kilograms);
        var totalFluidEnergy = plant.FluidNodes.Sum(static node => node.InternalEnergy.Joules);

        var sample = new ReferenceTrajectorySample(
            presentation.LogicalStep,
            presentation.LogicalStep / (double)StepsPerSecond,
            ControlRoomSnapshotFingerprint.Compute(presentation),
            presentation.AnyTripActive,
            generator.BreakerClosed,
            generator.RequestedElectricalPower.NumericValue ?? double.NaN,
            generator.ElectricalOutput.NumericValue ?? double.NaN,
            rotor.ShaftPower.NumericValue ?? double.NaN,
            turbine.TotalShaftPower.Megawatts,
            turbine.TotalSteamMassFlowRate.KilogramsPerSecond,
            admissionTrain.AdmissionValve.MassFlowRate.KilogramsPerSecond,
            admissionTrain.ControlValve.EffectivePosition.Percent,
            admissionTrain.AdmissionValve.EffectivePosition.Percent,
            admissionTrain.TurbineInletPressure.Kilopascals,
            admissionTrain.TurbineInletTemperature.DegreesCelsius,
            admissionTrain.TurbineInletPhase.ToString(),
            rotor.Speed.NumericValue ?? double.NaN,
            condenser.FinalSteamSpacePressure.Kilopascals,
            drum.LiquidLevelFraction.Fraction,
            totalFluidMass,
            totalFluidEnergy,
            exhaust.Mass.Kilograms,
            hotwell.Mass.Kilograms,
            feedwater.Mass.Kilograms,
            drumInventory.Mass.Kilograms,
            header.Mass.Kilograms,
            heatBalance.MassClosureResidualKilograms,
            heatBalance.FullEnergyPathClosureResidualJoules,
            thermofluid.BalanceMassRateResidualKilogramsPerSecond,
            thermofluid.BalancePowerResidualWatts);

        AssertFinite(sample);
        return sample;
    }

    private static bool IsHealthy(ReferenceTrajectorySample sample)
        => !sample.AnyTrip
            && sample.GeneratorBreakerClosed
            && sample.RequestedElectricalPowerMegawatts > 4.5d
            && sample.GrossElectricalPowerMegawatts > 4.0d
            && sample.ShaftPowerMegawatts > 4.5d;

    private static void AssertFinite(ReferenceTrajectorySample sample)
    {
        foreach (var value in new[]
        {
            sample.SimulatedSeconds,
            sample.RequestedElectricalPowerMegawatts,
            sample.GrossElectricalPowerMegawatts,
            sample.ShaftPowerMegawatts,
            sample.CanonicalTotalTurbineShaftPowerMegawatts,
            sample.TotalTurbineSteamFlowKilogramsPerSecond,
            sample.AdmissionMassFlowKilogramsPerSecond,
            sample.ControlValvePositionPercent,
            sample.AdmissionValvePositionPercent,
            sample.TurbineInletPressureKilopascals,
            sample.TurbineInletTemperatureDegreesCelsius,
            sample.RotorSpeedRpm,
            sample.CondenserPressureKilopascals,
            sample.DrumLevelFraction,
            sample.TotalFluidMassKilograms,
            sample.TotalFluidInternalEnergyJoules,
            sample.ExhaustMassKilograms,
            sample.HotwellMassKilograms,
            sample.FeedwaterInventoryMassKilograms,
            sample.DrumInventoryMassKilograms,
            sample.MainSteamHeaderMassKilograms,
            sample.MassClosureResidualKilograms,
            sample.EnergyClosureResidualJoules,
            sample.BalanceMassRateResidualKilogramsPerSecond,
            sample.BalancePowerResidualWatts,
        })
        {
            Assert.True(double.IsFinite(value), $"Non-finite I.3 reference trajectory value at logical step {sample.LogicalStep}.");
        }
    }

    private static IReadOnlyList<ShaftDropEpisode> BuildShaftDropEpisodes(IReadOnlyList<ReferenceTrajectorySample> operatingSamples)
    {
        var episodes = new List<ShaftDropEpisode>();
        var start = -1;
        for (var i = 0; i <= operatingSamples.Count; i++)
        {
            var belowFloor = i < operatingSamples.Count && operatingSamples[i].ShaftPowerMegawatts <= 4.5d;
            if (belowFloor && start < 0)
            {
                start = i;
                continue;
            }

            if (belowFloor || start < 0)
            {
                continue;
            }

            var episodeSamples = operatingSamples.Skip(start).Take(i - start).ToArray();
            episodes.Add(new ShaftDropEpisode(
                episodeSamples[0].LogicalStep,
                episodeSamples[^1].LogicalStep,
                episodeSamples[0].SimulatedSeconds,
                episodeSamples[^1].SimulatedSeconds,
                episodeSamples.Length,
                episodeSamples.Min(static sample => sample.ShaftPowerMegawatts),
                episodeSamples.Min(static sample => sample.CanonicalTotalTurbineShaftPowerMegawatts),
                episodeSamples.Min(static sample => sample.GrossElectricalPowerMegawatts),
                episodeSamples.Min(static sample => sample.TotalTurbineSteamFlowKilogramsPerSecond),
                episodeSamples.Min(static sample => sample.AdmissionMassFlowKilogramsPerSecond),
                string.Join("|", episodeSamples.Select(static sample => sample.TurbineInletPhase).Distinct(StringComparer.Ordinal))));
            start = -1;
        }

        return episodes;
    }

    private static string BuildHealthFailureDiagnostic(
        IReadOnlyList<ReferenceTrajectorySample> healthViolations,
        IReadOnlyList<ReferenceTrajectorySample> shaftFloorViolations,
        IReadOnlyList<ShaftDropEpisode> shaftDropEpisodes)
    {
        if (healthViolations.Count == 0)
        {
            return "I.3 failed outside the generation-health predicate; inspect generated artifacts.";
        }

        var first = healthViolations[0];
        var longest = shaftDropEpisodes.OrderByDescending(static episode => episode.SampleCount).FirstOrDefault();
        var longestText = longest is null
            ? "none"
            : FormattableString.Invariant($"{longest.StartSeconds:0.###}-{longest.EndSeconds:0.###}s/{longest.SampleCount} samples/min-shaft={longest.MinimumRotorShaftPowerMegawatts:0.###}MW");
        return FormattableString.Invariant(
            $"I.3 completed the full 300 s trajectory but generation-health did not remain continuously green. violations={healthViolations.Count}; shaft-floor-violations={shaftFloorViolations.Count}; shaft-drop-episodes={shaftDropEpisodes.Count}; first=step {first.LogicalStep} t={first.SimulatedSeconds:0.###}s request/gross/rotor-shaft/canonical-shaft={first.RequestedElectricalPowerMegawatts:0.###}/{first.GrossElectricalPowerMegawatts:0.###}/{first.ShaftPowerMegawatts:0.###}/{first.CanonicalTotalTurbineShaftPowerMegawatts:0.###}MW steam/admission={first.TotalTurbineSteamFlowKilogramsPerSecond:0.###}/{first.AdmissionMassFlowKilogramsPerSecond:0.###}kg/s phase={first.TurbineInletPhase}; longest={longestText}. See 03/06/07 diagnostic CSV artifacts.");
    }

    private static IReadOnlyList<InventorySlope> BuildInventorySlopes(IReadOnlyList<ReferenceTrajectorySample> window)
        => new[]
        {
            BuildSlope("total-fluid-mass", "kg/s", window, static sample => sample.TotalFluidMassKilograms),
            BuildSlope("total-fluid-internal-energy", "W", window, static sample => sample.TotalFluidInternalEnergyJoules),
            BuildSlope("exhaust-mass", "kg/s", window, static sample => sample.ExhaustMassKilograms),
            BuildSlope("hotwell-mass", "kg/s", window, static sample => sample.HotwellMassKilograms),
            BuildSlope("feedwater-inventory-mass", "kg/s", window, static sample => sample.FeedwaterInventoryMassKilograms),
            BuildSlope("drum-inventory-mass", "kg/s", window, static sample => sample.DrumInventoryMassKilograms),
            BuildSlope("main-steam-header-mass", "kg/s", window, static sample => sample.MainSteamHeaderMassKilograms),
        };

    private static InventorySlope BuildSlope(
        string metricId,
        string unit,
        IReadOnlyList<ReferenceTrajectorySample> window,
        Func<ReferenceTrajectorySample, double> selector)
    {
        var meanTime = window.Average(static sample => sample.SimulatedSeconds);
        var meanValue = window.Average(selector);
        var numerator = 0d;
        var denominator = 0d;
        foreach (var sample in window)
        {
            var dx = sample.SimulatedSeconds - meanTime;
            numerator += dx * (selector(sample) - meanValue);
            denominator += dx * dx;
        }

        var slope = denominator > 0d ? numerator / denominator : double.NaN;
        return new InventorySlope(metricId, unit, meanValue, slope);
    }

    private static IReadOnlyList<ToleranceBudget> BuildToleranceBudgets(
        IReadOnlyList<ReferenceTrajectorySample> window,
        IReadOnlyList<InventorySlope> slopes)
    {
        var budgets = new List<ToleranceBudget>
        {
            BuildWindowBudget("gross-electrical-power", "MW", window, static sample => sample.GrossElectricalPowerMegawatts, 0.05d),
            BuildWindowBudget("shaft-power", "MW", window, static sample => sample.ShaftPowerMegawatts, 0.05d),
            BuildWindowBudget("rotor-speed", "rpm", window, static sample => sample.RotorSpeedRpm, 1d),
            BuildWindowBudget("condenser-pressure", "kPa", window, static sample => sample.CondenserPressureKilopascals, 0.1d),
            BuildWindowBudget("drum-level-fraction", "fraction", window, static sample => sample.DrumLevelFraction, 0.005d),
            BuildWindowBudget("total-fluid-mass", "kg", window, static sample => sample.TotalFluidMassKilograms, 0.1d),
            BuildWindowBudget("total-fluid-internal-energy", "J", window, static sample => sample.TotalFluidInternalEnergyJoules, 1_000d),
            BuildWindowBudget("exhaust-mass", "kg", window, static sample => sample.ExhaustMassKilograms, 0.1d),
            BuildWindowBudget("hotwell-mass", "kg", window, static sample => sample.HotwellMassKilograms, 0.1d),
            BuildWindowBudget("feedwater-inventory-mass", "kg", window, static sample => sample.FeedwaterInventoryMassKilograms, 0.1d),
            BuildWindowBudget("drum-inventory-mass", "kg", window, static sample => sample.DrumInventoryMassKilograms, 0.1d),
            BuildWindowBudget("main-steam-header-mass", "kg", window, static sample => sample.MainSteamHeaderMassKilograms, 0.1d),
        };

        foreach (var slope in slopes)
        {
            var floor = slope.Unit == "W" ? 100d : 0.01d;
            budgets.Add(new ToleranceBudget(
                $"slope.{slope.MetricId}",
                slope.Unit,
                0d,
                Math.Max(floor, 2d * Math.Abs(slope.SlopePerSecond)),
                "I3-observed-final-60s-linear-slope; target-zero; freeze-after-validation"));
        }

        return budgets;
    }

    private static ToleranceBudget BuildWindowBudget(
        string metricId,
        string unit,
        IReadOnlyList<ReferenceTrajectorySample> window,
        Func<ReferenceTrajectorySample, double> selector,
        double absoluteFloor)
    {
        var target = window.Average(selector);
        var maximumDeviation = window.Max(sample => Math.Abs(selector(sample) - target));
        return new ToleranceBudget(
            metricId,
            unit,
            target,
            Math.Max(absoluteFloor, 2d * maximumDeviation),
            "I3-final-60s-mean; tolerance=max[absolute-floor;2x-observed-max-deviation]; freeze-after-validation");
    }

    private static string ComputeTrajectoryFingerprint(IReadOnlyList<ReferenceTrajectorySample> samples)
    {
        var text = string.Join("\n", samples.Select(FormatFingerprintRow));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
    }

    private static string FormatFingerprintRow(ReferenceTrajectorySample sample)
        => FormattableString.Invariant(
            $"{sample.LogicalStep}|{sample.PresentationFingerprint}|{sample.AnyTrip}|{sample.GeneratorBreakerClosed}|{sample.RequestedElectricalPowerMegawatts:G17}|{sample.GrossElectricalPowerMegawatts:G17}|{sample.ShaftPowerMegawatts:G17}|{sample.CanonicalTotalTurbineShaftPowerMegawatts:G17}|{sample.TotalTurbineSteamFlowKilogramsPerSecond:G17}|{sample.AdmissionMassFlowKilogramsPerSecond:G17}|{sample.ControlValvePositionPercent:G17}|{sample.AdmissionValvePositionPercent:G17}|{sample.TurbineInletPressureKilopascals:G17}|{sample.TurbineInletTemperatureDegreesCelsius:G17}|{sample.TurbineInletPhase}|{sample.RotorSpeedRpm:G17}|{sample.CondenserPressureKilopascals:G17}|{sample.DrumLevelFraction:G17}|{sample.TotalFluidMassKilograms:G17}|{sample.TotalFluidInternalEnergyJoules:G17}|{sample.ExhaustMassKilograms:G17}|{sample.HotwellMassKilograms:G17}|{sample.FeedwaterInventoryMassKilograms:G17}|{sample.DrumInventoryMassKilograms:G17}|{sample.MainSteamHeaderMassKilograms:G17}|{sample.MassClosureResidualKilograms:G17}|{sample.EnergyClosureResidualJoules:G17}|{sample.BalanceMassRateResidualKilogramsPerSecond:G17}|{sample.BalancePowerResidualWatts:G17}");

    private static void WriteArtifacts(
        IReadOnlyList<ReferenceTrajectorySample> samples,
        IReadOnlyList<InventorySlope> slopes,
        IReadOnlyList<ToleranceBudget> budgets,
        IReadOnlyList<ReferenceTrajectorySample> healthViolations,
        IReadOnlyList<ReferenceTrajectorySample> shaftFloorViolations,
        IReadOnlyList<ShaftDropEpisode> shaftDropEpisodes,
        double maxMassClosure,
        double maxEnergyClosure,
        double maxBalanceMassRate,
        double maxBalancePower,
        string trajectoryFingerprint,
        string finalPresentationFingerprint,
        bool passes)
    {
        var directory = ReportDirectory();
        File.Copy(
            Path.Combine(FindRepositoryRoot(), "eng", "phase-i-reference-trajectory-contract.csv"),
            Path.Combine(directory, "02-reference-trajectory-contract.csv"),
            overwrite: true);

        var trajectoryLines = new List<string>
        {
            "logical_step,simulated_seconds,presentation_fingerprint,any_trip,generator_breaker_closed,requested_mwe,gross_mwe,rotor_shaft_mwe,canonical_total_turbine_shaft_mwe,total_turbine_steam_flow_kg_s,admission_flow_kg_s,control_valve_percent,admission_valve_percent,turbine_inlet_pressure_kpa,turbine_inlet_temperature_c,turbine_inlet_phase,rotor_rpm,condenser_pressure_kpa,drum_level_fraction,total_fluid_mass_kg,total_fluid_internal_energy_j,exhaust_mass_kg,hotwell_mass_kg,feedwater_inventory_mass_kg,drum_inventory_mass_kg,main_steam_header_mass_kg,mass_closure_kg,energy_closure_j,balance_mass_rate_kg_s,balance_power_w",
        };
        trajectoryLines.AddRange(samples.Select(FormatCsvRow));
        File.WriteAllLines(Path.Combine(directory, "03-reference-trajectory-samples.csv"), trajectoryLines, Utf8WithoutBom);

        var slopeLines = new List<string>
        {
            "metric_id,unit,final_window_mean,linear_slope_per_second",
        };
        slopeLines.AddRange(slopes.Select(static slope => FormattableString.Invariant(
            $"{slope.MetricId},{slope.Unit},{slope.MeanValue:G17},{slope.SlopePerSecond:G17}")));
        File.WriteAllLines(Path.Combine(directory, "04-conservation-inventory-final-window-slopes.csv"), slopeLines, Utf8WithoutBom);

        var budgetLines = new List<string>
        {
            "metric_id,unit,target,absolute_tolerance,derivation",
        };
        budgetLines.AddRange(budgets.Select(static budget => FormattableString.Invariant(
            $"{budget.MetricId},{budget.Unit},{budget.Target:G17},{budget.AbsoluteTolerance:G17},{budget.Derivation}")));
        File.WriteAllLines(Path.Combine(directory, "05-versioned-tolerance-budgets.csv"), budgetLines, Utf8WithoutBom);

        var violationLines = new List<string>
        {
            "logical_step,simulated_seconds,reasons,requested_mwe,gross_mwe,rotor_shaft_mwe,canonical_total_turbine_shaft_mwe,total_turbine_steam_flow_kg_s,admission_flow_kg_s,control_valve_percent,admission_valve_percent,turbine_inlet_pressure_kpa,turbine_inlet_temperature_c,turbine_inlet_phase,rotor_rpm,condenser_pressure_kpa,drum_level_fraction",
        };
        violationLines.AddRange(healthViolations.Select(static sample => string.Join(",", new[]
        {
            sample.LogicalStep.ToString(CultureInfo.InvariantCulture),
            sample.SimulatedSeconds.ToString("G17", CultureInfo.InvariantCulture),
            HealthViolationReasons(sample),
            sample.RequestedElectricalPowerMegawatts.ToString("G17", CultureInfo.InvariantCulture),
            sample.GrossElectricalPowerMegawatts.ToString("G17", CultureInfo.InvariantCulture),
            sample.ShaftPowerMegawatts.ToString("G17", CultureInfo.InvariantCulture),
            sample.CanonicalTotalTurbineShaftPowerMegawatts.ToString("G17", CultureInfo.InvariantCulture),
            sample.TotalTurbineSteamFlowKilogramsPerSecond.ToString("G17", CultureInfo.InvariantCulture),
            sample.AdmissionMassFlowKilogramsPerSecond.ToString("G17", CultureInfo.InvariantCulture),
            sample.ControlValvePositionPercent.ToString("G17", CultureInfo.InvariantCulture),
            sample.AdmissionValvePositionPercent.ToString("G17", CultureInfo.InvariantCulture),
            sample.TurbineInletPressureKilopascals.ToString("G17", CultureInfo.InvariantCulture),
            sample.TurbineInletTemperatureDegreesCelsius.ToString("G17", CultureInfo.InvariantCulture),
            sample.TurbineInletPhase,
            sample.RotorSpeedRpm.ToString("G17", CultureInfo.InvariantCulture),
            sample.CondenserPressureKilopascals.ToString("G17", CultureInfo.InvariantCulture),
            sample.DrumLevelFraction.ToString("G17", CultureInfo.InvariantCulture),
        })));
        File.WriteAllLines(Path.Combine(directory, "06-generation-health-violations.csv"), violationLines, Utf8WithoutBom);

        var episodeLines = new List<string>
        {
            "start_step,end_step,start_seconds,end_seconds,sampled_seconds,min_rotor_shaft_mwe,min_canonical_total_turbine_shaft_mwe,min_gross_mwe,min_total_turbine_steam_flow_kg_s,min_admission_flow_kg_s,turbine_inlet_phases",
        };
        episodeLines.AddRange(shaftDropEpisodes.Select(static episode => FormattableString.Invariant(
            $"{episode.StartLogicalStep},{episode.EndLogicalStep},{episode.StartSeconds:G17},{episode.EndSeconds:G17},{episode.SampleCount},{episode.MinimumRotorShaftPowerMegawatts:G17},{episode.MinimumCanonicalTotalTurbineShaftPowerMegawatts:G17},{episode.MinimumGrossElectricalPowerMegawatts:G17},{episode.MinimumTotalTurbineSteamFlowKilogramsPerSecond:G17},{episode.MinimumAdmissionFlowKilogramsPerSecond:G17},{episode.TurbineInletPhases}")));
        File.WriteAllLines(Path.Combine(directory, "07-shaft-drop-episodes.csv"), episodeLines, Utf8WithoutBom);

        var summary = new[]
        {
            "=== 01-current-v2-phase-i-reference-trajectory-conservation-inventory-baseline ===",
            "I.3 Hotfix 1 completes the full versioned 300-second exact-v2 ExplicitCommittedState trajectory even when generation-health fails, so transient shaft-power drops are characterized before the unchanged final health gate is evaluated. It does not change plant physics, numerical mathematics, H.30 OPT-IN ONLY policy, exact-version persistence semantics or the 10 ms fixed step.",
            $"trajectory-id=phase-i-desktop-v2-healthy-300s-v1; exact-initial-condition=integrated-operations-desktop-stable@2; production-policy=ExplicitCommittedState; simulated-seconds={ReferenceSeconds}; logical-steps={ReferenceSteps}; sample-stride-steps={StepsPerSecond}; samples={samples.Count}; final-window-seconds={FinalWindowSeconds};",
            $"healthy-operating-samples={samples.Skip(1).Count(IsHealthy)}/{ReferenceSeconds}; generation-health-violations={healthViolations.Count}; shaft-floor-violations={shaftFloorViolations.Count}; shaft-drop-episodes={shaftDropEpisodes.Count}; trip-samples={samples.Count(static sample => sample.AnyTrip)}; initial-reference-sample-included=True; final-presentation-fingerprint={finalPresentationFingerprint}; trajectory-fingerprint={trajectoryFingerprint};",
            healthViolations.Count == 0
                ? "first-generation-health-violation=none;"
                : FormattableString.Invariant($"first-generation-health-violation-step={healthViolations[0].LogicalStep}; first-generation-health-violation-seconds={healthViolations[0].SimulatedSeconds:G17}; first-rotor-shaft-mwe={healthViolations[0].ShaftPowerMegawatts:G17}; first-canonical-total-shaft-mwe={healthViolations[0].CanonicalTotalTurbineShaftPowerMegawatts:G17}; first-steam-flow-kg-s={healthViolations[0].TotalTurbineSteamFlowKilogramsPerSecond:G17}; first-admission-flow-kg-s={healthViolations[0].AdmissionMassFlowKilogramsPerSecond:G17}; first-turbine-inlet-phase={healthViolations[0].TurbineInletPhase};"),
            FormattableString.Invariant($"max-network-mass-closure-kg={maxMassClosure:G17}; max-network-energy-closure-j={maxEnergyClosure:G17}; max-network-balance-mass-rate-kg-s={maxBalanceMassRate:G17}; max-network-balance-power-w={maxBalancePower:G17};"),
            $"inventory-slope-observations={slopes.Count}; tolerance-budget-entries={budgets.Count}; tolerance-budget-schema=I3-v1; tolerance-budget-derivation=final-window baseline statistics plus explicit absolute floors; budgets-freeze-after-validation=True;",
            "authoritative-default=integrated-operations-desktop-stable@2|ExplicitCommittedState; qualified-opt-in=integrated-operations-desktop-stable@3|FourNodeBranchContinuityCorrectedCommitOptIn; phase-h-production-policy-decision=OPT-IN ONLY; production-fixed-step=10.000 ms; runtime-behavior-changed=False;",
            "legacy-mode-retirement-authorized=False; H24-post-H28-rerun=False; H28-rerun=False; reference-baseline-is-internal-regression-evidence=True; external-historical-measurement=False;",
            $"phase-i-reference-trajectory-baseline-passes={passes}; phase-i-conservation-inventory-baseline-passes={passes}; i3-audit-passes={passes}; phase-i-reference-tolerance-baseline-established={passes};",
            passes
                ? "I.3 recommendation: after a green gate, freeze this exact trajectory, slope and tolerance-budget evidence as the Phase-I v1 regression baseline. Keep slope budgets observational/regression-facing; do not tune runtime physics or seed values to fit them."
                : "I.3 Hotfix 1 recommendation: do not freeze tolerance budgets and do not weaken the generation-health contract. Use 03/06/07 artifacts to classify the shaft-power drop against steam flow, admission flow and turbine-inlet phase before deciding whether a runtime correction is required. Keep slope budgets observational/regression-facing; do not tune runtime physics or seed values to fit them.",
        };
        File.WriteAllLines(Path.Combine(directory, "01-phase-i-reference-trajectory-conservation-inventory-baseline.summary.txt"), summary, Utf8WithoutBom);
    }

    private static string HealthViolationReasons(ReferenceTrajectorySample sample)
    {
        var reasons = new List<string>();
        if (sample.AnyTrip) reasons.Add("trip");
        if (!sample.GeneratorBreakerClosed) reasons.Add("breaker-open");
        if (sample.RequestedElectricalPowerMegawatts <= 4.5d) reasons.Add("request-floor");
        if (sample.GrossElectricalPowerMegawatts <= 4.0d) reasons.Add("gross-floor");
        if (sample.ShaftPowerMegawatts <= 4.5d) reasons.Add("shaft-floor");
        return string.Join("|", reasons);
    }

    private static string FormatCsvRow(ReferenceTrajectorySample sample)
        => string.Join(",", new[]
        {
            sample.LogicalStep.ToString(CultureInfo.InvariantCulture),
            sample.SimulatedSeconds.ToString("G17", CultureInfo.InvariantCulture),
            sample.PresentationFingerprint,
            sample.AnyTrip.ToString(),
            sample.GeneratorBreakerClosed.ToString(),
            sample.RequestedElectricalPowerMegawatts.ToString("G17", CultureInfo.InvariantCulture),
            sample.GrossElectricalPowerMegawatts.ToString("G17", CultureInfo.InvariantCulture),
            sample.ShaftPowerMegawatts.ToString("G17", CultureInfo.InvariantCulture),
            sample.CanonicalTotalTurbineShaftPowerMegawatts.ToString("G17", CultureInfo.InvariantCulture),
            sample.TotalTurbineSteamFlowKilogramsPerSecond.ToString("G17", CultureInfo.InvariantCulture),
            sample.AdmissionMassFlowKilogramsPerSecond.ToString("G17", CultureInfo.InvariantCulture),
            sample.ControlValvePositionPercent.ToString("G17", CultureInfo.InvariantCulture),
            sample.AdmissionValvePositionPercent.ToString("G17", CultureInfo.InvariantCulture),
            sample.TurbineInletPressureKilopascals.ToString("G17", CultureInfo.InvariantCulture),
            sample.TurbineInletTemperatureDegreesCelsius.ToString("G17", CultureInfo.InvariantCulture),
            sample.TurbineInletPhase,
            sample.RotorSpeedRpm.ToString("G17", CultureInfo.InvariantCulture),
            sample.CondenserPressureKilopascals.ToString("G17", CultureInfo.InvariantCulture),
            sample.DrumLevelFraction.ToString("G17", CultureInfo.InvariantCulture),
            sample.TotalFluidMassKilograms.ToString("G17", CultureInfo.InvariantCulture),
            sample.TotalFluidInternalEnergyJoules.ToString("G17", CultureInfo.InvariantCulture),
            sample.ExhaustMassKilograms.ToString("G17", CultureInfo.InvariantCulture),
            sample.HotwellMassKilograms.ToString("G17", CultureInfo.InvariantCulture),
            sample.FeedwaterInventoryMassKilograms.ToString("G17", CultureInfo.InvariantCulture),
            sample.DrumInventoryMassKilograms.ToString("G17", CultureInfo.InvariantCulture),
            sample.MainSteamHeaderMassKilograms.ToString("G17", CultureInfo.InvariantCulture),
            sample.MassClosureResidualKilograms.ToString("G17", CultureInfo.InvariantCulture),
            sample.EnergyClosureResidualJoules.ToString("G17", CultureInfo.InvariantCulture),
            sample.BalanceMassRateResidualKilogramsPerSecond.ToString("G17", CultureInfo.InvariantCulture),
            sample.BalancePowerResidualWatts.ToString("G17", CultureInfo.InvariantCulture),
        });

    private static void AdvanceCheckpoint(ControlRoomRuntimeCoordinator coordinator, int stepCount)
    {
        var remaining = stepCount;
        while (remaining > 0)
        {
            var requested = Math.Min(remaining, coordinator.ExecutionBudget.MaximumSimulationStepsPerBatch);
            var result = coordinator.AdvanceRunning(requested, publicationStride: requested);
            Assert.Equal(requested, result.ExecutedStepCount);
            remaining -= result.ExecutedStepCount;
        }
    }

    private static void AssertFrozenEvidence(string fileName, string expectedSha256, params string[] expectedTokens)
    {
        var path = Path.Combine(EvidenceDirectory(), fileName);
        Assert.True(File.Exists(path), $"Frozen I.2 evidence file is missing: {fileName}");
        Assert.Equal(expectedSha256, CanonicalSha256(path));
        var text = File.ReadAllText(path);
        foreach (var token in expectedTokens)
        {
            Assert.Contains(token, text, StringComparison.Ordinal);
        }
    }

    private static string EvidenceDirectory()
        => Path.Combine(FindRepositoryRoot(), "tests", "NuclearReactorSimulator.Application.Tests", "Scenarios", "Gameplay", "Evidence");

    private static string ReportDirectory()
        => Path.Combine(FindRepositoryRoot(), "artifacts", "i3-phase-i-reference-trajectory-conservation-inventory-baseline");

    private static void ResetReportDirectory()
    {
        var directory = ReportDirectory();
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
        Directory.CreateDirectory(directory);
        File.WriteAllText(
            Path.Combine(directory, "00-progress.txt"),
            $"{DateTimeOffset.UtcNow:O} I.3 Phase-I reference trajectory / conservation-inventory baseline started{Environment.NewLine}",
            Utf8WithoutBom);
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "NuclearReactorSimulator.sln")))
            {
                return current.FullName;
            }
            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate NuclearReactorSimulator.sln from the test output directory.");
    }

    private static string CanonicalSha256(string path)
    {
        var text = File.ReadAllText(path).Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
    }

    private sealed record ReferenceTrajectorySample(
        long LogicalStep,
        double SimulatedSeconds,
        string PresentationFingerprint,
        bool AnyTrip,
        bool GeneratorBreakerClosed,
        double RequestedElectricalPowerMegawatts,
        double GrossElectricalPowerMegawatts,
        double ShaftPowerMegawatts,
        double CanonicalTotalTurbineShaftPowerMegawatts,
        double TotalTurbineSteamFlowKilogramsPerSecond,
        double AdmissionMassFlowKilogramsPerSecond,
        double ControlValvePositionPercent,
        double AdmissionValvePositionPercent,
        double TurbineInletPressureKilopascals,
        double TurbineInletTemperatureDegreesCelsius,
        string TurbineInletPhase,
        double RotorSpeedRpm,
        double CondenserPressureKilopascals,
        double DrumLevelFraction,
        double TotalFluidMassKilograms,
        double TotalFluidInternalEnergyJoules,
        double ExhaustMassKilograms,
        double HotwellMassKilograms,
        double FeedwaterInventoryMassKilograms,
        double DrumInventoryMassKilograms,
        double MainSteamHeaderMassKilograms,
        double MassClosureResidualKilograms,
        double EnergyClosureResidualJoules,
        double BalanceMassRateResidualKilogramsPerSecond,
        double BalancePowerResidualWatts);

    private sealed record InventorySlope(string MetricId, string Unit, double MeanValue, double SlopePerSecond);

    private sealed record ToleranceBudget(string MetricId, string Unit, double Target, double AbsoluteTolerance, string Derivation);

    private sealed record ShaftDropEpisode(
        long StartLogicalStep,
        long EndLogicalStep,
        double StartSeconds,
        double EndSeconds,
        int SampleCount,
        double MinimumRotorShaftPowerMegawatts,
        double MinimumCanonicalTotalTurbineShaftPowerMegawatts,
        double MinimumGrossElectricalPowerMegawatts,
        double MinimumTotalTurbineSteamFlowKilogramsPerSecond,
        double MinimumAdmissionFlowKilogramsPerSecond,
        string TurbineInletPhases);
}
