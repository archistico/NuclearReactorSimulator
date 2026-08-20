namespace NuclearReactorSimulator.Application;

/// <summary>
/// Describes the currently composed application candidate without coupling the UI to build-time constants.
/// </summary>
public sealed record ApplicationDescriptor(string ProductName, string Milestone, string Status)
{
    public static ApplicationDescriptor Current { get; } = new(
        "Nuclear Reactor Simulator",
        "M10.9.5.1 — Contextual Command Consequence Model / Consequence Semantics & Catalog",
        "M10.9.4.1 / Phase I is validated and closed. Authoritative desktop production remains integrated-operations-desktop-stable@4 with CorrelationConsistentInverseDomain thermodynamics, FourNodeBranchContinuityCorrectedCommitOptIn hydraulics and the unchanged 10 ms fixed step; desktop exact @3 remains historical, exact @2 remains fail-closed rollback/reference, and pre-synchronization-grid-loading@3 remains the independent validated synchronization identity. M10.9.5.1 adds a deterministic Application-only authored qualitative consequence catalog for all 27 current ControlRoomCommandKind values. Direct intent, expected influence, already-published permissive references and monitor targets remain separate semantics. Monitor references resolve to existing ControlRoomSnapshot paths or canonical whole-plant mimic elements and carry MEASURED / MODEL / canonical-state provenance. Unsupported or future command-target shapes return NO AUTHORED CONSEQUENCE MAP. The catalog does not dispatch commands, write plant state, predict numeric future values, create new permissive/protection ownership or change Avalonia UI. M10.9.5.2 dependency-chain projection is next only after build, complete ordinary tests and the focused M10.9.5.1 catalog gate are green."
    );
}
