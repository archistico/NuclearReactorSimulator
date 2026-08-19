namespace NuclearReactorSimulator.Application;

/// <summary>
/// Describes the currently composed application baseline without coupling the UI to build-time constants.
/// </summary>
public sealed record ApplicationDescriptor(string ProductName, string Milestone, string Status)
{
    public static ApplicationDescriptor Current { get; } = new(
        "Nuclear Reactor Simulator",
        "M10.9.4.1-H.30 — Phase H Closure & Production Qualification Decision",
        "H.29 is user-validated as a production activation candidate: exact v3 corrected ownership produced 400/400 qualified commits with zero rollback/fallback/unsafe/untargeted disagreement, deterministic repeat, exact replay/checkpoint compatibility and an explicit deployment kill back to exact v2 ExplicitCommittedState. H.30 introduces no new numerical algorithm, retuning or production-selector change. It freezes the validated H.19-H.29 evidence chain and proposes the evidence-derived Phase H closure decision OPT-IN ONLY: corrected ownership is technically qualified for opt-in production use, but H.28 remains bounded-but-costly with median wall-cost ratio 4.6214685710690242 and p95 ratio 10.684444741413872, so exact v2 ExplicitCommittedState remains the authoritative default and rollback/reference while exact v3 FourNodeBranchContinuityCorrectedCommitOptIn remains the qualified opt-in path. A green H.30 gate closes Phase H and unblocks Phase I.");
}
