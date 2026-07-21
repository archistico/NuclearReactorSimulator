using System.Collections.ObjectModel;
using NuclearReactorSimulator.Domain.Physics.Reactor.PrimaryCircuit.Boundaries;

namespace NuclearReactorSimulator.Simulation.Physics.Reactor.PrimaryCircuit.Boundaries;

/// <summary>
/// Canonical complete set of per-step inputs for all temporary M3 primary-circuit boundaries.
/// </summary>
public sealed class PrimaryCircuitBoundaryInputs
{
    public PrimaryCircuitBoundaryInputs(
        PrimaryCircuitBoundarySystemDefinition definition,
        IEnumerable<FeedwaterBoundaryInput> feedwaterInputs,
        IEnumerable<SteamExportBoundaryInput> steamExportInputs)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(feedwaterInputs);
        ArgumentNullException.ThrowIfNull(steamExportInputs);

        var canonicalFeedwater = Canonicalize(feedwaterInputs, static item => item.BoundaryId, nameof(feedwaterInputs));
        var canonicalSteamExport = Canonicalize(steamExportInputs, static item => item.BoundaryId, nameof(steamExportInputs));

        ValidateExactSet(
            definition.FeedwaterBoundaries.Select(static item => item.Id),
            canonicalFeedwater.Select(static item => item.BoundaryId),
            "feedwater boundary");
        ValidateExactSet(
            definition.SteamExportBoundaries.Select(static item => item.Id),
            canonicalSteamExport.Select(static item => item.BoundaryId),
            "steam-export boundary");

        Definition = definition;
        FeedwaterInputs = new ReadOnlyCollection<FeedwaterBoundaryInput>(canonicalFeedwater);
        SteamExportInputs = new ReadOnlyCollection<SteamExportBoundaryInput>(canonicalSteamExport);
    }

    public PrimaryCircuitBoundarySystemDefinition Definition { get; }

    public IReadOnlyList<FeedwaterBoundaryInput> FeedwaterInputs { get; }

    public IReadOnlyList<SteamExportBoundaryInput> SteamExportInputs { get; }

    public FeedwaterBoundaryInput GetFeedwaterInput(string boundaryId)
        => GetById(FeedwaterInputs, boundaryId, static item => item.BoundaryId, "feedwater boundary input");

    public SteamExportBoundaryInput GetSteamExportInput(string boundaryId)
        => GetById(SteamExportInputs, boundaryId, static item => item.BoundaryId, "steam-export boundary input");

    private static T[] Canonicalize<T>(IEnumerable<T> source, Func<T, string> idSelector, string parameterName)
        where T : class
    {
        var canonical = source
            .Select(item => item ?? throw new ArgumentException("Boundary input collections cannot contain null entries.", parameterName))
            .OrderBy(idSelector, StringComparer.Ordinal)
            .ToArray();

        if (canonical.Select(idSelector).Distinct(StringComparer.Ordinal).Count() != canonical.Length)
        {
            throw new ArgumentException($"Boundary input ids in '{parameterName}' must be unique.", parameterName);
        }

        return canonical;
    }

    private static void ValidateExactSet(IEnumerable<string> expectedIds, IEnumerable<string> actualIds, string label)
    {
        var expected = expectedIds.OrderBy(static id => id, StringComparer.Ordinal).ToArray();
        var actual = actualIds.OrderBy(static id => id, StringComparer.Ordinal).ToArray();
        if (!expected.SequenceEqual(actual, StringComparer.Ordinal))
        {
            throw new ArgumentException(
                $"Boundary inputs must contain exactly one input for every defined {label}. Expected [{string.Join(", ", expected)}], actual [{string.Join(", ", actual)}].");
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
