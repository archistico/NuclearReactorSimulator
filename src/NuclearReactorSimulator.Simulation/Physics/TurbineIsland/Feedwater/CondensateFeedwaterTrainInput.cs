using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Feedwater;

/// <summary>
/// Per-step manually commanded thermal-conditioning input for one M4.4 condensate/feedwater train.
/// Pump operating states remain canonical PlantState component states.
/// </summary>
public sealed record CondensateFeedwaterTrainInput
{
    public CondensateFeedwaterTrainInput(string trainId, Power thermalConditioningPower)
    {
        if (string.IsNullOrWhiteSpace(trainId))
        {
            throw new ArgumentException("Condensate/feedwater train input id cannot be empty or whitespace.", nameof(trainId));
        }

        if (thermalConditioningPower < Power.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(thermalConditioningPower),
                thermalConditioningPower,
                "Feedwater thermal-conditioning power cannot be negative.");
        }

        TrainId = trainId.Trim();
        ThermalConditioningPower = thermalConditioningPower;
    }

    public string TrainId { get; }

    public Power ThermalConditioningPower { get; }
}
