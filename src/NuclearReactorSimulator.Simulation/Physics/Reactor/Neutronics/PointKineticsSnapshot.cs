using System.Collections.ObjectModel;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor.Neutronics;

namespace NuclearReactorSimulator.Simulation.Physics.Reactor.Neutronics;

/// <summary>
/// Immutable diagnostic projection of one point-kinetics state and the reactivity applied to it.
/// </summary>
public sealed class PointKineticsSnapshot
{
    internal PointKineticsSnapshot(
        PointKineticsState state,
        Reactivity reactivity,
        DelayedNeutronFraction effectiveDelayedNeutronFraction,
        double? logarithmicNeutronPopulationRatePerSecond,
        double? reactorPeriodSeconds)
    {
        ArgumentNullException.ThrowIfNull(state);

        State = state;
        Reactivity = reactivity;
        EffectiveDelayedNeutronFraction = effectiveDelayedNeutronFraction;
        PromptCriticalMargin = global::NuclearReactorSimulator.Domain.Physics.Quantities.Reactivity.FromDeltaKOverK(
            reactivity.DeltaKOverK - effectiveDelayedNeutronFraction.Fraction);
        ReactivityDollars = reactivity.DeltaKOverK / effectiveDelayedNeutronFraction.Fraction;
        ReactivityCents = ReactivityDollars * 100d;
        LogarithmicNeutronPopulationRatePerSecond = logarithmicNeutronPopulationRatePerSecond;
        ReactorPeriodSeconds = reactorPeriodSeconds;
        DelayedNeutronGroups = new ReadOnlyCollection<DelayedNeutronGroupState>(state.DelayedNeutronGroups.ToArray());
    }

    public PointKineticsState State { get; }

    public NeutronPopulation NeutronPopulation => State.NeutronPopulation;

    public IReadOnlyList<DelayedNeutronGroupState> DelayedNeutronGroups { get; }

    public Reactivity Reactivity { get; }

    public DelayedNeutronFraction EffectiveDelayedNeutronFraction { get; }

    public Reactivity PromptCriticalMargin { get; }

    public bool IsPromptCritical => PromptCriticalMargin >= global::NuclearReactorSimulator.Domain.Physics.Quantities.Reactivity.Zero;

    /// <summary>
    /// Reactivity expressed relative to beta-effective for this parameter set.
    /// This is a parameter-set-relative diagnostic, not an intrinsic unit stored by <see cref="Reactivity"/>.
    /// </summary>
    public double ReactivityDollars { get; }

    public double ReactivityCents { get; }

    /// <summary>
    /// Instantaneous d(ln n)/dt diagnostic. Null when neutron population is zero.
    /// </summary>
    public double? LogarithmicNeutronPopulationRatePerSecond { get; }

    /// <summary>
    /// Signed instantaneous reactor period in seconds. Positive means growth, negative means decay.
    /// Null represents an effectively infinite/undefined period at zero logarithmic rate or zero population.
    /// </summary>
    public double? ReactorPeriodSeconds { get; }
}
