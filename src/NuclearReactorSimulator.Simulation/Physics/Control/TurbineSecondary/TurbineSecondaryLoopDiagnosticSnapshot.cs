using NuclearReactorSimulator.Domain.Physics.Control.TurbineSecondary;

namespace NuclearReactorSimulator.Simulation.Physics.Control.TurbineSecondary;

public sealed record TurbineSecondaryLoopDiagnosticSnapshot(
    string LoopId,
    TurbineSecondaryControlLoopKind Kind,
    string ControllerId,
    string ActuatorId,
    string TargetId,
    double Setpoint,
    double? Measurement,
    double ControllerOutput,
    bool MeasurementAvailable,
    bool OutputSaturated);
