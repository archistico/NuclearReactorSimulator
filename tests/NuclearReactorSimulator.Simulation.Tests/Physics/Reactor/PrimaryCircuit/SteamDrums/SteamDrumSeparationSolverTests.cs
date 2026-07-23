using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor.Core;
using NuclearReactorSimulator.Domain.Physics.Reactor.Core.Channels;
using NuclearReactorSimulator.Domain.Physics.Reactor.PrimaryCircuit.Circulation;
using NuclearReactorSimulator.Domain.Physics.Reactor.PrimaryCircuit.SteamDrums;
using NuclearReactorSimulator.Domain.Physics.Reactor.ThermalPower;
using NuclearReactorSimulator.Domain.Physics.Thermal;
using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using NuclearReactorSimulator.Simulation.Physics.Reactor.PrimaryCircuit.Circulation;
using NuclearReactorSimulator.Simulation.Physics.Reactor.PrimaryCircuit.SteamDrums;
using NuclearReactorSimulator.Simulation.Plant;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Physics.Reactor.PrimaryCircuit.SteamDrums;

public sealed class SteamDrumSeparationSolverTests
{
    [Fact]
    public void Solve_SaturatedMixtureSeparatesReturnFlowByMassQuality()
    {
        var fixture = CreateFixture(FluidPhase.SaturatedMixture, 0.25d);

        var result = fixture.Solver.Solve(fixture.State);
        var drum = result.Snapshot.GetDrum("drum-a");

        Assert.True(drum.IncomingReturnMassFlowRate > MassFlowRate.Zero);
        Assert.Equal(
            drum.IncomingReturnMassFlowRate.KilogramsPerSecond * 0.25d,
            drum.SeparatedSteamMassFlowRate.KilogramsPerSecond,
            12);
        Assert.Equal(
            drum.IncomingReturnMassFlowRate.KilogramsPerSecond * 0.75d,
            drum.RecirculatedLiquidMassFlowRate.KilogramsPerSecond,
            12);
        Assert.InRange(drum.LiquidLevelFraction.Fraction, 0d, 1d);
        Assert.True(drum.VoidFraction.Fraction > 0.25d);
    }


    [Fact]
    public void Solve_CirculationDemandBalanced_UsesCommittedPumpDemandForLiquidRecirculation()
    {
        var fixture = CreateFixture(
            FluidPhase.SaturatedMixture,
            0.25d,
            SteamDrumLiquidRecirculationMode.CirculationDemandBalanced);
        var circulation = new MainCirculationSystemSolver(fixture.Solver.Definition.MainCirculationSystem)
            .Solve(fixture.State);

        var result = fixture.Solver.Solve(fixture.State, circulation);
        var drum = result.Snapshot.GetDrum("drum-a");
        var loop = circulation.GetLoop("loop");
        var expectedLiquidKilogramsPerSecond = loop.Pumps
            .Sum(static pump => Math.Max(0d, pump.MassFlowRate.KilogramsPerSecond));

        Assert.Equal(expectedLiquidKilogramsPerSecond, drum.RecirculatedLiquidMassFlowRate.KilogramsPerSecond, 12);
        var drumBalance = result.SourceTerms.FluidNodeBalances["drum-node"];
        Assert.Equal(
            -(drum.SeparatedSteamMassFlowRate.KilogramsPerSecond + drum.RecirculatedLiquidMassFlowRate.KilogramsPerSecond),
            drumBalance.NetMassFlowRate.KilogramsPerSecond,
            12);
        Assert.Equal(0d, result.SourceTerms.FluidNodeBalances.Values.Sum(static balance => balance.NetMassFlowRate.KilogramsPerSecond), 12);
    }

    [Fact]
    public void Solve_SourceTermsAreInternallyMassAndEnergyConservative()
    {
        var fixture = CreateFixture(FluidPhase.SaturatedMixture, 0.25d);
        var result = fixture.Solver.Solve(fixture.State);
        var balances = result.SourceTerms.FluidNodeBalances.Values.ToArray();

        Assert.Equal(0d, balances.Sum(static balance => balance.NetMassFlowRate.KilogramsPerSecond), 9);
        Assert.Equal(0d, balances.Sum(static balance => balance.NetEnergyRate.Watts), 6);
        Assert.Equal(Power.Zero, result.SourceTerms.ExternalPower);
        Assert.Equal(0d, result.Snapshot.GetDrum("drum-a").SeparationMassResidualKilogramsPerSecond, 12);
        Assert.Equal(0d, result.Snapshot.GetDrum("drum-a").SeparationEnergyResidualWatts, 6);
    }

    [Fact]
    public void Solve_SubcooledInventoryRecirculatesAllSeparatedFlowAsLiquid()
    {
        var fixture = CreateFixture(FluidPhase.SubcooledLiquid, null);
        var drum = fixture.Solver.Solve(fixture.State).Snapshot.GetDrum("drum-a");

        Assert.Equal(MassFlowRate.Zero, drum.SeparatedSteamMassFlowRate);
        Assert.Equal(drum.IncomingReturnMassFlowRate, drum.RecirculatedLiquidMassFlowRate);
        Assert.Equal(SteamDrumLevelFraction.Full, drum.LiquidLevelFraction);
        Assert.Equal(VoidFraction.NoVoid, drum.VoidFraction);
    }

    [Fact]
    public void NetworkOrchestrator_WithSteamDrumSourceTerms_PreservesGlobalMassAndEnergyClosure()
    {
        var fixture = CreateFixture(FluidPhase.SaturatedMixture, 0.25d);
        var separation = fixture.Solver.Solve(fixture.State);
        var network = new PlantNetworkOrchestrator(new PreservingThermodynamicModel())
            .Step(fixture.State, TimeSpan.FromMilliseconds(20), separation.SourceTerms);

        Assert.InRange(Math.Abs(network.Audit.MassClosureResidualKilograms), 0d, 1e-6d);
        Assert.InRange(Math.Abs(network.Audit.BalancePowerResidualWatts), 0d, 1e-3d);
        Assert.InRange(Math.Abs(network.Audit.EnergyClosureResidualJoules), 0d, 10d);
        Assert.True(network.CandidateState.GetFluidNode("steam-outlet").Mass > fixture.State.GetFluidNode("steam-outlet").Mass);
    }

    private static Fixture CreateFixture(
        FluidPhase drumPhase,
        double? quality,
        SteamDrumLiquidRecirculationMode liquidRecirculationMode = SteamDrumLiquidRecirculationMode.LegacyReturnSplit)
    {
        var thermodynamics = new SimplifiedWaterSteamThermodynamicModel();
        var saturation = thermodynamics.GetSaturationProperties(Temperature.FromDegreesCelsius(280d));
        var plant = BuildPlant();

        FluidNodeState SimpleFluid(string id, double pressureMpa, double temperatureCelsius, double massKilograms = 8_000d)
            => new(
                plant.GetFluidNode(id),
                new FluidNodeInventory(Mass.FromKilograms(massKilograms), Energy.FromMegajoules(5_000)),
                new FluidThermodynamicState(
                    Pressure.FromMegapascals(pressureMpa),
                    Temperature.FromDegreesCelsius(temperatureCelsius),
                    FluidPhase.SubcooledLiquid,
                    null));

        FluidNodeState drumState;
        if (drumPhase == FluidPhase.SaturatedMixture)
        {
            var vaporQuality = VaporQuality.FromFraction(quality ?? throw new ArgumentNullException(nameof(quality)));
            const double massKilograms = 1_000d;
            var specificVolume =
                ((1d - vaporQuality.Fraction) * saturation.SaturatedLiquidSpecificVolumeCubicMetresPerKilogram)
                + (vaporQuality.Fraction * saturation.SaturatedVaporSpecificVolumeCubicMetresPerKilogram);
            var specificEnergy =
                ((1d - vaporQuality.Fraction) * saturation.SaturatedLiquidInternalEnergy.JoulesPerKilogram)
                + (vaporQuality.Fraction * saturation.SaturatedVaporInternalEnergy.JoulesPerKilogram);
            var definition = plant.GetFluidNode("drum-node");
            Assert.Equal(definition.Volume.CubicMetres, specificVolume * massKilograms, 9);
            drumState = new FluidNodeState(
                definition,
                new FluidNodeInventory(Mass.FromKilograms(massKilograms), Energy.FromJoules(specificEnergy * massKilograms)),
                new FluidThermodynamicState(saturation.Pressure, saturation.Temperature, drumPhase, vaporQuality));
        }
        else
        {
            drumState = new FluidNodeState(
                plant.GetFluidNode("drum-node"),
                new FluidNodeInventory(Mass.FromKilograms(8_000d), Energy.FromMegajoules(5_000)),
                new FluidThermodynamicState(
                    Pressure.FromMegapascals(6.2d),
                    Temperature.FromDegreesCelsius(270d),
                    drumPhase,
                    null));
        }

        var state = new PlantState(
            plant,
            new[]
            {
                SimpleFluid("suction", 6.0d, 270d),
                SimpleFluid("pressure", 7.4d, 272d),
                SimpleFluid("outlet", 7.0d, 285d),
                drumState,
                SimpleFluid("steam-outlet", 6.0d, 280d, 500d),
            },
            Array.Empty<ValveState>(),
            new[] { new PumpState("mcp", PumpSpeed.Rated) },
            new[]
            {
                ThermalBodyState.FromTemperature(plant.GetThermalBody("fuel"), Temperature.FromDegreesCelsius(700d)),
                ThermalBodyState.FromTemperature(plant.GetThermalBody("structure"), Temperature.FromDegreesCelsius(500d)),
            },
            Array.Empty<HeatSourceState>());

        var core = new AggregatedCoreDefinition(
            "core",
            plant,
            new[]
            {
                new CoreZoneDefinition(
                    "zone",
                    new CoreZoneCoordinate(0, 0),
                    CoreZonePowerFraction.FromPercent(100),
                    "fuel",
                    "structure",
                    "outlet"),
            });
        var groups = new FuelChannelGroupSetDefinition(
            "channels",
            core,
            new[]
            {
                new FuelChannelGroupDefinition(
                    "group",
                    "zone",
                    100,
                    CoreZonePowerFraction.FromPercent(100),
                    "channel",
                    "pressure",
                    "outlet",
                    "fuel",
                    "structure",
                    HeatDepositionFraction.FromPercent(70),
                    HeatDepositionFraction.FromPercent(10),
                    HeatDepositionFraction.FromPercent(20)),
            });
        var circulation = new MainCirculationSystemDefinition(
            "mcs",
            groups,
            new[]
            {
                new MainCirculationLoopDefinition(
                    "loop",
                    "suction",
                    "pressure",
                    "drum-node",
                    new[] { "mcp" },
                    new[] { new MainCirculationBranchDefinition("group", "return") }),
            });
        var drums = new SteamDrumSystemDefinition(
            "drums",
            circulation,
            new[]
            {
                new SteamDrumDefinition(
                    "drum-a",
                    "loop",
                    "drum-node",
                    "steam-outlet",
                    liquidRecirculationMode),
            });

        return new Fixture(state, new SteamDrumSeparationSolver(drums));
    }

    private static PlantDefinition BuildPlant()
    {
        var thermodynamics = new SimplifiedWaterSteamThermodynamicModel();
        var saturation = thermodynamics.GetSaturationProperties(Temperature.FromDegreesCelsius(280d));
        const double quality = 0.25d;
        const double drumMassKilograms = 1_000d;
        var drumSpecificVolume =
            ((1d - quality) * saturation.SaturatedLiquidSpecificVolumeCubicMetresPerKilogram)
            + (quality * saturation.SaturatedVaporSpecificVolumeCubicMetresPerKilogram);

        FluidNodeDefinition Node(string id, double volume = 10d) => new(id, Volume.FromCubicMetres(volume));
        PipeDefinition Pipe(string id, string from, string to, double resistance) => new(
            id,
            from,
            to,
            QuadraticHydraulicResistance.FromPascalSecondsSquaredPerKilogramSquared(resistance));

        return new PlantDefinition(
            "plant",
            new[]
            {
                Node("suction"),
                Node("pressure"),
                Node("outlet"),
                Node("drum-node", drumSpecificVolume * drumMassKilograms),
                Node("steam-outlet"),
            },
            new[]
            {
                Pipe("channel", "pressure", "outlet", 100_000d),
                Pipe("return", "outlet", "drum-node", 150_000d),
            },
            Array.Empty<ValveDefinition>(),
            new[]
            {
                new PumpDefinition(
                    "mcp",
                    Pipe("mcp-path", "suction", "pressure", 80_000d),
                    PressureDifference.FromMegapascals(1.8d),
                    QuadraticHydraulicResistance.FromPascalSecondsSquaredPerKilogramSquared(40_000d),
                    PumpEfficiency.FromPercent(82d)),
            },
            new[]
            {
                new ThermalBodyDefinition("fuel", HeatCapacity.FromJoulesPerKelvin(10_000_000d)),
                new ThermalBodyDefinition("structure", HeatCapacity.FromJoulesPerKelvin(20_000_000d)),
            },
            Array.Empty<HeatTransferDefinition>(),
            Array.Empty<HeatSourceDefinition>());
    }

    private sealed record Fixture(PlantState State, SteamDrumSeparationSolver Solver);

    private sealed class PreservingThermodynamicModel : IFluidThermodynamicModel
    {
        public FluidThermodynamicState Resolve(
            FluidNodeDefinition definition,
            FluidNodeInventory inventory,
            FluidThermodynamicState previousState)
            => previousState;
    }
}
