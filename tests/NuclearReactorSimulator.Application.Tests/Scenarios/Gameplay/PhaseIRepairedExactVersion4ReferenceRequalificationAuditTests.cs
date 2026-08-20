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
/// Final Phase-I production-reference requalification for the repaired exact-v4 desktop identity.
/// The historical exact-v3 I.3 evidence remains immutable; its 19 frozen regression budgets are
/// reused as acceptance authority and are not regenerated or widened by this gate.
/// </summary>
public sealed class PhaseIRepairedExactVersion4ReferenceRequalificationAuditTests
{
    private const string OptInEnvironmentVariable = "NRS_I5_REPAIRED_V4_300S_REFERENCE_AUDIT";
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
    public void HistoricalI3BudgetsRemainFrozenAndCurrentProductionContractIsRepairedExactV4()
    {
        var root = FindRepositoryRoot();
        var manifestPath = Path.Combine(root, "eng", "evidence-manifests", "i3-validated.csv");
        var budgetPath = FrozenBudgetPath();
        var contractPath = Path.Combine(root, "eng", "phase-i-repaired-v4-300s-reference-requalification-contract.csv");

        Assert.True(File.Exists(manifestPath));
        Assert.True(File.Exists(budgetPath));
        Assert.True(File.Exists(contractPath));

        var manifest = File.ReadAllText(manifestPath);
        Assert.Contains("status,VALIDATED", manifest, StringComparison.Ordinal);
        Assert.Contains("trajectory-id,phase-i-production-v3-healthy-300s-v1", manifest, StringComparison.Ordinal);
        Assert.Contains("authoritative-default,integrated-operations-desktop-stable@3|FourNodeBranchContinuityCorrectedCommitOptIn", manifest, StringComparison.Ordinal);
        Assert.Contains("tolerance-budget-count,19", manifest, StringComparison.Ordinal);
        Assert.Contains("budgets-sha256,9B7A2653F08059ECBD16F39FEB0DD7350F62C98A5892A8215D34404D6C9301BB", manifest, StringComparison.Ordinal);

        var budgets = ReadFrozenBudgets();
        Assert.Equal(19, budgets.Count);

        var contract = File.ReadAllLines(contractPath);
        Assert.Equal(2, contract.Length);
        Assert.Equal(
            "trajectory_id,schema_version,exact_initial_condition,production_policy,thermodynamic_closure,simulated_seconds,logical_steps,step_health_resolution_ms,reference_sample_stride_steps,final_window_seconds,budget_source,budget_count,reference_role",
            contract[0]);
        Assert.Equal(
            "phase-i-production-v4-repaired-healthy-300s-rq1,1,integrated-operations-desktop-stable@4,FourNodeBranchContinuityCorrectedCommitOptIn,CorrelationConsistentInverseDomain,300,30000,10,100,60,I3_ValidatedAuthoritativeToleranceBudgets.csv,19,PHASE-I-FINAL-PRODUCTION-REQUALIFICATION",
            contract[1]);

        Assert.Equal(
            DesktopHydraulicProductionPolicy.I5RepairedFourNodeCorrectedCommit,
            DesktopHydraulicProductionPolicySelector.AuthoritativeDefaultPolicy);

        var production = DesktopHydraulicProductionPolicySelector.Resolve(
            DesktopHydraulicProductionPolicySelector.AuthoritativeDefaultPolicy);
        Assert.Equal(DesktopHydraulicProductionPolicy.I5RepairedFourNodeCorrectedCommit, production.EffectivePolicy);
        Assert.Equal("integrated-operations-desktop-stable", production.InitialCondition.InitialConditionId);
        Assert.Equal(4, production.InitialCondition.Version);
        Assert.False(production.ExplicitKillApplied);

        var historicalV3 = DesktopHydraulicProductionPolicySelector.Resolve(
            DesktopHydraulicProductionPolicySelector.H29ActivationCandidatePolicy);
        Assert.Equal(DesktopHydraulicProductionPolicy.H29FourNodeCorrectedCommitCandidate, historicalV3.EffectivePolicy);
        Assert.Equal(3, historicalV3.InitialCondition.Version);

        var rollback = DesktopHydraulicProductionPolicySelector.Resolve(
            DesktopHydraulicProductionPolicySelector.AuthoritativeDefaultPolicy,
            explicitKillRequested: true);
        Assert.Equal(DesktopHydraulicProductionPolicy.ExplicitCommittedState, rollback.EffectivePolicy);
        Assert.Equal(2, rollback.InitialCondition.Version);
        Assert.True(rollback.ExplicitKillApplied);
    }

    [Fact(Explicit = true)]
    [Trait("Category", "PhaseIRepairedExactVersion4ReferenceRequalificationAudit")]
    public void AuthoritativeRepairedV4_ThreeHundredSeconds_RequalifiesFrozenI3BudgetsAndRemainsHealthy()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable(OptInEnvironmentVariable), "1", StringComparison.Ordinal))
        {
            return;
        }

        ResetReportDirectory();

        Assert.Equal(
            DesktopHydraulicProductionPolicy.I5RepairedFourNodeCorrectedCommit,
            DesktopHydraulicProductionPolicySelector.AuthoritativeDefaultPolicy);
        var decision = DesktopHydraulicProductionPolicySelector.Resolve(
            DesktopHydraulicProductionPolicySelector.AuthoritativeDefaultPolicy);
        Assert.False(decision.ExplicitKillApplied);
        Assert.Equal("integrated-operations-desktop-stable", decision.InitialCondition.InitialConditionId);
        Assert.Equal(4, decision.InitialCondition.Version);

        var engine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            DesktopHydraulicProductionPolicySelector.CreateFactory(decision).CreateRuntimeEngine());
        Assert.Equal(TimeSpan.FromMilliseconds(10d), engine.FixedDeltaTime);
        Assert.Equal(
            HydraulicNumericalCouplingMode.FourNodeBranchContinuityCorrectedCommitOptIn,
            CurrentHydraulics(engine).Mode);

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

        var finalWindow = samples
            .Where(static sample => sample.SimulatedSeconds >= ReferenceSeconds - FinalWindowSeconds)
            .ToArray();
        Assert.Equal(FinalWindowSeconds + 1, finalWindow.Length);

        var slopes = BuildInventorySlopes(finalWindow);
        Assert.Equal(7, slopes.Count);
        Assert.All(slopes, static slope => Assert.True(double.IsFinite(slope.SlopePerSecond)));

        var frozenBudgets = ReadFrozenBudgets();
        Assert.Equal(19, frozenBudgets.Count);
        var budgetComparisons = CompareFrozenBudgets(finalWindow, slopes, frozenBudgets);
        Assert.Equal(19, budgetComparisons.Count);

        var telemetry = telemetryProbe.Snapshot();
        var deterministicFingerprintA = DeterminismFingerprint();
        var deterministicFingerprintB = DeterminismFingerprint();
        var deterministicRepeat = string.Equals(
            deterministicFingerprintA,
            deterministicFingerprintB,
            StringComparison.Ordinal);
        var trajectoryFingerprint = ComputeTrajectoryFingerprint(samples);

        var conservationPasses = maxMassClosure <= MaximumMassClosureResidualKilograms
            && maxEnergyClosure <= MaximumEnergyClosureResidualJoules
            && maxBalanceMassRate <= MaximumBalanceMassRateResidualKilogramsPerSecond
            && maxBalancePower <= MaximumBalancePowerResidualWatts
            && slopes.All(static slope => double.IsFinite(slope.SlopePerSecond));
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
        var frozenBudgetPasses = budgetComparisons.All(static comparison => comparison.Passes);
        var passes = healthViolations.Count == 0
            && reverseFlowViolations.Count == 0
            && conservationPasses
            && telemetryPasses
            && frozenBudgetPasses
            && deterministicRepeat;

        WriteArtifacts(
            samples,
            slopes,
            frozenBudgets,
            budgetComparisons,
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
            frozenBudgetPasses,
            passes);

        Assert.True(
            passes,
            FormattableString.Invariant(
                $"I.5 repaired-v4 300 s reference requalification failed. health-violations={healthViolations.Count}; targeted-reverse-flow={reverseFlowViolations.Count}; frozen-budget-violations={budgetComparisons.Count(static comparison => !comparison.Passes)}; commits={telemetry.CorrectedCommittedSteps}; rollbacks={telemetry.RollbackSteps}; fallbacks={telemetry.ExplicitFallbackSteps}; unsafe={telemetry.UnsafeCommitViolations}; untargeted={telemetry.UntargetedBranchDisagreementSteps}; deterministic={deterministicRepeat}; max-closure={maxMassClosure:G17}/{maxEnergyClosure:G17}; max-balance={maxBalanceMassRate:G17}/{maxBalancePower:G17}."));
    }

    private static IReadOnlyList<ToleranceBudget> ReadFrozenBudgets()
    {
        var lines = File.ReadAllLines(FrozenBudgetPath());
        Assert.Equal(20, lines.Length);
        Assert.Equal("metric_id,unit,target,absolute_tolerance,derivation", lines[0]);

        return lines
            .Skip(1)
            .Where(static line => !string.IsNullOrWhiteSpace(line))
            .Select(static line =>
            {
                var fields = line.Split(',', 5, StringSplitOptions.None);
                if (fields.Length != 5)
                {
                    throw new InvalidDataException($"Invalid frozen I.3 budget row: {line}");
                }

                return new ToleranceBudget(
                    fields[0],
                    fields[1],
                    double.Parse(fields[2], NumberStyles.Float, CultureInfo.InvariantCulture),
                    double.Parse(fields[3], NumberStyles.Float, CultureInfo.InvariantCulture),
                    fields[4]);
            })
            .ToArray();
    }

    private static IReadOnlyList<BudgetComparison> CompareFrozenBudgets(
        IReadOnlyList<ReferenceSample> window,
        IReadOnlyList<InventorySlope> slopes,
        IReadOnlyList<ToleranceBudget> budgets)
    {
        var observed = new Dictionary<string, double>(StringComparer.Ordinal)
        {
            ["gross-electrical-power"] = window.Average(static sample => sample.GrossElectricalPowerMegawatts),
            ["shaft-power"] = window.Average(static sample => sample.RotorShaftPowerMegawatts),
            ["rotor-speed"] = window.Average(static sample => sample.RotorSpeedRpm),
            ["condenser-pressure"] = window.Average(static sample => sample.CondenserPressureKilopascals),
            ["drum-level-fraction"] = window.Average(static sample => sample.DrumLevelFraction),
            ["total-fluid-mass"] = window.Average(static sample => sample.TotalFluidMassKilograms),
            ["total-fluid-internal-energy"] = window.Average(static sample => sample.TotalFluidInternalEnergyJoules),
            ["exhaust-mass"] = window.Average(static sample => sample.ExhaustMassKilograms),
            ["hotwell-mass"] = window.Average(static sample => sample.HotwellMassKilograms),
            ["feedwater-inventory-mass"] = window.Average(static sample => sample.FeedwaterInventoryMassKilograms),
            ["drum-inventory-mass"] = window.Average(static sample => sample.DrumInventoryMassKilograms),
            ["main-steam-header-mass"] = window.Average(static sample => sample.MainSteamHeaderMassKilograms),
        };

        foreach (var slope in slopes)
        {
            observed[$"slope.{slope.MetricId}"] = slope.SlopePerSecond;
        }

        var result = new List<BudgetComparison>(budgets.Count);
        foreach (var budget in budgets)
        {
            if (!observed.TryGetValue(budget.MetricId, out var observedValue))
            {
                throw new InvalidDataException($"No repaired-v4 observation is mapped to frozen I.3 budget '{budget.MetricId}'.");
            }

            var absoluteDeviation = Math.Abs(observedValue - budget.Target);
            result.Add(new BudgetComparison(
                budget.MetricId,
                budget.Unit,
                budget.Target,
                budget.AbsoluteTolerance,
                observedValue,
                absoluteDeviation,
                absoluteDeviation <= budget.AbsoluteTolerance));
        }

        return result;
    }

    private static StepObservation CaptureStepObservation(
        IntegratedAutomaticOperationRuntimeEngine engine,
        ControlRoomSnapshot presentation)
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

    private static ReferenceSample CaptureReferenceSample(
        IntegratedAutomaticOperationRuntimeEngine engine,
        ControlRoomSnapshot presentation)
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
            Assert.True(double.IsFinite(value), $"Non-finite repaired-v4 300 s observation at logical step {observation.LogicalStep}.");
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

    private static InventorySlope BuildSlope(
        string metricId,
        string unit,
        IReadOnlyList<ReferenceSample> window,
        Func<ReferenceSample, double> selector)
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

        return new InventorySlope(
            metricId,
            unit,
            meanValue,
            denominator > 0d ? numerator / denominator : double.NaN);
    }

    private static string DeterminismFingerprint()
    {
        var decision = DesktopHydraulicProductionPolicySelector.Resolve(
            DesktopHydraulicProductionPolicySelector.AuthoritativeDefaultPolicy);
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
        IReadOnlyList<ToleranceBudget> frozenBudgets,
        IReadOnlyList<BudgetComparison> budgetComparisons,
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
        bool frozenBudgetPasses,
        bool passes)
    {
        var directory = ReportDirectory();
        File.Copy(
            Path.Combine(FindRepositoryRoot(), "eng", "phase-i-repaired-v4-300s-reference-requalification-contract.csv"),
            Path.Combine(directory, "02-repaired-v4-reference-contract.csv"),
            overwrite: true);

        var sampleLines = new List<string>
        {
            "logical_step,simulated_seconds,presentation_fingerprint,trip,breaker,request_mwe,gross_mwe,rotor_shaft_mwe,canonical_shaft_mwe,total_steam_flow_kg_s,stop_flow_kg_s,control_flow_kg_s,admission_flow_kg_s,rotor_rpm,condenser_kpa,drum_level_fraction,total_fluid_mass_kg,total_fluid_internal_energy_j,exhaust_mass_kg,hotwell_mass_kg,feedwater_mass_kg,drum_inventory_mass_kg,header_mass_kg",
        };
        sampleLines.AddRange(samples.Select(FormatReferenceSample));
        File.WriteAllLines(Path.Combine(directory, "03-repaired-v4-reference-trajectory-samples.csv"), sampleLines, Utf8WithoutBom);

        var slopeLines = new List<string> { "metric_id,unit,final_window_mean,linear_slope_per_second" };
        slopeLines.AddRange(slopes.Select(static slope => FormattableString.Invariant(
            $"{slope.MetricId},{slope.Unit},{slope.MeanValue:G17},{slope.SlopePerSecond:G17}")));
        File.WriteAllLines(Path.Combine(directory, "04-repaired-v4-final-window-slopes.csv"), slopeLines, Utf8WithoutBom);

        var comparisonLines = new List<string>
        {
            "metric_id,unit,frozen_target,frozen_absolute_tolerance,repaired_v4_observed,absolute_deviation,passes",
        };
        comparisonLines.AddRange(budgetComparisons.Select(static comparison => FormattableString.Invariant(
            $"{comparison.MetricId},{comparison.Unit},{comparison.FrozenTarget:G17},{comparison.FrozenAbsoluteTolerance:G17},{comparison.ObservedValue:G17},{comparison.AbsoluteDeviation:G17},{comparison.Passes}")));
        File.WriteAllLines(Path.Combine(directory, "05-frozen-i3-budget-comparison.csv"), comparisonLines, Utf8WithoutBom);

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
        File.WriteAllLines(
            Path.Combine(directory, "09-determinism-control.csv"),
            new[]
            {
                "control_steps,fingerprint_a,fingerprint_b,repeat",
                $"{DeterminismSteps},{determinismA},{determinismB},{deterministicRepeat}",
            },
            Utf8WithoutBom);

        var frozenBudgetLines = new List<string> { "metric_id,unit,target,absolute_tolerance,derivation" };
        frozenBudgetLines.AddRange(frozenBudgets.Select(static budget => FormattableString.Invariant(
            $"{budget.MetricId},{budget.Unit},{budget.Target:G17},{budget.AbsoluteTolerance:G17},{budget.Derivation}")));
        File.WriteAllLines(Path.Combine(directory, "10-frozen-i3-tolerance-budgets.csv"), frozenBudgetLines, Utf8WithoutBom);

        var budgetViolationCount = budgetComparisons.Count(static comparison => !comparison.Passes);
        var summary = new[]
        {
            "=== 01-i5-repaired-v4-300s-reference-requalification ===",
            "I.5 final production-reference requalification runs authoritative exact @4 for 300 simulated seconds while preserving the validated exact-v3 I.3 trajectory and its 19 budgets as immutable historical provenance. Every 10 ms step is checked for healthy generation and targeted-train continuity; final-window observations are compared against the frozen I.3 budgets without regeneration, widening or physical retuning.",
            $"trajectory-id=phase-i-production-v4-repaired-healthy-300s-rq1; exact-initial-condition=integrated-operations-desktop-stable@4; production-policy=FourNodeBranchContinuityCorrectedCommitOptIn; thermodynamic-closure=CorrelationConsistentInverseDomain; simulated-seconds={ReferenceSeconds}; logical-steps={ReferenceSteps}; step-health-resolution-ms=10; reference-samples={samples.Count}; final-window-seconds={FinalWindowSeconds};",
            $"generation-health-violations={healthViolations.Count}; targeted-reverse-flow-violations={reverseFlowViolations.Count}; trip-reference-samples={samples.Count(static sample => sample.AnyTrip)}; trajectory-fingerprint={trajectoryFingerprint}; final-presentation-fingerprint={samples[^1].PresentationFingerprint};",
            FormattableString.Invariant($"max-network-mass-closure-kg={maxMassClosure:G17}; max-network-energy-closure-j={maxEnergyClosure:G17}; max-network-balance-mass-rate-kg-s={maxBalanceMassRate:G17}; max-network-balance-power-w={maxBalancePower:G17}; inventory-slope-observations={slopes.Count}; frozen-i3-budget-comparisons={budgetComparisons.Count}; frozen-i3-budget-violations={budgetViolationCount};"),
            $"corrected-triggered={telemetry.TriggeredSteps}; corrected-eligible={telemetry.CandidateEligibleSteps}; corrected-authorized={telemetry.CommitAuthorizedSteps}; corrected-committed={telemetry.CorrectedCommittedSteps}; corrected-rollbacks={telemetry.RollbackSteps}; corrected-fallbacks={telemetry.ExplicitFallbackSteps}; corrected-fallback-commit-violations={telemetry.FallbackCommitViolations}; corrected-unsafe={telemetry.UnsafeCommitViolations}; corrected-untargeted-disagreements={telemetry.UntargetedBranchDisagreementSteps};",
            $"determinism-control-steps={DeterminismSteps}; deterministic-repeat={deterministicRepeat}; deterministic-fingerprint={determinismA};",
            "authoritative-default=integrated-operations-desktop-stable@4|CorrelationConsistentInverseDomain|FourNodeBranchContinuityCorrectedCommitOptIn; historical-i3-reference=integrated-operations-desktop-stable@3|HistoricalCorrelationTopology|FourNodeBranchContinuityCorrectedCommitOptIn; rollback-reference=integrated-operations-desktop-stable@2|HistoricalCorrelationTopology|ExplicitCommittedState; historical-i3-v3-reinterpreted=False; frozen-i3-budgets-retuned=False; production-fixed-step=10.000 ms;",
            $"repaired-v4-generation-continuity-passes={healthViolations.Count == 0 && reverseFlowViolations.Count == 0}; repaired-v4-conservation-inventory-passes={conservationPasses}; repaired-v4-production-telemetry-passes={telemetryPasses}; frozen-i3-budget-regression-passes={frozenBudgetPasses}; phase-i-reference-determinism-passes={deterministicRepeat}; repaired-v4-reference-requalification-passes={passes}; i5-repaired-v4-300s-reference-passes={passes};",
            passes
                ? "I.5 recommendation: accept authoritative exact @4 against the unchanged I.3 health/conservation/tolerance authority and proceed directly to cumulative M10.9.4.1 / Phase-I closure. Preserve exact @3 I.3 artifacts as historical provenance and exact @2 as fail-closed rollback/reference."
                : "I.5 recommendation: do not close Phase I. Preserve the frozen I.3 budgets and localize the exact repaired-v4 health, continuity, conservation or budget regression that failed; do not widen budgets to force acceptance.",
        };
        File.WriteAllLines(Path.Combine(directory, "01-i5-repaired-v4-300s-reference-requalification.summary.txt"), summary, Utf8WithoutBom);
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

    private static string FrozenBudgetPath()
        => Path.Combine(
            FindRepositoryRoot(),
            "eng",
            "frozen-evidence",
            "ordinary",
            "I3_ValidatedAuthoritativeToleranceBudgets.csv");

    private static string ReportDirectory()
        => Path.Combine(FindRepositoryRoot(), "artifacts", "i5-repaired-v4-300s-reference-requalification");

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
            $"{DateTimeOffset.UtcNow:O} I.5 repaired-v4 300 s reference requalification started{Environment.NewLine}",
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

    private sealed record ToleranceBudget(
        string MetricId,
        string Unit,
        double Target,
        double AbsoluteTolerance,
        string Derivation);

    private sealed record BudgetComparison(
        string MetricId,
        string Unit,
        double FrozenTarget,
        double FrozenAbsoluteTolerance,
        double ObservedValue,
        double AbsoluteDeviation,
        bool Passes);
}
