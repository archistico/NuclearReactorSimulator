using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Simulation.Physics.Electrical;

public sealed record SynchronousGeneratorSnapshot(
    string GeneratorId,
    string RotorId,
    string BreakerId,
    Frequency InitialElectricalFrequency,
    Frequency FinalElectricalFrequency,
    PhaseAngle InitialElectricalPhaseAngle,
    PhaseAngle FinalElectricalPhaseAngle,
    PhaseAngleDifference InitialPhaseDifference,
    PhaseAngleDifference FinalPhaseDifference,
    ElectricPotential TerminalLineVoltage,
    ElectricPotential GridLineVoltage,
    Frequency FrequencyDifferenceAtCloseCheck,
    ElectricPotential VoltageDifferenceAtCloseCheck,
    bool SynchronizationConditionsSatisfied,
    bool BreakerInitiallyClosed,
    bool BreakerFinallyClosed,
    bool CloseBreakerCommand,
    bool OpenBreakerCommand,
    bool CloseCommandAccepted,
    bool CloseCommandRejected,
    Power RequestedElectricalPower,
    Torque CommandedElectromagneticTorque,
    Torque EffectiveElectromagneticTorque,
    Power MechanicalInputPower,
    Power ElectricalOutputPower,
    Power ConversionLossPower);
