namespace NuclearReactorSimulator.Application;

/// <summary>
/// Describes the currently composed application candidate without coupling the UI to build-time constants.
/// </summary>
public sealed record ApplicationDescriptor(string ProductName, string Milestone, string Status)
{
    public static ApplicationDescriptor Current { get; } = new(
        "Nuclear Reactor Simulator",
        "M10.9.5.2 — Contextual Command Consequence Model / Explicit Dependency-Chain Projection",
        "M10.9.4.1 / Phase I is validated and closed. M10.9.5.1 is the validated post-Phase-I baseline. Authoritative desktop production remains integrated-operations-desktop-stable@4 with CorrelationConsistentInverseDomain thermodynamics and FourNodeBranchContinuityCorrectedCommitOptIn hydraulics at 10 ms; exact @3 remains historical, exact @2 remains fail-closed rollback/reference, and pre-synchronization-grid-loading@3 remains the independent validated synchronization identity. M10.9.5.2 adds an Application-only authored bounded dependency-chain projection over the validated M10.9.5.1 command-consequence catalog. Every chain distinguishes COMMAND INTENT, CONTROL/ACTUATOR STATE, PHYSICAL PROCESS PATH, MEASUREMENT/MODEL OBSERVATION and PROTECTION/ALARM RELATION. Static topology references are limited to already-published whole-plant mimic elements/connections and ControlRoomSnapshot paths; targeted-device references remain the canonical typed command target. No automatic graph traversal, dispatch, plant-state mutation, predictive numerical physics, new permissive/protection ownership or Avalonia integration is introduced. Unknown or invalid command-target shapes fail closed with NO AUTHORED DEPENDENCY CHAIN. M10.9.5.3 COMMANDS/context-inspector/schematic integration is next only after build, complete ordinary tests and the focused M10.9.5.2 dependency-chain gate are green."
    );
}
