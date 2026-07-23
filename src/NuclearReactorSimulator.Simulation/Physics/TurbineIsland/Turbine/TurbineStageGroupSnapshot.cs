using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Turbine;

public sealed record TurbineStageGroupSnapshot(
    string StageGroupId,
    string AdmissionBoundaryId,
    string InletNodeId,
    string ExhaustNodeId,
    string RotorId,
    bool TripBlocked,
    MassFlowRate CommandedMassFlowRate,
    MassFlowRate EffectiveMassFlowRate,
    Pressure InletPressure,
    Temperature InletTemperature,
    FluidPhase InletPhase,
    VaporQuality? InletVaporQuality,
    SpecificEnergy InletSpecificInternalEnergy,
    Pressure ExhaustPressure,
    Temperature ExhaustTemperature,
    bool ThermodynamicWorkModelActive,
    SpecificEnergy PressureTemperatureAvailableSpecificWork,
    SpecificEnergy InletEnergyBoundedSpecificWork,
    SpecificEnergy EffectiveIdealSpecificWork,
    bool ThermodynamicWorkLimited,
    SpecificEnergy NominalSpecificWork,
    SpecificEnergy ExtractedSpecificWork,
    SpecificEnergy ExhaustSpecificInternalEnergy,
    Power InletEnergyFlowRate,
    Power ExhaustEnergyFlowRate,
    Power ShaftPower,
    Torque ShaftTorque);
