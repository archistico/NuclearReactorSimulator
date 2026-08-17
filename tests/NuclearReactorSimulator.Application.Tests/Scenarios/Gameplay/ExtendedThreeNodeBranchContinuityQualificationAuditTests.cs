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
/// M10.9.4.1-H.16 extended three-node shadow qualification of the validated H.13 bounded
/// previous-phase hysteresis policy after H.15 localized the H.14 interval-723 failure to header.
/// Production Resolve(), production integration, H.9 and the hysteresis limits remain unchanged.
/// </summary>
public sealed class ExtendedThreeNodeBranchContinuityQualificationAuditTests
{
    private static readonly TimeSpan Step = TimeSpan.FromMilliseconds(10d);
    private static readonly Encoding Utf8WithoutBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    private static readonly string[] H14TargetNodeIds = { "steam", "stop-out" };
    private static readonly string[] H16TargetNodeIds = { "steam", "stop-out", "header" };
    private const int H13WindowIntervalCount = 500;
    private const int BroaderIntervalCount = 2_000;
    private const double PressureTrigger = 0.060d;
    private const double FlowTriggerKilogramsPerSecond = 40d;
    private const double MaximumAlgorithmWorkRatio = 32d;

    private const double SteamMassKilograms = 3322.9485347676582d;
    private const double SteamEnergyJoules = 8238192716.5426521d;
    private const double SteamEnergyProbeJoules = 2059.5481791356628d;
    private const double StopOutMassKilograms = 3165.1742481741885d;
    private const double StopOutEnergyJoules = 7863528392.8413477d;
    private const double StopOutEnergyProbeJoules = 1965.8820982103368d;

    private static readonly SemiImplicitHydraulicPrototypeOptions H4Primary = new(
        maximumIterations: 72,
        relaxationFactor: 0.15d,
        relativePressureTolerance: 1e-5d,
        absoluteFlowToleranceKilogramsPerSecond: 1e-2d);

    [Fact(Explicit = true)]
    [Trait("Category", "ExtendedThreeNodeBranchContinuityQualificationAudit")]
    public void ThreeNodeBoundedHysteresis_RequalifiesTheExtendedShadowWindowWithoutRetuning()
    {
        var productionThermodynamics = new SimplifiedWaterSteamThermodynamicModel();
        var prototype = new SemiImplicitHydraulicPrototypeSolver(productionThermodynamics);
        var gate = new HybridSemiImplicitHydraulicGateSolver(productionThermodynamics);
        var reference = BuildReferenceTrajectory(prototype);
        var baseline = EvaluatePrimaryGate(reference, gate);
        var h13Baseline = baseline.Where(static item => item.IntervalIndex <= H13WindowIntervalCount).ToArray();
        var intervals = reference.ToDictionary(static item => item.Index);

        Assert.Equal(BroaderIntervalCount, reference.Count);
        Assert.Equal(15, baseline.Count);
        Assert.Equal(7, h13Baseline.Length);
        Assert.Equal(5, h13Baseline.Count(static item => item.PrimaryResult.Converged));

        var productionH13 = RunProductionH9(h13Baseline, intervals, productionThermodynamics);
        Assert.Equal(5, productionH13.Events.Count(static item => item.Result.Converged));
        Assert.Equal(2, productionH13.Events.Count(static item => item.Result.LineSearchExhausted));

        var h14Control = RunPolicy(
            baseline,
            intervals,
            productionThermodynamics,
            ThermodynamicBranchContinuityOptions.H13BoundedHysteresis,
            H14TargetNodeIds);
        Assert.Equal(14, h14Control.Events.Count(static item => item.Result.Converged));
        Assert.Equal(1, h14Control.Events.Count(static item => item.Result.LineSearchExhausted));
        var h14Interval723 = Assert.Single(h14Control.Events, static item => item.IntervalIndex == 723);
        Assert.False(h14Interval723.Result.Converged);
        Assert.True(h14Interval723.Result.LineSearchExhausted);
        Assert.DoesNotContain(h14Interval723.Decisions, static decision => decision.SelectionDiffersFromProduction);
        Assert.DoesNotContain(h14Interval723.Decisions, static decision =>
            string.Equals(decision.DecisionKind, "production-hysteresis-release", StringComparison.Ordinal));

        var threeNode = RunPolicy(
            baseline,
            intervals,
            productionThermodynamics,
            ThermodynamicBranchContinuityOptions.H13BoundedHysteresis,
            H16TargetNodeIds);
        var threeNodeRepeat = RunPolicy(
            baseline,
            intervals,
            productionThermodynamics,
            ThermodynamicBranchContinuityOptions.H13BoundedHysteresis,
            H16TargetNodeIds);
        var threeNodeDeterministic = Fingerprint(threeNode) == Fingerprint(threeNodeRepeat);
        Assert.True(threeNodeDeterministic, "H.16 three-node bounded-hysteresis trigger evaluation was not exactly deterministic.");
        Assert.Equal(baseline.Count, threeNode.Events.Count);
        Assert.All(threeNode.Events, static item => Assert.True(AcceptedMeritStrictlyDecreases(item.Result.Iterations)));
        Assert.InRange(threeNode.DeterministicHydraulicEvaluationWorkRatio, 0d, MaximumAlgorithmWorkRatio);
        Assert.InRange(threeNode.MaximumHydraulicMassClosureKilogramsPerSecond, 0d, 1e-8d);
        Assert.InRange(threeNode.MaximumHydraulicEnergyOwnershipResidualWatts, 0d, 1e-3d);

        var interval723 = Assert.Single(threeNode.Events, static item => item.IntervalIndex == 723);
        var header723Overrides = interval723.Decisions.Count(static decision =>
            string.Equals(decision.NodeId, "header", StringComparison.Ordinal)
            && decision.SelectionDiffersFromProduction);
        var interval723Recovered = interval723.Result.Converged
            && !interval723.Result.LineSearchExhausted
            && header723Overrides > 0;

        var committedObservation = ObserveCommittedTargetSelection(reference, productionThermodynamics, H16TargetNodeIds);
        var committedObservationRepeat = ObserveCommittedTargetSelection(reference, productionThermodynamics, H16TargetNodeIds);
        var committedObservationDeterministic = ObservationFingerprint(committedObservation)
            == ObservationFingerprint(committedObservationRepeat);
        Assert.True(committedObservationDeterministic, "H.16 committed three-node branch observation was not exactly deterministic.");
        Assert.Equal(BroaderIntervalCount * H16TargetNodeIds.Length, committedObservation.Rows.Count);
        var committedTransparent = committedObservation.Rows.All(static item => !item.SelectionDiffersFromProduction);

        var challenges = RunReleaseChallenges(productionThermodynamics);
        var challengeRepeat = RunReleaseChallenges(productionThermodynamics);
        var challengeDeterministic = ChallengeFingerprint(challenges) == ChallengeFingerprint(challengeRepeat);
        Assert.True(challengeDeterministic, "H.16 inherited hysteresis release challenges were not exactly deterministic.");
        Assert.Equal(2, challenges.Count(static item => string.Equals(item.ExpectedDecisionKind, "hold-previous-phase-hysteresis", StringComparison.Ordinal)));
        Assert.Equal(2, challenges.Count(static item => string.Equals(item.ExpectedDecisionKind, "production-hysteresis-release", StringComparison.Ordinal)));
        Assert.All(challenges, static item => Assert.True(item.Passed, item.Name));

        var threeNodeQualifies = QualifiesBroaderPolicy(threeNode, threeNodeDeterministic, baseline.Count);
        var releaseChallengesPass = challengeDeterministic && challenges.All(static item => item.Passed);
        var qualificationPasses = threeNodeQualifies
            && interval723Recovered
            && committedObservationDeterministic
            && committedTransparent
            && releaseChallengesPass;

        WriteAuditReports(
            baseline,
            productionH13,
            h14Control,
            threeNode,
            threeNodeDeterministic,
            threeNodeQualifies,
            interval723Recovered,
            header723Overrides,
            committedObservation,
            committedObservationDeterministic,
            committedTransparent,
            challenges,
            challengeDeterministic,
            releaseChallengesPass,
            qualificationPasses);
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
        ThermodynamicBranchContinuityOptions options,
        IReadOnlyList<string> targetNodeIds)
    {
        var events = new List<PolicyEvent>(baseline.Count);
        foreach (var item in baseline)
        {
            var interval = intervals[item.IntervalIndex];
            var shadowThermodynamics = new ThermodynamicBranchContinuityModel(
                productionThermodynamics,
                productionThermodynamics,
                options,
                targetNodeIds);
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
            events.ToArray(),
            (BroaderIntervalCount + evaluationSum) / (double)BroaderIntervalCount,
            decisions.Count(static item => item.SelectionDiffersFromProduction),
            decisions.Count(static item => string.Equals(
                item.DecisionKind,
                "hold-previous-phase-hysteresis",
                StringComparison.Ordinal)),
            decisions.Count(static item => string.Equals(item.DecisionKind, "production-hysteresis-release", StringComparison.Ordinal)),
            MaximumFinite(decisions.Select(static item => item.AvoidedProductionPressureDifferencePascals)),
            MaximumFinite(decisions.Select(static item => item.AvoidedProductionTemperatureDifferenceKelvins)),
            events.Count == 0 ? 0d : events.Max(static item => Math.Abs(item.Result.AppliedHydraulicMassRateClosureResidualKilogramsPerSecond)),
            events.Count == 0 ? 0d : events.Max(static item => Math.Abs(item.Result.AppliedHydraulicEnergyOwnershipResidualWatts)));
    }

    private static CommittedObservation ObserveCommittedTargetSelection(
        IReadOnlyList<ReferenceInterval> reference,
        SimplifiedWaterSteamThermodynamicModel productionThermodynamics,
        IReadOnlyList<string> targetNodeIds)
    {
        var rows = new List<CommittedObservationRow>(reference.Count * targetNodeIds.Count);
        var shadow = new ThermodynamicBranchContinuityModel(
            productionThermodynamics,
            productionThermodynamics,
            ThermodynamicBranchContinuityOptions.H13BoundedHysteresis,
            targetNodeIds);
        var previousCommittedPhases = new Dictionary<string, FluidPhase>(StringComparer.Ordinal);
        var committedPhaseTransitions = 0;

        foreach (var interval in reference)
        {
            foreach (var nodeId in targetNodeIds)
            {
                var node = Assert.Single(interval.Start.FluidNodes, item => string.Equals(item.Id, nodeId, StringComparison.Ordinal));
                if (previousCommittedPhases.TryGetValue(nodeId, out var previousPhase) && previousPhase != node.Phase)
                {
                    committedPhaseTransitions++;
                }

                previousCommittedPhases[nodeId] = node.Phase;
                var decisionStart = shadow.Decisions.Count;
                var selected = shadow.Resolve(node.Definition, node.Inventory, node.Thermodynamics);
                var decision = Assert.Single(shadow.Decisions.Skip(decisionStart));
                rows.Add(new CommittedObservationRow(
                    interval.Index,
                    nodeId,
                    node.Phase.ToString(),
                    decision.ProductionPhase,
                    selected.Phase.ToString(),
                    decision.DecisionKind,
                    decision.SelectionDiffersFromProduction,
                    decision.SelectedPreviousPhase,
                    decision.PreviousPhaseRelativePressureDrift,
                    decision.PreviousPhaseTemperatureDriftKelvins));
            }
        }

        return new CommittedObservation(rows.ToArray(), committedPhaseTransitions);
    }

    private static IReadOnlyList<ReleaseChallenge> RunReleaseChallenges(
        SimplifiedWaterSteamThermodynamicModel productionThermodynamics)
    {
        return new[]
        {
            RunReleaseChallenge(
                "steam-near-hold",
                productionThermodynamics,
                new FluidNodeDefinition("steam", Volume.FromCubicMetres(100d)),
                new FluidNodeInventory(
                    Mass.FromKilograms(SteamMassKilograms),
                    Energy.FromJoules(SteamEnergyJoules + SteamEnergyProbeJoules)),
                new FluidThermodynamicState(
                    Pressure.FromPascals(6362325.9673817037d),
                    Temperature.FromKelvins(552.58890484070866d),
                    FluidPhase.SaturatedMixture,
                    VaporQuality.FromFraction(0.98827242641541357d)),
                FluidPhase.SuperheatedVapor,
                FluidPhase.SaturatedMixture,
                "hold-previous-phase-hysteresis"),
            RunReleaseChallenge(
                "steam-distant-release",
                productionThermodynamics,
                new FluidNodeDefinition("steam", Volume.FromCubicMetres(100d)),
                new FluidNodeInventory(
                    Mass.FromKilograms(SteamMassKilograms),
                    Energy.FromJoules(SteamEnergyJoules + SteamEnergyProbeJoules)),
                new FluidThermodynamicState(
                    Pressure.FromPascals(5_000_000d),
                    Temperature.FromKelvins(530d),
                    FluidPhase.SaturatedMixture,
                    VaporQuality.FromFraction(0.95d)),
                FluidPhase.SuperheatedVapor,
                FluidPhase.SuperheatedVapor,
                "production-hysteresis-release"),
            RunReleaseChallenge(
                "stop-out-near-hold",
                productionThermodynamics,
                new FluidNodeDefinition("stop-out", Volume.FromCubicMetres(100d)),
                new FluidNodeInventory(
                    Mass.FromKilograms(StopOutMassKilograms),
                    Energy.FromJoules(StopOutEnergyJoules - StopOutEnergyProbeJoules)),
                new FluidThermodynamicState(
                    Pressure.FromPascals(8601730.4979163781d),
                    Temperature.FromKelvins(588.83285718179309d),
                    FluidPhase.SuperheatedVapor,
                    null),
                FluidPhase.SaturatedMixture,
                FluidPhase.SuperheatedVapor,
                "hold-previous-phase-hysteresis"),
            RunReleaseChallenge(
                "stop-out-distant-release",
                productionThermodynamics,
                new FluidNodeDefinition("stop-out", Volume.FromCubicMetres(100d)),
                new FluidNodeInventory(
                    Mass.FromKilograms(StopOutMassKilograms),
                    Energy.FromJoules(StopOutEnergyJoules - StopOutEnergyProbeJoules)),
                new FluidThermodynamicState(
                    Pressure.FromPascals(11_000_000d),
                    Temperature.FromKelvins(620d),
                    FluidPhase.SuperheatedVapor,
                    null),
                FluidPhase.SaturatedMixture,
                FluidPhase.SaturatedMixture,
                "production-hysteresis-release"),
        };
    }

    private static ReleaseChallenge RunReleaseChallenge(
        string name,
        SimplifiedWaterSteamThermodynamicModel productionThermodynamics,
        FluidNodeDefinition definition,
        FluidNodeInventory inventory,
        FluidThermodynamicState previousState,
        FluidPhase expectedProductionPhase,
        FluidPhase expectedSelectedPhase,
        string expectedDecisionKind)
    {
        var shadow = new ThermodynamicBranchContinuityModel(
            productionThermodynamics,
            productionThermodynamics,
            ThermodynamicBranchContinuityOptions.H13BoundedHysteresis,
            H16TargetNodeIds);
        var production = productionThermodynamics.Resolve(definition, inventory, previousState);
        var selected = shadow.Resolve(definition, inventory, previousState);
        var decision = Assert.Single(shadow.Decisions);
        var passed = production.Phase == expectedProductionPhase
            && selected.Phase == expectedSelectedPhase
            && string.Equals(decision.DecisionKind, expectedDecisionKind, StringComparison.Ordinal);
        return new ReleaseChallenge(
            name,
            definition.Id,
            previousState.Phase.ToString(),
            production.Phase.ToString(),
            selected.Phase.ToString(),
            expectedDecisionKind,
            decision.DecisionKind,
            decision.PreviousPhaseRelativePressureDrift,
            decision.PreviousPhaseTemperatureDriftKelvins,
            passed);
    }

    private static bool QualifiesBroaderPolicy(PolicyRun run, bool deterministicRepeat, int expectedEventCount)
    {
        var options = JacobianHydraulicCorrectorOptions.H9AuditDefault;
        return deterministicRepeat
            && expectedEventCount >= 7
            && run.Events.Count == expectedEventCount
            && run.Events.All(static item => item.Result.Converged)
            && run.Events.All(static item => !item.Result.LineSearchExhausted)
            && run.Events.All(item => item.Result.MaximumRelativePressureFixedPointResidual <= options.RelativePressureTolerance)
            && run.Events.All(item => item.Result.MaximumAbsoluteFlowFixedPointResidualKilogramsPerSecond <= options.AbsoluteFlowToleranceKilogramsPerSecond)
            && run.Events.All(static item => item.Result.NormalizedMeritResidual <= 1d)
            && run.Events.All(static item => AcceptedMeritStrictlyDecreases(item.Result.Iterations))
            && run.DeterministicHydraulicEvaluationWorkRatio <= MaximumAlgorithmWorkRatio
            && run.MaximumHydraulicMassClosureKilogramsPerSecond <= 1e-8d
            && run.MaximumHydraulicEnergyOwnershipResidualWatts <= 1e-3d;
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
        var intervals = new List<ReferenceInterval>(BroaderIntervalCount);
        for (var index = 0; index < BroaderIntervalCount; index++)
        {
            var start = ToPlantState(engine.LatestCanonicalSnapshot.Control.ProtectedControl.FullPlant.CandidatePlant);
            Assert.Equal(HydraulicNumericalCouplingMode.ExplicitCommittedState, start.Definition.HydraulicNumericalCoupling.Mode);
            var presentation = engine.Step(ControlRoomRunState.Running);
            Assert.False(presentation.AnyTripActive, $"Unexpected reference trip at H.16 reference interval {index + 1}.");
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
                item.Result.MaximumRelativePressureFixedPointResidual.ToString("G17", CultureInfo.InvariantCulture),
                item.Result.MaximumAbsoluteFlowFixedPointResidualKilogramsPerSecond.ToString("G17", CultureInfo.InvariantCulture),
                item.Result.NormalizedMeritResidual.ToString("G17", CultureInfo.InvariantCulture),
                string.Join(";", item.Decisions.Select(DecisionFingerprint)))));

    private static string DecisionFingerprint(ThermodynamicBranchContinuityDecision decision)
        => FormattableString.Invariant(
            $"{decision.Sequence}:{decision.NodeId}:{decision.PreviousPhase}:{decision.ProductionPhase}:{decision.SelectedPhase}:{decision.DecisionKind}:{decision.PreviousPhaseRelativePressureDrift:G17}:{decision.PreviousPhaseTemperatureDriftKelvins:G17}");

    private static string ObservationFingerprint(CommittedObservation observation)
        => string.Join(
            "||",
            observation.Rows.Select(static item => FormattableString.Invariant(
                $"{item.IntervalIndex}:{item.NodeId}:{item.CommittedPhase}:{item.ProductionPhase}:{item.SelectedPhase}:{item.DecisionKind}:{item.RelativePressureDrift:G17}:{item.TemperatureDriftKelvins:G17}")));

    private static string ChallengeFingerprint(IReadOnlyList<ReleaseChallenge> challenges)
        => string.Join(
            "||",
            challenges.Select(static item => FormattableString.Invariant(
                $"{item.Name}:{item.NodeId}:{item.PreviousPhase}:{item.ProductionPhase}:{item.SelectedPhase}:{item.ActualDecisionKind}:{item.RelativePressureDrift:G17}:{item.TemperatureDriftKelvins:G17}:{item.Passed}")));

    private static void WriteAuditReports(
        IReadOnlyList<BaselineTriggerEvent> baseline,
        ProductionRun productionH13,
        PolicyRun h14Control,
        PolicyRun threeNode,
        bool threeNodeDeterministic,
        bool threeNodeQualifies,
        bool interval723Recovered,
        int header723Overrides,
        CommittedObservation observation,
        bool observationDeterministic,
        bool committedTransparent,
        IReadOnlyList<ReleaseChallenge> challenges,
        bool challengeDeterministic,
        bool releaseChallengesPass,
        bool qualificationPasses)
    {
        var root = FindRepositoryRoot();
        var directory = Path.Combine(root, "artifacts", "h16-three-node-branch-continuity");
        Directory.CreateDirectory(directory);

        var eventRows = new List<string>
        {
            "interval,h4_primary_converged,h14_two_node_converged,h14_two_node_line_search_exhausted,h16_three_node_converged,h16_three_node_line_search_exhausted,h16_pressure_residual,h16_flow_residual_kg_s,h16_normalized_merit,h16_hydraulic_evaluations,h16_branch_decisions,h16_branch_overrides,h16_header_overrides,h16_previous_phase_holds,h16_hysteresis_releases",
        };
        foreach (var trigger in baseline)
        {
            var control = Assert.Single(h14Control.Events, candidate => candidate.IntervalIndex == trigger.IntervalIndex);
            var item = Assert.Single(threeNode.Events, candidate => candidate.IntervalIndex == trigger.IntervalIndex);
            var eventOverrides = item.Decisions.Count(static decision => decision.SelectionDiffersFromProduction);
            var headerOverrides = item.Decisions.Count(static decision =>
                string.Equals(decision.NodeId, "header", StringComparison.Ordinal)
                && decision.SelectionDiffersFromProduction);
            var eventHolds = item.Decisions.Count(static decision => decision.SelectedPreviousPhase);
            var eventReleases = item.Decisions.Count(static decision =>
                string.Equals(decision.DecisionKind, "production-hysteresis-release", StringComparison.Ordinal));
            eventRows.Add(FormattableString.Invariant(
                $"{item.IntervalIndex},{trigger.PrimaryResult.Converged},{control.Result.Converged},{control.Result.LineSearchExhausted},{item.Result.Converged},{item.Result.LineSearchExhausted},{item.Result.MaximumRelativePressureFixedPointResidual:G17},{item.Result.MaximumAbsoluteFlowFixedPointResidualKilogramsPerSecond:G17},{item.Result.NormalizedMeritResidual:G17},{item.Result.HydraulicEvaluationCount},{item.Decisions.Count},{eventOverrides},{headerOverrides},{eventHolds},{eventReleases}"));
        }

        var observationRows = new List<string>
        {
            "interval,node,committed_phase,production_reresolve_phase,hysteresis_selected_phase,decision_kind,selection_differs_from_production,selected_previous_phase,previous_phase_pressure_drift,previous_phase_temperature_drift_k",
        };
        observationRows.AddRange(observation.Rows.Select(static item => FormattableString.Invariant(
            $"{item.IntervalIndex},{item.NodeId},{item.CommittedPhase},{item.ProductionPhase},{item.SelectedPhase},{item.DecisionKind},{item.SelectionDiffersFromProduction},{item.SelectedPreviousPhase},{item.RelativePressureDrift:G17},{item.TemperatureDriftKelvins:G17}")));

        var challengeRows = new List<string>
        {
            "name,node,previous_phase,production_phase,selected_phase,expected_decision,actual_decision,previous_phase_pressure_drift,previous_phase_temperature_drift_k,passed",
        };
        challengeRows.AddRange(challenges.Select(static item => FormattableString.Invariant(
            $"{item.Name},{item.NodeId},{item.PreviousPhase},{item.ProductionPhase},{item.SelectedPhase},{item.ExpectedDecisionKind},{item.ActualDecisionKind},{item.RelativePressureDrift:G17},{item.TemperatureDriftKelvins:G17},{item.Passed}")));

        var committedObservationHolds = observation.Rows.Count(static item =>
            string.Equals(item.DecisionKind, "hold-previous-phase-hysteresis", StringComparison.Ordinal));
        var committedObservationReleases = observation.Rows.Count(static item =>
            string.Equals(item.DecisionKind, "production-hysteresis-release", StringComparison.Ordinal));
        var challengeHolds = challenges.Count(static item =>
            string.Equals(item.ActualDecisionKind, "hold-previous-phase-hysteresis", StringComparison.Ordinal));
        var challengeReleases = challenges.Count(static item =>
            string.Equals(item.ActualDecisionKind, "production-hysteresis-release", StringComparison.Ordinal));
        var h14Interval723 = Assert.Single(h14Control.Events, static item => item.IntervalIndex == 723);
        var h16Interval723 = Assert.Single(threeNode.Events, static item => item.IntervalIndex == 723);

        var metricsRows = new[]
        {
            "metric,value",
            FormattableString.Invariant($"production_shadow_intervals,{BroaderIntervalCount}"),
            FormattableString.Invariant($"triggered_events,{baseline.Count}"),
            FormattableString.Invariant($"triggered_events_first_500,{baseline.Count(static item => item.IntervalIndex <= H13WindowIntervalCount)}"),
            FormattableString.Invariant($"h14_two_node_converged,{h14Control.Events.Count(static item => item.Result.Converged)}"),
            FormattableString.Invariant($"h14_two_node_line_search_exhausted,{h14Control.Events.Count(static item => item.Result.LineSearchExhausted)}"),
            FormattableString.Invariant($"h14_interval_723_converged,{h14Interval723.Result.Converged}"),
            FormattableString.Invariant($"h16_three_node_converged,{threeNode.Events.Count(static item => item.Result.Converged)}"),
            FormattableString.Invariant($"h16_three_node_line_search_exhausted,{threeNode.Events.Count(static item => item.Result.LineSearchExhausted)}"),
            FormattableString.Invariant($"h16_interval_723_converged,{h16Interval723.Result.Converged}"),
            FormattableString.Invariant($"h16_interval_723_header_overrides,{header723Overrides}"),
            FormattableString.Invariant($"h16_branch_overrides,{threeNode.BranchOverrideCount}"),
            FormattableString.Invariant($"h16_previous_phase_holds,{threeNode.PreviousPhaseHoldCount}"),
            FormattableString.Invariant($"h16_solver_hysteresis_releases,{threeNode.HysteresisReleaseCount}"),
            FormattableString.Invariant($"committed_observation_rows,{observation.Rows.Count}"),
            FormattableString.Invariant($"committed_observation_overrides,{observation.Rows.Count(static item => item.SelectionDiffersFromProduction)}"),
            FormattableString.Invariant($"committed_observation_holds,{committedObservationHolds}"),
            FormattableString.Invariant($"committed_observation_releases,{committedObservationReleases}"),
            FormattableString.Invariant($"committed_phase_transitions,{observation.CommittedPhaseTransitionCount}"),
            FormattableString.Invariant($"release_challenge_holds,{challengeHolds}"),
            FormattableString.Invariant($"release_challenge_releases,{challengeReleases}"),
            FormattableString.Invariant($"three_node_policy_qualifies,{threeNodeQualifies}"),
            FormattableString.Invariant($"interval_723_recovered,{interval723Recovered}"),
            FormattableString.Invariant($"committed_selection_transparent,{committedTransparent}"),
            FormattableString.Invariant($"release_challenges_pass,{releaseChallengesPass}"),
            FormattableString.Invariant($"three_node_shadow_qualification_passes,{qualificationPasses}"),
        };

        File.WriteAllLines(Path.Combine(directory, "02-triggered-event-three-node-comparison.csv"), eventRows, Utf8WithoutBom);
        File.WriteAllLines(Path.Combine(directory, "03-committed-three-node-branch-observation.csv"), observationRows, Utf8WithoutBom);
        File.WriteAllLines(Path.Combine(directory, "04-hysteresis-release-challenges.csv"), challengeRows, Utf8WithoutBom);
        File.WriteAllLines(Path.Combine(directory, "05-three-node-qualification-metrics.csv"), metricsRows, Utf8WithoutBom);

        var h13ProductionConverged = productionH13.Events.Count(static item => item.Result.Converged);
        var h13ProductionExhausted = productionH13.Events.Count(static item => item.Result.LineSearchExhausted);
        var h14Converged = h14Control.Events.Count(static item => item.Result.Converged);
        var h14Exhausted = h14Control.Events.Count(static item => item.Result.LineSearchExhausted);
        var threeNodeConverged = threeNode.Events.Count(static item => item.Result.Converged);
        var threeNodeExhausted = threeNode.Events.Count(static item => item.Result.LineSearchExhausted);
        var recommendation = qualificationPasses
            ? "H.16 recommendation: extending the unchanged 2%/5 K bounded previous-phase hysteresis policy to header resolves the H.15-localized interval-723 mechanism and qualifies all 15 frozen P060/F040 events over the 2000-interval horizon. Keep production explicit and proceed to a longer-horizon/cross-profile shadow qualification before any activation candidate."
            : "H.16 recommendation: the three-node extension does not yet qualify the complete 2000-interval evidence set. Keep production explicit and inspect the per-event three-node comparison before changing hysteresis limits, H.9 or production routing.";
        var auditPasses = h13ProductionConverged == 5
            && h13ProductionExhausted == 2
            && baseline.Count == 15
            && baseline.Count(static item => item.IntervalIndex <= H13WindowIntervalCount) == 7
            && h14Converged == 14
            && h14Exhausted == 1
            && !h14Interval723.Result.Converged
            && h14Interval723.Result.LineSearchExhausted
            && threeNodeDeterministic
            && observationDeterministic
            && challengeDeterministic
            && threeNode.Events.Count == baseline.Count
            && threeNode.DeterministicHydraulicEvaluationWorkRatio <= MaximumAlgorithmWorkRatio
            && threeNode.MaximumHydraulicMassClosureKilogramsPerSecond <= 1e-8d
            && threeNode.MaximumHydraulicEnergyOwnershipResidualWatts <= 1e-3d;

        var summary = new[]
        {
            "================================================================================",
            "M10.9.4.1-H.16 EXTENDED THREE-NODE BRANCH-CONTINUITY SHADOW QUALIFICATION SUMMARY",
            "================================================================================",
            "=== 01-current-v2-three-node-bounded-hysteresis-qualification ===",
            "Shadow-only requalification of the validated bounded previous-phase hysteresis policy extended from H.14 targets steam/stop-out to H.15-localized target header; production Resolve(), production explicit integration, H.9 and the 2%/5 K hysteresis limits remain unchanged and no shadow candidate is committed.",
            FormattableString.Invariant($"production-shadow-steps={BroaderIntervalCount}; frozen-trigger=P060-F040; triggered-events={baseline.Count}; H13-window-triggered-events={baseline.Count(static item => item.IntervalIndex <= H13WindowIntervalCount)}; H13-production-H9-converged={h13ProductionConverged}/7; H13-production-line-search-exhausted={h13ProductionExhausted};"),
            FormattableString.Invariant($"H14-control-targets=steam|stop-out; H14-two-node-converged={h14Converged}/{baseline.Count}; line-search-exhausted={h14Exhausted}; interval-723-converged={h14Interval723.Result.Converged}; interval-723-branch-overrides={h14Interval723.Decisions.Count(static decision => decision.SelectionDiffersFromProduction)};"),
            FormattableString.Invariant($"H16-targets=steam|stop-out|header; three-node-converged={threeNodeConverged}/{baseline.Count}; line-search-exhausted={threeNodeExhausted}; interval-723-recovered={interval723Recovered}; interval-723-header-overrides={header723Overrides}; branch-overrides={threeNode.BranchOverrideCount}; previous-phase-holds={threeNode.PreviousPhaseHoldCount}; solver-hysteresis-releases={threeNode.HysteresisReleaseCount}; deterministic-work-ratio={threeNode.DeterministicHydraulicEvaluationWorkRatio:0.000000}; deterministic-repeat={threeNodeDeterministic}; three-node-policy-qualifies={threeNodeQualifies};"),
            FormattableString.Invariant($"committed-target-observations={observation.Rows.Count}; committed-selection-overrides={observation.Rows.Count(static item => item.SelectionDiffersFromProduction)}; committed-selection-transparent={committedTransparent}; committed-previous-phase-holds={committedObservationHolds}; committed-hysteresis-releases={committedObservationReleases}; committed-target-phase-transitions={observation.CommittedPhaseTransitionCount}; deterministic-repeat={observationDeterministic};"),
            FormattableString.Invariant($"release-challenges={challenges.Count}; required-holds=2; observed-holds={challengeHolds}; required-releases=2; observed-releases={challengeReleases}; release-challenges-pass={releaseChallengesPass}; deterministic-repeat={challengeDeterministic};"),
            FormattableString.Invariant($"max-closure/ownership={threeNode.MaximumHydraulicMassClosureKilogramsPerSecond:0.000000000}/{threeNode.MaximumHydraulicEnergyOwnershipResidualWatts:0.000000000}; three-node-shadow-qualification-passes={qualificationPasses}; three-node-branch-continuity-audit-passes={auditPasses};"),
            "bounded-hysteresis-limits-changed=False; production-resolve-order-changed=False; production-previous-state-hysteresis-introduced=False; production-hybrid-active=False; production-fixed-step=10.000 ms; shadow-candidates-committed=False; H9-corrector-replaced=False; plant-network-orchestrator-routing-changed=False; trigger-retuning=False; physical-coefficient-retuning=False; hidden-flow-filtering=False;",
            recommendation,
            FormattableString.Invariant($"Detailed CSV files: \"{directory}\""),
        };
        File.WriteAllLines(
            Path.Combine(directory, "01-current-v2-three-node-branch-continuity-qualification.summary.txt"),
            summary,
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
        IReadOnlyList<PolicyEvent> Events,
        double DeterministicHydraulicEvaluationWorkRatio,
        int BranchOverrideCount,
        int PreviousPhaseHoldCount,
        int HysteresisReleaseCount,
        double MaximumAvoidedProductionPressureDifferencePascals,
        double MaximumAvoidedProductionTemperatureDifferenceKelvins,
        double MaximumHydraulicMassClosureKilogramsPerSecond,
        double MaximumHydraulicEnergyOwnershipResidualWatts);

    private sealed record CommittedObservationRow(
        int IntervalIndex,
        string NodeId,
        string CommittedPhase,
        string ProductionPhase,
        string SelectedPhase,
        string DecisionKind,
        bool SelectionDiffersFromProduction,
        bool SelectedPreviousPhase,
        double RelativePressureDrift,
        double TemperatureDriftKelvins);

    private sealed record CommittedObservation(
        IReadOnlyList<CommittedObservationRow> Rows,
        int CommittedPhaseTransitionCount);

    private sealed record ReleaseChallenge(
        string Name,
        string NodeId,
        string PreviousPhase,
        string ProductionPhase,
        string SelectedPhase,
        string ExpectedDecisionKind,
        string ActualDecisionKind,
        double RelativePressureDrift,
        double TemperatureDriftKelvins,
        bool Passed);
}
