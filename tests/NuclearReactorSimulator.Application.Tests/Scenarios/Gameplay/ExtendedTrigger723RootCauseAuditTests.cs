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
/// M10.9.4.1-H.15 shadow-only root-cause diagnosis of the extended P060/F040 trigger at interval 723.
/// It reuses the validated H.9 solver, H.13 bounded branch-continuity policy and H.10-H.12 diagnostics
/// without changing or committing any production state.
/// </summary>
public sealed class ExtendedTrigger723RootCauseAuditTests
{
    private static readonly TimeSpan Step = TimeSpan.FromMilliseconds(10d);
    private static readonly Encoding Utf8WithoutBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    private static readonly string[] TargetContinuityNodeIds = { "steam", "stop-out" };
    private static readonly int[] NeighborhoodIntervals = { 721, 722, 723, 724 };
    private const int IntervalCount = 724;
    private const int TargetInterval = 723;
    private const double PressureTrigger = 0.060d;
    private const double FlowTriggerKilogramsPerSecond = 40d;

    private static readonly SemiImplicitHydraulicPrototypeOptions H4Primary = new(
        maximumIterations: 72,
        relaxationFactor: 0.15d,
        relativePressureTolerance: 1e-5d,
        absoluteFlowToleranceKilogramsPerSecond: 1e-2d);

    [Fact(Explicit = true)]
    [Trait("Category", "ExtendedTrigger723RootCauseAudit")]
    public void ExtendedTrigger723_IsDiagnosedAcrossAllHydraulicPathsAndThermodynamicNodes()
    {
        var productionThermodynamics = new SimplifiedWaterSteamThermodynamicModel();
        var prototype = new SemiImplicitHydraulicPrototypeSolver(productionThermodynamics);
        var gate = new HybridSemiImplicitHydraulicGateSolver(productionThermodynamics);
        var smoothnessAnalyzer = new HydraulicMapSmoothnessAnalyzer(productionThermodynamics);

        var reference = BuildReferenceTrajectory(prototype);
        var triggers = EvaluatePrimaryGate(reference, gate);
        var intervals = reference.ToDictionary(static item => item.Index);

        Assert.InRange(reference.Count, IntervalCount, IntervalCount);
        Assert.InRange(triggers.Count, 9, 9);
        Assert.InRange(triggers.Count(static item => item.PrimaryResult.Converged), 6, 6);
        Assert.Contains(triggers, static item => item.IntervalIndex == TargetInterval && !item.PrimaryResult.Converged);

        var neighborhood = RunNeighborhood(intervals, productionThermodynamics, smoothnessAnalyzer);
        var neighborhoodRepeat = RunNeighborhood(intervals, productionThermodynamics, smoothnessAnalyzer);
        var deterministicRepeat = Fingerprint(neighborhood) == Fingerprint(neighborhoodRepeat);
        Assert.True(deterministicRepeat, "H.15 interval-723 neighborhood diagnosis was not exactly deterministic.");

        var target = Assert.Single(neighborhood, static item => item.IntervalIndex == TargetInterval);
        Assert.False(target.Result.Converged);
        Assert.True(target.Result.LineSearchExhausted);
        Assert.Equal(0, target.BranchOverrideCount);
        Assert.Equal(0, target.HysteresisReleaseCount);
        Assert.True(target.PreviousPhaseHoldCount > 0);

        var targetTrigger = Assert.Single(triggers, static item => item.IntervalIndex == TargetInterval);
        Assert.False(targetTrigger.PrimaryResult.Converged);

        WriteAuditReports(triggers, neighborhood, deterministicRepeat, productionThermodynamics);
    }

    private static IReadOnlyList<NeighborhoodDiagnostic> RunNeighborhood(
        IReadOnlyDictionary<int, ReferenceInterval> intervals,
        SimplifiedWaterSteamThermodynamicModel productionThermodynamics,
        HydraulicMapSmoothnessAnalyzer smoothnessAnalyzer)
    {
        var diagnostics = new List<NeighborhoodDiagnostic>(NeighborhoodIntervals.Length);
        foreach (var intervalIndex in NeighborhoodIntervals)
        {
            var interval = intervals[intervalIndex];
            var shadowThermodynamics = new ThermodynamicBranchContinuityModel(
                productionThermodynamics,
                productionThermodynamics,
                ThermodynamicBranchContinuityOptions.H13BoundedHysteresis,
                TargetContinuityNodeIds);
            var solver = new JacobianHydraulicCorrectorSolver(shadowThermodynamics);
            var result = solver.Step(
                interval.Start,
                Step,
                interval.FrozenNonHydraulicBalances,
                JacobianHydraulicCorrectorOptions.H9AuditDefault);
            var decisions = shadowThermodynamics.Decisions.ToArray();
            var candidateSmoothness = smoothnessAnalyzer.Analyze(result.CandidateState);
            var explicitEndSmoothness = smoothnessAnalyzer.Analyze(interval.End);
            var residuals = BuildNodeResiduals(result);
            var inverseDiagnostics = DiagnoseInverseBranches(result.CandidateState, productionThermodynamics);
            var explicitEndInverseDiagnostics = DiagnoseInverseBranches(interval.End, productionThermodynamics);

            diagnostics.Add(new NeighborhoodDiagnostic(
                intervalIndex,
                result,
                decisions.Count(static item => item.SelectionDiffersFromProduction),
                decisions.Count(static item => string.Equals(item.DecisionKind, "hold-previous-phase-hysteresis", StringComparison.Ordinal)),
                decisions.Count(static item => string.Equals(item.DecisionKind, "production-hysteresis-release", StringComparison.Ordinal)),
                candidateSmoothness,
                explicitEndSmoothness,
                residuals,
                inverseDiagnostics,
                explicitEndInverseDiagnostics));
        }

        return diagnostics.ToArray();
    }


    private static IReadOnlyList<WaterSteamInverseBranchSelectionDiagnostic> DiagnoseInverseBranches(
        PlantState state,
        SimplifiedWaterSteamThermodynamicModel productionThermodynamics)
        => state.FluidNodes
            .OrderBy(static node => node.Id, StringComparer.Ordinal)
            .Select(node => productionThermodynamics.DiagnoseInverseBranchSelection(
                node.Definition,
                node.Inventory,
                node.Thermodynamics))
            .ToArray();

    private static IReadOnlyList<NodeResidual> BuildNodeResiduals(JacobianHydraulicCorrectorStepResult result)
    {
        return result.CandidateState.FluidNodes
            .OrderBy(static node => node.Id, StringComparer.Ordinal)
            .Select(node =>
            {
                var mapped = result.HydraulicEvaluation.FluidNodeBalances[node.Id];
                var applied = result.AppliedHydraulicBalances[node.Id];
                return new NodeResidual(
                    node.Id,
                    mapped.NetMassFlowRate.KilogramsPerSecond - applied.NetMassFlowRate.KilogramsPerSecond,
                    mapped.NetEnergyRate.Watts - applied.NetEnergyRate.Watts);
            })
            .ToArray();
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
            Assert.False(presentation.AnyTripActive, $"Unexpected reference trip at H.15 interval {index + 1}.");
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

    private static string Fingerprint(IReadOnlyList<NeighborhoodDiagnostic> diagnostics)
        => string.Join(
            "||",
            diagnostics.Select(item => string.Join(
                "|",
                item.IntervalIndex,
                item.Result.Converged,
                item.Result.LineSearchExhausted,
                item.Result.MaximumRelativePressureFixedPointResidual.ToString("G17", CultureInfo.InvariantCulture),
                item.Result.MaximumAbsoluteFlowFixedPointResidualKilogramsPerSecond.ToString("G17", CultureInfo.InvariantCulture),
                item.Result.NormalizedMeritResidual.ToString("G17", CultureInfo.InvariantCulture),
                item.BranchOverrideCount,
                item.PreviousPhaseHoldCount,
                item.HysteresisReleaseCount,
                item.CandidateSmoothness.HydraulicBranchSwitchCount,
                item.CandidateSmoothness.HydraulicNonSmoothEvidenceCount,
                item.CandidateSmoothness.ThermodynamicPhaseSwitchCount,
                item.CandidateSmoothness.ThermodynamicNonSmoothEvidenceCount,
                string.Join(";", item.NodeResiduals.Select(static residual => FormattableString.Invariant(
                    $"{residual.NodeId}:{residual.MassResidualKilogramsPerSecond:G17}:{residual.EnergyResidualWatts:G17}"))),
                string.Join(";", item.InverseDiagnostics.Select(static diagnostic => FormattableString.Invariant(
                    $"{diagnostic.NodeId}:{diagnostic.ProductionSelectedBranch}:{diagnostic.MultiplePhaseRootsAvailable}:{diagnostic.LateBoundarySaturatedShadowedByEarlierSuperheated}"))),
                string.Join(";", item.ExplicitEndInverseDiagnostics.Select(static diagnostic => FormattableString.Invariant(
                    $"{diagnostic.NodeId}:{diagnostic.ProductionSelectedBranch}:{diagnostic.MultiplePhaseRootsAvailable}:{diagnostic.LateBoundarySaturatedShadowedByEarlierSuperheated}"))))));

    private static void WriteAuditReports(
        IReadOnlyList<BaselineTriggerEvent> triggers,
        IReadOnlyList<NeighborhoodDiagnostic> diagnostics,
        bool deterministicRepeat,
        SimplifiedWaterSteamThermodynamicModel productionThermodynamics)
    {
        var root = FindRepositoryRoot();
        var directory = Path.Combine(root, "artifacts", "h15-extended-trigger-723-root-cause");
        Directory.CreateDirectory(directory);

        var triggerIndexes = triggers.Select(static item => item.IntervalIndex).ToHashSet();
        var solverRows = new List<string>
        {
            "interval,p060_f040_trigger,h4_primary_converged,h9_hysteresis_converged,h9_line_search_exhausted,pressure_residual,flow_residual_kg_s,normalized_merit,hydraulic_evaluations,jacobian_builds,jacobian_acceptances,jacobian_rejected,residual_fallback_attempts,residual_fallback_acceptances,branch_overrides,previous_phase_holds,hysteresis_releases",
        };
        foreach (var item in diagnostics)
        {
            var trigger = triggers.FirstOrDefault(triggerItem => triggerItem.IntervalIndex == item.IntervalIndex);
            var h4PrimaryConverged = trigger is null
                ? string.Empty
                : trigger.PrimaryResult.Converged.ToString();
            solverRows.Add(FormattableString.Invariant(
                $"{item.IntervalIndex},{triggerIndexes.Contains(item.IntervalIndex)},{h4PrimaryConverged},{item.Result.Converged},{item.Result.LineSearchExhausted},{item.Result.MaximumRelativePressureFixedPointResidual:G17},{item.Result.MaximumAbsoluteFlowFixedPointResidualKilogramsPerSecond:G17},{item.Result.NormalizedMeritResidual:G17},{item.Result.HydraulicEvaluationCount},{item.Result.JacobianBuildAttempts},{item.Result.JacobianDirectionAcceptances},{item.Result.JacobianRejectedCount},{item.Result.ResidualFallbackAttempts},{item.Result.ResidualFallbackAcceptances},{item.BranchOverrideCount},{item.PreviousPhaseHoldCount},{item.HysteresisReleaseCount}"));
        }
        File.WriteAllLines(Path.Combine(directory, "02-neighborhood-solver-results.csv"), solverRows, Utf8WithoutBom);

        var residualRows = new List<string>
        {
            "interval,node,mapped_minus_applied_mass_kg_s,mapped_minus_applied_energy_w,abs_mass_rank,abs_energy_rank",
        };
        foreach (var item in diagnostics)
        {
            var massRanks = item.NodeResiduals
                .OrderByDescending(static residual => Math.Abs(residual.MassResidualKilogramsPerSecond))
                .Select((residual, index) => (residual.NodeId, Rank: index + 1))
                .ToDictionary(static pair => pair.NodeId, static pair => pair.Rank, StringComparer.Ordinal);
            var energyRanks = item.NodeResiduals
                .OrderByDescending(static residual => Math.Abs(residual.EnergyResidualWatts))
                .Select((residual, index) => (residual.NodeId, Rank: index + 1))
                .ToDictionary(static pair => pair.NodeId, static pair => pair.Rank, StringComparer.Ordinal);
            foreach (var residual in item.NodeResiduals)
            {
                residualRows.Add(FormattableString.Invariant(
                    $"{item.IntervalIndex},{residual.NodeId},{residual.MassResidualKilogramsPerSecond:G17},{residual.EnergyResidualWatts:G17},{massRanks[residual.NodeId]},{energyRanks[residual.NodeId]}"));
            }
        }
        File.WriteAllLines(Path.Combine(directory, "03-node-fixed-point-residual-ranking.csv"), residualRows, Utf8WithoutBom);

        var pathRows = new List<string>
        {
            "interval,state,component_kind,component_id,from_node,to_node,base_branch,coarse_minus_branch,coarse_plus_branch,fine_minus_branch,fine_plus_branch,base_driving_pressure_pa,base_flow_kg_s,derivative_scale_growth,one_sided_slope_asymmetry,branch_switch,nonsmooth_evidence",
        };
        foreach (var item in diagnostics)
        {
            AppendPathRows(pathRows, item.IntervalIndex, "h9-hysteresis-candidate", item.CandidateSmoothness);
            AppendPathRows(pathRows, item.IntervalIndex, "explicit-end", item.ExplicitEndSmoothness);
        }
        File.WriteAllLines(Path.Combine(directory, "04-all-hydraulic-path-probes.csv"), pathRows, Utf8WithoutBom);

        var thermoRows = new List<string>
        {
            "interval,state,node,base_phase,energy_minus_phase,energy_plus_phase,mass_minus_phase,mass_plus_phase,energy_minus_resolved,energy_plus_resolved,mass_minus_resolved,mass_plus_resolved,base_pressure_pa,energy_derivative_scale_growth,mass_derivative_scale_growth,phase_or_envelope_switch,nonsmooth_evidence",
        };
        foreach (var item in diagnostics)
        {
            AppendThermodynamicRows(thermoRows, item.IntervalIndex, "h9-hysteresis-candidate", item.CandidateSmoothness);
            AppendThermodynamicRows(thermoRows, item.IntervalIndex, "explicit-end", item.ExplicitEndSmoothness);
        }
        File.WriteAllLines(Path.Combine(directory, "05-all-thermodynamic-node-probes.csv"), thermoRows, Utf8WithoutBom);

        var inverseRows = new List<string>
        {
            "interval,state,node,selected_branch,selected_phase,saturated_root_available,superheated_root_available,multiple_phase_roots,coarse_saturated,boundary_saturated,coarse_superheated,boundary_superheated,late_boundary_saturated_shadowed",
        };
        var candidateRows = new List<string>
        {
            "interval,state,node,branch,attempt_order,root_found,phase,pressure_pa,temperature_k,vapor_quality",
        };
        foreach (var item in diagnostics)
        {
            AppendInverseRows(inverseRows, candidateRows, item.IntervalIndex, "h9-hysteresis-candidate", item.InverseDiagnostics);
            AppendInverseRows(inverseRows, candidateRows, item.IntervalIndex, "explicit-end", item.ExplicitEndInverseDiagnostics);
        }
        File.WriteAllLines(Path.Combine(directory, "06-all-node-inverse-branch-selection.csv"), inverseRows, Utf8WithoutBom);
        File.WriteAllLines(Path.Combine(directory, "07-all-node-inverse-branch-candidates.csv"), candidateRows, Utf8WithoutBom);

        var target = Assert.Single(diagnostics, static item => item.IntervalIndex == TargetInterval);
        var dominantMass = target.NodeResiduals.MaxBy(static item => Math.Abs(item.MassResidualKilogramsPerSecond))
            ?? throw new InvalidOperationException("H.15 target residual set is empty.");
        var dominantEnergy = target.NodeResiduals.MaxBy(static item => Math.Abs(item.EnergyResidualWatts))
            ?? throw new InvalidOperationException("H.15 target residual set is empty.");
        var targetSwitchingNodes = target.CandidateSmoothness.ThermodynamicNodes
            .Where(static item => item.PhaseOrEnvelopeSwitchObserved)
            .Select(static item => item.NodeId)
            .OrderBy(static id => id, StringComparer.Ordinal)
            .ToArray();
        var targetNonSmoothNodes = target.CandidateSmoothness.ThermodynamicNodes
            .Where(static item => item.NonSmoothEvidence)
            .Select(static item => item.NodeId)
            .OrderBy(static id => id, StringComparer.Ordinal)
            .ToArray();
        var targetSwitchingPaths = target.CandidateSmoothness.HydraulicPaths
            .Where(static item => item.BranchSwitchObserved)
            .Select(static item => $"{item.ComponentKind}:{item.ComponentId}")
            .OrderBy(static id => id, StringComparer.Ordinal)
            .ToArray();
        var targetNonSmoothPaths = target.CandidateSmoothness.HydraulicPaths
            .Where(static item => item.NonSmoothEvidence)
            .Select(static item => $"{item.ComponentKind}:{item.ComponentId}")
            .OrderBy(static id => id, StringComparer.Ordinal)
            .ToArray();
        var overlapNodes = target.InverseDiagnostics
            .Where(static item => item.MultiplePhaseRootsAvailable)
            .Select(static item => item.NodeId)
            .OrderBy(static id => id, StringComparer.Ordinal)
            .ToArray();
        var lateShadowNodes = target.InverseDiagnostics
            .Where(static item => item.LateBoundarySaturatedShadowedByEarlierSuperheated)
            .Select(static item => item.NodeId)
            .OrderBy(static id => id, StringComparer.Ordinal)
            .ToArray();
        var explicitOverlapNodes = target.ExplicitEndInverseDiagnostics
            .Where(static item => item.MultiplePhaseRootsAvailable)
            .Select(static item => item.NodeId)
            .OrderBy(static id => id, StringComparer.Ordinal)
            .ToArray();
        var explicitLateShadowNodes = target.ExplicitEndInverseDiagnostics
            .Where(static item => item.LateBoundarySaturatedShadowedByEarlierSuperheated)
            .Select(static item => item.NodeId)
            .OrderBy(static id => id, StringComparer.Ordinal)
            .ToArray();

        var switchingEvidence = target.CandidateSmoothness.HydraulicBranchSwitchCount > 0
            || target.CandidateSmoothness.ThermodynamicPhaseSwitchCount > 0;
        var nonSmoothEvidence = target.CandidateSmoothness.HydraulicNonSmoothEvidenceCount > 0
            || target.CandidateSmoothness.ThermodynamicNonSmoothEvidenceCount > 0;
        var inverseBranchEvidence = lateShadowNodes.Except(explicitLateShadowNodes, StringComparer.Ordinal).Any();
        var diagnosticPasses = deterministicRepeat
            && target.Result.LineSearchExhausted
            && !target.Result.Converged
            && target.BranchOverrideCount == 0
            && target.HysteresisReleaseCount == 0;
        var recommendation = switchingEvidence || nonSmoothEvidence || inverseBranchEvidence
            ? "H.15 recommendation: interval 723 exposes localized switching/non-smooth/inverse-branch evidence outside the already targeted steam/stop-out continuity mechanism; keep production explicit and localize the reported nodes/paths before changing any solver or hysteresis policy."
            : "H.15 recommendation: interval 723 does not expose local switching/non-smooth or inverse-branch-selection evidence under the H.10-H.12 probes; keep production explicit and proceed to fixed-point existence/residual-floor and basin-of-attraction analysis for this interval.";

        var summary = new[]
        {
            "================================================================================",
            "M10.9.4.1-H.15 EXTENDED TRIGGER 723 ROOT-CAUSE DIAGNOSIS SUMMARY",
            "================================================================================",
            "=== 01-current-v2-extended-trigger-723-root-cause ===",
            "Shadow-only diagnosis of the sole H.14 broader-qualification failure at interval 723. The validated H.9 corrector and H.13 bounded thermodynamic branch-continuity policy remain unchanged; production stays explicit and no candidate is committed.",
            FormattableString.Invariant($"production-shadow-steps={IntervalCount}; frozen-trigger=P060-F040; triggered-events-through-724={triggers.Count}; H4-primary-converged={triggers.Count(static item => item.PrimaryResult.Converged)}/{triggers.Count}; target-interval={TargetInterval}; neighborhood=721|722|723|724;"),
            FormattableString.Invariant($"target H4-primary-converged=False; H9+bounded-hysteresis-converged={target.Result.Converged}; line-search-exhausted={target.Result.LineSearchExhausted}; pressure-residual={target.Result.MaximumRelativePressureFixedPointResidual:G17}; flow-residual={target.Result.MaximumAbsoluteFlowFixedPointResidualKilogramsPerSecond:G17} kg/s; normalized-merit={target.Result.NormalizedMeritResidual:G17}; branch-overrides={target.BranchOverrideCount}; hysteresis-releases={target.HysteresisReleaseCount}; previous-phase-holds={target.PreviousPhaseHoldCount};"),
            FormattableString.Invariant($"target-candidate hydraulic-branch-switches={target.CandidateSmoothness.HydraulicBranchSwitchCount}; hydraulic-nonsmooth-paths={target.CandidateSmoothness.HydraulicNonSmoothEvidenceCount}; thermodynamic-phase-or-envelope-switches={target.CandidateSmoothness.ThermodynamicPhaseSwitchCount}; thermodynamic-nonsmooth-nodes={target.CandidateSmoothness.ThermodynamicNonSmoothEvidenceCount}; max-hydraulic-derivative-scale-growth={target.CandidateSmoothness.MaximumHydraulicDerivativeScaleGrowth:G17}; max-hydraulic-one-sided-slope-asymmetry={target.CandidateSmoothness.MaximumHydraulicOneSidedSlopeAsymmetry:G17}; max-thermodynamic-derivative-scale-growth={target.CandidateSmoothness.MaximumThermodynamicDerivativeScaleGrowth:G17};"),
            FormattableString.Invariant($"explicit-end hydraulic-branch-switches={target.ExplicitEndSmoothness.HydraulicBranchSwitchCount}; hydraulic-nonsmooth-paths={target.ExplicitEndSmoothness.HydraulicNonSmoothEvidenceCount}; thermodynamic-phase-or-envelope-switches={target.ExplicitEndSmoothness.ThermodynamicPhaseSwitchCount}; thermodynamic-nonsmooth-nodes={target.ExplicitEndSmoothness.ThermodynamicNonSmoothEvidenceCount};"),
            FormattableString.Invariant($"dominant-node-residuals mass={dominantMass.NodeId}:{dominantMass.MassResidualKilogramsPerSecond:G17} kg/s; energy={dominantEnergy.NodeId}:{dominantEnergy.EnergyResidualWatts:G17} W; switching-paths={JoinOrNone(targetSwitchingPaths)}; nonsmooth-paths={JoinOrNone(targetNonSmoothPaths)}; switching-thermodynamic-nodes={JoinOrNone(targetSwitchingNodes)}; nonsmooth-thermodynamic-nodes={JoinOrNone(targetNonSmoothNodes)};"),
            FormattableString.Invariant($"inverse-map candidate-overlapping-root-nodes={JoinOrNone(overlapNodes)}; candidate-late-boundary-saturated-shadow-nodes={JoinOrNone(lateShadowNodes)}; explicit-overlapping-root-nodes={JoinOrNone(explicitOverlapNodes)}; explicit-late-boundary-saturated-shadow-nodes={JoinOrNone(explicitLateShadowNodes)}; switching-evidence-found={switchingEvidence}; non-smooth-evidence-found={nonSmoothEvidence}; inverse-branch-evidence-found={inverseBranchEvidence}; deterministic-repeat={deterministicRepeat}; extended-trigger-723-diagnostic-passes={diagnosticPasses};"),
            "bounded-hysteresis-policy-changed=False; production-resolve-order-changed=False; production-hybrid-active=False; production-fixed-step=10.000 ms; shadow-candidates-committed=False; H9-corrector-replaced=False; plant-network-orchestrator-routing-changed=False; trigger-retuning=False; physical-coefficient-retuning=False; hidden-flow-filtering=False;",
            recommendation,
            FormattableString.Invariant($"Detailed CSV files: \"{directory}\""),
        };
        File.WriteAllLines(Path.Combine(directory, "01-current-v2-extended-trigger-723-root-cause.summary.txt"), summary, Utf8WithoutBom);

        _ = productionThermodynamics;
    }


    private static void AppendInverseRows(
        ICollection<string> inverseRows,
        ICollection<string> candidateRows,
        int intervalIndex,
        string state,
        IReadOnlyList<WaterSteamInverseBranchSelectionDiagnostic> diagnostics)
    {
        foreach (var inverse in diagnostics)
        {
            inverseRows.Add(FormattableString.Invariant(
                $"{intervalIndex},{state},{inverse.NodeId},{inverse.ProductionSelectedBranch},{inverse.ProductionSelectedPhase},{inverse.SaturatedRootAvailable},{inverse.SuperheatedRootAvailable},{inverse.MultiplePhaseRootsAvailable},{inverse.CoarseSaturatedRootFound},{inverse.BoundaryAwareSaturatedRootFound},{inverse.CoarseSuperheatedRootFound},{inverse.BoundaryAwareSuperheatedRootFound},{inverse.LateBoundarySaturatedShadowedByEarlierSuperheated}"));
            foreach (var candidate in inverse.Candidates)
            {
                var quality = candidate.VaporQuality.HasValue
                    ? candidate.VaporQuality.Value.ToString("G17", CultureInfo.InvariantCulture)
                    : string.Empty;
                candidateRows.Add(FormattableString.Invariant(
                    $"{intervalIndex},{state},{inverse.NodeId},{candidate.Branch},{candidate.AttemptOrder},{candidate.RootFound},{candidate.Phase},{candidate.PressurePascals:G17},{candidate.TemperatureKelvins:G17},{quality}"));
            }
        }
    }

    private static void AppendPathRows(
        ICollection<string> rows,
        int intervalIndex,
        string state,
        HydraulicMapSmoothnessReport report)
    {
        foreach (var item in report.HydraulicPaths)
        {
            rows.Add(FormattableString.Invariant(
                $"{intervalIndex},{state},{item.ComponentKind},{item.ComponentId},{item.FromNodeId},{item.ToNodeId},{item.BaseBranch},{item.CoarseMinusBranch},{item.CoarsePlusBranch},{item.FineMinusBranch},{item.FinePlusBranch},{item.BaseDrivingPressurePascals:G17},{item.BaseMassFlowKilogramsPerSecond:G17},{item.DerivativeScaleGrowth:G17},{item.OneSidedSlopeAsymmetry:G17},{item.BranchSwitchObserved},{item.NonSmoothEvidence}"));
        }
    }

    private static void AppendThermodynamicRows(
        ICollection<string> rows,
        int intervalIndex,
        string state,
        HydraulicMapSmoothnessReport report)
    {
        foreach (var item in report.ThermodynamicNodes)
        {
            rows.Add(FormattableString.Invariant(
                $"{intervalIndex},{state},{item.NodeId},{item.BasePhase},{item.EnergyMinusPhase},{item.EnergyPlusPhase},{item.MassMinusPhase},{item.MassPlusPhase},{item.EnergyMinusResolved},{item.EnergyPlusResolved},{item.MassMinusResolved},{item.MassPlusResolved},{item.BasePressurePascals:G17},{item.EnergyDerivativeScaleGrowth:G17},{item.MassDerivativeScaleGrowth:G17},{item.PhaseOrEnvelopeSwitchObserved},{item.NonSmoothEvidence}"));
        }
    }

    private static string JoinOrNone(IReadOnlyList<string> values)
        => values.Count == 0 ? "none" : string.Join("|", values);

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

    private sealed record BaselineTriggerEvent(int IntervalIndex, HybridSemiImplicitHydraulicGateStepResult PrimaryResult);

    private sealed record NodeResidual(
        string NodeId,
        double MassResidualKilogramsPerSecond,
        double EnergyResidualWatts);

    private sealed record NeighborhoodDiagnostic(
        int IntervalIndex,
        JacobianHydraulicCorrectorStepResult Result,
        int BranchOverrideCount,
        int PreviousPhaseHoldCount,
        int HysteresisReleaseCount,
        HydraulicMapSmoothnessReport CandidateSmoothness,
        HydraulicMapSmoothnessReport ExplicitEndSmoothness,
        IReadOnlyList<NodeResidual> NodeResiduals,
        IReadOnlyList<WaterSteamInverseBranchSelectionDiagnostic> InverseDiagnostics,
        IReadOnlyList<WaterSteamInverseBranchSelectionDiagnostic> ExplicitEndInverseDiagnostics);
}
