using System.Collections.ObjectModel;
using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Domain.Physics.Reactor.Neutronics;

/// <summary>
/// Immutable point-kinetics state: normalized neutron population plus delayed-neutron precursor groups.
/// </summary>
public sealed class PointKineticsState
{
    public PointKineticsState(
        NeutronPopulation neutronPopulation,
        IEnumerable<DelayedNeutronGroupState> delayedNeutronGroups)
    {
        ArgumentNullException.ThrowIfNull(delayedNeutronGroups);

        var canonical = delayedNeutronGroups
            .Select(static group => group ?? throw new ArgumentException("Delayed-neutron group states cannot contain null entries.", "delayedNeutronGroups"))
            .OrderBy(static group => group.GroupId, StringComparer.Ordinal)
            .ToArray();

        if (canonical.Length == 0)
        {
            throw new ArgumentException("Point-kinetics state requires at least one delayed-neutron group.", nameof(delayedNeutronGroups));
        }

        if (canonical.Select(static group => group.GroupId).Distinct(StringComparer.Ordinal).Count() != canonical.Length)
        {
            throw new ArgumentException("Delayed-neutron group-state ids must be unique.", nameof(delayedNeutronGroups));
        }

        NeutronPopulation = neutronPopulation;
        DelayedNeutronGroups = new ReadOnlyCollection<DelayedNeutronGroupState>(canonical);
    }

    public NeutronPopulation NeutronPopulation { get; }

    public IReadOnlyList<DelayedNeutronGroupState> DelayedNeutronGroups { get; }

    public DelayedNeutronGroupState GetGroup(string id)
        => DelayedNeutronGroups.FirstOrDefault(group => string.Equals(group.GroupId, id, StringComparison.Ordinal))
            ?? throw new KeyNotFoundException($"Unknown delayed-neutron group state '{id}'.");

    public static PointKineticsState CreateCriticalEquilibrium(
        PointKineticsParameters parameters,
        NeutronPopulation neutronPopulation)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        var promptLifetimeSeconds = parameters.PromptNeutronGenerationTimeSeconds;
        var groups = parameters.DelayedNeutronGroups.Select(group =>
        {
            var concentration = group.Fraction.Fraction
                / (promptLifetimeSeconds * group.DecayConstant.PerSecond)
                * neutronPopulation.Relative;

            return new DelayedNeutronGroupState(
                group.Id,
                DelayedNeutronPrecursorPopulation.FromRelative(concentration));
        });

        return new PointKineticsState(neutronPopulation, groups);
    }
}
