using System.Collections.ObjectModel;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor.Core;
using NuclearReactorSimulator.Domain.Physics.Reactor.Core.Spatial;

namespace NuclearReactorSimulator.Simulation.Physics.Reactor.Core.Spatial;

/// <summary>
/// Immutable M9.4 quasi-spatial refinement snapshot. The total feedback is one scalar contribution for the existing global
/// point-kinetics seam; candidate core state is only the next-step power-shape projection.
/// </summary>
public sealed class QuasiSpatialCoreFeedbackSnapshot
{
    public QuasiSpatialCoreFeedbackSnapshot(
        QuasiSpatialCoreFeedbackDefinition definition,
        AggregatedCoreState committedCoreState,
        AggregatedCoreState candidateCoreState,
        Reactivity powerWeightedFeedbackReactivity,
        IEnumerable<QuasiSpatialCoreZoneSnapshot> zones)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        CommittedCoreState = committedCoreState ?? throw new ArgumentNullException(nameof(committedCoreState));
        CandidateCoreState = candidateCoreState ?? throw new ArgumentNullException(nameof(candidateCoreState));
        PowerWeightedFeedbackReactivity = powerWeightedFeedbackReactivity;
        Zones = new ReadOnlyCollection<QuasiSpatialCoreZoneSnapshot>(
            (zones ?? throw new ArgumentNullException(nameof(zones)))
                .OrderBy(static item => item.ZoneId, StringComparer.Ordinal)
                .ToArray());
    }

    public QuasiSpatialCoreFeedbackDefinition Definition { get; }

    public AggregatedCoreState CommittedCoreState { get; }

    public AggregatedCoreState CandidateCoreState { get; }

    public Reactivity PowerWeightedFeedbackReactivity { get; }

    public IReadOnlyList<QuasiSpatialCoreZoneSnapshot> Zones { get; }

    public QuasiSpatialCoreZoneSnapshot GetZone(string zoneId)
        => Zones.FirstOrDefault(item => string.Equals(item.ZoneId, zoneId, StringComparison.Ordinal))
            ?? throw new KeyNotFoundException($"Unknown quasi-spatial core zone '{zoneId}'.");
}
