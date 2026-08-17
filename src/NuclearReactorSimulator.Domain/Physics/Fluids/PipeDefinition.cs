using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Domain.Physics.Fluids;

/// <summary>
/// Defines a bidirectional passive hydraulic connection between two fluid nodes.
/// From/to establish only the positive reference direction; actual flow may reverse.
/// </summary>
public sealed record PipeDefinition
{
    public PipeDefinition(
        string id,
        string fromNodeId,
        string toNodeId,
        QuadraticHydraulicResistance resistance,
        FluidEnergyTransportMode energyTransportMode = FluidEnergyTransportMode.SpecificInternalEnergy)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("A pipe identifier cannot be empty or whitespace.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(fromNodeId))
        {
            throw new ArgumentException("A pipe from-node identifier cannot be empty or whitespace.", nameof(fromNodeId));
        }

        if (string.IsNullOrWhiteSpace(toNodeId))
        {
            throw new ArgumentException("A pipe to-node identifier cannot be empty or whitespace.", nameof(toNodeId));
        }

        var normalizedFromNodeId = fromNodeId.Trim();
        var normalizedToNodeId = toNodeId.Trim();

        if (string.Equals(normalizedFromNodeId, normalizedToNodeId, StringComparison.Ordinal))
        {
            throw new ArgumentException("A pipe must connect two distinct fluid nodes.", nameof(toNodeId));
        }

        if (resistance.PascalSecondsSquaredPerKilogramSquared <= 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(resistance), resistance, "A pipe hydraulic resistance must be greater than zero.");
        }

        if (!Enum.IsDefined(energyTransportMode))
        {
            throw new ArgumentOutOfRangeException(
                nameof(energyTransportMode),
                energyTransportMode,
                "Unknown fluid energy-transport mode.");
        }

        Id = id.Trim();
        FromNodeId = normalizedFromNodeId;
        ToNodeId = normalizedToNodeId;
        Resistance = resistance;
        EnergyTransportMode = energyTransportMode;
    }

    public string Id { get; }

    public string FromNodeId { get; }

    public string ToNodeId { get; }

    public QuadraticHydraulicResistance Resistance { get; }

    /// <summary>
    /// Specific-energy convention used by this connection when producing node balances.
    /// The default preserves the historical internal-energy advection contract.
    /// </summary>
    public FluidEnergyTransportMode EnergyTransportMode { get; }
}
