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
/// M10.9.4.1-H.7 shadow-only corrector algorithm revision. The exact H.5/H.6 committed explicit
/// trajectory and frozen P060/F040 trigger set are reused. The historical Picard solver remains intact;
/// this audit evaluates a separate residual-based fixed-point solver with deterministic backtracking.
/// No revised shadow candidate is committed to the production trajectory.
/// </summary>
public sealed class ResidualBacktrackingCorrectorAuditTests
{
    private static readonly TimeSpan Step = TimeSpan.FromMilliseconds(10d);
    private static readonly Encoding Utf8WithoutBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    private const int IntervalCount = 500;
    private const double PressureTrigger = 0.060d;
    private const double FlowTriggerKilogramsPerSecond = 40d;
    private const double MaximumAlgorithmWorkRatio = 8d;

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
    [Trait("Category", "ResidualBacktrackingCorrectorAudit")]
    public void CurrentV2TriggeredIntervals_EvaluateResidualBacktrackingRevisionWithoutProductionActivation()
    {
        var thermodynamics = new SimplifiedWaterSteamThermodynamicModel();
        var prototype = new SemiImplicitHydraulicPrototypeSolver(thermodynamics);
        var gate = new HybridSemiImplicitHydraulicGateSolver(thermodynamics);
        var revised = new ResidualBacktrackingHydraulicCorrectorSolver(thermodynamics);
        var reference = BuildReferenceTrajectory(prototype);
        var baseline = EvaluatePrimaryGate(reference, gate);
        var h6Rescue = EvaluateH6Rescue(reference, baseline, prototype);

        Assert.True(reference.Count == IntervalCount, $"Expected {IntervalCount} committed reference intervals but found {reference.Count}.");
        Assert.True(baseline.Count == 7, $"Expected the validated H.5/H.6 trigger count 7 but found {baseline.Count}.");
        Assert.True(baseline.Count(static item => item.PrimaryResult.Converged) == 5, "Expected the validated H.4 primary convergence count 5/7.");
        Assert.True(baseline.Count(static item => !item.PrimaryResult.Converged) == 2, "Expected the validated H.4 primary non-convergence count 2/7.");
        Assert.True(h6Rescue.Count(static item => item.Converged) == 6, "Expected the validated H.6 selected-rescue convergence count 6/7.");
        Assert.Single(h6Rescue, static item => !item.Converged);

        var run = EvaluateRevisedCorrector(reference, baseline, revised);
        var repeat = EvaluateRevisedCorrector(reference, baseline, revised);
        var deterministicRepeat = run.Events.SequenceEqual(repeat.Events);

        Assert.True(deterministicRepeat, "H.7 residual/backtracking shadow evidence was not exactly deterministic.");
        Assert.All(run.Events, static item => Assert.True(item.AcceptedMeritStrictlyDecreases, item.ToString()));
        Assert.True(run.MaximumInventoryMassResidualKilograms <= 1e-6d, run.ToString());
        Assert.True(run.MaximumInventoryEnergyResidualJoules <= 1e-2d, run.ToString());
        Assert.True(run.MaximumHydraulicMassClosureKilogramsPerSecond <= 1e-8d, run.ToString());
        Assert.True(run.MaximumHydraulicEnergyOwnershipResidualWatts <= 1e-3d, run.ToString());
        Assert.True(double.IsFinite(run.DeterministicHydraulicEvaluationWorkRatio));
        Assert.True(run.DeterministicHydraulicEvaluationWorkRatio >= 1d);
        Assert.False(run.ProductionHybridActive);
        Assert.False(run.ShadowCandidatesCommitted);

        var qualified = QualifiesAlgorithmRevision(run, deterministicRepeat);
        WriteAuditReports(
            baseline,
            h6Rescue,
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
            Assert.Equal(
                HydraulicNumericalCouplingMode.ExplicitCommittedState,
                start.Definition.HydraulicNumericalCoupling.Mode);
            var presentation = engine.Step(ControlRoomRunState.Running);
            Assert.False(presentation.AnyTripActive, $"Unexpected reference trip at H.7 interval {index + 1}.");
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
        var options = new HybridSemiImplicitHydraulicGateOptions(
            PressureTrigger,
            FlowTriggerKilogramsPerSecond,
            H4Primary);
        var events = new List<BaselineTriggerEvent>();

        foreach (var interval in reference)
        {
            var result = gate.Step(interval.Start, Step, interval.FrozenNonHydraulicBalances, options);
            if (!result.UsedSemiImplicitCorrection)
            {
                continue;
            }

            events.Add(new BaselineTriggerEvent(interval.Index, result));
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
            var result = prototype.StepSemiImplicit(
                interval.Start,
                Step,
                interval.FrozenNonHydraulicBalances,
                H6SelectedRescue);
            return new LegacyRescueEvent(
                interval.Index,
                result.Converged,
                result.IterationCount,
                result.MaximumRelativePressureResidual,
                result.MaximumAbsoluteFlowResidualKilogramsPerSecond);
        }).ToArray();
    }

    private static RevisedRun EvaluateRevisedCorrector(
        IReadOnlyList<ReferenceInterval> reference,
        IReadOnlyList<BaselineTriggerEvent> baseline,
        ResidualBacktrackingHydraulicCorrectorSolver solver)
    {
        var options = ResidualBacktrackingHydraulicCorrectorOptions.H7AuditDefault;
        var intervals = reference.ToDictionary(static item => item.Index);
        var events = new List<RevisedEvent>(baseline.Count);
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

            events.Add(new RevisedEvent(
                interval.Index,
                result.Converged,
                result.LineSearchExhausted,
                result.IterationCount,
                result.HydraulicEvaluationCount,
                result.BacktrackingTrialCount,
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

        var workRatio = (IntervalCount + hydraulicEvaluationSum) / IntervalCount;
        return new RevisedRun(
            events.ToArray(),
            workRatio,
            maximumMassResidual,
            maximumEnergyResidual,
            maximumMassClosure,
            maximumEnergyOwnership,
            DeterministicRepeat: true,
            QualificationPasses: false,
            ProductionHybridActive: false,
            ShadowCandidatesCommitted: false);
    }

    private static bool QualifiesAlgorithmRevision(RevisedRun run, bool deterministicRepeat)
    {
        var options = ResidualBacktrackingHydraulicCorrectorOptions.H7AuditDefault;
        return deterministicRepeat
            && run.Events.All(static item => item.Converged)
            && run.Events.All(static item => !item.LineSearchExhausted)
            && run.Events.All(item => item.PressureFixedPointResidual <= options.RelativePressureTolerance)
            && run.Events.All(item => item.FlowFixedPointResidualKilogramsPerSecond <= options.AbsoluteFlowToleranceKilogramsPerSecond)
            && run.Events.All(static item => item.NormalizedMeritResidual <= 1d)
            && run.Events.All(static item => item.AcceptedMeritStrictlyDecreases)
            && run.DeterministicHydraulicEvaluationWorkRatio <= MaximumAlgorithmWorkRatio
            && run.MaximumInventoryMassResidualKilograms <= 1e-6d
            && run.MaximumInventoryEnergyResidualJoules <= 1e-2d
            && run.MaximumHydraulicMassClosureKilogramsPerSecond <= 1e-8d
            && run.MaximumHydraulicEnergyOwnershipResidualWatts <= 1e-3d;
    }

    private static bool AcceptedMeritStrictlyDecreases(IReadOnlyList<ResidualBacktrackingHydraulicIteration> iterations)
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

    private static string TraceFingerprint(IReadOnlyList<ResidualBacktrackingHydraulicIteration> iterations)
        => string.Join(
            "|",
            iterations.Select(item => FormattableString.Invariant(
                $"{item.IterationIndex}:{item.AcceptedRelaxationFactor:G17}:{item.BacktrackingTrials}:{item.MaximumRelativePressureFixedPointResidual:G17}:{item.MaximumAbsoluteFlowFixedPointResidualKilogramsPerSecond:G17}:{item.NormalizedMeritResidual:G17}")));

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

    private static double MaximumRelativeDifference(
        PlantState reference,
        PlantState candidate,
        Func<FluidNodeState, double> selector)
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

    private static PlantState ToPlantState(PlantSnapshot snapshot)
        => new(
            snapshot.Definition,
            snapshot.FluidNodes,
            snapshot.Valves,
            snapshot.Pumps,
            snapshot.ThermalBodies,
            snapshot.HeatSources);

    private static void WriteAuditReports(
        IReadOnlyList<BaselineTriggerEvent> baseline,
        IReadOnlyList<LegacyRescueEvent> h6Rescue,
        RevisedRun run)
    {
        var root = FindRepositoryRoot();
        var directory = Path.Combine(root, "artifacts", "h7-corrector-algorithm-revision");
        Directory.CreateDirectory(directory);
        var h6ByInterval = h6Rescue.ToDictionary(static item => item.IntervalIndex);

        var eventCsv = new List<string>
        {
            "interval,h4_primary_converged,h4_primary_iterations,h6_rescue_converged,h6_rescue_iterations,h6_pressure_residual,h6_flow_residual_kg_s,h7_converged,h7_line_search_exhausted,h7_iterations,h7_hydraulic_evaluations,h7_backtracking_trials,h7_min_accepted_relaxation,h7_pressure_fixed_point_residual,h7_flow_fixed_point_residual_kg_s,h7_normalized_merit,h7_accepted_merit_strictly_decreases,mass_gap_vs_explicit,energy_gap_vs_explicit,pressure_gap_vs_explicit",
        };
        foreach (var baselineEvent in baseline)
        {
            var legacy = h6ByInterval[baselineEvent.IntervalIndex];
            var revised = run.Events.Single(item => item.IntervalIndex == baselineEvent.IntervalIndex);
            eventCsv.Add(FormattableString.Invariant(
                $"{baselineEvent.IntervalIndex},{baselineEvent.PrimaryResult.Converged},{baselineEvent.PrimaryResult.IterationCount},{legacy.Converged},{legacy.IterationCount},{legacy.PressureResidual:0.000000000},{legacy.FlowResidualKilogramsPerSecond:0.000000000},{revised.Converged},{revised.LineSearchExhausted},{revised.IterationCount},{revised.HydraulicEvaluationCount},{revised.BacktrackingTrialCount},{revised.MinimumAcceptedRelaxationFactor:0.000000000},{revised.PressureFixedPointResidual:0.000000000},{revised.FlowFixedPointResidualKilogramsPerSecond:0.000000000},{revised.NormalizedMeritResidual:0.000000000},{revised.AcceptedMeritStrictlyDecreases},{revised.RelativeMassGap:0.000000000},{revised.RelativeEnergyGap:0.000000000},{revised.RelativePressureGap:0.000000000}"));
        }

        File.WriteAllLines(
            Path.Combine(directory, "02-current-v2-triggered-event-algorithm-comparison.csv"),
            eventCsv,
            Utf8WithoutBom);

        var traceCsv = new List<string>
        {
            "interval,iteration,accepted_relaxation,backtracking_trials,pressure_fixed_point_residual,flow_fixed_point_residual_kg_s,normalized_merit",
        };
        foreach (var revisedEvent in run.Events)
        {
            var parts = revisedEvent.TraceFingerprint.Split('|', StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                var fields = part.Split(':');
                traceCsv.Add(string.Join(
                    ',',
                    revisedEvent.IntervalIndex.ToString(CultureInfo.InvariantCulture),
                    fields[0],
                    fields[1],
                    fields[2],
                    fields[3],
                    fields[4],
                    fields[5]));
            }
        }

        File.WriteAllLines(
            Path.Combine(directory, "03-current-v2-residual-backtracking-trace.csv"),
            traceCsv,
            Utf8WithoutBom);

        var gapCsv = new List<string>
        {
            "interval,converged,mass_gap_vs_explicit,energy_gap_vs_explicit,pressure_gap_vs_explicit,state_fingerprint",
        };
        gapCsv.AddRange(run.Events.Select(item => FormattableString.Invariant(
            $"{item.IntervalIndex},{item.Converged},{item.RelativeMassGap:0.000000000},{item.RelativeEnergyGap:0.000000000},{item.RelativePressureGap:0.000000000},\"{item.StateFingerprint}\"")));
        File.WriteAllLines(
            Path.Combine(directory, "04-current-v2-revised-candidate-gaps.csv"),
            gapCsv,
            Utf8WithoutBom);

        var options = ResidualBacktrackingHydraulicCorrectorOptions.H7AuditDefault;
        var baselineConverged = baseline.Count(static item => item.PrimaryResult.Converged);
        var h6Converged = h6Rescue.Count(static item => item.Converged);
        var h6MaximumPressureResidual = h6Rescue.Max(static item => item.PressureResidual);
        var h6MaximumFlowResidual = h6Rescue.Max(static item => item.FlowResidualKilogramsPerSecond);
        var revisedConverged = run.Events.Count(static item => item.Converged);
        var exhausted = run.Events.Count(static item => item.LineSearchExhausted);
        var maxPressureResidual = run.Events.Max(static item => item.PressureFixedPointResidual);
        var maxFlowResidual = run.Events.Max(static item => item.FlowFixedPointResidualKilogramsPerSecond);
        var maxMerit = run.Events.Max(static item => item.NormalizedMeritResidual);
        var maxMassGap = run.Events.Max(static item => item.RelativeMassGap);
        var maxEnergyGap = run.Events.Max(static item => item.RelativeEnergyGap);
        var maxPressureGap = run.Events.Max(static item => item.RelativePressureGap);
        var minAcceptedRelaxation = run.Events
            .Where(static item => item.MinimumAcceptedRelaxationFactor > 0d)
            .Select(static item => item.MinimumAcceptedRelaxationFactor)
            .DefaultIfEmpty(0d)
            .Min();
        var recommendation = run.QualificationPasses
            ? "H.7 recommendation: the residual/backtracking algorithm resolves the frozen H.5/H.6 trigger set; keep production explicit and proceed to broader free-running/scenario shadow qualification before any activation candidate."
            : "H.7 recommendation: the residual/backtracking algorithm does not yet qualify over every frozen trigger event; keep production explicit and continue nonlinear-corrector development before broader shadow qualification.";
        var summaryLines = new[]
        {
            "================================================================================",
            "M10.9.4.1-H.7 CORRECTOR ALGORITHM REVISION SUMMARY",
            "================================================================================",
            "=== 01-current-v2-residual-backtracking-corrector ===",
            "Shadow-only algorithm revision over the exact H.5/H.6 committed explicit intervals selected by frozen P060/F040; the historical Picard corrector remains unchanged and no H.7 candidate is committed.",
            FormattableString.Invariant($"production-shadow-steps={IntervalCount}; frozen-trigger=P060-F040; triggered-events={baseline.Count}; H4-primary-converged={baselineConverged}/{baseline.Count}; H6-selected-rescue=R0125-I096; H6-selected-rescue-converged={h6Converged}/{baseline.Count}; H6-selected-rescue-max-residuals: pressure={h6MaximumPressureResidual:0.000000000}; flow={h6MaximumFlowResidual:0.000000000} kg/s;"),
            FormattableString.Invariant($"H7-algorithm=residual-fixed-point+deterministic-backtracking; max-iterations={options.MaximumIterations}; initial-relaxation={options.InitialRelaxationFactor:0.000000}; backtracking-factor={options.BacktrackingFactor:0.000000}; minimum-relaxation={options.MinimumRelaxationFactor:0.000000000}; pressure-tolerance={options.RelativePressureTolerance:0.000000000}; flow-tolerance={options.AbsoluteFlowToleranceKilogramsPerSecond:0.000000000} kg/s;"),
            FormattableString.Invariant($"H7-converged-events={revisedConverged}/{run.Events.Count}; line-search-exhausted-events={exhausted}; max-fixed-point-residuals: pressure={maxPressureResidual:0.000000000}; flow={maxFlowResidual:0.000000000} kg/s; max-normalized-merit={maxMerit:0.000000000}; minimum-accepted-relaxation={minAcceptedRelaxation:0.000000000};"),
            FormattableString.Invariant($"deterministic-hydraulic-evaluation-work-ratio={run.DeterministicHydraulicEvaluationWorkRatio:0.000000}; work-ratio-limit={MaximumAlgorithmWorkRatio:0.000000}; accepted-merit-strictly-decreases={run.Events.All(static item => item.AcceptedMeritStrictlyDecreases)}; deterministic-repeat={run.DeterministicRepeat};"),
            FormattableString.Invariant($"max-gaps-vs-explicit mass/energy/pressure={maxMassGap:0.000000000}/{maxEnergyGap:0.000000000}/{maxPressureGap:0.000000000}; max-inventory-residuals mass/energy={run.MaximumInventoryMassResidualKilograms:0.000000000}/{run.MaximumInventoryEnergyResidualJoules:0.000000000}; max-hydraulic-closure/ownership={run.MaximumHydraulicMassClosureKilogramsPerSecond:0.000000000}/{run.MaximumHydraulicEnergyOwnershipResidualWatts:0.000000000};"),
            FormattableString.Invariant($"corrector-algorithm-revision-qualification-passes={run.QualificationPasses}; production-hybrid-active={run.ProductionHybridActive}; production-fixed-step=10.000 ms; shadow-candidates-committed={run.ShadowCandidatesCommitted}; historical-picard-replaced=False; plant-network-orchestrator-routing-changed=False; trigger-retuning=False; physical-coefficient-retuning=False; hidden-flow-filtering=False;"),
            recommendation,
        };

        File.WriteAllLines(
            Path.Combine(directory, "01-current-v2-corrector-algorithm-revision.summary.txt"),
            summaryLines,
            Utf8WithoutBom);
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

    private sealed record ReferenceInterval(
        int Index,
        PlantState Start,
        PlantState End,
        IReadOnlyDictionary<string, FluidNodeBalance> FrozenNonHydraulicBalances);

    private sealed record BaselineTriggerEvent(
        int IntervalIndex,
        HybridSemiImplicitHydraulicGateStepResult PrimaryResult);

    private sealed record LegacyRescueEvent(
        int IntervalIndex,
        bool Converged,
        int IterationCount,
        double PressureResidual,
        double FlowResidualKilogramsPerSecond);

    private sealed record RevisedEvent(
        int IntervalIndex,
        bool Converged,
        bool LineSearchExhausted,
        int IterationCount,
        int HydraulicEvaluationCount,
        int BacktrackingTrialCount,
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

    private sealed record RevisedRun(
        IReadOnlyList<RevisedEvent> Events,
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
