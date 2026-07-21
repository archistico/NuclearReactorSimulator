using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Condenser;

public sealed record CondenserSnapshot(
    string CondenserId,
    string TurbineStageGroupId,
    string SteamSpaceNodeId,
    string HotwellNodeId,
    string CoolingBoundaryId,
    Pressure InitialSteamSpacePressure,
    Pressure FinalSteamSpacePressure,
    PressureDifference InitialVacuumBelowAtmosphere,
    PressureDifference FinalVacuumBelowAtmosphere,
    Temperature InitialSteamSpaceTemperature,
    Temperature FinalSteamSpaceTemperature,
    FluidPhase InitialSteamSpacePhase,
    FluidPhase FinalSteamSpacePhase,
    VaporQuality? InitialSteamSpaceVaporQuality,
    VaporQuality? FinalSteamSpaceVaporQuality,
    double CondensableVaporMassFraction,
    Mass AvailableCondensableMass,
    MassFlowRate MaximumCondensationMassFlowRate,
    MassFlowRate InventoryLimitedCondensationMassFlowRate,
    MassFlowRate ThermalLimitedCondensationMassFlowRate,
    MassFlowRate ActualCondensationMassFlowRate,
    SpecificEnergy SteamSpecificInternalEnergy,
    SpecificEnergy CondensateSpecificInternalEnergy,
    Power SteamEnergyRemovalRate,
    Power HotwellEnergyAdditionRate,
    Power HeatRejectionPower,
    Mass InitialHotwellMass,
    Mass FinalHotwellMass,
    Temperature InitialHotwellTemperature,
    Temperature FinalHotwellTemperature,
    FluidPhase InitialHotwellPhase,
    FluidPhase FinalHotwellPhase);
