namespace NuclearReactorSimulator.Application;

/// <summary>
/// Describes the currently composed application baseline without coupling the UI to build-time constants.
/// </summary>
public sealed record ApplicationDescriptor(string ProductName, string Milestone, string Status)
{
    public static ApplicationDescriptor Current { get; } = new(
        "Nuclear Reactor Simulator",
        "M10.9.4.1-I.3 — Reference Trajectories, Conservation/Inventory Baseline & Tolerance Budgets — Hotfix 5 Corrected 300 s Healthy Reference Requalification",
        "I.2 is user-validated and remains the authoritative Phase-I baseline. H.30 remains closed as OPT-IN ONLY: exact v2 ExplicitCommittedState is still authoritative default/rollback/reference and exact v3 FourNodeBranchContinuityCorrectedCommitOptIn remains qualified opt-in. The validated I.3 Hotfix 4 Classifier Fix 1 evidence established 338/338 exact-v2 generation drops coincident one-for-one with targeted stop/control/admission reverse flow, while exact v3 produced zero drops and zero targeted-train reverse-flow steps with 1,791 corrected commits and zero rollback/fallback/unsafe/untargeted disagreement over 100 seconds. Hotfix 5 does not change runtime behavior or H.30 policy: it extends exact v3 to the full 300-second healthy reference horizon, checks generation health and targeted-train direction at every 10 ms step, samples conservation/inventory and final-window slopes every second, and performs a separate deterministic control. I.3 tolerance budgets remain unfrozen. A green Hotfix 5 may only unblock a separate H.30 production-policy re-review; it does not activate v3 by itself. H.24/H.28 are not rerun, legacy H.5/H.21 modes remain source-retained, and the 10 ms fixed step, physics, H.9/H.20/H.22 contracts, production selector and persistence semantics remain unchanged.");
}
