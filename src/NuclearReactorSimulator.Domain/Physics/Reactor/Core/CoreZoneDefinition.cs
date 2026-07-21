namespace NuclearReactorSimulator.Domain.Physics.Reactor.Core;

/// <summary>
/// Immutable logical definition of one aggregated core zone and the plant domains used for local diagnostics.
/// </summary>
public sealed record CoreZoneDefinition
{
    public CoreZoneDefinition(
        string id,
        CoreZoneCoordinate coordinate,
        CoreZonePowerFraction nominalPowerFraction,
        string fuelThermalBodyId,
        string structureThermalBodyId,
        string coolantFluidNodeId)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Core-zone id cannot be empty or whitespace.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(fuelThermalBodyId))
        {
            throw new ArgumentException("Fuel thermal-body id cannot be empty or whitespace.", nameof(fuelThermalBodyId));
        }

        if (string.IsNullOrWhiteSpace(structureThermalBodyId))
        {
            throw new ArgumentException("Structure thermal-body id cannot be empty or whitespace.", nameof(structureThermalBodyId));
        }

        if (string.IsNullOrWhiteSpace(coolantFluidNodeId))
        {
            throw new ArgumentException("Coolant fluid-node id cannot be empty or whitespace.", nameof(coolantFluidNodeId));
        }

        Id = id.Trim();
        Coordinate = coordinate;
        NominalPowerFraction = nominalPowerFraction;
        FuelThermalBodyId = fuelThermalBodyId.Trim();
        StructureThermalBodyId = structureThermalBodyId.Trim();
        CoolantFluidNodeId = coolantFluidNodeId.Trim();
    }

    public string Id { get; }

    public CoreZoneCoordinate Coordinate { get; }

    public CoreZonePowerFraction NominalPowerFraction { get; }

    public string FuelThermalBodyId { get; }

    public string StructureThermalBodyId { get; }

    public string CoolantFluidNodeId { get; }
}
