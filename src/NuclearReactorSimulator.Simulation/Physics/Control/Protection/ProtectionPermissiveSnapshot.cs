using NuclearReactorSimulator.Domain.Physics.Instrumentation;

namespace NuclearReactorSimulator.Simulation.Physics.Control.Protection;

public sealed record ProtectionPermissiveSnapshot(
    string PermissiveId,
    string MeasurementChannelId,
    double? Measurement,
    SignalValidity MeasurementValidity,
    SignalQuality MeasurementQuality,
    bool IsSatisfied);
