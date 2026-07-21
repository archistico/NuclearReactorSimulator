using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Thermal;
using NuclearReactorSimulator.Domain.Plant;
using Xunit;

namespace NuclearReactorSimulator.Domain.Tests.Plant;

public sealed class PlantCompositionTests
{
    [Fact]
    public void Definition_CanonicalizesAllRegistriesAndIsIndependentFromCallerArrays()
    {
        var nodes = new[] { FluidNode("node-b"), FluidNode("node-a") };
        var definition = Plant(nodes: nodes);

        nodes[0] = FluidNode("replacement");

        Assert.Equal(new[] { "node-a", "node-b" }, definition.FluidNodes.Select(static item => item.Id));
        Assert.Equal("node-b", definition.GetFluidNode("node-b").Id);
        Assert.Equal(new[] { "heat-link" }, definition.HeatTransfers.Select(static item => item.Id));
    }

    [Fact]
    public void Definition_RejectsUnknownHydraulicEndpoint()
    {
        var badPipe = Pipe("bad-pipe", "node-a", "missing");

        Assert.Throws<ArgumentException>(() => Plant(pipes: [badPipe]));
    }

    [Fact]
    public void Definition_RejectsUnknownValveAndPumpEndpoints()
    {
        var badValve = new ValveDefinition(
            "bad-valve",
            Pipe("bad-valve-path", "node-a", "missing"),
            ValveCharacteristic.Linear,
            ValveFailSafeAction.FailClosed);
        var badPump = new PumpDefinition(
            "bad-pump",
            Pipe("bad-pump-path", "missing", "node-b"),
            PressureDifference.FromKilopascals(200d),
            Resistance(),
            PumpEfficiency.FromPercent(80d));

        Assert.Throws<ArgumentException>(() => Plant(valves: [badValve]));
        Assert.Throws<ArgumentException>(() => Plant(pumps: [badPump]));
    }

    [Fact]
    public void Definition_AllowsHeatTransferBetweenThermalBodyAndFluidNode()
    {
        var definition = Plant();
        var link = definition.GetHeatTransfer("heat-link");

        Assert.Equal("wall-a", link.FromDomainId);
        Assert.Equal("node-b", link.ToDomainId);
    }

    [Fact]
    public void Definition_RejectsUnknownThermalDomainAndHeatSourceTarget()
    {
        var badLink = new HeatTransferDefinition(
            "bad-link",
            "wall-a",
            "missing",
            ThermalConductance.FromWattsPerKelvin(1_000d));
        var badSource = new HeatSourceDefinition("bad-source", "missing", Power.FromMegawatts(10d));

        Assert.Throws<ArgumentException>(() => Plant(heatTransfers: [badLink]));
        Assert.Throws<ArgumentException>(() => Plant(heatSources: [badSource]));
    }

    [Fact]
    public void Definition_RequiresGloballyUniqueTopologyIdsIncludingWrappedHydraulicPaths()
    {
        var duplicateValve = new ValveDefinition(
            "valve-a",
            Pipe("passive-pipe", "node-a", "node-b"),
            ValveCharacteristic.Linear,
            ValveFailSafeAction.FailClosed);

        Assert.Throws<ArgumentException>(() => Plant(valves: [duplicateValve]));
    }

    [Fact]
    public void State_RequiresExactlyOneStateForEachStatefulDefinition()
    {
        var definition = Plant();

        Assert.Throws<ArgumentException>(() => new PlantState(
            definition,
            [FluidState(definition.GetFluidNode("node-a"))],
            [new ValveState("valve-a", ValvePosition.FullyOpen)],
            [new PumpState("pump-a", PumpSpeed.Rated)],
            [ThermalBodyState.FromTemperature(definition.GetThermalBody("wall-a"), Temperature.FromDegreesCelsius(300d))],
            [new HeatSourceState("heater-a")]));
    }

    [Fact]
    public void State_RejectsAStateBuiltFromANonCanonicalDefinition()
    {
        var definition = Plant();
        var replacementDefinition = new FluidNodeDefinition("node-a", Volume.FromCubicMetres(99d));
        var wrongState = FluidState(replacementDefinition);

        Assert.Throws<ArgumentException>(() => new PlantState(
            definition,
            [wrongState, FluidState(definition.GetFluidNode("node-b"))],
            [new ValveState("valve-a", ValvePosition.FullyOpen)],
            [new PumpState("pump-a", PumpSpeed.Rated)],
            [ThermalBodyState.FromTemperature(definition.GetThermalBody("wall-a"), Temperature.FromDegreesCelsius(300d))],
            [new HeatSourceState("heater-a")]));
    }

    [Fact]
    public void State_IsCanonicalCompleteAndIndependentFromCallerArrays()
    {
        var definition = Plant();
        var nodeStates = new[]
        {
            FluidState(definition.GetFluidNode("node-b")),
            FluidState(definition.GetFluidNode("node-a")),
        };
        var state = new PlantState(
            definition,
            nodeStates,
            [new ValveState("valve-a", ValvePosition.FromPercent(50d))],
            [new PumpState("pump-a", PumpSpeed.FromPercent(75d))],
            [ThermalBodyState.FromTemperature(definition.GetThermalBody("wall-a"), Temperature.FromDegreesCelsius(300d))],
            [new HeatSourceState("heater-a", false)]);

        nodeStates[0] = FluidState(definition.GetFluidNode("node-a"));

        Assert.Equal(new[] { "node-a", "node-b" }, state.FluidNodes.Select(static item => item.Id));
        Assert.Equal(50d, state.GetValve("valve-a").Position.Percent, 12);
        Assert.Equal(75d, state.GetPump("pump-a").Speed.Percent, 12);
        Assert.False(state.GetHeatSource("heater-a").IsEnabled);
    }

    internal static PlantDefinition Plant(
        FluidNodeDefinition[]? nodes = null,
        PipeDefinition[]? pipes = null,
        ValveDefinition[]? valves = null,
        PumpDefinition[]? pumps = null,
        ThermalBodyDefinition[]? thermalBodies = null,
        HeatTransferDefinition[]? heatTransfers = null,
        HeatSourceDefinition[]? heatSources = null)
    {
        var actualNodes = nodes ?? [FluidNode("node-b"), FluidNode("node-a")];
        var actualPipes = pipes ?? [Pipe("passive-pipe", "node-a", "node-b")];
        var actualValves = valves ??
        [
            new ValveDefinition(
                "valve-a",
                Pipe("valve-a-path", "node-a", "node-b"),
                ValveCharacteristic.Linear,
                ValveFailSafeAction.FailClosed),
        ];
        var actualPumps = pumps ??
        [
            new PumpDefinition(
                "pump-a",
                Pipe("pump-a-path", "node-a", "node-b"),
                PressureDifference.FromKilopascals(200d),
                Resistance(),
                PumpEfficiency.FromPercent(80d)),
        ];
        var actualThermalBodies = thermalBodies ??
        [
            new ThermalBodyDefinition("wall-a", HeatCapacity.FromJoulesPerKelvin(5_000_000d)),
        ];
        var actualHeatTransfers = heatTransfers ??
        [
            new HeatTransferDefinition(
                "heat-link",
                "wall-a",
                "node-b",
                ThermalConductance.FromWattsPerKelvin(2_000d)),
        ];
        var actualHeatSources = heatSources ??
        [
            new HeatSourceDefinition("heater-a", "wall-a", Power.FromMegawatts(10d)),
        ];

        return new PlantDefinition(
            "test-plant",
            actualNodes,
            actualPipes,
            actualValves,
            actualPumps,
            actualThermalBodies,
            actualHeatTransfers,
            actualHeatSources);
    }

    internal static FluidNodeState FluidState(FluidNodeDefinition definition)
        => new(
            definition,
            new FluidNodeInventory(Mass.FromKilograms(1_000d), Energy.FromMegajoules(500d)),
            new FluidThermodynamicState(
                Pressure.FromMegapascals(5d),
                Temperature.FromDegreesCelsius(250d)));

    private static FluidNodeDefinition FluidNode(string id)
        => new(id, Volume.FromCubicMetres(10d));

    private static PipeDefinition Pipe(string id, string from, string to)
        => new(id, from, to, Resistance());

    private static QuadraticHydraulicResistance Resistance()
        => QuadraticHydraulicResistance.FromPascalSecondsSquaredPerKilogramSquared(100_000d);
}
