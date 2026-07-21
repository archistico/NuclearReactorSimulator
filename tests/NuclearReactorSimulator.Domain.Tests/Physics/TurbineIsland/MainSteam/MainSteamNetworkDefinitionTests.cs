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
using Xunit;

namespace NuclearReactorSimulator.Domain.Tests.Physics.TurbineIsland.MainSteam;

public sealed class MainSteamNetworkDefinitionTests
{
    [Fact]
    public void Definition_BindsEveryM3SteamExportAndCanonicalAdmissionValveChain()
    {
        var primary = CreatePrimaryCircuit();

        var definition = new MainSteamNetworkDefinition(
            "main-steam",
            primary,
            new[] { new MainSteamLineDefinition("line-a", "export", "main-steam-line", "header") },
            new[] { new TurbineAdmissionTrainDefinition("train-a", "header", "stop", "control", "admission", "turbine-inlet") },
            new[] { new TurbineAdmissionBoundaryDefinition("turbine-boundary", "train-a", "turbine-inlet") });

        Assert.Same(primary, definition.PrimaryCircuit);
        Assert.Same(primary.PlantDefinition, definition.PlantDefinition);
        Assert.Equal("main-steam-line", definition.GetSteamLine("line-a").PipeId);
        Assert.Equal("admission", definition.GetAdmissionTrain("train-a").AdmissionValveId);
        Assert.Equal("turbine-inlet", definition.GetTurbineAdmissionBoundary("turbine-boundary").SourceNodeId);
    }

    [Fact]
    public void Definition_RequiresEveryM3SteamExportSeamToFeedExactlyOneLine()
    {
        var primary = CreatePrimaryCircuit();

        Assert.Throws<ArgumentException>(() => new MainSteamNetworkDefinition(
            "main-steam",
            primary,
            Array.Empty<MainSteamLineDefinition>(),
            new[] { new TurbineAdmissionTrainDefinition("train-a", "header", "stop", "control", "admission", "turbine-inlet") },
            new[] { new TurbineAdmissionBoundaryDefinition("turbine-boundary", "train-a", "turbine-inlet") }));
    }

    [Fact]
    public void Definition_RejectsBrokenAdmissionValveChain()
    {
        var primary = CreatePrimaryCircuit();

        Assert.Throws<ArgumentException>(() => new MainSteamNetworkDefinition(
            "main-steam",
            primary,
            new[] { new MainSteamLineDefinition("line-a", "export", "main-steam-line", "header") },
            new[] { new TurbineAdmissionTrainDefinition("train-a", "header", "control", "stop", "admission", "turbine-inlet") },
            new[] { new TurbineAdmissionBoundaryDefinition("turbine-boundary", "train-a", "turbine-inlet") }));
    }

    private static IntegratedPrimaryCircuitDefinition CreatePrimaryCircuit()
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

        return new IntegratedPrimaryCircuitDefinition("primary", boundaries);
    }
}
