using System.Collections.ObjectModel;
using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Thermal;

namespace NuclearReactorSimulator.Domain.Plant;

/// <summary>
/// Immutable, canonical topology definition for one composed plant.
/// M3.1 defines identity and connectivity only; no solver execution occurs here.
/// </summary>
public sealed class PlantDefinition
{
    private readonly Dictionary<string, int> _fluidNodeIndex;
    private readonly Dictionary<string, int> _pipeIndex;
    private readonly Dictionary<string, int> _valveIndex;
    private readonly Dictionary<string, int> _pumpIndex;
    private readonly Dictionary<string, int> _thermalBodyIndex;
    private readonly Dictionary<string, int> _heatTransferIndex;
    private readonly Dictionary<string, int> _heatSourceIndex;

    public PlantDefinition(
        string id,
        IEnumerable<FluidNodeDefinition> fluidNodes,
        IEnumerable<PipeDefinition> pipes,
        IEnumerable<ValveDefinition> valves,
        IEnumerable<PumpDefinition> pumps,
        IEnumerable<ThermalBodyDefinition> thermalBodies,
        IEnumerable<HeatTransferDefinition> heatTransfers,
        IEnumerable<HeatSourceDefinition> heatSources,
        HydraulicNumericalCouplingDefinition? hydraulicNumericalCoupling = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Plant id cannot be empty or whitespace.", nameof(id));
        }

        ArgumentNullException.ThrowIfNull(fluidNodes);
        ArgumentNullException.ThrowIfNull(pipes);
        ArgumentNullException.ThrowIfNull(valves);
        ArgumentNullException.ThrowIfNull(pumps);
        ArgumentNullException.ThrowIfNull(thermalBodies);
        ArgumentNullException.ThrowIfNull(heatTransfers);
        ArgumentNullException.ThrowIfNull(heatSources);

        var canonicalFluidNodes = Canonicalize(fluidNodes, static item => item.Id, nameof(fluidNodes));
        var canonicalPipes = Canonicalize(pipes, static item => item.Id, nameof(pipes));
        var canonicalValves = Canonicalize(valves, static item => item.Id, nameof(valves));
        var canonicalPumps = Canonicalize(pumps, static item => item.Id, nameof(pumps));
        var canonicalThermalBodies = Canonicalize(thermalBodies, static item => item.Id, nameof(thermalBodies));
        var canonicalHeatTransfers = Canonicalize(heatTransfers, static item => item.Id, nameof(heatTransfers));
        var canonicalHeatSources = Canonicalize(heatSources, static item => item.Id, nameof(heatSources));

        if (canonicalFluidNodes.Length == 0 && canonicalThermalBodies.Length == 0)
        {
            throw new ArgumentException("A plant must contain at least one fluid node or thermal body.", nameof(fluidNodes));
        }

        EnsureGloballyUniqueTopologyIds(
            canonicalFluidNodes,
            canonicalPipes,
            canonicalValves,
            canonicalPumps,
            canonicalThermalBodies,
            canonicalHeatTransfers,
            canonicalHeatSources);

        var fluidNodeIds = canonicalFluidNodes
            .Select(static item => item.Id)
            .ToHashSet(StringComparer.Ordinal);
        var thermalDomainIds = canonicalFluidNodes
            .Select(static item => item.Id)
            .Concat(canonicalThermalBodies.Select(static item => item.Id))
            .ToHashSet(StringComparer.Ordinal);

        foreach (var pipe in canonicalPipes)
        {
            ValidateHydraulicEndpoints(pipe.Id, pipe.FromNodeId, pipe.ToNodeId, fluidNodeIds);
        }

        foreach (var valve in canonicalValves)
        {
            ValidateHydraulicEndpoints(valve.Id, valve.Pipe.FromNodeId, valve.Pipe.ToNodeId, fluidNodeIds);
        }

        foreach (var pump in canonicalPumps)
        {
            ValidateHydraulicEndpoints(pump.Id, pump.Pipe.FromNodeId, pump.Pipe.ToNodeId, fluidNodeIds);
        }

        foreach (var heatTransfer in canonicalHeatTransfers)
        {
            ValidateThermalDomain(heatTransfer.Id, heatTransfer.FromDomainId, "from", thermalDomainIds);
            ValidateThermalDomain(heatTransfer.Id, heatTransfer.ToDomainId, "to", thermalDomainIds);
        }

        foreach (var heatSource in canonicalHeatSources)
        {
            ValidateThermalDomain(heatSource.Id, heatSource.TargetDomainId, "target", thermalDomainIds);
        }

        Id = id.Trim();
        FluidNodes = new ReadOnlyCollection<FluidNodeDefinition>(canonicalFluidNodes);
        Pipes = new ReadOnlyCollection<PipeDefinition>(canonicalPipes);
        Valves = new ReadOnlyCollection<ValveDefinition>(canonicalValves);
        Pumps = new ReadOnlyCollection<PumpDefinition>(canonicalPumps);
        ThermalBodies = new ReadOnlyCollection<ThermalBodyDefinition>(canonicalThermalBodies);
        HeatTransfers = new ReadOnlyCollection<HeatTransferDefinition>(canonicalHeatTransfers);
        HeatSources = new ReadOnlyCollection<HeatSourceDefinition>(canonicalHeatSources);
        _fluidNodeIndex = CreateIndex(canonicalFluidNodes, static item => item.Id);
        _pipeIndex = CreateIndex(canonicalPipes, static item => item.Id);
        _valveIndex = CreateIndex(canonicalValves, static item => item.Id);
        _pumpIndex = CreateIndex(canonicalPumps, static item => item.Id);
        _thermalBodyIndex = CreateIndex(canonicalThermalBodies, static item => item.Id);
        _heatTransferIndex = CreateIndex(canonicalHeatTransfers, static item => item.Id);
        _heatSourceIndex = CreateIndex(canonicalHeatSources, static item => item.Id);
        HydraulicNumericalCoupling = hydraulicNumericalCoupling ?? HydraulicNumericalCouplingDefinition.ExplicitCommittedState;
    }

    public string Id { get; }

    public IReadOnlyList<FluidNodeDefinition> FluidNodes { get; }

    public IReadOnlyList<PipeDefinition> Pipes { get; }

    public IReadOnlyList<ValveDefinition> Valves { get; }

    public IReadOnlyList<PumpDefinition> Pumps { get; }

    public IReadOnlyList<ThermalBodyDefinition> ThermalBodies { get; }

    public IReadOnlyList<HeatTransferDefinition> HeatTransfers { get; }

    public IReadOnlyList<HeatSourceDefinition> HeatSources { get; }

    public HydraulicNumericalCouplingDefinition HydraulicNumericalCoupling { get; }

    public FluidNodeDefinition GetFluidNode(string id) => FluidNodes[GetFluidNodeIndex(id)];

    public PipeDefinition GetPipe(string id) => Pipes[GetIndex(_pipeIndex, id, "pipe")];

    public ValveDefinition GetValve(string id) => Valves[GetValveIndex(id)];

    public PumpDefinition GetPump(string id) => Pumps[GetPumpIndex(id)];

    public ThermalBodyDefinition GetThermalBody(string id) => ThermalBodies[GetThermalBodyIndex(id)];

    public HeatTransferDefinition GetHeatTransfer(string id) => HeatTransfers[GetIndex(_heatTransferIndex, id, "heat transfer")];

    public HeatSourceDefinition GetHeatSource(string id) => HeatSources[GetHeatSourceIndex(id)];

    internal int GetFluidNodeIndex(string id) => GetIndex(_fluidNodeIndex, id, "fluid node");

    internal int GetValveIndex(string id) => GetIndex(_valveIndex, id, "valve");

    internal int GetPumpIndex(string id) => GetIndex(_pumpIndex, id, "pump");

    internal int GetThermalBodyIndex(string id) => GetIndex(_thermalBodyIndex, id, "thermal body");

    internal int GetHeatSourceIndex(string id) => GetIndex(_heatSourceIndex, id, "heat source");

    private static T[] Canonicalize<T>(IEnumerable<T> source, Func<T, string> idSelector, string parameterName)
        where T : class
    {
        var canonical = source
            .Select(item => item ?? throw new ArgumentException("Plant collections cannot contain null entries.", parameterName))
            .OrderBy(idSelector, StringComparer.Ordinal)
            .ToArray();

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in canonical)
        {
            var itemId = idSelector(item);
            if (!seen.Add(itemId))
            {
                throw new ArgumentException($"Duplicate id '{itemId}' in plant collection '{parameterName}'.", parameterName);
            }
        }

        return canonical;
    }

    private static void EnsureGloballyUniqueTopologyIds(
        IEnumerable<FluidNodeDefinition> fluidNodes,
        IEnumerable<PipeDefinition> pipes,
        IEnumerable<ValveDefinition> valves,
        IEnumerable<PumpDefinition> pumps,
        IEnumerable<ThermalBodyDefinition> thermalBodies,
        IEnumerable<HeatTransferDefinition> heatTransfers,
        IEnumerable<HeatSourceDefinition> heatSources)
    {
        var seen = new Dictionary<string, string>(StringComparer.Ordinal);

        void Add(string entityId, string label)
        {
            if (seen.TryGetValue(entityId, out var existingLabel))
            {
                throw new ArgumentException(
                    $"Plant topology id '{entityId}' is used by both {existingLabel} and {label}. Topology ids must be globally unique.");
            }

            seen.Add(entityId, label);
        }

        foreach (var item in fluidNodes)
        {
            Add(item.Id, $"fluid node '{item.Id}'");
        }

        foreach (var item in pipes)
        {
            Add(item.Id, $"pipe '{item.Id}'");
        }

        foreach (var item in valves)
        {
            Add(item.Id, $"valve '{item.Id}'");
            Add(item.Pipe.Id, $"valve '{item.Id}' hydraulic path '{item.Pipe.Id}'");
        }

        foreach (var item in pumps)
        {
            Add(item.Id, $"pump '{item.Id}'");
            Add(item.Pipe.Id, $"pump '{item.Id}' hydraulic path '{item.Pipe.Id}'");
        }

        foreach (var item in thermalBodies)
        {
            Add(item.Id, $"thermal body '{item.Id}'");
        }

        foreach (var item in heatTransfers)
        {
            Add(item.Id, $"heat transfer '{item.Id}'");
        }

        foreach (var item in heatSources)
        {
            Add(item.Id, $"heat source '{item.Id}'");
        }
    }

    private static void ValidateHydraulicEndpoints(
        string componentId,
        string fromNodeId,
        string toNodeId,
        IReadOnlySet<string> fluidNodeIds)
    {
        if (!fluidNodeIds.Contains(fromNodeId))
        {
            throw new ArgumentException($"Hydraulic component '{componentId}' references unknown from-node '{fromNodeId}'.");
        }

        if (!fluidNodeIds.Contains(toNodeId))
        {
            throw new ArgumentException($"Hydraulic component '{componentId}' references unknown to-node '{toNodeId}'.");
        }
    }

    private static void ValidateThermalDomain(
        string componentId,
        string domainId,
        string endpointLabel,
        IReadOnlySet<string> thermalDomainIds)
    {
        if (!thermalDomainIds.Contains(domainId))
        {
            throw new ArgumentException($"Thermal component '{componentId}' references unknown {endpointLabel} domain '{domainId}'.");
        }
    }

    private static Dictionary<string, int> CreateIndex<T>(IReadOnlyList<T> source, Func<T, string> idSelector)
        where T : class
    {
        var index = new Dictionary<string, int>(source.Count, StringComparer.Ordinal);
        for (var position = 0; position < source.Count; position++)
        {
            index.Add(idSelector(source[position]), position);
        }
        return index;
    }

    private static int GetIndex(IReadOnlyDictionary<string, int> index, string id, string label)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException($"A {label} id cannot be empty or whitespace.", nameof(id));
        }

        return index.TryGetValue(id, out var position)
            ? position
            : throw new KeyNotFoundException($"Unknown {label} '{id}'.");
    }
}
