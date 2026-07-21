using NuclearReactorSimulator.Simulation.Physics.Control.Protection;

namespace NuclearReactorSimulator.Simulation.Physics.Control.Alarms;

/// <summary>M5.6 top-level snapshot. Alarm acknowledgement is presentation state and cannot alter the protected physical snapshot.</summary>
public sealed record AlarmedProtectedAutomaticControlSnapshot(
    ProtectedAutomaticControlSnapshot ProtectedControl,
    AlarmSystemSnapshot Alarms);
