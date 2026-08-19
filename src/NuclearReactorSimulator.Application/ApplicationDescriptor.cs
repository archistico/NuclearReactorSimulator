namespace NuclearReactorSimulator.Application;

/// <summary>
/// Describes the currently composed application baseline without coupling the UI to build-time constants.
/// </summary>
public sealed record ApplicationDescriptor(string ProductName, string Milestone, string Status)
{
    public static ApplicationDescriptor Current { get; } = new(
        "Nuclear Reactor Simulator",
        "M10.9.4.1-I.2 — Audit Consolidation & CI Baseline Hardening",
        "I.1 Hotfix 1 is user-validated and establishes the Phase-I exact-version compatibility baseline. H.30 remains closed as OPT-IN ONLY: exact v2 ExplicitCommittedState is still authoritative default/rollback/reference and exact v3 FourNodeBranchContinuityCorrectedCommitOptIn remains qualified opt-in. I.2 freezes validated I.1 evidence, separates ordinary/current-evidence/scheduled-long/historical-frozen audit tiers, and adds provider-backed CI entry points without rerunning frozen H.24/H.28 or changing numerical runtime. Historical H.5 hybrid and H.21 shadow modes remain source-retained retirement candidates until a later milestone removes their executable source dependencies. The 10 ms fixed step, physics, H.9/H.20/H.22 contracts, production selector and persistence semantics remain unchanged.");
}
