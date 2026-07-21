using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Faults;
using NuclearReactorSimulator.Application.Scenarios.Faults.InstrumentationControl;
using NuclearReactorSimulator.Application.Scenarios.Operations;
using NuclearReactorSimulator.Domain.Physics.Instrumentation;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Faults;

public sealed class InstrumentationControlFaultTests
{
    [Fact]
    public void SensorBias_UsesCanonicalM51MeasuredSignalSeam()
    {
        var reference = CreateEngine();
        var faulted = CreateEngine();
        ((IInstrumentationControlFaultTarget)faulted).ActivateSensorBias("bias", "power", 5_000_000d);

        reference.Step(ControlRoomRunState.Paused);
        faulted.Step(ControlRoomRunState.Paused);

        var normal = reference.CurrentState.MeasuredSignals.GetSignal("power");
        var biased = faulted.CurrentState.MeasuredSignals.GetSignal("power");
        Assert.Equal(SensorFaultMode.Bias, biased.ActiveFaultMode);
        Assert.Equal(SignalValidity.Valid, biased.Validity);
        Assert.Equal(SignalQuality.Suspect, biased.Quality);
        Assert.True(normal.EngineeringValue.HasValue);
        Assert.True(biased.EngineeringValue.HasValue);
        Assert.Equal(normal.EngineeringValue.Value + 5_000_000d, biased.EngineeringValue.Value, 6);
    }

    [Fact]
    public void SensorFreeze_HoldsCommittedOutputAndMarksMeasurementInvalid()
    {
        var engine = CreateEngine();
        engine.Step(ControlRoomRunState.Paused);
        var committed = engine.CurrentState.MeasuredSignals.GetSignal("flow").EngineeringValue;
        var target = (IInstrumentationControlFaultTarget)engine;
        target.ActivateSensorFreeze("freeze", "flow");

        engine.Step(ControlRoomRunState.Paused);

        var frozen = engine.CurrentState.MeasuredSignals.GetSignal("flow");
        Assert.Equal(SensorFaultMode.Freeze, frozen.ActiveFaultMode);
        Assert.Equal(SignalValidity.Invalid, frozen.Validity);
        Assert.Equal(SignalQuality.Suspect, frozen.Quality);
        Assert.Equal(committed, frozen.EngineeringValue);
    }

    [Fact]
    public void InvalidProtectionChannel_PropagatesThroughCommittedMeasuredFrameAndTripsFailSafe()
    {
        var engine = CreateEngine();
        ((IInstrumentationControlFaultTarget)engine).ActivateSensorUnavailable("pressure-unavailable", "pressure");

        var first = engine.Step(ControlRoomRunState.Paused);
        Assert.False(first.ReactorScramActive);
        Assert.Equal(SensorFaultMode.Unavailable, engine.CurrentState.MeasuredSignals.GetSignal("pressure").ActiveFaultMode);

        var second = engine.Step(ControlRoomRunState.Paused);
        Assert.True(second.ReactorScramActive);
    }

    [Fact]
    public void ControllerOutputFailLow_ForcesCanonicalControllerCommandWithoutDirectPhysicalWrite()
    {
        var engine = CreateEngine();
        ((IInstrumentationControlFaultTarget)engine).ActivateControllerOutputFailLow("flow-low", "flow-control");

        engine.Step(ControlRoomRunState.Paused);

        var controller = engine.CurrentState.ReactorPrimaryControlState.ControlAndActuator.Controllers.GetController("flow-control");
        var pump = engine.CurrentState.PlantState.PlantState.GetPump("pump");
        Assert.Equal(0d, controller.LastOutput, 12);
        Assert.False(pump.IsRunning);
        Assert.Equal(0d, pump.Speed.Fraction, 12);
    }

    [Fact]
    public void ControllerOutputFreeze_HoldsCapturedOutputUntilFaultClears()
    {
        var engine = CreateEngine();
        engine.Step(ControlRoomRunState.Paused);
        var captured = engine.CurrentState.ReactorPrimaryControlState.ControlAndActuator.Controllers
            .GetController("flow-control").LastOutput;
        var target = (IInstrumentationControlFaultTarget)engine;
        target.ActivateControllerOutputFreeze("flow-freeze", "flow-control");
        engine.QueueOperatorCommand(new ControlRoomCommand(
            ControlRoomCommandKind.MainCirculationPumpStop,
            "pump",
            ControlRoomCommandTargetKind.Pump));

        engine.Step(ControlRoomRunState.Paused);

        Assert.Equal(
            captured,
            engine.CurrentState.ReactorPrimaryControlState.ControlAndActuator.Controllers.GetController("flow-control").LastOutput,
            12);
        Assert.True(engine.CurrentState.PlantState.PlantState.GetPump("pump").IsRunning);

        target.ClearInstrumentationControlFault("flow-freeze");
        engine.Step(ControlRoomRunState.Paused);
        Assert.False(engine.CurrentState.PlantState.PlantState.GetPump("pump").IsRunning);
    }

    [Fact]
    public void ActuatorCommandFreeze_CapturesCanonicalCommandPathState()
    {
        var engine = CreateEngine();
        engine.Step(ControlRoomRunState.Paused);
        var before = engine.CurrentState.TurbineSecondaryControlState.ControlAndActuator.Actuators.Actuators
            .Single(static state => state.ActuatorId == "speed-actuator").LastControllerOutput;
        ((IInstrumentationControlFaultTarget)engine).ActivateActuatorCommandFreeze("speed-command-freeze", "speed-actuator");
        engine.QueueOperatorCommand(new ControlRoomCommand(
            ControlRoomCommandKind.TurbineSpeedRaise,
            "rotor",
            ControlRoomCommandTargetKind.TurbineRotor));

        engine.Step(ControlRoomRunState.Paused);

        var after = engine.CurrentState.TurbineSecondaryControlState.ControlAndActuator.Actuators.Actuators
            .Single(static state => state.ActuatorId == "speed-actuator").LastControllerOutput;
        Assert.Equal(before, after, 12);
    }

    [Fact]
    public void ActuatorCommandFailLowAndHigh_UseCanonicalActuatorInputRangeBounds()
    {
        var lowEngine = CreateEngine();
        var highEngine = CreateEngine();
        var lowTarget = (IInstrumentationControlFaultTarget)lowEngine;
        var highTarget = (IInstrumentationControlFaultTarget)highEngine;

        lowTarget.ActivateActuatorCommandFailLow("feedwater-command-low", "feedwater-actuator");
        highTarget.ActivateActuatorCommandFailHigh("condensate-command-high", "condensate-actuator");

        lowEngine.Step(ControlRoomRunState.Paused);
        highEngine.Step(ControlRoomRunState.Paused);

        var lowDefinition = lowEngine.PersistentInputs.TurbineSecondaryInputs.Definition.ActuatorSystem
            .GetActuator("feedwater-actuator");
        var highDefinition = highEngine.PersistentInputs.TurbineSecondaryInputs.Definition.ActuatorSystem
            .GetActuator("condensate-actuator");
        var lowState = lowEngine.CurrentState.TurbineSecondaryControlState.ControlAndActuator.Actuators.Actuators
            .Single(static state => state.ActuatorId == "feedwater-actuator");
        var highState = highEngine.CurrentState.TurbineSecondaryControlState.ControlAndActuator.Actuators.Actuators
            .Single(static state => state.ActuatorId == "condensate-actuator");

        Assert.Equal(lowDefinition.InputRange.Minimum, lowState.LastControllerOutput, 12);
        Assert.Equal(highDefinition.InputRange.Maximum, highState.LastControllerOutput, 12);
    }

    [Fact]
    public void BuiltInScenarioPack_BindsAllM83FaultTypesWithoutCustomRegistry()
    {
        var registry = new VersionedInitialConditionRegistry(new IVersionedInitialConditionFactory[]
        {
            new PowerManoeuvringInitialConditionFactory(),
        });

        var session = new ScenarioSessionFactory(registry).Load(InstrumentationControlFaultScenarioPack.Demonstration);

        Assert.Equal(11, session.Scenario.Faults.Count);
        Assert.All(session.Scenario.Faults, static fault =>
            Assert.Contains(fault.FaultTypeId, InstrumentationControlFaultTypeIds.All));
        Assert.Equal(11, session.Coordinator.Current.Faults.PendingCount);
    }

    [Fact]
    public void InvalidInstrumentationOrControlTarget_FailsClosedAtActivationBoundary()
    {
        var registry = new VersionedInitialConditionRegistry(new IVersionedInitialConditionFactory[]
        {
            new PowerManoeuvringInitialConditionFactory(),
        });
        var scenario = new ScenarioDefinition(
            "invalid-m83-target",
            "Invalid M8.3 target",
            "Verifies fail-closed binding to canonical instrumentation IDs.",
            PowerManoeuvringNormalShutdownProgram.InitialCondition,
            faults: new[]
            {
                new ScenarioFaultDefinition(
                    "bad",
                    InstrumentationControlFaultTypeIds.SensorFreeze,
                    "missing-channel",
                    ScenarioFaultTriggerDefinition.AtLogicalStep(1)),
            });
        var session = new ScenarioSessionFactory(registry).Load(scenario);

        Assert.Throws<KeyNotFoundException>(() =>
            session.Coordinator.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.SingleStep)));
    }

    private static IntegratedAutomaticOperationRuntimeEngine CreateEngine()
        => Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            new PowerManoeuvringInitialConditionFactory().CreateRuntimeEngine());
}
