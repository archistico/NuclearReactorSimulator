using System.Collections.ObjectModel;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.TurbineIsland.Condenser;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Turbine;
using NuclearReactorSimulator.Simulation.Plant;

namespace NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Condenser;

public sealed class CondenserSystemSnapshot
{
    public CondenserSystemSnapshot(
        CondenserSystemDefinition definition,
        TurbineExpansionSnapshot turbineExpansion,
        IEnumerable<CondenserSnapshot> condensers,
        IEnumerable<CondenserCoolingBoundarySnapshot> coolingBoundaries)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        TurbineExpansion = turbineExpansion ?? throw new ArgumentNullException(nameof(turbineExpansion));
        ArgumentNullException.ThrowIfNull(condensers);
        ArgumentNullException.ThrowIfNull(coolingBoundaries);

        if (!ReferenceEquals(turbineExpansion.Definition, definition.TurbineExpansionSystem))
        {
            throw new ArgumentException("Turbine snapshot does not use the condenser system's canonical M4.2 definition.", nameof(turbineExpansion));
        }

        var canonicalCondensers = condensers.OrderBy(static item => item.CondenserId, StringComparer.Ordinal).ToArray();
        var canonicalBoundaries = coolingBoundaries.OrderBy(static item => item.BoundaryId, StringComparer.Ordinal).ToArray();
        ValidateExactSet(definition.Condensers.Select(static item => item.Id), canonicalCondensers.Select(static item => item.CondenserId), "condenser");
        ValidateExactSet(definition.CoolingBoundaries.Select(static item => item.Id), canonicalBoundaries.Select(static item => item.BoundaryId), "cooling boundary");

        Condensers = new ReadOnlyCollection<CondenserSnapshot>(canonicalCondensers);
        CoolingBoundaries = new ReadOnlyCollection<CondenserCoolingBoundarySnapshot>(canonicalBoundaries);
        TotalCondensationMassFlowRate = MassFlowRate.FromKilogramsPerSecond(
            CompensatedSum(canonicalCondensers.Select(static item => item.ActualCondensationMassFlowRate.KilogramsPerSecond)));
        TotalHeatRejectionPower = Power.FromWatts(
            CompensatedSum(canonicalCondensers.Select(static item => item.HeatRejectionPower.Watts)));
    }

    public CondenserSystemDefinition Definition { get; }

    public TurbineExpansionSnapshot TurbineExpansion { get; }

    public IReadOnlyList<CondenserSnapshot> Condensers { get; }

    public IReadOnlyList<CondenserCoolingBoundarySnapshot> CoolingBoundaries { get; }

    public MassFlowRate TotalCondensationMassFlowRate { get; }

    public Power TotalHeatRejectionPower { get; }

    public PlantNetworkAudit ThermofluidAudit => TurbineExpansion.ThermofluidAudit;

    public CondenserSnapshot GetCondenser(string id)
        => Condensers.FirstOrDefault(item => string.Equals(item.CondenserId, id, StringComparison.Ordinal))
            ?? throw new KeyNotFoundException($"Unknown condenser snapshot '{id}'.");

    public CondenserCoolingBoundarySnapshot GetCoolingBoundary(string id)
        => CoolingBoundaries.FirstOrDefault(item => string.Equals(item.BoundaryId, id, StringComparison.Ordinal))
            ?? throw new KeyNotFoundException($"Unknown condenser cooling-boundary snapshot '{id}'.");

    private static void ValidateExactSet(IEnumerable<string> expectedIds, IEnumerable<string> actualIds, string label)
    {
        var expected = expectedIds.OrderBy(static item => item, StringComparer.Ordinal).ToArray();
        var actual = actualIds.OrderBy(static item => item, StringComparer.Ordinal).ToArray();
        if (!expected.SequenceEqual(actual, StringComparer.Ordinal))
        {
            throw new ArgumentException(
                $"Condenser-system snapshot must contain exactly one snapshot per defined {label}. Expected [{string.Join(", ", expected)}], actual [{string.Join(", ", actual)}].");
        }
    }

    private static double CompensatedSum(IEnumerable<double> values)
    {
        var sum = 0d;
        var compensation = 0d;
        foreach (var value in values)
        {
            var adjusted = value - compensation;
            var next = sum + adjusted;
            compensation = (next - sum) - adjusted;
            sum = next;
        }

        return sum;
    }
}
