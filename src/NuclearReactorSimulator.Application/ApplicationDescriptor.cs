namespace NuclearReactorSimulator.Application;

/// <summary>
/// Describes the currently composed application baseline without coupling the UI to build-time constants.
/// </summary>
public sealed record ApplicationDescriptor(string ProductName, string Milestone, string Status)
{
    public static ApplicationDescriptor Current { get; } = new(
        "Nuclear Reactor Simulator",
        "M10.9.4.1-H.29 — Production Activation Candidate",
        "H.28 is user-validated with the corrected path classified bounded-but-costly, and H.24 Requalification 1 is user-validated over the unchanged 30,000-interval/four-profile committed domain after the H.28.1 optimization branch: 9,626/9,626 corrected commits, zero rollback/fallback/unsafe/untargeted disagreement, deterministic repeat and fingerprint 7AF233CE51A866B3E00C2C032AA58EEFBD7290DE0940725E5F4B7860EA6287BE. H.29 introduces no new numerical algorithm or retuning. It adds exact initial-condition v3 as a separately reviewed FourNodeBranchContinuityCorrectedCommitOptIn production-default candidate, preserves v2 ExplicitCommittedState as the authoritative default and explicit deployment rollback/kill reference, adds internal numerical telemetry counters, and qualifies save/replay/checkpoint version compatibility without exposing numerical diagnostics to the operator UI. H.30 remains the sole authority for the final ACTIVATE / OPT-IN ONLY / REMAIN EXPLICIT Phase H decision.");
}
