using System.Reflection;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.ControlRoom.Automation;
using NuclearReactorSimulator.Application.ControlRoom.MissionPerformance;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Challenges;
using NuclearReactorSimulator.Application.Scenarios.Challenges.Demand;
using NuclearReactorSimulator.Application.Scenarios.Challenges.Packs;
using NuclearReactorSimulator.Application.Scenarios.Challenges.Replay;
using NuclearReactorSimulator.Application.Scenarios.Operations;
using NuclearReactorSimulator.Application.Scenarios.Recording;
using NuclearReactorSimulator.Application.Scenarios.Training;
using NuclearReactorSimulator.Domain.Physics.Control.Supervisory;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.ControlRoom.MissionPerformance;

public sealed class M10971MissionPerformancePresentationContractTests
{
    [Fact]
    public void Projection_CopiesCanonicalMissionDemandScoreAndModesWithoutOwningSemantics()
    {
        var factory = CreateFactory();
        var pack = InitialOperationalChallengePack.BoundedDemandFollowing;
        var recording = RecordBoundedDemandTrace(factory);
        var replay = OperationalChallengeRecordingProjector.Project(pack, recording);
        var finalFrame = recording.Frames[^1];
        var demand = replay.Frames[^1].ExternalDemand;
        var authority = new PlantControlAuthorityPresentationSnapshot(
            true,
            PlantControlAuthorityMode.Assisted,
            PlantControlAuthorityMode.Manual,
            PlantControlAuthorityHealth.SuspendedByProtection,
            "Protection owns the effective boundary.",
            "TRAINING",
            7,
            Array.Empty<PlantControllerModePresentationSnapshot>());

        var projected = MissionPerformanceSnapshotProjector.Project(
            pack,
            replay.Lifecycle,
            finalFrame.Snapshot,
            demand,
            replay.Score,
            TrainingGuidanceMode.ChecklistOnly,
            authority,
            recording.Events);
        var repeat = MissionPerformanceSnapshotProjector.Project(
            pack,
            replay.Lifecycle,
            finalFrame.Snapshot,
            demand,
            replay.Score,
            TrainingGuidanceMode.ChecklistOnly,
            authority,
            recording.Events);

        Assert.Equal(projected.PackExactId, repeat.PackExactId);
        Assert.Equal(projected.LogicalStep, repeat.LogicalStep);
        Assert.Equal(projected.Demand, repeat.Demand);
        Assert.Equal(projected.Score.ScoringPolicyExactId, repeat.Score.ScoringPolicyExactId);
        Assert.Equal(projected.Score.FinalPercentage, repeat.Score.FinalPercentage);
        Assert.Equal(projected.Score.Dimensions, repeat.Score.Dimensions);
        Assert.Equal(projected.RecentEvents, repeat.RecentEvents);
        Assert.Equal(pack.ExactId, projected.PackExactId);
        Assert.Equal(pack.Challenge.ExactId, projected.ChallengeExactId);
        var objective = Assert.Single(
            pack.Scenario.Objectives,
            item => string.Equals(item.ObjectiveId, pack.Challenge.ObjectiveId, StringComparison.Ordinal));
        Assert.Equal(objective.ObjectiveId, projected.ObjectiveId);
        Assert.Equal(objective.Title, projected.ObjectiveTitle);
        Assert.Equal(objective.Description, projected.ObjectiveDescription);
        Assert.NotEqual(pack.Challenge.Title, projected.ObjectiveTitle);
        Assert.Equal(replay.Lifecycle.State, projected.LifecycleState);
        Assert.Equal(replay.Lifecycle.LogicalStep, projected.LogicalStep);
        Assert.True(replay.Lifecycle.ActivatedLogicalStep.HasValue);
        Assert.True(projected.ElapsedLogicalSteps.HasValue);
        Assert.Equal(
            replay.Lifecycle.LogicalStep - replay.Lifecycle.ActivatedLogicalStep.Value,
            projected.ElapsedLogicalSteps.Value);
        Assert.True(projected.ElapsedLogicalSteps.Value >= 0);
        Assert.True(projected.Demand.ExternalDemandAvailable);
        Assert.Equal(demand.ExternalDemandMegawatts, projected.Demand.ExternalDemandMegawatts);
        Assert.Equal(demand.RequestedGeneratorLoadMegawatts, projected.Demand.RequestedGeneratorLoadMegawatts);
        Assert.Equal(demand.ActualElectricalOutputMegawatts, projected.Demand.ActualElectricalOutputMegawatts);
        Assert.Equal(demand.DemandOutputErrorMegawatts, projected.Demand.DemandOutputErrorMegawatts);
        Assert.True(projected.Score.IsAvailable);
        Assert.Equal(replay.Score.ScoringPolicyExactId, projected.Score.ScoringPolicyExactId);
        Assert.Equal(replay.Score.FinalPercentage, projected.Score.FinalPercentage);
        Assert.Equal(replay.Score.Grade, projected.Score.Grade);
        Assert.Equal(replay.Score.Dimensions.Count, projected.Score.Dimensions.Count);
        Assert.Equal(TrainingGuidanceMode.ChecklistOnly, projected.AssistanceMode);
        Assert.True(projected.PlantControlAuthorityAvailable);
        Assert.Equal(PlantControlAuthorityMode.Assisted, projected.RequestedControlAuthority);
        Assert.Equal(PlantControlAuthorityMode.Manual, projected.EffectiveControlAuthority);
        Assert.Equal(PlantControlAuthorityHealth.SuspendedByProtection, projected.ControlAuthorityHealth);
        Assert.Contains(projected.RecentEvents, static item => item.Kind == MissionPerformanceEventKind.Objective);
        Assert.Contains(projected.RecentEvents, static item => item.Kind == MissionPerformanceEventKind.Scoring);
    }

    [Fact]
    public void Projection_KeepsDemandRequestAndActualSeparateWhenExternalDemandIsUnavailable()
    {
        var factory = CreateFactory();
        var recording = RecordBoundedDemandTrace(factory);
        var pack = InitialOperationalChallengePack.ControlledNormalShutdown;
        var replay = OperationalChallengeRecordingProjector.Project(pack, recording);
        var finalFrame = recording.Frames[^1];
        var unavailableDemand = ExternalEnergyDemandEvidenceSnapshot.Unavailable(finalFrame.LogicalStep);

        var projected = MissionPerformanceSnapshotProjector.Project(
            pack,
            replay.Lifecycle,
            finalFrame.Snapshot,
            unavailableDemand,
            replay.Score,
            TrainingGuidanceMode.Hidden);

        Assert.False(projected.Demand.ExternalDemandAvailable);
        Assert.Null(projected.Demand.ExternalDemandMegawatts);
        Assert.NotNull(projected.Demand.RequestedGeneratorLoadMegawatts);
        Assert.NotNull(projected.Demand.ActualElectricalOutputMegawatts);
        Assert.Null(projected.Demand.DemandOutputErrorMegawatts);
    }

    [Fact]
    public void Projection_UsesOnlyDeterministicObjectiveProtectionAndScoringEvidence()
    {
        var factory = CreateFactory();
        var pack = InitialOperationalChallengePack.BoundedDemandFollowing;
        var recording = RecordBoundedDemandTrace(factory);
        var replay = OperationalChallengeRecordingProjector.Project(pack, recording);
        var finalFrame = recording.Frames[^1];
        var protection = new ScenarioRecordingEvent(
            recording.Events[^1].Sequence + 1,
            finalFrame.LogicalStep,
            ScenarioRecordingEventKind.ProtectionTransition,
            "generator-trip",
            "Active");

        var projected = MissionPerformanceSnapshotProjector.Project(
            pack,
            replay.Lifecycle,
            finalFrame.Snapshot,
            replay.Frames[^1].ExternalDemand,
            replay.Score,
            TrainingGuidanceMode.Hidden,
            recordingEvents: recording.Events.Append(protection));

        var protectionEvent = Assert.Single(projected.RecentEvents, static item => item.Kind == MissionPerformanceEventKind.Protection);
        Assert.Equal("generator-trip", protectionEvent.SourceId);
        Assert.Equal(finalFrame.LogicalStep, protectionEvent.LogicalStep);
        Assert.True(protectionEvent.IsCritical);
        Assert.Equal(
            projected.RecentEvents.OrderBy(static item => item.LogicalStep)
                .ThenBy(static item => item.Kind)
                .ThenBy(static item => item.SourceSequence ?? long.MaxValue)
                .ThenBy(static item => item.SourceId, StringComparer.Ordinal)
                .ToArray(),
            projected.RecentEvents.ToArray());
    }

    [Fact]
    public void Projection_AlignsFrozenTerminalLifecycleToCurrentEvidenceWithoutMovingTerminalBoundary()
    {
        var factory = CreateFactory();
        var pack = InitialOperationalChallengePack.BoundedDemandFollowing;
        var recording = RecordBoundedDemandTrace(factory);
        var replay = OperationalChallengeRecordingProjector.Project(pack, recording);
        var finalFrame = recording.Frames[^1];
        Assert.True(replay.Lifecycle.TerminalLogicalStep.HasValue);
        Assert.True(replay.Lifecycle.ActivatedLogicalStep.HasValue);
        var terminalStep = replay.Lifecycle.TerminalLogicalStep.Value;
        Assert.True(terminalStep < finalFrame.LogicalStep);
        var frozenLifecycle = new ChallengeLifecycleSnapshot(
            replay.Lifecycle.ChallengeExactId,
            replay.Lifecycle.State,
            terminalStep,
            replay.Lifecycle.ActivatedLogicalStep,
            terminalStep,
            replay.Lifecycle.TargetWindowStartLogicalStep,
            replay.Lifecycle.TargetWindowEndLogicalStep,
            replay.Lifecycle.HardFailureDeadlineLogicalStep,
            replay.Lifecycle.Observations,
            replay.Lifecycle.Transitions);

        var demand = ScenarioChallengeExternalDemandProjector.Project(
            pack.Challenge,
            frozenLifecycle,
            finalFrame.Snapshot);
        var projected = MissionPerformanceSnapshotProjector.Project(
            pack,
            frozenLifecycle,
            finalFrame.Snapshot,
            demand,
            replay.Score,
            TrainingGuidanceMode.Hidden);

        Assert.Equal(finalFrame.LogicalStep, demand.LogicalStep);
        Assert.Equal(finalFrame.LogicalStep, projected.LogicalStep);
        Assert.True(projected.TerminalLogicalStep.HasValue);
        Assert.Equal(terminalStep, projected.TerminalLogicalStep.Value);
        Assert.True(projected.ElapsedLogicalSteps.HasValue);
        Assert.Equal(
            finalFrame.LogicalStep - replay.Lifecycle.ActivatedLogicalStep.Value,
            projected.ElapsedLogicalSteps.Value);
    }

    [Fact]
    public void Projection_DoesNotAlignNonTerminalLifecycleAcrossLogicalSteps()
    {
        var factory = CreateFactory();
        var pack = InitialOperationalChallengePack.BoundedDemandFollowing;
        var recording = RecordBoundedDemandTrace(factory);
        var finalFrame = recording.Frames[^1];
        var earlierStep = checked(finalFrame.LogicalStep - 1);
        var nonTerminalLifecycle = new ChallengeLifecycleSnapshot(
            pack.Challenge.ExactId,
            ChallengeLifecycleState.Active,
            earlierStep,
            earlierStep,
            null,
            null,
            null,
            null,
            Array.Empty<ChallengeConditionObservation>(),
            Array.Empty<ChallengeLifecycleTransition>());

        Assert.Throws<InvalidOperationException>(() => ScenarioChallengeExternalDemandProjector.Project(
            pack.Challenge,
            nonTerminalLifecycle,
            finalFrame.Snapshot));
    }

    [Fact]
    public void Projection_FiltersFutureProtectionEvidenceAndBoundsRecentEvents()
    {
        var factory = CreateFactory();
        var pack = InitialOperationalChallengePack.BoundedDemandFollowing;
        var recording = RecordBoundedDemandTrace(factory);
        var finalFrame = recording.Frames[^1];
        var lifecycle = new ChallengeLifecycleSnapshot(
            pack.Challenge.ExactId,
            ChallengeLifecycleState.Active,
            finalFrame.LogicalStep,
            0,
            null,
            null,
            null,
            null,
            Array.Empty<ChallengeConditionObservation>(),
            Array.Empty<ChallengeLifecycleTransition>());
        var events = Enumerable.Range(1, 120)
            .Select(index => new ScenarioRecordingEvent(
                index,
                finalFrame.LogicalStep,
                ScenarioRecordingEventKind.ProtectionTransition,
                $"protection-{index:D3}",
                "Active"))
            .Append(new ScenarioRecordingEvent(
                121,
                checked(finalFrame.LogicalStep + 1),
                ScenarioRecordingEventKind.ProtectionTransition,
                "future-protection",
                "Active"));

        var projected = MissionPerformanceSnapshotProjector.Project(
            pack,
            lifecycle,
            finalFrame.Snapshot,
            ExternalEnergyDemandEvidenceSnapshot.Unavailable(finalFrame.LogicalStep),
            score: null,
            assistanceMode: TrainingGuidanceMode.Hidden,
            recordingEvents: events);

        Assert.True(projected.RecentEvents.Count == 100);
        Assert.All(projected.RecentEvents, item => Assert.True(item.LogicalStep <= projected.LogicalStep));
        Assert.DoesNotContain(projected.RecentEvents, static item => item.SourceId == "future-protection");
        var firstSourceSequence = projected.RecentEvents[0].SourceSequence;
        var lastSourceSequence = projected.RecentEvents[^1].SourceSequence;
        Assert.True(firstSourceSequence.HasValue);
        Assert.True(lastSourceSequence.HasValue);
        Assert.Equal(21L, firstSourceSequence.GetValueOrDefault());
        Assert.Equal(120L, lastSourceSequence.GetValueOrDefault());
    }

    [Fact]
    public void PresentationSurface_HasNoCommandRuntimeWallClockOrScoringCalculatorAuthority()
    {
        var types = new[]
        {
            typeof(MissionPerformanceSnapshot),
            typeof(MissionPerformanceDemandSnapshot),
            typeof(MissionPerformanceScoreSnapshot),
            typeof(MissionPerformanceScoreDimensionSnapshot),
            typeof(MissionPerformanceEventSnapshot),
            typeof(MissionPerformanceSnapshotProjector),
        };

        foreach (var type in types)
        {
            foreach (var memberType in PublicMemberTypes(type).Select(Unwrap))
            {
                var name = memberType.FullName ?? memberType.Name;
                Assert.DoesNotContain("CommandDispatcher", name, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("RuntimeEngine", name, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("ChallengeScoreCalculator", name, StringComparison.OrdinalIgnoreCase);
                Assert.NotEqual(typeof(DateTime), memberType);
                Assert.NotEqual(typeof(DateTimeOffset), memberType);
                Assert.NotEqual(typeof(TimeSpan), memberType);
            }
        }
    }

    [Fact]
    public void ArtifactSummary_WritesM10971PresentationContractEvidence()
    {
        var directory = ResolveArtifactDirectory();
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "01-m10971-mission-performance-presentation-contract.summary.txt");
        File.WriteAllLines(path, new[]
        {
            "scope=M10.9.7.1 immutable mission/performance presentation contract over M10.9.6 VALIDATED/CLOSED; no workstation placement, UI, new scoring arithmetic, challenge definition, plant command authority or physics change;",
            "mission-objective-lifecycle-projected=True; logical-time-only=True; wall-clock-dependence=False;",
            "external-grid-demand-vs-requested-load-separated=True; external-grid-demand-vs-actual-output-separated=True; requested-load-vs-actual-output-separated=True; external-demand-may-be-unavailable-independently=True;",
            "score-copied-from-m1096-owner=True; score-formulas-in-presentation=False; score-dimension-decomposition-projected=True;",
            "objective-events-from-lifecycle=True; protection-events-from-canonical-recording=True; scoring-status-from-m1096-score=True; deterministic-event-order=True;",
            "assistance-mode-observational=True; requested-effective-control-authority-observational=True; plant-command-authority=False;",
            "operator-computer-f1-f8-contract-changed=False; workstation-placement-decision-deferred-to-m10972=True;",
            "m10971-mission-performance-presentation-contract-passes=True; next-step=M10.9.7.2 explicit workstation placement/navigation decision;",
        });
        Assert.True(File.Exists(path));
    }

    private static ScenarioRecording RecordBoundedDemandTrace(ScenarioSessionFactory factory)
    {
        var pack = InitialOperationalChallengePack.BoundedDemandFollowing;
        var session = factory.Load(pack.Scenario);
        using var recorder = new ScenarioRecorder(session);
        var generator = session.Coordinator.Current.Electrical.Generators.Single();

        session.CommandDispatcher.Dispatch(new ControlRoomCommand(
            ControlRoomCommandKind.GeneratorLoadRaise,
            generator.GeneratorId,
            ControlRoomCommandTargetKind.Generator));
        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.Run));
        _ = session.Coordinator.AdvanceRunning(stepCount: 1, publicationStride: 1);
        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.Pause));
        session.CommandDispatcher.Dispatch(new ControlRoomCommand(
            ControlRoomCommandKind.GeneratorLoadLower,
            generator.GeneratorId,
            ControlRoomCommandTargetKind.Generator));
        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.Run));
        _ = session.Coordinator.AdvanceRunning(stepCount: 3, publicationStride: 1);
        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.Pause));
        return recorder.Capture();
    }

    private static ScenarioSessionFactory CreateFactory()
        => new(new VersionedInitialConditionRegistry(new IVersionedInitialConditionFactory[]
        {
            new PowerManoeuvringInitialConditionFactory(),
        }));

    private static IEnumerable<Type> PublicMemberTypes(Type type)
    {
        foreach (var constructor in type.GetConstructors(BindingFlags.Instance | BindingFlags.Public))
        {
            foreach (var parameter in constructor.GetParameters())
            {
                yield return parameter.ParameterType;
            }
        }
        foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly))
        {
            yield return method.ReturnType;
            foreach (var parameter in method.GetParameters())
            {
                yield return parameter.ParameterType;
            }
        }
        foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly))
        {
            yield return property.PropertyType;
        }
    }

    private static Type Unwrap(Type type)
    {
        if (type.IsArray)
        {
            return type.GetElementType() ?? type;
        }
        if (type.IsGenericType)
        {
            return type.GetGenericArguments()[0];
        }
        return type;
    }

    private static string ResolveArtifactDirectory()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "NuclearReactorSimulator.sln")))
        {
            current = current.Parent;
        }
        if (current is null)
        {
            throw new InvalidOperationException("Repository root could not be resolved for M10.9.7.1 audit artifacts.");
        }
        return Path.Combine(current.FullName, "artifacts", "m1097-mission-performance-contract");
    }
}
