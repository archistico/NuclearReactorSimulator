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
/// M10.9.4.1-H.3 audit-only comparison. The validated production runtime remains explicit at 10 ms.
/// Non-hydraulic forcing is reconstructed from the validated reference trajectory and frozen per interval,
/// so the only changed numerical owner in the prototype path is pressure/flow coupling.
/// </summary>
public sealed class SemiImplicitHydraulicPrototypeAuditTests
{
    private static readonly TimeSpan Step = TimeSpan.FromMilliseconds(10d);
    private static readonly Encoding Utf8WithoutBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    private const int IntervalCount = 50;

    [Fact(Explicit = true)]
    [Trait("Category", "SemiImplicitHydraulicPrototypeAudit")]
    public void CurrentV2FrozenForcingComparison_RecordsConvergenceChatterConservationDeterminismAndCost()
    {
        var solver = new SemiImplicitHydraulicPrototypeSolver(new SimplifiedWaterSteamThermodynamicModel());
        var reference = BuildReferenceTrajectory(solver);

        var explicitRun = RunTrajectory(reference, solver, useSemiImplicit: false);
        var semiImplicitRun = RunTrajectory(reference, solver, useSemiImplicit: true);
        var deterministicRepeat = RunTrajectory(reference, solver, useSemiImplicit: true);

        Assert.Equal(IntervalCount, reference.Count);
        Assert.Equal(IntervalCount, explicitRun.Steps.Count);
        Assert.Equal(IntervalCount, semiImplicitRun.Steps.Count);
        Assert.Equal(IntervalCount, semiImplicitRun.ConvergedStepCount);
        Assert.True(explicitRun.MaximumReferenceMassReplayErrorKilograms <= 1e-6d, explicitRun.ToString());
        Assert.True(explicitRun.MaximumReferenceEnergyReplayErrorJoules <= 1e-2d, explicitRun.ToString());
        Assert.True(semiImplicitRun.MaximumInventoryIntegrationMassResidualKilograms <= 1e-6d, semiImplicitRun.ToString());
        Assert.True(semiImplicitRun.MaximumInventoryIntegrationEnergyResidualJoules <= 1e-2d, semiImplicitRun.ToString());
        Assert.True(semiImplicitRun.MaximumHydraulicMassRateClosureResidualKilogramsPerSecond <= 1e-8d, semiImplicitRun.ToString());
        Assert.True(semiImplicitRun.MaximumHydraulicEnergyOwnershipResidualWatts <= 1e-3d, semiImplicitRun.ToString());
        Assert.True(semiImplicitRun.MaximumIterationCount <= SemiImplicitHydraulicPrototypeOptions.H3AuditDefault.MaximumIterations);
        Assert.True(double.IsFinite(semiImplicitRun.WallSecondsPerSimulatedSecond));
        Assert.True(semiImplicitRun.WallSecondsPerSimulatedSecond >= 0d);
        Assert.True(double.IsFinite(explicitRun.WallSecondsPerSimulatedSecond));
        Assert.True(explicitRun.WallSecondsPerSimulatedSecond >= 0d);
        Assert.True(ExactlyDeterministic(semiImplicitRun, deterministicRepeat));

        WriteAuditReports(explicitRun, semiImplicitRun);
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
            Assert.False(presentation.AnyTripActive, $"Unexpected reference trip at H.3 interval {index + 1}.");
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

    private static TrajectoryRun RunTrajectory(
        IReadOnlyList<ReferenceInterval> reference,
        SemiImplicitHydraulicPrototypeSolver solver,
        bool useSemiImplicit)
    {
        var current = reference[0].Start;
        var steps = new List<StepEvidence>(reference.Count);
        var maximumReferenceMassReplayError = 0d;
        var maximumReferenceEnergyReplayError = 0d;
        var maximumInventoryMassResidual = 0d;
        var maximumInventoryEnergyResidual = 0d;
        var maximumHydraulicMassClosure = 0d;
        var maximumHydraulicEnergyOwnershipResidual = 0d;
        var previousPumpFlow = double.NaN;
        var previousChannelFlow = double.NaN;
        var previousReturnFlow = double.NaN;
        var maximumPumpFlowStepChange = 0d;
        var maximumChannelFlowStepChange = 0d;
        var maximumReturnFlowStepChange = 0d;
        var maximumFractionalSubcooledPressureChange = 0d;
        var iterationSum = 0d;
        var maximumIterationCount = 0;
        var convergedSteps = 0;
        var maximumPressureResidual = 0d;
        var maximumFlowResidual = 0d;

        var stopwatch = Stopwatch.StartNew();
        foreach (var interval in reference)
        {
            current = RebindDiscreteAndThermalState(current, interval.Start);
            var start = current;
            var result = useSemiImplicit
                ? solver.StepSemiImplicit(start, Step, interval.FrozenNonHydraulicBalances)
                : solver.StepExplicit(start, Step, interval.FrozenNonHydraulicBalances);
            current = result.CandidateState;

            if (!useSemiImplicit)
            {
                maximumReferenceMassReplayError = Math.Max(
                    maximumReferenceMassReplayError,
                    MaximumInventoryDifference(current, interval.End, static node => node.Mass.Kilograms));
                maximumReferenceEnergyReplayError = Math.Max(
                    maximumReferenceEnergyReplayError,
                    MaximumInventoryDifference(current, interval.End, static node => node.InternalEnergy.Joules));
            }

            var inventoryResidual = InventoryIntegrationResidual(
                start,
                current,
                result.AppliedHydraulicBalances,
                interval.FrozenNonHydraulicBalances,
                Step);
            maximumInventoryMassResidual = Math.Max(maximumInventoryMassResidual, inventoryResidual.MassKilograms);
            maximumInventoryEnergyResidual = Math.Max(maximumInventoryEnergyResidual, inventoryResidual.EnergyJoules);
            maximumHydraulicMassClosure = Math.Max(
                maximumHydraulicMassClosure,
                result.HydraulicEvaluation.MassRateClosureResidualKilogramsPerSecond);
            maximumHydraulicEnergyOwnershipResidual = Math.Max(
                maximumHydraulicEnergyOwnershipResidual,
                result.HydraulicEvaluation.HydraulicEnergyOwnershipResidualWatts);

            var endEvaluation = solver.Evaluate(current);
            var pumpFlow = endEvaluation.GetPumpMassFlowRate("pump").KilogramsPerSecond;
            var channelFlow = endEvaluation.GetPipeMassFlowRate("channel").KilogramsPerSecond;
            var returnFlow = endEvaluation.GetPipeMassFlowRate("return").KilogramsPerSecond;
            if (double.IsFinite(previousPumpFlow))
            {
                maximumPumpFlowStepChange = Math.Max(maximumPumpFlowStepChange, Math.Abs(pumpFlow - previousPumpFlow));
                maximumChannelFlowStepChange = Math.Max(maximumChannelFlowStepChange, Math.Abs(channelFlow - previousChannelFlow));
                maximumReturnFlowStepChange = Math.Max(maximumReturnFlowStepChange, Math.Abs(returnFlow - previousReturnFlow));
            }

            previousPumpFlow = pumpFlow;
            previousChannelFlow = channelFlow;
            previousReturnFlow = returnFlow;
            var pressureChange = MaximumFractionalSubcooledPressureChange(start, current);
            maximumFractionalSubcooledPressureChange = Math.Max(maximumFractionalSubcooledPressureChange, pressureChange);
            iterationSum += result.IterationCount;
            maximumIterationCount = Math.Max(maximumIterationCount, result.IterationCount);
            if (result.Converged)
            {
                convergedSteps++;
            }

            maximumPressureResidual = Math.Max(maximumPressureResidual, result.MaximumRelativePressureResidual);
            maximumFlowResidual = Math.Max(maximumFlowResidual, result.MaximumAbsoluteFlowResidualKilogramsPerSecond);
            steps.Add(new StepEvidence(
                interval.Index,
                pumpFlow,
                channelFlow,
                returnFlow,
                pressureChange,
                result.IterationCount,
                result.Converged,
                result.MaximumRelativePressureResidual,
                result.MaximumAbsoluteFlowResidualKilogramsPerSecond));
        }
        stopwatch.Stop();

        var simulatedSeconds = reference.Count * Step.TotalSeconds;
        var finalReference = reference[^1].End;
        return new TrajectoryRun(
            useSemiImplicit,
            steps,
            convergedSteps,
            iterationSum / reference.Count,
            maximumIterationCount,
            maximumPressureResidual,
            maximumFlowResidual,
            maximumPumpFlowStepChange,
            maximumChannelFlowStepChange,
            maximumReturnFlowStepChange,
            maximumFractionalSubcooledPressureChange,
            maximumReferenceMassReplayError,
            maximumReferenceEnergyReplayError,
            maximumInventoryMassResidual,
            maximumInventoryEnergyResidual,
            maximumHydraulicMassClosure,
            maximumHydraulicEnergyOwnershipResidual,
            MaximumRelativeFinalStateDifference(current, finalReference, static node => node.Mass.Kilograms),
            MaximumRelativeFinalStateDifference(current, finalReference, static node => node.InternalEnergy.Joules),
            MaximumRelativeFinalStateDifference(current, finalReference, static node => node.Pressure.Pascals),
            stopwatch.Elapsed.TotalSeconds,
            simulatedSeconds > 0d ? stopwatch.Elapsed.TotalSeconds / simulatedSeconds : 0d,
            current.FluidNodes.Select(static node => new FinalNodeState(node.Id, node.Mass.Kilograms, node.InternalEnergy.Joules, node.Pressure.Pascals)).ToArray());
    }

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
            && left.ConvergedStepCount == right.ConvergedStepCount
            && left.AverageIterationCount == right.AverageIterationCount
            && left.MaximumIterationCount == right.MaximumIterationCount
            && left.MaximumRelativePressureResidual == right.MaximumRelativePressureResidual
            && left.MaximumAbsoluteFlowResidualKilogramsPerSecond == right.MaximumAbsoluteFlowResidualKilogramsPerSecond;

    private static void WriteAuditReports(TrajectoryRun explicitRun, TrajectoryRun prototypeRun)
    {
        var directory = Path.Combine(FindRepositoryRoot(), "artifacts", "h3-semi-implicit-hydraulic-prototype");
        Directory.CreateDirectory(directory);

        const string stem = "01-current-v2-semi-implicit-hydraulic-prototype";
        var csv = new StringBuilder();
        csv.AppendLine("step,explicit_pump_kg_s,prototype_pump_kg_s,explicit_channel_kg_s,prototype_channel_kg_s,explicit_return_kg_s,prototype_return_kg_s,explicit_max_subcooled_pressure_fraction,prototype_max_subcooled_pressure_fraction,prototype_iterations,prototype_converged,prototype_pressure_residual,prototype_flow_residual_kg_s");
        for (var index = 0; index < explicitRun.Steps.Count; index++)
        {
            var explicitStep = explicitRun.Steps[index];
            var prototypeStep = prototypeRun.Steps[index];
            csv.AppendLine(FormattableString.Invariant(
                $"{explicitStep.Index},{explicitStep.PumpFlowKilogramsPerSecond:R},{prototypeStep.PumpFlowKilogramsPerSecond:R},{explicitStep.ChannelFlowKilogramsPerSecond:R},{prototypeStep.ChannelFlowKilogramsPerSecond:R},{explicitStep.ReturnFlowKilogramsPerSecond:R},{prototypeStep.ReturnFlowKilogramsPerSecond:R},{explicitStep.MaximumFractionalSubcooledPressureChange:R},{prototypeStep.MaximumFractionalSubcooledPressureChange:R},{prototypeStep.IterationCount},{prototypeStep.Converged},{prototypeStep.MaximumRelativePressureResidual:R},{prototypeStep.MaximumAbsoluteFlowResidualKilogramsPerSecond:R}"));
        }
        File.WriteAllText(Path.Combine(directory, $"{stem}.csv"), csv.ToString(), Utf8WithoutBom);

        var pumpRatio = SafeRatio(prototypeRun.MaximumPumpFlowStepChangeKilogramsPerSecond, explicitRun.MaximumPumpFlowStepChangeKilogramsPerSecond);
        var channelRatio = SafeRatio(prototypeRun.MaximumChannelFlowStepChangeKilogramsPerSecond, explicitRun.MaximumChannelFlowStepChangeKilogramsPerSecond);
        var returnRatio = SafeRatio(prototypeRun.MaximumReturnFlowStepChangeKilogramsPerSecond, explicitRun.MaximumReturnFlowStepChangeKilogramsPerSecond);
        var pressureRatio = SafeRatio(prototypeRun.MaximumFractionalSubcooledPressureChange, explicitRun.MaximumFractionalSubcooledPressureChange);
        var costRatio = SafeRatio(prototypeRun.WallSecondsPerSimulatedSecond, explicitRun.WallSecondsPerSimulatedSecond);
        var materialImprovement = pumpRatio < 1d && channelRatio < 1d && returnRatio < 1d && pressureRatio < 1d;

        var summaryLines = new[]
        {
            "================================================================================",
            "M10.9.4.1-H.3 SEMI-IMPLICIT HYDRAULIC PROTOTYPE SUMMARY",
            "================================================================================",
            $"=== {stem} ===",
            "Audit-only deterministic pressure/flow prototype over frozen non-hydraulic forcing reconstructed from the validated current-v2 10 ms trajectory; production runtime remains explicit.",
            FormattableString.Invariant($"steps={prototypeRun.Steps.Count}; simulated-window={prototypeRun.Steps.Count * Step.TotalSeconds:0.000}s; converged-steps={prototypeRun.ConvergedStepCount}/{prototypeRun.Steps.Count}; average-iterations={prototypeRun.AverageIterationCount:0.000}; maximum-iterations={prototypeRun.MaximumIterationCount};"),
            FormattableString.Invariant($"max-flow-step-change explicit/prototype: pump={explicitRun.MaximumPumpFlowStepChangeKilogramsPerSecond:0.000000000}/{prototypeRun.MaximumPumpFlowStepChangeKilogramsPerSecond:0.000000000} kg/s; channel={explicitRun.MaximumChannelFlowStepChangeKilogramsPerSecond:0.000000000}/{prototypeRun.MaximumChannelFlowStepChangeKilogramsPerSecond:0.000000000} kg/s; return={explicitRun.MaximumReturnFlowStepChangeKilogramsPerSecond:0.000000000}/{prototypeRun.MaximumReturnFlowStepChangeKilogramsPerSecond:0.000000000} kg/s;"),
            FormattableString.Invariant($"chatter-ratios prototype/explicit: pump={pumpRatio:0.000000}; channel={channelRatio:0.000000}; return={returnRatio:0.000000}; pressure={pressureRatio:0.000000}; material-improvement={materialImprovement};"),
            FormattableString.Invariant($"max-fractional-subcooled-pressure-change explicit/prototype={explicitRun.MaximumFractionalSubcooledPressureChange:0.000000000}/{prototypeRun.MaximumFractionalSubcooledPressureChange:0.000000000}; max-iteration-pressure-residual={prototypeRun.MaximumRelativePressureResidual:0.000000000}; max-iteration-flow-residual={prototypeRun.MaximumAbsoluteFlowResidualKilogramsPerSecond:0.000000000} kg/s;"),
            FormattableString.Invariant($"max-inventory-integration-residual: mass={prototypeRun.MaximumInventoryIntegrationMassResidualKilograms:0.000000000} kg; energy={prototypeRun.MaximumInventoryIntegrationEnergyResidualJoules:0.000000} J; hydraulic-mass-rate-closure={prototypeRun.MaximumHydraulicMassRateClosureResidualKilogramsPerSecond:0.000000000} kg/s; hydraulic-energy-ownership-residual={prototypeRun.MaximumHydraulicEnergyOwnershipResidualWatts:0.000000} W;"),
            FormattableString.Invariant($"final-relative-gap-vs-explicit: mass={prototypeRun.MaximumFinalMassRelativeDifference:0.000000000}; energy={prototypeRun.MaximumFinalEnergyRelativeDifference:0.000000000}; pressure={prototypeRun.MaximumFinalPressureRelativeDifference:0.000000000};"),
            FormattableString.Invariant($"wall-seconds-per-simulated-second explicit/prototype={explicitRun.WallSecondsPerSimulatedSecond:0.000000}/{prototypeRun.WallSecondsPerSimulatedSecond:0.000000}; prototype-cost-ratio={costRatio:0.000000}; deterministic-repeat=True;"),
            "production-semi-implicit-active=False; physical-coefficient-retuning=False; hidden-flow-filtering=False; H.4-activation-deferred=True",
        };
        var summary = string.Join(Environment.NewLine, summaryLines) + Environment.NewLine;
        File.WriteAllText(Path.Combine(directory, $"{stem}.summary.txt"), summary, Utf8WithoutBom);
        Console.WriteLine(summary);

        var finalCsv = new StringBuilder();
        finalCsv.AppendLine("node_id,prototype_mass_kg,prototype_internal_energy_j,prototype_pressure_pa");
        foreach (var node in prototypeRun.FinalNodes)
        {
            finalCsv.AppendLine(FormattableString.Invariant($"{node.Id},{node.MassKilograms:R},{node.InternalEnergyJoules:R},{node.PressurePascals:R}"));
        }
        File.WriteAllText(Path.Combine(directory, "02-current-v2-semi-implicit-final-state.csv"), finalCsv.ToString(), Utf8WithoutBom);
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

    private sealed record StepEvidence(
        int Index,
        double PumpFlowKilogramsPerSecond,
        double ChannelFlowKilogramsPerSecond,
        double ReturnFlowKilogramsPerSecond,
        double MaximumFractionalSubcooledPressureChange,
        int IterationCount,
        bool Converged,
        double MaximumRelativePressureResidual,
        double MaximumAbsoluteFlowResidualKilogramsPerSecond);

    private sealed record FinalNodeState(string Id, double MassKilograms, double InternalEnergyJoules, double PressurePascals);

    private sealed record InventoryResidual(double MassKilograms, double EnergyJoules);

    private sealed record TrajectoryRun(
        bool SemiImplicit,
        IReadOnlyList<StepEvidence> Steps,
        int ConvergedStepCount,
        double AverageIterationCount,
        int MaximumIterationCount,
        double MaximumRelativePressureResidual,
        double MaximumAbsoluteFlowResidualKilogramsPerSecond,
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
        double WallSeconds,
        double WallSecondsPerSimulatedSecond,
        IReadOnlyList<FinalNodeState> FinalNodes);
}
