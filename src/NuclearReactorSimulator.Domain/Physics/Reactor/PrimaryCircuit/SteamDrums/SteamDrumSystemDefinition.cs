using System.Collections.ObjectModel;
using NuclearReactorSimulator.Domain.Physics.Reactor.PrimaryCircuit.Circulation;

namespace NuclearReactorSimulator.Domain.Physics.Reactor.PrimaryCircuit.SteamDrums;

/// <summary>
/// Canonical semantic composition of steam drums around an existing main-circulation system.
/// Exactly one drum is assigned to each circulation loop in M3.6.
/// </summary>
public sealed class SteamDrumSystemDefinition
{
    public SteamDrumSystemDefinition(
        string id,
        MainCirculationSystemDefinition mainCirculationSystem,
        IEnumerable<SteamDrumDefinition> drums)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Steam-drum system id cannot be empty or whitespace.", nameof(id));
        }

        ArgumentNullException.ThrowIfNull(mainCirculationSystem);
        ArgumentNullException.ThrowIfNull(drums);

        var canonicalDrums = drums
            .Select(drum => drum ?? throw new ArgumentException("Steam-drum collection cannot contain null entries.", nameof(drums)))
            .OrderBy(static drum => drum.Id, StringComparer.Ordinal)
            .ToArray();
        if (canonicalDrums.Length == 0)
        {
            throw new ArgumentException("A steam-drum system must contain at least one drum.", nameof(drums));
        }

        EnsureUnique(canonicalDrums.Select(static drum => drum.Id), "Steam-drum ids", nameof(drums));
        EnsureUnique(canonicalDrums.Select(static drum => drum.MainCirculationLoopId), "Main-circulation loop assignments", nameof(drums));
        EnsureUnique(canonicalDrums.Select(static drum => drum.InventoryNodeId), "Steam-drum inventory nodes", nameof(drums));
        EnsureUnique(canonicalDrums.Select(static drum => drum.SteamOutletNodeId), "Steam-outlet nodes", nameof(drums));

        var plant = mainCirculationSystem.ChannelGroups.CoreDefinition.PlantDefinition;
        foreach (var drum in canonicalDrums)
        {
            var loop = mainCirculationSystem.GetLoop(drum.MainCirculationLoopId);
            _ = plant.GetFluidNode(drum.InventoryNodeId);
            _ = plant.GetFluidNode(drum.SteamOutletNodeId);

            if (!string.Equals(loop.ReturnCollectorNodeId, drum.InventoryNodeId, StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    $"Steam drum '{drum.Id}' inventory node '{drum.InventoryNodeId}' must be the return collector for loop '{loop.Id}'.",
                    nameof(drums));
            }

            if (string.Equals(drum.InventoryNodeId, loop.SuctionHeaderNodeId, StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    $"Steam drum '{drum.Id}' must use a dedicated inventory node distinct from loop '{loop.Id}' suction header.",
                    nameof(drums));
            }

            if (string.Equals(drum.SteamOutletNodeId, loop.SuctionHeaderNodeId, StringComparison.Ordinal)
                || string.Equals(drum.SteamOutletNodeId, loop.PressureHeaderNodeId, StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    $"Steam drum '{drum.Id}' steam-outlet node must be distinct from the circulation headers of loop '{loop.Id}'.",
                    nameof(drums));
            }
        }

        var expectedLoopIds = mainCirculationSystem.Loops.Select(static loop => loop.Id).ToHashSet(StringComparer.Ordinal);
        var assignedLoopIds = canonicalDrums.Select(static drum => drum.MainCirculationLoopId).ToHashSet(StringComparer.Ordinal);
        if (!expectedLoopIds.SetEquals(assignedLoopIds))
        {
            var missing = expectedLoopIds.Except(assignedLoopIds, StringComparer.Ordinal).OrderBy(static idValue => idValue, StringComparer.Ordinal);
            throw new ArgumentException(
                $"Every main-circulation loop must have exactly one steam drum in M3.6. Missing: {string.Join(", ", missing)}.",
                nameof(drums));
        }

        Id = id.Trim();
        MainCirculationSystem = mainCirculationSystem;
        Drums = new ReadOnlyCollection<SteamDrumDefinition>(canonicalDrums);
    }

    public string Id { get; }

    public MainCirculationSystemDefinition MainCirculationSystem { get; }

    public IReadOnlyList<SteamDrumDefinition> Drums { get; }

    public SteamDrumDefinition GetDrum(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Steam-drum id cannot be empty or whitespace.", nameof(id));
        }

        return Drums.FirstOrDefault(drum => string.Equals(drum.Id, id, StringComparison.Ordinal))
            ?? throw new KeyNotFoundException($"Unknown steam drum '{id}'.");
    }

    private static void EnsureUnique(IEnumerable<string> values, string label, string parameterName)
    {
        var array = values.ToArray();
        if (array.Distinct(StringComparer.Ordinal).Count() != array.Length)
        {
            throw new ArgumentException($"{label} must be unique.", parameterName);
        }
    }
}
