using System.Globalization;
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
/// M10.9.4.1-H.9 shadow-only Jacobian-informed nonlinear hydraulic corrector audit. The exact H.5-H.8
/// committed explicit trajectory and frozen P060/F040 trigger set are reused. Production remains
/// explicit; H.9 candidates are observational only and never committed to the trajectory.
/// </summary>
public sealed class JacobianHydraulicCorrectorAuditTests
{
    private static readonly TimeSpan Step = TimeSpan.FromMilliseconds(10d);
    private static readonly Encoding Utf8WithoutBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    private const int IntervalCount = 500;
    private const double PressureTrigger = 0.060d;
    private const double FlowTriggerKilogramsPerSecond = 40d;
    private const double MaximumAlgorithmWorkRatio = 32d;

    private static readonly SemiImplicitHydraulicPrototypeOptions H4Primary = new(
        maximumIterations: 72,
        relaxationFactor: 0.15d,
        relativePressureTolerance: 1e-5d,
        absoluteFlowToleranceKilogramsPerSecond: 1e-2d);

    private static readonly SemiImplicitHydraulicPrototypeOptions H6SelectedRescue = new(
        maximumIterations: 96,
        relaxationFactor: 0.125d,
        relativePressureTolerance: 1e-5d,
        absoluteFlowToleranceKilogramsPerSecond: 1e-2d);

    [Fact(Explicit = true)]
    [Trait("Category", "JacobianHydraulicCorrectorAudit")]
    public void CurrentV2TriggeredIntervals_EvaluateJacobianInformedCorrectorWithoutProductionActivation()
    {
        var thermodynamics = new SimplifiedWaterSteamThermodynamicModel();
        var prototype = new SemiImplicitHydraulicPrototypeSolver(thermodynamics);
        var gate = new HybridSemiImplicitHydraulicGateSolver(thermodynamics);
        var h7Solver = new ResidualBacktrackingHydraulicCorrectorSolver(thermodynamics);
        var h8Solver = new AndersonHydraulicCorrectorSolver(thermodynamics);
        var h9Solver = new JacobianHydraulicCorrectorSolver(thermodynamics);
        var reference = BuildReferenceTrajectory(prototype);
        var baseline = EvaluatePrimaryGate(reference, gate);
        var h6Rescue = EvaluateH6Rescue(reference, baseline, prototype);
        var h7 = EvaluateH7(reference, baseline, h7Solver);

        Assert.True(reference.Count == IntervalCount, $"Expected {IntervalCount} committed reference intervals but found {reference.Count}.");
        Assert.True(baseline.Count == 7, $"Expected the validated frozen trigger count 7 but found {baseline.Count}.");
        Assert.True(baseline.Count(static item => item.PrimaryResult.Converged) == 5, "Expected validated H.4 primary convergence count 5/7.");
        Assert.True(h6Rescue.Count(static item => item.Converged) == 6, "Expected validated H.6 rescue convergence count 6/7.");
        Assert.True(h7.Count(static item => item.Converged) == 5, "Expected validated H.7 residual/backtracking convergence count 5/7.");
        Assert.True(h7.Count(static item => item.LineSearchExhausted) == 2, "Expected validated H.7 line-search exhaustion count 2/7.");

        var h8 = EvaluateH8(reference, baseline, h8Solver);
        Assert.True(h8.Events.Count(static item => item.Converged) == 5, "Expected validated H.8 safeguarded-Anderson convergence count 5/7.");
        Assert.True(h8.Events.Count(static item => item.LineSearchExhausted) == 2, "Expected validated H.8 line-search exhaustion count 2/7.");

        var run = EvaluateH9(reference, baseline, h9Solver);
        var repeat = EvaluateH9(reference, baseline, h9Solver);
        var deterministicRepeat = run.Events.SequenceEqual(repeat.Events);

        Assert.True(deterministicRepeat, "H.9 Jacobian-informed shadow evidence was not exactly deterministic.");
        Assert.All(run.Events, static item => Assert.True(item.AcceptedMeritStrictlyDecreases, item.ToString()));
        Assert.True(run.MaximumInventoryMassResidualKilograms <= 1e-6d, run.ToString());
        Assert.True(run.MaximumInventoryEnergyResidualJoules <= 1e-2d, run.ToString());
        Assert.True(run.MaximumHydraulicMassClosureKilogramsPerSecond <= 1e-8d, run.ToString());
        Assert.True(run.MaximumHydraulicEnergyOwnershipResidualWatts <= 1e-3d, run.ToString());
        Assert.True(double.IsFinite(run.DeterministicHydraulicEvaluationWorkRatio));
        Assert.True(run.DeterministicHydraulicEvaluationWorkRatio >= 1d);
        Assert.False(run.ProductionHybridActive);
        Assert.False(run.ShadowCandidatesCommitted);

        var qualified = QualifiesH9(run, deterministicRepeat);
        WriteAuditReports(
            baseline,
            h6Rescue,
            h7,
            h8,
            run with { DeterministicRepeat = deterministicRepeat, QualificationPasses = qualified });
    }

    private static IReadOnlyList<ReferenceInterval> BuildReferenceTrajectory(SemiImplicitHydraulicPrototypeSolver solver)
    {
        var engine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            DesktopSustainedGenerationInitialConditionFactory.CreateNumericalStiffnessEvidenceRuntimeEngine(Step));
        var intervals = new List<ReferenceInterval>(IntervalCount);
        for (var index = 0; index < IntervalCount; index++)
        {
            var start = ToPlantState(engine.LatestCanonicalSnapshot.Control.ProtectedControl.FullPlant.CandidatePlant);
            Assert.Equal(HydraulicNumericalCouplingMode.ExplicitCommittedState, start.Definition.HydraulicNumericalCoupling.Mode);
            var presentation = engine.Step(ControlRoomRunState.Running);
            Assert.False(presentation.AnyTripActive, $"Unexpected reference trip at H.9 interval {index + 1}.");
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

    private static IReadOnlyList<BaselineTriggerEvent> EvaluatePrimaryGate(
        IReadOnlyList<ReferenceInterval> reference,
        HybridSemiImplicitHydraulicGateSolver gate)
    {
        var options = new HybridSemiImplicitHydraulicGateOptions(PressureTrigger, FlowTriggerKilogramsPerSecond, H4Primary);
        var events = new List<BaselineTriggerEvent>();
        foreach (var interval in reference)
        {
            var result = gate.Step(interval.Start, Step, interval.FrozenNonHydraulicBalances, options);
            if (result.UsedSemiImplicitCorrection)
            {
                events.Add(new BaselineTriggerEvent(interval.Index, result));
            }
        }

        return events;
    }

    private static IReadOnlyList<LegacyRescueEvent> EvaluateH6Rescue(
        IReadOnlyList<ReferenceInterval> reference,
        IReadOnlyList<BaselineTriggerEvent> baseline,
        SemiImplicitHydraulicPrototypeSolver prototype)
    {
        var intervals = reference.ToDictionary(static item => item.Index);
        return baseline.Select(item =>
        {
            var interval = intervals[item.IntervalIndex];
            var result = prototype.StepSemiImplicit(interval.Start, Step, interval.FrozenNonHydraulicBalances, H6SelectedRescue);
            return new LegacyRescueEvent(
                interval.Index,
                result.Converged,
                result.IterationCount,
                result.MaximumRelativePressureResidual,
                result.MaximumAbsoluteFlowResidualKilogramsPerSecond);
        }).ToArray();
    }

    private static IReadOnlyList<H7Event> EvaluateH7(
        IReadOnlyList<ReferenceInterval> reference,
        IReadOnlyList<BaselineTriggerEvent> baseline,
        ResidualBacktrackingHydraulicCorrectorSolver solver)
    {
        var intervals = reference.ToDictionary(static item => item.Index);
        return baseline.Select(item =>
        {
            var interval = intervals[item.IntervalIndex];
            var result = solver.Step(interval.Start, Step, interval.FrozenNonHydraulicBalances, ResidualBacktrackingHydraulicCorrectorOptions.H7AuditDefault);
            return new H7Event(
                interval.Index,
                result.Converged,
                result.LineSearchExhausted,
                result.IterationCount,
                result.MaximumRelativePressureFixedPointResidual,
                result.MaximumAbsoluteFlowFixedPointResidualKilogramsPerSecond,
                result.NormalizedMeritResidual);
        }).ToArray();
    }

    private static H8Run EvaluateH8(
        IReadOnlyList<ReferenceInterval> reference,
        IReadOnlyList<BaselineTriggerEvent> baseline,
        AndersonHydraulicCorrectorSolver solver)
    {
        var options = AndersonHydraulicCorrectorOptions.H8AuditDefault;
        var intervals = reference.ToDictionary(static item => item.Index);
        var events = new List<H8Event>(baseline.Count);
        var maximumMassResidual = 0d;
        var maximumEnergyResidual = 0d;
        var maximumMassClosure = 0d;
        var maximumEnergyOwnership = 0d;
        var hydraulicEvaluationSum = 0d;

        foreach (var baselineEvent in baseline)
        {
            var interval = intervals[baselineEvent.IntervalIndex];
            var result = solver.Step(interval.Start, Step, interval.FrozenNonHydraulicBalances, options);
            var inventoryResidual = InventoryIntegrationResidual(
                interval.Start,
                result.CandidateState,
                result.AppliedHydraulicBalances,
                interval.FrozenNonHydraulicBalances,
                Step);
            var gap = RelativeCandidateGap(interval.End, result.CandidateState);
            var strictlyDecreasing = AcceptedMeritStrictlyDecreases(result.Iterations);

            maximumMassResidual = Math.Max(maximumMassResidual, inventoryResidual.MassKilograms);
            maximumEnergyResidual = Math.Max(maximumEnergyResidual, inventoryResidual.EnergyJoules);
            maximumMassClosure = Math.Max(maximumMassClosure, result.AppliedHydraulicMassRateClosureResidualKilogramsPerSecond);
            maximumEnergyOwnership = Math.Max(maximumEnergyOwnership, result.AppliedHydraulicEnergyOwnershipResidualWatts);
            hydraulicEvaluationSum += result.HydraulicEvaluationCount;

            events.Add(new H8Event(
                interval.Index,
                result.Converged,
                result.LineSearchExhausted,
                result.IterationCount,
                result.HydraulicEvaluationCount,
                result.BacktrackingTrialCount,
                result.AndersonDirectionAttempts,
                result.AndersonDirectionAcceptances,
                result.ResidualFallbackAttempts,
                result.ResidualFallbackAcceptances,
                result.LeastSquaresRejectedCount,
                result.MaximumAndersonCoefficientL1Norm,
                result.MinimumAcceptedRelaxationFactor,
                result.MaximumRelativePressureFixedPointResidual,
                result.MaximumAbsoluteFlowFixedPointResidualKilogramsPerSecond,
                result.NormalizedMeritResidual,
                strictlyDecreasing,
                gap.Mass,
                gap.Energy,
                gap.Pressure,
                TraceFingerprint(result.Iterations),
                StateFingerprint(result.CandidateState)));
        }

        return new H8Run(
            events.ToArray(),
            (IntervalCount + hydraulicEvaluationSum) / IntervalCount,
            maximumMassResidual,
            maximumEnergyResidual,
            maximumMassClosure,
            maximumEnergyOwnership,
            DeterministicRepeat: true,
            QualificationPasses: false,
            ProductionHybridActive: false,
            ShadowCandidatesCommitted: false);
    }

    private static H9Run EvaluateH9(
        IReadOnlyList<ReferenceInterval> reference,
        IReadOnlyList<BaselineTriggerEvent> baseline,
        JacobianHydraulicCorrectorSolver solver)
    {
        var options = JacobianHydraulicCorrectorOptions.H9AuditDefault;
        var intervals = reference.ToDictionary(static item => item.Index);
        var events = new List<H9Event>(baseline.Count);
        var maximumMassResidual = 0d;
        var maximumEnergyResidual = 0d;
        var maximumMassClosure = 0d;
        var maximumEnergyOwnership = 0d;
        var hydraulicEvaluationSum = 0d;

        foreach (var baselineEvent in baseline)
        {
            var interval = intervals[baselineEvent.IntervalIndex];
            var result = solver.Step(interval.Start, Step, interval.FrozenNonHydraulicBalances, options);
            var inventoryResidual = InventoryIntegrationResidual(
                interval.Start,
                result.CandidateState,
                result.AppliedHydraulicBalances,
                interval.FrozenNonHydraulicBalances,
                Step);
            var gap = RelativeCandidateGap(interval.End, result.CandidateState);
            var strictlyDecreasing = AcceptedMeritStrictlyDecreases(result.Iterations);

            maximumMassResidual = Math.Max(maximumMassResidual, inventoryResidual.MassKilograms);
            maximumEnergyResidual = Math.Max(maximumEnergyResidual, inventoryResidual.EnergyJoules);
            maximumMassClosure = Math.Max(maximumMassClosure, result.AppliedHydraulicMassRateClosureResidualKilogramsPerSecond);
            maximumEnergyOwnership = Math.Max(maximumEnergyOwnership, result.AppliedHydraulicEnergyOwnershipResidualWatts);
            hydraulicEvaluationSum += result.HydraulicEvaluationCount;

            events.Add(new H9Event(
                interval.Index,
                result.Converged,
                result.LineSearchExhausted,
                result.IterationCount,
                result.HydraulicEvaluationCount,
                result.BacktrackingTrialCount,
                result.JacobianBuildAttempts,
                result.JacobianDirectionAcceptances,
                result.JacobianRejectedCount,
                result.ResidualFallbackAttempts,
                result.ResidualFallbackAcceptances,
                result.ProbeEvaluationCount,
                result.MaximumJacobianDimension,
                result.MaximumPivotConditionEstimate,
                result.MaximumNormalizedNewtonStepInfinityNorm,
                result.MaximumCoordinateResidualInfinityNorm,
                result.MinimumAcceptedRelaxationFactor,
                result.MaximumRelativePressureFixedPointResidual,
                result.MaximumAbsoluteFlowFixedPointResidualKilogramsPerSecond,
                result.NormalizedMeritResidual,
                strictlyDecreasing,
                gap.Mass,
                gap.Energy,
                gap.Pressure,
                TraceFingerprint(result.Iterations),
                StateFingerprint(result.CandidateState)));
        }

        return new H9Run(
            events.ToArray(),
            (IntervalCount + hydraulicEvaluationSum) / IntervalCount,
            maximumMassResidual,
            maximumEnergyResidual,
            maximumMassClosure,
            maximumEnergyOwnership,
            DeterministicRepeat: true,
            QualificationPasses: false,
            ProductionHybridActive: false,
            ShadowCandidatesCommitted: false);
    }

    private static bool QualifiesH9(H9Run run, bool deterministicRepeat)
    {
        var options = JacobianHydraulicCorrectorOptions.H9AuditDefault;
        return deterministicRepeat
            && run.Events.All(static item => item.Converged)
            && run.Events.All(static item => !item.LineSearchExhausted)
            && run.Events.All(item => item.PressureFixedPointResidual <= options.RelativePressureTolerance)
            && run.Events.All(item => item.FlowFixedPointResidualKilogramsPerSecond <= options.AbsoluteFlowToleranceKilogramsPerSecond)
            && run.Events.All(static item => item.NormalizedMeritResidual <= 1d)
            && run.Events.All(static item => item.AcceptedMeritStrictlyDecreases)
            && run.Events.All(item => item.MaximumPivotConditionEstimate <= options.MaximumPivotConditionEstimate)
            && run.Events.All(item => item.MaximumNormalizedNewtonStepInfinityNorm <= options.MaximumNormalizedNewtonStep)
            && run.DeterministicHydraulicEvaluationWorkRatio <= MaximumAlgorithmWorkRatio
            && run.MaximumInventoryMassResidualKilograms <= 1e-6d
            && run.MaximumInventoryEnergyResidualJoules <= 1e-2d
            && run.MaximumHydraulicMassClosureKilogramsPerSecond <= 1e-8d
            && run.MaximumHydraulicEnergyOwnershipResidualWatts <= 1e-3d;
    }

    private static bool AcceptedMeritStrictlyDecreases(IReadOnlyList<AndersonHydraulicIteration> iterations)
    {
        for (var index = 1; index < iterations.Count; index++)
        {
            if (!(iterations[index].NormalizedMeritResidual < iterations[index - 1].NormalizedMeritResidual))
            {
                return false;
            }
        }

        return true;
    }

    private static string TraceFingerprint(IReadOnlyList<AndersonHydraulicIteration> iterations)
        => string.Join(
            "|",
            iterations.Select(item => FormattableString.Invariant(
                $"{item.IterationIndex}:{item.DirectionKind}:{item.HistorySampleCount}:{item.AcceptedRelaxationFactor:G17}:{item.BacktrackingTrials}:{item.AndersonCoefficientL1Norm:G17}:{item.MaximumRelativePressureFixedPointResidual:G17}:{item.MaximumAbsoluteFlowFixedPointResidualKilogramsPerSecond:G17}:{item.NormalizedMeritResidual:G17}")));

    private static bool AcceptedMeritStrictlyDecreases(IReadOnlyList<JacobianHydraulicIteration> iterations)
    {
        for (var index = 1; index < iterations.Count; index++)
        {
            if (!(iterations[index].NormalizedMeritResidual < iterations[index - 1].NormalizedMeritResidual))
            {
                return false;
            }
        }

        return true;
    }

    private static string TraceFingerprint(IReadOnlyList<JacobianHydraulicIteration> iterations)
        => string.Join(
            "|",
            iterations.Select(item => FormattableString.Invariant(
                $"{item.IterationIndex}:{item.DirectionKind}:{item.AcceptedRelaxationFactor:G17}:{item.BacktrackingTrials}:{item.JacobianDimension}:{item.ProbeEvaluations}:{item.PivotConditionEstimate:G17}:{item.NormalizedNewtonStepInfinityNorm:G17}:{item.CoordinateResidualInfinityNorm:G17}:{item.MaximumRelativePressureFixedPointResidual:G17}:{item.MaximumAbsoluteFlowFixedPointResidualKilogramsPerSecond:G17}:{item.NormalizedMeritResidual:G17}")));

    private static string StateFingerprint(PlantState state)
        => string.Join(
            "|",
            state.FluidNodes.OrderBy(static node => node.Id, StringComparer.Ordinal).Select(node => FormattableString.Invariant(
                $"{node.Id}:{node.Mass.Kilograms:G17}:{node.InternalEnergy.Joules:G17}:{node.Pressure.Pascals:G17}")));

    private static CandidateGap RelativeCandidateGap(PlantState reference, PlantState candidate)
        => new(
            MaximumRelativeDifference(reference, candidate, static node => node.Mass.Kilograms),
            MaximumRelativeDifference(reference, candidate, static node => node.InternalEnergy.Joules),
            MaximumRelativeDifference(reference, candidate, static node => node.Pressure.Pascals));

    private static double MaximumRelativeDifference(PlantState reference, PlantState candidate, Func<FluidNodeState, double> selector)
    {
        var candidateNodes = candidate.FluidNodes.ToDictionary(static node => node.Id, StringComparer.Ordinal);
        var maximum = 0d;
        foreach (var referenceNode in reference.FluidNodes)
        {
            var referenceValue = selector(referenceNode);
            var candidateValue = selector(candidateNodes[referenceNode.Id]);
            var scale = Math.Max(Math.Max(Math.Abs(referenceValue), Math.Abs(candidateValue)), 1e-12d);
            maximum = Math.Max(maximum, Math.Abs(candidateValue - referenceValue) / scale);
        }

        return maximum;
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

    private static IReadOnlyDictionary<string, FluidNodeBalance> DeriveInventoryBalances(PlantState start, PlantState end, TimeSpan deltaTime)
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

    private static PlantState ToPlantState(PlantSnapshot snapshot)
        => new(snapshot.Definition, snapshot.FluidNodes, snapshot.Valves, snapshot.Pumps, snapshot.ThermalBodies, snapshot.HeatSources);

    private static void WriteAuditReports(
        IReadOnlyList<BaselineTriggerEvent> baseline,
        IReadOnlyList<LegacyRescueEvent> h6Rescue,
        IReadOnlyList<H7Event> h7,
        H8Run h8,
        H9Run run)
    {
        var root = FindRepositoryRoot();
        var directory = Path.Combine(root, "artifacts", "h9-jacobian-informed-corrector");
        Directory.CreateDirectory(directory);
        var h6ByInterval = h6Rescue.ToDictionary(static item => item.IntervalIndex);
        var h7ByInterval = h7.ToDictionary(static item => item.IntervalIndex);
        var h8ByInterval = h8.Events.ToDictionary(static item => item.IntervalIndex);

        var eventCsv = new List<string>
        {
            "interval,h4_converged,h6_converged,h7_converged,h8_converged,h9_converged,h9_line_search_exhausted,h9_iterations,h9_hydraulic_evaluations,h9_backtracking_trials,h9_jacobian_builds,h9_jacobian_acceptances,h9_jacobian_rejected,h9_fallback_attempts,h9_fallback_acceptances,h9_probe_evaluations,h9_max_jacobian_dimension,h9_max_pivot_condition_estimate,h9_max_normalized_newton_step,h9_max_coordinate_residual,h9_min_accepted_relaxation,h9_pressure_fixed_point_residual,h9_flow_fixed_point_residual_kg_s,h9_normalized_merit,mass_gap_vs_explicit,energy_gap_vs_explicit,pressure_gap_vs_explicit",
        };
        foreach (var baselineEvent in baseline)
        {
            var h6Event = h6ByInterval[baselineEvent.IntervalIndex];
            var h7Event = h7ByInterval[baselineEvent.IntervalIndex];
            var h8Event = h8ByInterval[baselineEvent.IntervalIndex];
            var h9Event = run.Events.Single(item => item.IntervalIndex == baselineEvent.IntervalIndex);
            eventCsv.Add(FormattableString.Invariant(
                $"{baselineEvent.IntervalIndex},{baselineEvent.PrimaryResult.Converged},{h6Event.Converged},{h7Event.Converged},{h8Event.Converged},{h9Event.Converged},{h9Event.LineSearchExhausted},{h9Event.IterationCount},{h9Event.HydraulicEvaluationCount},{h9Event.BacktrackingTrialCount},{h9Event.JacobianBuildAttempts},{h9Event.JacobianDirectionAcceptances},{h9Event.JacobianRejectedCount},{h9Event.ResidualFallbackAttempts},{h9Event.ResidualFallbackAcceptances},{h9Event.ProbeEvaluationCount},{h9Event.MaximumJacobianDimension},{h9Event.MaximumPivotConditionEstimate:0.000000000},{h9Event.MaximumNormalizedNewtonStepInfinityNorm:0.000000000},{h9Event.MaximumCoordinateResidualInfinityNorm:0.000000000},{h9Event.MinimumAcceptedRelaxationFactor:0.000000000},{h9Event.PressureFixedPointResidual:0.000000000},{h9Event.FlowFixedPointResidualKilogramsPerSecond:0.000000000},{h9Event.NormalizedMeritResidual:0.000000000},{h9Event.RelativeMassGap:0.000000000},{h9Event.RelativeEnergyGap:0.000000000},{h9Event.RelativePressureGap:0.000000000}"));
        }

        File.WriteAllLines(Path.Combine(directory, "02-current-v2-triggered-event-jacobian-comparison.csv"), eventCsv, Utf8WithoutBom);

        var traceCsv = new List<string>
        {
            "interval,iteration,direction,accepted_relaxation,backtracking_trials,jacobian_dimension,probe_evaluations,pivot_condition_estimate,normalized_newton_step_inf,coordinate_residual_inf,pressure_fixed_point_residual,flow_fixed_point_residual_kg_s,normalized_merit",
        };
        foreach (var h9Event in run.Events)
        {
            foreach (var part in h9Event.TraceFingerprint.Split('|', StringSplitOptions.RemoveEmptyEntries))
            {
                var fields = part.Split(':');
                traceCsv.Add(string.Join(",", new[] { h9Event.IntervalIndex.ToString(CultureInfo.InvariantCulture) }.Concat(fields)));
            }
        }

        File.WriteAllLines(Path.Combine(directory, "03-current-v2-jacobian-trace.csv"), traceCsv, Utf8WithoutBom);

        var gapCsv = new List<string>
        {
            "interval,converged,mass_gap_vs_explicit,energy_gap_vs_explicit,pressure_gap_vs_explicit,state_fingerprint",
        };
        gapCsv.AddRange(run.Events.Select(item => FormattableString.Invariant(
            $"{item.IntervalIndex},{item.Converged},{item.RelativeMassGap:0.000000000},{item.RelativeEnergyGap:0.000000000},{item.RelativePressureGap:0.000000000},\"{item.StateFingerprint}\"")));
        File.WriteAllLines(Path.Combine(directory, "04-current-v2-jacobian-candidate-gaps.csv"), gapCsv, Utf8WithoutBom);

        var options = JacobianHydraulicCorrectorOptions.H9AuditDefault;
        var h4Converged = baseline.Count(static item => item.PrimaryResult.Converged);
        var h6Converged = h6Rescue.Count(static item => item.Converged);
        var h7Converged = h7.Count(static item => item.Converged);
        var h8Converged = h8.Events.Count(static item => item.Converged);
        var h9Converged = run.Events.Count(static item => item.Converged);
        var exhausted = run.Events.Count(static item => item.LineSearchExhausted);
        var maxPressureResidual = run.Events.Max(static item => item.PressureFixedPointResidual);
        var maxFlowResidual = run.Events.Max(static item => item.FlowFixedPointResidualKilogramsPerSecond);
        var maxMerit = run.Events.Max(static item => item.NormalizedMeritResidual);
        var maxCoordinateResidual = run.Events.Max(static item => item.MaximumCoordinateResidualInfinityNorm);
        var maxPivotCondition = run.Events.Max(static item => item.MaximumPivotConditionEstimate);
        var maxNewtonStep = run.Events.Max(static item => item.MaximumNormalizedNewtonStepInfinityNorm);
        var maxDimension = run.Events.Max(static item => item.MaximumJacobianDimension);
        var jacobianBuilds = run.Events.Sum(static item => item.JacobianBuildAttempts);
        var jacobianAcceptances = run.Events.Sum(static item => item.JacobianDirectionAcceptances);
        var jacobianRejected = run.Events.Sum(static item => item.JacobianRejectedCount);
        var fallbackAttempts = run.Events.Sum(static item => item.ResidualFallbackAttempts);
        var fallbackAcceptances = run.Events.Sum(static item => item.ResidualFallbackAcceptances);
        var probeEvaluations = run.Events.Sum(static item => item.ProbeEvaluationCount);
        var maxMassGap = run.Events.Max(static item => item.RelativeMassGap);
        var maxEnergyGap = run.Events.Max(static item => item.RelativeEnergyGap);
        var maxPressureGap = run.Events.Max(static item => item.RelativePressureGap);
        var minAcceptedRelaxation = run.Events
            .Where(static item => item.MinimumAcceptedRelaxationFactor > 0d)
            .Select(static item => item.MinimumAcceptedRelaxationFactor)
            .DefaultIfEmpty(0d)
            .Min();
        var recommendation = run.QualificationPasses
            ? "H.9 recommendation: the Jacobian-informed corrector resolves all frozen trigger events; keep production explicit and proceed to broader free-running/scenario shadow qualification before any activation candidate."
            : "H.9 recommendation: the Jacobian-informed corrector still does not resolve every frozen trigger event; keep production explicit and diagnose switching/non-smooth hydraulic-map structure before increasing solver complexity.";
        var summaryLines = new[]
        {
            "================================================================================",
            "M10.9.4.1-H.9 JACOBIAN-INFORMED NONLINEAR HYDRAULIC CORRECTOR SUMMARY",
            "================================================================================",
            "=== 01-current-v2-damped-newton-jacobian-corrector ===",
            "Shadow-only finite-difference Jacobian/Newton correction over the exact H.5-H.8 committed explicit intervals selected by frozen P060/F040; production remains explicit and no H.9 candidate is committed.",
            FormattableString.Invariant($"production-shadow-steps={IntervalCount}; frozen-trigger=P060-F040; triggered-events={baseline.Count}; H4-primary-converged={h4Converged}/{baseline.Count}; H6-rescue-converged={h6Converged}/{baseline.Count}; H7-residual-backtracking-converged={h7Converged}/{baseline.Count}; H8-Anderson-converged={h8Converged}/{baseline.Count};"),
            FormattableString.Invariant($"H9-algorithm=conservative-coordinate finite-difference Jacobian+damped-Newton; max-iterations={options.MaximumIterations}; finite-difference-step={options.FiniteDifferenceRelativeStep:G17}; diagonal-regularization={options.JacobianDiagonalRegularization:G17}; max-pivot-condition={options.MaximumPivotConditionEstimate:G17}; max-normalized-Newton-step={options.MaximumNormalizedNewtonStep:0.000000}; initial-relaxation={options.InitialRelaxationFactor:0.000000}; backtracking-factor={options.BacktrackingFactor:0.000000}; minimum-relaxation={options.MinimumRelaxationFactor:0.000000000}; pressure-tolerance={options.RelativePressureTolerance:0.000000000}; flow-tolerance={options.AbsoluteFlowToleranceKilogramsPerSecond:0.000000000} kg/s;"),
            FormattableString.Invariant($"H9-converged-events={h9Converged}/{run.Events.Count}; line-search-exhausted-events={exhausted}; Jacobian-builds/acceptances={jacobianBuilds}/{jacobianAcceptances}; Jacobian-rejected={jacobianRejected}; residual-fallback-attempts/acceptances={fallbackAttempts}/{fallbackAcceptances}; probe-evaluations={probeEvaluations}; max-Jacobian-dimension={maxDimension};"),
            FormattableString.Invariant($"max-pivot-condition-estimate={maxPivotCondition:0.000000000}; max-normalized-Newton-step={maxNewtonStep:0.000000000}; max-coordinate-residual-inf={maxCoordinateResidual:0.000000000}; max-fixed-point-residuals: pressure={maxPressureResidual:0.000000000}; flow={maxFlowResidual:0.000000000} kg/s; max-normalized-merit={maxMerit:0.000000000}; minimum-accepted-relaxation={minAcceptedRelaxation:0.000000000};"),
            FormattableString.Invariant($"deterministic-hydraulic-evaluation-work-ratio={run.DeterministicHydraulicEvaluationWorkRatio:0.000000}; work-ratio-limit={MaximumAlgorithmWorkRatio:0.000000}; accepted-merit-strictly-decreases={run.Events.All(static item => item.AcceptedMeritStrictlyDecreases)}; deterministic-repeat={run.DeterministicRepeat};"),
            FormattableString.Invariant($"max-gaps-vs-explicit mass/energy/pressure={maxMassGap:0.000000000}/{maxEnergyGap:0.000000000}/{maxPressureGap:0.000000000}; max-inventory-residuals mass/energy={run.MaximumInventoryMassResidualKilograms:0.000000000}/{run.MaximumInventoryEnergyResidualJoules:0.000000000}; max-hydraulic-closure/ownership={run.MaximumHydraulicMassClosureKilogramsPerSecond:0.000000000}/{run.MaximumHydraulicEnergyOwnershipResidualWatts:0.000000000};"),
            FormattableString.Invariant($"jacobian-informed-corrector-qualification-passes={run.QualificationPasses}; production-hybrid-active={run.ProductionHybridActive}; production-fixed-step=10.000 ms; shadow-candidates-committed={run.ShadowCandidatesCommitted}; historical-picard-replaced=False; H7-corrector-replaced=False; H8-corrector-replaced=False; plant-network-orchestrator-routing-changed=False; trigger-retuning=False; physical-coefficient-retuning=False; hidden-flow-filtering=False;"),
            recommendation,
        };
        File.WriteAllLines(Path.Combine(directory, "01-current-v2-jacobian-informed-corrector.summary.txt"), summaryLines, Utf8WithoutBom);
    }

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

    private sealed record ReferenceInterval(int Index, PlantState Start, PlantState End, IReadOnlyDictionary<string, FluidNodeBalance> FrozenNonHydraulicBalances);
    private sealed record BaselineTriggerEvent(int IntervalIndex, HybridSemiImplicitHydraulicGateStepResult PrimaryResult);
    private sealed record LegacyRescueEvent(int IntervalIndex, bool Converged, int IterationCount, double PressureResidual, double FlowResidualKilogramsPerSecond);
    private sealed record H7Event(int IntervalIndex, bool Converged, bool LineSearchExhausted, int IterationCount, double PressureResidual, double FlowResidualKilogramsPerSecond, double NormalizedMeritResidual);
    private sealed record H8Event(
        int IntervalIndex,
        bool Converged,
        bool LineSearchExhausted,
        int IterationCount,
        int HydraulicEvaluationCount,
        int BacktrackingTrialCount,
        int AndersonDirectionAttempts,
        int AndersonDirectionAcceptances,
        int ResidualFallbackAttempts,
        int ResidualFallbackAcceptances,
        int LeastSquaresRejectedCount,
        double MaximumAndersonCoefficientL1Norm,
        double MinimumAcceptedRelaxationFactor,
        double PressureFixedPointResidual,
        double FlowFixedPointResidualKilogramsPerSecond,
        double NormalizedMeritResidual,
        bool AcceptedMeritStrictlyDecreases,
        double RelativeMassGap,
        double RelativeEnergyGap,
        double RelativePressureGap,
        string TraceFingerprint,
        string StateFingerprint);
    private sealed record H8Run(
        IReadOnlyList<H8Event> Events,
        double DeterministicHydraulicEvaluationWorkRatio,
        double MaximumInventoryMassResidualKilograms,
        double MaximumInventoryEnergyResidualJoules,
        double MaximumHydraulicMassClosureKilogramsPerSecond,
        double MaximumHydraulicEnergyOwnershipResidualWatts,
        bool DeterministicRepeat,
        bool QualificationPasses,
        bool ProductionHybridActive,
        bool ShadowCandidatesCommitted);
    private sealed record H9Event(
        int IntervalIndex,
        bool Converged,
        bool LineSearchExhausted,
        int IterationCount,
        int HydraulicEvaluationCount,
        int BacktrackingTrialCount,
        int JacobianBuildAttempts,
        int JacobianDirectionAcceptances,
        int JacobianRejectedCount,
        int ResidualFallbackAttempts,
        int ResidualFallbackAcceptances,
        int ProbeEvaluationCount,
        int MaximumJacobianDimension,
        double MaximumPivotConditionEstimate,
        double MaximumNormalizedNewtonStepInfinityNorm,
        double MaximumCoordinateResidualInfinityNorm,
        double MinimumAcceptedRelaxationFactor,
        double PressureFixedPointResidual,
        double FlowFixedPointResidualKilogramsPerSecond,
        double NormalizedMeritResidual,
        bool AcceptedMeritStrictlyDecreases,
        double RelativeMassGap,
        double RelativeEnergyGap,
        double RelativePressureGap,
        string TraceFingerprint,
        string StateFingerprint);
    private sealed record H9Run(
        IReadOnlyList<H9Event> Events,
        double DeterministicHydraulicEvaluationWorkRatio,
        double MaximumInventoryMassResidualKilograms,
        double MaximumInventoryEnergyResidualJoules,
        double MaximumHydraulicMassClosureKilogramsPerSecond,
        double MaximumHydraulicEnergyOwnershipResidualWatts,
        bool DeterministicRepeat,
        bool QualificationPasses,
        bool ProductionHybridActive,
        bool ShadowCandidatesCommitted);
    private sealed record CandidateGap(double Mass, double Energy, double Pressure);
    private sealed record InventoryResidual(double MassKilograms, double EnergyJoules);
}
