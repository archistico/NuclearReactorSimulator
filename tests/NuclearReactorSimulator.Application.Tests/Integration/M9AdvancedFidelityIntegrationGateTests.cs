using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Analysis;
using NuclearReactorSimulator.Application.Scenarios.Historical;
using NuclearReactorSimulator.Application.Scenarios.Recording;
using NuclearReactorSimulator.Application.Scenarios.Xenon;
using NuclearReactorSimulator.Application.Validation;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Integration;

public sealed class M9AdvancedFidelityIntegrationGateTests
{
    [Fact]
    public void XenonSession_RecordCheckpointAnalyzeReplayAndReferenceProjection_RemainDeterministicallyAligned()
    {
        var factory = CreateXenonFactory();
        var session = factory.Load(AdvancedXenonScenarioPack.RestartAfterShutdown);
        using var recorder = new ScenarioRecorder(session);
        var origin = recorder.CreateCheckpoint("m97-origin");

        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.AlarmAcknowledgeAll));
        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.SingleStep));
        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.SingleStep));
        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.SingleStep));
        var recording = recorder.Complete();

        var replay = new ScenarioFullReplayRunner(factory).ReplayAndVerify(
            AdvancedXenonScenarioPack.RestartAfterShutdown,
            recording);
        var analysis = new ScenarioPostIncidentAnalyzer().Analyze(
            recording,
            new PostIncidentAnalysisOptions(preIncidentSteps: 0, postIncidentSteps: 3));
        var originalSamples = recording.Frames
            .Select(static frame => ControlRoomReferenceMetricExtractor.Extract(frame.Snapshot))
            .ToArray();
        var replaySamples = replay.ReplayedRecording.Frames
            .Select(static frame => ControlRoomReferenceMetricExtractor.Extract(frame.Snapshot))
            .ToArray();

        Assert.Equal(recording.Frames.Count, replay.VerifiedFrameCount);
        Assert.Equal(recording.Events.Count, replay.VerifiedEventCount);
        Assert.Equal(PostIncidentAnalysisAnchorKind.OperatorAction, analysis.AnchorKind);
        Assert.Equal(origin.CheckpointId, analysis.PrecedingCheckpointId);
        Assert.Equal(recording.FinalLogicalStep, replay.Session.Coordinator.Current.LogicalStep);
        Assert.Equal(
            recording.Frames.Select(static frame => frame.SnapshotFingerprint),
            replay.ReplayedRecording.Frames.Select(static frame => frame.SnapshotFingerprint));

        var originalXenon = originalSamples
            .Select(static sample => sample.Metrics[ReferenceValidationMetricIds.ReactorXenonReactivityPcm])
            .ToArray();
        var replayXenon = replaySamples
            .Select(static sample => sample.Metrics[ReferenceValidationMetricIds.ReactorXenonReactivityPcm])
            .ToArray();
        Assert.Equal(originalXenon, replayXenon);
        Assert.All(originalXenon, static value =>
        {
            Assert.True(value.HasValue);
            Assert.True(double.IsFinite(value.Value));
            Assert.True(value.Value < 0d);
        });
    }

    [Fact]
    public void HistoricalFidelityCapabilitySet_CoversAllAdvancedM9RuntimeSeamsWithoutClaimingCalibration()
    {
        var required = new[]
        {
            HistoricalModelCapabilityIds.RecorderCheckpointReplay,
            HistoricalModelCapabilityIds.PostIncidentAnalysis,
            HistoricalModelCapabilityIds.IodineXenon,
            HistoricalModelCapabilityIds.QuasiSpatialCoreFeedback,
        };

        Assert.All(required, capability => Assert.Contains(capability, HistoricalModelCapabilityIds.ValidatedThroughM94));
        Assert.Equal("NRS-M9.5-VALIDATED", BuiltInReferenceValidationCatalog.ValidatedModelVersion);
        Assert.All(BuiltInReferenceValidationCatalog.All, static definition =>
            Assert.True(definition.ReferenceSource.Contains("not an external historical measurement", StringComparison.OrdinalIgnoreCase)));
    }

    private static ScenarioSessionFactory CreateXenonFactory()
        => new(new VersionedInitialConditionRegistry(new IVersionedInitialConditionFactory[]
        {
            new XenonRestartInitialConditionFactory(),
            new LowPowerXenonInitialConditionFactory(),
        }));
}
