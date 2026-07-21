using NuclearReactorSimulator.Domain.Physics.Control;
using NuclearReactorSimulator.Domain.Physics.Control.ReactorPrimary;
using NuclearReactorSimulator.Domain.Physics.Instrumentation;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor.ControlRods;
using NuclearReactorSimulator.Domain.Physics.Reactor.Neutronics;
using NuclearReactorSimulator.Domain.Physics.Reactor.ThermalPower;
using NuclearReactorSimulator.Domain.Physics.TurbineIsland.Integration;
using NuclearReactorSimulator.Simulation.Physics.Control;
using NuclearReactorSimulator.Simulation.Physics.Control.ReactorPrimary;
using NuclearReactorSimulator.Simulation.Physics.Instrumentation;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Integration;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Physics.Control.ReactorPrimary;

public sealed class ReactorPrimaryControlSolverTests
{
    [Fact]
    public void Step_UsesMeasuredPowerAndFlowToMoveRodsAndCommandCanonicalMainCirculationPump()
    {
        var fixture = CreateFixture();
        var solver = new ReactorPrimaryControlSolver(fixture.ControlDefinition);

        var result = solver.Step(
            fixture.Signals,
            fixture.FullPlantState,
            fixture.ControlState,
            fixture.ControlInputs,
            TimeSpan.FromSeconds(1d));

        Assert.Equal(ControlRodMotion.Withdraw, result.CandidateState.ControlRods.GetRod("rod-1").Motion);
        Assert.Equal(0.6d, result.CandidateState.ControlRods.GetRod("rod-1").Position.FractionWithdrawn, 10);
        Assert.Equal(1d, result.CommandedFullPlantState.PlantState.GetPump("pump").Speed.Fraction, 10);
        Assert.True(result.CommandedFullPlantState.PlantState.GetPump("pump").IsRunning);
        Assert.True(result.Snapshot.CandidateRodReactivity.Total > result.Snapshot.CommittedRodReactivity.Total);
        Assert.Equal(2, result.Snapshot.Loops.Count);
    }


    [Fact]
    public void RodMotion_FeedsValidatedPointKineticsOnFollowingCommittedStep()
    {
        var fixture = CreateFixture();
        var solver = new ReactorPrimaryControlSolver(fixture.ControlDefinition);

        var first = solver.Step(
            fixture.Signals, fixture.FullPlantState, fixture.ControlState, fixture.ControlInputs, TimeSpan.FromSeconds(1d));
        var second = solver.Step(
            fixture.Signals, fixture.FullPlantState, first.CandidateState, fixture.ControlInputs, TimeSpan.FromMilliseconds(100d));

        Assert.Equal(100_000_000d, first.Snapshot.FissionPower.TotalFissionThermalPower.Watts, 4);
        Assert.True(second.Snapshot.TotalReactivityUsed > Reactivity.Zero);
        Assert.True(second.Snapshot.FissionPower.TotalFissionThermalPower > first.Snapshot.FissionPower.TotalFissionThermalPower);
    }

    [Fact]
    public void ControlledFullPlant_RewritesManualFissionPowerSeamFromValidatedPointKinetics()
    {
        var fixture = CreateFixture();
        var baseInputs = new IntegratedSecondaryCycleInputs(fixture.FullPlantDefinition, fixture.GeneratorInputs);
        var solver = new ReactorPrimaryControlledFullPlantSolver(fixture.ControlDefinition, fixture.ThermodynamicModel);

        var result = solver.Step(
            fixture.Signals,
            fixture.FullPlantState,
            fixture.ControlState,
            baseInputs,
            fixture.ControlInputs,
            TimeSpan.FromMilliseconds(1d));

        var effectivePower = result.EffectivePlantInputs
            .GeneratorGridInputs
            .TurbineInputs
            .MainSteamInputs
            .PrimaryCircuitInputs
            .TotalFissionThermalPower;
        var originalPower = baseInputs
            .GeneratorGridInputs
            .TurbineInputs
            .MainSteamInputs
            .PrimaryCircuitInputs
            .TotalFissionThermalPower;

        Assert.Equal(Power.Zero, originalPower);
        Assert.Equal(100_000_000d, effectivePower.Watts, 4);
        Assert.Equal(effectivePower, result.Snapshot.Control.FissionPower.TotalFissionThermalPower);
    }

    [Fact]
    public void Catalog_ExposesCanonicalMainCirculationMeasurementSources()
    {
        var fixture = CreateFixture();
        var catalog = InstrumentSignalSourceCatalog.CreateFullPlantCatalog(fixture.FullPlantDefinition);

        Assert.Equal("kg/s", catalog.GetSource("main-circulation-loop/loop/total-pump-flow").EngineeringUnitSymbol);
        Assert.Equal("Pa", catalog.GetSource("main-circulation-loop/loop/header-pressure-rise").EngineeringUnitSymbol);
    }

    [Fact]
    public void Definition_RejectsPowerLoopThatDoesNotUseMeasuredReactorThermalPower()
    {
        var fixture = CreateFixture();
        var badInstrumentation = new InstrumentationSystemDefinition("bad-inst", new[]
        {
            Channel("power", "plant/primary/total-mass", "kg"),
            Channel("flow", "main-circulation-loop/loop/total-pump-flow", "kg/s"),
        });
        var control = new ControlSystemDefinition("control", badInstrumentation, new[]
        {
            Controller("power-control", "power", new ControllerOutputRange(-1d, 1d), 1d),
            Controller("flow-control", "flow", new ControllerOutputRange(0d, 100d), 0.1d),
        });
        var actuators = new ActuatorSystemDefinition("actuators", control, new[]
        {
            ActuatorDefinition.ControlRod("rod-actuator", "power-control", "regulating", ControlRodCommandTargetKind.Group, new ControllerOutputRange(-1d, 1d)),
            ActuatorDefinition.Pump("pump-actuator", "flow-control", "pump", new ControllerOutputRange(0d, 100d)),
        });

        Assert.Throws<ArgumentException>(() => new ReactorPrimaryControlSystemDefinition(
            "reactor-primary-control",
            fixture.FullPlantDefinition,
            fixture.RodDefinition,
            fixture.KineticsParameters,
            fixture.FissionPowerDefinition,
            actuators,
            new[]
            {
                new ReactorPrimaryControlLoopDefinition("power-loop", ReactorPrimaryControlLoopKind.ReactorPowerRodRegulation, "power-control", "rod-actuator"),
                new ReactorPrimaryControlLoopDefinition("flow-loop", ReactorPrimaryControlLoopKind.MainCirculationPumpFlow, "flow-control", "pump-actuator"),
            }));
    }

    private static Fixture CreateFixture()
    {
        var physical = global::NuclearReactorSimulator.Simulation.Tests.Physics.Electrical.GeneratorGridSolverTests.CreateFixture(
            3_000d,
            breakerClosed: true,
            generatorPhaseDegrees: 0d,
            gridPhaseDegrees: 0d);
        var fullPlantDefinition = new IntegratedSecondaryCycleDefinition("secondary-cycle", physical.Definition);
        var fullPlantState = new FullPlantState(fullPlantDefinition, physical.PlantState, physical.TurbineState, physical.ElectricalState);

        var instrumentation = new InstrumentationSystemDefinition("instrumentation", new[]
        {
            Channel("power", "plant/reactor/thermal-power", "W"),
            Channel("flow", "main-circulation-loop/loop/total-pump-flow", "kg/s"),
        });
        var control = new ControlSystemDefinition("control", instrumentation, new[]
        {
            Controller("power-control", "power", new ControllerOutputRange(-1d, 1d), 1d),
            Controller("flow-control", "flow", new ControllerOutputRange(0d, 100d), 0.1d),
        });
        var actuators = new ActuatorSystemDefinition("actuators", control, new[]
        {
            ActuatorDefinition.ControlRod(
                "rod-actuator", "power-control", "regulating", ControlRodCommandTargetKind.Group,
                new ControllerOutputRange(-1d, 1d), neutralDeadbandFraction: 0.05d, positiveOutputWithdraws: true),
            ActuatorDefinition.Pump("pump-actuator", "flow-control", "pump", new ControllerOutputRange(0d, 100d)),
        });
        var rodDefinition = new ControlRodSystemDefinition(
            new[]
            {
                new ControlRodDefinition(
                    "rod-1", "regulating", ControlRodTravelRate.FromFractionPerSecond(0.1d),
                    Reactivity.FromDeltaKOverK(-0.001d), Reactivity.FromDeltaKOverK(0.001d)),
            },
            new[] { new ControlRodGroupDefinition("regulating", new[] { "rod-1" }) });
        var kineticsParameters = new PointKineticsParameters(
            TimeSpan.FromSeconds(0.1d),
            new[] { new DelayedNeutronGroupDefinition("dn", DelayedNeutronFraction.FromFraction(0.0065d), DecayConstant.FromPerSecond(0.08d)) });
        var fissionPowerDefinition = new FissionPowerDefinition(
            "fission-power",
            new FissionPowerCalibration(NeutronPopulation.FromRelative(1d), Power.FromMegawatts(100d)),
            new[] { new FissionHeatDestinationDefinition("fuel", HeatDepositionFraction.Full) });
        var definition = new ReactorPrimaryControlSystemDefinition(
            "reactor-primary-control",
            fullPlantDefinition,
            rodDefinition,
            kineticsParameters,
            fissionPowerDefinition,
            actuators,
            new[]
            {
                new ReactorPrimaryControlLoopDefinition("power-loop", ReactorPrimaryControlLoopKind.ReactorPowerRodRegulation, "power-control", "rod-actuator"),
                new ReactorPrimaryControlLoopDefinition("flow-loop", ReactorPrimaryControlLoopKind.MainCirculationPumpFlow, "flow-control", "pump-actuator"),
            });
        var rodState = new ControlRodSystemState(new[]
        {
            new ControlRodState("rod-1", ControlRodPosition.FromFractionWithdrawn(0.5d)),
        });
        var kineticsState = PointKineticsState.CreateCriticalEquilibrium(kineticsParameters, NeutronPopulation.FromRelative(1d));
        var controlState = ReactorPrimaryControlState.CreateInitial(definition, rodState, kineticsState);
        var controllerInputs = new ControllerInputs(control, new[]
        {
            new ControllerInput("power-control", ControllerMode.Automatic, 100d, 0d),
            new ControllerInput("flow-control", ControllerMode.Automatic, 1_000d, 0d),
        });
        var signals = new MeasuredSignalFrame(instrumentation, new[]
        {
            new MeasuredSignal("power", "W", 0d, 0d, SignalValidity.Valid, SignalQuality.Good, false, SensorFaultMode.None),
            new MeasuredSignal("flow", "kg/s", 0d, 0d, SignalValidity.Valid, SignalQuality.Good, false, SensorFaultMode.None),
        });

        var controlInputs = new ReactorPrimaryControlInputs(definition, controllerInputs, Reactivity.Zero);
        return new Fixture(
            fullPlantDefinition, fullPlantState, rodDefinition, kineticsParameters, fissionPowerDefinition,
            definition, controlState, controllerInputs, controlInputs, signals, physical.Inputs, physical.ThermodynamicModel);
    }

    private static InstrumentChannelDefinition Channel(string id, string sourceId, string unit)
        => new(id, sourceId, unit, new SignalRange(-1e12d, 1e12d), LinearSignalScale.NormalizedZeroToOne, TimeSpan.Zero, false);

    private static PidControllerDefinition Controller(string id, string channelId, ControllerOutputRange range, double kp)
        => new(id, channelId, ControllerAlgorithmKind.Proportional, kp, 0d, 0d, range);

    private sealed record Fixture(
        IntegratedSecondaryCycleDefinition FullPlantDefinition,
        FullPlantState FullPlantState,
        ControlRodSystemDefinition RodDefinition,
        PointKineticsParameters KineticsParameters,
        FissionPowerDefinition FissionPowerDefinition,
        ReactorPrimaryControlSystemDefinition ControlDefinition,
        ReactorPrimaryControlState ControlState,
        ControllerInputs ControllerInputs,
        ReactorPrimaryControlInputs ControlInputs,
        MeasuredSignalFrame Signals,
        global::NuclearReactorSimulator.Simulation.Physics.Electrical.GeneratorGridInputs GeneratorInputs,
        global::NuclearReactorSimulator.Simulation.Physics.Fluids.IFluidThermodynamicModel ThermodynamicModel);
}
