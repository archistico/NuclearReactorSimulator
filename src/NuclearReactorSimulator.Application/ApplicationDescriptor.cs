namespace NuclearReactorSimulator.Application;

/// <summary>
/// Describes the currently composed application candidate without coupling the UI to build-time constants.
/// </summary>
public sealed record ApplicationDescriptor(string ProductName, string Milestone, string Status)
{
    public static ApplicationDescriptor Current { get; } = new(
        "Nuclear Reactor Simulator",
        "M10.9.5.4 — Contextual Command Consequence Model / Observed Response Evidence",
        "M10.9.4.1 / Phase I is validated and closed. M10.9.5.1 consequence semantics, M10.9.5.2 dependency-chain projection and M10.9.5.3 COMMANDS/context-inspector/schematic integration are validated. Authoritative desktop production remains integrated-operations-desktop-stable@4 with CorrelationConsistentInverseDomain thermodynamics and FourNodeBranchContinuityCorrectedCommitOptIn hydraulics at 10 ms; exact @3 remains historical, exact @2 remains fail-closed rollback/reference, and pre-synchronization-grid-loading@3 remains the independent validated synchronization identity. M10.9.5.4 adds presentation-only OBSERVED RESPONSE evidence after F4 COMMANDS dispatch. The command's authored M10.9.5.1 monitor set is sampled at the dispatch boundary and through a bounded 500-logical-step window; baseline/latest values, actual numeric delta or state-change direction, accepted/rejected feedback and observed protection state are displayed without generic SUCCESS/FAILURE or causal attribution. Rejected commands show no fictional plant effects. Observation samples are derivable JsonIgnored presentation evidence and do not alter replay/save fingerprints, plant physics, command dispatch, permissive/protection ownership or exact-version behavior. M10.9.5.5 closure is next only after build, complete ordinary tests and the focused M10.9.5.4 evidence gate are green."
    );
}
