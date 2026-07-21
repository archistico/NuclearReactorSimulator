using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Feedwater;

/// <summary>
/// Immutable M4.4 diagnostic projection of one canonical condensate/feedwater pump solved from the committed PlantState.
/// </summary>
public sealed record FeedwaterPumpSnapshot(
    string PumpId,
    string FromNodeId,
    string ToNodeId,
    bool IsRunning,
    PumpSpeed EffectiveSpeed,
    MassFlowRate MassFlowRate,
    PressureDifference ActivePressureBoost,
    PressureDifference InternalPressureLoss,
    Power HydraulicPowerExchange,
    Power ShaftPowerDemand);
