namespace NuclearReactorSimulator.Application;

/// <summary>
/// Describes the currently composed application candidate without coupling the UI to build-time constants.
/// </summary>
public sealed record ApplicationDescriptor(string ProductName, string Milestone, string Status)
{
    public static ApplicationDescriptor Current { get; } = new(
        "Nuclear Reactor Simulator",
        "M10.9.6.2 — Operational Challenge & Energy-Demand Framework / Deterministic External Energy-Demand Profiles",
        "M10.9.4.1 / Phase I, M10.9.5 Contextual Command Consequence Model and M10.9.6.1 Challenge Lifecycle & Logical-Time Contract are validated. Authoritative desktop production remains integrated-operations-desktop-stable@4 with CorrelationConsistentInverseDomain thermodynamics and FourNodeBranchContinuityCorrectedCommitOptIn hydraulics at the unchanged 10 ms fixed step; exact @3 remains historical, exact @2 remains fail-closed rollback/reference, and pre-synchronization-grid-loading@3 remains the independent validated synchronization identity. M10.9.6.2 adds deterministic Application-layer external electrical-demand references owned only by versioned challenge definitions. Profiles are logical-step-only and auditable as constant, step, bounded-ramp or piecewise HOLD/LINEAR control-point sequences. EXTERNAL GRID DEMAND remains distinct from generator requested load and actual electrical output; demand/output error is observational evidence only. Demand is unavailable for challenges that do not own a profile or before challenge activation, future-schedule visibility is definition-owned, and ScenarioChallengeExternalDemandProjector has no dispatcher, generator-setpoint, grid-coupling or supervisory-control authority. No score arithmetic, challenge UI, physical coefficient, protection threshold, command type or exact-version behavior changes in M10.9.6.2."
    );
}
