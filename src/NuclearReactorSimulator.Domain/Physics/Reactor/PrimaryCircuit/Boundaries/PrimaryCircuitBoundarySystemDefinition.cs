using System.Collections.ObjectModel;
using NuclearReactorSimulator.Domain.Physics.Reactor.PrimaryCircuit.SteamDrums;

namespace NuclearReactorSimulator.Domain.Physics.Reactor.PrimaryCircuit.Boundaries;

/// <summary>
/// Canonical semantic composition of the temporary M3 feedwater and steam-export plant boundaries.
/// Exactly one feedwater source and one steam-export sink are associated with every steam drum.
/// </summary>
public sealed class PrimaryCircuitBoundarySystemDefinition
{
    public PrimaryCircuitBoundarySystemDefinition(
        string id,
        SteamDrumSystemDefinition steamDrumSystem,
        IEnumerable<FeedwaterBoundaryDefinition> feedwaterBoundaries,
        IEnumerable<SteamExportBoundaryDefinition> steamExportBoundaries)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Primary-circuit boundary-system id cannot be empty or whitespace.", nameof(id));
        }

        ArgumentNullException.ThrowIfNull(steamDrumSystem);
        ArgumentNullException.ThrowIfNull(feedwaterBoundaries);
        ArgumentNullException.ThrowIfNull(steamExportBoundaries);

        var canonicalFeedwater = Canonicalize(feedwaterBoundaries, static item => item.Id, nameof(feedwaterBoundaries));
        var canonicalSteamExport = Canonicalize(steamExportBoundaries, static item => item.Id, nameof(steamExportBoundaries));

        if (canonicalFeedwater.Length == 0)
        {
            throw new ArgumentException("A primary-circuit boundary system must contain at least one feedwater boundary.", nameof(feedwaterBoundaries));
        }

        if (canonicalSteamExport.Length == 0)
        {
            throw new ArgumentException("A primary-circuit boundary system must contain at least one steam-export boundary.", nameof(steamExportBoundaries));
        }

        EnsureGloballyUniqueBoundaryIds(canonicalFeedwater, canonicalSteamExport);

        var plant = steamDrumSystem.MainCirculationSystem.ChannelGroups.CoreDefinition.PlantDefinition;
        foreach (var boundary in canonicalFeedwater)
        {
            var drum = steamDrumSystem.GetDrum(boundary.SteamDrumId);
            _ = plant.GetFluidNode(boundary.TargetNodeId);

            if (!string.Equals(boundary.TargetNodeId, drum.InventoryNodeId, StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    $"Feedwater boundary '{boundary.Id}' must target steam drum '{drum.Id}' inventory node '{drum.InventoryNodeId}'.",
                    nameof(feedwaterBoundaries));
            }
        }

        foreach (var boundary in canonicalSteamExport)
        {
            var drum = steamDrumSystem.GetDrum(boundary.SteamDrumId);
            _ = plant.GetFluidNode(boundary.SourceNodeId);

            if (!string.Equals(boundary.SourceNodeId, drum.SteamOutletNodeId, StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    $"Steam-export boundary '{boundary.Id}' must source steam drum '{drum.Id}' outlet node '{drum.SteamOutletNodeId}'.",
                    nameof(steamExportBoundaries));
            }
        }

        ValidateExactlyOnePerDrum(
            steamDrumSystem,
            canonicalFeedwater.Select(static item => item.SteamDrumId),
            "feedwater boundary",
            nameof(feedwaterBoundaries));
        ValidateExactlyOnePerDrum(
            steamDrumSystem,
            canonicalSteamExport.Select(static item => item.SteamDrumId),
            "steam-export boundary",
            nameof(steamExportBoundaries));

        Id = id.Trim();
        SteamDrumSystem = steamDrumSystem;
        FeedwaterBoundaries = new ReadOnlyCollection<FeedwaterBoundaryDefinition>(canonicalFeedwater);
        SteamExportBoundaries = new ReadOnlyCollection<SteamExportBoundaryDefinition>(canonicalSteamExport);
    }

    public string Id { get; }

    public SteamDrumSystemDefinition SteamDrumSystem { get; }

    public IReadOnlyList<FeedwaterBoundaryDefinition> FeedwaterBoundaries { get; }

    public IReadOnlyList<SteamExportBoundaryDefinition> SteamExportBoundaries { get; }

    public FeedwaterBoundaryDefinition GetFeedwaterBoundary(string id)
        => GetById(FeedwaterBoundaries, id, static item => item.Id, "feedwater boundary");

    public SteamExportBoundaryDefinition GetSteamExportBoundary(string id)
        => GetById(SteamExportBoundaries, id, static item => item.Id, "steam-export boundary");

    private static T[] Canonicalize<T>(IEnumerable<T> source, Func<T, string> idSelector, string parameterName)
        where T : class
    {
        var canonical = source
            .Select(item => item ?? throw new ArgumentException("Boundary collections cannot contain null entries.", parameterName))
            .OrderBy(idSelector, StringComparer.Ordinal)
            .ToArray();

        if (canonical.Select(idSelector).Distinct(StringComparer.Ordinal).Count() != canonical.Length)
        {
            throw new ArgumentException($"Boundary ids in '{parameterName}' must be unique.", parameterName);
        }

        return canonical;
    }

    private static void EnsureGloballyUniqueBoundaryIds(
        IEnumerable<FeedwaterBoundaryDefinition> feedwater,
        IEnumerable<SteamExportBoundaryDefinition> steamExport)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var id in feedwater.Select(static item => item.Id).Concat(steamExport.Select(static item => item.Id)))
        {
            if (!ids.Add(id))
            {
                throw new ArgumentException($"Boundary id '{id}' is used by more than one primary-circuit boundary.");
            }
        }
    }

    private static void ValidateExactlyOnePerDrum(
        SteamDrumSystemDefinition steamDrumSystem,
        IEnumerable<string> assignedDrumIds,
        string label,
        string parameterName)
    {
        var assignments = assignedDrumIds.ToArray();
        var duplicates = assignments
            .GroupBy(static id => id, StringComparer.Ordinal)
            .Where(static group => group.Count() != 1)
            .Select(static group => group.Key)
            .OrderBy(static id => id, StringComparer.Ordinal)
            .ToArray();
        if (duplicates.Length > 0)
        {
            throw new ArgumentException($"Each steam drum must have exactly one {label}. Duplicate assignments: {string.Join(", ", duplicates)}.", parameterName);
        }

        var expected = steamDrumSystem.Drums.Select(static drum => drum.Id).ToHashSet(StringComparer.Ordinal);
        var actual = assignments.ToHashSet(StringComparer.Ordinal);
        if (!expected.SetEquals(actual))
        {
            var missing = expected.Except(actual, StringComparer.Ordinal).OrderBy(static id => id, StringComparer.Ordinal);
            throw new ArgumentException($"Each steam drum must have exactly one {label}. Missing: {string.Join(", ", missing)}.", parameterName);
        }
    }

    private static T GetById<T>(IEnumerable<T> source, string id, Func<T, string> idSelector, string label)
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
