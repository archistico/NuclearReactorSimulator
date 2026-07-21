namespace NuclearReactorSimulator.Simulation.Physics.Control.Protection;

/// <summary>Observable result of protection-over-normal-control command arbitration.</summary>
public sealed record ProtectionArbitrationSnapshot(
    bool ReactorScramOverrideApplied,
    bool TurbineTripOverrideApplied,
    bool GeneratorTripOverrideApplied,
    bool RodWithdrawalInhibited,
    bool TurbineAdmissionOpeningInhibited,
    bool GeneratorBreakerCloseInhibited,
    IReadOnlyList<string> StopValvesForcedClosed);
