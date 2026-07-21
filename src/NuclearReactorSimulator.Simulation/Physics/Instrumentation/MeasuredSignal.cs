using NuclearReactorSimulator.Domain.Physics.Instrumentation;

namespace NuclearReactorSimulator.Simulation.Physics.Instrumentation;

/// <summary>
/// Controller/UI-facing measured signal. It intentionally contains no true plant value or direct true-state reference.
/// </summary>
public sealed record MeasuredSignal(
    string ChannelId,
    string EngineeringUnitSymbol,
    double? EngineeringValue,
    double? ScaledValue,
    SignalValidity Validity,
    SignalQuality Quality,
    bool OutOfMeasurementRange,
    SensorFaultMode ActiveFaultMode);
