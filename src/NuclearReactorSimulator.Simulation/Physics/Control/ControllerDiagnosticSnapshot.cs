using NuclearReactorSimulator.Domain.Physics.Control;
using NuclearReactorSimulator.Domain.Physics.Instrumentation;

namespace NuclearReactorSimulator.Simulation.Physics.Control;

public sealed record ControllerDiagnosticSnapshot(
    string ControllerId,
    string MeasurementChannelId,
    ControllerMode Mode,
    double Setpoint,
    double? Measurement,
    SignalValidity MeasurementValidity,
    SignalQuality MeasurementQuality,
    double Error,
    double ProportionalTerm,
    double IntegralTerm,
    double DerivativeTerm,
    double UnsaturatedOutput,
    double Output,
    bool IsSaturated,
    bool AntiWindupActive,
    bool BumplessTransferApplied,
    ControllerExecutionStatus Status);
