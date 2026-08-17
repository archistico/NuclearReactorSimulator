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
/// M10.9.4.1-H.10 shadow-only diagnosis of branch switching and local non-smoothness around the two
/// persistent H.7-H.9 nonlinear-corrector failures. No new corrector is introduced and no shadow state
/// is committed. The exact 500-step explicit trajectory and frozen P060/F040 trigger set are reused.
/// </summary>
public sealed class HydraulicMapSwitchingNonSmoothnessAuditTests
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
    [Trait("Category", "HydraulicMapSwitchingNonSmoothnessAudit")]
    public void PersistentH9Failures_AreDiagnosedForSwitchingAndScaleSensitiveNonSmoothness()
    {
        var thermodynamics = new SimplifiedWaterSteamThermodynamicModel();
        var prototype = new SemiImplicitHydraulicPrototypeSolver(thermodynamics);
        var gate = new HybridSemiImplicitHydraulicGateSolver(thermodynamics);
        var h7Solver = new ResidualBacktrackingHydraulicCorrectorSolver(thermodynamics);
        var h8Solver = new AndersonHydraulicCorrectorSolver(thermodynamics);
        var h9Solver = new JacobianHydraulicCorrectorSolver(thermodynamics);
        var analyzer = new HydraulicMapSmoothnessAnalyzer(thermodynamics);
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
            return new H9DiagnosticEvent(interval.Index, result);
        }).ToArray();

        Assert.Equal(5, h9Events.Count(static item => item.Result.Converged));
        Assert.Equal(2, h9Events.Count(static item => item.Result.LineSearchExhausted));
        var persistentFailures = h9Events.Where(static item => !item.Result.Converged).ToArray();
        Assert.Equal(2, persistentFailures.Length);

        var diagnostics = persistentFailures.Select(item =>
        {
            var interval = intervals[item.IntervalIndex];
            return new DiagnosticEvent(
                item.IntervalIndex,
                item.Result.MaximumRelativePressureFixedPointResidual,
                item.Result.MaximumAbsoluteFlowFixedPointResidualKilogramsPerSecond,
                item.Result.NormalizedMeritResidual,
                analyzer.Analyze(item.Result.CandidateState),
                analyzer.Analyze(interval.End));
        }).ToArray();
        var repeatDiagnostics = persistentFailures.Select(item =>
        {
            var interval = intervals[item.IntervalIndex];
            return new DiagnosticEvent(
                item.IntervalIndex,
                item.Result.MaximumRelativePressureFixedPointResidual,
                item.Result.MaximumAbsoluteFlowFixedPointResidualKilogramsPerSecond,
                item.Result.NormalizedMeritResidual,
                analyzer.Analyze(item.Result.CandidateState),
                analyzer.Analyze(interval.End));
        }).ToArray();

        var deterministicRepeat = DiagnosticFingerprint(diagnostics) == DiagnosticFingerprint(repeatDiagnostics);
        Assert.True(deterministicRepeat, "H.10 local smoothness evidence was not exactly deterministic.");
        Assert.All(diagnostics, static item => Assert.NotEmpty(item.H9Candidate.HydraulicPaths));
        Assert.All(diagnostics, static item => Assert.NotEmpty(item.H9Candidate.ThermodynamicNodes));

        WriteAuditReports(
            baseline.Count,
            h6Converged,
            h7Converged,
            h8Converged,
            diagnostics,
            deterministicRepeat);
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
            Assert.False(presentation.AnyTripActive, $"Unexpected reference trip at H.10 interval {index + 1}.");
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

    private static string DiagnosticFingerprint(IReadOnlyList<DiagnosticEvent> diagnostics)
        => string.Join(
            "||",
            diagnostics.Select(item => string.Join(
                "|",
                new[]
                {
                    item.IntervalIndex.ToString(CultureInfo.InvariantCulture),
                    ReportFingerprint(item.H9Candidate),
                    ReportFingerprint(item.ExplicitEnd),
                })));

    private static string ReportFingerprint(HydraulicMapSmoothnessReport report)
    {
        var paths = string.Join(
            ";",
            report.HydraulicPaths.Select(item => FormattableString.Invariant(
                $"{item.ComponentKind}:{item.ComponentId}:{item.BaseBranch}:{item.CoarseMinusBranch}:{item.CoarsePlusBranch}:{item.FineMinusBranch}:{item.FinePlusBranch}:{item.BaseDrivingPressurePascals:G17}:{item.CoarsePressureProbePascals:G17}:{item.BaseMassFlowKilogramsPerSecond:G17}:{item.CoarseMinusMassFlowKilogramsPerSecond:G17}:{item.CoarsePlusMassFlowKilogramsPerSecond:G17}:{item.FineMinusMassFlowKilogramsPerSecond:G17}:{item.FinePlusMassFlowKilogramsPerSecond:G17}:{item.DerivativeScaleGrowth:G17}:{item.OneSidedSlopeAsymmetry:G17}:{item.BranchSwitchObserved}:{item.NonSmoothEvidence}")));
        var nodes = string.Join(
            ";",
            report.ThermodynamicNodes.Select(item => FormattableString.Invariant(
                $"{item.NodeId}:{item.BasePhase}:{item.EnergyMinusPhase}:{item.EnergyPlusPhase}:{item.MassMinusPhase}:{item.MassPlusPhase}:{item.EnergyMinusResolved}:{item.EnergyPlusResolved}:{item.MassMinusResolved}:{item.MassPlusResolved}:{item.BasePressurePascals:G17}:{item.EnergyDerivativeScaleGrowth:G17}:{item.MassDerivativeScaleGrowth:G17}:{item.PhaseOrEnvelopeSwitchObserved}:{item.NonSmoothEvidence}")));
        return paths + "#" + nodes;
    }

    private static void WriteAuditReports(
        int triggeredEvents,
        int h6Converged,
        int h7Converged,
        int h8Converged,
        IReadOnlyList<DiagnosticEvent> diagnostics,
        bool deterministicRepeat)
    {
        var root = FindRepositoryRoot();
        var directory = Path.Combine(root, "artifacts", "h10-hydraulic-map-switching-nonsmoothness");
        Directory.CreateDirectory(directory);
        var options = HydraulicMapSmoothnessProbeOptions.H10AuditDefault;

        var eventCsv = new List<string>
        {
            "interval,h9_pressure_residual,h9_flow_residual_kg_s,h9_normalized_merit,h9_hydraulic_branch_switches,h9_hydraulic_nonsmooth_paths,h9_thermo_phase_or_envelope_switches,h9_thermo_nonsmooth_nodes,h9_max_hydraulic_derivative_scale_growth,h9_max_hydraulic_slope_asymmetry,h9_max_thermo_derivative_scale_growth,explicit_hydraulic_branch_switches,explicit_hydraulic_nonsmooth_paths,explicit_thermo_phase_or_envelope_switches,explicit_thermo_nonsmooth_nodes",
        };
        foreach (var item in diagnostics)
        {
            eventCsv.Add(FormattableString.Invariant(
                $"{item.IntervalIndex},{item.H9PressureResidual:G17},{item.H9FlowResidualKilogramsPerSecond:G17},{item.H9NormalizedMerit:G17},{item.H9Candidate.HydraulicBranchSwitchCount},{item.H9Candidate.HydraulicNonSmoothEvidenceCount},{item.H9Candidate.ThermodynamicPhaseSwitchCount},{item.H9Candidate.ThermodynamicNonSmoothEvidenceCount},{item.H9Candidate.MaximumHydraulicDerivativeScaleGrowth:G17},{item.H9Candidate.MaximumHydraulicOneSidedSlopeAsymmetry:G17},{item.H9Candidate.MaximumThermodynamicDerivativeScaleGrowth:G17},{item.ExplicitEnd.HydraulicBranchSwitchCount},{item.ExplicitEnd.HydraulicNonSmoothEvidenceCount},{item.ExplicitEnd.ThermodynamicPhaseSwitchCount},{item.ExplicitEnd.ThermodynamicNonSmoothEvidenceCount}"));
        }

        File.WriteAllLines(
            Path.Combine(directory, "02-persistent-event-overview.csv"),
            eventCsv,
            Utf8WithoutBom);

        var pathCsv = new List<string>
        {
            "interval,state,component_kind,component_id,from_node,to_node,base_branch,coarse_minus_branch,coarse_plus_branch,fine_minus_branch,fine_plus_branch,base_driving_pressure_pa,coarse_probe_pa,base_flow_kg_s,coarse_minus_flow_kg_s,coarse_plus_flow_kg_s,fine_minus_flow_kg_s,fine_plus_flow_kg_s,coarse_central_slope,fine_central_slope,derivative_scale_growth,one_sided_slope_asymmetry,branch_switch,nonsmooth_evidence",
        };
        foreach (var item in diagnostics)
        {
            AppendPathRows(pathCsv, item.IntervalIndex, "h9-candidate", item.H9Candidate);
            AppendPathRows(pathCsv, item.IntervalIndex, "explicit-end", item.ExplicitEnd);
        }

        File.WriteAllLines(
            Path.Combine(directory, "03-hydraulic-path-local-probes.csv"),
            pathCsv,
            Utf8WithoutBom);

        var thermoCsv = new List<string>
        {
            "interval,state,node,base_phase,energy_minus_phase,energy_plus_phase,mass_minus_phase,mass_plus_phase,energy_minus_resolved,energy_plus_resolved,mass_minus_resolved,mass_plus_resolved,base_pressure_pa,energy_derivative_scale_growth,mass_derivative_scale_growth,phase_or_envelope_switch,nonsmooth_evidence",
        };
        foreach (var item in diagnostics)
        {
            AppendThermodynamicRows(thermoCsv, item.IntervalIndex, "h9-candidate", item.H9Candidate);
            AppendThermodynamicRows(thermoCsv, item.IntervalIndex, "explicit-end", item.ExplicitEnd);
        }

        File.WriteAllLines(
            Path.Combine(directory, "04-thermodynamic-node-local-probes.csv"),
            thermoCsv,
            Utf8WithoutBom);

        var h9HydraulicSwitches = diagnostics.Sum(static item => item.H9Candidate.HydraulicBranchSwitchCount);
        var h9HydraulicNonSmooth = diagnostics.Sum(static item => item.H9Candidate.HydraulicNonSmoothEvidenceCount);
        var h9ThermoSwitches = diagnostics.Sum(static item => item.H9Candidate.ThermodynamicPhaseSwitchCount);
        var h9ThermoNonSmooth = diagnostics.Sum(static item => item.H9Candidate.ThermodynamicNonSmoothEvidenceCount);
        var explicitHydraulicSwitches = diagnostics.Sum(static item => item.ExplicitEnd.HydraulicBranchSwitchCount);
        var explicitThermoSwitches = diagnostics.Sum(static item => item.ExplicitEnd.ThermodynamicPhaseSwitchCount);
        var maxHydraulicGrowth = diagnostics.Max(static item => item.H9Candidate.MaximumHydraulicDerivativeScaleGrowth);
        var maxHydraulicAsymmetry = diagnostics.Max(static item => item.H9Candidate.MaximumHydraulicOneSidedSlopeAsymmetry);
        var maxThermoGrowth = diagnostics.Max(static item => item.H9Candidate.MaximumThermodynamicDerivativeScaleGrowth);
        var switchingEvidenceFound = h9HydraulicSwitches > 0 || h9ThermoSwitches > 0;
        var nonSmoothEvidenceFound = h9HydraulicNonSmooth > 0 || h9ThermoNonSmooth > 0;
        var diagnosticPasses = deterministicRepeat && diagnostics.Count == 2;
        var recommendation = switchingEvidenceFound || nonSmoothEvidenceFound
            ? "H.10 recommendation: local switching/non-smooth evidence is present around the persistent H.9 failures; keep production explicit and localize the responsible paths/nodes before selecting an active-set or semi-smooth formulation."
            : "H.10 recommendation: the selected local probes do not expose switching/non-smoothness around the persistent H.9 failures; keep production explicit and test fixed-point existence/basin structure before adding solver complexity.";
        var summary = new[]
        {
            "================================================================================",
            "M10.9.4.1-H.10 HYDRAULIC MAP SWITCHING & NON-SMOOTHNESS DIAGNOSIS SUMMARY",
            "================================================================================",
            "=== 01-current-v2-persistent-failure-local-map-diagnosis ===",
            "Shadow-only local diagnosis around the two persistent H.7-H.9 nonlinear-corrector failures over the frozen P060/F040 evidence set; no new corrector is introduced and production remains explicit.",
            FormattableString.Invariant($"production-shadow-steps={IntervalCount}; frozen-trigger=P060-F040; triggered-events={triggeredEvents}; H4-primary-converged=5/{triggeredEvents}; H6-rescue-converged={h6Converged}/{triggeredEvents}; H7-residual-backtracking-converged={h7Converged}/{triggeredEvents}; H8-Anderson-converged={h8Converged}/{triggeredEvents}; H9-Jacobian-Newton-converged=5/{triggeredEvents}; persistent-H9-failures={diagnostics.Count};"),
            FormattableString.Invariant($"H10-diagnostic=two-scale law-local pressure probes+conserved thermodynamic inventory probes; relative-pressure-probe={options.RelativePressureProbe:G17}; relative-inventory-probe={options.RelativeInventoryProbe:G17}; fine-probe-factor={options.FineProbeFactor:G17}; derivative-scale-growth-threshold={options.DerivativeScaleGrowthThreshold:G17}; one-sided-slope-asymmetry-threshold={options.OneSidedSlopeAsymmetryThreshold:G17};"),
            FormattableString.Invariant($"H9-candidate hydraulic-branch-switches={h9HydraulicSwitches}; hydraulic-nonsmooth-paths={h9HydraulicNonSmooth}; thermodynamic-phase-or-envelope-switches={h9ThermoSwitches}; thermodynamic-nonsmooth-nodes={h9ThermoNonSmooth}; max-hydraulic-derivative-scale-growth={maxHydraulicGrowth:0.000000000}; max-hydraulic-one-sided-slope-asymmetry={maxHydraulicAsymmetry:0.000000000}; max-thermodynamic-derivative-scale-growth={maxThermoGrowth:0.000000000};"),
            FormattableString.Invariant($"explicit-end control hydraulic-branch-switches={explicitHydraulicSwitches}; thermodynamic-phase-or-envelope-switches={explicitThermoSwitches}; switching-evidence-found={switchingEvidenceFound}; non-smooth-evidence-found={nonSmoothEvidenceFound}; deterministic-repeat={deterministicRepeat}; switching-nonsmoothness-diagnostic-passes={diagnosticPasses};"),
            "production-hybrid-active=False; production-fixed-step=10.000 ms; shadow-candidates-committed=False; H9-corrector-replaced=False; plant-network-orchestrator-routing-changed=False; trigger-retuning=False; physical-coefficient-retuning=False; hidden-flow-filtering=False;",
            recommendation,
            FormattableString.Invariant($"Detailed CSV files: \"{directory}\""),
        };
        File.WriteAllLines(
            Path.Combine(directory, "01-current-v2-hydraulic-map-switching-nonsmoothness.summary.txt"),
            summary,
            Utf8WithoutBom);
    }

    private static void AppendPathRows(
        ICollection<string> rows,
        int intervalIndex,
        string stateLabel,
        HydraulicMapSmoothnessReport report)
    {
        foreach (var item in report.HydraulicPaths)
        {
            rows.Add(FormattableString.Invariant(
                $"{intervalIndex},{stateLabel},{item.ComponentKind},{item.ComponentId},{item.FromNodeId},{item.ToNodeId},{item.BaseBranch},{item.CoarseMinusBranch},{item.CoarsePlusBranch},{item.FineMinusBranch},{item.FinePlusBranch},{item.BaseDrivingPressurePascals:G17},{item.CoarsePressureProbePascals:G17},{item.BaseMassFlowKilogramsPerSecond:G17},{item.CoarseMinusMassFlowKilogramsPerSecond:G17},{item.CoarsePlusMassFlowKilogramsPerSecond:G17},{item.FineMinusMassFlowKilogramsPerSecond:G17},{item.FinePlusMassFlowKilogramsPerSecond:G17},{item.CoarseCentralSlopeKilogramsPerSecondPerPascal:G17},{item.FineCentralSlopeKilogramsPerSecondPerPascal:G17},{item.DerivativeScaleGrowth:G17},{item.OneSidedSlopeAsymmetry:G17},{item.BranchSwitchObserved},{item.NonSmoothEvidence}"));
        }
    }

    private static void AppendThermodynamicRows(
        ICollection<string> rows,
        int intervalIndex,
        string stateLabel,
        HydraulicMapSmoothnessReport report)
    {
        foreach (var item in report.ThermodynamicNodes)
        {
            rows.Add(FormattableString.Invariant(
                $"{intervalIndex},{stateLabel},{item.NodeId},{item.BasePhase},{item.EnergyMinusPhase},{item.EnergyPlusPhase},{item.MassMinusPhase},{item.MassPlusPhase},{item.EnergyMinusResolved},{item.EnergyPlusResolved},{item.MassMinusResolved},{item.MassPlusResolved},{item.BasePressurePascals:G17},{item.EnergyDerivativeScaleGrowth:G17},{item.MassDerivativeScaleGrowth:G17},{item.PhaseOrEnvelopeSwitchObserved},{item.NonSmoothEvidence}"));
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
        PlantState End,
        IReadOnlyDictionary<string, FluidNodeBalance> FrozenNonHydraulicBalances);

    private sealed record BaselineTriggerEvent(int IntervalIndex, HybridSemiImplicitHydraulicGateStepResult PrimaryResult);

    private sealed record H9DiagnosticEvent(int IntervalIndex, JacobianHydraulicCorrectorStepResult Result);

    private sealed record DiagnosticEvent(
        int IntervalIndex,
        double H9PressureResidual,
        double H9FlowResidualKilogramsPerSecond,
        double H9NormalizedMerit,
        HydraulicMapSmoothnessReport H9Candidate,
        HydraulicMapSmoothnessReport ExplicitEnd);
}
