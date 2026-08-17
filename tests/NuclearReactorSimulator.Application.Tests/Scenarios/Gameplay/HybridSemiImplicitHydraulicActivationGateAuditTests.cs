using System.Diagnostics;
using System.Text;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios.Training;
using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using NuclearReactorSimulator.Simulation.Plant;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay;

/// <summary>
/// M10.9.4.1-H.4 audit-only hybrid activation/cost gate. Production current-v2 remains explicit at
/// 10 ms. The audit sweeps deterministic predictor thresholds and Picard controls over the same frozen
/// forcing used by H.3, then reports whether any bounded-work configuration preserves material chatter
/// improvement, convergence, conservation and deterministic repeat.
/// </summary>
public sealed class HybridSemiImplicitHydraulicActivationGateAuditTests
{
    private static readonly TimeSpan Step = TimeSpan.FromMilliseconds(10d);
    private static readonly Encoding Utf8WithoutBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    private const int IntervalCount = 50;

    [Fact(Explicit = true)]
    [Trait("Category", "HybridSemiImplicitHydraulicActivationGateAudit")]
    public void CurrentV2HybridSweep_RecordsQualityBoundedWorkDeterminismAndActivationRecommendation()
    {
        var thermodynamics = new SimplifiedWaterSteamThermodynamicModel();
        var prototype = new SemiImplicitHydraulicPrototypeSolver(thermodynamics);
        var hybrid = new HybridSemiImplicitHydraulicGateSolver(thermodynamics);
        var reference = BuildReferenceTrajectory(prototype);
        var explicitRun = RunExplicitTrajectory(reference, prototype);
        var candidates = BuildCandidateConfigurations()
            .Select(configuration => RunHybridTrajectory(reference, hybrid, configuration))
            .ToArray();

        Assert.Equal(IntervalCount, reference.Count);
        Assert.Equal(IntervalCount, explicitRun.Steps.Count);
        Assert.True(explicitRun.MaximumReferenceMassReplayErrorKilograms <= 1e-6d, explicitRun.ToString());
        Assert.True(explicitRun.MaximumReferenceEnergyReplayErrorJoules <= 1e-2d, explicitRun.ToString());
        Assert.All(candidates, candidate =>
        {
            Assert.Equal(IntervalCount, candidate.Run.Steps.Count);
            Assert.True(candidate.Run.MaximumInventoryIntegrationMassResidualKilograms <= 1e-6d, candidate.ToString());
            Assert.True(candidate.Run.MaximumInventoryIntegrationEnergyResidualJoules <= 1e-2d, candidate.ToString());
            Assert.True(candidate.Run.MaximumHydraulicMassRateClosureResidualKilogramsPerSecond <= 1e-8d, candidate.ToString());
            Assert.True(candidate.Run.MaximumHydraulicEnergyOwnershipResidualWatts <= 1e-3d, candidate.ToString());
            Assert.True(double.IsFinite(candidate.Run.WallSecondsPerSimulatedSecond));
            Assert.True(candidate.Run.WallSecondsPerSimulatedSecond >= 0d);
            Assert.True(double.IsFinite(candidate.Run.DeterministicWorkRatio));
            Assert.True(candidate.Run.DeterministicWorkRatio >= 1d);
        });

        var selected = SelectCandidate(candidates, explicitRun);
        var deterministicRepeat = RunHybridTrajectory(reference, hybrid, selected.Configuration);
        Assert.True(ExactlyDeterministic(selected.Run, deterministicRepeat.Run));

        WriteAuditReports(explicitRun, candidates, selected);
    }

    private static IReadOnlyList<HybridConfiguration> BuildCandidateConfigurations()
    {
        static SemiImplicitHydraulicPrototypeOptions Corrector(double relaxation, int maximumIterations)
            => new(maximumIterations, relaxation, 1e-5d, 1e-2d);

        return new[]
        {
            new HybridConfiguration("P085-R010", 0.085d, 1_000_000d, Corrector(0.10d, 96)),
            new HybridConfiguration("P075-R010", 0.075d, 1_000_000d, Corrector(0.10d, 96)),
            new HybridConfiguration("P060-R010", 0.060d, 1_000_000d, Corrector(0.10d, 96)),
            new HybridConfiguration("F060-R010", 1.000d, 60d, Corrector(0.10d, 96)),
            new HybridConfiguration("F040-R010", 1.000d, 40d, Corrector(0.10d, 96)),
            new HybridConfiguration("P080-F060-R015", 0.080d, 60d, Corrector(0.15d, 72)),
            new HybridConfiguration("P075-F050-R015", 0.075d, 50d, Corrector(0.15d, 72)),
            new HybridConfiguration("P060-F040-R015", 0.060d, 40d, Corrector(0.15d, 72)),
        };
    }

    private static IReadOnlyList<ReferenceInterval> BuildReferenceTrajectory(SemiImplicitHydraulicPrototypeSolver solver)
    {
        var engine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            DesktopSustainedGenerationInitialConditionFactory.CreateNumericalStiffnessEvidenceRuntimeEngine(Step));
        var intervals = new List<ReferenceInterval>(IntervalCount);

        for (var index = 0; index < IntervalCount; index++)
        {
            var start = ToPlantState(engine.LatestCanonicalSnapshot.Control.ProtectedControl.FullPlant.CandidatePlant);
            var presentation = engine.Step(ControlRoomRunState.Running);
            Assert.False(presentation.AnyTripActive, $"Unexpected reference trip at H.4 interval {index + 1}.");
            var end = ToPlantState(engine.LatestCanonicalSnapshot.Control.ProtectedControl.FullPlant.CandidatePlant);
            var hydraulic = solver.Evaluate(start);
            var totalBalances = DeriveInventoryBalances(start, end, Step);
            var frozen = start.FluidNodes.ToDictionary(
                static node => node.Id,
                node => totalBalances[node.Id] - hydraulic.FluidNodeBalances[node.Id],
                StringComparer.Ordinal);

            intervals.Add(new ReferenceInterval(index + 1, start, end, frozen));
        }

        return intervals;
    }

    private static TrajectoryRun RunExplicitTrajectory(
        IReadOnlyList<ReferenceInterval> reference,
        SemiImplicitHydraulicPrototypeSolver solver)
    {
        var current = reference[0].Start;
        var accumulator = new RunAccumulator(reference.Count);
        var stopwatch = Stopwatch.StartNew();

        foreach (var interval in reference)
        {
            current = RebindDiscreteAndThermalState(current, interval.Start);
            var start = current;
            var result = solver.StepExplicit(start, Step, interval.FrozenNonHydraulicBalances);
            current = result.CandidateState;
            var endEvaluation = solver.Evaluate(current);

            accumulator.MaximumReferenceMassReplayErrorKilograms = Math.Max(
                accumulator.MaximumReferenceMassReplayErrorKilograms,
                MaximumInventoryDifference(current, interval.End, static node => node.Mass.Kilograms));
            accumulator.MaximumReferenceEnergyReplayErrorJoules = Math.Max(
                accumulator.MaximumReferenceEnergyReplayErrorJoules,
                MaximumInventoryDifference(current, interval.End, static node => node.InternalEnergy.Joules));

            AccumulateCommonEvidence(
                accumulator,
                interval,
                start,
                current,
                endEvaluation,
                result.AppliedHydraulicBalances,
                iterationCount: 1,
                converged: true,
                usedCorrection: false,
                predictorPressureChange: MaximumFractionalSubcooledPressureChange(start, current),
                predictorFlowChange: 0d,
                pressureResidual: 0d,
                flowResidual: 0d);
        }

        stopwatch.Stop();
        return accumulator.Build(reference, current, stopwatch.Elapsed.TotalSeconds);
    }

    private static HybridCandidateRun RunHybridTrajectory(
        IReadOnlyList<ReferenceInterval> reference,
        HybridSemiImplicitHydraulicGateSolver solver,
        HybridConfiguration configuration)
    {
        var options = new HybridSemiImplicitHydraulicGateOptions(
            configuration.PressureTriggerFraction,
            configuration.FlowTriggerKilogramsPerSecond,
            configuration.CorrectorOptions);
        var current = reference[0].Start;
        var accumulator = new RunAccumulator(reference.Count);
        var stopwatch = Stopwatch.StartNew();

        foreach (var interval in reference)
        {
            current = RebindDiscreteAndThermalState(current, interval.Start);
            var start = current;
            var result = solver.Step(start, Step, interval.FrozenNonHydraulicBalances, options);
            current = result.CandidateState;

            AccumulateCommonEvidence(
                accumulator,
                interval,
                start,
                current,
                result.HydraulicEvaluation,
                result.AppliedHydraulicBalances,
                result.IterationCount,
                result.Converged,
                result.UsedSemiImplicitCorrection,
                result.PredictorMaximumFractionalSubcooledPressureChange,
                result.PredictorMaximumAbsoluteHydraulicFlowChangeKilogramsPerSecond,
                result.MaximumRelativePressureResidual,
                result.MaximumAbsoluteFlowResidualKilogramsPerSecond);
        }

        stopwatch.Stop();
        return new HybridCandidateRun(configuration, accumulator.Build(reference, current, stopwatch.Elapsed.TotalSeconds));
    }

    private static void AccumulateCommonEvidence(
        RunAccumulator accumulator,
        ReferenceInterval interval,
        PlantState start,
        PlantState end,
        SemiImplicitHydraulicEvaluation endEvaluation,
        IReadOnlyDictionary<string, FluidNodeBalance> appliedHydraulicBalances,
        int iterationCount,
        bool converged,
        bool usedCorrection,
        double predictorPressureChange,
        double predictorFlowChange,
        double pressureResidual,
        double flowResidual)
    {
        var inventoryResidual = InventoryIntegrationResidual(
            start,
            end,
            appliedHydraulicBalances,
            interval.FrozenNonHydraulicBalances,
            Step);
        accumulator.MaximumInventoryIntegrationMassResidualKilograms = Math.Max(
            accumulator.MaximumInventoryIntegrationMassResidualKilograms,
            inventoryResidual.MassKilograms);
        accumulator.MaximumInventoryIntegrationEnergyResidualJoules = Math.Max(
            accumulator.MaximumInventoryIntegrationEnergyResidualJoules,
            inventoryResidual.EnergyJoules);
        accumulator.MaximumHydraulicMassRateClosureResidualKilogramsPerSecond = Math.Max(
            accumulator.MaximumHydraulicMassRateClosureResidualKilogramsPerSecond,
            endEvaluation.MassRateClosureResidualKilogramsPerSecond);
        accumulator.MaximumHydraulicEnergyOwnershipResidualWatts = Math.Max(
            accumulator.MaximumHydraulicEnergyOwnershipResidualWatts,
            endEvaluation.HydraulicEnergyOwnershipResidualWatts);

        var pumpFlow = endEvaluation.GetPumpMassFlowRate("pump").KilogramsPerSecond;
        var channelFlow = endEvaluation.GetPipeMassFlowRate("channel").KilogramsPerSecond;
        var returnFlow = endEvaluation.GetPipeMassFlowRate("return").KilogramsPerSecond;
        if (double.IsFinite(accumulator.PreviousPumpFlowKilogramsPerSecond))
        {
            accumulator.MaximumPumpFlowStepChangeKilogramsPerSecond = Math.Max(
                accumulator.MaximumPumpFlowStepChangeKilogramsPerSecond,
                Math.Abs(pumpFlow - accumulator.PreviousPumpFlowKilogramsPerSecond));
            accumulator.MaximumChannelFlowStepChangeKilogramsPerSecond = Math.Max(
                accumulator.MaximumChannelFlowStepChangeKilogramsPerSecond,
                Math.Abs(channelFlow - accumulator.PreviousChannelFlowKilogramsPerSecond));
            accumulator.MaximumReturnFlowStepChangeKilogramsPerSecond = Math.Max(
                accumulator.MaximumReturnFlowStepChangeKilogramsPerSecond,
                Math.Abs(returnFlow - accumulator.PreviousReturnFlowKilogramsPerSecond));
        }

        accumulator.PreviousPumpFlowKilogramsPerSecond = pumpFlow;
        accumulator.PreviousChannelFlowKilogramsPerSecond = channelFlow;
        accumulator.PreviousReturnFlowKilogramsPerSecond = returnFlow;
        var pressureChange = MaximumFractionalSubcooledPressureChange(start, end);
        accumulator.MaximumFractionalSubcooledPressureChange = Math.Max(
            accumulator.MaximumFractionalSubcooledPressureChange,
            pressureChange);
        accumulator.MaximumPredictorPressureChange = Math.Max(
            accumulator.MaximumPredictorPressureChange,
            predictorPressureChange);
        accumulator.MaximumPredictorFlowChangeKilogramsPerSecond = Math.Max(
            accumulator.MaximumPredictorFlowChangeKilogramsPerSecond,
            predictorFlowChange);
        accumulator.IterationSum += iterationCount;
        accumulator.CorrectorIterationSum += usedCorrection ? iterationCount : 0;
        accumulator.MaximumIterationCount = Math.Max(accumulator.MaximumIterationCount, iterationCount);
        if (usedCorrection)
        {
            accumulator.CorrectionCount++;
            if (converged)
            {
                accumulator.ConvergedCorrectionCount++;
            }
        }

        accumulator.MaximumRelativePressureResidual = Math.Max(
            accumulator.MaximumRelativePressureResidual,
            pressureResidual);
        accumulator.MaximumAbsoluteFlowResidualKilogramsPerSecond = Math.Max(
            accumulator.MaximumAbsoluteFlowResidualKilogramsPerSecond,
            flowResidual);
        accumulator.Steps.Add(new StepEvidence(
            interval.Index,
            pumpFlow,
            channelFlow,
            returnFlow,
            pressureChange,
            usedCorrection,
            iterationCount,
            converged,
            predictorPressureChange,
            predictorFlowChange,
            pressureResidual,
            flowResidual));
    }

    private static HybridCandidateRun SelectCandidate(
        IReadOnlyList<HybridCandidateRun> candidates,
        TrajectoryRun explicitRun)
    {
        return candidates
            .OrderBy(candidate => ActivationCriteria(candidate.Run, explicitRun) ? 0 : 1)
            .ThenBy(static candidate => candidate.Run.DeterministicWorkRatio)
            .ThenBy(candidate => QualityScore(candidate.Run, explicitRun))
            .ThenBy(static candidate => candidate.Configuration.Id, StringComparer.Ordinal)
            .First();
    }

    private static bool ActivationCriteria(TrajectoryRun candidate, TrajectoryRun explicitRun)
    {
        var pumpRatio = SafeRatio(candidate.MaximumPumpFlowStepChangeKilogramsPerSecond, explicitRun.MaximumPumpFlowStepChangeKilogramsPerSecond);
        var channelRatio = SafeRatio(candidate.MaximumChannelFlowStepChangeKilogramsPerSecond, explicitRun.MaximumChannelFlowStepChangeKilogramsPerSecond);
        var returnRatio = SafeRatio(candidate.MaximumReturnFlowStepChangeKilogramsPerSecond, explicitRun.MaximumReturnFlowStepChangeKilogramsPerSecond);
        var pressureRatio = SafeRatio(candidate.MaximumFractionalSubcooledPressureChange, explicitRun.MaximumFractionalSubcooledPressureChange);

        return candidate.CorrectionCount > 0
            && candidate.ConvergedCorrectionCount == candidate.CorrectionCount
            && candidate.DeterministicWorkRatio <= 4d
            && pumpRatio <= 0.80d
            && channelRatio <= 0.60d
            && returnRatio <= 0.50d
            && pressureRatio <= 1.00d
            && candidate.MaximumFinalMassRelativeDifference <= 0.001d
            && candidate.MaximumFinalEnergyRelativeDifference <= 0.001d
            && candidate.MaximumFinalPressureRelativeDifference <= 0.010d
            && candidate.MaximumInventoryIntegrationMassResidualKilograms <= 1e-6d
            && candidate.MaximumInventoryIntegrationEnergyResidualJoules <= 1e-2d
            && candidate.MaximumHydraulicMassRateClosureResidualKilogramsPerSecond <= 1e-8d
            && candidate.MaximumHydraulicEnergyOwnershipResidualWatts <= 1e-3d;
    }

    private static double QualityScore(TrajectoryRun candidate, TrajectoryRun explicitRun)
        => SafeRatio(candidate.MaximumPumpFlowStepChangeKilogramsPerSecond, explicitRun.MaximumPumpFlowStepChangeKilogramsPerSecond)
            + SafeRatio(candidate.MaximumChannelFlowStepChangeKilogramsPerSecond, explicitRun.MaximumChannelFlowStepChangeKilogramsPerSecond)
            + SafeRatio(candidate.MaximumReturnFlowStepChangeKilogramsPerSecond, explicitRun.MaximumReturnFlowStepChangeKilogramsPerSecond)
            + SafeRatio(candidate.MaximumFractionalSubcooledPressureChange, explicitRun.MaximumFractionalSubcooledPressureChange);

    private static PlantState ToPlantState(PlantSnapshot snapshot)
        => new(
            snapshot.Definition,
            snapshot.FluidNodes,
            snapshot.Valves,
            snapshot.Pumps,
            snapshot.ThermalBodies,
            snapshot.HeatSources);

    private static PlantState RebindDiscreteAndThermalState(PlantState fluidOwner, PlantState reference)
        => new(
            reference.Definition,
            fluidOwner.FluidNodes,
            reference.Valves,
            reference.Pumps,
            reference.ThermalBodies,
            reference.HeatSources);

    private static IReadOnlyDictionary<string, FluidNodeBalance> DeriveInventoryBalances(
        PlantState start,
        PlantState end,
        TimeSpan deltaTime)
    {
        var endNodes = end.FluidNodes.ToDictionary(static node => node.Id, StringComparer.Ordinal);
        var seconds = deltaTime.TotalSeconds;
        return start.FluidNodes.ToDictionary(
            static node => node.Id,
            node => new FluidNodeBalance(
                MassFlowRate.FromKilogramsPerSecond((endNodes[node.Id].Mass.Kilograms - node.Mass.Kilograms) / seconds),
                Power.FromWatts((endNodes[node.Id].InternalEnergy.Joules - node.InternalEnergy.Joules) / seconds)),
            StringComparer.Ordinal);
    }

    private static InventoryResidual InventoryIntegrationResidual(
        PlantState start,
        PlantState end,
        IReadOnlyDictionary<string, FluidNodeBalance> hydraulic,
        IReadOnlyDictionary<string, FluidNodeBalance> frozen,
        TimeSpan deltaTime)
    {
        var endNodes = end.FluidNodes.ToDictionary(static node => node.Id, StringComparer.Ordinal);
        var maximumMassResidual = 0d;
        var maximumEnergyResidual = 0d;
        var seconds = deltaTime.TotalSeconds;
        foreach (var startNode in start.FluidNodes)
        {
            var frozenBalance = frozen.TryGetValue(startNode.Id, out var value) ? value : FluidNodeBalance.Zero;
            var total = hydraulic[startNode.Id] + frozenBalance;
            var expectedMass = startNode.Mass.Kilograms + (total.NetMassFlowRate.KilogramsPerSecond * seconds);
            var expectedEnergy = startNode.InternalEnergy.Joules + (total.NetEnergyRate.Watts * seconds);
            var endNode = endNodes[startNode.Id];
            maximumMassResidual = Math.Max(maximumMassResidual, Math.Abs(endNode.Mass.Kilograms - expectedMass));
            maximumEnergyResidual = Math.Max(maximumEnergyResidual, Math.Abs(endNode.InternalEnergy.Joules - expectedEnergy));
        }

        return new InventoryResidual(maximumMassResidual, maximumEnergyResidual);
    }

    private static double MaximumFractionalSubcooledPressureChange(PlantState start, PlantState end)
    {
        var endNodes = end.FluidNodes.ToDictionary(static node => node.Id, StringComparer.Ordinal);
        var maximum = 0d;
        foreach (var startNode in start.FluidNodes.Where(static node => node.Phase == FluidPhase.SubcooledLiquid))
        {
            var endPressure = endNodes[startNode.Id].Pressure.Pascals;
            var scale = Math.Max(Math.Abs(startNode.Pressure.Pascals), 1_000d);
            maximum = Math.Max(maximum, Math.Abs(endPressure - startNode.Pressure.Pascals) / scale);
        }

        return maximum;
    }

    private static double MaximumInventoryDifference(
        PlantState left,
        PlantState right,
        Func<FluidNodeState, double> selector)
    {
        var rightNodes = right.FluidNodes.ToDictionary(static node => node.Id, StringComparer.Ordinal);
        return left.FluidNodes.Max(node => Math.Abs(selector(node) - selector(rightNodes[node.Id])));
    }

    private static double MaximumRelativeFinalStateDifference(
        PlantState left,
        PlantState right,
        Func<FluidNodeState, double> selector)
    {
        var rightNodes = right.FluidNodes.ToDictionary(static node => node.Id, StringComparer.Ordinal);
        var maximum = 0d;
        foreach (var node in left.FluidNodes)
        {
            var leftValue = selector(node);
            var rightValue = selector(rightNodes[node.Id]);
            var scale = Math.Max(Math.Max(Math.Abs(leftValue), Math.Abs(rightValue)), 1e-12d);
            maximum = Math.Max(maximum, Math.Abs(leftValue - rightValue) / scale);
        }

        return maximum;
    }

    private static bool ExactlyDeterministic(TrajectoryRun left, TrajectoryRun right)
        => left.Steps.SequenceEqual(right.Steps)
            && left.FinalNodes.SequenceEqual(right.FinalNodes)
            && left.CorrectionCount == right.CorrectionCount
            && left.ConvergedCorrectionCount == right.ConvergedCorrectionCount
            && left.AverageIterationCount == right.AverageIterationCount
            && left.MaximumIterationCount == right.MaximumIterationCount
            && left.MaximumRelativePressureResidual == right.MaximumRelativePressureResidual
            && left.MaximumAbsoluteFlowResidualKilogramsPerSecond == right.MaximumAbsoluteFlowResidualKilogramsPerSecond
            && left.DeterministicWorkRatio == right.DeterministicWorkRatio;

    private static void WriteAuditReports(
        TrajectoryRun explicitRun,
        IReadOnlyList<HybridCandidateRun> candidates,
        HybridCandidateRun selected)
    {
        var directory = Path.Combine(FindRepositoryRoot(), "artifacts", "h4-hybrid-semi-implicit-gate");
        Directory.CreateDirectory(directory);

        var sweep = new StringBuilder();
        sweep.AppendLine("configuration,pressure_trigger_fraction,flow_trigger_kg_s,relaxation,max_iterations,corrections,correction_fraction,converged_corrections,average_iterations,max_iterations_used,deterministic_work_ratio,pump_chatter_ratio,channel_chatter_ratio,return_chatter_ratio,pressure_ratio,final_mass_gap,final_energy_gap,final_pressure_gap,wall_seconds_per_simulated_second,wall_cost_ratio,activation_criteria");
        foreach (var candidate in candidates.OrderBy(static item => item.Configuration.Id, StringComparer.Ordinal))
        {
            var run = candidate.Run;
            sweep.AppendLine(FormattableString.Invariant(
                $"{candidate.Configuration.Id},{candidate.Configuration.PressureTriggerFraction:R},{candidate.Configuration.FlowTriggerKilogramsPerSecond:R},{candidate.Configuration.CorrectorOptions.RelaxationFactor:R},{candidate.Configuration.CorrectorOptions.MaximumIterations},{run.CorrectionCount},{run.CorrectionCount / (double)run.Steps.Count:R},{run.ConvergedCorrectionCount},{run.AverageIterationCount:R},{run.MaximumIterationCount},{run.DeterministicWorkRatio:R},{SafeRatio(run.MaximumPumpFlowStepChangeKilogramsPerSecond, explicitRun.MaximumPumpFlowStepChangeKilogramsPerSecond):R},{SafeRatio(run.MaximumChannelFlowStepChangeKilogramsPerSecond, explicitRun.MaximumChannelFlowStepChangeKilogramsPerSecond):R},{SafeRatio(run.MaximumReturnFlowStepChangeKilogramsPerSecond, explicitRun.MaximumReturnFlowStepChangeKilogramsPerSecond):R},{SafeRatio(run.MaximumFractionalSubcooledPressureChange, explicitRun.MaximumFractionalSubcooledPressureChange):R},{run.MaximumFinalMassRelativeDifference:R},{run.MaximumFinalEnergyRelativeDifference:R},{run.MaximumFinalPressureRelativeDifference:R},{run.WallSecondsPerSimulatedSecond:R},{SafeRatio(run.WallSecondsPerSimulatedSecond, explicitRun.WallSecondsPerSimulatedSecond):R},{ActivationCriteria(run, explicitRun)}"));
        }
        File.WriteAllText(Path.Combine(directory, "01-current-v2-hybrid-sweep.csv"), sweep.ToString(), Utf8WithoutBom);

        var selectedTrajectory = new StringBuilder();
        selectedTrajectory.AppendLine("step,explicit_pump_kg_s,hybrid_pump_kg_s,explicit_channel_kg_s,hybrid_channel_kg_s,explicit_return_kg_s,hybrid_return_kg_s,explicit_pressure_fraction,hybrid_pressure_fraction,used_correction,iterations,predictor_pressure_fraction,predictor_flow_change_kg_s,pressure_residual,flow_residual_kg_s");
        for (var index = 0; index < explicitRun.Steps.Count; index++)
        {
            var explicitStep = explicitRun.Steps[index];
            var hybridStep = selected.Run.Steps[index];
            selectedTrajectory.AppendLine(FormattableString.Invariant(
                $"{explicitStep.Index},{explicitStep.PumpFlowKilogramsPerSecond:R},{hybridStep.PumpFlowKilogramsPerSecond:R},{explicitStep.ChannelFlowKilogramsPerSecond:R},{hybridStep.ChannelFlowKilogramsPerSecond:R},{explicitStep.ReturnFlowKilogramsPerSecond:R},{hybridStep.ReturnFlowKilogramsPerSecond:R},{explicitStep.MaximumFractionalSubcooledPressureChange:R},{hybridStep.MaximumFractionalSubcooledPressureChange:R},{hybridStep.UsedCorrection},{hybridStep.IterationCount},{hybridStep.PredictorPressureChange:R},{hybridStep.PredictorFlowChangeKilogramsPerSecond:R},{hybridStep.MaximumRelativePressureResidual:R},{hybridStep.MaximumAbsoluteFlowResidualKilogramsPerSecond:R}"));
        }
        File.WriteAllText(Path.Combine(directory, "02-current-v2-selected-hybrid-trajectory.csv"), selectedTrajectory.ToString(), Utf8WithoutBom);

        var finalCsv = new StringBuilder();
        finalCsv.AppendLine("node_id,hybrid_mass_kg,hybrid_internal_energy_j,hybrid_pressure_pa");
        foreach (var node in selected.Run.FinalNodes)
        {
            finalCsv.AppendLine(FormattableString.Invariant($"{node.Id},{node.MassKilograms:R},{node.InternalEnergyJoules:R},{node.PressurePascals:R}"));
        }
        File.WriteAllText(Path.Combine(directory, "03-current-v2-selected-final-state.csv"), finalCsv.ToString(), Utf8WithoutBom);

        var runSelected = selected.Run;
        var activationRecommended = ActivationCriteria(runSelected, explicitRun);
        var summaryLines = new[]
        {
            "================================================================================",
            "M10.9.4.1-H.4 HYBRID SEMI-IMPLICIT ACTIVATION & COST GATE SUMMARY",
            "================================================================================",
            "=== 01-current-v2-hybrid-sweep ===",
            "Audit-only deterministic explicit-predictor/semi-implicit-corrector sweep over the validated H.3 frozen-forcing window; wall-clock cost is observational and never participates in deterministic trigger or selection logic.",
            FormattableString.Invariant($"configurations={candidates.Count}; steps-per-configuration={runSelected.Steps.Count}; selected={selected.Configuration.Id}; pressure-trigger={selected.Configuration.PressureTriggerFraction:0.000000}; flow-trigger={selected.Configuration.FlowTriggerKilogramsPerSecond:0.000} kg/s; relaxation={selected.Configuration.CorrectorOptions.RelaxationFactor:0.000}; max-corrector-iterations={selected.Configuration.CorrectorOptions.MaximumIterations};"),
            FormattableString.Invariant($"selected-corrections={runSelected.CorrectionCount}/{runSelected.Steps.Count}; correction-fraction={runSelected.CorrectionCount / (double)runSelected.Steps.Count:0.000000}; converged-corrections={runSelected.ConvergedCorrectionCount}/{runSelected.CorrectionCount}; average-iterations-all-steps={runSelected.AverageIterationCount:0.000}; maximum-iterations={runSelected.MaximumIterationCount}; deterministic-work-ratio={runSelected.DeterministicWorkRatio:0.000000};"),
            FormattableString.Invariant($"chatter-ratios hybrid/explicit: pump={SafeRatio(runSelected.MaximumPumpFlowStepChangeKilogramsPerSecond, explicitRun.MaximumPumpFlowStepChangeKilogramsPerSecond):0.000000}; channel={SafeRatio(runSelected.MaximumChannelFlowStepChangeKilogramsPerSecond, explicitRun.MaximumChannelFlowStepChangeKilogramsPerSecond):0.000000}; return={SafeRatio(runSelected.MaximumReturnFlowStepChangeKilogramsPerSecond, explicitRun.MaximumReturnFlowStepChangeKilogramsPerSecond):0.000000}; pressure={SafeRatio(runSelected.MaximumFractionalSubcooledPressureChange, explicitRun.MaximumFractionalSubcooledPressureChange):0.000000};"),
            FormattableString.Invariant($"final-relative-gap-vs-explicit: mass={runSelected.MaximumFinalMassRelativeDifference:0.000000000}; energy={runSelected.MaximumFinalEnergyRelativeDifference:0.000000000}; pressure={runSelected.MaximumFinalPressureRelativeDifference:0.000000000};"),
            FormattableString.Invariant($"max-residuals: inventory-mass={runSelected.MaximumInventoryIntegrationMassResidualKilograms:0.000000000} kg; inventory-energy={runSelected.MaximumInventoryIntegrationEnergyResidualJoules:0.000000} J; hydraulic-mass-rate={runSelected.MaximumHydraulicMassRateClosureResidualKilogramsPerSecond:0.000000000} kg/s; hydraulic-energy-ownership={runSelected.MaximumHydraulicEnergyOwnershipResidualWatts:0.000000} W;"),
            FormattableString.Invariant($"wall-seconds-per-simulated-second explicit/hybrid={explicitRun.WallSecondsPerSimulatedSecond:0.000000}/{runSelected.WallSecondsPerSimulatedSecond:0.000000}; observational-wall-cost-ratio={SafeRatio(runSelected.WallSecondsPerSimulatedSecond, explicitRun.WallSecondsPerSimulatedSecond):0.000000}; deterministic-repeat=True;"),
            $"activation-criteria-met={activationRecommended}; production-hybrid-active=False; production-fixed-step=10.000 ms; physical-coefficient-retuning=False; hidden-flow-filtering=False;",
            activationRecommended
                ? "H.4 recommendation: bounded-work hybrid activation is numerically admissible for a separate production-integration candidate; this audit does not activate it."
                : "H.4 recommendation: do not activate hybrid production coupling yet; retain explicit 10 ms production and continue numerical optimization before integration.",
        };
        var summary = string.Join(Environment.NewLine, summaryLines) + Environment.NewLine;
        File.WriteAllText(Path.Combine(directory, "01-current-v2-hybrid-sweep.summary.txt"), summary, Utf8WithoutBom);
        Console.WriteLine(summary);
    }

    private static double SafeRatio(double numerator, double denominator)
        => Math.Abs(denominator) > 1e-15d ? numerator / denominator : 0d;

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "NuclearReactorSimulator.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root containing NuclearReactorSimulator.sln.");
    }

    private sealed record ReferenceInterval(
        int Index,
        PlantState Start,
        PlantState End,
        IReadOnlyDictionary<string, FluidNodeBalance> FrozenNonHydraulicBalances);

    private sealed record HybridConfiguration(
        string Id,
        double PressureTriggerFraction,
        double FlowTriggerKilogramsPerSecond,
        SemiImplicitHydraulicPrototypeOptions CorrectorOptions);

    private sealed record StepEvidence(
        int Index,
        double PumpFlowKilogramsPerSecond,
        double ChannelFlowKilogramsPerSecond,
        double ReturnFlowKilogramsPerSecond,
        double MaximumFractionalSubcooledPressureChange,
        bool UsedCorrection,
        int IterationCount,
        bool Converged,
        double PredictorPressureChange,
        double PredictorFlowChangeKilogramsPerSecond,
        double MaximumRelativePressureResidual,
        double MaximumAbsoluteFlowResidualKilogramsPerSecond);

    private sealed record FinalNodeState(string Id, double MassKilograms, double InternalEnergyJoules, double PressurePascals);

    private sealed record InventoryResidual(double MassKilograms, double EnergyJoules);

    private sealed record HybridCandidateRun(HybridConfiguration Configuration, TrajectoryRun Run);

    private sealed record TrajectoryRun(
        IReadOnlyList<StepEvidence> Steps,
        int CorrectionCount,
        int ConvergedCorrectionCount,
        double AverageIterationCount,
        int MaximumIterationCount,
        double MaximumRelativePressureResidual,
        double MaximumAbsoluteFlowResidualKilogramsPerSecond,
        double MaximumPredictorPressureChange,
        double MaximumPredictorFlowChangeKilogramsPerSecond,
        double MaximumPumpFlowStepChangeKilogramsPerSecond,
        double MaximumChannelFlowStepChangeKilogramsPerSecond,
        double MaximumReturnFlowStepChangeKilogramsPerSecond,
        double MaximumFractionalSubcooledPressureChange,
        double MaximumReferenceMassReplayErrorKilograms,
        double MaximumReferenceEnergyReplayErrorJoules,
        double MaximumInventoryIntegrationMassResidualKilograms,
        double MaximumInventoryIntegrationEnergyResidualJoules,
        double MaximumHydraulicMassRateClosureResidualKilogramsPerSecond,
        double MaximumHydraulicEnergyOwnershipResidualWatts,
        double MaximumFinalMassRelativeDifference,
        double MaximumFinalEnergyRelativeDifference,
        double MaximumFinalPressureRelativeDifference,
        double DeterministicWorkRatio,
        double WallSeconds,
        double WallSecondsPerSimulatedSecond,
        IReadOnlyList<FinalNodeState> FinalNodes);

    private sealed class RunAccumulator
    {
        public RunAccumulator(int capacity)
        {
            Steps = new List<StepEvidence>(capacity);
        }

        public List<StepEvidence> Steps { get; }
        public int CorrectionCount { get; set; }
        public int ConvergedCorrectionCount { get; set; }
        public double IterationSum { get; set; }
        public double CorrectorIterationSum { get; set; }
        public int MaximumIterationCount { get; set; }
        public double MaximumRelativePressureResidual { get; set; }
        public double MaximumAbsoluteFlowResidualKilogramsPerSecond { get; set; }
        public double MaximumPredictorPressureChange { get; set; }
        public double MaximumPredictorFlowChangeKilogramsPerSecond { get; set; }
        public double PreviousPumpFlowKilogramsPerSecond { get; set; } = double.NaN;
        public double PreviousChannelFlowKilogramsPerSecond { get; set; } = double.NaN;
        public double PreviousReturnFlowKilogramsPerSecond { get; set; } = double.NaN;
        public double MaximumPumpFlowStepChangeKilogramsPerSecond { get; set; }
        public double MaximumChannelFlowStepChangeKilogramsPerSecond { get; set; }
        public double MaximumReturnFlowStepChangeKilogramsPerSecond { get; set; }
        public double MaximumFractionalSubcooledPressureChange { get; set; }
        public double MaximumReferenceMassReplayErrorKilograms { get; set; }
        public double MaximumReferenceEnergyReplayErrorJoules { get; set; }
        public double MaximumInventoryIntegrationMassResidualKilograms { get; set; }
        public double MaximumInventoryIntegrationEnergyResidualJoules { get; set; }
        public double MaximumHydraulicMassRateClosureResidualKilogramsPerSecond { get; set; }
        public double MaximumHydraulicEnergyOwnershipResidualWatts { get; set; }

        public TrajectoryRun Build(
            IReadOnlyList<ReferenceInterval> reference,
            PlantState finalState,
            double wallSeconds)
        {
            var finalReference = reference[^1].End;
            var simulatedSeconds = reference.Count * Step.TotalSeconds;
            var deterministicWorkRatio = Steps.Count > 0
                ? (Steps.Count + CorrectorIterationSum) / Steps.Count
                : 1d;

            return new TrajectoryRun(
                Steps.ToArray(),
                CorrectionCount,
                ConvergedCorrectionCount,
                Steps.Count > 0 ? IterationSum / Steps.Count : 0d,
                MaximumIterationCount,
                MaximumRelativePressureResidual,
                MaximumAbsoluteFlowResidualKilogramsPerSecond,
                MaximumPredictorPressureChange,
                MaximumPredictorFlowChangeKilogramsPerSecond,
                MaximumPumpFlowStepChangeKilogramsPerSecond,
                MaximumChannelFlowStepChangeKilogramsPerSecond,
                MaximumReturnFlowStepChangeKilogramsPerSecond,
                MaximumFractionalSubcooledPressureChange,
                MaximumReferenceMassReplayErrorKilograms,
                MaximumReferenceEnergyReplayErrorJoules,
                MaximumInventoryIntegrationMassResidualKilograms,
                MaximumInventoryIntegrationEnergyResidualJoules,
                MaximumHydraulicMassRateClosureResidualKilogramsPerSecond,
                MaximumHydraulicEnergyOwnershipResidualWatts,
                MaximumRelativeFinalStateDifference(finalState, finalReference, static node => node.Mass.Kilograms),
                MaximumRelativeFinalStateDifference(finalState, finalReference, static node => node.InternalEnergy.Joules),
                MaximumRelativeFinalStateDifference(finalState, finalReference, static node => node.Pressure.Pascals),
                deterministicWorkRatio,
                wallSeconds,
                simulatedSeconds > 0d ? wallSeconds / simulatedSeconds : 0d,
                finalState.FluidNodes.Select(static node => new FinalNodeState(node.Id, node.Mass.Kilograms, node.InternalEnergy.Joules, node.Pressure.Pascals)).ToArray());
        }
    }
}
