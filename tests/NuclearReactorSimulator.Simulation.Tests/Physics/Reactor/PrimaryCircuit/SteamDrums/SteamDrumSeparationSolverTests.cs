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

        var result = fixture.Solver.Solve(fixture.State, circulation, TimeSpan.FromMilliseconds(10));
        var drum = result.Snapshot.GetDrum("drum-a");
        var loop = circulation.GetLoop("loop");
        var expectedLiquidKilogramsPerSecond = loop.Pumps
            .Sum(static pump => Math.Max(0d, pump.MassFlowRate.KilogramsPerSecond));

        Assert.Equal(expectedLiquidKilogramsPerSecond, drum.RecirculatedLiquidMassFlowRate.KilogramsPerSecond, 12);
        Assert.Equal(expectedLiquidKilogramsPerSecond, drum.RequestedLiquidRecirculationMassFlowRate.KilogramsPerSecond, 12);
        Assert.False(drum.LiquidRecirculationInventoryLimited);
        Assert.True(drum.HasSeparableLiquidInventory);
        var drumBalance = result.SourceTerms.FluidNodeBalances["drum-node"];
        Assert.Equal(
            -(drum.SeparatedSteamMassFlowRate.KilogramsPerSecond + drum.RecirculatedLiquidMassFlowRate.KilogramsPerSecond),
            drumBalance.NetMassFlowRate.KilogramsPerSecond,
            12);
        Assert.Equal(0d, result.SourceTerms.FluidNodeBalances.Values.Sum(static balance => balance.NetMassFlowRate.KilogramsPerSecond), 12);
    }

    [Fact]
    public void Solve_CirculationDemandBalanced_FullyVaporInventoryCannotFabricateLiquidRecirculation()
    {
        var fixture = CreateFixture(
            FluidPhase.SuperheatedVapor,
            null,
            SteamDrumLiquidRecirculationMode.CirculationDemandBalanced);
        var circulation = new MainCirculationSystemSolver(fixture.Solver.Definition.MainCirculationSystem)
            .Solve(fixture.State);

        var result = fixture.Solver.Solve(fixture.State, circulation, TimeSpan.FromMilliseconds(10));
        var drum = result.Snapshot.GetDrum("drum-a");

        Assert.True(drum.RequestedLiquidRecirculationMassFlowRate > MassFlowRate.Zero);
        Assert.Equal(Mass.Zero, drum.SeparableLiquidInventoryMass);
        Assert.False(drum.HasSeparableLiquidInventory);
        Assert.Equal(0d, drum.SeparableLiquidInventoryMassFraction, 12);
        Assert.True(drum.CommittedLiquidInventoryDepleted);
        Assert.True(drum.WaterSteamSeparationUnavailable);
        Assert.Equal(MassFlowRate.Zero, drum.RecirculatedLiquidMassFlowRate);
        Assert.Equal(MassFlowRate.Zero, drum.MaximumInventorySupportedLiquidRecirculationMassFlowRate);
        Assert.Equal(drum.RequestedLiquidRecirculationMassFlowRate, drum.LiquidRecirculationInventoryDeficitMassFlowRate);
        Assert.True(drum.LiquidRecirculationInventoryLimited);
    }

    [Fact]
    public void Solve_CirculationDemandBalanced_CapsLiquidRecirculationBySameStepAvailableLiquid()
    {
        var fixture = CreateFixture(
            FluidPhase.SaturatedMixture,
            0.25d,
            SteamDrumLiquidRecirculationMode.CirculationDemandBalanced);
        var circulation = new MainCirculationSystemSolver(fixture.Solver.Definition.MainCirculationSystem)
            .Solve(fixture.State);
        var interval = TimeSpan.FromDays(1);

        var result = fixture.Solver.Solve(fixture.State, circulation, interval);
        var drum = result.Snapshot.GetDrum("drum-a");
        var expectedIncomingLiquid = drum.IncomingReturnMassFlowRate.KilogramsPerSecond
            - drum.SeparatedSteamMassFlowRate.KilogramsPerSecond;
        var expectedMaximum = expectedIncomingLiquid + (drum.SeparableLiquidInventoryMass.Kilograms / interval.TotalSeconds);

        Assert.Equal(750d, drum.SeparableLiquidInventoryMass.Kilograms, 9);
        Assert.Equal(0.75d, drum.SeparableLiquidInventoryMassFraction, 12);
        Assert.False(drum.CommittedLiquidInventoryDepleted);
        Assert.False(drum.WaterSteamSeparationUnavailable);
        Assert.True(drum.RequestedLiquidRecirculationMassFlowRate > drum.MaximumInventorySupportedLiquidRecirculationMassFlowRate);
        Assert.Equal(expectedMaximum, drum.MaximumInventorySupportedLiquidRecirculationMassFlowRate.KilogramsPerSecond, 12);
        Assert.Equal(expectedMaximum, drum.RecirculatedLiquidMassFlowRate.KilogramsPerSecond, 12);
        Assert.True(drum.LiquidRecirculationInventoryLimited);
    }

    [Fact]
    public void Solve_CurrentSteamSource_WithoutReturnEnergySurplusOrStoredVaporProducesNoSteam()
    {
        var fixture = CreateFixture(
            FluidPhase.SubcooledLiquid,
            null,
            SteamDrumLiquidRecirculationMode.CirculationDemandBalanced,
            steamSourceResistancePascalSecondsSquaredPerKilogramSquared: 100d);

        var drum = fixture.Solver
            .Solve(fixture.State, TimeSpan.FromMilliseconds(10))
            .Snapshot
            .GetDrum("drum-a");

        Assert.True(drum.UsesPressureEnergyInventorySteamSource);
        Assert.True(drum.SteamSourcePressureDrivenCapacityMassFlowRate > MassFlowRate.Zero);
        Assert.Equal(Mass.Zero, drum.SteamSourceStoredVaporInventoryMass);
        Assert.Equal(MassFlowRate.Zero, drum.SteamSourceIncomingEnergySupportedMassFlowRate);
        Assert.Equal(MassFlowRate.Zero, drum.SeparatedSteamMassFlowRate);
        Assert.True(drum.SteamSourceAvailabilityLimited);
    }

    [Fact]
    public void Solve_CurrentSteamSource_IncreasingReturnEnergyIncreasesAvailableSteamMonotonically()
    {
        var lower = CreateFixture(
            FluidPhase.SubcooledLiquid,
            null,
            SteamDrumLiquidRecirculationMode.CirculationDemandBalanced,
            steamSourceResistancePascalSecondsSquaredPerKilogramSquared: 100d,
            outletSpecificEnergyKilojoulesPerKilogram: 1_200d);
        var higher = CreateFixture(
            FluidPhase.SubcooledLiquid,
            null,
            SteamDrumLiquidRecirculationMode.CirculationDemandBalanced,
            steamSourceResistancePascalSecondsSquaredPerKilogramSquared: 100d,
            outletSpecificEnergyKilojoulesPerKilogram: 1_800d);

        var lowerDrum = lower.Solver.Solve(lower.State, TimeSpan.FromMilliseconds(10)).Snapshot.GetDrum("drum-a");
        var higherDrum = higher.Solver.Solve(higher.State, TimeSpan.FromMilliseconds(10)).Snapshot.GetDrum("drum-a");

        Assert.True(lowerDrum.SteamSourceIncomingEnergySupportedMassFlowRate > MassFlowRate.Zero);
        Assert.True(higherDrum.SteamSourceIncomingEnergySupportedMassFlowRate > lowerDrum.SteamSourceIncomingEnergySupportedMassFlowRate);
        Assert.True(higherDrum.SteamSourceAvailableMassFlowRate > lowerDrum.SteamSourceAvailableMassFlowRate);
    }

    [Fact]
    public void Solve_CurrentSteamSource_IsPressureBoundedAndInternallyConservative()
    {
        var fixture = CreateFixture(
            FluidPhase.SaturatedMixture,
            0.25d,
            SteamDrumLiquidRecirculationMode.CirculationDemandBalanced,
            steamSourceResistancePascalSecondsSquaredPerKilogramSquared: 100_000d,
            outletSpecificEnergyKilojoulesPerKilogram: 1_800d);

        var result = fixture.Solver.Solve(fixture.State, TimeSpan.FromMilliseconds(10));
        var drum = result.Snapshot.GetDrum("drum-a");

        Assert.True(drum.UsesPressureEnergyInventorySteamSource);
        Assert.True(drum.SteamSourcePressureDrivenCapacityMassFlowRate > MassFlowRate.Zero);
        Assert.True(drum.SteamSourceAvailableMassFlowRate > drum.SteamSourcePressureDrivenCapacityMassFlowRate);
        Assert.Equal(drum.SteamSourcePressureDrivenCapacityMassFlowRate, drum.SeparatedSteamMassFlowRate);
        Assert.True(drum.SteamSourcePressureLimited);
        Assert.Equal(0d, result.SourceTerms.FluidNodeBalances.Values.Sum(static balance => balance.NetMassFlowRate.KilogramsPerSecond), 12);
        Assert.Equal(0d, result.SourceTerms.FluidNodeBalances.Values.Sum(static balance => balance.NetEnergyRate.Watts), 6);
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
    public void Solve_LegacyReturnSplit_IgnoresIntegrationIntervalAndRetainsHistoricalSplit()
    {
        var fixture = CreateFixture(
            FluidPhase.SaturatedMixture,
            0.25d,
            SteamDrumLiquidRecirculationMode.LegacyReturnSplit);

        var drum = fixture.Solver
            .Solve(fixture.State, TimeSpan.FromDays(1))
            .Snapshot
            .GetDrum("drum-a");
        var expectedLiquid = drum.IncomingReturnMassFlowRate - drum.SeparatedSteamMassFlowRate;

        Assert.Equal(expectedLiquid, drum.RecirculatedLiquidMassFlowRate);
        Assert.Equal(expectedLiquid, drum.RequestedLiquidRecirculationMassFlowRate);
        Assert.Equal(expectedLiquid, drum.MaximumInventorySupportedLiquidRecirculationMassFlowRate);
        Assert.False(drum.LiquidRecirculationInventoryLimited);
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
        SteamDrumLiquidRecirculationMode liquidRecirculationMode = SteamDrumLiquidRecirculationMode.LegacyReturnSplit,
        double? steamSourceResistancePascalSecondsSquaredPerKilogramSquared = null,
        double outletSpecificEnergyKilojoulesPerKilogram = 625d)
    {
        var thermodynamics = new SimplifiedWaterSteamThermodynamicModel();
        var saturation = thermodynamics.GetSaturationProperties(Temperature.FromDegreesCelsius(280d));
        var plant = BuildPlant();

        FluidNodeState SimpleFluid(
            string id,
            double pressureMpa,
            double temperatureCelsius,
            double massKilograms = 8_000d,
            double specificEnergyKilojoulesPerKilogram = 625d)
            => new(
                plant.GetFluidNode(id),
                new FluidNodeInventory(
                    Mass.FromKilograms(massKilograms),
                    Energy.FromJoules(specificEnergyKilojoulesPerKilogram * 1_000d * massKilograms)),
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
                SimpleFluid("outlet", 7.0d, 285d, specificEnergyKilojoulesPerKilogram: outletSpecificEnergyKilojoulesPerKilogram),
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
                    liquidRecirculationMode,
                    steamSourceResistancePascalSecondsSquaredPerKilogramSquared.HasValue
                        ? new SteamDrumSteamSourceDefinition(
                            QuadraticHydraulicResistance.FromPascalSecondsSquaredPerKilogramSquared(
                                steamSourceResistancePascalSecondsSquaredPerKilogramSquared.Value))
                        : null),
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
