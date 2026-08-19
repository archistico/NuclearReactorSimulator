namespace NuclearReactorSimulator.Application;

/// <summary>
/// Describes the currently composed application candidate without coupling the UI to build-time constants.
/// </summary>
public sealed record ApplicationDescriptor(string ProductName, string Milestone, string Status)
{
    public static ApplicationDescriptor Current { get; } = new(
        "Nuclear Reactor Simulator",
        "M10.9.4.1-I.3 Hotfix 2 — Authoritative Production Reference Trajectory, Conservation/Inventory & Tolerance Baseline — Compact Frozen Evidence Contracts",
        "H.30 Requalification 1 is validated with ACTIVATE: exact v3 FourNodeBranchContinuityCorrectedCommitOptIn is the authoritative desktop production default and exact v2 ExplicitCommittedState remains fail-closed rollback/reference. H.28 remains bounded-but-costly and the fixed step remains 10 ms. I.3 now reruns the authoritative production selector for 300 seconds / 30,000 steps, checks generation health and targeted stop/control/admission flow direction every 10 ms, records one-second conservation/inventory samples, measures seven final-window slopes, verifies corrected telemetry and deterministic repeat, and derives 19 versioned internal regression tolerance budgets. Runtime physics and numerical mathematics are not retuned to fit the budgets. Large Gameplay/Evidence audit payloads are intentionally excluded from candidate ZIPs. Ordinary tests use compact immutable frozen evidence under eng/frozen-evidence/ordinary; omitted multi-megabyte historical traces are authenticated by eng/frozen-evidence/large-payload-manifest.csv, while decision provenance remains under eng/evidence-manifests.");
}
