using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor.PrimaryCircuit.SteamDrums;

namespace NuclearReactorSimulator.Simulation.Physics.Reactor.PrimaryCircuit.SteamDrums;

public sealed record SteamDrumSnapshot(
    string DrumId,
    string MainCirculationLoopId,
    string InventoryNodeId,
    string SteamOutletNodeId,
    string LiquidRecirculationNodeId,
    Mass InventoryMass,
    Energy InventoryInternalEnergy,
    Pressure Pressure,
    Temperature Temperature,
    FluidPhase Phase,
    VaporQuality? VaporQuality,
    VoidFraction VoidFraction,
    SteamDrumLevelFraction LiquidLevelFraction,
    MassFlowRate IncomingReturnMassFlowRate,
    MassFlowRate SeparatedSteamMassFlowRate,
    MassFlowRate RecirculatedLiquidMassFlowRate,
    SpecificEnergy SteamSpecificInternalEnergy,
    SpecificEnergy LiquidSpecificInternalEnergy,
    Power SteamEnergyRate,
    Power LiquidEnergyRate,
    double SeparationMassResidualKilogramsPerSecond,
    double SeparationEnergyResidualWatts);
