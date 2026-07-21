namespace NuclearReactorSimulator.Simulation.Physics.Control.Alarms;

public sealed record AlarmEventSnapshot(long Sequence, string AlarmId, AlarmEventKind Kind);
