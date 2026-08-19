namespace NuclearReactorSimulator.Application;

/// <summary>
/// Describes the currently composed application candidate without coupling the UI to build-time constants.
/// </summary>
public sealed record ApplicationDescriptor(string ProductName, string Milestone, string Status)
{
    public static ApplicationDescriptor Current { get; } = new(
        "Nuclear Reactor Simulator",
        "M10.9.4.1-H.30 Requalification 1 — Production Policy Re-review after I.3 Continuity Evidence",
        "I.2 remains the last fully validated Phase-I baseline. Validated I.3 Hotfix 4 evidence established 338/338 exact-v2 generation-drop steps coincident one-for-one with targeted stop/control/admission reverse flow, while exact v3 produced zero drops and zero targeted reverse flow. Validated I.3 Hotfix 5 then ran exact v3 for the full 300-second / 30,000-step healthy reference horizon with zero generation-health or targeted-reverse-flow violations, 3,757 corrected commits, zero rollback/fallback/unsafe/untargeted disagreement and deterministic repeat. H.30 Requalification 1 therefore proposes ACTIVATE: exact v3 FourNodeBranchContinuityCorrectedCommitOptIn becomes the authoritative desktop production default and exact v2 ExplicitCommittedState remains the fail-closed rollback/reference identity. H.28 remains bounded-but-costly; H.9/H.20/H.22, P060/F040, branch-continuity limits, physical coefficients and the 10 ms fixed step are unchanged. I.3 tolerance budgets remain unfrozen until the re-review is validated and the authoritative reference baseline is rerun.");
}
