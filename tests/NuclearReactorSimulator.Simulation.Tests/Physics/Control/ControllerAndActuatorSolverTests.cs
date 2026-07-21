using NuclearReactorSimulator.Domain.Physics.Control;
using NuclearReactorSimulator.Domain.Physics.Instrumentation;
using NuclearReactorSimulator.Domain.Physics.Reactor.ControlRods;
using NuclearReactorSimulator.Simulation.Physics.Control;
using NuclearReactorSimulator.Simulation.Physics.Instrumentation;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Physics.Control;

public sealed class ControllerAndActuatorSolverTests
{
    [Fact]
    public void ProportionalController_ConsumesMeasuredSignalAndProducesDeterministicOutput()
    {
        var fixture = CreateFixture(ControllerAlgorithmKind.Proportional, 2d, 0d, 0d, new ControllerOutputRange(-100d, 100d));
        var state = new ControllerSystemState(fixture.Control, new[] { new ControllerChannelState("C", true, ControllerMode.Automatic, 0d, 0d, 0d) });
        var result = new ControllerSystemSolver(fixture.Control).Step(
            SignalFrame(fixture.Instrumentation, 3d),
            state,
            Inputs(fixture.Control, ControllerMode.Automatic, 5d, 0d),
            TimeSpan.FromSeconds(1d));

        Assert.Equal(4d, result.Snapshot.Outputs.GetOutput("C").Output);
        Assert.Equal(2d, result.Snapshot.GetDiagnostic("C").Error);
    }

    [Fact]
    public void PIController_IntegratesOnlyFromCommittedStateAndFixedTimestep()
    {
        var fixture = CreateFixture(ControllerAlgorithmKind.ProportionalIntegral, 0d, 0.5d, 0d, new ControllerOutputRange(-100d, 100d));
        var state = new ControllerSystemState(fixture.Control, new[] { new ControllerChannelState("C", true, ControllerMode.Automatic, 1d, 4d, 1d) });
        var result = new ControllerSystemSolver(fixture.Control).Step(
            SignalFrame(fixture.Instrumentation, 1d),
            state,
            Inputs(fixture.Control, ControllerMode.Automatic, 5d, 0d),
            TimeSpan.FromSeconds(2d));

        Assert.Equal(5d, result.CandidateState.GetController("C").IntegralTerm);
        Assert.Equal(5d, result.Snapshot.Outputs.GetOutput("C").Output);
    }

    [Fact]
    public void PIDController_DerivativeUsesCommittedPreviousErrorAndFixedTimestep()
    {
        var fixture = CreateFixture(ControllerAlgorithmKind.ProportionalIntegralDerivative, 0d, 0d, 2d, new ControllerOutputRange(-100d, 100d));
        var state = new ControllerSystemState(fixture.Control, new[] { new ControllerChannelState("C", true, ControllerMode.Automatic, 0d, 1d, 0d) });
        var result = new ControllerSystemSolver(fixture.Control).Step(
            SignalFrame(fixture.Instrumentation, 6d),
            state,
            Inputs(fixture.Control, ControllerMode.Automatic, 10d, 0d),
            TimeSpan.FromSeconds(0.5d));

        Assert.Equal(12d, result.Snapshot.GetDiagnostic("C").DerivativeTerm);
        Assert.Equal(12d, result.Snapshot.Outputs.GetOutput("C").Output);
    }

    [Fact]
    public void ManualMode_ClampsCommandWithoutRunningAutomaticLaw()
    {
        var fixture = CreateFixture(ControllerAlgorithmKind.ProportionalIntegralDerivative, 10d, 5d, 2d, new ControllerOutputRange(0d, 100d));
        var result = new ControllerSystemSolver(fixture.Control).Step(
            SignalFrame(fixture.Instrumentation, 99d),
            ControllerSystemState.CreateUninitialized(fixture.Control),
            Inputs(fixture.Control, ControllerMode.Manual, -1000d, 150d),
            TimeSpan.FromMilliseconds(10d));

        var output = result.Snapshot.Outputs.GetOutput("C");
        Assert.Equal(100d, output.Output);
        Assert.True(output.IsSaturated);
        Assert.Equal(ControllerExecutionStatus.Manual, output.Status);
    }

    [Fact]
    public void ManualToAutomaticTransfer_TracksPreviousOutputWithoutBump()
    {
        var fixture = CreateFixture(ControllerAlgorithmKind.ProportionalIntegral, 3d, 2d, 0d, new ControllerOutputRange(0d, 100d));
        var state = new ControllerSystemState(fixture.Control, new[] { new ControllerChannelState("C", true, ControllerMode.Manual, 0d, 0d, 42d) });
        var result = new ControllerSystemSolver(fixture.Control).Step(
            SignalFrame(fixture.Instrumentation, 0d),
            state,
            Inputs(fixture.Control, ControllerMode.Automatic, 10d, 42d),
            TimeSpan.FromSeconds(1d));

        Assert.Equal(42d, result.Snapshot.Outputs.GetOutput("C").Output);
        Assert.True(result.Snapshot.GetDiagnostic("C").BumplessTransferApplied);
    }

    [Fact]
    public void Saturation_UsesConditionalIntegrationAntiWindup()
    {
        var fixture = CreateFixture(ControllerAlgorithmKind.ProportionalIntegral, 10d, 5d, 0d, new ControllerOutputRange(0d, 100d));
        var state = new ControllerSystemState(fixture.Control, new[] { new ControllerChannelState("C", true, ControllerMode.Automatic, 90d, 10d, 100d) });
        var result = new ControllerSystemSolver(fixture.Control).Step(
            SignalFrame(fixture.Instrumentation, 0d),
            state,
            Inputs(fixture.Control, ControllerMode.Automatic, 10d, 0d),
            TimeSpan.FromSeconds(1d));

        Assert.Equal(90d, result.CandidateState.GetController("C").IntegralTerm);
        Assert.Equal(100d, result.Snapshot.Outputs.GetOutput("C").Output);
        Assert.True(result.Snapshot.GetDiagnostic("C").AntiWindupActive);
    }

    [Fact]
    public void InvalidMeasurement_HoldsLastCommandAndDoesNotIntegrate()
    {
        var fixture = CreateFixture(ControllerAlgorithmKind.ProportionalIntegral, 1d, 1d, 0d, new ControllerOutputRange(0d, 100d));
        var state = new ControllerSystemState(fixture.Control, new[] { new ControllerChannelState("C", true, ControllerMode.Automatic, 25d, 2d, 33d) });
        var result = new ControllerSystemSolver(fixture.Control).Step(
            SignalFrame(fixture.Instrumentation, null, SignalValidity.Invalid, SignalQuality.Unavailable),
            state,
            Inputs(fixture.Control, ControllerMode.Automatic, 50d, 0d),
            TimeSpan.FromSeconds(10d));

        Assert.Equal(33d, result.Snapshot.Outputs.GetOutput("C").Output);
        Assert.Equal(25d, result.CandidateState.GetController("C").IntegralTerm);
        Assert.Equal(ControllerExecutionStatus.MeasurementUnavailable, result.Snapshot.Outputs.GetOutput("C").Status);
    }

    [Fact]
    public void AutomaticRequestWithUnavailableMeasurement_PreservesPendingBumplessTransferUntilMeasurementRecovers()
    {
        var fixture = CreateFixture(ControllerAlgorithmKind.ProportionalIntegral, 2d, 1d, 0d, new ControllerOutputRange(0d, 100d));
        var initial = new ControllerSystemState(fixture.Control, new[] { new ControllerChannelState("C", true, ControllerMode.Manual, 0d, 0d, 40d) });
        var solver = new ControllerSystemSolver(fixture.Control);
        var inputs = Inputs(fixture.Control, ControllerMode.Automatic, 10d, 40d);

        var unavailable = solver.Step(
            SignalFrame(fixture.Instrumentation, null, SignalValidity.Invalid, SignalQuality.Unavailable),
            initial,
            inputs,
            TimeSpan.FromSeconds(1d));
        var recovered = solver.Step(
            SignalFrame(fixture.Instrumentation, 0d),
            unavailable.CandidateState,
            inputs,
            TimeSpan.FromSeconds(1d));

        Assert.Equal(ControllerMode.Manual, unavailable.CandidateState.GetController("C").LastMode);
        Assert.Equal(40d, recovered.Snapshot.Outputs.GetOutput("C").Output);
        Assert.True(recovered.Snapshot.GetDiagnostic("C").BumplessTransferApplied);
    }

    [Fact]
    public void ActuatorBoundary_MapsControllerOutputToTypedValvePumpAndRodCommands()
    {
        var instrumentation = Instrumentation();
        var control = new ControlSystemDefinition("control", instrumentation, new[]
        {
            Controller("V", "pv", new ControllerOutputRange(0d, 100d)),
            Controller("P", "pv", new ControllerOutputRange(0d, 100d)),
            Controller("R", "pv", new ControllerOutputRange(-1d, 1d)),
        });
        var actuators = new ActuatorSystemDefinition("actuators", control, new[]
        {
            ActuatorDefinition.Valve("VA", "V", "valve", new ControllerOutputRange(0d, 100d)),
            ActuatorDefinition.Pump("PA", "P", "pump", new ControllerOutputRange(0d, 100d)),
            ActuatorDefinition.ControlRod("RA", "R", "rod-group", ControlRodCommandTargetKind.Group, new ControllerOutputRange(-1d, 1d)),
        });
        var outputs = new ControllerOutputFrame(control, new[]
        {
            new ControllerOutput("V", 25d, 25d, false, ControllerExecutionStatus.Automatic),
            new ControllerOutput("P", 60d, 60d, false, ControllerExecutionStatus.Automatic),
            new ControllerOutput("R", 1d, 1d, false, ControllerExecutionStatus.Automatic),
        });

        var result = new ActuatorSystemSolver(actuators).Step(outputs, ActuatorSystemState.CreateInitial(actuators));

        Assert.Equal(0.25d, result.Commands.ValveCommands.Single().RequestedPosition.Fraction);
        Assert.Equal(0.6d, result.Commands.PumpCommands.Single().RequestedSpeed.Fraction);
        Assert.True(result.Commands.PumpCommands.Single().RunCommand);
        Assert.Equal(ControlRodMotion.Withdraw, result.Commands.RodCommands.Single().Command.Motion);
    }

    [Fact]
    public void RodActuator_UsesConfiguredNeutralDeadbandAndDirection()
    {
        var instrumentation = Instrumentation();
        var control = new ControlSystemDefinition("control", instrumentation, new[] { Controller("R", "pv", new ControllerOutputRange(-1d, 1d)) });
        var actuator = ActuatorDefinition.ControlRod(
            "RA", "R", "rod", ControlRodCommandTargetKind.Rod, new ControllerOutputRange(-1d, 1d), 0.1d, positiveOutputWithdraws: false);
        var system = new ActuatorSystemDefinition("actuators", control, new[] { actuator });
        var solver = new ActuatorSystemSolver(system);

        var hold = solver.Step(Frame(control, 0d), ActuatorSystemState.CreateInitial(system));
        var positive = solver.Step(Frame(control, 1d), ActuatorSystemState.CreateInitial(system));
        var negative = solver.Step(Frame(control, -1d), ActuatorSystemState.CreateInitial(system));

        Assert.Equal(ControlRodMotion.Hold, hold.Commands.RodCommands.Single().Command.Motion);
        Assert.Equal(ControlRodMotion.Insert, positive.Commands.RodCommands.Single().Command.Motion);
        Assert.Equal(ControlRodMotion.Withdraw, negative.Commands.RodCommands.Single().Command.Motion);
    }

    [Fact]
    public void ControlAndActuatorComposition_ConsumesMeasuredFrameOnlyAndIsDeterministic()
    {
        var fixture = CreateFixture(ControllerAlgorithmKind.Proportional, 1d, 0d, 0d, new ControllerOutputRange(0d, 100d));
        var actuatorDefinition = new ActuatorSystemDefinition(
            "actuators",
            fixture.Control,
            new[] { ActuatorDefinition.Valve("VA", "C", "valve", new ControllerOutputRange(0d, 100d)) });
        var committed = new ControlAndActuatorState(
            actuatorDefinition,
            new ControllerSystemState(fixture.Control, new[] { new ControllerChannelState("C", true, ControllerMode.Automatic, 0d, 0d, 0d) }),
            ActuatorSystemState.CreateInitial(actuatorDefinition));
        var solver = new ControlAndActuatorSolver(actuatorDefinition);
        var signals = SignalFrame(fixture.Instrumentation, 20d);
        var inputs = Inputs(fixture.Control, ControllerMode.Automatic, 50d, 0d);

        var left = solver.Step(signals, committed, inputs, TimeSpan.FromSeconds(1d));
        var right = solver.Step(signals, committed, inputs, TimeSpan.FromSeconds(1d));

        Assert.Equal(left.ControllerStep.Snapshot.Outputs.GetOutput("C"), right.ControllerStep.Snapshot.Outputs.GetOutput("C"));
        Assert.Equal(left.ActuatorStep.Commands.ValveCommands.Single(), right.ActuatorStep.Commands.ValveCommands.Single());
        Assert.DoesNotContain(
            typeof(global::NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Integration.FullPlantSnapshot),
            typeof(ControllerSystemSolver).GetMethods().SelectMany(static method => method.GetParameters()).Select(static parameter => parameter.ParameterType));
    }

    private static ControllerOutputFrame Frame(ControlSystemDefinition definition, double value)
        => new(definition, new[] { new ControllerOutput("R", value, value, false, ControllerExecutionStatus.Automatic) });

    private static ControllerInputs Inputs(ControlSystemDefinition definition, ControllerMode mode, double setpoint, double manual)
        => new(definition, definition.Controllers.Select(controller => new ControllerInput(controller.Id, mode, setpoint, manual)));

    private static MeasuredSignalFrame SignalFrame(
        InstrumentationSystemDefinition definition,
        double? value,
        SignalValidity validity = SignalValidity.Valid,
        SignalQuality quality = SignalQuality.Good)
        => new(definition, new[]
        {
            new MeasuredSignal("pv", "unit", value, value, validity, quality, false, SensorFaultMode.None),
        });

    private static InstrumentationSystemDefinition Instrumentation()
        => new("instrumentation", new[]
        {
            new InstrumentChannelDefinition("pv", "source", "unit", new SignalRange(-1_000d, 1_000d), LinearSignalScale.NormalizedZeroToOne, TimeSpan.Zero),
        });

    private static PidControllerDefinition Controller(string id, string channel, ControllerOutputRange range)
        => new(id, channel, ControllerAlgorithmKind.Proportional, 1d, 0d, 0d, range);

    private static Fixture CreateFixture(
        ControllerAlgorithmKind algorithm,
        double kp,
        double ki,
        double kd,
        ControllerOutputRange range)
    {
        var instrumentation = Instrumentation();
        var controller = new PidControllerDefinition("C", "pv", algorithm, kp, ki, kd, range);
        return new Fixture(instrumentation, new ControlSystemDefinition("control", instrumentation, new[] { controller }));
    }

    private sealed record Fixture(InstrumentationSystemDefinition Instrumentation, ControlSystemDefinition Control);
}
