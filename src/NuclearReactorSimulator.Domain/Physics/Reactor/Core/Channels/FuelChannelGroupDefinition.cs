using NuclearReactorSimulator.Domain.Physics.Reactor.Core;
using NuclearReactorSimulator.Domain.Physics.Reactor.ThermalPower;

namespace NuclearReactorSimulator.Domain.Physics.Reactor.Core.Channels;

/// <summary>
/// Immutable semantic grouping of equivalent fuel channels inside one aggregated core zone.
/// The hydraulic path and thermal domains are canonical plant components; this definition does not duplicate them.
/// </summary>
public sealed record FuelChannelGroupDefinition
{
    private const double DepositionFractionTolerance = 1e-12d;

    public FuelChannelGroupDefinition(
        string id,
        string zoneId,
        int representedChannelCount,
        CoreZonePowerFraction zonePowerFraction,
        string hydraulicPipeId,
        string inletCoolantNodeId,
        string outletCoolantNodeId,
        string fuelThermalBodyId,
        string structureThermalBodyId,
        HeatDepositionFraction fuelHeatFraction,
        HeatDepositionFraction structureHeatFraction,
        HeatDepositionFraction coolantHeatFraction)
    {
        Id = ValidateId(id, nameof(id), "Fuel-channel group");
        ZoneId = ValidateId(zoneId, nameof(zoneId), "Core-zone");
        HydraulicPipeId = ValidateId(hydraulicPipeId, nameof(hydraulicPipeId), "Hydraulic pipe");
        InletCoolantNodeId = ValidateId(inletCoolantNodeId, nameof(inletCoolantNodeId), "Inlet coolant node");
        OutletCoolantNodeId = ValidateId(outletCoolantNodeId, nameof(outletCoolantNodeId), "Outlet coolant node");
        FuelThermalBodyId = ValidateId(fuelThermalBodyId, nameof(fuelThermalBodyId), "Fuel thermal body");
        StructureThermalBodyId = ValidateId(structureThermalBodyId, nameof(structureThermalBodyId), "Structure thermal body");

        if (representedChannelCount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(representedChannelCount),
                representedChannelCount,
                "Represented channel count must be greater than zero.");
        }

        var depositionSum = fuelHeatFraction.Fraction + structureHeatFraction.Fraction + coolantHeatFraction.Fraction;
        if (Math.Abs(depositionSum - 1d) > DepositionFractionTolerance)
        {
            throw new ArgumentException(
                $"Fuel-channel heat-deposition fractions must sum to 1.0 within tolerance {DepositionFractionTolerance:G}; actual sum is {depositionSum:R}.");
        }

        RepresentedChannelCount = representedChannelCount;
        ZonePowerFraction = zonePowerFraction;
        FuelHeatFraction = fuelHeatFraction;
        StructureHeatFraction = structureHeatFraction;
        CoolantHeatFraction = coolantHeatFraction;
    }

    public string Id { get; }

    public string ZoneId { get; }

    public int RepresentedChannelCount { get; }

    /// <summary>Fraction of the parent zone's fission power represented by this group.</summary>
    public CoreZonePowerFraction ZonePowerFraction { get; }

    public string HydraulicPipeId { get; }

    public string InletCoolantNodeId { get; }

    public string OutletCoolantNodeId { get; }

    public string FuelThermalBodyId { get; }

    public string StructureThermalBodyId { get; }

    public HeatDepositionFraction FuelHeatFraction { get; }

    public HeatDepositionFraction StructureHeatFraction { get; }

    public HeatDepositionFraction CoolantHeatFraction { get; }

    private static string ValidateId(string value, string parameterName, string label)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{label} id cannot be empty or whitespace.", parameterName);
        }

        return value.Trim();
    }
}
