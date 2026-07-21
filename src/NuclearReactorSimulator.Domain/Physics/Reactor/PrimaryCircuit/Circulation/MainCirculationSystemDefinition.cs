using System.Collections.ObjectModel;
using NuclearReactorSimulator.Domain.Physics.Reactor.Core.Channels;

namespace NuclearReactorSimulator.Domain.Physics.Reactor.PrimaryCircuit.Circulation;

/// <summary>
/// Canonical semantic composition of the main-circulation loops around an existing plant topology.
/// No pump, pipe, node or fuel-channel state is duplicated here.
/// </summary>
public sealed class MainCirculationSystemDefinition
{
    public MainCirculationSystemDefinition(
        string id,
        FuelChannelGroupSetDefinition channelGroups,
        IEnumerable<MainCirculationLoopDefinition> loops)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Main-circulation system id cannot be empty or whitespace.", nameof(id));
        }

        ArgumentNullException.ThrowIfNull(channelGroups);
        ArgumentNullException.ThrowIfNull(loops);

        var canonicalLoops = loops
            .Select(loop => loop ?? throw new ArgumentException("Main-circulation loop collection cannot contain null entries.", nameof(loops)))
            .OrderBy(static loop => loop.Id, StringComparer.Ordinal)
            .ToArray();
        if (canonicalLoops.Length == 0)
        {
            throw new ArgumentException("A main-circulation system must contain at least one loop.", nameof(loops));
        }

        if (canonicalLoops.Select(static loop => loop.Id).Distinct(StringComparer.Ordinal).Count() != canonicalLoops.Length)
        {
            throw new ArgumentException("Main-circulation loop ids must be unique.", nameof(loops));
        }

        var plant = channelGroups.CoreDefinition.PlantDefinition;
        var assignedPumps = new HashSet<string>(StringComparer.Ordinal);
        var assignedGroups = new HashSet<string>(StringComparer.Ordinal);
        var assignedReturnPipes = new HashSet<string>(StringComparer.Ordinal);

        foreach (var loop in canonicalLoops)
        {
            _ = plant.GetFluidNode(loop.SuctionHeaderNodeId);
            _ = plant.GetFluidNode(loop.PressureHeaderNodeId);
            _ = plant.GetFluidNode(loop.ReturnCollectorNodeId);

            foreach (var pumpId in loop.PumpIds)
            {
                if (!assignedPumps.Add(pumpId))
                {
                    throw new ArgumentException($"Pump '{pumpId}' is assigned to more than one main-circulation loop.", nameof(loops));
                }

                var pump = plant.GetPump(pumpId);
                if (!string.Equals(pump.Pipe.FromNodeId, loop.SuctionHeaderNodeId, StringComparison.Ordinal)
                    || !string.Equals(pump.Pipe.ToNodeId, loop.PressureHeaderNodeId, StringComparison.Ordinal))
                {
                    throw new ArgumentException(
                        $"Pump '{pumpId}' in loop '{loop.Id}' must run from suction header '{loop.SuctionHeaderNodeId}' to pressure header '{loop.PressureHeaderNodeId}'.",
                        nameof(loops));
                }
            }

            foreach (var branch in loop.Branches)
            {
                if (!assignedGroups.Add(branch.FuelChannelGroupId))
                {
                    throw new ArgumentException(
                        $"Fuel-channel group '{branch.FuelChannelGroupId}' is assigned to more than one main-circulation loop.",
                        nameof(loops));
                }

                if (!assignedReturnPipes.Add(branch.ReturnPipeId))
                {
                    throw new ArgumentException(
                        $"Return pipe '{branch.ReturnPipeId}' is assigned to more than one main-circulation branch.",
                        nameof(loops));
                }

                var group = channelGroups.GetGroup(branch.FuelChannelGroupId);
                if (!string.Equals(group.InletCoolantNodeId, loop.PressureHeaderNodeId, StringComparison.Ordinal))
                {
                    throw new ArgumentException(
                        $"Fuel-channel group '{group.Id}' in loop '{loop.Id}' must take inlet coolant from pressure header '{loop.PressureHeaderNodeId}'.",
                        nameof(loops));
                }

                var returnPipe = plant.GetPipe(branch.ReturnPipeId);
                if (!string.Equals(returnPipe.FromNodeId, group.OutletCoolantNodeId, StringComparison.Ordinal)
                    || !string.Equals(returnPipe.ToNodeId, loop.ReturnCollectorNodeId, StringComparison.Ordinal))
                {
                    throw new ArgumentException(
                        $"Return pipe '{returnPipe.Id}' for fuel-channel group '{group.Id}' must run from outlet '{group.OutletCoolantNodeId}' to return collector '{loop.ReturnCollectorNodeId}'.",
                        nameof(loops));
                }

                if (string.Equals(group.HydraulicPipeId, branch.ReturnPipeId, StringComparison.Ordinal))
                {
                    throw new ArgumentException(
                        $"Fuel-channel group '{group.Id}' cannot reuse its active channel path '{group.HydraulicPipeId}' as return pipe.",
                        nameof(loops));
                }
            }
        }

        var expectedGroupIds = channelGroups.Groups.Select(static group => group.Id).ToHashSet(StringComparer.Ordinal);
        if (!expectedGroupIds.SetEquals(assignedGroups))
        {
            var missing = expectedGroupIds.Except(assignedGroups, StringComparer.Ordinal).OrderBy(static value => value, StringComparer.Ordinal);
            throw new ArgumentException(
                $"Every fuel-channel group must belong to exactly one main-circulation loop. Missing: {string.Join(", ", missing)}.",
                nameof(loops));
        }

        Id = id.Trim();
        ChannelGroups = channelGroups;
        Loops = new ReadOnlyCollection<MainCirculationLoopDefinition>(canonicalLoops);
    }

    public string Id { get; }

    public FuelChannelGroupSetDefinition ChannelGroups { get; }

    public IReadOnlyList<MainCirculationLoopDefinition> Loops { get; }

    public MainCirculationLoopDefinition GetLoop(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Main-circulation loop id cannot be empty or whitespace.", nameof(id));
        }

        return Loops.FirstOrDefault(loop => string.Equals(loop.Id, id, StringComparison.Ordinal))
            ?? throw new KeyNotFoundException($"Unknown main-circulation loop '{id}'.");
    }
}
