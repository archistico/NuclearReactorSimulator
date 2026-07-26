using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios.Training;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.ControlRoom;

public sealed class TurbineValveOperatorControlTests
{
    [Fact]
    public void ControlValve_ManualModeAndDemand_PublishRequestedAndActualPositionsSeparately()
    {
        var engine = CreateEngine();
        var initial = Assert.Single(engine.CreatePresentationSnapshot(ControlRoomRunState.Paused)
            .TurbineSecondary.AdmissionTrains);

        engine.QueueOperatorCommand(ValveCommand(
            ControlRoomCommandKind.TurbineControlValveManualMode,
            initial.ControlValveId));
        engine.QueueOperatorCommand(ValveCommand(
            ControlRoomCommandKind.TurbineControlValveManualDemandSet,
            initial.ControlValveId,
            37d));

        var requested = Assert.Single(engine.CreatePresentationSnapshot(ControlRoomRunState.Paused)
            .TurbineSecondary.AdmissionTrains);
        Assert.True(requested.ControlValveManualMode);
        Assert.Equal(37d, requested.ControlValveManualDemand.NumericValue!.Value, 6);

        var stepped = Assert.Single(engine.Step(ControlRoomRunState.Paused)
            .TurbineSecondary.AdmissionTrains);
        Assert.Equal(37d, stepped.ControlValveRequestedPosition.NumericValue!.Value, 6);
        Assert.NotEqual(
            stepped.ControlValveRequestedPosition.NumericValue,
            stepped.ControlValvePosition.NumericValue);
    }

    [Fact]
    public void ControlValve_AutomaticMode_ReturnsAuthorityToGovernor()
    {
        var engine = CreateEngine();
        var valveId = Assert.Single(engine.CreatePresentationSnapshot(ControlRoomRunState.Paused)
            .TurbineSecondary.AdmissionTrains).ControlValveId;

        engine.QueueOperatorCommand(ValveCommand(
            ControlRoomCommandKind.TurbineControlValveManualMode,
            valveId));
        engine.QueueOperatorCommand(ValveCommand(
            ControlRoomCommandKind.TurbineControlValveAutomaticMode,
            valveId));

        var train = Assert.Single(engine.CreatePresentationSnapshot(ControlRoomRunState.Paused)
            .TurbineSecondary.AdmissionTrains);
        Assert.False(train.ControlValveManualMode);
        Assert.Equal("AUTO / GOVERNOR", train.ControlValveModeText);
    }

    [Fact]
    public void StopValve_OpenRequest_RemainsVisibleWhileTurbineTripForcesActualClosed()
    {
        var engine = CreateEngine();
        var valveId = Assert.Single(engine.CreatePresentationSnapshot(ControlRoomRunState.Paused)
            .TurbineSecondary.AdmissionTrains).StopValveId;

        engine.QueueOperatorCommand(ValveCommand(ControlRoomCommandKind.TurbineValveOpen, valveId));
        engine.QueueOperatorCommand(new ControlRoomCommand(ControlRoomCommandKind.TurbineTrip));

        var train = Assert.Single(engine.Step(ControlRoomRunState.Paused)
            .TurbineSecondary.AdmissionTrains);
        Assert.Equal(100d, train.StopValveRequestedPosition.NumericValue!.Value, 6);
        Assert.Equal(0d, train.StopValvePosition.NumericValue!.Value, 6);
        Assert.True(train.StopValveForcedClosed);
        Assert.Contains("TRIP OVERRIDE", train.ValveAuthorityText, StringComparison.Ordinal);
    }


    [Fact]
    public void StopValve_OpenRequest_ResumesFiniteTravelAfterAcceptedProtectionReset()
    {
        var engine = CreateEngine();
        var initial = Assert.Single(engine.CreatePresentationSnapshot(ControlRoomRunState.Paused)
            .TurbineSecondary.AdmissionTrains);

        engine.QueueOperatorCommand(ValveCommand(
            ControlRoomCommandKind.TurbineValveClose,
            initial.StopValveId));
        var closing = Assert.Single(engine.Step(ControlRoomRunState.Paused)
            .TurbineSecondary.AdmissionTrains);
        Assert.InRange(closing.StopValvePosition.NumericValue!.Value, 0.000001d, 99.999999d);

        engine.QueueOperatorCommand(ValveCommand(
            ControlRoomCommandKind.TurbineValveOpen,
            initial.StopValveId));
        engine.QueueOperatorCommand(new ControlRoomCommand(ControlRoomCommandKind.TurbineTrip));
        var trippedSnapshot = engine.Step(ControlRoomRunState.Paused);
        var tripped = Assert.Single(trippedSnapshot.TurbineSecondary.AdmissionTrains);
        Assert.True(trippedSnapshot.TurbineTripActive);
        Assert.Equal(100d, tripped.StopValveRequestedPosition.NumericValue!.Value, 6);
        Assert.Equal(0d, tripped.StopValvePosition.NumericValue!.Value, 6);
        Assert.True(tripped.StopValveForcedClosed);

        var ready = trippedSnapshot;
        for (var step = 0; step < 200 && !ready.ProtectionReset.CanResetNow; step++)
        {
            ready = engine.Step(ControlRoomRunState.Paused);
        }

        Assert.True(ready.ProtectionReset.CanResetNow);
        engine.QueueOperatorCommand(new ControlRoomCommand(ControlRoomCommandKind.ProtectionReset));
        var resetSnapshot = engine.Step(ControlRoomRunState.Paused);
        var reset = Assert.Single(resetSnapshot.TurbineSecondary.AdmissionTrains);

        Assert.True(resetSnapshot.ProtectionReset.LastResetAccepted);
        Assert.False(resetSnapshot.TurbineTripActive);
        Assert.Equal(100d, reset.StopValveRequestedPosition.NumericValue!.Value, 6);
        Assert.InRange(reset.StopValvePosition.NumericValue!.Value, 0.000001d, 99.999999d);
        Assert.False(reset.StopValveForcedClosed);

        var resumed = Assert.Single(engine.Step(ControlRoomRunState.Paused)
            .TurbineSecondary.AdmissionTrains);
        Assert.True(resumed.StopValvePosition.NumericValue > reset.StopValvePosition.NumericValue);
    }

    [Fact]
    public void AdmissionValve_CloseRequest_UsesNormalActuatorTravel()
    {
        var engine = CreateEngine();
        var initial = Assert.Single(engine.CreatePresentationSnapshot(ControlRoomRunState.Paused)
            .TurbineSecondary.AdmissionTrains);

        engine.QueueOperatorCommand(ValveCommand(
            ControlRoomCommandKind.TurbineValveClose,
            initial.AdmissionValveId));
        var train = Assert.Single(engine.Step(ControlRoomRunState.Paused)
            .TurbineSecondary.AdmissionTrains);

        Assert.Equal(0d, train.AdmissionValveRequestedPosition.NumericValue!.Value, 6);
        Assert.True(train.AdmissionValvePosition.NumericValue < initial.AdmissionValvePosition.NumericValue);
    }

    [Fact]
    public void ManualDemand_IsRejectedUntilControlValveIsInManualMode()
    {
        var engine = CreateEngine();
        var valveId = Assert.Single(engine.CreatePresentationSnapshot(ControlRoomRunState.Paused)
            .TurbineSecondary.AdmissionTrains).ControlValveId;

        var exception = Assert.Throws<InvalidOperationException>(() => engine.QueueOperatorCommand(ValveCommand(
            ControlRoomCommandKind.TurbineControlValveManualDemandSet,
            valveId,
            50d)));

        Assert.Contains("MANUAL", exception.Message, StringComparison.Ordinal);
    }

    private static IntegratedAutomaticOperationRuntimeEngine CreateEngine()
        => Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            new DesktopSustainedGenerationInitialConditionFactory().CreateRuntimeEngine());

    private static ControlRoomCommand ValveCommand(
        ControlRoomCommandKind kind,
        string valveId,
        double? numericValue = null)
        => new(kind, valveId, ControlRoomCommandTargetKind.Valve, numericValue);
}
