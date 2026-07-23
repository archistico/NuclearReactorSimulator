using NuclearReactorSimulator.Domain.Physics.Control;
using NuclearReactorSimulator.Domain.Physics.Control.ReactorPrimary;
using NuclearReactorSimulator.Domain.Physics.Control.TurbineSecondary;
using NuclearReactorSimulator.Domain.Physics.Instrumentation;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor.ControlRods;
using NuclearReactorSimulator.Domain.Physics.Reactor.Neutronics;
using NuclearReactorSimulator.Domain.Physics.Reactor.ThermalPower;
using NuclearReactorSimulator.Domain.Physics.TurbineIsland.Integration;
using NuclearReactorSimulator.Simulation.Physics.Control;
using NuclearReactorSimulator.Simulation.Physics.Control.ReactorPrimary;
using NuclearReactorSimulator.Simulation.Physics.Control.TurbineSecondary;
using NuclearReactorSimulator.Simulation.Physics.Instrumentation;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Integration;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Physics.Control.TurbineSecondary;

public sealed class TurbineSecondaryControlSolverTests
{
    [Fact]
    public void Step_AppliesGovernorAdmissionAndFeedwaterCommandsToCanonicalPlantState()
    {
        var fixture = CreateFixture();
        var solver = new TurbineSecondaryControlSolver(fixture.SecondaryDefinition);

        var result = solver.Step(
            fixture.Signals,
            fixture.FullPlantState,
            fixture.SecondaryState,
            fixture.SecondaryInputs,
            TimeSpan.FromSeconds(1d));

        Assert.True(result.CommandedFullPlantState.PlantState.GetValve("control").Position.IsClosed);
        Assert.True(result.CommandedFullPlantState.PlantState.GetValve("admission").Position.IsFullyOpen);
        Assert.Equal(1d, result.CommandedFullPlantState.PlantState.GetPump("feedwater-pump").Speed.Fraction, 10);
        Assert.True(result.CommandedFullPlantState.PlantState.GetPump("feedwater-pump").IsRunning);
        Assert.True(result.CommandedFullPlantState.PlantState.GetPump("condensate-pump").Speed.IsStopped);
        Assert.False(result.CommandedFullPlantState.PlantState.GetPump("condensate-pump").IsRunning);
        Assert.Equal(4, result.Snapshot.Loops.Count);
    }


    [Fact]
    public void Step_RateLimitedSecondaryActuatorsMoveTowardCommandsWithoutInstantaneousJumps()
    {
        var fixture = CreateFixture(valveTravelRatePerSecond: 0.5d, pumpTravelRatePerSecond: 0.25d);
        var solver = new TurbineSecondaryControlSolver(fixture.SecondaryDefinition);

        var committedControlValve = fixture.FullPlantState.PlantState.GetValve("control");
        var committedCondensatePump = fixture.FullPlantState.PlantState.GetPump("condensate-pump");
        Assert.True(committedControlValve.Position.IsFullyOpen);
        Assert.Equal(1d, committedCondensatePump.Speed.Fraction, 12);

        var result = solver.Step(
            fixture.Signals,
            fixture.FullPlantState,
            fixture.SecondaryState,
            fixture.SecondaryInputs,
            TimeSpan.FromMilliseconds(100d));

        var requestedControl = Assert.Single(
            result.ControlAndActuatorStep.Snapshot.ActuatorCommands.ValveCommands,
            static command => command.ValveId == "control");
        var requestedCondensate = Assert.Single(
            result.ControlAndActuatorStep.Snapshot.ActuatorCommands.PumpCommands,
            static command => command.PumpId == "condensate-pump");
        Assert.True(requestedControl.RequestedPosition.IsClosed);
        Assert.True(requestedCondensate.RequestedSpeed.IsStopped);
        Assert.False(requestedCondensate.RunCommand);

        var physicalControl = result.CommandedFullPlantState.PlantState.GetValve("control");
        var physicalCondensate = result.CommandedFullPlantState.PlantState.GetPump("condensate-pump");
        Assert.Equal(0.95d, physicalControl.Position.Fraction, 12);
        Assert.Equal(0.975d, physicalCondensate.Speed.Fraction, 12);
        Assert.True(physicalCondensate.IsRunning);

        var completed = solver.Step(
            fixture.Signals,
            fixture.FullPlantState,
            fixture.SecondaryState,
            fixture.SecondaryInputs,
            TimeSpan.FromSeconds(5d));
        Assert.True(completed.CommandedFullPlantState.PlantState.GetValve("control").Position.IsClosed);
        Assert.True(completed.CommandedFullPlantState.PlantState.GetPump("condensate-pump").Speed.IsStopped);
        Assert.False(completed.CommandedFullPlantState.PlantState.GetPump("condensate-pump").IsRunning);
    }

    [Fact]
    public void IntegratedStep_DerivesTurbineFlowFromCommandedAdmissionPathAndKeepsValidatedM53Kinetics()
    {
        var fixture = CreateFixture();
        var solver = new TurbineSecondaryControlledFullPlantSolver(
            fixture.ReactorDefinition,
            fixture.SecondaryDefinition,
            fixture.Physical.ThermodynamicModel);

        var result = solver.Step(
            fixture.Signals,
            fixture.FullPlantState,
            fixture.ReactorState,
            fixture.SecondaryState,
            new IntegratedSecondaryCycleInputs(fixture.FullPlantDefinition, fixture.Physical.Inputs),
            fixture.ReactorInputs,
            fixture.SecondaryInputs,
            TimeSpan.FromMilliseconds(1d));

        var stageInput = result.EffectivePlantInputs.GeneratorGridInputs.TurbineInputs.GetStageGroupInput("stage");
        var fissionPower = result.EffectivePlantInputs.GeneratorGridInputs.TurbineInputs.MainSteamInputs.PrimaryCircuitInputs.TotalFissionThermalPower;

        Assert.Equal(MassFlowRate.Zero, stageInput.MassFlowRate);
        Assert.Equal(100_000_000d, fissionPower.Watts, 4);
        Assert.Equal(Power.Zero, result.FullPlantStep.Snapshot.IntegratedCycle.TurbineExpansion.TotalShaftPower);
    }

    [Fact]
    public void Definition_RejectsStopValveAsNormalGovernorTarget()
    {
        var fixture = CreateFixture();
        var control = new ControlSystemDefinition("bad-secondary-control", fixture.Instrumentation, new[]
        {
            Controller("speed-control", "speed", new ControllerOutputRange(0d, 100d), 0.1d),
        });
        var actuators = new ActuatorSystemDefinition("bad-secondary-actuators", control, new[]
        {
            ActuatorDefinition.Valve("stop-actuator", "speed-control", "stop", new ControllerOutputRange(0d, 100d)),
        });

        Assert.Throws<ArgumentException>(() => new TurbineSecondaryControlSystemDefinition(
            "bad-secondary",
            fixture.FullPlantDefinition,
            actuators,
            new[]
            {
                new TurbineSecondaryControlLoopDefinition(
                    "speed-loop", TurbineSecondaryControlLoopKind.TurbineSpeedAdmission, "speed-control", "stop-actuator"),
            }));
    }

    [Fact]
    public void Catalog_ExposesCanonicalHotwellInventorySource()
    {
        var fixture = CreateFixture();
        var catalog = InstrumentSignalSourceCatalog.CreateFullPlantCatalog(fixture.FullPlantDefinition);

        Assert.Equal("kg", catalog.GetSource("condenser/condenser/hotwell-mass").EngineeringUnitSymbol);
    }

    internal static Fixture CreateFixture(
        double? valveTravelRatePerSecond = null,
        double? pumpTravelRatePerSecond = null)
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
            Channel("speed", "turbine-rotor/rotor/speed", "rpm"),
            Channel("pressure", "steam-drum/drum-a/pressure", "Pa"),
            Channel("level", "steam-drum/drum-a/level", "fraction"),
            Channel("hotwell", "condenser/condenser/hotwell-mass", "kg"),
        });

        var reactorControl = new ControlSystemDefinition("reactor-control", instrumentation, new[]
        {
            Controller("power-control", "power", new ControllerOutputRange(-1d, 1d), 1d),
            Controller("flow-control", "flow", new ControllerOutputRange(0d, 100d), 0.1d),
        });
        var reactorActuators = new ActuatorSystemDefinition("reactor-actuators", reactorControl, new[]
        {
            ActuatorDefinition.ControlRod(
                "rod-actuator", "power-control", "regulating", ControlRodCommandTargetKind.Group,
                new ControllerOutputRange(-1d, 1d)),
            ActuatorDefinition.Pump("mcp-actuator", "flow-control", "pump", new ControllerOutputRange(0d, 100d)),
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
        var reactorDefinition = new ReactorPrimaryControlSystemDefinition(
            "reactor-primary",
            fullPlantDefinition,
            rodDefinition,
            kineticsParameters,
            fissionPowerDefinition,
            reactorActuators,
            new[]
            {
                new ReactorPrimaryControlLoopDefinition("power-loop", ReactorPrimaryControlLoopKind.ReactorPowerRodRegulation, "power-control", "rod-actuator"),
                new ReactorPrimaryControlLoopDefinition("flow-loop", ReactorPrimaryControlLoopKind.MainCirculationPumpFlow, "flow-control", "mcp-actuator"),
            });

        var secondaryControl = new ControlSystemDefinition("secondary-control", instrumentation, new[]
        {
            Controller("speed-control", "speed", new ControllerOutputRange(0d, 100d), 1d),
            Controller("pressure-control", "pressure", new ControllerOutputRange(0d, 100d), 0.0001d),
            Controller("level-control", "level", new ControllerOutputRange(0d, 100d), 100d),
            Controller("hotwell-control", "hotwell", new ControllerOutputRange(0d, 100d), 0.01d),
        });
        var secondaryActuators = new ActuatorSystemDefinition("secondary-actuators", secondaryControl, new[]
        {
            ActuatorDefinition.Valve(
                "speed-actuator", "speed-control", "control", new ControllerOutputRange(0d, 100d), valveTravelRatePerSecond.HasValue ? ActuatorTravelRate.FromFractionPerSecond(valveTravelRatePerSecond.Value) : null),
            ActuatorDefinition.Valve(
                "pressure-actuator", "pressure-control", "admission", new ControllerOutputRange(0d, 100d), valveTravelRatePerSecond.HasValue ? ActuatorTravelRate.FromFractionPerSecond(valveTravelRatePerSecond.Value) : null),
            ActuatorDefinition.Pump(
                "feedwater-actuator", "level-control", "feedwater-pump", new ControllerOutputRange(0d, 100d), pumpTravelRatePerSecond.HasValue ? ActuatorTravelRate.FromFractionPerSecond(pumpTravelRatePerSecond.Value) : null),
            ActuatorDefinition.Pump(
                "condensate-actuator", "hotwell-control", "condensate-pump", new ControllerOutputRange(0d, 100d), pumpTravelRatePerSecond.HasValue ? ActuatorTravelRate.FromFractionPerSecond(pumpTravelRatePerSecond.Value) : null),
        });
        var secondaryDefinition = new TurbineSecondaryControlSystemDefinition(
            "secondary-controls",
            fullPlantDefinition,
            secondaryActuators,
            new[]
            {
                new TurbineSecondaryControlLoopDefinition("speed-loop", TurbineSecondaryControlLoopKind.TurbineSpeedAdmission, "speed-control", "speed-actuator"),
                new TurbineSecondaryControlLoopDefinition("pressure-loop", TurbineSecondaryControlLoopKind.SteamPressureAdmission, "pressure-control", "pressure-actuator"),
                new TurbineSecondaryControlLoopDefinition("level-loop", TurbineSecondaryControlLoopKind.SteamDrumLevelFeedwater, "level-control", "feedwater-actuator"),
                new TurbineSecondaryControlLoopDefinition("hotwell-loop", TurbineSecondaryControlLoopKind.HotwellInventoryCondensate, "hotwell-control", "condensate-actuator"),
            });

        var reactorState = ReactorPrimaryControlState.CreateInitial(
            reactorDefinition,
            new ControlRodSystemState(new[] { new ControlRodState("rod-1", ControlRodPosition.FromFractionWithdrawn(0.5d)) }),
            PointKineticsState.CreateCriticalEquilibrium(kineticsParameters, NeutronPopulation.FromRelative(1d)));
        var secondaryState = TurbineSecondaryControlState.CreateInitial(secondaryDefinition);

        var reactorInputs = new ReactorPrimaryControlInputs(
            reactorDefinition,
            new ControllerInputs(reactorControl, new[]
            {
                new ControllerInput("power-control", ControllerMode.Automatic, 100d, 0d),
                new ControllerInput("flow-control", ControllerMode.Automatic, 0d, 0d),
            }),
            Reactivity.Zero);
        var secondaryInputs = new TurbineSecondaryControlInputs(
            secondaryDefinition,
            new ControllerInputs(secondaryControl, new[]
            {
                new ControllerInput("speed-control", ControllerMode.Automatic, 0d, 0d),
                new ControllerInput("pressure-control", ControllerMode.Automatic, 10_000_000d, 0d),
                new ControllerInput("level-control", ControllerMode.Automatic, 1d, 0d),
                new ControllerInput("hotwell-control", ControllerMode.Automatic, 10_000d, 0d),
            }));

        var signals = new MeasuredSignalFrame(instrumentation, new[]
        {
            Signal("power", "W", 100d),
            Signal("flow", "kg/s", 0d),
            Signal("speed", "rpm", 3_000d),
            Signal("pressure", "Pa", 6_000_000d),
            Signal("level", "fraction", 0d),
            Signal("hotwell", "kg", 10_000d),
        });

        return new Fixture(
            physical,
            fullPlantDefinition,
            fullPlantState,
            instrumentation,
            reactorDefinition,
            reactorState,
            reactorInputs,
            secondaryDefinition,
            secondaryState,
            secondaryInputs,
            signals);
    }

    private static InstrumentChannelDefinition Channel(string id, string sourceId, string unit)
        => new(id, sourceId, unit, new SignalRange(-1e12d, 1e12d), LinearSignalScale.NormalizedZeroToOne, TimeSpan.Zero, false);

    private static PidControllerDefinition Controller(string id, string channelId, ControllerOutputRange range, double kp)
        => new(id, channelId, ControllerAlgorithmKind.Proportional, kp, 0d, 0d, range);

    private static MeasuredSignal Signal(string id, string unit, double value)
        => new(id, unit, value, value, SignalValidity.Valid, SignalQuality.Good, false, SensorFaultMode.None);

    internal sealed record Fixture(
        global::NuclearReactorSimulator.Simulation.Tests.Physics.Electrical.GeneratorGridSolverTests.Fixture Physical,
        IntegratedSecondaryCycleDefinition FullPlantDefinition,
        FullPlantState FullPlantState,
        InstrumentationSystemDefinition Instrumentation,
        ReactorPrimaryControlSystemDefinition ReactorDefinition,
        ReactorPrimaryControlState ReactorState,
        ReactorPrimaryControlInputs ReactorInputs,
        TurbineSecondaryControlSystemDefinition SecondaryDefinition,
        TurbineSecondaryControlState SecondaryState,
        TurbineSecondaryControlInputs SecondaryInputs,
        MeasuredSignalFrame Signals);
}
