using System.Globalization;
using System.Text;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios.Faults.SecondaryTransients;
using NuclearReactorSimulator.Application.Scenarios.Training;
using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using NuclearReactorSimulator.Simulation.Plant;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay;

/// <summary>
/// M10.9.4.1-H.19 long-horizon and cross-profile shadow qualification of the H.18-validated
/// steam/stop-out/header/turbine-inlet bounded previous-phase hysteresis target set. Production remains explicit.
/// </summary>
public sealed class FourNodeLongHorizonCrossProfileQualificationAuditTests
{
    private static readonly TimeSpan Step = TimeSpan.FromMilliseconds(10d);
    private static readonly Encoding Utf8WithoutBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    private static readonly string[] H19TargetNodeIds = { "steam", "stop-out", "header", "turbine-inlet" };
    private const int H16ControlIntervalCount = 2_000;
    private const int ObservationStride = 10;
    private const int MaximumStratifiedQualificationEvents = 512;
    private const int ExpectedH17CensusTriggerEvents = 3_046;
    private const int ExpectedH17TriggerEpisodes = 92;
    private const int ExpectedH17QualifiedRepresentatives = 473;
    private const int MinimumTemporalSamplesPerProfile = 64;
    private const int TriggerEpisodeMaximumQuietGapIntervals = 25;
    private const int DeterminismSentinelsPerProfile = 5;
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

    private static readonly ProfileDefinition[] Profiles =
    {
        new("steady-long", 12_000, ProfileKind.Steady),
        new("load-pulse", 6_000, ProfileKind.LoadPulse),
        new("cooling-pulse", 6_000, ProfileKind.CoolingPulse),
        new("combined-load-cooling", 6_000, ProfileKind.CombinedLoadCooling),
    };

    [Fact]
    public void DeterminismFingerprints_AreCanonicalAcrossEquivalentTraversalOrders()
    {
        var firstObservationRow = new CommittedObservationRow(
            "steady-long", 1, "steam", "SaturatedMixture", "SaturatedMixture", "SaturatedMixture",
            "hold-previous-phase-hysteresis", false, true, false, 0.001d, 0.1d);
        var secondObservationRow = new CommittedObservationRow(
            "load-pulse", 2, "header", "SaturatedMixture", "SuperheatedVapor", "SaturatedMixture",
            "hold-previous-phase-hysteresis", true, true, false, 0.002d, 0.2d);
        var observationForward = new CommittedObservation(new[] { firstObservationRow, secondObservationRow }, 0, 2);
        var observationReverse = new CommittedObservation(new[] { secondObservationRow, firstObservationRow }, 0, 2);
        Assert.Equal(ObservationFingerprint(observationForward), ObservationFingerprint(observationReverse));

        var firstInverseRow = new InverseBranchScanRow(
            "steady-long", 1, "steam", true, "coarse-saturated", "SaturatedMixture", true, false,
            "coarse-saturated", "SaturatedMixture", true, false, false);
        var secondInverseRow = new InverseBranchScanRow(
            "load-pulse", 2, "header", true, "coarse-superheated", "SuperheatedVapor", true, true,
            "coarse-saturated", "SaturatedMixture", true, false, true);
        var inverseForward = new InverseBranchScan(new[] { firstInverseRow, secondInverseRow }, Array.Empty<string>(), Array.Empty<string>());
        var inverseReverse = new InverseBranchScan(new[] { secondInverseRow, firstInverseRow }, Array.Empty<string>(), Array.Empty<string>());
        Assert.Equal(InverseScanFingerprint(inverseForward), InverseScanFingerprint(inverseReverse));
    }

    [Fact(Explicit = true)]
    [Trait("Category", "FourNodeLongHorizonCrossProfileQualificationAudit")]
    public void FourNodeBoundedHysteresis_QualifiesLongHorizonAndCrossProfileWithoutDiscoveringAnotherUntargetedLateShadowNode()
    {
        var productionThermodynamics = new SimplifiedWaterSteamThermodynamicModel();
        var prototype = new SemiImplicitHydraulicPrototypeSolver(productionThermodynamics);
        var gate = new HybridSemiImplicitHydraulicGateSolver(productionThermodynamics);

        ResetProgress();
        var trajectoryList = new List<ProfileTrajectory>(Profiles.Length);
        foreach (var profile in Profiles)
        {
            WriteProgress($"reference-start profile={profile.Id} intervals={profile.IntervalCount}");
            var trajectory = BuildReferenceTrajectory(profile, prototype);
            trajectoryList.Add(trajectory);
            WriteProgress($"reference-complete profile={profile.Id} intervals={trajectory.Intervals.Count}");
        }
        var trajectories = trajectoryList.ToArray();
        var totalIntervals = trajectories.Sum(static profile => profile.Intervals.Count);
        Assert.Equal(30_000, totalIntervals);
        var intervalMap = trajectories
            .SelectMany(static profile => profile.Intervals)
            .ToDictionary(static item => (item.ProfileId, item.Index));

        var baselineList = new List<ProfileBaseline>(trajectories.Length);
        foreach (var profile in trajectories)
        {
            WriteProgress($"trigger-census-start profile={profile.ProfileId}");
            var baseline = EvaluatePrimaryGate(profile, gate);
            baselineList.Add(baseline);
            WriteProgress($"trigger-census-complete profile={profile.ProfileId} triggers={baseline.Events.Count}");
        }
        var baselines = baselineList.ToArray();
        Assert.All(baselines, static baseline => Assert.True(baseline.Events.Count >= 7, $"Profile {baseline.ProfileId} did not retain enough P060/F040 trigger coverage."));
        Assert.All(baselines, static baseline => Assert.Contains(baseline.Events, static item => item.IntervalIndex > 500));

        var steady = Assert.Single(trajectories, static profile => string.Equals(profile.ProfileId, "steady-long", StringComparison.Ordinal));
        Assert.Equal(12_000, steady.Intervals.Count);
        var steadyBaseline = Assert.Single(baselines, static baseline => string.Equals(baseline.ProfileId, "steady-long", StringComparison.Ordinal));
        var h16ControlBaseline = new ProfileBaseline(
            "steady-long-h16-control",
            steadyBaseline.Events.Where(static item => item.IntervalIndex <= H16ControlIntervalCount).ToArray());
        Assert.Equal(15, h16ControlBaseline.Events.Count);

        var h16Control = RunPolicy(
            h16ControlBaseline.Events,
            intervalMap,
            productionThermodynamics,
            H16ControlIntervalCount);
        Assert.Equal(15, h16Control.Events.Count(static item => item.Result.Converged));
        Assert.DoesNotContain(h16Control.Events, static item => item.Result.LineSearchExhausted);
        var h16Interval723 = Assert.Single(h16Control.Events, static item => item.IntervalIndex == 723);
        Assert.True(h16Interval723.Result.Converged);
        Assert.Contains(h16Interval723.Decisions, static decision =>
            string.Equals(decision.NodeId, "header", StringComparison.Ordinal)
            && decision.SelectionDiffersFromProduction);

        var allTriggers = baselines.SelectMany(static item => item.Events).ToArray();
        WriteProgress($"trigger-stratification-start census={allTriggers.Length}");
        var stratification = StratifyTriggerEvents(baselines);
        Assert.Equal(allTriggers.Length, stratification.CensusTriggerCount);
        Assert.Equal(ExpectedH17CensusTriggerEvents, allTriggers.Length);
        Assert.Equal(ExpectedH17TriggerEpisodes, stratification.Episodes.Count);
        Assert.Equal(ExpectedH17QualifiedRepresentatives, stratification.SelectedEvents.Count);
        Assert.InRange(stratification.SelectedEvents.Count, 15, MaximumStratifiedQualificationEvents);
        var frozenH17Representatives = LoadFrozenH17Representatives();
        Assert.Equal(ExpectedH17QualifiedRepresentatives, frozenH17Representatives.Count);
        Assert.Equal(245, frozenH17Representatives.Count(static item => !item.H17Converged));
        Assert.Equal(228, frozenH17Representatives.Count(static item => item.H17Converged));
        Assert.Equal(120, frozenH17Representatives.Count(static item => !item.H17Converged && item.TurbineInletPhaseMismatch));
        Assert.Equal(125, frozenH17Representatives.Count(static item => !item.H17Converged && !item.TurbineInletPhaseMismatch));
        var frozenH17Keys = frozenH17Representatives
            .Select(static item => (item.ProfileId, item.IntervalIndex))
            .ToHashSet();
        var h19Keys = stratification.SelectedEvents
            .Select(static item => (item.ProfileId, item.IntervalIndex))
            .ToHashSet();
        Assert.True(frozenH17Keys.SetEquals(h19Keys), "H.19 stratification no longer matches the validated frozen H.17 representative set.");
        Assert.NotEmpty(stratification.Episodes);
        Assert.All(stratification.Episodes, static episode =>
            Assert.True(episode.SelectedCount > 0, $"Trigger episode {episode.ProfileId}:{episode.EpisodeIndex} was not represented in the H.19 stratified qualification set."));
        Assert.Contains(stratification.SelectedEvents, static item =>
            string.Equals(item.ProfileId, "steady-long", StringComparison.Ordinal)
            && item.IntervalIndex == 723);
        WriteProgress($"trigger-stratification-complete census={allTriggers.Length} episodes={stratification.Episodes.Count} selected={stratification.SelectedEvents.Count}");

        var qualificationBaselines = baselines
            .Select(baseline => new ProfileBaseline(
                baseline.ProfileId,
                stratification.SelectedEvents
                    .Where(item => string.Equals(item.ProfileId, baseline.ProfileId, StringComparison.Ordinal))
                    .OrderBy(static item => item.IntervalIndex)
                    .ToArray()))
            .ToArray();
        var determinismSentinels = SelectDeterminismSentinels(qualificationBaselines);
        var determinismSentinelKeys = determinismSentinels
            .Select(static item => (item.ProfileId, item.IntervalIndex))
            .ToHashSet();
        WriteProgress($"policy-start census={allTriggers.Length} qualified-samples={stratification.SelectedEvents.Count} determinism-sentinels={determinismSentinels.Count}");
        var policy = RunPolicy(stratification.SelectedEvents, intervalMap, productionThermodynamics, totalIntervals);
        WriteProgress($"policy-complete qualified-samples={policy.Events.Count}");
        var policyRepeat = RunPolicy(determinismSentinels, intervalMap, productionThermodynamics, totalIntervals);
        var deterministicRepeat = Fingerprint(policy, determinismSentinelKeys) == Fingerprint(policyRepeat);
        WriteProgress($"policy-determinism-complete sentinels={determinismSentinels.Count} deterministic={deterministicRepeat}");
        Assert.True(deterministicRepeat, "H.19 cross-profile four-node policy was not exactly deterministic.");
        Assert.Equal(stratification.SelectedEvents.Count, policy.Events.Count);
        var frozenH17ByKey = frozenH17Representatives.ToDictionary(static item => (item.ProfileId, item.IntervalIndex));
        var recoveredH17Failures = policy.Events.Count(item =>
            !frozenH17ByKey[(item.ProfileId, item.IntervalIndex)].H17Converged
            && item.Result.Converged);
        var preservedH17Successes = policy.Events.Count(item =>
            frozenH17ByKey[(item.ProfileId, item.IntervalIndex)].H17Converged
            && item.Result.Converged);
        var recoveredH17MismatchFailures = policy.Events.Count(item =>
            !frozenH17ByKey[(item.ProfileId, item.IntervalIndex)].H17Converged
            && frozenH17ByKey[(item.ProfileId, item.IntervalIndex)].TurbineInletPhaseMismatch
            && item.Result.Converged);
        var recoveredH17NoMismatchFailures = policy.Events.Count(item =>
            !frozenH17ByKey[(item.ProfileId, item.IntervalIndex)].H17Converged
            && !frozenH17ByKey[(item.ProfileId, item.IntervalIndex)].TurbineInletPhaseMismatch
            && item.Result.Converged);
        WriteProgress($"h17-recovery-matrix recovered-failures={recoveredH17Failures}/245 preserved-successes={preservedH17Successes}/228 mismatch={recoveredH17MismatchFailures}/120 no-mismatch={recoveredH17NoMismatchFailures}/125");
        Assert.All(policy.Events, static item => Assert.True(AcceptedMeritStrictlyDecreases(item.Result.Iterations)));
        Assert.InRange(policy.DeterministicHydraulicEvaluationWorkRatio, 0d, MaximumAlgorithmWorkRatio);
        Assert.InRange(policy.MaximumHydraulicMassClosureKilogramsPerSecond, 0d, 1e-8d);
        Assert.InRange(policy.MaximumHydraulicEnergyOwnershipResidualWatts, 0d, 1e-3d);

        var triggerKeys = allTriggers.Select(static item => (item.ProfileId, item.IntervalIndex)).ToHashSet();
        WriteProgress("committed-observation-start");
        var committedObservation = ObserveCommittedTargetSelection(
            trajectories,
            productionThermodynamics,
            triggerKeys);
        var observationDeterminismKeys = SelectObservationDeterminismKeys(trajectories, determinismSentinelKeys);
        var committedRepeat = ObserveCommittedTargetSelectionAtKeys(
            intervalMap,
            productionThermodynamics,
            observationDeterminismKeys);
        var committedDeterministic = ObservationFingerprint(committedObservation, observationDeterminismKeys)
            == ObservationFingerprint(committedRepeat);
        WriteProgress($"committed-observation-complete rows={committedObservation.Rows.Count} deterministic-samples={observationDeterminismKeys.Count} deterministic={committedDeterministic}");
        Assert.True(committedDeterministic, "H.19 committed target branch observation was not exactly deterministic.");
        Assert.Equal(totalIntervals * H19TargetNodeIds.Length, committedObservation.CommittedPhaseStateChecks);
        var committedTransparent = committedObservation.Rows.All(static item => !item.SelectionDiffersFromProduction);

        WriteProgress("inverse-scan-start");
        var inverseScan = ScanTriggeredCandidateAndExplicitInverseBranches(policy, intervalMap, productionThermodynamics);
        var inverseRepeat = ScanTriggeredCandidateAndExplicitInverseBranches(policyRepeat, intervalMap, productionThermodynamics);
        var inverseDeterministic = InverseScanFingerprint(inverseScan, determinismSentinelKeys)
            == InverseScanFingerprint(inverseRepeat);
        WriteProgress($"inverse-scan-complete rows={inverseScan.Rows.Count} deterministic-sentinels={determinismSentinels.Count} deterministic={inverseDeterministic}");
        Assert.True(inverseDeterministic, "H.19 inverse-branch discovery scan was not exactly deterministic.");

        var challenges = RunReleaseChallenges(productionThermodynamics);
        var challengeRepeat = RunReleaseChallenges(productionThermodynamics);
        var challengeDeterministic = ChallengeFingerprint(challenges) == ChallengeFingerprint(challengeRepeat);
        Assert.True(challengeDeterministic, "H.19 inherited hold/release challenges were not exactly deterministic.");
        Assert.All(challenges, static item => Assert.True(item.Passed, item.Name));

        var crossProfileQualifies = QualifiesPolicy(policy, deterministicRepeat, stratification.SelectedEvents.Count)
            && baselines.All(static baseline => baseline.Events.Count >= 7)
            && baselines.All(static baseline => baseline.Events.Any(item => item.IntervalIndex > 500))
            && stratification.Episodes.All(static episode => episode.SelectedCount > 0)
            && stratification.SelectedEvents.Count <= MaximumStratifiedQualificationEvents;
        var releaseChallengesPass = challengeDeterministic && challenges.All(static item => item.Passed);
        var noUntargetedCandidateOnlyLateShadow = inverseScan.UntargetedCandidateOnlyLateShadowNodeIds.Count == 0;
        var noUntargetedCandidatePhaseMismatch = inverseScan.UntargetedCandidatePhaseMismatchNodeIds.Count == 0;
        var qualificationPasses = crossProfileQualifies
            && committedDeterministic
            && committedTransparent
            && inverseDeterministic
            && noUntargetedCandidateOnlyLateShadow
            && noUntargetedCandidatePhaseMismatch
            && releaseChallengesPass;

        WriteAuditReports(
            trajectories,
            baselines,
            h16Control,
            stratification,
            policy,
            deterministicRepeat,
            crossProfileQualifies,
            committedObservation,
            committedDeterministic,
            committedTransparent,
            inverseScan,
            inverseDeterministic,
            challenges,
            challengeDeterministic,
            releaseChallengesPass,
            qualificationPasses);
    }

    private static IReadOnlyList<FrozenH17Representative> LoadFrozenH17Representatives()
    {
        var path = Path.Combine(
            FindRepositoryRoot(),
            "eng",
            "frozen-evidence",
            "ordinary",
            "H17_Hotfix6_FrozenQualifiedRepresentativeEvidence.csv");
        var lines = File.ReadAllLines(path);
        Assert.True(lines.Length > 1, "Frozen H.17 representative evidence is empty.");

        return lines
            .Skip(1)
            .Where(static line => !string.IsNullOrWhiteSpace(line))
            .Select(static line =>
            {
                var columns = line.Split(',');
                Assert.True(columns.Length >= 14, "Frozen H.17 representative evidence row is malformed.");
                return new FrozenH17Representative(
                    columns[0],
                    int.Parse(columns[1], NumberStyles.Integer, CultureInfo.InvariantCulture),
                    bool.Parse(columns[3]),
                    bool.Parse(columns[12]));
            })
            .OrderBy(static item => item.ProfileId, StringComparer.Ordinal)
            .ThenBy(static item => item.IntervalIndex)
            .ToArray();
    }

    private static ProfileTrajectory BuildReferenceTrajectory(
        ProfileDefinition profile,
        SemiImplicitHydraulicPrototypeSolver solver)
    {
        var engine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            DesktopSustainedGenerationInitialConditionFactory.CreateNumericalStiffnessEvidenceRuntimeEngine(Step));
        var initialPresentation = engine.CreatePresentationSnapshot(ControlRoomRunState.Running);
        var generatorId = Assert.Single(initialPresentation.Electrical.Generators).GeneratorId;
        var coolingTarget = (ISecondaryTransientFaultTarget)engine;
        var intervals = new List<ReferenceInterval>(profile.IntervalCount);

        for (var index = 1; index <= profile.IntervalCount; index++)
        {
            if (ApplyProfileAction(profile.Kind, index, engine, generatorId, coolingTarget))
            {
                var transition = engine.Step(ControlRoomRunState.Running);
                Assert.False(transition.AnyTripActive, $"Unexpected transition-step trip in H.19 profile {profile.Id} before interval {index}.");
            }

            var start = ToPlantState(engine.LatestCanonicalSnapshot.Control.ProtectedControl.FullPlant.CandidatePlant);
            var presentation = engine.Step(ControlRoomRunState.Running);
            Assert.False(presentation.AnyTripActive, $"Unexpected reference trip in H.19 profile {profile.Id} interval {index}.");
            var end = ToPlantState(engine.LatestCanonicalSnapshot.Control.ProtectedControl.FullPlant.CandidatePlant);
            var hydraulic = solver.Evaluate(start);
            var totalBalances = DeriveInventoryBalances(start, end, Step);
            var frozen = start.FluidNodes.ToDictionary(
                static node => node.Id,
                node => totalBalances[node.Id] - hydraulic.FluidNodeBalances[node.Id],
                StringComparer.Ordinal);
            intervals.Add(new ReferenceInterval(profile.Id, index, start, end, frozen));
            if (index % 1_000 == 0 || index == profile.IntervalCount)
            {
                WriteProgress($"reference-progress profile={profile.Id} interval={index}/{profile.IntervalCount}");
            }
        }

        return new ProfileTrajectory(profile.Id, intervals.ToArray());
    }

    private static bool ApplyProfileAction(
        ProfileKind kind,
        int intervalIndex,
        IControlRoomRuntimeEngine engine,
        string generatorId,
        ISecondaryTransientFaultTarget coolingTarget)
    {
        switch (kind)
        {
            case ProfileKind.Steady:
                return false;
            case ProfileKind.LoadPulse:
                if (intervalIndex == 501)
                {
                    QueueGeneratorLoad(engine, generatorId, ControlRoomCommandKind.GeneratorLoadLower);
                    return true;
                }
                if (intervalIndex == 3_501)
                {
                    QueueGeneratorLoad(engine, generatorId, ControlRoomCommandKind.GeneratorLoadRaise);
                    return true;
                }
                return false;
            case ProfileKind.CoolingPulse:
                if (intervalIndex == 501)
                {
                    coolingTarget.ActivateCondenserCoolingDegradation("h19-cooling-pulse", "cooling", 0.75d);
                    return true;
                }
                if (intervalIndex == 3_501)
                {
                    coolingTarget.ClearSecondaryTransientFault("h19-cooling-pulse");
                    return true;
                }
                return false;
            case ProfileKind.CombinedLoadCooling:
                if (intervalIndex == 501)
                {
                    QueueGeneratorLoad(engine, generatorId, ControlRoomCommandKind.GeneratorLoadLower);
                    return true;
                }
                if (intervalIndex == 1_001)
                {
                    coolingTarget.ActivateCondenserCoolingDegradation("h19-combined-cooling", "cooling", 0.75d);
                    return true;
                }
                if (intervalIndex == 3_501)
                {
                    QueueGeneratorLoad(engine, generatorId, ControlRoomCommandKind.GeneratorLoadRaise);
                    return true;
                }
                if (intervalIndex == 4_001)
                {
                    coolingTarget.ClearSecondaryTransientFault("h19-combined-cooling");
                    return true;
                }
                return false;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind));
        }
    }

    private static void QueueGeneratorLoad(IControlRoomRuntimeEngine engine, string generatorId, ControlRoomCommandKind kind)
        => engine.QueueOperatorCommand(new ControlRoomCommand(kind, generatorId, ControlRoomCommandTargetKind.Generator));

    private static ProfileBaseline EvaluatePrimaryGate(
        ProfileTrajectory trajectory,
        HybridSemiImplicitHydraulicGateSolver gate)
    {
        var options = new HybridSemiImplicitHydraulicGateOptions(PressureTrigger, FlowTriggerKilogramsPerSecond, H4Primary);
        var events = new List<BaselineTriggerEvent>();
        foreach (var interval in trajectory.Intervals)
        {
            var result = gate.Step(interval.Start, Step, interval.FrozenNonHydraulicBalances, options);
            if (result.UsedSemiImplicitCorrection)
            {
                events.Add(new BaselineTriggerEvent(interval.ProfileId, interval.Index, result));
            }
        }

        return new ProfileBaseline(trajectory.ProfileId, events.ToArray());
    }

    private static PolicyRun RunPolicy(
        IReadOnlyList<BaselineTriggerEvent> baseline,
        IReadOnlyDictionary<(string ProfileId, int IntervalIndex), ReferenceInterval> intervals,
        SimplifiedWaterSteamThermodynamicModel productionThermodynamics,
        int totalReferenceIntervals)
    {
        var events = new List<PolicyEvent>(baseline.Count);
        var completed = 0;
        foreach (var item in baseline)
        {
            var interval = intervals[(item.ProfileId, item.IntervalIndex)];
            var shadowThermodynamics = new ThermodynamicBranchContinuityModel(
                productionThermodynamics,
                productionThermodynamics,
                ThermodynamicBranchContinuityOptions.H13BoundedHysteresis,
                H19TargetNodeIds);
            var solver = new JacobianHydraulicCorrectorSolver(shadowThermodynamics);
            var result = solver.Step(
                interval.Start,
                Step,
                interval.FrozenNonHydraulicBalances,
                JacobianHydraulicCorrectorOptions.H9AuditDefault);
            events.Add(new PolicyEvent(item.ProfileId, item.IntervalIndex, result, shadowThermodynamics.Decisions.ToArray()));
            completed++;
            if (baseline.Count >= 50 && (completed % 25 == 0 || completed == baseline.Count))
            {
                WriteProgress($"policy-evaluation-progress completed={completed}/{baseline.Count}");
            }
        }

        var evaluationSum = events.Sum(static item => item.Result.HydraulicEvaluationCount);
        var decisions = events.SelectMany(static item => item.Decisions).ToArray();
        return new PolicyRun(
            events.ToArray(),
            (totalReferenceIntervals + evaluationSum) / (double)totalReferenceIntervals,
            decisions.Count(static item => item.SelectionDiffersFromProduction),
            decisions.Count(static item => item.SelectedPreviousPhase),
            decisions.Count(static item => string.Equals(item.DecisionKind, "production-hysteresis-release", StringComparison.Ordinal)),
            MaximumFinite(decisions.Select(static item => item.AvoidedProductionPressureDifferencePascals)),
            MaximumFinite(decisions.Select(static item => item.AvoidedProductionTemperatureDifferenceKelvins)),
            events.Count == 0 ? 0d : events.Max(static item => Math.Abs(item.Result.AppliedHydraulicMassRateClosureResidualKilogramsPerSecond)),
            events.Count == 0 ? 0d : events.Max(static item => Math.Abs(item.Result.AppliedHydraulicEnergyOwnershipResidualWatts)));
    }

    private static CommittedObservation ObserveCommittedTargetSelection(
        IReadOnlyList<ProfileTrajectory> trajectories,
        SimplifiedWaterSteamThermodynamicModel productionThermodynamics,
        IReadOnlySet<(string ProfileId, int IntervalIndex)> triggerKeys)
    {
        var rows = new List<CommittedObservationRow>();
        var committedPhaseTransitions = 0;
        var committedPhaseStateChecks = 0;

        foreach (var profile in trajectories)
        {
            var shadow = new ThermodynamicBranchContinuityModel(
                productionThermodynamics,
                productionThermodynamics,
                ThermodynamicBranchContinuityOptions.H13BoundedHysteresis,
                H19TargetNodeIds);
            var previousCommittedPhases = new Dictionary<string, FluidPhase>(StringComparer.Ordinal);

            foreach (var interval in profile.Intervals)
            {
                foreach (var nodeId in H19TargetNodeIds)
                {
                    var node = Assert.Single(interval.Start.FluidNodes, item => string.Equals(item.Id, nodeId, StringComparison.Ordinal));
                    committedPhaseStateChecks++;
                    var transition = previousCommittedPhases.TryGetValue(nodeId, out var previousPhase) && previousPhase != node.Phase;
                    if (transition)
                    {
                        committedPhaseTransitions++;
                    }
                    previousCommittedPhases[nodeId] = node.Phase;

                    var observeSelection = interval.Index == 1
                        || interval.Index % ObservationStride == 0
                        || transition
                        || triggerKeys.Contains((interval.ProfileId, interval.Index));
                    if (!observeSelection)
                    {
                        continue;
                    }

                    var decisionStart = shadow.Decisions.Count;
                    var selected = shadow.Resolve(node.Definition, node.Inventory, node.Thermodynamics);
                    var decision = Assert.Single(shadow.Decisions.Skip(decisionStart));
                    rows.Add(new CommittedObservationRow(
                        interval.ProfileId,
                        interval.Index,
                        nodeId,
                        node.Phase.ToString(),
                        decision.ProductionPhase,
                        selected.Phase.ToString(),
                        decision.DecisionKind,
                        decision.SelectionDiffersFromProduction,
                        decision.SelectedPreviousPhase,
                        transition,
                        decision.PreviousPhaseRelativePressureDrift,
                        decision.PreviousPhaseTemperatureDriftKelvins));
                }
            }
        }

        return new CommittedObservation(rows.ToArray(), committedPhaseTransitions, committedPhaseStateChecks);
    }

    private static InverseBranchScan ScanTriggeredCandidateAndExplicitInverseBranches(
        PolicyRun policy,
        IReadOnlyDictionary<(string ProfileId, int IntervalIndex), ReferenceInterval> intervals,
        SimplifiedWaterSteamThermodynamicModel productionThermodynamics)
    {
        var rows = new List<InverseBranchScanRow>();
        var untargetedCandidateOnlyLateShadow = new HashSet<string>(StringComparer.Ordinal);
        var untargetedCandidatePhaseMismatch = new HashSet<string>(StringComparer.Ordinal);

        var completedEvents = 0;
        foreach (var item in policy.Events)
        {
            var reference = intervals[(item.ProfileId, item.IntervalIndex)];
            var explicitNodes = reference.End.FluidNodes.ToDictionary(static node => node.Id, StringComparer.Ordinal);
            foreach (var candidateNode in item.Result.CandidateState.FluidNodes.OrderBy(static node => node.Id, StringComparer.Ordinal))
            {
                var explicitNode = explicitNodes[candidateNode.Id];
                var candidate = productionThermodynamics.DiagnoseInverseBranchSelection(
                    candidateNode.Definition,
                    candidateNode.Inventory,
                    candidateNode.Thermodynamics);
                var explicitEnd = productionThermodynamics.DiagnoseInverseBranchSelection(
                    explicitNode.Definition,
                    explicitNode.Inventory,
                    explicitNode.Thermodynamics);
                var targeted = H19TargetNodeIds.Contains(candidateNode.Id, StringComparer.Ordinal);
                var candidateOnlyLateShadow = candidate.LateBoundarySaturatedShadowedByEarlierSuperheated
                    && !explicitEnd.LateBoundarySaturatedShadowedByEarlierSuperheated;
                if (!targeted && candidateOnlyLateShadow)
                {
                    untargetedCandidateOnlyLateShadow.Add(candidateNode.Id);
                }
                var candidatePhaseMismatch = !string.Equals(
                    candidate.ProductionSelectedPhase,
                    explicitEnd.ProductionSelectedPhase,
                    StringComparison.Ordinal);
                if (!targeted && candidatePhaseMismatch)
                {
                    untargetedCandidatePhaseMismatch.Add(candidateNode.Id);
                }

                rows.Add(new InverseBranchScanRow(
                    item.ProfileId,
                    item.IntervalIndex,
                    candidateNode.Id,
                    targeted,
                    candidate.ProductionSelectedBranch,
                    candidate.ProductionSelectedPhase,
                    candidate.MultiplePhaseRootsAvailable,
                    candidate.LateBoundarySaturatedShadowedByEarlierSuperheated,
                    explicitEnd.ProductionSelectedBranch,
                    explicitEnd.ProductionSelectedPhase,
                    explicitEnd.MultiplePhaseRootsAvailable,
                    explicitEnd.LateBoundarySaturatedShadowedByEarlierSuperheated,
                    candidateOnlyLateShadow));
            }

            completedEvents++;
            if (policy.Events.Count >= 50 && (completedEvents % 25 == 0 || completedEvents == policy.Events.Count))
            {
                WriteProgress($"inverse-scan-progress completed={completedEvents}/{policy.Events.Count}");
            }
        }

        return new InverseBranchScan(
            rows.ToArray(),
            untargetedCandidateOnlyLateShadow.Order(StringComparer.Ordinal).ToArray(),
            untargetedCandidatePhaseMismatch.Order(StringComparer.Ordinal).ToArray());
    }

    private static TriggerStratification StratifyTriggerEvents(IReadOnlyList<ProfileBaseline> baselines)
    {
        var selected = new Dictionary<(string ProfileId, int IntervalIndex), BaselineTriggerEvent>();
        var reasons = new Dictionary<(string ProfileId, int IntervalIndex), HashSet<string>>();
        var episodeSeeds = new List<TriggerEpisodeSeed>();

        void Select(BaselineTriggerEvent item, string reason)
        {
            var key = (item.ProfileId, item.IntervalIndex);
            selected[key] = item;
            if (!reasons.TryGetValue(key, out var itemReasons))
            {
                itemReasons = new HashSet<string>(StringComparer.Ordinal);
                reasons.Add(key, itemReasons);
            }
            itemReasons.Add(reason);
        }

        foreach (var baseline in baselines)
        {
            var ordered = baseline.Events.OrderBy(static item => item.IntervalIndex).ToArray();
            if (ordered.Length == 0)
            {
                continue;
            }

            if (string.Equals(baseline.ProfileId, "steady-long", StringComparison.Ordinal))
            {
                foreach (var item in ordered.Where(static item => item.IntervalIndex <= H16ControlIntervalCount))
                {
                    Select(item, "h16-control");
                }
            }

            var episodeStart = 0;
            var episodeIndex = 1;
            for (var index = 1; index <= ordered.Length; index++)
            {
                var closesEpisode = index == ordered.Length
                    || ordered[index].IntervalIndex - ordered[index - 1].IntervalIndex > TriggerEpisodeMaximumQuietGapIntervals;
                if (!closesEpisode)
                {
                    continue;
                }

                var episodeEvents = ordered[episodeStart..index];
                episodeSeeds.Add(new TriggerEpisodeSeed(baseline.ProfileId, episodeIndex, episodeEvents));
                Select(episodeEvents[0], $"episode-{episodeIndex}-first");
                Select(episodeEvents[^1], $"episode-{episodeIndex}-last");
                Select(episodeEvents.MaxBy(CombinedQualificationSeverity)!, $"episode-{episodeIndex}-hardest");

                episodeStart = index;
                episodeIndex++;
            }

            foreach (var actionInterval in ProfileActionIntervals(baseline.ProfileId))
            {
                var before = ordered.LastOrDefault(item => item.IntervalIndex <= actionInterval);
                var after = ordered.FirstOrDefault(item => item.IntervalIndex >= actionInterval);
                if (before is not null)
                {
                    Select(before, $"action-{actionInterval}-before");
                }
                if (after is not null)
                {
                    Select(after, $"action-{actionInterval}-after");
                }
            }
        }

        if (selected.Count > MaximumStratifiedQualificationEvents)
        {
            throw new InvalidOperationException(
                $"H.19 trigger-episode mandatory representatives require {selected.Count} H.9 solves, above the bounded stratified qualification budget of {MaximumStratifiedQualificationEvents}. Increase episode coalescing only with explicit evidence; do not silently drop mandatory episode/control representatives.");
        }

        for (var quartile = 1; quartile <= 3 && selected.Count < MaximumStratifiedQualificationEvents; quartile++)
        {
            foreach (var episode in episodeSeeds)
            {
                if (selected.Count >= MaximumStratifiedQualificationEvents || episode.Events.Count < 4)
                {
                    continue;
                }

                var position = (int)Math.Round(
                    quartile * (episode.Events.Count - 1d) / 4d,
                    MidpointRounding.AwayFromZero);
                Select(episode.Events[position], $"episode-{episode.EpisodeIndex}-quartile-{quartile}");
            }
        }

        var orderedByProfile = baselines.ToDictionary(
            static baseline => baseline.ProfileId,
            static baseline => baseline.Events.OrderBy(static item => item.IntervalIndex).ToArray(),
            StringComparer.Ordinal);
        for (var sample = 0; sample < MinimumTemporalSamplesPerProfile && selected.Count < MaximumStratifiedQualificationEvents; sample++)
        {
            foreach (var baseline in baselines)
            {
                if (selected.Count >= MaximumStratifiedQualificationEvents)
                {
                    break;
                }

                var ordered = orderedByProfile[baseline.ProfileId];
                if (ordered.Length == 0)
                {
                    continue;
                }

                var temporalTarget = Math.Min(MinimumTemporalSamplesPerProfile, ordered.Length);
                if (sample >= temporalTarget)
                {
                    continue;
                }

                var position = temporalTarget == 1
                    ? 0
                    : (int)Math.Round(
                        sample * (ordered.Length - 1d) / (temporalTarget - 1d),
                        MidpointRounding.AwayFromZero);
                Select(ordered[position], "profile-temporal-stratification");
            }
        }

        var selectedEvents = selected.Values
            .OrderBy(static item => item.ProfileId, StringComparer.Ordinal)
            .ThenBy(static item => item.IntervalIndex)
            .ToArray();
        var episodes = episodeSeeds
            .Select(seed => new TriggerEpisode(
                seed.ProfileId,
                seed.EpisodeIndex,
                seed.Events[0].IntervalIndex,
                seed.Events[^1].IntervalIndex,
                seed.Events.Count,
                seed.Events.Count(item => selected.ContainsKey((item.ProfileId, item.IntervalIndex))),
                seed.Events.Max(TriggerSeverity),
                seed.Events.Max(H4ResidualSeverity)))
            .ToArray();
        var reasonMap = reasons.ToDictionary(
            static item => item.Key,
            static item => string.Join('|', item.Value.Order(StringComparer.Ordinal)));

        return new TriggerStratification(
            baselines.Sum(static item => item.Events.Count),
            selectedEvents,
            episodes,
            reasonMap);
    }

    private static double TriggerSeverity(BaselineTriggerEvent item)
        => MaximumSeverity(
            item.PrimaryResult.PredictorMaximumFractionalSubcooledPressureChange / PressureTrigger,
            item.PrimaryResult.PredictorMaximumAbsoluteHydraulicFlowChangeKilogramsPerSecond / FlowTriggerKilogramsPerSecond);

    private static double H4ResidualSeverity(BaselineTriggerEvent item)
        => MaximumSeverity(
            item.PrimaryResult.MaximumRelativePressureResidual / H4Primary.RelativePressureTolerance,
            item.PrimaryResult.MaximumAbsoluteFlowResidualKilogramsPerSecond / H4Primary.AbsoluteFlowToleranceKilogramsPerSecond);

    private static double MaximumSeverity(double first, double second)
    {
        var safeFirst = double.IsFinite(first) ? first : double.PositiveInfinity;
        var safeSecond = double.IsFinite(second) ? second : double.PositiveInfinity;
        return Math.Max(safeFirst, safeSecond);
    }

    private static double CombinedQualificationSeverity(BaselineTriggerEvent item)
        => Math.Max(TriggerSeverity(item), H4ResidualSeverity(item));

    private static IReadOnlyList<int> ProfileActionIntervals(string profileId)
        => profileId switch
        {
            "steady-long" => Array.Empty<int>(),
            "load-pulse" => new[] { 501, 3_501 },
            "cooling-pulse" => new[] { 501, 3_501 },
            "combined-load-cooling" => new[] { 501, 1_001, 3_501, 4_001 },
            _ => throw new ArgumentOutOfRangeException(nameof(profileId)),
        };

    private static IReadOnlyList<BaselineTriggerEvent> SelectDeterminismSentinels(
        IReadOnlyList<ProfileBaseline> baselines)
    {
        var selected = new Dictionary<(string ProfileId, int IntervalIndex), BaselineTriggerEvent>();
        foreach (var baseline in baselines)
        {
            var ordered = baseline.Events.OrderBy(static item => item.IntervalIndex).ToArray();
            if (ordered.Length == 0)
            {
                continue;
            }

            for (var sentinel = 0; sentinel < DeterminismSentinelsPerProfile; sentinel++)
            {
                var position = DeterminismSentinelsPerProfile == 1
                    ? 0
                    : (int)Math.Round(
                        sentinel * (ordered.Length - 1d) / (DeterminismSentinelsPerProfile - 1d),
                        MidpointRounding.AwayFromZero);
                var item = ordered[position];
                selected[(item.ProfileId, item.IntervalIndex)] = item;
            }

            if (string.Equals(baseline.ProfileId, "steady-long", StringComparison.Ordinal))
            {
                var interval723 = ordered.FirstOrDefault(static item => item.IntervalIndex == 723);
                if (interval723 is not null)
                {
                    selected[(interval723.ProfileId, interval723.IntervalIndex)] = interval723;
                }
            }
        }

        return selected.Values
            .OrderBy(static item => item.ProfileId, StringComparer.Ordinal)
            .ThenBy(static item => item.IntervalIndex)
            .ToArray();
    }

    private static IReadOnlySet<(string ProfileId, int IntervalIndex)> SelectObservationDeterminismKeys(
        IReadOnlyList<ProfileTrajectory> trajectories,
        IReadOnlySet<(string ProfileId, int IntervalIndex)> triggerSentinelKeys)
    {
        var keys = new HashSet<(string ProfileId, int IntervalIndex)>(triggerSentinelKeys);
        foreach (var profile in trajectories)
        {
            keys.Add((profile.ProfileId, 1));
            keys.Add((profile.ProfileId, profile.Intervals.Count));
            for (var index = 1_000; index <= profile.Intervals.Count; index += 1_000)
            {
                keys.Add((profile.ProfileId, index));
            }
        }

        return keys;
    }

    private static CommittedObservation ObserveCommittedTargetSelectionAtKeys(
        IReadOnlyDictionary<(string ProfileId, int IntervalIndex), ReferenceInterval> intervals,
        SimplifiedWaterSteamThermodynamicModel productionThermodynamics,
        IReadOnlySet<(string ProfileId, int IntervalIndex)> keys)
    {
        var rows = new List<CommittedObservationRow>(keys.Count * H19TargetNodeIds.Length);
        foreach (var key in keys.OrderBy(static item => item.ProfileId, StringComparer.Ordinal).ThenBy(static item => item.IntervalIndex))
        {
            var interval = intervals[key];
            foreach (var nodeId in H19TargetNodeIds)
            {
                var node = Assert.Single(interval.Start.FluidNodes, item => string.Equals(item.Id, nodeId, StringComparison.Ordinal));
                var shadow = new ThermodynamicBranchContinuityModel(
                    productionThermodynamics,
                    productionThermodynamics,
                    ThermodynamicBranchContinuityOptions.H13BoundedHysteresis,
                    H19TargetNodeIds);
                var selected = shadow.Resolve(node.Definition, node.Inventory, node.Thermodynamics);
                var decision = Assert.Single(shadow.Decisions);
                rows.Add(new CommittedObservationRow(
                    interval.ProfileId,
                    interval.Index,
                    nodeId,
                    node.Phase.ToString(),
                    decision.ProductionPhase,
                    selected.Phase.ToString(),
                    decision.DecisionKind,
                    decision.SelectionDiffersFromProduction,
                    decision.SelectedPreviousPhase,
                    false,
                    decision.PreviousPhaseRelativePressureDrift,
                    decision.PreviousPhaseTemperatureDriftKelvins));
            }
        }

        return new CommittedObservation(rows.ToArray(), 0, rows.Count);
    }

    private static IReadOnlyList<ReleaseChallenge> RunReleaseChallenges(
        SimplifiedWaterSteamThermodynamicModel productionThermodynamics)
        => new[]
        {
            RunReleaseChallenge(
                "steam-near-hold",
                productionThermodynamics,
                new FluidNodeDefinition("steam", Volume.FromCubicMetres(100d)),
                new FluidNodeInventory(Mass.FromKilograms(SteamMassKilograms), Energy.FromJoules(SteamEnergyJoules + SteamEnergyProbeJoules)),
                new FluidThermodynamicState(Pressure.FromPascals(6362325.9673817037d), Temperature.FromKelvins(552.58890484070866d), FluidPhase.SaturatedMixture, VaporQuality.FromFraction(0.98827242641541357d)),
                FluidPhase.SuperheatedVapor,
                FluidPhase.SaturatedMixture,
                "hold-previous-phase-hysteresis"),
            RunReleaseChallenge(
                "steam-distant-release",
                productionThermodynamics,
                new FluidNodeDefinition("steam", Volume.FromCubicMetres(100d)),
                new FluidNodeInventory(Mass.FromKilograms(SteamMassKilograms), Energy.FromJoules(SteamEnergyJoules + SteamEnergyProbeJoules)),
                new FluidThermodynamicState(Pressure.FromPascals(5_000_000d), Temperature.FromKelvins(530d), FluidPhase.SaturatedMixture, VaporQuality.FromFraction(0.95d)),
                FluidPhase.SuperheatedVapor,
                FluidPhase.SuperheatedVapor,
                "production-hysteresis-release"),
            RunReleaseChallenge(
                "stop-out-near-hold",
                productionThermodynamics,
                new FluidNodeDefinition("stop-out", Volume.FromCubicMetres(100d)),
                new FluidNodeInventory(Mass.FromKilograms(StopOutMassKilograms), Energy.FromJoules(StopOutEnergyJoules - StopOutEnergyProbeJoules)),
                new FluidThermodynamicState(Pressure.FromPascals(8601730.4979163781d), Temperature.FromKelvins(588.83285718179309d), FluidPhase.SuperheatedVapor, null),
                FluidPhase.SaturatedMixture,
                FluidPhase.SuperheatedVapor,
                "hold-previous-phase-hysteresis"),
            RunReleaseChallenge(
                "stop-out-distant-release",
                productionThermodynamics,
                new FluidNodeDefinition("stop-out", Volume.FromCubicMetres(100d)),
                new FluidNodeInventory(Mass.FromKilograms(StopOutMassKilograms), Energy.FromJoules(StopOutEnergyJoules - StopOutEnergyProbeJoules)),
                new FluidThermodynamicState(Pressure.FromPascals(11_000_000d), Temperature.FromKelvins(620d), FluidPhase.SuperheatedVapor, null),
                FluidPhase.SaturatedMixture,
                FluidPhase.SaturatedMixture,
                "production-hysteresis-release"),
        };

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
            H19TargetNodeIds);
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

    private static bool QualifiesPolicy(PolicyRun run, bool deterministicRepeat, int expectedEventCount)
    {
        var options = JacobianHydraulicCorrectorOptions.H9AuditDefault;
        return deterministicRepeat
            && expectedEventCount >= 15
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

    private static double MaximumFinite(IEnumerable<double> values)
    {
        var finite = values.Where(double.IsFinite).ToArray();
        return finite.Length == 0 ? 0d : finite.Max();
    }

    private static string Fingerprint(
        PolicyRun run,
        IReadOnlySet<(string ProfileId, int IntervalIndex)>? keys = null)
        => string.Join(
            "||",
            run.Events
                .Where(item => keys is null || keys.Contains((item.ProfileId, item.IntervalIndex)))
                .OrderBy(static item => item.ProfileId, StringComparer.Ordinal)
                .ThenBy(static item => item.IntervalIndex)
                .Select(item => string.Join(
                "|",
                item.ProfileId,
                item.IntervalIndex,
                item.Result.Converged,
                item.Result.LineSearchExhausted,
                item.Result.MaximumRelativePressureFixedPointResidual.ToString("G17", CultureInfo.InvariantCulture),
                item.Result.MaximumAbsoluteFlowFixedPointResidualKilogramsPerSecond.ToString("G17", CultureInfo.InvariantCulture),
                item.Result.NormalizedMeritResidual.ToString("G17", CultureInfo.InvariantCulture),
                string.Join(";", item.Decisions.Select(DecisionFingerprint)))));

    private static string DecisionFingerprint(ThermodynamicBranchContinuityDecision decision)
        => FormattableString.Invariant($"{decision.Sequence}:{decision.NodeId}:{decision.PreviousPhase}:{decision.ProductionPhase}:{decision.SelectedPhase}:{decision.DecisionKind}:{decision.PreviousPhaseRelativePressureDrift:G17}:{decision.PreviousPhaseTemperatureDriftKelvins:G17}");

    private static string ObservationFingerprint(
        CommittedObservation observation,
        IReadOnlySet<(string ProfileId, int IntervalIndex)>? keys = null)
        => string.Join(
            "||",
            observation.Rows
                .Where(item => keys is null || keys.Contains((item.ProfileId, item.IntervalIndex)))
                .OrderBy(static item => item.ProfileId, StringComparer.Ordinal)
                .ThenBy(static item => item.IntervalIndex)
                .ThenBy(static item => item.NodeId, StringComparer.Ordinal)
                .Select(static item => FormattableString.Invariant(
                    $"{item.ProfileId}:{item.IntervalIndex}:{item.NodeId}:{item.CommittedPhase}:{item.ProductionPhase}:{item.SelectedPhase}:{item.DecisionKind}:{item.RelativePressureDrift:G17}:{item.TemperatureDriftKelvins:G17}")));

    private static string InverseScanFingerprint(
        InverseBranchScan scan,
        IReadOnlySet<(string ProfileId, int IntervalIndex)>? keys = null)
        => string.Join(
            "||",
            scan.Rows
                .Where(item => keys is null || keys.Contains((item.ProfileId, item.IntervalIndex)))
                .OrderBy(static item => item.ProfileId, StringComparer.Ordinal)
                .ThenBy(static item => item.IntervalIndex)
                .ThenBy(static item => item.NodeId, StringComparer.Ordinal)
                .Select(static item => FormattableString.Invariant(
                    $"{item.ProfileId}:{item.IntervalIndex}:{item.NodeId}:{item.Targeted}:{item.CandidateSelectedBranch}:{item.CandidateSelectedPhase}:{item.CandidateMultipleRoots}:{item.CandidateLateShadow}:{item.ExplicitSelectedBranch}:{item.ExplicitSelectedPhase}:{item.ExplicitMultipleRoots}:{item.ExplicitLateShadow}:{item.CandidateOnlyLateShadow}")));

    private static string ChallengeFingerprint(IReadOnlyList<ReleaseChallenge> challenges)
        => string.Join(
            "||",
            challenges.Select(static item => FormattableString.Invariant(
                $"{item.Name}:{item.NodeId}:{item.PreviousPhase}:{item.ProductionPhase}:{item.SelectedPhase}:{item.ActualDecisionKind}:{item.RelativePressureDrift:G17}:{item.TemperatureDriftKelvins:G17}:{item.Passed}")));

    private static void WriteAuditReports(
        IReadOnlyList<ProfileTrajectory> trajectories,
        IReadOnlyList<ProfileBaseline> baselines,
        PolicyRun h16Control,
        TriggerStratification stratification,
        PolicyRun policy,
        bool deterministicRepeat,
        bool crossProfileQualifies,
        CommittedObservation observation,
        bool observationDeterministic,
        bool committedTransparent,
        InverseBranchScan inverseScan,
        bool inverseDeterministic,
        IReadOnlyList<ReleaseChallenge> challenges,
        bool challengeDeterministic,
        bool releaseChallengesPass,
        bool qualificationPasses)
    {
        var root = FindRepositoryRoot();
        var directory = Path.Combine(root, "artifacts", "h19-four-node-long-horizon-cross-profile-qualification");
        Directory.CreateDirectory(directory);

        var profileRows = new List<string>
        {
            "profile,reference_intervals,census_trigger_events,post_500_trigger_events,trigger_episodes,qualified_trigger_samples,converged,line_search_exhausted,branch_overrides,previous_phase_holds,hysteresis_releases,max_pressure_residual,max_flow_residual_kg_s,max_normalized_merit",
        };
        foreach (var profile in trajectories)
        {
            var baseline = Assert.Single(baselines, item => string.Equals(item.ProfileId, profile.ProfileId, StringComparison.Ordinal));
            var events = policy.Events.Where(item => string.Equals(item.ProfileId, profile.ProfileId, StringComparison.Ordinal)).ToArray();
            var decisions = events.SelectMany(static item => item.Decisions).ToArray();
            var hysteresisReleases = decisions.Count(static item =>
                string.Equals(item.DecisionKind, "production-hysteresis-release", StringComparison.Ordinal));
            var profileEpisodes = stratification.Episodes.Count(item => string.Equals(item.ProfileId, profile.ProfileId, StringComparison.Ordinal));
            profileRows.Add(FormattableString.Invariant(
                $"{profile.ProfileId},{profile.Intervals.Count},{baseline.Events.Count},{baseline.Events.Count(static item => item.IntervalIndex > 500)},{profileEpisodes},{events.Length},{events.Count(static item => item.Result.Converged)},{events.Count(static item => item.Result.LineSearchExhausted)},{decisions.Count(static item => item.SelectionDiffersFromProduction)},{decisions.Count(static item => item.SelectedPreviousPhase)},{hysteresisReleases},{MaximumFinite(events.Select(static item => item.Result.MaximumRelativePressureFixedPointResidual)):G17},{MaximumFinite(events.Select(static item => item.Result.MaximumAbsoluteFlowFixedPointResidualKilogramsPerSecond)):G17},{MaximumFinite(events.Select(static item => item.Result.NormalizedMeritResidual)):G17}"));
        }

        var triggerRows = new List<string>
        {
            "profile,interval,selection_reasons,h4_primary_converged,h19_converged,line_search_exhausted,pressure_residual,flow_residual_kg_s,normalized_merit,hydraulic_evaluations,branch_decisions,branch_overrides,previous_phase_holds,hysteresis_releases",
        };
        foreach (var trigger in stratification.SelectedEvents)
        {
            var item = Assert.Single(policy.Events, candidate =>
                string.Equals(candidate.ProfileId, trigger.ProfileId, StringComparison.Ordinal)
                && candidate.IntervalIndex == trigger.IntervalIndex);
            var hysteresisReleases = item.Decisions.Count(static decision =>
                string.Equals(decision.DecisionKind, "production-hysteresis-release", StringComparison.Ordinal));
            var reasons = stratification.SelectionReasons[(trigger.ProfileId, trigger.IntervalIndex)];
            triggerRows.Add(FormattableString.Invariant(
                $"{item.ProfileId},{item.IntervalIndex},{reasons},{trigger.PrimaryResult.Converged},{item.Result.Converged},{item.Result.LineSearchExhausted},{item.Result.MaximumRelativePressureFixedPointResidual:G17},{item.Result.MaximumAbsoluteFlowFixedPointResidualKilogramsPerSecond:G17},{item.Result.NormalizedMeritResidual:G17},{item.Result.HydraulicEvaluationCount},{item.Decisions.Count},{item.Decisions.Count(static decision => decision.SelectionDiffersFromProduction)},{item.Decisions.Count(static decision => decision.SelectedPreviousPhase)},{hysteresisReleases}"));
        }

        var episodeRows = new List<string>
        {
            "profile,episode,start_interval,end_interval,span_intervals,census_triggers,selected_samples,max_trigger_severity,max_h4_residual_severity",
        };
        episodeRows.AddRange(stratification.Episodes.Select(static item => FormattableString.Invariant(
            $"{item.ProfileId},{item.EpisodeIndex},{item.StartInterval},{item.EndInterval},{item.EndInterval - item.StartInterval + 1},{item.TriggerCount},{item.SelectedCount},{item.MaximumTriggerSeverity:G17},{item.MaximumH4ResidualSeverity:G17}")));

        var observationRows = new List<string>
        {
            "profile,interval,node,committed_phase,production_reresolve_phase,hysteresis_selected_phase,decision_kind,selection_differs_from_production,selected_previous_phase,committed_phase_transition,relative_pressure_drift,temperature_drift_K",
        };
        observationRows.AddRange(observation.Rows.Select(static item => FormattableString.Invariant(
            $"{item.ProfileId},{item.IntervalIndex},{item.NodeId},{item.CommittedPhase},{item.ProductionPhase},{item.SelectedPhase},{item.DecisionKind},{item.SelectionDiffersFromProduction},{item.SelectedPreviousPhase},{item.CommittedPhaseTransition},{item.RelativePressureDrift:G17},{item.TemperatureDriftKelvins:G17}")));

        var inverseRows = new List<string>
        {
            "profile,interval,node,targeted,candidate_selected_branch,candidate_selected_phase,candidate_multiple_roots,candidate_late_boundary_saturated_shadow,explicit_selected_branch,explicit_selected_phase,explicit_multiple_roots,explicit_late_boundary_saturated_shadow,candidate_phase_mismatch,candidate_only_late_shadow",
        };
        inverseRows.AddRange(inverseScan.Rows.Select(static item => FormattableString.Invariant(
            $"{item.ProfileId},{item.IntervalIndex},{item.NodeId},{item.Targeted},{item.CandidateSelectedBranch},{item.CandidateSelectedPhase},{item.CandidateMultipleRoots},{item.CandidateLateShadow},{item.ExplicitSelectedBranch},{item.ExplicitSelectedPhase},{item.ExplicitMultipleRoots},{item.ExplicitLateShadow},{!string.Equals(item.CandidateSelectedPhase, item.ExplicitSelectedPhase, StringComparison.Ordinal)},{item.CandidateOnlyLateShadow}")));

        var challengeRows = new List<string>
        {
            "name,node,previous_phase,production_phase,selected_phase,expected_decision,actual_decision,relative_pressure_drift,temperature_drift_K,passed",
        };
        challengeRows.AddRange(challenges.Select(static item => FormattableString.Invariant(
            $"{item.Name},{item.NodeId},{item.PreviousPhase},{item.ProductionPhase},{item.SelectedPhase},{item.ExpectedDecisionKind},{item.ActualDecisionKind},{item.RelativePressureDrift:G17},{item.TemperatureDriftKelvins:G17},{item.Passed}")));

        var totalIntervals = trajectories.Sum(static item => item.Intervals.Count);
        var totalTriggers = baselines.Sum(static item => item.Events.Count);
        var qualifiedTriggerSamples = stratification.SelectedEvents.Count;
        var converged = policy.Events.Count(static item => item.Result.Converged);
        var exhausted = policy.Events.Count(static item => item.Result.LineSearchExhausted);
        var h16Interval723 = Assert.Single(h16Control.Events, static item => item.IntervalIndex == 723);
        var h16HeaderOverrides = h16Interval723.Decisions.Count(static item =>
            string.Equals(item.NodeId, "header", StringComparison.Ordinal)
            && item.SelectionDiffersFromProduction);
        var challengeHolds = challenges.Count(static item => string.Equals(item.ActualDecisionKind, "hold-previous-phase-hysteresis", StringComparison.Ordinal));
        var challengeReleases = challenges.Count(static item => string.Equals(item.ActualDecisionKind, "production-hysteresis-release", StringComparison.Ordinal));
        var stratifiedBaselines = baselines
            .Select(baseline => new ProfileBaseline(
                baseline.ProfileId,
                stratification.SelectedEvents
                    .Where(item => string.Equals(item.ProfileId, baseline.ProfileId, StringComparison.Ordinal))
                    .ToArray()))
            .ToArray();
        var determinismSentinelCount = SelectDeterminismSentinels(stratifiedBaselines).Count;
        var frozenH17Representatives = LoadFrozenH17Representatives();
        var frozenH17ByKey = frozenH17Representatives.ToDictionary(static item => (item.ProfileId, item.IntervalIndex));
        var recoveredH17Failures = policy.Events.Count(item =>
            !frozenH17ByKey[(item.ProfileId, item.IntervalIndex)].H17Converged
            && item.Result.Converged);
        var preservedH17Successes = policy.Events.Count(item =>
            frozenH17ByKey[(item.ProfileId, item.IntervalIndex)].H17Converged
            && item.Result.Converged);
        var recoveredH17MismatchFailures = policy.Events.Count(item =>
            !frozenH17ByKey[(item.ProfileId, item.IntervalIndex)].H17Converged
            && frozenH17ByKey[(item.ProfileId, item.IntervalIndex)].TurbineInletPhaseMismatch
            && item.Result.Converged);
        var recoveredH17NoMismatchFailures = policy.Events.Count(item =>
            !frozenH17ByKey[(item.ProfileId, item.IntervalIndex)].H17Converged
            && !frozenH17ByKey[(item.ProfileId, item.IntervalIndex)].TurbineInletPhaseMismatch
            && item.Result.Converged);
        var representativeKeysMatchFrozenH17 = frozenH17Representatives
            .Select(static item => (item.ProfileId, item.IntervalIndex))
            .ToHashSet()
            .SetEquals(stratification.SelectedEvents.Select(static item => (item.ProfileId, item.IntervalIndex)));
        var recommendation = qualificationPasses
            ? "H.19 recommendation: the unchanged four-node 2%/5 K bounded previous-phase hysteresis policy qualifies the deterministic trigger-episode-stratified sample over the exhaustive 30000-interval P060/F040 census, preserves committed-state transparency and exposes no new untargeted candidate-only late saturated-root shadow or candidate-vs-explicit phase-mismatch node in the qualified representatives. Keep production explicit; before activation, retain this census+stratified qualification contract and rollback/shadow telemetry."
            : "H.19 recommendation: long-horizon/cross-profile stratified qualification is not yet complete. Keep production explicit and inspect the failing representative, trigger episode or newly discovered untargeted branch-disagreement node before changing the policy or designing activation.";
        var auditPasses = h16Control.Events.Count == 15
            && h16Control.Events.All(static item => item.Result.Converged)
            && h16HeaderOverrides > 0
            && baselines.Count == Profiles.Length
            && totalTriggers == ExpectedH17CensusTriggerEvents
            && stratification.Episodes.Count == ExpectedH17TriggerEpisodes
            && qualifiedTriggerSamples == ExpectedH17QualifiedRepresentatives
            && representativeKeysMatchFrozenH17
            && baselines.All(static item => item.Events.Count >= 7)
            && deterministicRepeat
            && observationDeterministic
            && inverseDeterministic
            && challengeDeterministic
            && policy.Events.Count == qualifiedTriggerSamples
            && stratification.Episodes.All(static episode => episode.SelectedCount > 0)
            && qualifiedTriggerSamples <= MaximumStratifiedQualificationEvents
            && policy.DeterministicHydraulicEvaluationWorkRatio <= MaximumAlgorithmWorkRatio
            && policy.MaximumHydraulicMassClosureKilogramsPerSecond <= 1e-8d
            && policy.MaximumHydraulicEnergyOwnershipResidualWatts <= 1e-3d;

        var summary = new[]
        {
            "================================================================================",
            "M10.9.4.1-H.19 FOUR-NODE LONG-HORIZON & CROSS-PROFILE SHADOW QUALIFICATION SUMMARY",
            "================================================================================",
            "=== 01-current-v2-four-node-long-horizon-cross-profile-bounded-hysteresis ===",
            "Shadow-only long-horizon and cross-profile qualification of the H.18 validated steam/stop-out/header/turbine-inlet target extension using unchanged H.9 and unchanged 2%/5 K bounded previous-phase hysteresis. Production Resolve(), explicit integration and P060/F040 remain unchanged; no shadow candidate is committed.",
            FormattableString.Invariant($"profiles={trajectories.Count}; profile-ids={string.Join('|', trajectories.Select(static item => item.ProfileId))}; production-shadow-steps={totalIntervals}; frozen-trigger=P060-F040; census-triggered-events={totalTriggers}; trigger-episodes={stratification.Episodes.Count}; qualified-trigger-samples={qualifiedTriggerSamples}; trigger-episode-max-quiet-gap={TriggerEpisodeMaximumQuietGapIntervals}; post-500-trigger-events={baselines.Sum(static item => item.Events.Count(candidate => candidate.IntervalIndex > 500))};"),
            FormattableString.Invariant($"H16-control-steps={H16ControlIntervalCount}; H16-control-triggered-events={h16Control.Events.Count}; H16-control-converged={h16Control.Events.Count(static item => item.Result.Converged)}/{h16Control.Events.Count}; H16-control-interval-723-converged={h16Interval723.Result.Converged}; H16-control-interval-723-header-overrides={h16HeaderOverrides};"),
            FormattableString.Invariant($"frozen-H17-representatives={frozenH17Representatives.Count}; representative-keys-match-frozen-H17={representativeKeysMatchFrozenH17}; recovered-H17-failures={recoveredH17Failures}/245; preserved-H17-successes={preservedH17Successes}/228; recovered-H17-turbine-inlet-mismatch={recoveredH17MismatchFailures}/120; recovered-H17-no-mismatch={recoveredH17NoMismatchFailures}/125;"),
            FormattableString.Invariant($"H19-targets=steam|stop-out|header|turbine-inlet; stratified-converged={converged}/{qualifiedTriggerSamples}; line-search-exhausted={exhausted}; represented-trigger-episodes={stratification.Episodes.Count(static episode => episode.SelectedCount > 0)}/{stratification.Episodes.Count}; branch-overrides={policy.BranchOverrideCount}; previous-phase-holds={policy.PreviousPhaseHoldCount}; solver-hysteresis-releases={policy.HysteresisReleaseCount}; deterministic-work-ratio={policy.DeterministicHydraulicEvaluationWorkRatio:0.000000}; deterministic-sentinel-events={determinismSentinelCount}; deterministic-repeat={deterministicRepeat}; cross-profile-stratified-policy-qualifies={crossProfileQualifies};"),
            FormattableString.Invariant($"committed-phase-state-checks={observation.CommittedPhaseStateChecks}; committed-selection-observations={observation.Rows.Count}; committed-selection-overrides={observation.Rows.Count(static item => item.SelectionDiffersFromProduction)}; committed-selection-transparent={committedTransparent}; committed-target-phase-transitions={observation.CommittedPhaseTransitionCount}; observation-stride={ObservationStride}; deterministic-repeat={observationDeterministic};"),
            FormattableString.Invariant($"inverse-qualified-sample-node-scans={inverseScan.Rows.Count}; candidate-late-shadow-nodes={JoinOrNone(inverseScan.Rows.Where(static item => item.CandidateLateShadow).Select(static item => item.NodeId).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal))}; untargeted-candidate-only-late-shadow-nodes={JoinOrNone(inverseScan.UntargetedCandidateOnlyLateShadowNodeIds)}; untargeted-candidate-vs-explicit-phase-mismatch-nodes={JoinOrNone(inverseScan.UntargetedCandidatePhaseMismatchNodeIds)}; no-new-untargeted-branch-disagreement-in-qualified-sample={inverseScan.UntargetedCandidateOnlyLateShadowNodeIds.Count == 0 && inverseScan.UntargetedCandidatePhaseMismatchNodeIds.Count == 0}; deterministic-repeat={inverseDeterministic};"),
            FormattableString.Invariant($"release-challenges={challenges.Count}; observed-holds={challengeHolds}; observed-releases={challengeReleases}; release-challenges-pass={releaseChallengesPass}; deterministic-repeat={challengeDeterministic};"),
            FormattableString.Invariant($"max-closure/ownership={policy.MaximumHydraulicMassClosureKilogramsPerSecond:0.000000000}/{policy.MaximumHydraulicEnergyOwnershipResidualWatts:0.000000000}; four-node-long-horizon-cross-profile-shadow-qualification-passes={qualificationPasses}; h19-audit-passes={auditPasses};"),
            "bounded-hysteresis-limits-changed=False; target-node-set-changed-from-H18=False; production-resolve-order-changed=False; production-previous-state-hysteresis-introduced=False; production-hybrid-active=False; production-fixed-step=10.000 ms; shadow-candidates-committed=False; H9-corrector-replaced=False; plant-network-orchestrator-routing-changed=False; trigger-retuning=False; physical-coefficient-retuning=False; hidden-flow-filtering=False;",
            recommendation,
            FormattableString.Invariant($"Detailed CSV files: \"{directory}\""),
        };

        File.WriteAllLines(Path.Combine(directory, "01-current-v2-four-node-long-horizon-cross-profile-qualification.summary.txt"), summary, Utf8WithoutBom);
        File.WriteAllLines(Path.Combine(directory, "02-profile-qualification-summary.csv"), profileRows, Utf8WithoutBom);
        File.WriteAllLines(Path.Combine(directory, "03-triggered-event-cross-profile-results.csv"), triggerRows, Utf8WithoutBom);
        File.WriteAllLines(Path.Combine(directory, "03a-trigger-episode-stratification.csv"), episodeRows, Utf8WithoutBom);
        File.WriteAllLines(Path.Combine(directory, "04-committed-target-selection-observations.csv"), observationRows, Utf8WithoutBom);
        File.WriteAllLines(Path.Combine(directory, "05-triggered-all-node-inverse-branch-scan.csv"), inverseRows, Utf8WithoutBom);
        File.WriteAllLines(Path.Combine(directory, "06-hysteresis-release-challenges.csv"), challengeRows, Utf8WithoutBom);
        File.WriteAllLines(Path.Combine(directory, "07-four-node-long-horizon-cross-profile-metrics.csv"), new[]
        {
            "metric,value",
            FormattableString.Invariant($"production_shadow_steps,{totalIntervals}"),
            FormattableString.Invariant($"profiles,{trajectories.Count}"),
            FormattableString.Invariant($"census_triggered_events,{totalTriggers}"),
            FormattableString.Invariant($"trigger_episodes,{stratification.Episodes.Count}"),
            FormattableString.Invariant($"qualified_trigger_samples,{qualifiedTriggerSamples}"),
            FormattableString.Invariant($"representative_keys_match_frozen_h17,{representativeKeysMatchFrozenH17}"),
            FormattableString.Invariant($"recovered_h17_failures,{recoveredH17Failures}"),
            FormattableString.Invariant($"preserved_h17_successes,{preservedH17Successes}"),
            FormattableString.Invariant($"recovered_h17_turbine_inlet_mismatch_failures,{recoveredH17MismatchFailures}"),
            FormattableString.Invariant($"recovered_h17_no_mismatch_failures,{recoveredH17NoMismatchFailures}"),
            FormattableString.Invariant($"maximum_stratified_qualification_events,{MaximumStratifiedQualificationEvents}"),
            FormattableString.Invariant($"trigger_episode_maximum_quiet_gap_intervals,{TriggerEpisodeMaximumQuietGapIntervals}"),
            FormattableString.Invariant($"converged_qualified_samples,{converged}"),
            FormattableString.Invariant($"line_search_exhausted,{exhausted}"),
            FormattableString.Invariant($"branch_overrides,{policy.BranchOverrideCount}"),
            FormattableString.Invariant($"previous_phase_holds,{policy.PreviousPhaseHoldCount}"),
            FormattableString.Invariant($"solver_hysteresis_releases,{policy.HysteresisReleaseCount}"),
            FormattableString.Invariant($"deterministic_work_ratio,{policy.DeterministicHydraulicEvaluationWorkRatio:G17}"),
            FormattableString.Invariant($"committed_phase_state_checks,{observation.CommittedPhaseStateChecks}"),
            FormattableString.Invariant($"committed_selection_observations,{observation.Rows.Count}"),
            FormattableString.Invariant($"committed_phase_transitions,{observation.CommittedPhaseTransitionCount}"),
            FormattableString.Invariant($"untargeted_candidate_only_late_shadow_nodes,{inverseScan.UntargetedCandidateOnlyLateShadowNodeIds.Count}"),
            FormattableString.Invariant($"untargeted_candidate_phase_mismatch_nodes,{inverseScan.UntargetedCandidatePhaseMismatchNodeIds.Count}"),
            FormattableString.Invariant($"cross_profile_stratified_policy_qualifies,{crossProfileQualifies}"),
            FormattableString.Invariant($"committed_selection_transparent,{committedTransparent}"),
            FormattableString.Invariant($"release_challenges_pass,{releaseChallengesPass}"),
            FormattableString.Invariant($"four_node_long_horizon_cross_profile_shadow_qualification_passes,{qualificationPasses}"),
            FormattableString.Invariant($"h19_audit_passes,{auditPasses}"),
        }, Utf8WithoutBom);
    }

    private static string JoinOrNone(IEnumerable<string> values)
    {
        var materialized = values.ToArray();
        return materialized.Length == 0 ? "none" : string.Join('|', materialized);
    }

    private static void ResetProgress()
    {
        var directory = Path.Combine(FindRepositoryRoot(), "artifacts", "h19-four-node-long-horizon-cross-profile-qualification");
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "00-progress.txt"), "H.19 four-node long-horizon qualification started." + Environment.NewLine, Utf8WithoutBom);
    }

    private static void WriteProgress(string message)
    {
        var directory = Path.Combine(FindRepositoryRoot(), "artifacts", "h19-four-node-long-horizon-cross-profile-qualification");
        Directory.CreateDirectory(directory);
        File.AppendAllText(
            Path.Combine(directory, "00-progress.txt"),
            FormattableString.Invariant($"{DateTime.UtcNow:O} {message}{Environment.NewLine}"),
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

    private enum ProfileKind
    {
        Steady = 0,
        LoadPulse = 1,
        CoolingPulse = 2,
        CombinedLoadCooling = 3,
    }

    private sealed record ProfileDefinition(string Id, int IntervalCount, ProfileKind Kind);
    private sealed record FrozenH17Representative(
        string ProfileId,
        int IntervalIndex,
        bool H17Converged,
        bool TurbineInletPhaseMismatch);
    private sealed record ProfileTrajectory(string ProfileId, IReadOnlyList<ReferenceInterval> Intervals);
    private sealed record ReferenceInterval(
        string ProfileId,
        int Index,
        PlantState Start,
        PlantState End,
        IReadOnlyDictionary<string, FluidNodeBalance> FrozenNonHydraulicBalances);
    private sealed record TriggerEpisodeSeed(string ProfileId, int EpisodeIndex, IReadOnlyList<BaselineTriggerEvent> Events);
    private sealed record TriggerEpisode(
        string ProfileId,
        int EpisodeIndex,
        int StartInterval,
        int EndInterval,
        int TriggerCount,
        int SelectedCount,
        double MaximumTriggerSeverity,
        double MaximumH4ResidualSeverity);
    private sealed record TriggerStratification(
        int CensusTriggerCount,
        IReadOnlyList<BaselineTriggerEvent> SelectedEvents,
        IReadOnlyList<TriggerEpisode> Episodes,
        IReadOnlyDictionary<(string ProfileId, int IntervalIndex), string> SelectionReasons);
    private sealed record ProfileBaseline(string ProfileId, IReadOnlyList<BaselineTriggerEvent> Events);
    private sealed record BaselineTriggerEvent(string ProfileId, int IntervalIndex, HybridSemiImplicitHydraulicGateStepResult PrimaryResult);
    private sealed record PolicyEvent(
        string ProfileId,
        int IntervalIndex,
        JacobianHydraulicCorrectorStepResult Result,
        IReadOnlyList<ThermodynamicBranchContinuityDecision> Decisions);
    private sealed record PolicyRun(
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
        string ProfileId,
        int IntervalIndex,
        string NodeId,
        string CommittedPhase,
        string ProductionPhase,
        string SelectedPhase,
        string DecisionKind,
        bool SelectionDiffersFromProduction,
        bool SelectedPreviousPhase,
        bool CommittedPhaseTransition,
        double RelativePressureDrift,
        double TemperatureDriftKelvins);
    private sealed record CommittedObservation(
        IReadOnlyList<CommittedObservationRow> Rows,
        int CommittedPhaseTransitionCount,
        int CommittedPhaseStateChecks);
    private sealed record InverseBranchScanRow(
        string ProfileId,
        int IntervalIndex,
        string NodeId,
        bool Targeted,
        string CandidateSelectedBranch,
        string CandidateSelectedPhase,
        bool CandidateMultipleRoots,
        bool CandidateLateShadow,
        string ExplicitSelectedBranch,
        string ExplicitSelectedPhase,
        bool ExplicitMultipleRoots,
        bool ExplicitLateShadow,
        bool CandidateOnlyLateShadow);
    private sealed record InverseBranchScan(
        IReadOnlyList<InverseBranchScanRow> Rows,
        IReadOnlyList<string> UntargetedCandidateOnlyLateShadowNodeIds,
        IReadOnlyList<string> UntargetedCandidatePhaseMismatchNodeIds);
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
