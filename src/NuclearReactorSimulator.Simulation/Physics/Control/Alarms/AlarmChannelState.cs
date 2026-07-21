namespace NuclearReactorSimulator.Simulation.Physics.Control.Alarms;

public sealed record AlarmChannelState(
    string AlarmId,
    bool ConditionActive,
    bool IsLatched,
    bool IsAcknowledged,
    bool IsFirstOut,
    long? ActivationSequence);
