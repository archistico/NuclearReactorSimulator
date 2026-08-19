namespace NuclearReactorSimulator.Application;

/// <summary>
/// Describes the currently composed application baseline without coupling the UI to build-time constants.
/// </summary>
public sealed record ApplicationDescriptor(string ProductName, string Milestone, string Status)
{
    public static ApplicationDescriptor Current { get; } = new(
        "Nuclear Reactor Simulator",
        "M10.9.4.1-H.24 Requalification 1 — Post-H.28 Committed Long-Horizon & Cross-Profile Regression",
        "H.28 is user-validated after the H.28.1 optimization branch: median wall-cost ratio 4.6215 <= 8, p95 ratio 10.6844 <= 12, allocation ratio 1.1164 <= 16, 379/379 soak trigger/commit, zero rollback/fallback/unsafe/untargeted disagreement, and deterministic fingerprint 518BA948637F0C270C7F8228AB97FEF9148E29A4F5CE4376319AB5D1CFBE7F38. The corrected path is classified bounded-but-costly. H.24 Requalification 1 makes no numerical runtime change; it reruns the original 30,000-interval/four-profile H.24 committed domain once against the stabilized H.28 runtime because H.28.1 changed committed-runtime implementation code. Standard factories remain ExplicitCommittedState at 10 ms; FourNodeBranchContinuityCorrectedCommitOptIn remains separately opt-in. H.9, H.20, H.22, P060/F040, 2%/5 K hysteresis, the four-node target set and physical coefficients are unchanged. H.29 remains blocked until this post-optimization H.24 regression passes.");
}
