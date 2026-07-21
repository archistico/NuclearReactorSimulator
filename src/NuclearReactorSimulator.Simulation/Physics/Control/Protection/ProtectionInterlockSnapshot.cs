using NuclearReactorSimulator.Domain.Physics.Control.Protection;
using NuclearReactorSimulator.Domain.Physics.Instrumentation;

namespace NuclearReactorSimulator.Simulation.Physics.Control.Protection;

public sealed record ProtectionInterlockSnapshot(
    string InterlockId,
    string MeasurementChannelId,
    double? Measurement,
    SignalValidity MeasurementValidity,
    SignalQuality MeasurementQuality,
    bool IsActive,
    ProtectionInterlockAction Actions);
