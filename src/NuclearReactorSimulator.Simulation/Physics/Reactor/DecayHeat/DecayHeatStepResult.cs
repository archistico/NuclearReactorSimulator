using System.Collections.ObjectModel;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor.DecayHeat;

namespace NuclearReactorSimulator.Simulation.Physics.Reactor.DecayHeat;

/// <summary>
/// Exact finite-step decay-heat evolution result.
/// Average emitted power is intended for same-step thermal integration; the snapshot exposes end-of-step instantaneous power.
/// </summary>
public sealed record DecayHeatStepResult
{
    public DecayHeatStepResult(
        DecayHeatState state,
        Power precursorProductionPower,
        Energy producedDecayEnergy,
        Energy emittedDecayEnergy,
        Power averageDecayHeatPower,
        DecayHeatSnapshot snapshot,
        IEnumerable<DecayHeatDeposition> averageHeatDepositions)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(averageHeatDepositions);

        if (precursorProductionPower < Power.Zero || averageDecayHeatPower < Power.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(averageDecayHeatPower), "Decay-heat powers cannot be negative.");
        }

        if (producedDecayEnergy < Energy.Zero || emittedDecayEnergy < Energy.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(emittedDecayEnergy), "Decay-heat energies cannot be negative.");
        }

        State = state;
        PrecursorProductionPower = precursorProductionPower;
        ProducedDecayEnergy = producedDecayEnergy;
        EmittedDecayEnergy = emittedDecayEnergy;
        AverageDecayHeatPower = averageDecayHeatPower;
        Snapshot = snapshot;
        AverageHeatDepositions = new ReadOnlyCollection<DecayHeatDeposition>(averageHeatDepositions.ToArray());
    }

    public DecayHeatState State { get; }

    public Power PrecursorProductionPower { get; }

    public Energy ProducedDecayEnergy { get; }

    public Energy EmittedDecayEnergy { get; }

    public Power AverageDecayHeatPower { get; }

    public DecayHeatSnapshot Snapshot { get; }

    public IReadOnlyList<DecayHeatDeposition> AverageHeatDepositions { get; }

    public DecayHeatDeposition GetAverageDeposition(string targetDomainId)
        => AverageHeatDepositions.FirstOrDefault(deposition => string.Equals(deposition.TargetDomainId, targetDomainId, StringComparison.Ordinal))
            ?? throw new KeyNotFoundException($"Unknown average decay-heat target domain '{targetDomainId}'.");
}
