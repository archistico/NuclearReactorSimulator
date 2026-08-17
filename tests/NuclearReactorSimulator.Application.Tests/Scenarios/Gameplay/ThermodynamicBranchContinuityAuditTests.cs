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
/// M10.9.4.1-H.13 shadow-only experiment applying branch continuity only to the two H.11/H.12 nodes.
/// Production Resolve() remains unchanged and no candidate thermodynamic state is committed.
/// </summary>
public sealed class ThermodynamicBranchContinuityAuditTests
{
    private static readonly TimeSpan Step = TimeSpan.FromMilliseconds(10d);
    private static readonly Encoding Utf8WithoutBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    private static readonly string[] TargetNodeIds = { "steam", "stop-out" };
    private const int IntervalCount = 500;
    private const double PressureTrigger = 0.060d;
    private const double FlowTriggerKilogramsPerSecond = 40d;
    private const double MaximumAlgorithmWorkRatio = 32d;

    private static readonly SemiImplicitHydraulicPrototypeOptions H4Primary = new(
        maximumIterations: 72,
        relaxationFactor: 0.15d,
        relativePressureTolerance: 1e-5d,
        absoluteFlowToleranceKilogramsPerSecond: 1e-2d);

    [Fact(Explicit = true)]
    [Trait("Category", "ThermodynamicBranchContinuityAudit")]
    public void FrozenTriggerEvents_CompareProductionPreviousPhaseContinuityAndBoundedHysteresis()
    {
        var productionThermodynamics = new SimplifiedWaterSteamThermodynamicModel();
        var prototype = new SemiImplicitHydraulicPrototypeSolver(productionThermodynamics);
        var gate = new HybridSemiImplicitHydraulicGateSolver(productionThermodynamics);
        var reference = BuildReferenceTrajectory(prototype);
        var baseline = EvaluatePrimaryGate(reference, gate);
        var intervals = reference.ToDictionary(static item => item.Index);

        Assert.Equal(IntervalCount, reference.Count);
        Assert.Equal(7, baseline.Count);
        Assert.Equal(5, baseline.Count(static item => item.PrimaryResult.Converged));

        var productionRun = RunProductionH9(baseline, intervals, productionThermodynamics);
        Assert.Equal(5, productionRun.Events.Count(static item => item.Result.Converged));
        Assert.Equal(2, productionRun.Events.Count(static item => item.Result.LineSearchExhausted));

        var continuityRun = RunPolicy(
            baseline,
            intervals,
            productionThermodynamics,
            ThermodynamicBranchContinuityOptions.H13PreviousPhaseContinuity);
        var continuityRepeat = RunPolicy(
            baseline,
            intervals,
            productionThermodynamics,
            ThermodynamicBranchContinuityOptions.H13PreviousPhaseContinuity);
        var hysteresisRun = RunPolicy(
            baseline,
            intervals,
            productionThermodynamics,
            ThermodynamicBranchContinuityOptions.H13BoundedHysteresis);
        var hysteresisRepeat = RunPolicy(
            baseline,
            intervals,
            productionThermodynamics,
            ThermodynamicBranchContinuityOptions.H13BoundedHysteresis);

        var continuityDeterministic = Fingerprint(continuityRun) == Fingerprint(continuityRepeat);
        var hysteresisDeterministic = Fingerprint(hysteresisRun) == Fingerprint(hysteresisRepeat);
        Assert.True(continuityDeterministic, "H.13 previous-phase continuity was not exactly deterministic.");
        Assert.True(hysteresisDeterministic, "H.13 bounded hysteresis was not exactly deterministic.");
        Assert.Contains(continuityRun.Events.SelectMany(static item => item.Decisions), static item => item.SelectionDiffersFromProduction);
        Assert.Contains(hysteresisRun.Events.SelectMany(static item => item.Decisions), static item => item.SelectionDiffersFromProduction);
        Assert.InRange(continuityRun.DeterministicHydraulicEvaluationWorkRatio, 0d, MaximumAlgorithmWorkRatio);
        Assert.InRange(hysteresisRun.DeterministicHydraulicEvaluationWorkRatio, 0d, MaximumAlgorithmWorkRatio);
        Assert.InRange(continuityRun.MaximumHydraulicMassClosureKilogramsPerSecond, 0d, 1e-8d);
        Assert.InRange(hysteresisRun.MaximumHydraulicMassClosureKilogramsPerSecond, 0d, 1e-8d);
        Assert.InRange(continuityRun.MaximumHydraulicEnergyOwnershipResidualWatts, 0d, 1e-3d);
        Assert.InRange(hysteresisRun.MaximumHydraulicEnergyOwnershipResidualWatts, 0d, 1e-3d);
        Assert.All(continuityRun.Events, static item => Assert.True(AcceptedMeritStrictlyDecreases(item.Result.Iterations)));
        Assert.All(hysteresisRun.Events, static item => Assert.True(AcceptedMeritStrictlyDecreases(item.Result.Iterations)));

        var continuityQualifies = Qualifies(continuityRun, continuityDeterministic);
        var hysteresisQualifies = Qualifies(hysteresisRun, hysteresisDeterministic);
        WriteAuditReports(
            baseline.Count,
            productionRun,
            continuityRun,
            continuityDeterministic,
            continuityQualifies,
            hysteresisRun,
            hysteresisDeterministic,
            hysteresisQualifies);
    }

    private static ProductionRun RunProductionH9(
        IReadOnlyList<BaselineTriggerEvent> baseline,
        IReadOnlyDictionary<int, ReferenceInterval> intervals,
        IFluidThermodynamicModel productionThermodynamics)
    {
        var solver = new JacobianHydraulicCorrectorSolver(productionThermodynamics);
        var events = baseline.Select(item =>
        {
            var interval = intervals[item.IntervalIndex];
            return new ProductionEvent(
                interval.Index,
                solver.Step(
                    interval.Start,
                    Step,
                    interval.FrozenNonHydraulicBalances,
                    JacobianHydraulicCorrectorOptions.H9AuditDefault));
        }).ToArray();
        return new ProductionRun(events);
    }

    private static PolicyRun RunPolicy(
        IReadOnlyList<BaselineTriggerEvent> baseline,
        IReadOnlyDictionary<int, ReferenceInterval> intervals,
        SimplifiedWaterSteamThermodynamicModel productionThermodynamics,
        ThermodynamicBranchContinuityOptions options)
    {
        var events = new List<PolicyEvent>(baseline.Count);
        foreach (var item in baseline)
        {
            var interval = intervals[item.IntervalIndex];
            var shadowThermodynamics = new ThermodynamicBranchContinuityModel(
                productionThermodynamics,
                productionThermodynamics,
                options,
                TargetNodeIds);
            var solver = new JacobianHydraulicCorrectorSolver(shadowThermodynamics);
            var result = solver.Step(
                interval.Start,
                Step,
                interval.FrozenNonHydraulicBalances,
                JacobianHydraulicCorrectorOptions.H9AuditDefault);
            events.Add(new PolicyEvent(interval.Index, result, shadowThermodynamics.Decisions.ToArray()));
        }

        var evaluationSum = events.Sum(static item => item.Result.HydraulicEvaluationCount);
        var decisions = events.SelectMany(static item => item.Decisions).ToArray();
        return new PolicyRun(
            options.Policy.ToString(),
            options.MaximumRelativePressureDrift,
            options.MaximumTemperatureDriftKelvins,
            events.ToArray(),
            (IntervalCount + evaluationSum) / (double)IntervalCount,
            decisions.Count(static item => item.SelectionDiffersFromProduction),
            decisions.Count(static item => item.SelectedPreviousPhase && item.MultiplePhaseRootsAvailable),
            decisions.Count(static item => string.Equals(item.DecisionKind, "production-hysteresis-release", StringComparison.Ordinal)),
            CountTargetPhaseTransitions(events),
            MaximumFinite(decisions.Select(static item => item.AvoidedProductionPressureDifferencePascals)),
            MaximumFinite(decisions.Select(static item => item.AvoidedProductionTemperatureDifferenceKelvins)),
            events.Max(static item => Math.Abs(item.Result.AppliedHydraulicMassRateClosureResidualKilogramsPerSecond)),
            events.Max(static item => Math.Abs(item.Result.AppliedHydraulicEnergyOwnershipResidualWatts)));
    }

    private static bool Qualifies(PolicyRun run, bool deterministicRepeat)
    {
        var options = JacobianHydraulicCorrectorOptions.H9AuditDefault;
        return deterministicRepeat
            && run.Events.Count == 7
            && run.Events.All(static item => item.Result.Converged)
            && run.Events.All(static item => !item.Result.LineSearchExhausted)
            && run.Events.All(item => item.Result.MaximumRelativePressureFixedPointResidual <= options.RelativePressureTolerance)
            && run.Events.All(item => item.Result.MaximumAbsoluteFlowFixedPointResidualKilogramsPerSecond <= options.AbsoluteFlowToleranceKilogramsPerSecond)
            && run.Events.All(static item => item.Result.NormalizedMeritResidual <= 1d)
            && run.Events.All(static item => AcceptedMeritStrictlyDecreases(item.Result.Iterations))
            && run.DeterministicHydraulicEvaluationWorkRatio <= MaximumAlgorithmWorkRatio
            && run.TargetPhaseTransitionCount == 0
            && run.MaximumHydraulicMassClosureKilogramsPerSecond <= 1e-8d
            && run.MaximumHydraulicEnergyOwnershipResidualWatts <= 1e-3d;
    }

    private static int CountTargetPhaseTransitions(IReadOnlyList<PolicyEvent> events)
    {
        var count = 0;
        foreach (var item in events)
        {
            foreach (var nodeId in TargetNodeIds)
            {
                var selected = item.Decisions
                    .Where(decision => string.Equals(decision.NodeId, nodeId, StringComparison.Ordinal)
                        && decision.MultiplePhaseRootsAvailable)
                    .Select(static decision => decision.SelectedPhase)
                    .ToArray();
                for (var index = 1; index < selected.Length; index++)
                {
                    if (!string.Equals(selected[index - 1], selected[index], StringComparison.Ordinal))
                    {
                        count++;
                    }
                }
            }
        }

        return count;
    }

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

    private static double MaximumFinite(IEnumerable<double> values)
    {
        var finite = values.Where(double.IsFinite).ToArray();
        return finite.Length == 0 ? 0d : finite.Max();
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
            Assert.False(presentation.AnyTripActive, $"Unexpected reference trip at H.13 interval {index + 1}.");
            var end = ToPlantState(engine.LatestCanonicalSnapshot.Control.ProtectedControl.FullPlant.CandidatePlant);
            var hydraulic = solver.Evaluate(start);
            var totalBalances = DeriveInventoryBalances(start, end, Step);
            var frozen = start.FluidNodes.ToDictionary(
                static node => node.Id,
                node => totalBalances[node.Id] - hydraulic.FluidNodeBalances[node.Id],
                StringComparer.Ordinal);
            intervals.Add(new ReferenceInterval(index + 1, start, frozen));
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
        => new(snapshot.Definition, snapshot.FluidNodes, snapshot.Valves, snapshot.Pumps, snapshot.ThermalBodies, snapshot.HeatSources);

    private static string Fingerprint(PolicyRun run)
        => string.Join(
            "||",
            run.Events.Select(item => string.Join(
                "|",
                item.IntervalIndex,
                item.Result.Converged,
                item.Result.LineSearchExhausted,
                item.Result.MaximumRelativePressureFixedPointResidual.ToString("G17", System.Globalization.CultureInfo.InvariantCulture),
                item.Result.MaximumAbsoluteFlowFixedPointResidualKilogramsPerSecond.ToString("G17", System.Globalization.CultureInfo.InvariantCulture),
                item.Result.NormalizedMeritResidual.ToString("G17", System.Globalization.CultureInfo.InvariantCulture),
                string.Join(";", item.Decisions.Select(DecisionFingerprint)))));

    private static string DecisionFingerprint(ThermodynamicBranchContinuityDecision decision)
        => FormattableString.Invariant(
            $"{decision.Sequence}:{decision.NodeId}:{decision.PreviousPhase}:{decision.ProductionPhase}:{decision.SelectedPhase}:{decision.DecisionKind}:{decision.PreviousPhaseRelativePressureDrift:G17}:{decision.PreviousPhaseTemperatureDriftKelvins:G17}");

    private static void WriteAuditReports(
        int triggeredEvents,
        ProductionRun production,
        PolicyRun continuity,
        bool continuityDeterministic,
        bool continuityQualifies,
        PolicyRun hysteresis,
        bool hysteresisDeterministic,
        bool hysteresisQualifies)
    {
        var root = FindRepositoryRoot();
        var directory = Path.Combine(root, "artifacts", "h13-thermodynamic-branch-continuity");
        Directory.CreateDirectory(directory);

        var eventRows = new List<string>
        {
            "interval,production_converged,production_line_search_exhausted,production_pressure_residual,production_flow_residual_kg_s,continuity_converged,continuity_line_search_exhausted,continuity_pressure_residual,continuity_flow_residual_kg_s,hysteresis_converged,hysteresis_line_search_exhausted,hysteresis_pressure_residual,hysteresis_flow_residual_kg_s",
        };
        for (var index = 0; index < production.Events.Count; index++)
        {
            var p = production.Events[index];
            var c = continuity.Events[index];
            var h = hysteresis.Events[index];
            eventRows.Add(FormattableString.Invariant(
                $"{p.IntervalIndex},{p.Result.Converged},{p.Result.LineSearchExhausted},{p.Result.MaximumRelativePressureFixedPointResidual:G17},{p.Result.MaximumAbsoluteFlowFixedPointResidualKilogramsPerSecond:G17},{c.Result.Converged},{c.Result.LineSearchExhausted},{c.Result.MaximumRelativePressureFixedPointResidual:G17},{c.Result.MaximumAbsoluteFlowFixedPointResidualKilogramsPerSecond:G17},{h.Result.Converged},{h.Result.LineSearchExhausted},{h.Result.MaximumRelativePressureFixedPointResidual:G17},{h.Result.MaximumAbsoluteFlowFixedPointResidualKilogramsPerSecond:G17}"));
        }

        var policyRows = new List<string>
        {
            "policy,converged_events,line_search_exhausted_events,work_ratio,branch_overrides,previous_phase_holds,hysteresis_releases,target_phase_transitions,max_avoided_pressure_jump_pa,max_avoided_temperature_jump_k,max_mass_closure_kg_s,max_energy_ownership_w,deterministic,qualification_passes",
            PolicyCsv(continuity, continuityDeterministic, continuityQualifies),
            PolicyCsv(hysteresis, hysteresisDeterministic, hysteresisQualifies),
        };

        var decisionRows = new List<string>
        {
            "policy,interval,sequence,node,previous_phase,production_phase,selected_phase,decision_kind,multiple_roots,previous_phase_root_found,previous_phase_pressure_drift,previous_phase_temperature_drift_k,avoided_production_pressure_difference_pa,avoided_production_temperature_difference_k",
        };
        AddDecisions(decisionRows, continuity);
        AddDecisions(decisionRows, hysteresis);

        File.WriteAllLines(Path.Combine(directory, "02-triggered-event-policy-comparison.csv"), eventRows, Utf8WithoutBom);
        File.WriteAllLines(Path.Combine(directory, "03-branch-continuity-policy-summary.csv"), policyRows, Utf8WithoutBom);
        File.WriteAllLines(Path.Combine(directory, "04-branch-continuity-decisions.csv"), decisionRows, Utf8WithoutBom);

        var productionConverged = production.Events.Count(static item => item.Result.Converged);
        var productionExhausted = production.Events.Count(static item => item.Result.LineSearchExhausted);
        var continuityConverged = continuity.Events.Count(static item => item.Result.Converged);
        var hysteresisConverged = hysteresis.Events.Count(static item => item.Result.Converged);
        var selectedPolicy = hysteresisQualifies
            ? "bounded-previous-phase-hysteresis"
            : continuityQualifies
                ? "previous-phase-continuity"
                : "none";
        var experimentPasses = productionConverged == 5
            && productionExhausted == 2
            && continuityDeterministic
            && hysteresisDeterministic
            && continuity.Events.Count == triggeredEvents
            && hysteresis.Events.Count == triggeredEvents
            && continuity.BranchOverrideCount > 0
            && hysteresis.BranchOverrideCount > 0
            && continuity.DeterministicHydraulicEvaluationWorkRatio <= MaximumAlgorithmWorkRatio
            && hysteresis.DeterministicHydraulicEvaluationWorkRatio <= MaximumAlgorithmWorkRatio
            && continuity.MaximumHydraulicMassClosureKilogramsPerSecond <= 1e-8d
            && hysteresis.MaximumHydraulicMassClosureKilogramsPerSecond <= 1e-8d
            && continuity.MaximumHydraulicEnergyOwnershipResidualWatts <= 1e-3d
            && hysteresis.MaximumHydraulicEnergyOwnershipResidualWatts <= 1e-3d;
        var recommendation = (continuityQualifies || hysteresisQualifies)
            ? $"H.13 recommendation: {selectedPolicy} resolves all frozen P060/F040 events under the unchanged H.9 solver while preserving deterministic safeguards. Keep production explicit and proceed to broader shadow qualification of the selected thermodynamic branch-continuity policy before any activation candidate."
            : "H.13 recommendation: neither narrow branch-continuity policy resolves every frozen event under H.9. Keep production explicit and inspect the policy/decision traces before considering a more explicit active-set or semi-smooth thermodynamic formulation.";

        var summary = new[]
        {
            "================================================================================",
            "M10.9.4.1-H.13 THERMODYNAMIC BRANCH CONTINUITY / HYSTERESIS SHADOW EXPERIMENT SUMMARY",
            "================================================================================",
            "=== 01-current-v2-targeted-thermodynamic-branch-continuity ===",
            "Shadow-only comparison of production inverse-map selection, previous-phase continuity and bounded previous-phase hysteresis at H.12 nodes steam/stop-out; production Resolve() remains unchanged and no shadow state is committed.",
            FormattableString.Invariant($"production-shadow-steps={IntervalCount}; frozen-trigger=P060-F040; triggered-events={triggeredEvents}; production-H9-converged={productionConverged}/{triggeredEvents}; production-H9-line-search-exhausted={productionExhausted}; target-node-ids=steam|stop-out;"),
            FormattableString.Invariant($"previous-phase-continuity-converged={continuityConverged}/{triggeredEvents}; line-search-exhausted={continuity.Events.Count(static item => item.Result.LineSearchExhausted)}; branch-overrides={continuity.BranchOverrideCount}; previous-phase-holds={continuity.PreviousPhaseHoldCount}; target-phase-transitions={continuity.TargetPhaseTransitionCount}; max-avoided-pressure-jump={continuity.MaximumAvoidedProductionPressureDifferencePascals:0.000000} Pa; max-avoided-temperature-jump={continuity.MaximumAvoidedProductionTemperatureDifferenceKelvins:0.000000} K; deterministic-work-ratio={continuity.DeterministicHydraulicEvaluationWorkRatio:0.000000}; deterministic-repeat={continuityDeterministic}; qualification-passes={continuityQualifies};"),
            FormattableString.Invariant($"bounded-hysteresis-pressure-drift-limit={hysteresis.MaximumRelativePressureDrift:0.000000}; temperature-drift-limit={hysteresis.MaximumTemperatureDriftKelvins:0.000000} K; converged={hysteresisConverged}/{triggeredEvents}; line-search-exhausted={hysteresis.Events.Count(static item => item.Result.LineSearchExhausted)}; branch-overrides={hysteresis.BranchOverrideCount}; previous-phase-holds={hysteresis.PreviousPhaseHoldCount}; hysteresis-releases={hysteresis.HysteresisReleaseCount}; target-phase-transitions={hysteresis.TargetPhaseTransitionCount}; max-avoided-pressure-jump={hysteresis.MaximumAvoidedProductionPressureDifferencePascals:0.000000} Pa; max-avoided-temperature-jump={hysteresis.MaximumAvoidedProductionTemperatureDifferenceKelvins:0.000000} K; deterministic-work-ratio={hysteresis.DeterministicHydraulicEvaluationWorkRatio:0.000000}; deterministic-repeat={hysteresisDeterministic}; qualification-passes={hysteresisQualifies};"),
            FormattableString.Invariant($"max-closure/ownership continuity={continuity.MaximumHydraulicMassClosureKilogramsPerSecond:0.000000000}/{continuity.MaximumHydraulicEnergyOwnershipResidualWatts:0.000000000}; hysteresis={hysteresis.MaximumHydraulicMassClosureKilogramsPerSecond:0.000000000}/{hysteresis.MaximumHydraulicEnergyOwnershipResidualWatts:0.000000000}; selected-shadow-policy={selectedPolicy}; thermodynamic-branch-continuity-experiment-passes={experimentPasses};"),
            "production-resolve-order-changed=False; production-previous-state-hysteresis-introduced=False; production-hybrid-active=False; production-fixed-step=10.000 ms; shadow-candidates-committed=False; H9-corrector-replaced=False; plant-network-orchestrator-routing-changed=False; trigger-retuning=False; physical-coefficient-retuning=False; hidden-flow-filtering=False;",
            recommendation,
            FormattableString.Invariant($"Detailed CSV files: \"{directory}\""),
        };
        File.WriteAllLines(
            Path.Combine(directory, "01-current-v2-thermodynamic-branch-continuity.summary.txt"),
            summary,
            Utf8WithoutBom);
    }

    private static string PolicyCsv(PolicyRun run, bool deterministic, bool qualifies)
        => FormattableString.Invariant(
            $"{run.Policy},{run.Events.Count(static item => item.Result.Converged)},{run.Events.Count(static item => item.Result.LineSearchExhausted)},{run.DeterministicHydraulicEvaluationWorkRatio:G17},{run.BranchOverrideCount},{run.PreviousPhaseHoldCount},{run.HysteresisReleaseCount},{run.TargetPhaseTransitionCount},{run.MaximumAvoidedProductionPressureDifferencePascals:G17},{run.MaximumAvoidedProductionTemperatureDifferenceKelvins:G17},{run.MaximumHydraulicMassClosureKilogramsPerSecond:G17},{run.MaximumHydraulicEnergyOwnershipResidualWatts:G17},{deterministic},{qualifies}");

    private static void AddDecisions(List<string> rows, PolicyRun run)
    {
        foreach (var item in run.Events)
        {
            foreach (var decision in item.Decisions)
            {
                rows.Add(FormattableString.Invariant(
                    $"{run.Policy},{item.IntervalIndex},{decision.Sequence},{decision.NodeId},{decision.PreviousPhase},{decision.ProductionPhase},{decision.SelectedPhase},{decision.DecisionKind},{decision.MultiplePhaseRootsAvailable},{decision.PreviousPhaseRootFound},{decision.PreviousPhaseRelativePressureDrift:G17},{decision.PreviousPhaseTemperatureDriftKelvins:G17},{decision.AvoidedProductionPressureDifferencePascals:G17},{decision.AvoidedProductionTemperatureDifferenceKelvins:G17}"));
            }
        }
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
        IReadOnlyDictionary<string, FluidNodeBalance> FrozenNonHydraulicBalances);

    private sealed record BaselineTriggerEvent(int IntervalIndex, HybridSemiImplicitHydraulicGateStepResult PrimaryResult);

    private sealed record ProductionEvent(int IntervalIndex, JacobianHydraulicCorrectorStepResult Result);

    private sealed record ProductionRun(IReadOnlyList<ProductionEvent> Events);

    private sealed record PolicyEvent(
        int IntervalIndex,
        JacobianHydraulicCorrectorStepResult Result,
        IReadOnlyList<ThermodynamicBranchContinuityDecision> Decisions);

    private sealed record PolicyRun(
        string Policy,
        double MaximumRelativePressureDrift,
        double MaximumTemperatureDriftKelvins,
        IReadOnlyList<PolicyEvent> Events,
        double DeterministicHydraulicEvaluationWorkRatio,
        int BranchOverrideCount,
        int PreviousPhaseHoldCount,
        int HysteresisReleaseCount,
        int TargetPhaseTransitionCount,
        double MaximumAvoidedProductionPressureDifferencePascals,
        double MaximumAvoidedProductionTemperatureDifferenceKelvins,
        double MaximumHydraulicMassClosureKilogramsPerSecond,
        double MaximumHydraulicEnergyOwnershipResidualWatts);
}
