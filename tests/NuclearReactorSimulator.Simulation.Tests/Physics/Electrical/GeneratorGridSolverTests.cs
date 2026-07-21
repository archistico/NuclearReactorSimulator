using NuclearReactorSimulator.Domain.Physics.Electrical;
using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor.Core;
using NuclearReactorSimulator.Domain.Physics.Reactor.Core.Channels;
using NuclearReactorSimulator.Domain.Physics.Reactor.PrimaryCircuit.Boundaries;
using NuclearReactorSimulator.Domain.Physics.Reactor.PrimaryCircuit.Circulation;
using NuclearReactorSimulator.Domain.Physics.Reactor.PrimaryCircuit.Integration;
using NuclearReactorSimulator.Domain.Physics.Reactor.PrimaryCircuit.SteamDrums;
using NuclearReactorSimulator.Domain.Physics.Reactor.ThermalPower;
using NuclearReactorSimulator.Domain.Physics.Thermal;
using NuclearReactorSimulator.Domain.Physics.TurbineIsland.Condenser;
using NuclearReactorSimulator.Domain.Physics.TurbineIsland.Feedwater;
using NuclearReactorSimulator.Domain.Physics.TurbineIsland.MainSteam;
using NuclearReactorSimulator.Domain.Physics.TurbineIsland.Turbine;
using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Physics.Electrical;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using NuclearReactorSimulator.Simulation.Physics.Reactor.Core;
using NuclearReactorSimulator.Simulation.Physics.Reactor.PrimaryCircuit.Boundaries;
using NuclearReactorSimulator.Simulation.Physics.Reactor.PrimaryCircuit.Integration;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Condenser;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Feedwater;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.MainSteam;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Turbine;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Physics.Electrical;

public sealed class GeneratorGridSolverTests
{
    [Fact]
    public void Step_SynchronizedCloseConnectsBreakerExportsPowerAndFeedsTorqueBackToRotor()
    {
        var fixture = CreateFixture(3_000d, breakerClosed: false, generatorPhaseDegrees: 0d, gridPhaseDegrees: 0d, closeCommand: true);
        var solver = new GeneratorGridSolver(fixture.Definition, fixture.ThermodynamicModel);

        var result = solver.Step(fixture.PlantState, fixture.TurbineState, fixture.ElectricalState, fixture.Inputs, TimeSpan.FromMilliseconds(1d));
        var generator = Assert.Single(result.Snapshot.Generators);
        var rotor = Assert.Single(result.Snapshot.TurbineExpansion.Rotors);

        Assert.True(generator.SynchronizationConditionsSatisfied);
        Assert.Equal(50d, generator.InitialElectricalFrequency.Hertz, 12);
        Assert.Equal(PhaseAngleDifference.Zero, generator.InitialPhaseDifference);
        Assert.True(generator.CloseCommandAccepted);
        Assert.True(generator.BreakerFinallyClosed);
        Assert.True(generator.CommandedElectromagneticTorque > Torque.Zero);
        Assert.Equal(generator.EffectiveElectromagneticTorque, rotor.EffectiveExternalLoadTorque);
        Assert.True(generator.ElectricalOutputPower > Power.Zero);
        Assert.InRange(Math.Abs(result.Snapshot.ElectricalAudit.PowerClosureResidualWatts), 0d, 1e-6d);
        Assert.True(result.CandidateElectricalState.GetGenerator("generator").BreakerClosed);
    }

    [Fact]
    public void Step_UnsynchronizedCloseIsRejectedAndAppliesNoElectricalLoadTorque()
    {
        var fixture = CreateFixture(2_800d, breakerClosed: false, generatorPhaseDegrees: 40d, gridPhaseDegrees: 0d, closeCommand: true);
        var solver = new GeneratorGridSolver(fixture.Definition, fixture.ThermodynamicModel);

        var result = solver.Step(fixture.PlantState, fixture.TurbineState, fixture.ElectricalState, fixture.Inputs, TimeSpan.FromMilliseconds(1d));
        var generator = Assert.Single(result.Snapshot.Generators);

        Assert.False(generator.SynchronizationConditionsSatisfied);
        Assert.True(generator.CloseCommandRejected);
        Assert.False(generator.BreakerFinallyClosed);
        Assert.Equal(Torque.Zero, generator.CommandedElectromagneticTorque);
        Assert.Equal(Power.Zero, generator.ElectricalOutputPower);
    }

    [Fact]
    public void Step_VoltageMismatchAloneRejectsManualBreakerClose()
    {
        var fixture = CreateFixture(
            3_000d,
            breakerClosed: false,
            generatorPhaseDegrees: 0d,
            gridPhaseDegrees: 0d,
            closeCommand: true,
            terminalVoltageKilovolts: 350d);
        var solver = new GeneratorGridSolver(fixture.Definition, fixture.ThermodynamicModel);

        var result = solver.Step(fixture.PlantState, fixture.TurbineState, fixture.ElectricalState, fixture.Inputs, TimeSpan.FromMilliseconds(1d));
        var generator = Assert.Single(result.Snapshot.Generators);

        Assert.Equal(Frequency.Zero, generator.FrequencyDifferenceAtCloseCheck);
        Assert.Equal(PhaseAngleDifference.Zero, generator.InitialPhaseDifference);
        Assert.Equal(50d, generator.VoltageDifferenceAtCloseCheck.Kilovolts, 12);
        Assert.False(generator.SynchronizationConditionsSatisfied);
        Assert.True(generator.CloseCommandRejected);
        Assert.False(generator.BreakerFinallyClosed);
    }

    [Fact]
    public void Step_OpenCommandDisconnectsPreviouslyClosedGeneratorAndRemovesLoadTorque()
    {
        var fixture = CreateFixture(3_000d, breakerClosed: true, generatorPhaseDegrees: 0d, gridPhaseDegrees: 0d, openCommand: true);
        var solver = new GeneratorGridSolver(fixture.Definition, fixture.ThermodynamicModel);

        var result = solver.Step(fixture.PlantState, fixture.TurbineState, fixture.ElectricalState, fixture.Inputs, TimeSpan.FromMilliseconds(1d));
        var generator = Assert.Single(result.Snapshot.Generators);

        Assert.False(generator.BreakerFinallyClosed);
        Assert.Equal(Torque.Zero, generator.CommandedElectromagneticTorque);
        Assert.Equal(Power.Zero, generator.MechanicalInputPower);
        Assert.Equal(Power.Zero, generator.ElectricalOutputPower);
    }

    [Fact]
    public void Definition_RejectsGeneratorThatDoesNotCoverCanonicalTurbineRotor()
    {
        var fixture = CreateFixture(3_000d, breakerClosed: false, generatorPhaseDegrees: 0d, gridPhaseDegrees: 0d);
        var invalidGenerator = new SynchronousGeneratorDefinition(
            "invalid-generator",
            "unknown-rotor",
            "invalid-breaker",
            polePairs: 1,
            ElectricPotential.FromKilovolts(400d),
            Power.FromMegawatts(1_000d),
            GeneratorEfficiency.FromPercent(98d),
            Frequency.FromHertz(0.2d),
            PhaseAngleDifference.FromDegrees(10d),
            ElectricPotential.FromKilovolts(10d));

        Assert.Throws<ArgumentException>(() => new GeneratorGridSystemDefinition(
            "invalid",
            fixture.Definition.CondensateFeedwaterSystem,
            fixture.Definition.Grid,
            new[] { invalidGenerator }));
    }

    [Fact]
    public void Inputs_RejectLegacyM42ManualLoadTorqueWhileM45OwnsGeneratorLoading()
    {
        var fixture = CreateFixture(3_000d, breakerClosed: false, generatorPhaseDegrees: 0d, gridPhaseDegrees: 0d);
        var originalSecondary = fixture.Inputs.CondensateFeedwaterInputs;
        var originalCondenser = originalSecondary.CondenserInputs;
        var originalTurbine = originalCondenser.TurbineExpansionInputs;
        var conflictingTurbine = new TurbineExpansionInputs(
            originalTurbine.Definition,
            originalTurbine.MainSteamInputs,
            originalTurbine.StageGroupInputs,
            new[] { new TurbineRotorInput("rotor", Torque.FromNewtonMetres(1d)) });
        var conflictingCondenser = new CondenserSystemInputs(
            originalCondenser.Definition,
            conflictingTurbine,
            originalCondenser.CoolingBoundaryInputs);
        var conflictingSecondary = new CondensateFeedwaterSystemInputs(
            originalSecondary.Definition,
            conflictingCondenser,
            originalSecondary.TrainInputs);

        Assert.Throws<ArgumentException>(() => new GeneratorGridInputs(
            fixture.Definition,
            conflictingSecondary,
            fixture.Inputs.GeneratorInputs));
    }

    [Fact]
    public void Step_PreservesM44SecondaryCycleCompositionWhileApplyingElectricalLoad()
    {
        var fixture = CreateFixture(3_000d, breakerClosed: true, generatorPhaseDegrees: 0d, gridPhaseDegrees: 0d);
        var solver = new GeneratorGridSolver(fixture.Definition, fixture.ThermodynamicModel);

        var result = solver.Step(fixture.PlantState, fixture.TurbineState, fixture.ElectricalState, fixture.Inputs, TimeSpan.FromSeconds(1d));

        Assert.Single(result.Snapshot.CondensateFeedwater.Trains);
        Assert.True(result.Snapshot.CondensateFeedwater.GetTrain("feedwater-train").CondensatePump.MassFlowRate > MassFlowRate.Zero);
        Assert.Equal(MassFlowRate.Zero, result.Snapshot.CondensateFeedwater.ThermofluidAudit.SupplementalExternalMassFlowRate);
        Assert.InRange(Math.Abs(result.Snapshot.CondensateFeedwater.ThermofluidAudit.BalanceMassRateResidualKilogramsPerSecond), 0d, 1e-10d);
    }

    [Fact]
    public void Step_IsDeterministicForIdenticalCommittedMechanicalAndElectricalState()
    {
        var fixture = CreateFixture(3_000d, breakerClosed: true, generatorPhaseDegrees: 2d, gridPhaseDegrees: 1d);
        var solver = new GeneratorGridSolver(fixture.Definition, fixture.ThermodynamicModel);

        var left = solver.Step(fixture.PlantState, fixture.TurbineState, fixture.ElectricalState, fixture.Inputs, TimeSpan.FromMilliseconds(10d));
        var right = solver.Step(fixture.PlantState, fixture.TurbineState, fixture.ElectricalState, fixture.Inputs, TimeSpan.FromMilliseconds(10d));

        Assert.Equal(left.CandidateElectricalState.GridPhaseAngle, right.CandidateElectricalState.GridPhaseAngle);
        Assert.Equal(left.CandidateElectricalState.GetGenerator("generator"), right.CandidateElectricalState.GetGenerator("generator"));
        Assert.Equal(left.CandidateTurbineState.GetRotor("rotor"), right.CandidateTurbineState.GetRotor("rotor"));
        Assert.Equal(left.Snapshot.ElectricalAudit, right.Snapshot.ElectricalAudit);
    }

    internal static Fixture CreateFixture(
        double rotorSpeedRpm,
        bool breakerClosed,
        double generatorPhaseDegrees,
        double gridPhaseDegrees,
        bool closeCommand = false,
        bool openCommand = false,
        double terminalVoltageKilovolts = 400d)
    {
        var thermodynamicModel = new PreservingThermodynamicModel();
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
            "plant",
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

        FluidNodeState Fluid(
            string id,
            double pressureMegapascals,
            FluidPhase phase,
            double specificEnergyKilojoulesPerKilogram,
            VaporQuality? quality = null)
        {
            var mass = Mass.FromKilograms(10_000d);
            var specificEnergy = SpecificEnergy.FromKilojoulesPerKilogram(specificEnergyKilojoulesPerKilogram);
            return new FluidNodeState(
                plant.GetFluidNode(id),
                new FluidNodeInventory(mass, specificEnergy * mass),
                new FluidThermodynamicState(
                    Pressure.FromMegapascals(pressureMegapascals),
                    Temperature.FromDegreesCelsius(200d),
                    phase,
                    quality));
        }

        var plantState = new PlantState(
            plant,
            new[]
            {
                Fluid("suction", 6d, FluidPhase.SubcooledLiquid, 2_000d),
                Fluid("pressure", 6d, FluidPhase.SubcooledLiquid, 2_000d),
                Fluid("outlet", 6d, FluidPhase.SubcooledLiquid, 2_000d),
                Fluid("drum", 6d, FluidPhase.SubcooledLiquid, 2_000d),
                Fluid("steam", 7d, FluidPhase.SuperheatedVapor, 2_000d),
                Fluid("header", 6.5d, FluidPhase.SuperheatedVapor, 2_000d),
                Fluid("stop-out", 6d, FluidPhase.SuperheatedVapor, 2_000d),
                Fluid("control-out", 5.5d, FluidPhase.SuperheatedVapor, 2_000d),
                Fluid("turbine-inlet", 5d, FluidPhase.SuperheatedVapor, 2_000d),
                Fluid("exhaust", 0.1d, FluidPhase.SaturatedMixture, 2_000d, VaporQuality.FromPercent(90d)),
                Fluid("hotwell", 0.1d, FluidPhase.SubcooledLiquid, 500d),
                Fluid("feedwater-inventory", 0.2d, FluidPhase.SubcooledLiquid, 600d),
            },
            new[]
            {
                new ValveState("stop", ValvePosition.FullyOpen),
                new ValveState("control", ValvePosition.FullyOpen),
                new ValveState("admission", ValvePosition.FullyOpen),
            },
            new[]
            {
                new PumpState("pump", PumpSpeed.Stopped, isRunning: false),
                new PumpState("condensate-pump", PumpSpeed.Rated),
                new PumpState("feedwater-pump", PumpSpeed.Rated),
            },
            new[]
            {
                ThermalBodyState.FromTemperature(plant.GetThermalBody("fuel"), Temperature.FromDegreesCelsius(500d)),
                ThermalBodyState.FromTemperature(plant.GetThermalBody("structure"), Temperature.FromDegreesCelsius(350d)),
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
        var drums = new SteamDrumSystemDefinition("drums", circulation, new[] { new SteamDrumDefinition("drum-a", "loop", "drum", "steam") });
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
        var definition = new GeneratorGridSystemDefinition("electrical", feedwater, grid, new[] { generator });

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
            new[] { new TurbineStageGroupInput("stage", MassFlowRate.FromKilogramsPerSecond(1d)) },
            new[] { new TurbineRotorInput("rotor", Torque.Zero) });
        var condenserInputs = new CondenserSystemInputs(
            condensers,
            turbineInputs,
            new[] { new CondenserCoolingBoundaryInput("cooling", Power.Zero) });
        var feedwaterInputs = new CondensateFeedwaterSystemInputs(
            feedwater,
            condenserInputs,
            new[] { new CondensateFeedwaterTrainInput("feedwater-train", Power.Zero) });
        var inputs = new GeneratorGridInputs(
            definition,
            feedwaterInputs,
            new[]
            {
                new SynchronousGeneratorInput(
                    "generator",
                    ElectricPotential.FromKilovolts(terminalVoltageKilovolts),
                    Power.FromKilowatts(300d),
                    closeCommand,
                    openCommand),
            });
        var turbineState = new TurbineExpansionState(
            turbine,
            new[] { new TurbineRotorState("rotor", AngularSpeed.FromRevolutionsPerMinute(rotorSpeedRpm)) });
        var electricalState = new GeneratorGridState(
            definition,
            PhaseAngle.FromDegrees(gridPhaseDegrees),
            new[] { new SynchronousGeneratorState("generator", PhaseAngle.FromDegrees(generatorPhaseDegrees), breakerClosed) });

        return new Fixture(definition, plantState, turbineState, electricalState, inputs, thermodynamicModel);
    }

    internal sealed record Fixture(
        GeneratorGridSystemDefinition Definition,
        PlantState PlantState,
        TurbineExpansionState TurbineState,
        GeneratorGridState ElectricalState,
        GeneratorGridInputs Inputs,
        IFluidThermodynamicModel ThermodynamicModel);

    private sealed class PreservingThermodynamicModel : IFluidThermodynamicModel
    {
        public FluidThermodynamicState Resolve(
            FluidNodeDefinition definition,
            FluidNodeInventory inventory,
            FluidThermodynamicState previousState)
            => previousState;
    }
}
