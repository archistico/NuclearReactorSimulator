namespace NuclearReactorSimulator.Application;

/// <summary>
/// Describes the currently composed application baseline without coupling the UI to build-time constants.
/// </summary>
public sealed record ApplicationDescriptor(string ProductName, string Milestone, string Status)
{
    public static ApplicationDescriptor Current { get; } = new(
        "Nuclear Reactor Simulator",
        "M10.9.4.1-I.1 Hotfix 1 — Profile Compatibility & Legacy Retirement Inventory",
        "H.30 is user-validated and Phase H is closed as OPT-IN ONLY: exact v2 ExplicitCommittedState remains the authoritative default/rollback/reference and exact v3 FourNodeBranchContinuityCorrectedCommitOptIn remains the qualified opt-in path. I.1 begins Phase I without changing runtime behavior. It freezes validated H.30 closure evidence, inventories all 12 exact-version initial-condition factories across 9 profile IDs, retains historical exact identities required by scenario/save/replay compatibility, and classifies the old H.5 hybrid and H.21 shadow-integrated numerical modes as audit-only retirement candidates that cannot be deleted until Phase-I audit consolidation is complete. The 10 ms fixed step, numerical mathematics, protection, production selector and persistence semantics remain unchanged.");
}
