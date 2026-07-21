using System.Collections.ObjectModel;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor.PrimaryCircuit.Boundaries;

namespace NuclearReactorSimulator.Simulation.Physics.Reactor.PrimaryCircuit.Boundaries;

public sealed class PrimaryCircuitBoundarySystemSnapshot
{
    public PrimaryCircuitBoundarySystemSnapshot(
        PrimaryCircuitBoundarySystemDefinition definition,
        IEnumerable<FeedwaterBoundarySnapshot> feedwaterBoundaries,
        IEnumerable<SteamExportBoundarySnapshot> steamExportBoundaries,
        MassFlowRate netExternalMassFlowRate,
        Power netExternalPower)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(feedwaterBoundaries);
        ArgumentNullException.ThrowIfNull(steamExportBoundaries);

        var canonicalFeedwater = feedwaterBoundaries
            .OrderBy(static item => item.BoundaryId, StringComparer.Ordinal)
            .ToArray();
        var canonicalSteamExport = steamExportBoundaries
            .OrderBy(static item => item.BoundaryId, StringComparer.Ordinal)
            .ToArray();

        ValidateExactSet(
            definition.FeedwaterBoundaries.Select(static item => item.Id),
            canonicalFeedwater.Select(static item => item.BoundaryId),
            "feedwater");
        ValidateExactSet(
            definition.SteamExportBoundaries.Select(static item => item.Id),
            canonicalSteamExport.Select(static item => item.BoundaryId),
            "steam-export");

        Definition = definition;
        FeedwaterBoundaries = new ReadOnlyCollection<FeedwaterBoundarySnapshot>(canonicalFeedwater);
        SteamExportBoundaries = new ReadOnlyCollection<SteamExportBoundarySnapshot>(canonicalSteamExport);
        NetExternalMassFlowRate = netExternalMassFlowRate;
        NetExternalPower = netExternalPower;
    }

    public PrimaryCircuitBoundarySystemDefinition Definition { get; }

    public IReadOnlyList<FeedwaterBoundarySnapshot> FeedwaterBoundaries { get; }

    public IReadOnlyList<SteamExportBoundarySnapshot> SteamExportBoundaries { get; }

    public MassFlowRate NetExternalMassFlowRate { get; }

    public Power NetExternalPower { get; }

    public FeedwaterBoundarySnapshot GetFeedwaterBoundary(string id)
        => FeedwaterBoundaries.FirstOrDefault(item => string.Equals(item.BoundaryId, id, StringComparison.Ordinal))
            ?? throw new KeyNotFoundException($"Unknown feedwater boundary snapshot '{id}'.");

    public SteamExportBoundarySnapshot GetSteamExportBoundary(string id)
        => SteamExportBoundaries.FirstOrDefault(item => string.Equals(item.BoundaryId, id, StringComparison.Ordinal))
            ?? throw new KeyNotFoundException($"Unknown steam-export boundary snapshot '{id}'.");

    private static void ValidateExactSet(IEnumerable<string> expectedIds, IEnumerable<string> actualIds, string label)
    {
        var expected = expectedIds.OrderBy(static id => id, StringComparer.Ordinal).ToArray();
        var actual = actualIds.OrderBy(static id => id, StringComparer.Ordinal).ToArray();
        if (!expected.SequenceEqual(actual, StringComparer.Ordinal))
        {
            throw new ArgumentException($"Primary-circuit boundary snapshot must contain exactly one snapshot per defined {label} boundary.");
        }
    }
}
