namespace NuclearReactorSimulator.Application;

/// <summary>
/// Describes the currently composed application candidate without coupling the UI to build-time constants.
/// </summary>
public sealed record ApplicationDescriptor(string ProductName, string Milestone, string Status)
{
    public static ApplicationDescriptor Current { get; } = new(
        "Nuclear Reactor Simulator",
        "M10.9.6.1 — Operational Challenge & Energy-Demand Framework / Challenge Lifecycle & Logical-Time Contract",
        "M10.9.4.1 / Phase I and M10.9.5 Contextual Command Consequence Model are validated and closed. Authoritative desktop production remains integrated-operations-desktop-stable@4 with CorrelationConsistentInverseDomain thermodynamics and FourNodeBranchContinuityCorrectedCommitOptIn hydraulics at the unchanged 10 ms fixed step; exact @3 remains historical, exact @2 remains fail-closed rollback/reference, and pre-synchronization-grid-loading@3 remains the independent validated synchronization identity. M10.9.6.1 adds deterministic Application-layer challenge lifecycle only: versioned challenge identity, existing scenario/objective ownership, authored activation/observation/completion/failure references, logical-step readiness/target/deadline metadata, explicit assistance-policy declarations and a future scoring-policy identity. ScenarioChallengeTracker consumes immutable ControlRoomSnapshot evidence and accepted operator-action history through a read-only evidence seam and has no plant command or control-authority owner. Lifecycle is NOT STARTED -> READY -> ACTIVE -> COMPLETED|FAILED|CANCELLED; target windows are observational, time-based failure requires an authored hard logical-step deadline, and declared same-step failure evidence takes precedence over completion. No external demand profile, score arithmetic, challenge UI, physical coefficient, protection threshold, command type or exact-version behavior changes in M10.9.6.1."
    );
}
