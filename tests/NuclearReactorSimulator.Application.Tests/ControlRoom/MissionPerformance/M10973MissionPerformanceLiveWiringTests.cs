using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.ControlRoom.MissionPerformance;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Challenges.Packs;
using NuclearReactorSimulator.Application.Scenarios.Challenges.Replay;
using NuclearReactorSimulator.Application.Scenarios.Operations;
using NuclearReactorSimulator.Application.Scenarios.Recording;
using NuclearReactorSimulator.Application.Scenarios.Training;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.ControlRoom.MissionPerformance;

public sealed class M10973MissionPerformanceLiveWiringTests
{
    [Fact]
    public void PresentationComparer_IsStructuralAcrossRecreatedCollectionsAndDetectsRealChanges()
    {
        var snapshot = CreateProjectedSnapshot();
        var recreated = snapshot with
        {
            Score = snapshot.Score with { Dimensions = snapshot.Score.Dimensions.ToArray() },
            RecentEvents = snapshot.RecentEvents.ToArray(),
        };

        Assert.NotSame(snapshot.RecentEvents, recreated.RecentEvents);
        Assert.NotSame(snapshot.Score.Dimensions, recreated.Score.Dimensions);
        Assert.True(MissionPerformancePresentationComparer.AreEquivalent(snapshot, recreated));
        Assert.False(MissionPerformancePresentationComparer.AreEquivalent(
            snapshot,
            recreated with { LogicalStep = recreated.LogicalStep + 1 }));

        var changedEvents = recreated.RecentEvents.ToArray();
        Assert.NotEmpty(changedEvents);
        changedEvents[0] = changedEvents[0] with { Summary = changedEvents[0].Summary + " changed" };
        Assert.False(MissionPerformancePresentationComparer.AreEquivalent(
            snapshot,
            recreated with { RecentEvents = changedEvents }));
    }

    [Fact]
    public void InMemorySource_SuppressesRecreatedButEquivalentSnapshots()
    {
        var snapshot = CreateProjectedSnapshot();
        var source = new InMemoryMissionPerformanceSnapshotSource(snapshot);
        var publications = 0;
        source.SnapshotChanged += (_, _) => publications++;

        var equal = snapshot with
        {
            Score = snapshot.Score with { Dimensions = snapshot.Score.Dimensions.ToArray() },
            RecentEvents = snapshot.RecentEvents.ToArray(),
        };
        Assert.False(source.Publish(equal));
        Assert.Equal(0, publications);

        Assert.True(source.Publish(equal with { ObjectiveTitle = equal.ObjectiveTitle + " updated" }));
        Assert.Equal(1, publications);
    }

    [Fact]
    public void LiveSource_UsesDeterministicStepDemandAndProducesTheSameFinalScoreAsReplayProjection()
    {
        var factory = CreateFactory();
        var pack = InitialOperationalChallengePack.BoundedDemandFollowing;
        var session = factory.Load(pack.Scenario);
        using var recorder = new ScenarioRecorder(session);
        using var source = new MissionPerformanceLiveSnapshotSource(
            session,
            pack,
            TrainingGuidanceMode.Guided,
            recorder);
        var publications = 0;
        source.SnapshotChanged += (_, _) => publications++;
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

        var live = source.Current;
        var recording = recorder.Capture();
        var replay = OperationalChallengeRecordingProjector.Project(pack, recording);

        Assert.True(publications > 0);
        Assert.Equal(recording.FinalLogicalStep, live.LogicalStep);
        Assert.True(live.Demand.ExternalDemandAvailable);
        Assert.True(live.Demand.ExternalDemandMegawatts.HasValue);
        Assert.True(live.Demand.RequestedGeneratorLoadMegawatts.HasValue);
        Assert.True(live.Demand.ActualElectricalOutputMegawatts.HasValue);
        Assert.Equal(replay.Score.FinalScore, live.Score.FinalScore);
        Assert.Equal(replay.Score.FinalPercentage, live.Score.FinalPercentage);
        Assert.Equal(replay.Score.Grade, live.Score.Grade);
        Assert.Equal(replay.Score.Dimensions.Count, live.Score.Dimensions.Count);
        for (var index = 0; index < replay.Score.Dimensions.Count; index++)
        {
            var expected = replay.Score.Dimensions[index];
            var actual = live.Score.Dimensions[index];
            Assert.Equal(expected.Kind, actual.Kind);
            Assert.Equal(expected.MaximumPoints, actual.MaximumPoints);
            Assert.Equal(expected.AwardedPoints, actual.AwardedPoints);
            Assert.Equal(expected.IsEvidenceAvailable, actual.IsEvidenceAvailable);
            Assert.Equal(expected.PerformanceFraction, actual.PerformanceFraction);
            Assert.Equal(expected.EvidenceSourceId, actual.EvidenceSourceId);
            Assert.Equal(expected.EvidenceSummary, actual.EvidenceSummary);
            Assert.Equal(expected.IsCriticalFailure, actual.IsCriticalFailure);
        }
    }

    private static MissionPerformanceSnapshot CreateProjectedSnapshot()
    {
        var factory = CreateFactory();
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
        var recording = recorder.Capture();
        var replay = OperationalChallengeRecordingProjector.Project(pack, recording);
        return MissionPerformanceSnapshotProjector.Project(
            pack,
            replay.Lifecycle,
            recording.Frames[^1].Snapshot,
            replay.Frames[^1].ExternalDemand,
            replay.Score,
            TrainingGuidanceMode.Guided,
            recordingEvents: recording.Events);
    }

    private static ScenarioSessionFactory CreateFactory()
        => new(new VersionedInitialConditionRegistry(new IVersionedInitialConditionFactory[]
        {
            new PowerManoeuvringInitialConditionFactory(),
        }));
}
