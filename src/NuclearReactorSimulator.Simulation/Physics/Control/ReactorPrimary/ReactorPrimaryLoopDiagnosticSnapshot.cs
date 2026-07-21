using NuclearReactorSimulator.Domain.Physics.Control.ReactorPrimary;

namespace NuclearReactorSimulator.Simulation.Physics.Control.ReactorPrimary;

public sealed record ReactorPrimaryLoopDiagnosticSnapshot(
    string LoopId,
    ReactorPrimaryControlLoopKind Kind,
    string ControllerId,
    string ActuatorId,
    string TargetId,
    double Setpoint,
    double? Measurement,
    double ControllerOutput,
    bool MeasurementAvailable,
    bool OutputSaturated);
