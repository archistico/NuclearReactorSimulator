using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Domain.Physics.TurbineIsland.Feedwater;

/// <summary>
/// Canonical M4.4 condensate/feedwater path for one M3 steam-drum feedwater seam.
/// The conserved path is hotwell -> condensate pump -> feedwater inventory/conditioning node -> feedwater pump -> drum target.
/// </summary>
public sealed class CondensateFeedwaterTrainDefinition
{
    public CondensateFeedwaterTrainDefinition(
        string id,
        string condenserId,
        string feedwaterBoundaryId,
        string condensatePumpId,
        string feedwaterInventoryNodeId,
        string feedwaterPumpId,
        Power maximumThermalConditioningPower)
    {
        Id = ValidateId(id, nameof(id), "Condensate/feedwater train");
        CondenserId = ValidateId(condenserId, nameof(condenserId), "Condenser");
        FeedwaterBoundaryId = ValidateId(feedwaterBoundaryId, nameof(feedwaterBoundaryId), "Feedwater boundary");
        CondensatePumpId = ValidateId(condensatePumpId, nameof(condensatePumpId), "Condensate pump");
        FeedwaterInventoryNodeId = ValidateId(feedwaterInventoryNodeId, nameof(feedwaterInventoryNodeId), "Feedwater inventory node");
        FeedwaterPumpId = ValidateId(feedwaterPumpId, nameof(feedwaterPumpId), "Feedwater pump");

        if (maximumThermalConditioningPower < Power.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumThermalConditioningPower),
                maximumThermalConditioningPower,
                "Maximum feedwater thermal-conditioning power cannot be negative.");
        }

        MaximumThermalConditioningPower = maximumThermalConditioningPower;
    }

    public string Id { get; }

    public string CondenserId { get; }

    public string FeedwaterBoundaryId { get; }

    public string CondensatePumpId { get; }

    public string FeedwaterInventoryNodeId { get; }

    public string FeedwaterPumpId { get; }

    public Power MaximumThermalConditioningPower { get; }

    private static string ValidateId(string value, string parameterName, string label)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{label} id cannot be empty or whitespace.", parameterName);
        }

        return value.Trim();
    }
}
