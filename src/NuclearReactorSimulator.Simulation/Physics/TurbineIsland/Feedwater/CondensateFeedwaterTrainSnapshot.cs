using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Feedwater;

/// <summary>
/// Immutable M4.4 diagnostics for one hotwell-to-steam-drum condensate/feedwater train.
/// </summary>
public sealed record CondensateFeedwaterTrainSnapshot(
    string TrainId,
    string CondenserId,
    string FeedwaterBoundaryId,
    string HotwellNodeId,
    string FeedwaterInventoryNodeId,
    string FeedwaterTargetNodeId,
    FeedwaterPumpSnapshot CondensatePump,
    FeedwaterPumpSnapshot FeedwaterPump,
    Power ThermalConditioningPower,
    Mass InitialHotwellMass,
    Mass FinalHotwellMass,
    Mass InitialFeedwaterInventoryMass,
    Mass FinalFeedwaterInventoryMass,
    Temperature InitialFeedwaterInventoryTemperature,
    Temperature FinalFeedwaterInventoryTemperature,
    SpecificEnergy InitialFeedwaterSpecificInternalEnergy,
    SpecificEnergy FinalFeedwaterSpecificInternalEnergy,
    FluidPhase FinalFeedwaterInventoryPhase);
