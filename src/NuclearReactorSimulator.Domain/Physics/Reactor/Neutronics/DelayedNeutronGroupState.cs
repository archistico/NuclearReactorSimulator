using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Domain.Physics.Reactor.Neutronics;

/// <summary>
/// Immutable precursor population for one delayed-neutron group.
/// </summary>
public sealed record DelayedNeutronGroupState
{
    public DelayedNeutronGroupState(
        string groupId,
        DelayedNeutronPrecursorPopulation precursorPopulation)
    {
        GroupId = string.IsNullOrWhiteSpace(groupId)
            ? throw new ArgumentException("Delayed-neutron group id is required.", nameof(groupId))
            : groupId.Trim();
        PrecursorPopulation = precursorPopulation;
    }

    public string GroupId { get; }

    public DelayedNeutronPrecursorPopulation PrecursorPopulation { get; }
}
