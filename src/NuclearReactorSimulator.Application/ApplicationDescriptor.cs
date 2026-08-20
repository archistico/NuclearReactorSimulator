namespace NuclearReactorSimulator.Application;

/// <summary>
/// Describes the currently composed application candidate without coupling the UI to build-time constants.
/// </summary>
public sealed record ApplicationDescriptor(string ProductName, string Milestone, string Status)
{
    public static ApplicationDescriptor Current { get; } = new(
        "Nuclear Reactor Simulator",
        "M10.9.5.3 — Contextual Command Consequence Model / COMMANDS Context Inspector & Schematic Integration",
        "M10.9.4.1 / Phase I is validated and closed. M10.9.5.1 and M10.9.5.2 are validated. Authoritative desktop production remains integrated-operations-desktop-stable@4 with CorrelationConsistentInverseDomain thermodynamics and FourNodeBranchContinuityCorrectedCommitOptIn hydraulics at 10 ms; exact @3 remains historical, exact @2 remains fail-closed rollback/reference, and pre-synchronization-grid-loading@3 remains the independent validated synchronization identity. M10.9.5.3 integrates the validated authored command-consequence and dependency-chain projections into the existing F4 COMMANDS workstation using progressive disclosure. Selection remains non-dispatching; the operator sees current availability/blocker evidence, DIRECT EFFECT, EXPECTED INFLUENCE, WHAT TO MONITOR, a selectable dependency chain and presentation-only focus on the canonical whole-plant mimic. The mimic snapshot is reused from the existing ControlRoomPlantMimicProjector; no second topology, automatic graph traversal, numerical future prediction, new permissive/protection ownership or plant-state mutation is introduced. Blocked commands remain inspectable and the existing explicit ENTER/EXECUTE dispatch boundary remains unchanged. M10.9.5.4 observed-response evidence is next only after build, complete ordinary tests, the focused M10.9.5.3 gate and manual HMI inspection are green."
    );
}
