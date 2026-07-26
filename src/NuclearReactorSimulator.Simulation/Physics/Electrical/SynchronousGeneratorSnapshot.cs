using System.Text.Json.Serialization;
using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Simulation.Physics.Electrical;

/// <summary>
/// Signed generator/grid exchange snapshot. Positive power denotes generation/export; negative power denotes
/// motoring/import. Conversion loss remains non-negative in either direction.
/// </summary>
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
    Power ConversionLossPower)
{
    [JsonIgnore]
    public bool IsGenerating => ElectricalOutputPower > Power.Zero;

    [JsonIgnore]
    public bool IsMotoring => ElectricalOutputPower < Power.Zero;

    [JsonIgnore]
    public Power SignedMechanicalExchangePower => MechanicalInputPower;

    [JsonIgnore]
    public Power SignedElectricalExchangePower => ElectricalOutputPower;
}
