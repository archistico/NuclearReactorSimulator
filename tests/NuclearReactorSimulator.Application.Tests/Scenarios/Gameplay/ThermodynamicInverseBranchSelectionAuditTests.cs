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
/// M10.9.4.1-H.12 shadow-only audit of inverse thermodynamic branch selection at the two H.11 nodes.
/// It observes all existing simplified-model branch roots and selection priority without changing Resolve().
/// </summary>
public sealed class ThermodynamicInverseBranchSelectionAuditTests
{
    private static readonly TimeSpan Step = TimeSpan.FromMilliseconds(10d);
    private static readonly Encoding Utf8WithoutBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    private const int IntervalCount = 500;
    private const double PressureTrigger = 0.060d;
    private const double FlowTriggerKilogramsPerSecond = 40d;

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
    [Trait("Category", "ThermodynamicInverseBranchSelectionAudit")]
    public void PersistentH9Failures_ExplainH11PhaseSwitchesThroughInverseBranchSelection()
    {
        var thermodynamics = new SimplifiedWaterSteamThermodynamicModel();
        var prototype = new SemiImplicitHydraulicPrototypeSolver(thermodynamics);
        var gate = new HybridSemiImplicitHydraulicGateSolver(thermodynamics);
        var h7Solver = new ResidualBacktrackingHydraulicCorrectorSolver(thermodynamics);
        var h8Solver = new AndersonHydraulicCorrectorSolver(thermodynamics);
        var h9Solver = new JacobianHydraulicCorrectorSolver(thermodynamics);
        var h10Analyzer = new HydraulicMapSmoothnessAnalyzer(thermodynamics);
        var h11Analyzer = new ThermodynamicSwitchingLocalizationAnalyzer(thermodynamics, thermodynamics);
        var h12Analyzer = new ThermodynamicInverseBranchSelectionAnalyzer(thermodynamics, thermodynamics);
        var reference = BuildReferenceTrajectory(prototype);
        var baseline = EvaluatePrimaryGate(reference, gate);
        var intervals = reference.ToDictionary(static item => item.Index);

        Assert.Equal(IntervalCount, reference.Count);
        Assert.Equal(7, baseline.Count);
        Assert.Equal(5, baseline.Count(static item => item.PrimaryResult.Converged));

        var h6Converged = baseline.Count(item => prototype.StepSemiImplicit(
            intervals[item.IntervalIndex].Start,
            Step,
            intervals[item.IntervalIndex].FrozenNonHydraulicBalances,
            H6SelectedRescue).Converged);
        var h7Converged = baseline.Count(item => h7Solver.Step(
            intervals[item.IntervalIndex].Start,
            Step,
            intervals[item.IntervalIndex].FrozenNonHydraulicBalances,
            ResidualBacktrackingHydraulicCorrectorOptions.H7AuditDefault).Converged);
        var h8Converged = baseline.Count(item => h8Solver.Step(
            intervals[item.IntervalIndex].Start,
            Step,
            intervals[item.IntervalIndex].FrozenNonHydraulicBalances,
            AndersonHydraulicCorrectorOptions.H8AuditDefault).Converged);

        Assert.Equal(6, h6Converged);
        Assert.Equal(5, h7Converged);
        Assert.Equal(5, h8Converged);

        var h9Events = baseline.Select(item =>
        {
            var interval = intervals[item.IntervalIndex];
            var result = h9Solver.Step(
                interval.Start,
                Step,
                interval.FrozenNonHydraulicBalances,
                JacobianHydraulicCorrectorOptions.H9AuditDefault);
            return new H9Event(interval.Index, result);
        }).ToArray();

        Assert.Equal(5, h9Events.Count(static item => item.Result.Converged));
        Assert.Equal(2, h9Events.Count(static item => !item.Result.Converged));

        var diagnostics = h9Events
            .Where(static item => !item.Result.Converged)
            .Select(item => BuildDiagnostic(item, h10Analyzer, h11Analyzer, h12Analyzer))
            .ToArray();
        var repeatDiagnostics = h9Events
            .Where(static item => !item.Result.Converged)
            .Select(item => BuildDiagnostic(item, h10Analyzer, h11Analyzer, h12Analyzer))
            .ToArray();

        Assert.Equal(2, diagnostics.Length);
        Assert.Equal(2, diagnostics.Sum(static item => item.H11.LocalizedNodeCount));
        Assert.Equal(2, diagnostics.Sum(static item => item.H12.NodeCount));
        Assert.All(
            diagnostics.SelectMany(static item => item.H12.Nodes).SelectMany(static item => item.Probes),
            static probe => Assert.Equal(probe.H11ResolvedPhase, probe.ProductionSelectedPhase));

        var deterministicRepeat = Fingerprint(diagnostics) == Fingerprint(repeatDiagnostics);
        Assert.True(deterministicRepeat, "H.12 inverse branch-selection diagnosis was not exactly deterministic.");

        WriteAuditReports(
            baseline.Count,
            h6Converged,
            h7Converged,
            h8Converged,
            diagnostics,
            deterministicRepeat);
    }

    private static DiagnosticEvent BuildDiagnostic(
        H9Event item,
        HydraulicMapSmoothnessAnalyzer h10Analyzer,
        ThermodynamicSwitchingLocalizationAnalyzer h11Analyzer,
        ThermodynamicInverseBranchSelectionAnalyzer h12Analyzer)
    {
        var h10 = h10Analyzer.Analyze(item.Result.CandidateState);
        var h11 = h11Analyzer.Analyze(item.Result.CandidateState, h10);
        var h12 = h12Analyzer.Analyze(item.Result.CandidateState, h11);
        return new DiagnosticEvent(
            item.IntervalIndex,
            item.Result.MaximumRelativePressureFixedPointResidual,
            item.Result.MaximumAbsoluteFlowFixedPointResidualKilogramsPerSecond,
            item.Result.NormalizedMeritResidual,
            h10,
            h11,
            h12);
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
            Assert.False(presentation.AnyTripActive, $"Unexpected reference trip at H.12 interval {index + 1}.");
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

    private static string Fingerprint(IReadOnlyList<DiagnosticEvent> diagnostics)
        => string.Join(
            "||",
            diagnostics.Select(item => string.Join(
                "|",
                item.IntervalIndex,
                item.H9PressureResidual.ToString("G17", System.Globalization.CultureInfo.InvariantCulture),
                item.H9FlowResidualKilogramsPerSecond.ToString("G17", System.Globalization.CultureInfo.InvariantCulture),
                string.Join(";", item.H12.Nodes.Select(NodeFingerprint)))));

    private static string NodeFingerprint(ThermodynamicInverseBranchNodeDiagnosis node)
        => string.Join(
            ":",
            node.NodeId,
            node.MechanismClassification,
            node.LateBoundarySaturatedShadowedCount,
            node.PreviousStateTieBreakObserved,
            string.Join(",", node.Probes.Select(static probe => FormattableString.Invariant(
                $"{probe.Probe}/{probe.ProductionSelectedBranch}/{probe.CoarseSaturatedRootFound}/{probe.BoundaryAwareSaturatedRootFound}/{probe.CoarseSuperheatedRootFound}/{probe.PreviousStateSelectionSensitive}"))));

    private static void WriteAuditReports(
        int triggeredEvents,
        int h6Converged,
        int h7Converged,
        int h8Converged,
        IReadOnlyList<DiagnosticEvent> diagnostics,
        bool deterministicRepeat)
    {
        var repositoryRoot = FindRepositoryRoot();
        var directory = Path.Combine(repositoryRoot, "artifacts", "h12-thermodynamic-inverse-branch-selection");
        Directory.CreateDirectory(directory);

        var eventRows = new List<string>
        {
            "interval,h9_pressure_residual,h9_flow_residual_kg_s,h9_merit,h10_switch_nodes,h11_localized_nodes,h12_branch_nodes",
        };
        var nodeRows = new List<string>
        {
            "interval,node,nominal_phase,all_probes_overlapping_roots,coarse_saturated_detection_toggles,late_boundary_saturated_shadowed_count,previous_state_tiebreak_observed,mechanism,recommended_shadow_policy",
        };
        var probeRows = new List<string>
        {
            "interval,node,probe,h11_phase,production_selected_branch,production_selected_phase,saturated_root_available,superheated_root_available,multiple_phase_roots,coarse_saturated_root,boundary_aware_saturated_root,coarse_superheated_root,boundary_aware_superheated_root,late_boundary_saturated_shadowed,previous_state_selection_sensitive,mass_kg,internal_energy_j",
        };
        var candidateRows = new List<string>
        {
            "interval,node,probe,attempt_order,branch,root_found,phase,pressure_pa,temperature_k,vapor_quality",
        };

        foreach (var item in diagnostics)
        {
            eventRows.Add(FormattableString.Invariant(
                $"{item.IntervalIndex},{item.H9PressureResidual:G17},{item.H9FlowResidualKilogramsPerSecond:G17},{item.H9NormalizedMerit:G17},{item.H10.ThermodynamicPhaseSwitchCount},{item.H11.LocalizedNodeCount},{item.H12.NodeCount}"));
            foreach (var node in item.H12.Nodes)
            {
                nodeRows.Add(FormattableString.Invariant(
                    $"{item.IntervalIndex},{node.NodeId},{node.NominalPhase},{node.AllProbesHaveOverlappingPhaseRoots},{node.CoarseSaturatedDetectionToggles},{node.LateBoundarySaturatedShadowedCount},{node.PreviousStateTieBreakObserved},{node.MechanismClassification},{node.RecommendedShadowPolicy}"));
                foreach (var probe in node.Probes)
                {
                    probeRows.Add(FormattableString.Invariant(
                        $"{item.IntervalIndex},{node.NodeId},{probe.Probe},{probe.H11ResolvedPhase},{probe.ProductionSelectedBranch},{probe.ProductionSelectedPhase},{probe.SaturatedRootAvailable},{probe.SuperheatedRootAvailable},{probe.MultiplePhaseRootsAvailable},{probe.CoarseSaturatedRootFound},{probe.BoundaryAwareSaturatedRootFound},{probe.CoarseSuperheatedRootFound},{probe.BoundaryAwareSuperheatedRootFound},{probe.LateBoundarySaturatedShadowedByEarlierSuperheated},{probe.PreviousStateSelectionSensitive},{probe.MassKilograms:G17},{probe.InternalEnergyJoules:G17}"));
                    foreach (var candidate in probe.Candidates)
                    {
                        candidateRows.Add(FormattableString.Invariant(
                            $"{item.IntervalIndex},{node.NodeId},{probe.Probe},{candidate.AttemptOrder},{candidate.Branch},{candidate.RootFound},{candidate.Phase},{candidate.PressurePascals:G17},{candidate.TemperatureKelvins:G17},{NullableDouble(candidate.VaporQuality)}"));
                    }
                }
            }
        }

        File.WriteAllLines(Path.Combine(directory, "02-persistent-event-branch-selection.csv"), eventRows, Utf8WithoutBom);
        File.WriteAllLines(Path.Combine(directory, "03-node-branch-mechanisms.csv"), nodeRows, Utf8WithoutBom);
        File.WriteAllLines(Path.Combine(directory, "04-probe-branch-selection.csv"), probeRows, Utf8WithoutBom);
        File.WriteAllLines(Path.Combine(directory, "05-branch-candidates.csv"), candidateRows, Utf8WithoutBom);

        var nodes = diagnostics.SelectMany(static item => item.H12.Nodes).ToArray();
        var probes = nodes.SelectMany(static item => item.Probes).ToArray();
        var branchNodes = nodes.Length;
        var overlappingNodes = nodes.Count(static item => item.AllProbesHaveOverlappingPhaseRoots);
        var coarseToggleNodes = nodes.Count(static item => item.CoarseSaturatedDetectionToggles);
        var lateShadowNodes = nodes.Count(static item => item.LateBoundarySaturatedShadowedCount > 0);
        var lateShadowProbes = probes.Count(static item => item.LateBoundarySaturatedShadowedByEarlierSuperheated);
        var previousTieBreakNodes = nodes.Count(static item => item.PreviousStateTieBreakObserved);
        var selectionMatchesH11 = probes.All(static item => string.Equals(
            item.H11ResolvedPhase,
            item.ProductionSelectedPhase,
            StringComparison.Ordinal));
        var mechanismConfirmed = branchNodes == 2
            && overlappingNodes == 2
            && coarseToggleNodes == 2
            && lateShadowNodes == 2
            && lateShadowProbes > 0
            && previousTieBreakNodes == 0;
        var diagnosticPasses = deterministicRepeat
            && diagnostics.Count == 2
            && branchNodes == 2
            && selectionMatchesH11
            && probes.All(static item => item.Candidates.Count == 5);
        var distinctNodes = nodes
            .Select(static item => item.NodeId)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static value => value, StringComparer.Ordinal)
            .ToArray();
        var mechanisms = nodes
            .Select(static item => item.MechanismClassification)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static value => value, StringComparer.Ordinal)
            .ToArray();
        var recommendation = mechanismConfirmed
            ? "H.12 recommendation: both persistent failures expose overlapping saturated/superheated roots; the coarse saturated detector toggles under tiny conserved-inventory perturbations, allowing the earlier coarse-superheated branch to shadow a still-valid later boundary-aware saturated root, while previousState provides no tie-break. Keep production unchanged and design a narrow shadow continuity/hysteresis branch-selection experiment before any active-set or semi-smooth hydraulic solver."
            : "H.12 recommendation: the proposed overlapping-root/coarse-detection mechanism was not fully confirmed; keep production unchanged and inspect the detailed branch-candidate evidence before selecting any active-set policy.";

        var summary = new[]
        {
            "================================================================================",
            "M10.9.4.1-H.12 THERMODYNAMIC INVERSE BRANCH SELECTION AUDIT SUMMARY",
            "================================================================================",
            "=== 01-current-v2-inverse-map-branch-selection ===",
            "Shadow-only inspection of all simplified water/steam inverse-map branches at the concrete H.11 phase-boundary probes; Resolve() ordering and production behavior remain unchanged.",
            FormattableString.Invariant($"production-shadow-steps={IntervalCount}; frozen-trigger=P060-F040; triggered-events={triggeredEvents}; H4-primary-converged=5/{triggeredEvents}; H6-rescue-converged={h6Converged}/{triggeredEvents}; H7-residual-backtracking-converged={h7Converged}/{triggeredEvents}; H8-Anderson-converged={h8Converged}/{triggeredEvents}; H9-Jacobian-Newton-converged=5/{triggeredEvents}; persistent-H9-failures={diagnostics.Count};"),
            FormattableString.Invariant($"H11-localized-nodes={branchNodes}; distinct-node-ids={string.Join("|", distinctNodes)}; H12-overlapping-root-nodes={overlappingNodes}; coarse-saturated-detection-toggle-nodes={coarseToggleNodes}; late-boundary-saturated-shadow-nodes={lateShadowNodes}; late-boundary-saturated-shadow-probes={lateShadowProbes}; previous-state-tiebreak-nodes={previousTieBreakNodes};"),
            FormattableString.Invariant($"mechanisms={string.Join("|", mechanisms)}; production-selection-matches-H11={selectionMatchesH11}; overlapping-root-coarse-priority-mechanism-confirmed={mechanismConfirmed}; deterministic-repeat={deterministicRepeat}; thermodynamic-inverse-branch-selection-audit-passes={diagnosticPasses};"),
            "production-resolve-order=coarse-saturated>subcooled-liquid>coarse-superheated>boundary-aware-saturated>boundary-aware-superheated; resolver-order-changed=False; previous-state-hysteresis-introduced=False; active-set-enforced=False; semi-smooth-solver-introduced=False; production-hybrid-active=False; production-fixed-step=10.000 ms; shadow-candidates-committed=False; H9-corrector-replaced=False; plant-network-orchestrator-routing-changed=False; trigger-retuning=False; physical-coefficient-retuning=False; hidden-flow-filtering=False;",
            recommendation,
            FormattableString.Invariant($"Detailed CSV files: \"{directory}\""),
        };
        File.WriteAllLines(
            Path.Combine(directory, "01-current-v2-thermodynamic-inverse-branch-selection.summary.txt"),
            summary,
            Utf8WithoutBom);
    }

    private static string NullableDouble(double? value)
        => value.HasValue
            ? value.Value.ToString("G17", System.Globalization.CultureInfo.InvariantCulture)
            : string.Empty;

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

    private sealed record H9Event(int IntervalIndex, JacobianHydraulicCorrectorStepResult Result);

    private sealed record DiagnosticEvent(
        int IntervalIndex,
        double H9PressureResidual,
        double H9FlowResidualKilogramsPerSecond,
        double H9NormalizedMerit,
        HydraulicMapSmoothnessReport H10,
        ThermodynamicSwitchingLocalizationReport H11,
        ThermodynamicInverseBranchSelectionReport H12);
}
