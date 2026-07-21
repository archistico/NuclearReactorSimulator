using System.Collections.ObjectModel;
using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using NuclearReactorSimulator.Simulation.Physics.Thermal;

namespace NuclearReactorSimulator.Simulation.Plant;

/// <summary>
/// Immutable result of one staged composed-plant network step.
/// The candidate state is not committed by this type; the runtime remains the transactional commit boundary.
/// </summary>
public sealed class PlantNetworkStepResult
{
    public PlantNetworkStepResult(
        PlantState candidateState,
        PlantNetworkAudit audit,
        IReadOnlyDictionary<string, FluidNodeBalance> fluidNodeBalances,
        IReadOnlyDictionary<string, ThermalEnergyBalance> thermalBodyBalances)
    {
        ArgumentNullException.ThrowIfNull(candidateState);
        ArgumentNullException.ThrowIfNull(audit);
        ArgumentNullException.ThrowIfNull(fluidNodeBalances);
        ArgumentNullException.ThrowIfNull(thermalBodyBalances);

        CandidateState = candidateState;
        Audit = audit;
        FluidNodeBalances = CanonicalCopy(fluidNodeBalances);
        ThermalBodyBalances = CanonicalCopy(thermalBodyBalances);
    }

    public PlantState CandidateState { get; }

    public PlantNetworkAudit Audit { get; }

    public IReadOnlyDictionary<string, FluidNodeBalance> FluidNodeBalances { get; }

    public IReadOnlyDictionary<string, ThermalEnergyBalance> ThermalBodyBalances { get; }

    private static IReadOnlyDictionary<string, TValue> CanonicalCopy<TValue>(
        IReadOnlyDictionary<string, TValue> source)
    {
        var sorted = new SortedDictionary<string, TValue>(StringComparer.Ordinal);
        foreach (var entry in source)
        {
            sorted.Add(entry.Key, entry.Value);
        }

        return new ReadOnlyDictionary<string, TValue>(sorted);
    }
}
