using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Simulation.Physics.Reactor.DecayHeat;

/// <summary>
/// Immutable diagnostic snapshot for one equivalent decay-heat group.
/// </summary>
public sealed record DecayHeatGroupSnapshot(
    string GroupId,
    Energy StoredDecayEnergy,
    Power InstantaneousDecayHeatPower);
