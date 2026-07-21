using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Simulation.Physics.TurbineIsland.MainSteam;

public sealed record MainSteamValveSnapshot(
    string ValveId,
    ValvePosition EffectivePosition,
    ValveFlowCoefficient FlowCoefficient,
    PressureDifference PressureDifference,
    MassFlowRate MassFlowRate,
    Power InternalEnergyFlowRate);
