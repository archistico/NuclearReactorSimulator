namespace NuclearReactorSimulator.Application;

/// <summary>
/// Describes the currently composed application candidate without coupling the UI to build-time constants.
/// </summary>
public sealed record ApplicationDescriptor(string ProductName, string Milestone, string Status)
{
    public static ApplicationDescriptor Current { get; } = new(
        "Nuclear Reactor Simulator",
        "M10.9.4.1-I.4 Hotfix 2 — Known Limitations & Legacy Retirement Review / Canonical Frozen-Evidence Contract Alignment",
        "I.3 Hotfix 2 is validated: exact v3 FourNodeBranchContinuityCorrectedCommitOptIn remains the authoritative production default, exact v2 ExplicitCommittedState remains fail-closed rollback/reference, and the 300-second production reference freezes seven final-window slopes plus 19 regression budgets. I.4 reconciles those measured drifts with current known limitations and reviews H.5 DeterministicHybridSemiImplicit plus H.21 FourNodeBranchContinuityShadowIntegrated for retirement. Both legacy modes have no production, exact-version or current-CI dependency, but each still has four source and four test files that preserve executable historical seams, so source removal is deferred rather than performed inside Phase-I closure. H.28 remains bounded-but-costly; the fixed step remains 10 ms; runtime physics and numerical mathematics are unchanged. Candidate ZIPs continue to exclude tests/.../Gameplay/Evidence and use compact frozen evidence under eng/frozen-evidence/ordinary plus decision manifests under eng/evidence-manifests.");
}
