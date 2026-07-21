using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Simulation.Physics.TurbineIsland.MainSteam;

public sealed record TurbineAdmissionTrainSnapshot(
    string TrainId,
    string HeaderNodeId,
    string TurbineInletNodeId,
    MainSteamValveSnapshot StopValve,
    MainSteamValveSnapshot ControlValve,
    MainSteamValveSnapshot AdmissionValve,
    MassFlowRate StopToControlContinuityResidual,
    MassFlowRate ControlToAdmissionContinuityResidual,
    Pressure TurbineInletPressure,
    Temperature TurbineInletTemperature,
    FluidPhase TurbineInletPhase,
    VaporQuality? TurbineInletVaporQuality);
