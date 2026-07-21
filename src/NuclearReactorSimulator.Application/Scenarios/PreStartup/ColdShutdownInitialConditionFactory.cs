using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Domain.Physics.Control;
using NuclearReactorSimulator.Domain.Physics.Control.Alarms;
using NuclearReactorSimulator.Domain.Physics.Control.Protection;
using NuclearReactorSimulator.Domain.Physics.Control.ReactorPrimary;
using NuclearReactorSimulator.Domain.Physics.Control.TurbineSecondary;
using NuclearReactorSimulator.Domain.Physics.Electrical;
using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Instrumentation;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor.ControlRods;
using NuclearReactorSimulator.Domain.Physics.Reactor.Core;
using NuclearReactorSimulator.Domain.Physics.Reactor.Core.Channels;
using NuclearReactorSimulator.Domain.Physics.Reactor.Neutronics;
using NuclearReactorSimulator.Domain.Physics.Reactor.PrimaryCircuit.Boundaries;
using NuclearReactorSimulator.Domain.Physics.Reactor.PrimaryCircuit.Circulation;
using NuclearReactorSimulator.Domain.Physics.Reactor.PrimaryCircuit.Integration;
using NuclearReactorSimulator.Domain.Physics.Reactor.PrimaryCircuit.SteamDrums;
using NuclearReactorSimulator.Domain.Physics.Reactor.ThermalPower;
using NuclearReactorSimulator.Domain.Physics.Thermal;
using NuclearReactorSimulator.Domain.Physics.TurbineIsland.Condenser;
using NuclearReactorSimulator.Domain.Physics.TurbineIsland.Feedwater;
using NuclearReactorSimulator.Domain.Physics.TurbineIsland.Integration;
using NuclearReactorSimulator.Domain.Physics.TurbineIsland.MainSteam;
using NuclearReactorSimulator.Domain.Physics.TurbineIsland.Turbine;
using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Physics.Control;
using NuclearReactorSimulator.Simulation.Physics.Control.Alarms;
using NuclearReactorSimulator.Simulation.Physics.Control.Integration;
using NuclearReactorSimulator.Simulation.Physics.Control.Protection;
using NuclearReactorSimulator.Simulation.Physics.Control.ReactorPrimary;
using NuclearReactorSimulator.Simulation.Physics.Control.TurbineSecondary;
using NuclearReactorSimulator.Simulation.Physics.Electrical;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using NuclearReactorSimulator.Simulation.Physics.Instrumentation;
using NuclearReactorSimulator.Simulation.Physics.Reactor.PrimaryCircuit.Boundaries;
using NuclearReactorSimulator.Simulation.Physics.Reactor.PrimaryCircuit.Integration;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Condenser;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Feedwater;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Integration;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.MainSteam;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Turbine;

namespace NuclearReactorSimulator.Application.Scenarios.PreStartup;

/// <summary>
/// M7.2 canonical built-in cold-shutdown/pre-start recipe. The recipe composes fresh M1-M5 owners and uses the validated
/// simplified water/steam closure; it does not deserialize or patch individual authoritative states after construction.
/// </summary>
public sealed class ColdShutdownInitialConditionFactory : IVersionedInitialConditionFactory
{
    private static readonly TimeSpan RuntimeStep = TimeSpan.FromMilliseconds(10d);

    public InitialConditionDescriptor Descriptor { get; } = new(
        ColdShutdownPreStartupProgram.InitialCondition,
        "Cold Shutdown / Pre-Startup v1",
        "Cold, subcritical, turbine-stopped and generator-isolated educational plant condition with all modeled pumps and steam-admission valves initially stopped/closed.");

    public IControlRoomRuntimeEngine CreateRuntimeEngine()
        => CreateRuntimeEngineForOperationalSeed(NeutronPopulation.Zero, mainCirculationRunning: false);

    internal static IControlRoomRuntimeEngine CreateRuntimeEngineForOperationalSeed(
        NeutronPopulation initialNeutronPopulation,
        bool mainCirculationRunning)
        => CreateRuntimeEngineForOperationalSeed(
            initialNeutronPopulation,
            mainCirculationRunning,
            ControlRodPosition.FullyInserted,
            initialPrimaryTemperatureCelsius: 50d,
            turbineStartupLineup: false);

    internal static IControlRoomRuntimeEngine CreateRuntimeEngineForOperationalSeed(
        NeutronPopulation initialNeutronPopulation,
        bool mainCirculationRunning,
        ControlRodPosition initialRodPosition,
        double initialPrimaryTemperatureCelsius,
        bool turbineStartupLineup,
        double initialRotorSpeedRpm = 0d,
        bool initialGeneratorBreakerClosed = false,
        double initialRequestedElectricalPowerMegawatts = 0d,
        double initialCondenserCoolingPowerMegawatts = 0d)
    {
        var recipe = BuildRecipe(
            initialNeutronPopulation,
            mainCirculationRunning,
            initialRodPosition,
            initialPrimaryTemperatureCelsius,
            turbineStartupLineup,
            initialRotorSpeedRpm,
            initialGeneratorBreakerClosed,
            initialRequestedElectricalPowerMegawatts,
            initialCondenserCoolingPowerMegawatts);
        var solver = new IntegratedAutomaticOperationSolver(
            recipe.ReactorDefinition,
            recipe.SecondaryDefinition,
            recipe.ProtectionDefinition,
            recipe.AlarmDefinition,
            recipe.ThermodynamicModel,
            recipe.SignalSources);

        // M5.7 exposes immutable step snapshots rather than a separate arbitrary true-state snapshot constructor. Seed one
        // deterministic fixed step from the recipe and treat that committed candidate as version 1's exact runtime seed.
        var seed = solver.Step(recipe.State, recipe.Inputs, RuntimeStep);
        return new IntegratedAutomaticOperationRuntimeEngine(
            solver,
            seed.CandidateState,
            recipe.Inputs,
            seed.Snapshot,
            RuntimeStep,
            initialLogicalStep: 0);
    }

    private static Recipe BuildRecipe(
        NeutronPopulation initialNeutronPopulation,
        bool mainCirculationRunning,
        ControlRodPosition initialRodPosition,
        double initialPrimaryTemperatureCelsius,
        bool turbineStartupLineup,
        double initialRotorSpeedRpm,
        bool initialGeneratorBreakerClosed,
        double initialRequestedElectricalPowerMegawatts,
        double initialCondenserCoolingPowerMegawatts)
    {
        if (!double.IsFinite(initialCondenserCoolingPowerMegawatts) || initialCondenserCoolingPowerMegawatts < 0d)
        {
            throw new ArgumentOutOfRangeException(
                nameof(initialCondenserCoolingPowerMegawatts),
                initialCondenserCoolingPowerMegawatts,
                "Initial condenser cooling power must be finite and non-negative.");
        }

        if (!double.IsFinite(initialPrimaryTemperatureCelsius) || initialPrimaryTemperatureCelsius < 40d || initialPrimaryTemperatureCelsius > 300d)
        {
            throw new ArgumentOutOfRangeException(
                nameof(initialPrimaryTemperatureCelsius),
                initialPrimaryTemperatureCelsius,
                "Operational seed primary temperature must be finite and between 40 and 300 °C.");
        }
        if (!double.IsFinite(initialRotorSpeedRpm) || initialRotorSpeedRpm < 0d || initialRotorSpeedRpm > 3_300d)
        {
            throw new ArgumentOutOfRangeException(
                nameof(initialRotorSpeedRpm),
                initialRotorSpeedRpm,
                "Operational seed rotor speed must be finite and between 0 and 3300 rpm.");
        }
        if (!double.IsFinite(initialRequestedElectricalPowerMegawatts)
            || initialRequestedElectricalPowerMegawatts < 0d
            || initialRequestedElectricalPowerMegawatts > 1_000d)
        {
            throw new ArgumentOutOfRangeException(
                nameof(initialRequestedElectricalPowerMegawatts),
                initialRequestedElectricalPowerMegawatts,
                "Operational seed requested electrical power must be finite and between 0 and 1000 MWe.");
        }
        if (initialRequestedElectricalPowerMegawatts > 0d && !initialGeneratorBreakerClosed)
        {
            throw new ArgumentException(
                "A non-zero operational-seed electrical load requires the generator breaker to be initially closed.",
                nameof(initialRequestedElectricalPowerMegawatts));
        }

        var thermodynamicModel = new SimplifiedWaterSteamThermodynamicModel();
        FluidNodeDefinition Node(string id) => new(id, Volume.FromCubicMetres(10d));
        PipeDefinition Pipe(string id, string from, string to, double resistance = 100_000d) => new(
            id, from, to, QuadraticHydraulicResistance.FromPascalSecondsSquaredPerKilogramSquared(resistance));
        ValveDefinition Valve(string id, string from, string to) => new(
            id, Pipe($"{id}-path", from, to), ValveCharacteristic.Linear, ValveFailSafeAction.FailClosed);
        PumpDefinition Pump(string id, string from, string to, double boostMegapascals) => new(
            id,
            Pipe($"{id}-path", from, to, 100_000_000d),
            PressureDifference.FromMegapascals(boostMegapascals),
            QuadraticHydraulicResistance.FromPascalSecondsSquaredPerKilogramSquared(100_000_000d),
            PumpEfficiency.FromPercent(80d));

        var plant = new PlantDefinition(
            "educational-reference-plant",
            new[]
            {
                Node("suction"), Node("pressure"), Node("outlet"), Node("drum"), Node("steam"),
                Node("header"), Node("stop-out"), Node("control-out"), Node("turbine-inlet"), Node("exhaust"),
                Node("hotwell"), Node("feedwater-inventory"),
            },
            new[]
            {
                Pipe("channel", "pressure", "outlet"),
                Pipe("return", "outlet", "drum"),
                Pipe("main-steam-line", "steam", "header"),
            },
            new[]
            {
                Valve("stop", "header", "stop-out"),
                Valve("control", "stop-out", "control-out"),
                Valve("admission", "control-out", "turbine-inlet"),
            },
            new[]
            {
                Pump("pump", "suction", "pressure", 1d),
                Pump("condensate-pump", "hotwell", "feedwater-inventory", 1d),
                Pump("feedwater-pump", "feedwater-inventory", "drum", 7d),
            },
            new[]
            {
                new ThermalBodyDefinition("fuel", HeatCapacity.FromJoulesPerKelvin(10_000_000d)),
                new ThermalBodyDefinition("structure", HeatCapacity.FromJoulesPerKelvin(20_000_000d)),
            },
            Array.Empty<HeatTransferDefinition>(),
            Array.Empty<HeatSourceDefinition>());

        var primaryLiquid = CreateSubcooledLiquid(plant, "suction", thermodynamicModel, initialPrimaryTemperatureCelsius);
        var primaryLiquidPressure = primaryLiquid.Pressure;
        FluidNodeState PrimaryLiquid(string id) => CreateSubcooledLiquid(plant, id, thermodynamicModel, initialPrimaryTemperatureCelsius);
        FluidNodeState SteamSpace(string id) => CreateSaturatedSteamSpace(plant, id, thermodynamicModel, initialPrimaryTemperatureCelsius);
        FluidNodeState CondenserSteam(string id) => CreateSaturatedSteamSpace(plant, id, thermodynamicModel, 40d);
        FluidNodeState Condensate(string id) => CreateSubcooledLiquid(plant, id, thermodynamicModel, 40d);

        var hotwell = Condensate("hotwell");
        var plantState = new PlantState(
            plant,
            new[]
            {
                primaryLiquid,
                PrimaryLiquid("pressure"),
                PrimaryLiquid("outlet"),
                PrimaryLiquid("drum"),
                SteamSpace("steam"),
                SteamSpace("header"),
                SteamSpace("stop-out"),
                turbineStartupLineup ? CondenserSteam("control-out") : SteamSpace("control-out"),
                turbineStartupLineup ? CondenserSteam("turbine-inlet") : SteamSpace("turbine-inlet"),
                CondenserSteam("exhaust"),
                hotwell,
                Condensate("feedwater-inventory"),
            },
            new[]
            {
                new ValveState("stop", turbineStartupLineup ? ValvePosition.FullyOpen : ValvePosition.Closed),
                new ValveState("control", ValvePosition.Closed),
                new ValveState("admission", turbineStartupLineup ? ValvePosition.FullyOpen : ValvePosition.Closed),
            },
            new[]
            {
                new PumpState(
                    "pump",
                    mainCirculationRunning ? PumpSpeed.Rated : PumpSpeed.Stopped,
                    isRunning: mainCirculationRunning),
                new PumpState("condensate-pump", PumpSpeed.Stopped, isRunning: false),
                new PumpState("feedwater-pump", PumpSpeed.Stopped, isRunning: false),
            },
            new[]
            {
                ThermalBodyState.FromTemperature(plant.GetThermalBody("fuel"), Temperature.FromDegreesCelsius(initialPrimaryTemperatureCelsius)),
                ThermalBodyState.FromTemperature(plant.GetThermalBody("structure"), Temperature.FromDegreesCelsius(initialPrimaryTemperatureCelsius)),
            },
            Array.Empty<HeatSourceState>());

        var core = AggregatedCoreDefinition.CreateSingleZone("core", plant, "zone", "fuel", "structure", "outlet");
        var groups = new FuelChannelGroupSetDefinition(
            "groups",
            core,
            new[]
            {
                new FuelChannelGroupDefinition(
                    "group", "zone", 100, CoreZonePowerFraction.Full, "channel", "pressure", "outlet", "fuel", "structure",
                    HeatDepositionFraction.FromPercent(70d), HeatDepositionFraction.FromPercent(10d), HeatDepositionFraction.FromPercent(20d)),
            });
        var circulation = new MainCirculationSystemDefinition(
            "circulation",
            groups,
            new[]
            {
                new MainCirculationLoopDefinition(
                    "loop", "suction", "pressure", "drum", new[] { "pump" },
                    new[] { new MainCirculationBranchDefinition("group", "return") }),
            });
        var drums = new SteamDrumSystemDefinition(
            "drums", circulation, new[] { new SteamDrumDefinition("drum-a", "loop", "drum", "steam") });
        var boundaries = new PrimaryCircuitBoundarySystemDefinition(
            "boundaries",
            drums,
            new[] { new FeedwaterBoundaryDefinition("feed", "drum-a", "drum") },
            new[] { new SteamExportBoundaryDefinition("export", "drum-a", "steam") });
        var primary = new IntegratedPrimaryCircuitDefinition("primary", boundaries);
        var mainSteam = new MainSteamNetworkDefinition(
            "main-steam",
            primary,
            new[] { new MainSteamLineDefinition("line-a", "export", "main-steam-line", "header") },
            new[] { new TurbineAdmissionTrainDefinition("train-a", "header", "stop", "control", "admission", "turbine-inlet") },
            new[] { new TurbineAdmissionBoundaryDefinition("turbine-boundary", "train-a", "turbine-inlet") });
        var turbine = new TurbineExpansionSystemDefinition(
            "turbine",
            mainSteam,
            new[]
            {
                new TurbineRotorDefinition(
                    "rotor",
                    MomentOfInertia.FromKilogramSquareMetres(1_000d),
                    AngularSpeed.FromRevolutionsPerMinute(3_000d),
                    AngularSpeed.FromRevolutionsPerMinute(3_300d)),
            },
            new[]
            {
                new TurbineStageGroupDefinition(
                    "stage", "turbine-boundary", "exhaust", "rotor",
                    SpecificEnergy.FromKilojoulesPerKilogram(500d), TurbineEfficiency.FromPercent(80d)),
            });
        var condensers = new CondenserSystemDefinition(
            "condensers",
            turbine,
            new[]
            {
                new CondenserDefinition("condenser", "stage", "exhaust", "hotwell", "cooling", MassFlowRate.FromKilogramsPerSecond(10d)),
            },
            new[] { new CondenserCoolingBoundaryDefinition("cooling", "condenser") });
        var feedwater = new CondensateFeedwaterSystemDefinition(
            "feedwater-system",
            condensers,
            new[]
            {
                new CondensateFeedwaterTrainDefinition(
                    "feedwater-train", "condenser", "feed", "condensate-pump", "feedwater-inventory", "feedwater-pump",
                    Power.FromMegawatts(2d)),
            });
        var grid = new ElectricalGridDefinition("grid", Frequency.FromHertz(50d), ElectricPotential.FromKilovolts(400d));
        var generator = new SynchronousGeneratorDefinition(
            "generator",
            "rotor",
            "breaker",
            polePairs: 1,
            ElectricPotential.FromKilovolts(400d),
            Power.FromMegawatts(1_000d),
            GeneratorEfficiency.FromPercent(98d),
            Frequency.FromHertz(0.2d),
            PhaseAngleDifference.FromDegrees(10d),
            ElectricPotential.FromKilovolts(10d));
        var generatorGrid = new GeneratorGridSystemDefinition("electrical", feedwater, grid, new[] { generator });
        var fullPlantDefinition = new IntegratedSecondaryCycleDefinition("secondary-cycle", generatorGrid);
        var fullPlantState = new FullPlantState(
            fullPlantDefinition,
            plantState,
            new TurbineExpansionState(
                turbine,
                new[] { new TurbineRotorState("rotor", AngularSpeed.FromRevolutionsPerMinute(initialRotorSpeedRpm)) }),
            new GeneratorGridState(generatorGrid, PhaseAngle.Zero, new[]
            {
                new SynchronousGeneratorState("generator", PhaseAngle.Zero, breakerClosed: initialGeneratorBreakerClosed),
            }));

        var instrumentation = new InstrumentationSystemDefinition("instrumentation", new[]
        {
            Channel("power", "plant/reactor/thermal-power", "W"),
            Channel("flow", "main-circulation-loop/loop/total-pump-flow", "kg/s"),
            Channel("speed", "turbine-rotor/rotor/speed", "rpm"),
            Channel("pressure", "steam-drum/drum-a/pressure", "Pa"),
            Channel("level", "steam-drum/drum-a/level", "fraction"),
            Channel("hotwell", "condenser/condenser/hotwell-mass", "kg"),
            Channel("generator-output", "generator/generator/electrical-output", "W"),
            Channel("gross-generator-output", "plant/generator/gross-electrical-output", "W"),
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
            new[]
            {
                new DelayedNeutronGroupDefinition(
                    "dn", DelayedNeutronFraction.FromFraction(0.0065d), DecayConstant.FromPerSecond(0.08d)),
            });
        var fissionPowerDefinition = new FissionPowerDefinition(
            "fission-power",
            new FissionPowerCalibration(NeutronPopulation.Reference, Power.FromMegawatts(100d)),
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
                new ReactorPrimaryControlLoopDefinition(
                    "power-loop", ReactorPrimaryControlLoopKind.ReactorPowerRodRegulation, "power-control", "rod-actuator"),
                new ReactorPrimaryControlLoopDefinition(
                    "flow-loop", ReactorPrimaryControlLoopKind.MainCirculationPumpFlow, "flow-control", "mcp-actuator"),
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
            ActuatorDefinition.Valve("speed-actuator", "speed-control", "control", new ControllerOutputRange(0d, 100d)),
            ActuatorDefinition.Valve("pressure-actuator", "pressure-control", "admission", new ControllerOutputRange(0d, 100d)),
            ActuatorDefinition.Pump("feedwater-actuator", "level-control", "feedwater-pump", new ControllerOutputRange(0d, 100d)),
            ActuatorDefinition.Pump("condensate-actuator", "hotwell-control", "condensate-pump", new ControllerOutputRange(0d, 100d)),
        });
        var secondaryDefinition = new TurbineSecondaryControlSystemDefinition(
            "secondary-controls",
            fullPlantDefinition,
            secondaryActuators,
            new[]
            {
                new TurbineSecondaryControlLoopDefinition(
                    "speed-loop", TurbineSecondaryControlLoopKind.TurbineSpeedAdmission, "speed-control", "speed-actuator"),
                new TurbineSecondaryControlLoopDefinition(
                    "pressure-loop", TurbineSecondaryControlLoopKind.SteamPressureAdmission, "pressure-control", "pressure-actuator"),
                new TurbineSecondaryControlLoopDefinition(
                    "level-loop", TurbineSecondaryControlLoopKind.SteamDrumLevelFeedwater, "level-control", "feedwater-actuator"),
                new TurbineSecondaryControlLoopDefinition(
                    "hotwell-loop", TurbineSecondaryControlLoopKind.HotwellInventoryCondensate, "hotwell-control", "condensate-actuator"),
            });

        var protectionDefinition = new ProtectionSystemDefinition(
            "protection",
            fullPlantDefinition,
            instrumentation,
            new[]
            {
                new ProtectionFunctionDefinition(
                    "very-high-pressure", "pressure", ProtectionComparison.High, 25_000_000d, 24_000_000d,
                    ProtectionAction.ReactorScram),
            });
        var alarmDefinition = new AlarmSystemDefinition(
            "alarms",
            instrumentation,
            protectionDefinition,
            new[]
            {
                new AlarmDefinition(
                    "pressure-high", "Steam-drum pressure high", AlarmSeverity.Warning, AlarmLatchingMode.NonLatching,
                    new MeasuredAlarmConditionDefinition("pressure", AlarmComparison.High, 20_000_000d)),
                new AlarmDefinition(
                    "scram-active", "SCRAM active", AlarmSeverity.Trip, AlarmLatchingMode.LatchedUntilReset,
                    new ProtectionActionAlarmConditionDefinition(ProtectionAction.ReactorScram)),
            });

        var primaryBoundaryInputs = new PrimaryCircuitBoundaryInputs(
            boundaries,
            new[] { new FeedwaterBoundaryInput("feed", MassFlowRate.Zero, SpecificEnergy.Zero) },
            new[] { new SteamExportBoundaryInput("export", MassFlowRate.Zero) });
        var primaryInputs = new IntegratedPrimaryCircuitInputs(
            primary, AggregatedCoreState.CreateNominal(core), Power.Zero, Power.Zero, primaryBoundaryInputs);
        var mainSteamInputs = new MainSteamNetworkInputs(
            mainSteam, primaryInputs, new[] { new TurbineAdmissionBoundaryInput("turbine-boundary", MassFlowRate.Zero) });
        var turbineInputs = new TurbineExpansionInputs(
            turbine,
            mainSteamInputs,
            new[] { new TurbineStageGroupInput("stage", MassFlowRate.Zero) },
            new[] { new TurbineRotorInput("rotor", Torque.Zero) });
        var condenserInputs = new CondenserSystemInputs(
            condensers,
            turbineInputs,
            new[]
            {
                new CondenserCoolingBoundaryInput(
                    "cooling",
                    Power.FromMegawatts(initialCondenserCoolingPowerMegawatts)),
            });
        var feedwaterInputs = new CondensateFeedwaterSystemInputs(
            feedwater,
            condenserInputs,
            new[] { new CondensateFeedwaterTrainInput("feedwater-train", Power.Zero) });
        var generatorInputs = new GeneratorGridInputs(
            generatorGrid,
            feedwaterInputs,
            new[]
            {
                new SynchronousGeneratorInput(
                    "generator",
                    ElectricPotential.FromKilovolts(400d),
                    Power.FromMegawatts(initialRequestedElectricalPowerMegawatts),
                    closeBreakerCommand: false,
                    openBreakerCommand: false),
            });
        var plantInputs = new IntegratedSecondaryCycleInputs(fullPlantDefinition, generatorInputs);

        var reactorState = ReactorPrimaryControlState.CreateInitial(
            reactorDefinition,
            new ControlRodSystemState(new[] { new ControlRodState("rod-1", initialRodPosition) }),
            PointKineticsState.CreateCriticalEquilibrium(kineticsParameters, initialNeutronPopulation));
        var secondaryState = TurbineSecondaryControlState.CreateInitial(secondaryDefinition);
        var reactorInputs = new ReactorPrimaryControlInputs(
            reactorDefinition,
            new ControllerInputs(reactorControl, new[]
            {
                new ControllerInput("power-control", ControllerMode.Manual, 0d, 0d),
                new ControllerInput(
                    "flow-control",
                    ControllerMode.Manual,
                    0d,
                    mainCirculationRunning ? 100d : 0d),
            }),
            Reactivity.Zero);
        var secondaryInputs = new TurbineSecondaryControlInputs(
            secondaryDefinition,
            new ControllerInputs(secondaryControl, new[]
            {
                new ControllerInput(
                    "speed-control",
                    turbineStartupLineup ? ControllerMode.Automatic : ControllerMode.Manual,
                    initialRotorSpeedRpm,
                    0d),
                new ControllerInput(
                    "pressure-control",
                    ControllerMode.Manual,
                    0d,
                    turbineStartupLineup ? 100d : 0d),
                new ControllerInput(
                    "level-control",
                    turbineStartupLineup ? ControllerMode.Automatic : ControllerMode.Manual,
                    1d,
                    0d),
                new ControllerInput(
                    "hotwell-control",
                    turbineStartupLineup ? ControllerMode.Automatic : ControllerMode.Manual,
                    hotwell.Mass.Kilograms,
                    0d),
            }));

        var measuredSignals = new MeasuredSignalFrame(instrumentation, new[]
        {
            Signal("power", "W", 0d),
            Signal("flow", "kg/s", 0d),
            Signal("speed", "rpm", initialRotorSpeedRpm),
            Signal("pressure", "Pa", primaryLiquidPressure.Pascals),
            Signal("level", "fraction", 1d),
            Signal("hotwell", "kg", hotwell.Mass.Kilograms),
            Signal("generator-output", "W", 0d),
            Signal("gross-generator-output", "W", 0d),
        });
        var state = new IntegratedAutomaticOperationState(
            fullPlantDefinition,
            instrumentation,
            fullPlantState,
            InstrumentationState.CreateUninitialized(instrumentation),
            measuredSignals,
            reactorState,
            secondaryState,
            ProtectionSystemState.CreateInitial(protectionDefinition),
            AlarmSystemState.CreateInitial(alarmDefinition));
        var inputs = new IntegratedAutomaticOperationInputs(
            plantInputs,
            reactorInputs,
            secondaryInputs,
            new ProtectionSystemInputs(protectionDefinition),
            new AlarmSystemInputs(alarmDefinition),
            InstrumentationInputs.Healthy(instrumentation));
        var signalSources = InstrumentSignalSourceCatalog.CreateFullPlantCatalog(fullPlantDefinition);

        return new Recipe(
            thermodynamicModel,
            reactorDefinition,
            secondaryDefinition,
            protectionDefinition,
            alarmDefinition,
            state,
            inputs,
            signalSources);
    }

    private static FluidNodeState CreateSubcooledLiquid(
        PlantDefinition plant,
        string nodeId,
        SimplifiedWaterSteamThermodynamicModel thermodynamicModel,
        double temperatureCelsius)
    {
        var definition = plant.GetFluidNode(nodeId);
        var temperature = Temperature.FromDegreesCelsius(temperatureCelsius);
        var saturation = thermodynamicModel.GetSaturationProperties(temperature);
        var density = saturation.SaturatedLiquidDensity.KilogramsPerCubicMetre * 1.000001d;
        var mass = Mass.FromKilograms(density * definition.Volume.CubicMetres);
        var inventory = new FluidNodeInventory(mass, saturation.SaturatedLiquidInternalEnergy * mass);
        var previous = new FluidThermodynamicState(
            Pressure.FromPascals(saturation.Pressure.Pascals + 2_200d),
            temperature,
            FluidPhase.SubcooledLiquid,
            null);
        var resolved = thermodynamicModel.Resolve(definition, inventory, previous);
        return new FluidNodeState(definition, inventory, resolved);
    }

    private static FluidNodeState CreateSaturatedSteamSpace(
        PlantDefinition plant,
        string nodeId,
        SimplifiedWaterSteamThermodynamicModel thermodynamicModel,
        double temperatureCelsius)
    {
        try
        {
            return CreateSaturatedSteamSpaceAtQuality(
                plant,
                nodeId,
                thermodynamicModel,
                temperatureCelsius,
                vaporQualityFraction: 0.99d);
        }
        catch (WaterSteamStateOutOfRangeException)
        {
            // Near the dry-saturated boundary, the simplified closure can lose the root bracket between
            // scan segments. Preserve the established 0.99 recipe where it resolves, but deterministically
            // move the initialization slightly inside the two-phase envelope when required.
            return CreateSaturatedSteamSpaceAtQuality(
                plant,
                nodeId,
                thermodynamicModel,
                temperatureCelsius,
                vaporQualityFraction: 0.98d);
        }
    }

    private static FluidNodeState CreateSaturatedSteamSpaceAtQuality(
        PlantDefinition plant,
        string nodeId,
        SimplifiedWaterSteamThermodynamicModel thermodynamicModel,
        double temperatureCelsius,
        double vaporQualityFraction)
    {
        var definition = plant.GetFluidNode(nodeId);
        var temperature = Temperature.FromDegreesCelsius(temperatureCelsius);
        var saturation = thermodynamicModel.GetSaturationProperties(temperature);
        var liquidSpecificVolume = saturation.SaturatedLiquidSpecificVolumeCubicMetresPerKilogram;
        var vaporSpecificVolume = saturation.SaturatedVaporSpecificVolumeCubicMetresPerKilogram;
        var mixtureSpecificVolume = liquidSpecificVolume
            + (vaporQualityFraction * (vaporSpecificVolume - liquidSpecificVolume));
        var mixtureSpecificEnergy = SpecificEnergy.FromJoulesPerKilogram(
            saturation.SaturatedLiquidInternalEnergy.JoulesPerKilogram
            + (vaporQualityFraction
                * (saturation.SaturatedVaporInternalEnergy.JoulesPerKilogram
                    - saturation.SaturatedLiquidInternalEnergy.JoulesPerKilogram)));
        var mass = Mass.FromKilograms(definition.Volume.CubicMetres / mixtureSpecificVolume);
        var inventory = new FluidNodeInventory(mass, mixtureSpecificEnergy * mass);
        var previous = new FluidThermodynamicState(
            saturation.Pressure,
            temperature,
            FluidPhase.SaturatedMixture,
            VaporQuality.FromFraction(vaporQualityFraction));
        var resolved = thermodynamicModel.Resolve(definition, inventory, previous);
        return new FluidNodeState(definition, inventory, resolved);
    }

    private static InstrumentChannelDefinition Channel(string id, string sourceId, string unit)
        => new(id, sourceId, unit, new SignalRange(-1e12d, 1e12d), LinearSignalScale.NormalizedZeroToOne, TimeSpan.Zero, false);

    private static PidControllerDefinition Controller(string id, string channelId, ControllerOutputRange range, double kp)
        => new(id, channelId, ControllerAlgorithmKind.Proportional, kp, 0d, 0d, range);

    private static MeasuredSignal Signal(string id, string unit, double value)
        => new(id, unit, value, value, SignalValidity.Valid, SignalQuality.Good, false, SensorFaultMode.None);

    private sealed record Recipe(
        IFluidThermodynamicModel ThermodynamicModel,
        ReactorPrimaryControlSystemDefinition ReactorDefinition,
        TurbineSecondaryControlSystemDefinition SecondaryDefinition,
        ProtectionSystemDefinition ProtectionDefinition,
        AlarmSystemDefinition AlarmDefinition,
        IntegratedAutomaticOperationState State,
        IntegratedAutomaticOperationInputs Inputs,
        InstrumentSignalSourceCatalog SignalSources);
}
