using System.Collections.ObjectModel;
using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Thermal;

namespace NuclearReactorSimulator.Domain.Plant;

/// <summary>
/// Immutable, complete state projection for all stateful components declared by a plant definition.
/// Passive topology remains in <see cref="PlantDefinition"/> and is not duplicated here.
/// </summary>
public sealed class PlantState
{
    public PlantState(
        PlantDefinition definition,
        IEnumerable<FluidNodeState> fluidNodes,
        IEnumerable<ValveState> valves,
        IEnumerable<PumpState> pumps,
        IEnumerable<ThermalBodyState> thermalBodies,
        IEnumerable<HeatSourceState> heatSources)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(fluidNodes);
        ArgumentNullException.ThrowIfNull(valves);
        ArgumentNullException.ThrowIfNull(pumps);
        ArgumentNullException.ThrowIfNull(thermalBodies);
        ArgumentNullException.ThrowIfNull(heatSources);

        var canonicalFluidNodes = Canonicalize(fluidNodes, static item => item.Id, nameof(fluidNodes));
        var canonicalValves = Canonicalize(valves, static item => item.ValveId, nameof(valves));
        var canonicalPumps = Canonicalize(pumps, static item => item.PumpId, nameof(pumps));
        var canonicalThermalBodies = Canonicalize(thermalBodies, static item => item.Id, nameof(thermalBodies));
        var canonicalHeatSources = Canonicalize(heatSources, static item => item.HeatSourceId, nameof(heatSources));

        ValidateExactStateSet(
            definition.FluidNodes.Select(static item => item.Id),
            canonicalFluidNodes.Select(static item => item.Id),
            "fluid node");
        ValidateExactStateSet(
            definition.Valves.Select(static item => item.Id),
            canonicalValves.Select(static item => item.ValveId),
            "valve");
        ValidateExactStateSet(
            definition.Pumps.Select(static item => item.Id),
            canonicalPumps.Select(static item => item.PumpId),
            "pump");
        ValidateExactStateSet(
            definition.ThermalBodies.Select(static item => item.Id),
            canonicalThermalBodies.Select(static item => item.Id),
            "thermal body");
        ValidateExactStateSet(
            definition.HeatSources.Select(static item => item.Id),
            canonicalHeatSources.Select(static item => item.HeatSourceId),
            "heat source");

        foreach (var state in canonicalFluidNodes)
        {
            if (state.Definition != definition.GetFluidNode(state.Id))
            {
                throw new ArgumentException($"Fluid-node state '{state.Id}' does not use the plant's canonical definition.", nameof(fluidNodes));
            }
        }

        foreach (var state in canonicalThermalBodies)
        {
            if (state.Definition != definition.GetThermalBody(state.Id))
            {
                throw new ArgumentException($"Thermal-body state '{state.Id}' does not use the plant's canonical definition.", nameof(thermalBodies));
            }
        }

        Definition = definition;
        FluidNodes = new ReadOnlyCollection<FluidNodeState>(canonicalFluidNodes);
        Valves = new ReadOnlyCollection<ValveState>(canonicalValves);
        Pumps = new ReadOnlyCollection<PumpState>(canonicalPumps);
        ThermalBodies = new ReadOnlyCollection<ThermalBodyState>(canonicalThermalBodies);
        HeatSources = new ReadOnlyCollection<HeatSourceState>(canonicalHeatSources);
    }

    public PlantDefinition Definition { get; }

    public string Id => Definition.Id;

    public IReadOnlyList<FluidNodeState> FluidNodes { get; }

    public IReadOnlyList<ValveState> Valves { get; }

    public IReadOnlyList<PumpState> Pumps { get; }

    public IReadOnlyList<ThermalBodyState> ThermalBodies { get; }

    public IReadOnlyList<HeatSourceState> HeatSources { get; }

    public FluidNodeState GetFluidNode(string id) => GetById(FluidNodes, id, static item => item.Id, "fluid node");

    public ValveState GetValve(string id) => GetById(Valves, id, static item => item.ValveId, "valve");

    public PumpState GetPump(string id) => GetById(Pumps, id, static item => item.PumpId, "pump");

    public ThermalBodyState GetThermalBody(string id) => GetById(ThermalBodies, id, static item => item.Id, "thermal body");

    public HeatSourceState GetHeatSource(string id) => GetById(HeatSources, id, static item => item.HeatSourceId, "heat source");

    private static T[] Canonicalize<T>(IEnumerable<T> source, Func<T, string> idSelector, string parameterName)
        where T : class
    {
        var canonical = source
            .Select(item => item ?? throw new ArgumentException("Plant-state collections cannot contain null entries.", parameterName))
            .OrderBy(idSelector, StringComparer.Ordinal)
            .ToArray();

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in canonical)
        {
            var itemId = idSelector(item);
            if (!seen.Add(itemId))
            {
                throw new ArgumentException($"Duplicate state id '{itemId}' in '{parameterName}'.", parameterName);
            }
        }

        return canonical;
    }

    private static void ValidateExactStateSet(
        IEnumerable<string> definitionIds,
        IEnumerable<string> stateIds,
        string label)
    {
        var expected = definitionIds.OrderBy(static id => id, StringComparer.Ordinal).ToArray();
        var actual = stateIds.OrderBy(static id => id, StringComparer.Ordinal).ToArray();

        if (!expected.SequenceEqual(actual, StringComparer.Ordinal))
        {
            throw new ArgumentException(
                $"Plant state must contain exactly one state for every defined {label}. Expected [{string.Join(", ", expected)}], actual [{string.Join(", ", actual)}].");
        }
    }

    private static T GetById<T>(
        IEnumerable<T> source,
        string id,
        Func<T, string> idSelector,
        string label)
        where T : class
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException($"A {label} id cannot be empty or whitespace.", nameof(id));
        }

        return source.FirstOrDefault(item => string.Equals(idSelector(item), id, StringComparison.Ordinal))
            ?? throw new KeyNotFoundException($"Unknown {label} '{id}'.");
    }
}
