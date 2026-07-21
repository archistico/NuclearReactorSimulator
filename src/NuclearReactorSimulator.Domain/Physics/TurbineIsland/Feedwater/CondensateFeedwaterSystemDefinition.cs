using System.Collections.ObjectModel;
using NuclearReactorSimulator.Domain.Physics.TurbineIsland.Condenser;
using NuclearReactorSimulator.Domain.Plant;

namespace NuclearReactorSimulator.Domain.Physics.TurbineIsland.Feedwater;

/// <summary>
/// Canonical M4.4 composition that closes the condensate/feedwater mass path from condenser hotwells back to every M3 steam-drum feedwater target.
/// </summary>
public sealed class CondensateFeedwaterSystemDefinition
{
    public CondensateFeedwaterSystemDefinition(
        string id,
        CondenserSystemDefinition condenserSystem,
        IEnumerable<CondensateFeedwaterTrainDefinition> trains)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Condensate/feedwater-system id cannot be empty or whitespace.", nameof(id));
        }

        CondenserSystem = condenserSystem ?? throw new ArgumentNullException(nameof(condenserSystem));
        ArgumentNullException.ThrowIfNull(trains);

        var canonical = trains
            .Select(item => item ?? throw new ArgumentException("Condensate/feedwater train collections cannot contain null entries.", nameof(trains)))
            .OrderBy(static item => item.Id, StringComparer.Ordinal)
            .ToArray();
        if (canonical.Length == 0)
        {
            throw new ArgumentException("A condensate/feedwater system must contain at least one train.", nameof(trains));
        }

        if (canonical.Select(static item => item.Id).Distinct(StringComparer.Ordinal).Count() != canonical.Length)
        {
            throw new ArgumentException("Condensate/feedwater train ids must be unique.", nameof(trains));
        }

        ValidateTopology(condenserSystem, canonical);

        Id = id.Trim();
        Trains = new ReadOnlyCollection<CondensateFeedwaterTrainDefinition>(canonical);
    }

    public string Id { get; }

    public CondenserSystemDefinition CondenserSystem { get; }

    public PlantDefinition PlantDefinition => CondenserSystem.PlantDefinition;

    public IReadOnlyList<CondensateFeedwaterTrainDefinition> Trains { get; }

    public CondensateFeedwaterTrainDefinition GetTrain(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Condensate/feedwater train id cannot be empty or whitespace.", nameof(id));
        }

        return Trains.FirstOrDefault(item => string.Equals(item.Id, id, StringComparison.Ordinal))
            ?? throw new KeyNotFoundException($"Unknown condensate/feedwater train '{id}'.");
    }

    private static void ValidateTopology(
        CondenserSystemDefinition condenserSystem,
        IReadOnlyList<CondensateFeedwaterTrainDefinition> trains)
    {
        var plant = condenserSystem.PlantDefinition;
        var boundarySystem = condenserSystem.TurbineExpansionSystem.MainSteamNetwork.PrimaryCircuit.BoundarySystem;
        var assignedBoundaryIds = new HashSet<string>(StringComparer.Ordinal);
        var assignedPumpIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var train in trains)
        {
            var condenser = condenserSystem.GetCondenser(train.CondenserId);
            var feedwaterBoundary = boundarySystem.GetFeedwaterBoundary(train.FeedwaterBoundaryId);
            var condensatePump = plant.GetPump(train.CondensatePumpId);
            var feedwaterPump = plant.GetPump(train.FeedwaterPumpId);
            _ = plant.GetFluidNode(train.FeedwaterInventoryNodeId);

            if (!assignedBoundaryIds.Add(feedwaterBoundary.Id))
            {
                throw new ArgumentException(
                    $"Feedwater boundary '{feedwaterBoundary.Id}' is assigned to more than one M4.4 train.",
                    nameof(trains));
            }

            if (!assignedPumpIds.Add(condensatePump.Id) || !assignedPumpIds.Add(feedwaterPump.Id))
            {
                throw new ArgumentException("A condensate/feedwater pump cannot be shared by multiple M4.4 train roles.", nameof(trains));
            }

            if (!string.Equals(condensatePump.Pipe.FromNodeId, condenser.HotwellNodeId, StringComparison.Ordinal)
                || !string.Equals(condensatePump.Pipe.ToNodeId, train.FeedwaterInventoryNodeId, StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    $"Train '{train.Id}' condensate pump '{condensatePump.Id}' must connect condenser hotwell '{condenser.HotwellNodeId}' to feedwater inventory '{train.FeedwaterInventoryNodeId}'.",
                    nameof(trains));
            }

            if (!string.Equals(feedwaterPump.Pipe.FromNodeId, train.FeedwaterInventoryNodeId, StringComparison.Ordinal)
                || !string.Equals(feedwaterPump.Pipe.ToNodeId, feedwaterBoundary.TargetNodeId, StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    $"Train '{train.Id}' feedwater pump '{feedwaterPump.Id}' must connect feedwater inventory '{train.FeedwaterInventoryNodeId}' to M3 feedwater target '{feedwaterBoundary.TargetNodeId}'.",
                    nameof(trains));
            }
        }

        var expectedBoundaryIds = boundarySystem.FeedwaterBoundaries
            .Select(static item => item.Id)
            .ToHashSet(StringComparer.Ordinal);
        if (!expectedBoundaryIds.SetEquals(assignedBoundaryIds))
        {
            var missing = expectedBoundaryIds
                .Except(assignedBoundaryIds, StringComparer.Ordinal)
                .OrderBy(static item => item, StringComparer.Ordinal);
            throw new ArgumentException(
                $"Every M3 feedwater boundary must be replaced by exactly one M4.4 condensate/feedwater train. Missing: {string.Join(", ", missing)}.",
                nameof(trains));
        }
    }
}
