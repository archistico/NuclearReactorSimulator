using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Simulation.Physics.Reactor.PrimaryCircuit.Circulation;

public sealed record MainCirculationPumpSnapshot(
    string PumpId,
    bool IsRunning,
    PumpSpeed EffectiveSpeed,
    PressureDifference ActivePressureBoost,
    MassFlowRate MassFlowRate,
    VolumetricFlowRate VolumetricFlowRate,
    Power HydraulicPowerExchange,
    Power ShaftPowerDemand);
