using NuclearReactorSimulator.Domain.Physics.Instrumentation;

namespace NuclearReactorSimulator.Simulation.Physics.Instrumentation;

/// <summary>Diagnostic-only processing trace; controllers should consume <see cref="MeasuredSignalFrame"/> instead.</summary>
public sealed record InstrumentChannelDiagnosticSnapshot(
    string ChannelId,
    string SourceId,
    double TrueEngineeringValue,
    double FilteredEngineeringValue,
    double? OutputEngineeringValue,
    bool OutOfMeasurementRange,
    SensorFaultMode ActiveFaultMode,
    SignalValidity Validity,
    SignalQuality Quality);
