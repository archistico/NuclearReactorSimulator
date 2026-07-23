using NuclearReactorSimulator.Domain.Physics.Control;
using NuclearReactorSimulator.Domain.Physics.Instrumentation;
using NuclearReactorSimulator.Domain.Physics.Reactor.ControlRods;
using Xunit;

namespace NuclearReactorSimulator.Domain.Tests.Physics.Control;

public sealed class ControlDefinitionTests
{
    [Fact]
    public void ControllerOutputRange_RequiresFiniteOrderedBoundsAndNormalizesDeterministically()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ControllerOutputRange(1d, 1d));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ControllerOutputRange(double.NaN, 1d));
        var range = new ControllerOutputRange(-10d, 30d);
        Assert.Equal(-10d, range.Clamp(-100d));
        Assert.Equal(0.5d, range.Normalize(10d));
        Assert.Equal(1d, range.Normalize(100d));
    }

    [Fact]
    public void PidDefinition_EnforcesAlgorithmCoefficientShape()
    {
        var range = new ControllerOutputRange(0d, 1d);
        Assert.Throws<ArgumentException>(() => new PidControllerDefinition("P", "pv", ControllerAlgorithmKind.Proportional, 1d, 1d, 0d, range));
        Assert.Throws<ArgumentException>(() => new PidControllerDefinition("PI", "pv", ControllerAlgorithmKind.ProportionalIntegral, 1d, 1d, 1d, range));

        var pid = new PidControllerDefinition("PID", "pv", ControllerAlgorithmKind.ProportionalIntegralDerivative, -2d, -0.5d, -0.1d, range);
        Assert.Equal(-2d, pid.ProportionalGain);
    }

    [Fact]
    public void ControlSystem_RequiresMeasuredChannelIdsFromCanonicalInstrumentation()
    {
        var instrumentation = Instrumentation();
        var controller = Controller("controller", "pv");
        var system = new ControlSystemDefinition("control", instrumentation, new[] { controller });

        Assert.Same(instrumentation, system.Instrumentation);
        Assert.Throws<ArgumentException>(() => new ControlSystemDefinition("bad", instrumentation, new[] { Controller("bad", "missing") }));
    }

    [Fact]
    public void ActuatorSystem_RejectsUnknownControllersAndDuplicatePhysicalTargets()
    {
        var control = new ControlSystemDefinition("control", Instrumentation(), new[] { Controller("A", "pv"), Controller("B", "pv") });
        var range = new ControllerOutputRange(0d, 100d);
        var valveA = ActuatorDefinition.Valve("VA", "A", "valve", range);
        var valveB = ActuatorDefinition.Valve("VB", "B", "valve", range);

        Assert.Throws<ArgumentException>(() => new ActuatorSystemDefinition("duplicate", control, new[] { valveA, valveB }));
        Assert.Throws<ArgumentException>(() => new ActuatorSystemDefinition("unknown", control, new[] { ActuatorDefinition.Pump("P", "missing", "pump", range) }));
    }


    [Fact]
    public void ActuatorTravelRate_IsOptionalVersionedAndMustBeFinitePositiveWhenSpecified()
    {
        var range = new ControllerOutputRange(0d, 100d);
        var legacyValve = ActuatorDefinition.Valve("legacy-valve", "controller", "valve", range);
        var dynamicValve = ActuatorDefinition.Valve(
            "dynamic-valve", "controller", "valve", range, travelRate: ActuatorTravelRate.FromFractionPerSecond(0.5d));
        var dynamicPump = ActuatorDefinition.Pump(
            "dynamic-pump", "controller", "pump", range, travelRate: ActuatorTravelRate.FromFractionPerSecond(0.25d));

        Assert.Null(legacyValve.TravelRate);
        Assert.True(dynamicValve.TravelRate.HasValue);
        Assert.Equal(0.5d, dynamicValve.TravelRate.GetValueOrDefault().FractionPerSecond);
        Assert.Equal(TimeSpan.FromSeconds(2d), dynamicValve.TravelRate.GetValueOrDefault().FullTravelTime);
        Assert.True(dynamicPump.TravelRate.HasValue);
        Assert.Equal(0.25d, dynamicPump.TravelRate.GetValueOrDefault().FractionPerSecond);
        Assert.Equal(TimeSpan.FromSeconds(4d), dynamicPump.TravelRate.GetValueOrDefault().FullTravelTime);
        Assert.Throws<ArgumentOutOfRangeException>(() => ActuatorTravelRate.FromFractionPerSecond(0d));
        Assert.Throws<ArgumentOutOfRangeException>(() => ActuatorTravelRate.FromFractionPerSecond(double.NaN));
    }

    [Fact]
    public void RodActuator_RequiresExplicitTargetKindAndKeepsDeadbandConfiguration()
    {
        var actuator = ActuatorDefinition.ControlRod(
            "rod-actuator",
            "controller",
            "rod-group",
            ControlRodCommandTargetKind.Group,
            new ControllerOutputRange(-1d, 1d),
            neutralDeadbandFraction: 0.1d,
            positiveOutputWithdraws: false);

        Assert.Equal(ActuatorTargetKind.ControlRod, actuator.TargetKind);
        Assert.Equal(ControlRodCommandTargetKind.Group, actuator.RodTargetKind);
        Assert.Equal(0.1d, actuator.RodNeutralDeadbandFraction);
        Assert.False(actuator.PositiveRodOutputWithdraws);
    }

    private static InstrumentationSystemDefinition Instrumentation()
        => new("instrumentation", new[]
        {
            new InstrumentChannelDefinition("pv", "source", "unit", new SignalRange(-100d, 100d), LinearSignalScale.NormalizedZeroToOne, TimeSpan.Zero),
        });

    private static PidControllerDefinition Controller(string id, string channel)
        => new(id, channel, ControllerAlgorithmKind.Proportional, 1d, 0d, 0d, new ControllerOutputRange(0d, 100d));
}
