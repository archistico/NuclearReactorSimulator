namespace NuclearReactorSimulator.Application;

/// <summary>
/// Describes the currently composed application candidate without coupling the UI to build-time constants.
/// </summary>
public sealed record ApplicationDescriptor(string ProductName, string Milestone, string Status)
{
    public static ApplicationDescriptor Current { get; } = new(
        "Nuclear Reactor Simulator",
        "M10.9.4.1-I.5 REV1 — Final Repaired-v4 Phase-I Closure",
        "I.5 REV1 repaired exact-v4 production activation is validated. integrated-operations-desktop-stable@4 is the authoritative desktop production identity, combining CorrelationConsistentInverseDomain thermodynamics with FourNodeBranchContinuityCorrectedCommitOptIn at the unchanged 10 ms fixed step. Exact v3 remains immutable historical H.30/I.3 replay provenance and exact v2 remains fail-closed explicit rollback/reference. The pre-synchronization-grid-loading family is independent: exact @3 remains the validated corrected synchronization identity with 10 s stabilization and strict 20-60 s sustained operation. Historical I.3 exact-v3 300-second evidence, seven slopes and 19 frozen budgets remain immutable acceptance provenance. Final Phase-I closure requalifies authoritative exact @4 for 300 seconds against those unchanged budgets, then requires GameplayLong, OperationalEnvelope, ReferencePlant and cumulative closure to be green. No additional repair stage, budget retuning or historical exact-version reinterpretation is permitted. Candidate ZIPs exclude tests/.../Gameplay/Evidence, artifacts, bin and obj; compact frozen prerequisites remain under eng/frozen-evidence/ordinary and decision manifests under eng/evidence-manifests.");
}
