using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Recording;
using NuclearReactorSimulator.Application.Scenarios.Training;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Recording;

public sealed class TurbineValveReplayCheckpointTests
{
    [Fact]
    public void ValveCommands_ReplayAndCheckpointPreserveInFlightTargetsAndManualAuthority()
    {
        var factory = CreateFactory();
        var session = factory.Load(DesktopIntegratedOperationsProgram.Scenario);
        var initial = Assert.Single(session.Coordinator.Current.TurbineSecondary.AdmissionTrains);
        ScenarioRecording recording;
        ScenarioCheckpoint checkpoint;

        using (var recorder = new ScenarioRecorder(session))
        {
            session.CommandDispatcher.Dispatch(ValveCommand(
                ControlRoomCommandKind.TurbineControlValveManualMode,
                initial.ControlValveId));
            session.CommandDispatcher.Dispatch(ValveCommand(
                ControlRoomCommandKind.TurbineControlValveManualDemandSet,
                initial.ControlValveId,
                37d));
            session.CommandDispatcher.Dispatch(ValveCommand(
                ControlRoomCommandKind.TurbineValveClose,
                initial.StopValveId));
            session.CommandDispatcher.Dispatch(ValveCommand(
                ControlRoomCommandKind.TurbineValveClose,
                initial.AdmissionValveId));
            session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.SingleStep));

            var inFlight = Assert.Single(session.Coordinator.Current.TurbineSecondary.AdmissionTrains);
            Assert.True(inFlight.ControlValveManualMode);
            Assert.Equal(37d, inFlight.ControlValveManualDemand.NumericValue!.Value, 6);
            Assert.Equal(0d, inFlight.StopValveRequestedPosition.NumericValue!.Value, 6);
            Assert.InRange(inFlight.StopValvePosition.NumericValue!.Value, 0.000001d, 99.999999d);
            Assert.Equal(0d, inFlight.AdmissionValveRequestedPosition.NumericValue!.Value, 6);
            Assert.True(inFlight.AdmissionValvePosition.NumericValue < initial.AdmissionValvePosition.NumericValue);

            checkpoint = recorder.CreateCheckpoint("turbine-valves-in-flight");

            session.CommandDispatcher.Dispatch(ValveCommand(
                ControlRoomCommandKind.TurbineValveOpen,
                initial.StopValveId));
            session.CommandDispatcher.Dispatch(ValveCommand(
                ControlRoomCommandKind.TurbineValveOpen,
                initial.AdmissionValveId));
            session.CommandDispatcher.Dispatch(ValveCommand(
                ControlRoomCommandKind.TurbineControlValveAutomaticMode,
                initial.ControlValveId));
            session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.SingleStep));
            recording = recorder.Complete();
        }

        Assert.Contains(recording.OperatorActions, static action =>
            action.Command.Kind == ControlRoomCommandKind.TurbineControlValveManualDemandSet
            && action.Command.NumericValue == 37d);
        Assert.Contains(recording.OperatorActions, static action =>
            action.Command.Kind == ControlRoomCommandKind.TurbineValveClose);
        Assert.Contains(recording.OperatorActions, static action =>
            action.Command.Kind == ControlRoomCommandKind.TurbineValveOpen);

        var replay = new ScenarioFullReplayRunner(factory).ReplayAndVerify(
            DesktopIntegratedOperationsProgram.Scenario,
            recording);
        Assert.Equal(recording.FinalLogicalStep, replay.Session.Coordinator.Current.LogicalStep);
        Assert.Equal(
            recording.Frames[^1].SnapshotFingerprint,
            ControlRoomSnapshotFingerprint.Compute(replay.Session.Coordinator.Current));

        var restored = new ScenarioFullReplayRunner(factory).SeekAndVerify(
            DesktopIntegratedOperationsProgram.Scenario,
            recording,
            checkpoint);
        Assert.Equal(checkpoint.LogicalStep, restored.Coordinator.Current.LogicalStep);
        var restoredTrain = Assert.Single(restored.Coordinator.Current.TurbineSecondary.AdmissionTrains);
        Assert.True(restoredTrain.ControlValveManualMode);
        Assert.Equal(37d, restoredTrain.ControlValveManualDemand.NumericValue!.Value, 6);
        Assert.Equal(0d, restoredTrain.StopValveRequestedPosition.NumericValue!.Value, 6);
        Assert.InRange(restoredTrain.StopValvePosition.NumericValue!.Value, 0.000001d, 99.999999d);
        Assert.Equal(0d, restoredTrain.AdmissionValveRequestedPosition.NumericValue!.Value, 6);
        Assert.InRange(restoredTrain.AdmissionValvePosition.NumericValue!.Value, 0.000001d, 99.999999d);
        Assert.Equal(checkpoint.SnapshotFingerprint, ControlRoomSnapshotFingerprint.Compute(restored.Coordinator.Current));
    }

    private static ScenarioSessionFactory CreateFactory()
        => new(new VersionedInitialConditionRegistry(new IVersionedInitialConditionFactory[]
        {
            new DesktopSustainedGenerationInitialConditionFactory(),
        }));

    private static ControlRoomCommand ValveCommand(
        ControlRoomCommandKind kind,
        string valveId,
        double? numericValue = null)
        => new(kind, valveId, ControlRoomCommandTargetKind.Valve, numericValue);
}
