using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Condenser;

/// <summary>
/// M4.3 replaceable cooling-water/environment boundary for one condenser step.
/// AvailableHeatRejectionPower is the currently available external cooling capacity after environment/fault effects.
/// Current definitions may independently declare an installed hardware ceiling and a UA surface-transfer ceiling.
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

    /// <summary>Current external cooling capacity available after operating-condition and fault effects.</summary>
    public Power AvailableHeatRejectionPower { get; }

    /// <summary>Effective cooling-water/environment temperature seen by the condenser surface.</summary>
    public Temperature CoolantTemperature { get; }
}
