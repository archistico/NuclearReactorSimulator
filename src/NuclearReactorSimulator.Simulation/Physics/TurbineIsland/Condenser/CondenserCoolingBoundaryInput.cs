using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Condenser;

/// <summary>
/// M4.3 replaceable cooling-water/environment heat-rejection capacity for one condenser step.
/// </summary>
public sealed record CondenserCoolingBoundaryInput
{
    public CondenserCoolingBoundaryInput(string boundaryId, Power availableHeatRejectionPower)
    {
        if (string.IsNullOrWhiteSpace(boundaryId))
        {
            throw new ArgumentException("Condenser cooling-boundary id cannot be empty or whitespace.", nameof(boundaryId));
        }

        if (availableHeatRejectionPower < Power.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(availableHeatRejectionPower),
                availableHeatRejectionPower,
                "Available condenser heat-rejection power cannot be negative.");
        }

        BoundaryId = boundaryId.Trim();
        AvailableHeatRejectionPower = availableHeatRejectionPower;
    }

    public string BoundaryId { get; }

    public Power AvailableHeatRejectionPower { get; }
}
