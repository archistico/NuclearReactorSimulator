using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Condenser;

/// <summary>
/// M4.3 replaceable cooling-water/environment boundary for one condenser step.
/// AvailableHeatRejectionPower is an upper capacity ceiling; current surface-condenser definitions may also
/// close actual heat transfer through their canonical UA against CoolantTemperature.
/// </summary>
public sealed record CondenserCoolingBoundaryInput
{
    public CondenserCoolingBoundaryInput(
        string boundaryId,
        Power availableHeatRejectionPower,
        Temperature? coolantTemperature = null)
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
        CoolantTemperature = coolantTemperature ?? Temperature.FromDegreesCelsius(20d);
    }

    public string BoundaryId { get; }

    /// <summary>External cooling-system capacity ceiling available to the condenser.</summary>
    public Power AvailableHeatRejectionPower { get; }

    /// <summary>Effective cooling-water/environment temperature seen by the condenser surface.</summary>
    public Temperature CoolantTemperature { get; }
}
