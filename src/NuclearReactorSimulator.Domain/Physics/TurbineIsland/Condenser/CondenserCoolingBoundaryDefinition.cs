using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Domain.Physics.TurbineIsland.Condenser;

/// <summary>
/// Replaceable M4.3 heat-rejection seam between one condenser and an external cooling-water/environment model.
/// An optional installed-capacity ceiling belongs to the plant definition; runtime availability remains an input.
/// </summary>
public sealed class CondenserCoolingBoundaryDefinition
{
    public CondenserCoolingBoundaryDefinition(
        string id,
        string condenserId,
        Power? maximumInstalledHeatRejectionPower = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Condenser cooling-boundary id cannot be empty or whitespace.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(condenserId))
        {
            throw new ArgumentException("Condenser id cannot be empty or whitespace.", nameof(condenserId));
        }

        if (maximumInstalledHeatRejectionPower is { } installedCapacity && installedCapacity <= Power.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumInstalledHeatRejectionPower),
                maximumInstalledHeatRejectionPower,
                "Installed condenser heat-rejection capacity must be greater than zero when supplied.");
        }

        Id = id.Trim();
        CondenserId = condenserId.Trim();
        MaximumInstalledHeatRejectionPower = maximumInstalledHeatRejectionPower;
    }

    public string Id { get; }

    public string CondenserId { get; }

    /// <summary>
    /// Optional physical installed-capacity ceiling. Null preserves legacy semantics where the runtime available-capacity
    /// input is itself the only external cooling-capacity ceiling.
    /// </summary>
    public Power? MaximumInstalledHeatRejectionPower { get; }
}
