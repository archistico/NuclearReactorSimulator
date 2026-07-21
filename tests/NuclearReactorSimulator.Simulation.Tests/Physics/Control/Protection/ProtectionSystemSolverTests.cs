using NuclearReactorSimulator.Domain.Physics.Control;
using NuclearReactorSimulator.Domain.Physics.Control.Protection;
using NuclearReactorSimulator.Domain.Physics.Instrumentation;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor.Neutronics;
using NuclearReactorSimulator.Simulation.Physics.Control;
using NuclearReactorSimulator.Simulation.Physics.Control.ReactorPrimary;
using NuclearReactorSimulator.Simulation.Physics.Control.Protection;
using NuclearReactorSimulator.Simulation.Physics.Control.TurbineSecondary;
using NuclearReactorSimulator.Simulation.Physics.Instrumentation;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Integration;
using NuclearReactorSimulator.Simulation.Tests.Physics.Control.TurbineSecondary;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Physics.Control.Protection;

public sealed class ProtectionSystemSolverTests
{
    [Fact]
    public void TripFunction_LatchesAndResetRequiresSafeThresholdAndPermissive()
    {
        var fixture = TurbineSecondaryControlSolverTests.CreateFixture();
        var definition = CreateProtectionDefinition(fixture, ProtectionAction.ReactorScram);
        var solver = new ProtectionSystemSolver(definition);
        var initial = ProtectionSystemState.CreateInitial(definition);

        var tripped = solver.Step(fixture.Signals, initial, new ProtectionSystemInputs(definition));
        Assert.True(tripped.Snapshot.ReactorScramActive);
        Assert.True(tripped.CandidateState.IsFunctionLatched("high-pressure"));

        var safeButNotPermitted = WithValues(fixture.Signals, pressure: 4_000_000d, level: 0d);
        var rejected = solver.Step(safeButNotPermitted, tripped.CandidateState, new ProtectionSystemInputs(definition, resetRequested: true));
        Assert.True(rejected.Snapshot.ResetRejected);
        Assert.True(rejected.Snapshot.ReactorScramActive);

        var safeAndPermitted = WithValues(fixture.Signals, pressure: 4_000_000d, level: 1d);
        var reset = solver.Step(safeAndPermitted, rejected.CandidateState, new ProtectionSystemInputs(definition, resetRequested: true));
        Assert.True(reset.Snapshot.ResetAccepted);
        Assert.False(reset.Snapshot.ReactorScramActive);
        Assert.False(reset.CandidateState.IsFunctionLatched("high-pressure"));
    }

    [Fact]
    public void InvalidMeasurement_TripsFailClosedWhenConfigured()
    {
        var fixture = TurbineSecondaryControlSolverTests.CreateFixture();
        var definition = CreateProtectionDefinition(fixture, ProtectionAction.TurbineTrip);
        var invalid = ReplaceSignal(
            fixture.Signals,
            "pressure",
            new MeasuredSignal("pressure", "Pa", null, null, SignalValidity.Invalid, SignalQuality.Bad, false, SensorFaultMode.Unavailable));

        var result = new ProtectionSystemSolver(definition).Step(
            invalid,
            ProtectionSystemState.CreateInitial(definition),
            new ProtectionSystemInputs(definition));

        Assert.True(result.Snapshot.TurbineTripActive);
        Assert.True(result.Snapshot.Functions.Single().TriggerActive);
    }

    [Fact]
    public void Interlock_IsNonLatchingAndClearsWhenMeasuredConditionClears()
    {
        var fixture = TurbineSecondaryControlSolverTests.CreateFixture();
        var definition = new ProtectionSystemDefinition(
            "interlocks",
            fixture.FullPlantDefinition,
            fixture.Instrumentation,
            Array.Empty<ProtectionFunctionDefinition>(),
            new[]
            {
                new ProtectionInterlockDefinition(
                    "overspeed-close-inhibit",
                    "speed",
                    ProtectionComparison.High,
                    2_900d,
                    ProtectionInterlockAction.BlockGeneratorBreakerClose | ProtectionInterlockAction.BlockTurbineAdmissionOpening),
            });
        var solver = new ProtectionSystemSolver(definition);
        var state = ProtectionSystemState.CreateInitial(definition);

        var active = solver.Step(fixture.Signals, state, new ProtectionSystemInputs(definition));
        Assert.True(active.Snapshot.GeneratorBreakerCloseInhibited);
        Assert.True(active.Snapshot.TurbineAdmissionOpeningInhibited);

        var clearedSignals = WithValues(fixture.Signals, speed: 2_800d);
        var cleared = solver.Step(clearedSignals, active.CandidateState, new ProtectionSystemInputs(definition));
        Assert.False(cleared.Snapshot.GeneratorBreakerCloseInhibited);
        Assert.False(cleared.Snapshot.TurbineAdmissionOpeningInhibited);
    }

    [Fact]
    public void IntegratedTrip_OverridesNormalControlWithScramStopValveTurbineTripAndBreakerOpen()
    {
        var fixture = TurbineSecondaryControlSolverTests.CreateFixture();
        var definition = CreateProtectionDefinition(
            fixture,
            ProtectionAction.ReactorScram | ProtectionAction.TurbineTrip | ProtectionAction.GeneratorTrip);
        var solver = new ProtectedAutomaticFullPlantSolver(
            fixture.ReactorDefinition,
            fixture.SecondaryDefinition,
            definition,
            fixture.Physical.ThermodynamicModel);

        var result = solver.Step(
            fixture.Signals,
            fixture.FullPlantState,
            fixture.ReactorState,
            fixture.SecondaryState,
            ProtectionSystemState.CreateInitial(definition),
            new IntegratedSecondaryCycleInputs(fixture.FullPlantDefinition, fixture.Physical.Inputs),
            fixture.ReactorInputs,
            fixture.SecondaryInputs,
            new ProtectionSystemInputs(definition),
            TimeSpan.FromMilliseconds(1d));

        Assert.True(result.Snapshot.Protection.ReactorScramActive);
        Assert.True(result.Snapshot.Protection.TurbineTripActive);
        Assert.True(result.Snapshot.Protection.GeneratorTripActive);
        Assert.True(result.ReactorPrimaryControlStep.CandidateState.ControlRods.GetRod("rod-1").Position.FractionWithdrawn < 0.5d);
        Assert.True(result.FullPlantStep.CandidateState.PlantState.GetValve("stop").Position.IsClosed);
        Assert.True(result.EffectivePlantInputs.GeneratorGridInputs.TurbineInputs.GetRotorInput("rotor").TripCommand);
        Assert.Equal(MassFlowRate.Zero, result.EffectivePlantInputs.GeneratorGridInputs.TurbineInputs.GetStageGroupInput("stage").MassFlowRate);
        Assert.True(result.EffectivePlantInputs.GeneratorGridInputs.GetGeneratorInput("generator").OpenBreakerCommand);
        Assert.False(result.FullPlantStep.CandidateState.ElectricalState.GetGenerator("generator").BreakerClosed);
        Assert.Contains("stop", result.Snapshot.Arbitration.StopValvesForcedClosed);
    }

    [Fact]
    public void IntegratedInterlock_BlocksNormalRodWithdrawalWithoutCreatingATripLatch()
    {
        var fixture = TurbineSecondaryControlSolverTests.CreateFixture();
        var definition = new ProtectionSystemDefinition(
            "rod-interlock",
            fixture.FullPlantDefinition,
            fixture.Instrumentation,
            Array.Empty<ProtectionFunctionDefinition>(),
            new[]
            {
                new ProtectionInterlockDefinition(
                    "overspeed-rod-inhibit",
                    "speed",
                    ProtectionComparison.High,
                    2_900d,
                    ProtectionInterlockAction.BlockRodWithdrawal),
            });
        var reactorInputs = new ReactorPrimaryControlInputs(
            fixture.ReactorDefinition,
            new ControllerInputs(fixture.ReactorDefinition.ActuatorSystem.ControlSystem, new[]
            {
                new ControllerInput("power-control", ControllerMode.Automatic, 101d, 0d),
                new ControllerInput("flow-control", ControllerMode.Automatic, 0d, 0d),
            }),
            Reactivity.Zero);
        var solver = new ProtectedAutomaticFullPlantSolver(
            fixture.ReactorDefinition,
            fixture.SecondaryDefinition,
            definition,
            fixture.Physical.ThermodynamicModel);

        var result = solver.Step(
            fixture.Signals,
            fixture.FullPlantState,
            fixture.ReactorState,
            fixture.SecondaryState,
            ProtectionSystemState.CreateInitial(definition),
            new IntegratedSecondaryCycleInputs(fixture.FullPlantDefinition, fixture.Physical.Inputs),
            reactorInputs,
            fixture.SecondaryInputs,
            new ProtectionSystemInputs(definition),
            TimeSpan.FromMilliseconds(1d));

        Assert.True(result.Snapshot.Protection.RodWithdrawalInhibited);
        Assert.Equal(0.5d, result.ReactorPrimaryControlStep.CandidateState.ControlRods.GetRod("rod-1").Position.FractionWithdrawn, 10);
        Assert.Equal(ProtectionAction.None, result.Snapshot.Protection.LatchedActions);
    }

    [Fact]
    public void ManualTrip_LatchesUntilAcceptedReset()
    {
        var fixture = TurbineSecondaryControlSolverTests.CreateFixture();
        var definition = new ProtectionSystemDefinition(
            "manual-protection",
            fixture.FullPlantDefinition,
            fixture.Instrumentation,
            Array.Empty<ProtectionFunctionDefinition>(),
            new[]
            {
                new ProtectionInterlockDefinition(
                    "dummy-interlock",
                    "speed",
                    ProtectionComparison.High,
                    100_000d,
                    ProtectionInterlockAction.BlockRodWithdrawal,
                    blockOnInvalidMeasurement: false),
            });
        var solver = new ProtectionSystemSolver(definition);
        var tripped = solver.Step(
            fixture.Signals,
            ProtectionSystemState.CreateInitial(definition),
            new ProtectionSystemInputs(definition, manualGeneratorTrip: true, resetRequested: true));
        Assert.True(tripped.Snapshot.GeneratorTripActive);
        Assert.True(tripped.Snapshot.ResetRejected);

        var reset = solver.Step(
            fixture.Signals,
            tripped.CandidateState,
            new ProtectionSystemInputs(definition, resetRequested: true));
        Assert.True(reset.Snapshot.ResetAccepted);
        Assert.False(reset.Snapshot.GeneratorTripActive);
    }

    [Fact]
    public void Definition_RejectsUnknownMeasuredChannel()
    {
        var fixture = TurbineSecondaryControlSolverTests.CreateFixture();
        Assert.Throws<KeyNotFoundException>(() => new ProtectionSystemDefinition(
            "bad",
            fixture.FullPlantDefinition,
            fixture.Instrumentation,
            new[]
            {
                new ProtectionFunctionDefinition(
                    "bad-function", "missing-channel", ProtectionComparison.High, 1d, 0d, ProtectionAction.ReactorScram),
            }));
    }

    private static ProtectionSystemDefinition CreateProtectionDefinition(
        TurbineSecondaryControlSolverTests.Fixture fixture,
        ProtectionAction action)
        => new(
            "protection",
            fixture.FullPlantDefinition,
            fixture.Instrumentation,
            new[]
            {
                new ProtectionFunctionDefinition(
                    "high-pressure",
                    "pressure",
                    ProtectionComparison.High,
                    5_000_000d,
                    4_500_000d,
                    action),
            },
            resetPermissives: new[]
            {
                new ProtectionPermissiveDefinition("level-ok", "level", ProtectionComparison.High, 0.5d),
            });

    private static MeasuredSignalFrame WithValues(
        MeasuredSignalFrame source,
        double? pressure = null,
        double? level = null,
        double? speed = null)
        => new(
            source.Definition,
            source.Signals.Select(signal => signal.ChannelId switch
            {
                "pressure" when pressure.HasValue => signal with { EngineeringValue = pressure.Value, ScaledValue = pressure.Value },
                "level" when level.HasValue => signal with { EngineeringValue = level.Value, ScaledValue = level.Value },
                "speed" when speed.HasValue => signal with { EngineeringValue = speed.Value, ScaledValue = speed.Value },
                _ => signal,
            }));

    private static MeasuredSignalFrame ReplaceSignal(MeasuredSignalFrame source, string channelId, MeasuredSignal replacement)
        => new(source.Definition, source.Signals.Select(signal => signal.ChannelId == channelId ? replacement : signal));
}
