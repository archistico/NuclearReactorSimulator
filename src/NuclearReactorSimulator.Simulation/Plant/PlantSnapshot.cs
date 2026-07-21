using System.Collections.ObjectModel;
using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Thermal;
using NuclearReactorSimulator.Domain.Plant;

namespace NuclearReactorSimulator.Simulation.Plant;

/// <summary>
/// Immutable plant-level snapshot boundary built from one committed <see cref="PlantState"/>.
/// M3.1 deliberately contains no solver diagnostics; those are added by later orchestration milestones.
/// </summary>
public sealed class PlantSnapshot
{
    public PlantSnapshot(PlantState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        Definition = state.Definition;
        PlantId = state.Id;
        FluidNodes = new ReadOnlyCollection<FluidNodeState>(state.FluidNodes.ToArray());
        Valves = new ReadOnlyCollection<ValveState>(state.Valves.ToArray());
        Pumps = new ReadOnlyCollection<PumpState>(state.Pumps.ToArray());
        ThermalBodies = new ReadOnlyCollection<ThermalBodyState>(state.ThermalBodies.ToArray());
        HeatSources = new ReadOnlyCollection<HeatSourceState>(state.HeatSources.ToArray());
    }

    public PlantDefinition Definition { get; }

    public string PlantId { get; }

    public IReadOnlyList<FluidNodeState> FluidNodes { get; }

    public IReadOnlyList<ValveState> Valves { get; }

    public IReadOnlyList<PumpState> Pumps { get; }

    public IReadOnlyList<ThermalBodyState> ThermalBodies { get; }

    public IReadOnlyList<HeatSourceState> HeatSources { get; }

    public FluidNodeState GetFluidNode(string id)
        => FluidNodes.FirstOrDefault(item => string.Equals(item.Id, id, StringComparison.Ordinal))
            ?? throw new KeyNotFoundException($"Unknown fluid node '{id}'.");

    public ValveState GetValve(string id)
        => Valves.FirstOrDefault(item => string.Equals(item.ValveId, id, StringComparison.Ordinal))
            ?? throw new KeyNotFoundException($"Unknown valve '{id}'.");

    public PumpState GetPump(string id)
        => Pumps.FirstOrDefault(item => string.Equals(item.PumpId, id, StringComparison.Ordinal))
            ?? throw new KeyNotFoundException($"Unknown pump '{id}'.");

    public ThermalBodyState GetThermalBody(string id)
        => ThermalBodies.FirstOrDefault(item => string.Equals(item.Id, id, StringComparison.Ordinal))
            ?? throw new KeyNotFoundException($"Unknown thermal body '{id}'.");

    public HeatSourceState GetHeatSource(string id)
        => HeatSources.FirstOrDefault(item => string.Equals(item.HeatSourceId, id, StringComparison.Ordinal))
            ?? throw new KeyNotFoundException($"Unknown heat source '{id}'.");
}
