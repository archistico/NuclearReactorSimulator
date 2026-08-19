using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios.Recording;
using NuclearReactorSimulator.Application.Scenarios.Training;
using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Plant;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay;

/// <summary>
/// I.3 authoritative post-H.30-RQ1 production reference baseline. Runs the selected production default for 300 simulated seconds, verifies every 10 ms step for generation health and targeted-train flow direction, samples conservation/inventory every second, and derives the first frozen tolerance-budget candidate from the final 60 seconds. Runtime physics and numerical mathematics are observationally unchanged.
/// </summary>
public sealed class PhaseIReferenceTrajectoryConservationInventoryBaselineAuditTests
{
    private const string OptInEnvironmentVariable = "NRS_I3_PRODUCTION_REFERENCE_AUDIT";
    private const int StepsPerSecond = 100;
    private const int ReferenceSeconds = 300;
    private const int ReferenceSteps = ReferenceSeconds * StepsPerSecond;
    private const int FinalWindowSeconds = 60;
    private const int DeterminismSteps = 256;
    private const double MaximumMassClosureResidualKilograms = 1e-6d;
    private const double MaximumEnergyClosureResidualJoules = 1e-2d;
    private const double MaximumBalanceMassRateResidualKilogramsPerSecond = 1e-8d;
    private const double MaximumBalancePowerResidualWatts = 1e-3d;
    private static readonly UTF8Encoding Utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);

    [Fact]
    public void H30Rq1ValidatedManifest_RecordsActivatedProductionPolicyWithoutBundlingAuditPayloads()
    {
        var path = Path.Combine(FindRepositoryRoot(), "eng", "evidence-manifests", "h30-rq1-validated.csv");
        Assert.True(File.Exists(path), "H.30 RQ1 compact evidence manifest is missing.");
        var text = File.ReadAllText(path);
        Assert.Contains("decision,ACTIVATE", text, StringComparison.Ordinal);
        Assert.Contains("authoritative-default,integrated-operations-desktop-stable@3|FourNodeBranchContinuityCorrectedCommitOptIn", text, StringComparison.Ordinal);
        Assert.Contains("rollback-reference,integrated-operations-desktop-stable@2|ExplicitCommittedState", text, StringComparison.Ordinal);
        Assert.Contains("summary-sha256,5F615FB8125095721449B3299076FB192701B5CA91255DE1CCAE6070BEFBE2FE", text, StringComparison.Ordinal);
        Assert.Contains("metrics-sha256,96B8ECEC4026665B9DFB223CE1EA9040F66FAC3BEB02E7509C7793D12AF949FA", text, StringComparison.Ordinal);
    }

    [Fact]
    public void ProductionSelector_IsValidatedH30Rq1V3WithExactV2FailClosedRollback()
    {
        Assert.Equal(DesktopHydraulicProductionPolicy.H29FourNodeCorrectedCommitCandidate, DesktopHydraulicProductionPolicySelector.AuthoritativeDefaultPolicy);

        var production = DesktopHydraulicProductionPolicySelector.Resolve(DesktopHydraulicProductionPolicySelector.AuthoritativeDefaultPolicy);
        Assert.Equal(DesktopHydraulicProductionPolicy.H29FourNodeCorrectedCommitCandidate, production.EffectivePolicy);
        Assert.Equal("integrated-operations-desktop-stable", production.InitialCondition.InitialConditionId);
        Assert.Equal(3, production.InitialCondition.Version);
        Assert.False(production.ExplicitKillApplied);

        var rollback = DesktopHydraulicProductionPolicySelector.Resolve(DesktopHydraulicProductionPolicySelector.AuthoritativeDefaultPolicy, explicitKillRequested: true);
        Assert.Equal(DesktopHydraulicProductionPolicy.ExplicitCommittedState, rollback.EffectivePolicy);
        Assert.Equal("integrated-operations-desktop-stable", rollback.InitialCondition.InitialConditionId);
        Assert.Equal(2, rollback.InitialCondition.Version);
        Assert.True(rollback.ExplicitKillApplied);
    }

    [Fact]
    public void ProductionReferenceContract_IsAuthoritativeV3AndBudgetEstablishing()
    {
        var path = Path.Combine(FindRepositoryRoot(), "eng", "phase-i-reference-trajectory-contract.csv");
        Assert.True(File.Exists(path));
        var lines = File.ReadAllLines(path);
        Assert.Equal(2, lines.Length);
        Assert.Equal("trajectory_id,schema_version,exact_initial_condition,production_policy,simulated_seconds,logical_steps,step_health_resolution_ms,reference_sample_stride_steps,final_window_seconds,reference_role,budget_derivation,baseline_status", lines[0]);
        Assert.Equal("phase-i-production-v3-healthy-300s-v1,1,integrated-operations-desktop-stable@3,FourNodeBranchContinuityCorrectedCommitOptIn,300,30000,10,100,60,AUTHORITATIVE-PRODUCTION-REFERENCE,final-window-mean-plus-2x-observed-deviation-with-absolute-floor,CANDIDATE-TO-FREEZE-AFTER-I3", lines[1]);
    }

    [Fact(Explicit = true)]
    [Trait("Category", "PhaseIReferenceTrajectoryConservationInventoryBaselineAudit")]
    public void AuthoritativeProductionV3_ThreeHundredSeconds_EstablishesReferenceConservationInventoryAndToleranceBudgets()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable(OptInEnvironmentVariable), "1", StringComparison.Ordinal))
        {
            return;
        }

        ResetReportDirectory();
        Assert.Equal(DesktopHydraulicProductionPolicy.H29FourNodeCorrectedCommitCandidate, DesktopHydraulicProductionPolicySelector.AuthoritativeDefaultPolicy);
        var decision = DesktopHydraulicProductionPolicySelector.Resolve(DesktopHydraulicProductionPolicySelector.AuthoritativeDefaultPolicy);
        Assert.False(decision.ExplicitKillApplied);
        Assert.Equal("integrated-operations-desktop-stable", decision.InitialCondition.InitialConditionId);
        Assert.Equal(3, decision.InitialCondition.Version);

        var engine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            DesktopHydraulicProductionPolicySelector.CreateFactory(decision).CreateRuntimeEngine());
        Assert.Equal(TimeSpan.FromMilliseconds(10d), engine.FixedDeltaTime);
        Assert.Equal(HydraulicNumericalCouplingMode.FourNodeBranchContinuityCorrectedCommitOptIn, CurrentHydraulics(engine).Mode);

        var coordinator = new ControlRoomRuntimeCoordinator(engine);
        var samples = new List<ReferenceSample>(ReferenceSeconds + 1)
        {
            CaptureReferenceSample(engine, coordinator.Current),
        };
        var healthViolations = new List<StepObservation>();
        var reverseFlowViolations = new List<StepObservation>();
        var telemetryProbe = new DesktopHydraulicProductionTelemetryProbe();
        var maxMassClosure = 0d;
        var maxEnergyClosure = 0d;
        var maxBalanceMassRate = 0d;
        var maxBalancePower = 0d;

        for (var step = 1; step <= ReferenceSteps; step++)
        {
            var presentation = engine.Step(ControlRoomRunState.Running);
            telemetryProbe.Observe(engine);
            var observation = CaptureStepObservation(engine, presentation);
            AssertFinite(observation);

            maxMassClosure = Math.Max(maxMassClosure, Math.Abs(observation.MassClosureResidualKilograms));
            maxEnergyClosure = Math.Max(maxEnergyClosure, Math.Abs(observation.EnergyClosureResidualJoules));
            maxBalanceMassRate = Math.Max(maxBalanceMassRate, Math.Abs(observation.BalanceMassRateResidualKilogramsPerSecond));
            maxBalancePower = Math.Max(maxBalancePower, Math.Abs(observation.BalancePowerResidualWatts));

            if (!IsHealthy(observation))
            {
                healthViolations.Add(observation);
            }
            if (HasTargetedReverseFlow(observation))
            {
                reverseFlowViolations.Add(observation);
            }

            if (step % StepsPerSecond == 0)
            {
                samples.Add(CaptureReferenceSample(engine, presentation));
            }
            if (step % 3000 == 0)
            {
                File.AppendAllText(
                    Path.Combine(ReportDirectory(), "00-progress.txt"),
                    $"{DateTimeOffset.UtcNow:O} simulated-seconds={step / StepsPerSecond}; logical-step={step}{Environment.NewLine}",
                    Utf8WithoutBom);
            }
        }

        Assert.Equal(ReferenceSeconds + 1, samples.Count);
        Assert.Equal(ReferenceSteps, samples[^1].LogicalStep);

        var finalWindow = samples.Where(static sample => sample.SimulatedSeconds >= ReferenceSeconds - FinalWindowSeconds).ToArray();
        Assert.Equal(FinalWindowSeconds + 1, finalWindow.Length);
        var slopes = BuildInventorySlopes(finalWindow);
        Assert.Equal(7, slopes.Count);
        Assert.All(slopes, static slope => Assert.True(double.IsFinite(slope.SlopePerSecond)));
        var budgets = BuildToleranceBudgets(finalWindow, slopes);
        Assert.Equal(19, budgets.Count);
        Assert.All(budgets, static budget =>
        {
            Assert.True(double.IsFinite(budget.Target));
            Assert.True(double.IsFinite(budget.AbsoluteTolerance));
            Assert.True(budget.AbsoluteTolerance > 0d);
        });

        var telemetry = telemetryProbe.Snapshot();
        var deterministicFingerprintA = DeterminismFingerprint();
        var deterministicFingerprintB = DeterminismFingerprint();
        var deterministicRepeat = string.Equals(deterministicFingerprintA, deterministicFingerprintB, StringComparison.Ordinal);
        var trajectoryFingerprint = ComputeTrajectoryFingerprint(samples);

        var conservationPasses = maxMassClosure <= MaximumMassClosureResidualKilograms
            && maxEnergyClosure <= MaximumEnergyClosureResidualJoules
            && maxBalanceMassRate <= MaximumBalanceMassRateResidualKilogramsPerSecond
            && maxBalancePower <= MaximumBalancePowerResidualWatts
            && slopes.All(static slope => double.IsFinite(slope.SlopePerSecond))
            && budgets.All(static budget => double.IsFinite(budget.Target) && double.IsFinite(budget.AbsoluteTolerance) && budget.AbsoluteTolerance > 0d);
        var telemetryPasses = telemetry.ObservedSteps == ReferenceSteps
            && telemetry.FourNodeTelemetrySteps == ReferenceSteps
            && telemetry.TriggeredSteps > 0
            && telemetry.CandidateEligibleSteps == telemetry.TriggeredSteps
            && telemetry.CommitAuthorizedSteps == telemetry.TriggeredSteps
            && telemetry.CorrectedCommittedSteps == telemetry.TriggeredSteps
            && telemetry.RollbackSteps == 0
            && telemetry.ExplicitFallbackSteps == 0
            && telemetry.FallbackCommitViolations == 0
            && telemetry.UnsafeCommitViolations == 0
            && telemetry.UntargetedBranchDisagreementSteps == 0;
        var passes = healthViolations.Count == 0
            && reverseFlowViolations.Count == 0
            && conservationPasses
            && telemetryPasses
            && deterministicRepeat;

        WriteArtifacts(
            samples,
            slopes,
            budgets,
            healthViolations,
            reverseFlowViolations,
            telemetry,
            maxMassClosure,
            maxEnergyClosure,
            maxBalanceMassRate,
            maxBalancePower,
            deterministicFingerprintA,
            deterministicFingerprintB,
            deterministicRepeat,
            trajectoryFingerprint,
            conservationPasses,
            telemetryPasses,
            passes);

        Assert.True(
            passes,
            FormattableString.Invariant(
                $"I.3 authoritative production reference baseline failed. health-violations={healthViolations.Count}; targeted-reverse-flow={reverseFlowViolations.Count}; commits={telemetry.CorrectedCommittedSteps}; rollbacks={telemetry.RollbackSteps}; fallbacks={telemetry.ExplicitFallbackSteps}; unsafe={telemetry.UnsafeCommitViolations}; untargeted={telemetry.UntargetedBranchDisagreementSteps}; deterministic={deterministicRepeat}; max-closure={maxMassClosure:G17}/{maxEnergyClosure:G17}; max-balance={maxBalanceMassRate:G17}/{maxBalancePower:G17}."));
    }

    private static StepObservation CaptureStepObservation(IntegratedAutomaticOperationRuntimeEngine engine, ControlRoomSnapshot presentation)
    {
        var fullPlant = engine.LatestCanonicalSnapshot.Control.ProtectedControl.FullPlant;
        var turbine = fullPlant.IntegratedCycle.TurbineExpansion;
        var train = Assert.Single(turbine.MainSteamNetwork.AdmissionTrains);
        var stage = Assert.Single(turbine.StageGroups);
        var generator = Assert.Single(presentation.Electrical.Generators);
        var rotor = Assert.Single(presentation.TurbineSecondary.Rotors);
        return new StepObservation(
            presentation.LogicalStep,
            presentation.LogicalStep / (double)StepsPerSecond,
            presentation.AnyTripActive,
            generator.BreakerClosed,
            generator.RequestedElectricalPower.NumericValue ?? double.NaN,
            generator.ElectricalOutput.NumericValue ?? double.NaN,
            rotor.ShaftPower.NumericValue ?? double.NaN,
            turbine.TotalShaftPower.Megawatts,
            stage.EffectiveMassFlowRate.KilogramsPerSecond,
            train.StopValve.MassFlowRate.KilogramsPerSecond,
            train.ControlValve.MassFlowRate.KilogramsPerSecond,
            train.AdmissionValve.MassFlowRate.KilogramsPerSecond,
            fullPlant.HeatBalance.MassClosureResidualKilograms,
            fullPlant.HeatBalance.FullEnergyPathClosureResidualJoules,
            fullPlant.IntegratedCycle.ThermofluidAudit.BalanceMassRateResidualKilogramsPerSecond,
            fullPlant.IntegratedCycle.ThermofluidAudit.BalancePowerResidualWatts);
    }

    private static ReferenceSample CaptureReferenceSample(IntegratedAutomaticOperationRuntimeEngine engine, ControlRoomSnapshot presentation)
    {
        var fullPlant = engine.LatestCanonicalSnapshot.Control.ProtectedControl.FullPlant;
        var plant = fullPlant.CandidatePlant;
        var turbine = fullPlant.IntegratedCycle.TurbineExpansion;
        var admissionTrain = Assert.Single(turbine.MainSteamNetwork.AdmissionTrains);
        var condenser = Assert.Single(fullPlant.IntegratedCycle.Condenser.Condensers);
        var condensateTrain = Assert.Single(fullPlant.IntegratedCycle.CondensateFeedwater.Trains);
        var drum = Assert.Single(fullPlant.IntegratedCycle.PrimaryCircuit.SteamDrums.Drums);
        var steamLine = Assert.Single(turbine.MainSteamNetwork.SteamLines);
        var generator = Assert.Single(presentation.Electrical.Generators);
        var rotor = Assert.Single(presentation.TurbineSecondary.Rotors);
        var exhaust = plant.GetFluidNode(condenser.SteamSpaceNodeId);
        var hotwell = plant.GetFluidNode(condenser.HotwellNodeId);
        var feedwater = plant.GetFluidNode(condensateTrain.FeedwaterInventoryNodeId);
        var drumInventory = plant.GetFluidNode(drum.InventoryNodeId);
        var header = plant.GetFluidNode(steamLine.HeaderNodeId);
        return new ReferenceSample(
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
            admissionTrain.StopValve.MassFlowRate.KilogramsPerSecond,
            admissionTrain.ControlValve.MassFlowRate.KilogramsPerSecond,
            admissionTrain.AdmissionValve.MassFlowRate.KilogramsPerSecond,
            rotor.Speed.NumericValue ?? double.NaN,
            condenser.FinalSteamSpacePressure.Kilopascals,
            drum.LiquidLevelFraction.Fraction,
            plant.FluidNodes.Sum(static node => node.Mass.Kilograms),
            plant.FluidNodes.Sum(static node => node.InternalEnergy.Joules),
            exhaust.Mass.Kilograms,
            hotwell.Mass.Kilograms,
            feedwater.Mass.Kilograms,
            drumInventory.Mass.Kilograms,
            header.Mass.Kilograms);
    }

    private static bool IsHealthy(StepObservation observation)
        => !observation.AnyTrip
            && observation.GeneratorBreakerClosed
            && observation.RequestedElectricalPowerMegawatts > 4.5d
            && observation.GrossElectricalPowerMegawatts > 4.0d
            && observation.RotorShaftPowerMegawatts > 4.5d
            && observation.CanonicalShaftPowerMegawatts > 4.5d;

    private static bool HasTargetedReverseFlow(StepObservation observation)
        => observation.StopFlowKilogramsPerSecond < 0d
            || observation.ControlFlowKilogramsPerSecond < 0d
            || observation.AdmissionFlowKilogramsPerSecond < 0d;

    private static void AssertFinite(StepObservation observation)
    {
        foreach (var value in new[]
        {
            observation.SimulatedSeconds,
            observation.RequestedElectricalPowerMegawatts,
            observation.GrossElectricalPowerMegawatts,
            observation.RotorShaftPowerMegawatts,
            observation.CanonicalShaftPowerMegawatts,
            observation.StageFlowKilogramsPerSecond,
            observation.StopFlowKilogramsPerSecond,
            observation.ControlFlowKilogramsPerSecond,
            observation.AdmissionFlowKilogramsPerSecond,
            observation.MassClosureResidualKilograms,
            observation.EnergyClosureResidualJoules,
            observation.BalanceMassRateResidualKilogramsPerSecond,
            observation.BalancePowerResidualWatts,
        })
        {
            Assert.True(double.IsFinite(value), $"Non-finite corrected 300 s observation at logical step {observation.LogicalStep}.");
        }
    }

    private static IReadOnlyList<InventorySlope> BuildInventorySlopes(IReadOnlyList<ReferenceSample> window)
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

    private static InventorySlope BuildSlope(string metricId, string unit, IReadOnlyList<ReferenceSample> window, Func<ReferenceSample, double> selector)
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
        return new InventorySlope(metricId, unit, meanValue, denominator > 0d ? numerator / denominator : double.NaN);
    }

    private static IReadOnlyList<ToleranceBudget> BuildToleranceBudgets(
        IReadOnlyList<ReferenceSample> window,
        IReadOnlyList<InventorySlope> slopes)
    {
        var budgets = new List<ToleranceBudget>
        {
            BuildWindowBudget("gross-electrical-power", "MW", window, static sample => sample.GrossElectricalPowerMegawatts, 0.05d),
            BuildWindowBudget("shaft-power", "MW", window, static sample => sample.RotorShaftPowerMegawatts, 0.05d),
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
                "I3-production-final-60s-linear-slope; target-zero; freeze-after-validation"));
        }

        return budgets;
    }

    private static ToleranceBudget BuildWindowBudget(
        string metricId,
        string unit,
        IReadOnlyList<ReferenceSample> window,
        Func<ReferenceSample, double> selector,
        double absoluteFloor)
    {
        var target = window.Average(selector);
        var maximumDeviation = window.Max(sample => Math.Abs(selector(sample) - target));
        return new ToleranceBudget(
            metricId,
            unit,
            target,
            Math.Max(absoluteFloor, 2d * maximumDeviation),
            "I3-production-final-60s-mean; tolerance=max[absolute-floor;2x-observed-max-deviation]; freeze-after-validation");
    }

    private static string DeterminismFingerprint()
    {
        var decision = DesktopHydraulicProductionPolicySelector.Resolve(DesktopHydraulicProductionPolicySelector.AuthoritativeDefaultPolicy);
        var engine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            DesktopHydraulicProductionPolicySelector.CreateFactory(decision).CreateRuntimeEngine());
        var builder = new StringBuilder();
        for (var step = 1; step <= DeterminismSteps; step++)
        {
            var presentation = engine.Step(ControlRoomRunState.Running);
            var telemetry = CurrentHydraulics(engine).FourNodeBranchContinuity as FourNodeBranchContinuityIntegrationTelemetry;
            builder.Append(FormattableString.Invariant(
                $"{step}:{ControlRoomSnapshotFingerprint.Compute(presentation)}:{telemetry?.TriggerObserved}:{telemetry?.ProposedAuthority}:{telemetry?.Reason}:{telemetry?.RollbackRequired}:{telemetry?.CorrectedCommitAuthorized}:{telemetry?.CorrectedCandidateCommitted}:{telemetry?.CorrectedCommitReason}:{telemetry?.ShadowIterationCount}||"));
        }
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString())));
    }

    private static string ComputeTrajectoryFingerprint(IReadOnlyList<ReferenceSample> samples)
    {
        var text = string.Join("\n", samples.Select(static sample => FormattableString.Invariant(
            $"{sample.LogicalStep}|{sample.PresentationFingerprint}|{sample.AnyTrip}|{sample.GeneratorBreakerClosed}|{sample.RequestedElectricalPowerMegawatts:G17}|{sample.GrossElectricalPowerMegawatts:G17}|{sample.RotorShaftPowerMegawatts:G17}|{sample.CanonicalShaftPowerMegawatts:G17}|{sample.TotalSteamFlowKilogramsPerSecond:G17}|{sample.StopFlowKilogramsPerSecond:G17}|{sample.ControlFlowKilogramsPerSecond:G17}|{sample.AdmissionFlowKilogramsPerSecond:G17}|{sample.RotorSpeedRpm:G17}|{sample.CondenserPressureKilopascals:G17}|{sample.DrumLevelFraction:G17}|{sample.TotalFluidMassKilograms:G17}|{sample.TotalFluidInternalEnergyJoules:G17}|{sample.ExhaustMassKilograms:G17}|{sample.HotwellMassKilograms:G17}|{sample.FeedwaterInventoryMassKilograms:G17}|{sample.DrumInventoryMassKilograms:G17}|{sample.MainSteamHeaderMassKilograms:G17}")));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
    }

    private static void WriteArtifacts(
        IReadOnlyList<ReferenceSample> samples,
        IReadOnlyList<InventorySlope> slopes,
        IReadOnlyList<ToleranceBudget> budgets,
        IReadOnlyList<StepObservation> healthViolations,
        IReadOnlyList<StepObservation> reverseFlowViolations,
        FourNodeProductionActivationTelemetrySnapshot telemetry,
        double maxMassClosure,
        double maxEnergyClosure,
        double maxBalanceMassRate,
        double maxBalancePower,
        string determinismA,
        string determinismB,
        bool deterministicRepeat,
        string trajectoryFingerprint,
        bool conservationPasses,
        bool telemetryPasses,
        bool passes)
    {
        var directory = ReportDirectory();
        File.Copy(Path.Combine(FindRepositoryRoot(), "eng", "phase-i-reference-trajectory-contract.csv"), Path.Combine(directory, "02-reference-trajectory-contract.csv"), overwrite: true);

        var sampleLines = new List<string>
        {
            "logical_step,simulated_seconds,presentation_fingerprint,trip,breaker,request_mwe,gross_mwe,rotor_shaft_mwe,canonical_shaft_mwe,total_steam_flow_kg_s,stop_flow_kg_s,control_flow_kg_s,admission_flow_kg_s,rotor_rpm,condenser_kpa,drum_level_fraction,total_fluid_mass_kg,total_fluid_internal_energy_j,exhaust_mass_kg,hotwell_mass_kg,feedwater_mass_kg,drum_inventory_mass_kg,header_mass_kg",
        };
        sampleLines.AddRange(samples.Select(FormatReferenceSample));
        File.WriteAllLines(Path.Combine(directory, "03-reference-trajectory-samples.csv"), sampleLines, Utf8WithoutBom);

        var slopeLines = new List<string> { "metric_id,unit,final_window_mean,linear_slope_per_second" };
        slopeLines.AddRange(slopes.Select(static slope => FormattableString.Invariant($"{slope.MetricId},{slope.Unit},{slope.MeanValue:G17},{slope.SlopePerSecond:G17}")));
        File.WriteAllLines(Path.Combine(directory, "04-conservation-inventory-final-window-slopes.csv"), slopeLines, Utf8WithoutBom);

        var budgetLines = new List<string> { "metric_id,unit,target,absolute_tolerance,derivation" };
        budgetLines.AddRange(budgets.Select(static budget => FormattableString.Invariant($"{budget.MetricId},{budget.Unit},{budget.Target:G17},{budget.AbsoluteTolerance:G17},{budget.Derivation}")));
        File.WriteAllLines(Path.Combine(directory, "05-versioned-tolerance-budgets.csv"), budgetLines, Utf8WithoutBom);

        WriteStepObservations(Path.Combine(directory, "06-step-health-violations.csv"), healthViolations);
        WriteStepObservations(Path.Combine(directory, "07-targeted-reverse-flow-violations.csv"), reverseFlowViolations);

        var telemetryLines = new[]
        {
            "metric,value",
            $"observed_steps,{telemetry.ObservedSteps}",
            $"four_node_steps,{telemetry.FourNodeTelemetrySteps}",
            $"triggered,{telemetry.TriggeredSteps}",
            $"eligible,{telemetry.CandidateEligibleSteps}",
            $"authorized,{telemetry.CommitAuthorizedSteps}",
            $"committed,{telemetry.CorrectedCommittedSteps}",
            $"fallbacks,{telemetry.ExplicitFallbackSteps}",
            $"rollbacks,{telemetry.RollbackSteps}",
            $"fallback_commit_violations,{telemetry.FallbackCommitViolations}",
            $"unsafe_commits,{telemetry.UnsafeCommitViolations}",
            $"untargeted_disagreements,{telemetry.UntargetedBranchDisagreementSteps}",
        };
        File.WriteAllLines(Path.Combine(directory, "08-production-telemetry.csv"), telemetryLines, Utf8WithoutBom);
        File.WriteAllLines(Path.Combine(directory, "09-determinism-control.csv"), new[]
        {
            "control_steps,fingerprint_a,fingerprint_b,repeat",
            $"{DeterminismSteps},{determinismA},{determinismB},{deterministicRepeat}",
        }, Utf8WithoutBom);

        var summary = new[]
        {
            "=== 01-current-v3-phase-i-authoritative-reference-trajectory-conservation-inventory-tolerance-baseline ===",
            "I.3 runs the H.30 RQ1-authoritative production default for the full 300-second healthy reference horizon. Every 10 ms step is checked for generation health and reverse flow across stop/control/admission; one-second samples establish conservation/inventory observations, final-window slopes and versioned internal regression tolerance budgets. This gate does not retune runtime physics or numerical mathematics.",
            $"trajectory-id=phase-i-production-v3-healthy-300s-v1; exact-initial-condition=integrated-operations-desktop-stable@3; production-policy=FourNodeBranchContinuityCorrectedCommitOptIn; simulated-seconds={ReferenceSeconds}; logical-steps={ReferenceSteps}; step-health-resolution-ms=10; reference-samples={samples.Count}; final-window-seconds={FinalWindowSeconds};",
            $"generation-health-violations={healthViolations.Count}; targeted-reverse-flow-violations={reverseFlowViolations.Count}; trip-reference-samples={samples.Count(static sample => sample.AnyTrip)}; trajectory-fingerprint={trajectoryFingerprint}; final-presentation-fingerprint={samples[^1].PresentationFingerprint};",
            FormattableString.Invariant($"max-network-mass-closure-kg={maxMassClosure:G17}; max-network-energy-closure-j={maxEnergyClosure:G17}; max-network-balance-mass-rate-kg-s={maxBalanceMassRate:G17}; max-network-balance-power-w={maxBalancePower:G17}; inventory-slope-observations={slopes.Count}; tolerance-budget-entries={budgets.Count}; tolerance-budget-schema=I3-production-v1;"),
            $"corrected-triggered={telemetry.TriggeredSteps}; corrected-eligible={telemetry.CandidateEligibleSteps}; corrected-authorized={telemetry.CommitAuthorizedSteps}; corrected-committed={telemetry.CorrectedCommittedSteps}; corrected-rollbacks={telemetry.RollbackSteps}; corrected-fallbacks={telemetry.ExplicitFallbackSteps}; corrected-fallback-commit-violations={telemetry.FallbackCommitViolations}; corrected-unsafe={telemetry.UnsafeCommitViolations}; corrected-untargeted-disagreements={telemetry.UntargetedBranchDisagreementSteps};",
            $"determinism-control-steps={DeterminismSteps}; deterministic-repeat={deterministicRepeat}; deterministic-fingerprint={determinismA};",
            "authoritative-default=integrated-operations-desktop-stable@3|FourNodeBranchContinuityCorrectedCommitOptIn; rollback-reference=integrated-operations-desktop-stable@2|ExplicitCommittedState; phase-h-production-policy-decision=ACTIVATE; production-fixed-step=10.000 ms; runtime-behavior-changed=False; i3-reference-budgets-freeze-on-validation=True;",
            $"phase-i-reference-trajectory-baseline-passes={passes}; phase-i-generation-continuity-baseline-passes={healthViolations.Count == 0 && reverseFlowViolations.Count == 0}; phase-i-conservation-inventory-baseline-passes={conservationPasses}; phase-i-production-telemetry-baseline-passes={telemetryPasses}; phase-i-reference-determinism-passes={deterministicRepeat}; i3-audit-passes={passes}; phase-i-reference-tolerance-baseline-established={passes};",
            passes
                ? "I.3 recommendation: freeze this exact authoritative-v3 trajectory, final-window slopes and 19 tolerance budgets as the Phase-I production regression baseline. Keep the budgets regression-facing; do not tune runtime physics or seed values to fit them. Proceed to I.4 known-limitations and legacy-retirement review."
                : "I.3 recommendation: keep the reference baseline unfrozen. Do not weaken generation/continuity floors or retune runtime physics to fit candidate budgets; localize the failing production-reference condition first.",
        };
        File.WriteAllLines(Path.Combine(directory, "01-phase-i-reference-trajectory-conservation-inventory-baseline.summary.txt"), summary, Utf8WithoutBom);
    }

    private static void WriteStepObservations(string path, IReadOnlyList<StepObservation> observations)
    {
        var lines = new List<string>
        {
            "logical_step,simulated_seconds,trip,breaker,request_mwe,gross_mwe,rotor_shaft_mwe,canonical_shaft_mwe,stage_flow_kg_s,stop_flow_kg_s,control_flow_kg_s,admission_flow_kg_s,mass_closure_kg,energy_closure_j,balance_mass_rate_kg_s,balance_power_w",
        };
        lines.AddRange(observations.Select(static item => FormattableString.Invariant(
            $"{item.LogicalStep},{item.SimulatedSeconds:G17},{item.AnyTrip},{item.GeneratorBreakerClosed},{item.RequestedElectricalPowerMegawatts:G17},{item.GrossElectricalPowerMegawatts:G17},{item.RotorShaftPowerMegawatts:G17},{item.CanonicalShaftPowerMegawatts:G17},{item.StageFlowKilogramsPerSecond:G17},{item.StopFlowKilogramsPerSecond:G17},{item.ControlFlowKilogramsPerSecond:G17},{item.AdmissionFlowKilogramsPerSecond:G17},{item.MassClosureResidualKilograms:G17},{item.EnergyClosureResidualJoules:G17},{item.BalanceMassRateResidualKilogramsPerSecond:G17},{item.BalancePowerResidualWatts:G17}")));
        File.WriteAllLines(path, lines, Utf8WithoutBom);
    }

    private static string FormatReferenceSample(ReferenceSample sample)
        => string.Join(",", new[]
        {
            sample.LogicalStep.ToString(CultureInfo.InvariantCulture),
            sample.SimulatedSeconds.ToString("G17", CultureInfo.InvariantCulture),
            sample.PresentationFingerprint,
            sample.AnyTrip.ToString(),
            sample.GeneratorBreakerClosed.ToString(),
            sample.RequestedElectricalPowerMegawatts.ToString("G17", CultureInfo.InvariantCulture),
            sample.GrossElectricalPowerMegawatts.ToString("G17", CultureInfo.InvariantCulture),
            sample.RotorShaftPowerMegawatts.ToString("G17", CultureInfo.InvariantCulture),
            sample.CanonicalShaftPowerMegawatts.ToString("G17", CultureInfo.InvariantCulture),
            sample.TotalSteamFlowKilogramsPerSecond.ToString("G17", CultureInfo.InvariantCulture),
            sample.StopFlowKilogramsPerSecond.ToString("G17", CultureInfo.InvariantCulture),
            sample.ControlFlowKilogramsPerSecond.ToString("G17", CultureInfo.InvariantCulture),
            sample.AdmissionFlowKilogramsPerSecond.ToString("G17", CultureInfo.InvariantCulture),
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
        });

    private static PlantNetworkHydraulicNumericalSnapshot CurrentHydraulics(IntegratedAutomaticOperationRuntimeEngine engine)
        => engine.LatestCanonicalSnapshot.Control.ProtectedControl.FullPlant.IntegratedCycle.PrimaryCircuit.HydraulicNumerics;


    private static string ReportDirectory()
        => Path.Combine(FindRepositoryRoot(), "artifacts", "i3-phase-i-authoritative-reference-trajectory-baseline");

    private static void ResetReportDirectory()
    {
        var directory = ReportDirectory();
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "00-progress.txt"), $"{DateTimeOffset.UtcNow:O} I.3 authoritative production reference baseline started{Environment.NewLine}", Utf8WithoutBom);
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


    private sealed record StepObservation(
        long LogicalStep,
        double SimulatedSeconds,
        bool AnyTrip,
        bool GeneratorBreakerClosed,
        double RequestedElectricalPowerMegawatts,
        double GrossElectricalPowerMegawatts,
        double RotorShaftPowerMegawatts,
        double CanonicalShaftPowerMegawatts,
        double StageFlowKilogramsPerSecond,
        double StopFlowKilogramsPerSecond,
        double ControlFlowKilogramsPerSecond,
        double AdmissionFlowKilogramsPerSecond,
        double MassClosureResidualKilograms,
        double EnergyClosureResidualJoules,
        double BalanceMassRateResidualKilogramsPerSecond,
        double BalancePowerResidualWatts);

    private sealed record ReferenceSample(
        long LogicalStep,
        double SimulatedSeconds,
        string PresentationFingerprint,
        bool AnyTrip,
        bool GeneratorBreakerClosed,
        double RequestedElectricalPowerMegawatts,
        double GrossElectricalPowerMegawatts,
        double RotorShaftPowerMegawatts,
        double CanonicalShaftPowerMegawatts,
        double TotalSteamFlowKilogramsPerSecond,
        double StopFlowKilogramsPerSecond,
        double ControlFlowKilogramsPerSecond,
        double AdmissionFlowKilogramsPerSecond,
        double RotorSpeedRpm,
        double CondenserPressureKilopascals,
        double DrumLevelFraction,
        double TotalFluidMassKilograms,
        double TotalFluidInternalEnergyJoules,
        double ExhaustMassKilograms,
        double HotwellMassKilograms,
        double FeedwaterInventoryMassKilograms,
        double DrumInventoryMassKilograms,
        double MainSteamHeaderMassKilograms);

    private sealed record InventorySlope(string MetricId, string Unit, double MeanValue, double SlopePerSecond);

    private sealed record ToleranceBudget(string MetricId, string Unit, double Target, double AbsoluteTolerance, string Derivation);
}
