using System.Collections.ObjectModel;
using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Simulation.Physics.Reactor.ThermalPower;

/// <summary>
/// Immutable instantaneous fission-power projection and exact heat-deposition partition.
/// </summary>
public sealed class FissionPowerSnapshot
{
    internal FissionPowerSnapshot(
        string definitionId,
        NeutronPopulation neutronPopulation,
        Power totalFissionThermalPower,
        IEnumerable<FissionHeatDeposition> heatDepositions)
    {
        if (string.IsNullOrWhiteSpace(definitionId))
        {
            throw new ArgumentException("Fission-power definition id cannot be empty.", nameof(definitionId));
        }

        ArgumentNullException.ThrowIfNull(heatDepositions);

        var canonical = heatDepositions
            .Select(static deposition => deposition ?? throw new ArgumentException("Heat depositions cannot contain null entries.", "heatDepositions"))
            .OrderBy(static deposition => deposition.TargetDomainId, StringComparer.Ordinal)
            .ToArray();

        DefinitionId = definitionId;
        NeutronPopulation = neutronPopulation;
        TotalFissionThermalPower = totalFissionThermalPower;
        HeatDepositions = new ReadOnlyCollection<FissionHeatDeposition>(canonical);
    }

    public string DefinitionId { get; }

    public NeutronPopulation NeutronPopulation { get; }

    public Power TotalFissionThermalPower { get; }

    public IReadOnlyList<FissionHeatDeposition> HeatDepositions { get; }

    public FissionHeatDeposition GetDeposition(string targetDomainId)
        => HeatDepositions.FirstOrDefault(deposition => string.Equals(deposition.TargetDomainId, targetDomainId, StringComparison.Ordinal))
            ?? throw new KeyNotFoundException($"Unknown fission-heat target domain '{targetDomainId}'.");
}
