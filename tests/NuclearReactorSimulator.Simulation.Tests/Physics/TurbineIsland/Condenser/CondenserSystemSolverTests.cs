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
using NuclearReactorSimulator.Domain.Physics.TurbineIsland.MainSteam;
using NuclearReactorSimulator.Domain.Physics.TurbineIsland.Turbine;
using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using NuclearReactorSimulator.Simulation.Physics.Reactor.Core;
using NuclearReactorSimulator.Simulation.Physics.Reactor.PrimaryCircuit.Boundaries;
using NuclearReactorSimulator.Simulation.Physics.Reactor.PrimaryCircuit.Integration;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Condenser;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.MainSteam;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Turbine;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Physics.TurbineIsland.Condenser;

public sealed class CondenserSystemSolverTests
{
    [Fact]
    public void Step_CondensesSteamTransfersMassToHotwellAndRejectsHeatConservatively()
    {
        var fixture = CreateFixture(Power.FromMegawatts(3d), new PreservingThermodynamicModel());
        var solver = new CondenserSystemSolver(fixture.Definition, fixture.ThermodynamicModel);

        var result = solver.Step(
            fixture.PlantState,
            fixture.TurbineState,
            fixture.Inputs,
            TimeSpan.FromSeconds(1d));
        var condenser = Assert.Single(result.Snapshot.Condensers);

        Assert.Equal(2d, condenser.ActualCondensationMassFlowRate.KilogramsPerSecond, 12);
        Assert.Equal(3d, condenser.HeatRejectionPower.Megawatts, 9);
        Assert.Equal(9_998d, result.CandidatePlantState.GetFluidNode("exhaust").Mass.Kilograms, 9);
        Assert.Equal(10_002d, result.CandidatePlantState.GetFluidNode("hotwell").Mass.Kilograms, 9);
        Assert.Equal(-3d, result.Snapshot.ThermofluidAudit.SupplementalExternalPower.Megawatts, 9);
        Assert.InRange(Math.Abs(result.Snapshot.ThermofluidAudit.BalanceMassRateResidualKilogramsPerSecond), 0d, 1e-12d);
        Assert.InRange(Math.Abs(result.Snapshot.ThermofluidAudit.BalancePowerResidualWatts), 0d, 1e-6d);
        Assert.InRange(Math.Abs(result.Snapshot.ThermofluidAudit.EnergyClosureResidualJoules), 0d, 1e-3d);
    }

    [Fact]
    public void Step_CoolingBoundaryCapacityLimitsCondensationRate()
    {
        var fixture = CreateFixture(Power.FromMegawatts(1.5d), new PreservingThermodynamicModel());
        var solver = new CondenserSystemSolver(fixture.Definition, fixture.ThermodynamicModel);

        var result = solver.Step(
            fixture.PlantState,
            fixture.TurbineState,
            fixture.Inputs,
            TimeSpan.FromSeconds(1d));
        var condenser = Assert.Single(result.Snapshot.Condensers);
        var boundary = Assert.Single(result.Snapshot.CoolingBoundaries);

        Assert.Equal(1d, condenser.ThermalLimitedCondensationMassFlowRate.KilogramsPerSecond, 12);
        Assert.Equal(1d, condenser.ActualCondensationMassFlowRate.KilogramsPerSecond, 12);
        Assert.Equal(1.5d, boundary.UsedHeatRejectionPower.Megawatts, 9);
        Assert.Equal(Power.Zero, boundary.UnusedHeatRejectionPower);
    }

    [Fact]
    public void Step_NoCoolingCapacityProducesNoCondensation()
    {
        var fixture = CreateFixture(Power.Zero, new PreservingThermodynamicModel());
        var solver = new CondenserSystemSolver(fixture.Definition, fixture.ThermodynamicModel);

        var result = solver.Step(
            fixture.PlantState,
            fixture.TurbineState,
            fixture.Inputs,
            TimeSpan.FromSeconds(1d));
        var condenser = Assert.Single(result.Snapshot.Condensers);

        Assert.Equal(MassFlowRate.Zero, condenser.ActualCondensationMassFlowRate);
        Assert.Equal(Power.Zero, condenser.HeatRejectionPower);
        Assert.Equal(fixture.PlantState.GetFluidNode("hotwell").Mass, result.CandidatePlantState.GetFluidNode("hotwell").Mass);
    }

    [Fact]
    public void Step_CondensationReducesSteamSpacePressureAndIncreasesVacuumWhenClosureRespondsToInventory()
    {
        var model = new ExhaustMassPressureThermodynamicModel();
        var fixture = CreateFixture(Power.FromMegawatts(3d), model);
        var solver = new CondenserSystemSolver(fixture.Definition, model);

        var result = solver.Step(
            fixture.PlantState,
            fixture.TurbineState,
            fixture.Inputs,
            TimeSpan.FromSeconds(1d));
        var condenser = Assert.Single(result.Snapshot.Condensers);

        Assert.True(condenser.FinalSteamSpacePressure < condenser.InitialSteamSpacePressure);
        Assert.True(condenser.FinalVacuumBelowAtmosphere > condenser.InitialVacuumBelowAtmosphere);
    }

    [Fact]
    public void Step_IsDeterministicForIdenticalCommittedStatesAndInputs()
    {
        var fixture = CreateFixture(Power.FromMegawatts(3d), new PreservingThermodynamicModel());
        var solver = new CondenserSystemSolver(fixture.Definition, fixture.ThermodynamicModel);

        var left = solver.Step(fixture.PlantState, fixture.TurbineState, fixture.Inputs, TimeSpan.FromSeconds(1d));
        var right = solver.Step(fixture.PlantState, fixture.TurbineState, fixture.Inputs, TimeSpan.FromSeconds(1d));

        Assert.Equal(left.CandidatePlantState.GetFluidNode("exhaust").Inventory, right.CandidatePlantState.GetFluidNode("exhaust").Inventory);
        Assert.Equal(left.CandidatePlantState.GetFluidNode("hotwell").Inventory, right.CandidatePlantState.GetFluidNode("hotwell").Inventory);
        Assert.Equal(left.Snapshot.TotalHeatRejectionPower, right.Snapshot.TotalHeatRejectionPower);
        Assert.Equal(left.Snapshot.GetCondenser("condenser"), right.Snapshot.GetCondenser("condenser"));
    }

    [Fact]
    public void Inputs_RequireExactCoolingBoundaryCoverage()
    {
        var fixture = CreateFixture(Power.FromMegawatts(3d), new PreservingThermodynamicModel());

        Assert.Throws<ArgumentException>(() => new CondenserSystemInputs(
            fixture.Definition,
            fixture.Inputs.TurbineExpansionInputs,
            Array.Empty<CondenserCoolingBoundaryInput>()));
    }

    private static Fixture CreateFixture(Power coolingPower, IFluidThermodynamicModel thermodynamicModel)
    {
        FluidNodeDefinition Node(string id) => new(id, Volume.FromCubicMetres(10d));
        PipeDefinition Pipe(string id, string from, string to) => new(
            id, from, to, QuadraticHydraulicResistance.FromPascalSecondsSquaredPerKilogramSquared(100_000d));
        ValveDefinition Valve(string id, string from, string to) => new(
            id, Pipe($"{id}-path", from, to), ValveCharacteristic.Linear, ValveFailSafeAction.FailClosed);

        var plant = new PlantDefinition(
            "plant",
            new[]
            {
                Node("suction"), Node("pressure"), Node("outlet"), Node("drum"), Node("steam"),
                Node("header"), Node("stop-out"), Node("control-out"), Node("turbine-inlet"), Node("exhaust"), Node("hotwell"),
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
                new PumpDefinition(
                    "pump", Pipe("pump-path", "suction", "pressure"), PressureDifference.FromMegapascals(1d),
                    QuadraticHydraulicResistance.FromPascalSecondsSquaredPerKilogramSquared(50_000d), PumpEfficiency.FromPercent(80d)),
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
                    Temperature.FromDegreesCelsius(280d),
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
            },
            new[]
            {
                new ValveState("stop", ValvePosition.FullyOpen),
                new ValveState("control", ValvePosition.FullyOpen),
                new ValveState("admission", ValvePosition.FullyOpen),
            },
            new[] { new PumpState("pump", PumpSpeed.Stopped, isRunning: false) },
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
        var definition = new CondenserSystemDefinition(
            "condensers",
            turbine,
            new[]
            {
                new CondenserDefinition(
                    "condenser", "stage", "exhaust", "hotwell", "cooling",
                    MassFlowRate.FromKilogramsPerSecond(10d)),
            },
            new[] { new CondenserCoolingBoundaryDefinition("cooling", "condenser") });

        var primaryBoundaryInputs = new PrimaryCircuitBoundaryInputs(
            boundaries,
            new[] { new FeedwaterBoundaryInput("feed", MassFlowRate.Zero, SpecificEnergy.Zero) },
            new[] { new SteamExportBoundaryInput("export", MassFlowRate.Zero) });
        var primaryInputs = new IntegratedPrimaryCircuitInputs(
            primary,
            AggregatedCoreState.CreateNominal(core),
            Power.Zero,
            Power.Zero,
            primaryBoundaryInputs);
        var mainSteamInputs = new MainSteamNetworkInputs(
            mainSteam,
            primaryInputs,
            new[] { new TurbineAdmissionBoundaryInput("turbine-boundary", MassFlowRate.Zero) });
        var turbineInputs = new TurbineExpansionInputs(
            turbine,
            mainSteamInputs,
            new[] { new TurbineStageGroupInput("stage", MassFlowRate.Zero) },
            new[] { new TurbineRotorInput("rotor", Torque.Zero, tripCommand: false) });
        var inputs = new CondenserSystemInputs(
            definition,
            turbineInputs,
            new[] { new CondenserCoolingBoundaryInput("cooling", coolingPower) });
        var turbineState = new TurbineExpansionState(
            turbine,
            new[] { new TurbineRotorState("rotor", AngularSpeed.FromRevolutionsPerMinute(3_000d)) });

        return new Fixture(definition, plantState, turbineState, inputs, thermodynamicModel);
    }

    private sealed record Fixture(
        CondenserSystemDefinition Definition,
        PlantState PlantState,
        TurbineExpansionState TurbineState,
        CondenserSystemInputs Inputs,
        IFluidThermodynamicModel ThermodynamicModel);

    private sealed class PreservingThermodynamicModel : IFluidThermodynamicModel
    {
        public FluidThermodynamicState Resolve(
            FluidNodeDefinition definition,
            FluidNodeInventory inventory,
            FluidThermodynamicState previousState)
            => previousState;
    }

    private sealed class ExhaustMassPressureThermodynamicModel : IFluidThermodynamicModel
    {
        public FluidThermodynamicState Resolve(
            FluidNodeDefinition definition,
            FluidNodeInventory inventory,
            FluidThermodynamicState previousState)
        {
            if (!string.Equals(definition.Id, "exhaust", StringComparison.Ordinal))
            {
                return previousState;
            }

            return new FluidThermodynamicState(
                Pressure.FromPascals(inventory.Mass.Kilograms * 10d),
                previousState.Temperature,
                previousState.Phase,
                previousState.VaporQuality);
        }
    }
}
