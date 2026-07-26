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
using NuclearReactorSimulator.Domain.Physics.TurbineIsland.MainSteam;
using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using NuclearReactorSimulator.Simulation.Physics.Reactor.PrimaryCircuit.Boundaries;
using NuclearReactorSimulator.Simulation.Physics.Reactor.PrimaryCircuit.Integration;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.MainSteam;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Physics.TurbineIsland.MainSteam;

public sealed class MainSteamReliefBoundarySolverTests
{
    [Fact]
    public void Solve_RemainsClosedAtSetPressureAndLiftsLinearlyAboveIt()
    {
        var fixture = CreateFixture(6.5d, FluidPhase.SuperheatedVapor, vaporQuality: null);
        var solver = new MainSteamReliefBoundarySolver(fixture.Definition);

        var closed = Assert.Single(solver.Solve(fixture.State).Snapshots);
        var halfLiftFixture = CreateFixture(6.6d, FluidPhase.SuperheatedVapor, vaporQuality: null);
        var halfLift = Assert.Single(new MainSteamReliefBoundarySolver(halfLiftFixture.Definition)
            .Solve(halfLiftFixture.State).Snapshots);

        Assert.Equal(0d, closed.LiftFraction, 12);
        Assert.Equal(MassFlowRate.Zero, closed.MassFlowRate);
        Assert.Equal(0.5d, halfLift.LiftFraction, 12);
        Assert.Equal(1d, halfLift.VaporAvailabilityFraction, 12);
        Assert.True(halfLift.IsChoked);
        Assert.Equal(800d, halfLift.EffectiveThroatArea.SquareMillimetres, 9);
        Assert.True(halfLift.MassFlowRate > MassFlowRate.Zero);
    }

    [Fact]
    public void Solve_LimitsIdealVaporCapacityByCommittedVaporMassFraction()
    {
        var dryFixture = CreateFixture(6.7d, FluidPhase.SuperheatedVapor, vaporQuality: null);
        var wetFixture = CreateFixture(
            6.7d,
            FluidPhase.SaturatedMixture,
            VaporQuality.FromFraction(0.25d));
        var dry = Assert.Single(new MainSteamReliefBoundarySolver(dryFixture.Definition).Solve(dryFixture.State).Snapshots);
        var wet = Assert.Single(new MainSteamReliefBoundarySolver(wetFixture.Definition).Solve(wetFixture.State).Snapshots);

        Assert.Equal(1d, dry.VaporAvailabilityFraction, 12);
        Assert.Equal(0.25d, wet.VaporAvailabilityFraction, 12);
        Assert.Equal(dry.MassFlowRate.KilogramsPerSecond * 0.25d, wet.MassFlowRate.KilogramsPerSecond, 10);
        Assert.Equal(400d, wet.EffectiveThroatArea.SquareMillimetres, 9);
    }

    [Fact]
    public void Solve_BlocksSubcooledLiquidFromIdealVaporReliefCapacity()
    {
        var fixture = CreateFixture(6.7d, FluidPhase.SubcooledLiquid, vaporQuality: null);
        var snapshot = Assert.Single(new MainSteamReliefBoundarySolver(fixture.Definition)
            .Solve(fixture.State).Snapshots);

        Assert.Equal(1d, snapshot.LiftFraction, 12);
        Assert.Equal(0d, snapshot.VaporAvailabilityFraction, 12);
        Assert.Equal(Area.Zero, snapshot.EffectiveThroatArea);
        Assert.Equal(MassFlowRate.Zero, snapshot.MassFlowRate);
        Assert.Equal(Power.Zero, snapshot.EnergyExportRate);
    }

    [Fact]
    public void Solve_DeclaresEqualNodeAndExternalMassEnergyRemoval()
    {
        var fixture = CreateFixture(6.7d, FluidPhase.SuperheatedVapor, vaporQuality: null);
        var result = new MainSteamReliefBoundarySolver(fixture.Definition).Solve(fixture.State);
        var snapshot = Assert.Single(result.Snapshots);
        var balance = Assert.Single(result.SourceTerms.FluidNodeBalances);

        Assert.Equal("header", balance.Key);
        Assert.Equal(-snapshot.MassFlowRate.KilogramsPerSecond, balance.Value.NetMassFlowRate.KilogramsPerSecond, 12);
        Assert.Equal(-snapshot.EnergyExportRate.Watts, balance.Value.NetEnergyRate.Watts, 6);
        Assert.Equal(-snapshot.MassFlowRate.KilogramsPerSecond, result.SourceTerms.ExternalMassFlowRate.KilogramsPerSecond, 12);
        Assert.Equal(-snapshot.EnergyExportRate.Watts, result.SourceTerms.ExternalPower.Watts, 6);
        Assert.Equal(
            snapshot.ExportedSpecificInternalEnergy.JoulesPerKilogram * snapshot.MassFlowRate.KilogramsPerSecond,
            snapshot.EnergyExportRate.Watts,
            6);
    }

    [Fact]
    public void MainSteamStep_IntegratesReliefMassAndEnergyExactlyOnce()
    {
        var fixture = CreateFixture(6.7d, FluidPhase.SuperheatedVapor, vaporQuality: null);
        var solver = new MainSteamNetworkSolver(fixture.Definition, new PreservingThermodynamicModel());
        var deltaTime = TimeSpan.FromMilliseconds(100d);
        var initialHeader = fixture.State.GetFluidNode("header");

        var result = solver.Step(fixture.State, fixture.Inputs, deltaTime);
        var relief = Assert.Single(result.Snapshot.ReliefBoundaries);
        var candidateHeader = result.CandidateState.GetFluidNode("header");
        var relievedMass = relief.MassFlowRate.KilogramsPerSecond * deltaTime.TotalSeconds;
        var relievedEnergy = relief.EnergyExportRate.Watts * deltaTime.TotalSeconds;

        Assert.Equal(initialHeader.Mass.Kilograms - relievedMass, candidateHeader.Mass.Kilograms, 9);
        Assert.Equal(initialHeader.InternalEnergy.Joules - relievedEnergy, candidateHeader.InternalEnergy.Joules, 3);
        Assert.Equal(relief.MassFlowRate, result.Snapshot.TotalReliefMassFlowRate);
        Assert.Equal(relief.EnergyExportRate, result.Snapshot.TotalReliefEnergyExportRate);
        Assert.Equal(-relief.MassFlowRate.KilogramsPerSecond, result.Snapshot.Audit.ExpectedExternalMassFlowRate.KilogramsPerSecond, 9);
        Assert.Equal(-relief.EnergyExportRate.Watts, result.Snapshot.Audit.ExpectedExternalPower.Watts, 3);
        Assert.InRange(Math.Abs(result.Snapshot.Audit.BalanceMassRateResidualKilogramsPerSecond), 0d, 1e-9d);
        Assert.InRange(Math.Abs(result.Snapshot.Audit.BalancePowerResidualWatts), 0d, 1e-3d);
    }

    private static Fixture CreateFixture(
        double headerPressureMegapascals,
        FluidPhase headerPhase,
        VaporQuality? vaporQuality)
    {
        FluidNodeDefinition Node(string id) => new(id, Volume.FromCubicMetres(10d));
        PipeDefinition Pipe(string id, string from, string to) => new(
            id,
            from,
            to,
            QuadraticHydraulicResistance.FromPascalSecondsSquaredPerKilogramSquared(100_000d));
        ValveDefinition Valve(string id, string from, string to) => new(
            id,
            Pipe($"{id}-path", from, to),
            ValveCharacteristic.Linear,
            ValveFailSafeAction.FailClosed);

        var plant = new PlantDefinition(
            "plant",
            new[]
            {
                Node("suction"), Node("pressure"), Node("outlet"), Node("drum"), Node("steam"),
                Node("header"), Node("stop-out"), Node("control-out"), Node("turbine-inlet"),
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
                    "pump",
                    Pipe("pump-path", "suction", "pressure"),
                    PressureDifference.FromMegapascals(1d),
                    QuadraticHydraulicResistance.FromPascalSecondsSquaredPerKilogramSquared(50_000d),
                    PumpEfficiency.FromPercent(80d)),
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
            VaporQuality? quality = null)
            => new(
                plant.GetFluidNode(id),
                new FluidNodeInventory(Mass.FromKilograms(1_000d), Energy.FromMegajoules(2_000d)),
                new FluidThermodynamicState(
                    Pressure.FromMegapascals(pressureMegapascals),
                    Temperature.FromDegreesCelsius(278.5d),
                    phase,
                    quality));

        var state = new PlantState(
            plant,
            new[]
            {
                Fluid("suction", 6d, FluidPhase.SubcooledLiquid),
                Fluid("pressure", 6d, FluidPhase.SubcooledLiquid),
                Fluid("outlet", 6d, FluidPhase.SubcooledLiquid),
                Fluid("drum", 6d, FluidPhase.SubcooledLiquid),
                Fluid("steam", headerPressureMegapascals, FluidPhase.SuperheatedVapor),
                Fluid("header", headerPressureMegapascals, headerPhase, vaporQuality),
                Fluid("stop-out", headerPressureMegapascals, FluidPhase.SuperheatedVapor),
                Fluid("control-out", headerPressureMegapascals, FluidPhase.SuperheatedVapor),
                Fluid("turbine-inlet", headerPressureMegapascals, FluidPhase.SuperheatedVapor),
            },
            new[]
            {
                new ValveState("stop", ValvePosition.Closed),
                new ValveState("control", ValvePosition.Closed),
                new ValveState("admission", ValvePosition.Closed),
            },
            new[] { new PumpState("pump", PumpSpeed.Stopped, isRunning: false) },
            new[]
            {
                ThermalBodyState.FromTemperature(plant.GetThermalBody("fuel"), Temperature.FromDegreesCelsius(278.5d)),
                ThermalBodyState.FromTemperature(plant.GetThermalBody("structure"), Temperature.FromDegreesCelsius(278.5d)),
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
                    HeatDepositionFraction.FromPercent(70d),
                    HeatDepositionFraction.FromPercent(10d),
                    HeatDepositionFraction.FromPercent(20d)),
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
            "drums",
            circulation,
            new[] { new SteamDrumDefinition("drum-a", "loop", "drum", "steam") });
        var boundaries = new PrimaryCircuitBoundarySystemDefinition(
            "boundaries",
            drums,
            new[] { new FeedwaterBoundaryDefinition("feed", "drum-a", "drum") },
            new[] { new SteamExportBoundaryDefinition("export", "drum-a", "steam") });
        var primary = new IntegratedPrimaryCircuitDefinition("primary", boundaries);
        var relief = new MainSteamReliefBoundaryDefinition(
            "header-relief",
            "header",
            "atmospheric-relief-receiver",
            Pressure.StandardAtmosphere,
            Pressure.FromMegapascals(6.5d),
            Pressure.FromMegapascals(6.7d),
            new CompressibleSteamFlowDefinition(
                Area.FromSquareMillimetres(1_600d),
                dischargeCoefficient: 0.95d,
                specificGasConstant: SpecificGasConstant.FromJoulesPerKilogramKelvin(461.526d),
                heatCapacityRatio: 1.3d));
        var definition = new MainSteamNetworkDefinition(
            "main-steam",
            primary,
            new[] { new MainSteamLineDefinition("line-a", "export", "main-steam-line", "header") },
            new[] { new TurbineAdmissionTrainDefinition("train-a", "header", "stop", "control", "admission", "turbine-inlet") },
            new[] { new TurbineAdmissionBoundaryDefinition("turbine-boundary", "train-a", "turbine-inlet") },
            new[] { relief });

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
        var inputs = new MainSteamNetworkInputs(
            definition,
            primaryInputs,
            new[] { new TurbineAdmissionBoundaryInput("turbine-boundary", MassFlowRate.Zero) });

        return new Fixture(definition, state, inputs);
    }

    private sealed record Fixture(
        MainSteamNetworkDefinition Definition,
        PlantState State,
        MainSteamNetworkInputs Inputs);

    private sealed class PreservingThermodynamicModel : IFluidThermodynamicModel
    {
        public FluidThermodynamicState Resolve(
            FluidNodeDefinition definition,
            FluidNodeInventory inventory,
            FluidThermodynamicState previousState)
            => previousState;
    }
}
