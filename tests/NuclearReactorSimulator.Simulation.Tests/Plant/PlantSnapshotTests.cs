using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Thermal;
using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Plant;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Plant;

public sealed class PlantSnapshotTests
{
    [Fact]
    public void Snapshot_CopiesOneCommittedPlantStateInCanonicalOrder()
    {
        var definition = Definition();
        var state = new PlantState(
            definition,
            [FluidState(definition.GetFluidNode("node-b")), FluidState(definition.GetFluidNode("node-a"))],
            [new ValveState("valve-a", ValvePosition.FromPercent(40d))],
            [new PumpState("pump-a", PumpSpeed.FromPercent(60d))],
            [ThermalBodyState.FromTemperature(definition.GetThermalBody("wall-a"), Temperature.FromDegreesCelsius(300d))],
            [new HeatSourceState("heater-a", true)]);

        var snapshot = new PlantSnapshot(state);

        Assert.Same(definition, snapshot.Definition);
        Assert.Equal("test-plant", snapshot.PlantId);
        Assert.Equal(new[] { "node-a", "node-b" }, snapshot.FluidNodes.Select(static item => item.Id));
        Assert.Equal(40d, snapshot.GetValve("valve-a").Position.Percent, 12);
        Assert.Equal(60d, snapshot.GetPump("pump-a").Speed.Percent, 12);
        Assert.Equal(300d, snapshot.GetThermalBody("wall-a").Temperature.DegreesCelsius, 9);
        Assert.True(snapshot.GetHeatSource("heater-a").IsEnabled);
    }

    [Fact]
    public void Snapshot_RejectsUnknownLookupWithoutFallingBackToAnotherComponentKind()
    {
        var snapshot = new PlantSnapshot(State());

        Assert.Throws<KeyNotFoundException>(() => snapshot.GetFluidNode("wall-a"));
        Assert.Throws<KeyNotFoundException>(() => snapshot.GetPump("valve-a"));
    }

    private static PlantState State()
    {
        var definition = Definition();
        return new PlantState(
            definition,
            [FluidState(definition.GetFluidNode("node-a")), FluidState(definition.GetFluidNode("node-b"))],
            [new ValveState("valve-a", ValvePosition.FullyOpen)],
            [new PumpState("pump-a", PumpSpeed.Rated)],
            [ThermalBodyState.FromTemperature(definition.GetThermalBody("wall-a"), Temperature.FromDegreesCelsius(300d))],
            [new HeatSourceState("heater-a")]);
    }

    private static PlantDefinition Definition()
        => new(
            "test-plant",
            [Node("node-b"), Node("node-a")],
            [Pipe("passive-pipe")],
            [new ValveDefinition("valve-a", Pipe("valve-a-path"), ValveCharacteristic.Linear, ValveFailSafeAction.FailClosed)],
            [new PumpDefinition(
                "pump-a",
                Pipe("pump-a-path"),
                PressureDifference.FromKilopascals(200d),
                Resistance(),
                PumpEfficiency.FromPercent(80d))],
            [new ThermalBodyDefinition("wall-a", HeatCapacity.FromJoulesPerKelvin(5_000_000d))],
            [new HeatTransferDefinition("heat-link", "wall-a", "node-b", ThermalConductance.FromWattsPerKelvin(2_000d))],
            [new HeatSourceDefinition("heater-a", "wall-a", Power.FromMegawatts(10d))]);

    private static FluidNodeDefinition Node(string id) => new(id, Volume.FromCubicMetres(10d));

    private static PipeDefinition Pipe(string id) => new(id, "node-a", "node-b", Resistance());

    private static QuadraticHydraulicResistance Resistance()
        => QuadraticHydraulicResistance.FromPascalSecondsSquaredPerKilogramSquared(100_000d);

    private static FluidNodeState FluidState(FluidNodeDefinition definition)
        => new(
            definition,
            new FluidNodeInventory(Mass.FromKilograms(1_000d), Energy.FromMegajoules(500d)),
            new FluidThermodynamicState(Pressure.FromMegapascals(5d), Temperature.FromDegreesCelsius(250d)));
}
