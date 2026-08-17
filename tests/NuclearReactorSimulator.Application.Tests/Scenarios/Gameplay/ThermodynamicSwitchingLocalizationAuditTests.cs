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
/// M10.9.4.1-H.11 shadow-only localization of the thermodynamic phase/envelope switching identified by H.10.
/// The audit reuses the exact frozen P060/F040 evidence set, introduces no new nonlinear corrector and commits no state.
/// </summary>
public sealed class ThermodynamicSwitchingLocalizationAuditTests
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
    [Trait("Category", "ThermodynamicSwitchingLocalizationAudit")]
    public void PersistentH9Failures_LocalizeTheH10ThermodynamicBoundaries()
    {
        var thermodynamics = new SimplifiedWaterSteamThermodynamicModel();
        var prototype = new SemiImplicitHydraulicPrototypeSolver(thermodynamics);
        var gate = new HybridSemiImplicitHydraulicGateSolver(thermodynamics);
        var h7Solver = new ResidualBacktrackingHydraulicCorrectorSolver(thermodynamics);
        var h8Solver = new AndersonHydraulicCorrectorSolver(thermodynamics);
        var h9Solver = new JacobianHydraulicCorrectorSolver(thermodynamics);
        var h10Analyzer = new HydraulicMapSmoothnessAnalyzer(thermodynamics);
        var h11Analyzer = new ThermodynamicSwitchingLocalizationAnalyzer(thermodynamics, thermodynamics);
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
            .Select(item => BuildDiagnostic(item, intervals[item.IntervalIndex], prototype, h10Analyzer, h11Analyzer))
            .ToArray();
        var repeatDiagnostics = h9Events
            .Where(static item => !item.Result.Converged)
            .Select(item => BuildDiagnostic(item, intervals[item.IntervalIndex], prototype, h10Analyzer, h11Analyzer))
            .ToArray();

        Assert.Equal(2, diagnostics.Length);
        Assert.Equal(2, diagnostics.Sum(static item => item.H10Candidate.ThermodynamicPhaseSwitchCount));
        Assert.Equal(0, diagnostics.Sum(static item => item.H10ExplicitEnd.ThermodynamicPhaseSwitchCount));
        Assert.Equal(2, diagnostics.Sum(static item => item.H11Candidate.LocalizedNodeCount));
        Assert.All(diagnostics, static item => Assert.Empty(item.H11ExplicitEnd.Nodes));
        Assert.All(
            diagnostics.SelectMany(static item => item.H11Candidate.Nodes),
            static node =>
            {
                Assert.NotEqual("none", node.CrossingAxis);
                Assert.NotEqual("unclassified", node.BoundaryClassification);
            });

        var deterministicRepeat = Fingerprint(diagnostics) == Fingerprint(repeatDiagnostics);
        Assert.True(deterministicRepeat, "H.11 thermodynamic boundary localization was not exactly deterministic.");

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
        ReferenceInterval interval,
        SemiImplicitHydraulicPrototypeSolver prototype,
        HydraulicMapSmoothnessAnalyzer h10Analyzer,
        ThermodynamicSwitchingLocalizationAnalyzer h11Analyzer)
    {
        var candidateSmoothness = h10Analyzer.Analyze(item.Result.CandidateState);
        var explicitSmoothness = h10Analyzer.Analyze(interval.End);
        var candidateLocalization = h11Analyzer.Analyze(item.Result.CandidateState, candidateSmoothness);
        var explicitLocalization = h11Analyzer.Analyze(interval.End, explicitSmoothness);
        var mappedEvaluation = prototype.Evaluate(item.Result.CandidateState);
        var balanceResiduals = candidateLocalization.Nodes
            .Select(node =>
            {
                var applied = item.Result.AppliedHydraulicBalances[node.NodeId];
                var mapped = mappedEvaluation.FluidNodeBalances[node.NodeId];
                var residual = mapped - applied;
                return new LocalBalanceResidual(
                    node.NodeId,
                    residual.NetMassFlowRate.KilogramsPerSecond,
                    residual.NetEnergyRate.Watts);
            })
            .OrderBy(static value => value.NodeId, StringComparer.Ordinal)
            .ToArray();

        return new DiagnosticEvent(
            item.IntervalIndex,
            item.Result.MaximumRelativePressureFixedPointResidual,
            item.Result.MaximumAbsoluteFlowFixedPointResidualKilogramsPerSecond,
            item.Result.NormalizedMeritResidual,
            candidateSmoothness,
            explicitSmoothness,
            candidateLocalization,
            explicitLocalization,
            balanceResiduals);
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
            Assert.False(presentation.AnyTripActive, $"Unexpected reference trip at H.11 interval {index + 1}.");
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

    private static string Fingerprint(IReadOnlyList<DiagnosticEvent> diagnostics)
        => string.Join(
            "||",
            diagnostics.Select(item => string.Join(
                "|",
                item.IntervalIndex,
                item.H9PressureResidual.ToString("G17", System.Globalization.CultureInfo.InvariantCulture),
                item.H9FlowResidualKilogramsPerSecond.ToString("G17", System.Globalization.CultureInfo.InvariantCulture),
                item.H11Candidate.LocalizedNodeCount,
                string.Join(";", item.H11Candidate.Nodes.Select(NodeFingerprint)),
                string.Join(";", item.BalanceResiduals.Select(static residual => FormattableString.Invariant($"{residual.NodeId}:{residual.MassRateResidualKilogramsPerSecond:G17}:{residual.EnergyRateResidualWatts:G17}"))))));

    private static string NodeFingerprint(ThermodynamicSwitchingNodeLocalization node)
        => FormattableString.Invariant(
            $"{node.NodeId}:{node.CrossingAxis}:{node.BoundaryClassification}:{node.Nominal.Phase}:{node.EnergyMinus.Phase}:{node.EnergyPlus.Phase}:{node.MassMinus.Phase}:{node.MassPlus.Phase}:{node.EnergyMinus.Resolved}:{node.EnergyPlus.Resolved}:{node.MassMinus.Resolved}:{node.MassPlus.Resolved}:{node.Nominal.RelativePressureDistanceFromSaturation:G17}");

    private static void WriteAuditReports(
        int triggeredEvents,
        int h6Converged,
        int h7Converged,
        int h8Converged,
        IReadOnlyList<DiagnosticEvent> diagnostics,
        bool deterministicRepeat)
    {
        var repositoryRoot = FindRepositoryRoot();
        var directory = Path.Combine(repositoryRoot, "artifacts", "h11-thermodynamic-switching-localization");
        Directory.CreateDirectory(directory);

        var nodeRows = new List<string>
        {
            "interval,node,crossing_axis,boundary_classification,phase_boundary,envelope_boundary,suggested_active_set,energy_probe_j,mass_probe_kg,local_mass_balance_residual_kg_s,local_energy_balance_residual_w",
        };
        var probeRows = new List<string>
        {
            "interval,node,probe,resolved,phase,mass_kg,internal_energy_j,specific_volume_m3_kg,specific_internal_energy_j_kg,pressure_pa,temperature_k,vapor_quality,saturation_reference_available,saturation_pressure_pa,relative_pressure_distance_from_saturation,saturated_liquid_u_j_kg,saturated_vapor_u_j_kg,distance_above_liquid_u_j_kg,distance_below_vapor_u_j_kg",
        };
        var eventRows = new List<string>
        {
            "interval,h9_pressure_residual,h9_flow_residual_kg_s,h9_merit,h10_thermo_switch_nodes,h11_localized_nodes,explicit_end_switch_nodes",
        };

        foreach (var item in diagnostics)
        {
            eventRows.Add(FormattableString.Invariant(
                $"{item.IntervalIndex},{item.H9PressureResidual:G17},{item.H9FlowResidualKilogramsPerSecond:G17},{item.H9NormalizedMerit:G17},{item.H10Candidate.ThermodynamicPhaseSwitchCount},{item.H11Candidate.LocalizedNodeCount},{item.H10ExplicitEnd.ThermodynamicPhaseSwitchCount}"));
            var residuals = item.BalanceResiduals.ToDictionary(static residual => residual.NodeId, StringComparer.Ordinal);
            foreach (var node in item.H11Candidate.Nodes)
            {
                var residual = residuals[node.NodeId];
                nodeRows.Add(FormattableString.Invariant(
                    $"{item.IntervalIndex},{node.NodeId},{node.CrossingAxis},{node.BoundaryClassification},{node.PhaseBoundaryObserved},{node.EnvelopeBoundaryObserved},{node.SuggestedActiveSet},{node.EnergyProbeJoules:G17},{node.MassProbeKilograms:G17},{residual.MassRateResidualKilogramsPerSecond:G17},{residual.EnergyRateResidualWatts:G17}"));
                AppendProbe(probeRows, item.IntervalIndex, node.NodeId, node.Nominal);
                AppendProbe(probeRows, item.IntervalIndex, node.NodeId, node.EnergyMinus);
                AppendProbe(probeRows, item.IntervalIndex, node.NodeId, node.EnergyPlus);
                AppendProbe(probeRows, item.IntervalIndex, node.NodeId, node.MassMinus);
                AppendProbe(probeRows, item.IntervalIndex, node.NodeId, node.MassPlus);
            }
        }

        File.WriteAllLines(Path.Combine(directory, "02-persistent-event-localization.csv"), eventRows, Utf8WithoutBom);
        File.WriteAllLines(Path.Combine(directory, "03-localized-thermodynamic-nodes.csv"), nodeRows, Utf8WithoutBom);
        File.WriteAllLines(Path.Combine(directory, "04-thermodynamic-boundary-probes.csv"), probeRows, Utf8WithoutBom);

        var localizedNodes = diagnostics.Sum(static item => item.H11Candidate.LocalizedNodeCount);
        var phaseNodes = diagnostics.Sum(static item => item.H11Candidate.PhaseBoundaryNodeCount);
        var envelopeNodes = diagnostics.Sum(static item => item.H11Candidate.EnvelopeBoundaryNodeCount);
        var distinctNodeIds = diagnostics
            .SelectMany(static item => item.H11Candidate.Nodes)
            .Select(static node => node.NodeId)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static id => id, StringComparer.Ordinal)
            .ToArray();
        var crossingAxes = diagnostics
            .SelectMany(static item => item.H11Candidate.Nodes)
            .Select(static node => node.CrossingAxis)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static value => value, StringComparer.Ordinal)
            .ToArray();
        var boundaryClasses = diagnostics
            .SelectMany(static item => item.H11Candidate.Nodes)
            .Select(static node => node.BoundaryClassification)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static value => value, StringComparer.Ordinal)
            .ToArray();
        var maxMassResidual = diagnostics
            .SelectMany(static item => item.BalanceResiduals)
            .Select(static residual => Math.Abs(residual.MassRateResidualKilogramsPerSecond))
            .DefaultIfEmpty(0d)
            .Max();
        var maxEnergyResidual = diagnostics
            .SelectMany(static item => item.BalanceResiduals)
            .Select(static residual => Math.Abs(residual.EnergyRateResidualWatts))
            .DefaultIfEmpty(0d)
            .Max();
        var localizedBoundaryIsConcrete = diagnostics
            .SelectMany(static item => item.H11Candidate.Nodes)
            .All(static node => !string.Equals(node.CrossingAxis, "none", StringComparison.Ordinal)
                && !string.Equals(node.BoundaryClassification, "unclassified", StringComparison.Ordinal));
        var localizationPasses = deterministicRepeat
            && diagnostics.Count == 2
            && localizedNodes == 2
            && localizedBoundaryIsConcrete
            && diagnostics.Sum(static item => item.H10ExplicitEnd.ThermodynamicPhaseSwitchCount) == 0;
        var recommendation = localizedNodes > 0
            ? "H.11 recommendation: thermodynamic switching has been localized to concrete nodes and perturbation axes; keep production explicit and use the localized boundary class to design a narrow active-set/semi-smooth shadow experiment rather than another global corrector."
            : "H.11 recommendation: H.10 switching evidence could not be localized by the selected conserved probes; keep production explicit and investigate fixed-point existence/residual-floor structure.";

        var summary = new[]
        {
            "================================================================================",
            "M10.9.4.1-H.11 THERMODYNAMIC SWITCHING LOCALIZATION & ACTIVE-SET DIAGNOSIS SUMMARY",
            "================================================================================",
            "=== 01-current-v2-thermodynamic-boundary-localization ===",
            "Shadow-only localization of the two thermodynamic phase/envelope switches identified by H.10 around the persistent H.9 failures; no active set is enforced, no new corrector is introduced and production remains explicit.",
            FormattableString.Invariant($"production-shadow-steps={IntervalCount}; frozen-trigger=P060-F040; triggered-events={triggeredEvents}; H4-primary-converged=5/{triggeredEvents}; H6-rescue-converged={h6Converged}/{triggeredEvents}; H7-residual-backtracking-converged={h7Converged}/{triggeredEvents}; H8-Anderson-converged={h8Converged}/{triggeredEvents}; H9-Jacobian-Newton-converged=5/{triggeredEvents}; persistent-H9-failures={diagnostics.Count};"),
            FormattableString.Invariant($"H10-thermodynamic-switch-nodes={diagnostics.Sum(static item => item.H10Candidate.ThermodynamicPhaseSwitchCount)}; explicit-end-switch-nodes={diagnostics.Sum(static item => item.H10ExplicitEnd.ThermodynamicPhaseSwitchCount)}; H11-localized-nodes={localizedNodes}; distinct-node-ids={string.Join("|", distinctNodeIds)}; crossing-axes={string.Join("|", crossingAxes)}; boundary-classes={string.Join("|", boundaryClasses)}; phase-boundary-nodes={phaseNodes}; envelope-boundary-nodes={envelopeNodes};"),
            FormattableString.Invariant($"max-local-hydraulic-balance-residuals mass={maxMassResidual:0.000000000} kg/s; energy={maxEnergyResidual:0.000000000} W; deterministic-repeat={deterministicRepeat}; thermodynamic-switching-localization-passes={localizationPasses};"),
            "active-set-enforced=False; semi-smooth-solver-introduced=False; production-hybrid-active=False; production-fixed-step=10.000 ms; shadow-candidates-committed=False; H9-corrector-replaced=False; plant-network-orchestrator-routing-changed=False; trigger-retuning=False; physical-coefficient-retuning=False; hidden-flow-filtering=False;",
            recommendation,
            FormattableString.Invariant($"Detailed CSV files: \"{directory}\""),
        };
        File.WriteAllLines(
            Path.Combine(directory, "01-current-v2-thermodynamic-switching-localization.summary.txt"),
            summary,
            Utf8WithoutBom);
    }

    private static void AppendProbe(
        ICollection<string> rows,
        int interval,
        string nodeId,
        ThermodynamicSwitchingProbePoint probe)
    {
        rows.Add(FormattableString.Invariant(
            $"{interval},{nodeId},{probe.Label},{probe.Resolved},{probe.Phase},{probe.MassKilograms:G17},{probe.InternalEnergyJoules:G17},{probe.SpecificVolumeCubicMetresPerKilogram:G17},{probe.SpecificInternalEnergyJoulesPerKilogram:G17},{probe.PressurePascals:G17},{probe.TemperatureKelvins:G17},{NullableDouble(probe.VaporQualityFraction)},{probe.SaturationReferenceAvailable},{probe.SaturationPressurePascals:G17},{probe.RelativePressureDistanceFromSaturation:G17},{probe.SaturatedLiquidInternalEnergyJoulesPerKilogram:G17},{probe.SaturatedVaporInternalEnergyJoulesPerKilogram:G17},{probe.DistanceAboveSaturatedLiquidEnergyJoulesPerKilogram:G17},{probe.DistanceBelowSaturatedVaporEnergyJoulesPerKilogram:G17}"));
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
        PlantState End,
        IReadOnlyDictionary<string, FluidNodeBalance> FrozenNonHydraulicBalances);

    private sealed record BaselineTriggerEvent(int IntervalIndex, HybridSemiImplicitHydraulicGateStepResult PrimaryResult);

    private sealed record H9Event(int IntervalIndex, JacobianHydraulicCorrectorStepResult Result);

    private sealed record LocalBalanceResidual(
        string NodeId,
        double MassRateResidualKilogramsPerSecond,
        double EnergyRateResidualWatts);

    private sealed record DiagnosticEvent(
        int IntervalIndex,
        double H9PressureResidual,
        double H9FlowResidualKilogramsPerSecond,
        double H9NormalizedMerit,
        HydraulicMapSmoothnessReport H10Candidate,
        HydraulicMapSmoothnessReport H10ExplicitEnd,
        ThermodynamicSwitchingLocalizationReport H11Candidate,
        ThermodynamicSwitchingLocalizationReport H11ExplicitEnd,
        IReadOnlyList<LocalBalanceResidual> BalanceResiduals);
}
