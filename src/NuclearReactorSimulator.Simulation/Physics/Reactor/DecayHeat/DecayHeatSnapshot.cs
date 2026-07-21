using System.Collections.ObjectModel;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor.DecayHeat;

namespace NuclearReactorSimulator.Simulation.Physics.Reactor.DecayHeat;

/// <summary>
/// Immutable end-of-step diagnostic view of the current latent decay-energy inventory and heat rate.
/// </summary>
public sealed record DecayHeatSnapshot
{
    public DecayHeatSnapshot(
        string definitionId,
        DecayHeatState state,
        Power totalInstantaneousDecayHeatPower,
        IEnumerable<DecayHeatGroupSnapshot> groups,
        IEnumerable<DecayHeatDeposition> heatDepositions)
    {
        if (string.IsNullOrWhiteSpace(definitionId))
        {
            throw new ArgumentException("Decay-heat definition id cannot be empty.", nameof(definitionId));
        }

        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(groups);
        ArgumentNullException.ThrowIfNull(heatDepositions);

        if (totalInstantaneousDecayHeatPower < Power.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(totalInstantaneousDecayHeatPower),
                totalInstantaneousDecayHeatPower,
                "Instantaneous decay-heat power cannot be negative.");
        }

        DefinitionId = definitionId;
        State = state;
        TotalInstantaneousDecayHeatPower = totalInstantaneousDecayHeatPower;
        Groups = new ReadOnlyCollection<DecayHeatGroupSnapshot>(groups.ToArray());
        HeatDepositions = new ReadOnlyCollection<DecayHeatDeposition>(heatDepositions.ToArray());
    }

    public string DefinitionId { get; }

    public DecayHeatState State { get; }

    public Energy TotalStoredDecayEnergy => State.TotalStoredDecayEnergy;

    public Power TotalInstantaneousDecayHeatPower { get; }

    public IReadOnlyList<DecayHeatGroupSnapshot> Groups { get; }

    public IReadOnlyList<DecayHeatDeposition> HeatDepositions { get; }

    public DecayHeatDeposition GetDeposition(string targetDomainId)
        => HeatDepositions.FirstOrDefault(deposition => string.Equals(deposition.TargetDomainId, targetDomainId, StringComparison.Ordinal))
            ?? throw new KeyNotFoundException($"Unknown decay-heat target domain '{targetDomainId}'.");
}
