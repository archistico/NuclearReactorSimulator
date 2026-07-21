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
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using NuclearReactorSimulator.Simulation.Physics.Reactor.Core;
using NuclearReactorSimulator.Simulation.Physics.Reactor.PrimaryCircuit.Boundaries;
using NuclearReactorSimulator.Simulation.Physics.Reactor.PrimaryCircuit.Integration;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Condenser;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Feedwater;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.MainSteam;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Turbine;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Physics.TurbineIsland.Feedwater;

public sealed class CondensateFeedwaterSystemSolverTests
{
    [Fact]
    public void Step_ClosesHotwellToDrumPathWithoutExternalFeedwaterMassSource()
    {
        var fixture = CreateFixture(MassFlowRate.Zero, Power.Zero);
        var solver = new CondensateFeedwaterSystemSolver(fixture.Definition, fixture.ThermodynamicModel);

        var result = solver.Step(fixture.PlantState, fixture.TurbineState, fixture.Inputs, TimeSpan.FromSeconds(1d));
        var train = Assert.Single(result.Snapshot.Trains);

        Assert.True(train.CondensatePump.MassFlowRate > MassFlowRate.Zero);
        Assert.True(train.FeedwaterPump.MassFlowRate > MassFlowRate.Zero);
        Assert.Equal(MassFlowRate.Zero, result.Snapshot.ThermofluidAudit.SupplementalExternalMassFlowRate);
        Assert.InRange(Math.Abs(result.Snapshot.ThermofluidAudit.BalanceMassRateResidualKilogramsPerSecond), 0d, 1e-10d);
        Assert.InRange(Math.Abs(result.Snapshot.ThermofluidAudit.MassClosureResidualKilograms), 0d, 1e-8d);
    }

    [Fact]
    public void Step_ThermalConditioningIsExplicitExternalPowerAndUsesSingleIntegration()
    {
        var fixture = CreateFixture(MassFlowRate.Zero, Power.FromMegawatts(1d));
        var solver = new CondensateFeedwaterSystemSolver(fixture.Definition, fixture.ThermodynamicModel);

        var result = solver.Step(fixture.PlantState, fixture.TurbineState, fixture.Inputs, TimeSpan.FromSeconds(1d));
        var train = Assert.Single(result.Snapshot.Trains);

        Assert.Equal(1d, train.ThermalConditioningPower.Megawatts, 12);
        Assert.Equal(1d, result.Snapshot.TotalThermalConditioningPower.Megawatts, 12);
        Assert.Equal(1d, result.Snapshot.ThermofluidAudit.SupplementalExternalPower.Megawatts, 9);
        Assert.True(result.CandidatePlantState.GetFluidNode("feedwater-inventory").InternalEnergy
            > fixture.PlantState.GetFluidNode("feedwater-inventory").InternalEnergy);
    }

    [Fact]
    public void Inputs_RejectNonZeroLegacyM3FeedwaterBoundaryWhileM44OwnsReturnPath()
    {
        Assert.Throws<ArgumentException>(() => CreateFixture(MassFlowRate.FromKilogramsPerSecond(1d), Power.Zero));
    }

    [Fact]
    public void Inputs_RequireExactTrainCoverage()
    {
        var fixture = CreateFixture(MassFlowRate.Zero, Power.Zero);

        Assert.Throws<ArgumentException>(() => new CondensateFeedwaterSystemInputs(
            fixture.Definition,
            fixture.Inputs.CondenserInputs,
            Array.Empty<CondensateFeedwaterTrainInput>()));
    }

    [Fact]
    public void Inputs_RejectThermalConditioningAboveConfiguredMaximum()
    {
        var fixture = CreateFixture(MassFlowRate.Zero, Power.Zero);

        Assert.Throws<ArgumentOutOfRangeException>(() => new CondensateFeedwaterSystemInputs(
            fixture.Definition,
            fixture.Inputs.CondenserInputs,
            new[] { new CondensateFeedwaterTrainInput("feedwater-train", Power.FromMegawatts(3d)) }));
    }

    [Fact]
    public void Step_IsDeterministicForIdenticalCommittedStatesAndInputs()
    {
        var fixture = CreateFixture(MassFlowRate.Zero, Power.FromMegawatts(0.5d));
        var solver = new CondensateFeedwaterSystemSolver(fixture.Definition, fixture.ThermodynamicModel);

        var left = solver.Step(fixture.PlantState, fixture.TurbineState, fixture.Inputs, TimeSpan.FromSeconds(1d));
        var right = solver.Step(fixture.PlantState, fixture.TurbineState, fixture.Inputs, TimeSpan.FromSeconds(1d));

        Assert.Equal(left.CandidatePlantState.GetFluidNode("hotwell").Inventory, right.CandidatePlantState.GetFluidNode("hotwell").Inventory);
        Assert.Equal(left.CandidatePlantState.GetFluidNode("feedwater-inventory").Inventory, right.CandidatePlantState.GetFluidNode("feedwater-inventory").Inventory);
        Assert.Equal(left.Snapshot.GetTrain("feedwater-train"), right.Snapshot.GetTrain("feedwater-train"));
    }

    private static Fixture CreateFixture(MassFlowRate legacyFeedwaterFlow, Power conditioningPower)
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
                Node("header"), Node("stop-out"), Node("control-out"), Node("turbine-inlet"), Node("exhaust"), Node("hotwell"),
                Node("feedwater-inventory"),
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
            VaporQuality? vaporQuality = null)
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
                    vaporQuality));
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
                    "rotor", MomentOfInertia.FromKilogramSquareMetres(1_000d),
                    AngularSpeed.FromRevolutionsPerMinute(3_000d), AngularSpeed.FromRevolutionsPerMinute(3_300d)),
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
        var definition = new CondensateFeedwaterSystemDefinition(
            "feedwater-system",
            condensers,
            new[]
            {
                new CondensateFeedwaterTrainDefinition(
                    "feedwater-train", "condenser", "feed", "condensate-pump", "feedwater-inventory", "feedwater-pump",
                    Power.FromMegawatts(2d)),
            });

        var primaryBoundaryInputs = new PrimaryCircuitBoundaryInputs(
            boundaries,
            new[] { new FeedwaterBoundaryInput("feed", legacyFeedwaterFlow, SpecificEnergy.Zero) },
            new[] { new SteamExportBoundaryInput("export", MassFlowRate.Zero) });
        var primaryInputs = new IntegratedPrimaryCircuitInputs(
            primary, AggregatedCoreState.CreateNominal(core), Power.Zero, Power.Zero, primaryBoundaryInputs);
        var mainSteamInputs = new MainSteamNetworkInputs(
            mainSteam, primaryInputs, new[] { new TurbineAdmissionBoundaryInput("turbine-boundary", MassFlowRate.Zero) });
        var turbineInputs = new TurbineExpansionInputs(
            turbine,
            mainSteamInputs,
            new[] { new TurbineStageGroupInput("stage", MassFlowRate.Zero) },
            new[] { new TurbineRotorInput("rotor", Torque.Zero, tripCommand: false) });
        var condenserInputs = new CondenserSystemInputs(
            condensers, turbineInputs, new[] { new CondenserCoolingBoundaryInput("cooling", Power.Zero) });
        var inputs = new CondensateFeedwaterSystemInputs(
            definition,
            condenserInputs,
            new[] { new CondensateFeedwaterTrainInput("feedwater-train", conditioningPower) });
        var turbineState = new TurbineExpansionState(
            turbine, new[] { new TurbineRotorState("rotor", AngularSpeed.FromRevolutionsPerMinute(3_000d)) });

        return new Fixture(definition, plantState, turbineState, inputs, thermodynamicModel);
    }

    private sealed record Fixture(
        CondensateFeedwaterSystemDefinition Definition,
        PlantState PlantState,
        TurbineExpansionState TurbineState,
        CondensateFeedwaterSystemInputs Inputs,
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
