using NuclearReactorSimulator.Domain.Physics.Control.Protection;
using NuclearReactorSimulator.Domain.Physics.Instrumentation;

namespace NuclearReactorSimulator.Simulation.Physics.Control.Protection;

public sealed record ProtectionFunctionSnapshot(
    string FunctionId,
    string MeasurementChannelId,
    double? Measurement,
    SignalValidity MeasurementValidity,
    SignalQuality MeasurementQuality,
    bool SupervisionActive,
    bool TriggerActive,
    TimeSpan PickupElapsed,
    TimeSpan PickupDelay,
    bool PickupComplete,
    bool ResetConditionSafe,
    bool WasLatched,
    bool IsLatched,
    ProtectionAction Actions);
