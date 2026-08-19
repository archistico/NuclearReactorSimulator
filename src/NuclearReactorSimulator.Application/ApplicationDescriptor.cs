namespace NuclearReactorSimulator.Application;

/// <summary>
/// Describes the currently composed application baseline without coupling the UI to build-time constants.
/// </summary>
public sealed record ApplicationDescriptor(string ProductName, string Milestone, string Status)
{
    public static ApplicationDescriptor Current { get; } = new(
        "Nuclear Reactor Simulator",
        "M10.9.4.1-I.3 — Reference Trajectories, Conservation/Inventory Baseline & Tolerance Budgets — Hotfix 4 Explicit-vs-Corrected Branch Discontinuity Comparison — Classifier Fix 1 Targeted-Train Reverse-Flow Classification",
        "I.2 is user-validated and remains the authoritative Phase-I baseline. H.30 remains closed as OPT-IN ONLY: exact v2 ExplicitCommittedState is still authoritative default/rollback/reference and exact v3 FourNodeBranchContinuityCorrectedCommitOptIn remains qualified opt-in. The I.3 300-second healthy reference journey completed and exposed five isolated exact-v2 shaft-power drops, each coincident with zero turbine stage flow, reverse admission flow and a turbine-inlet pressure spike while trips and conservation remained green. Hotfix 4 Classifier Fix 1 does not weaken the shaft-health floor or change runtime behavior; the completed 10 ms comparison found 338 exact-v2 generation-drop steps, of which 8 coincide with reverse stop-valve flow and 330 with reverse admission flow, while exact v3 has zero targeted-train reverse-flow steps and zero drops. The classifier therefore evaluates reverse flow across the whole targeted stop/control/admission train rather than admission alone, while retaining the same four-node pressure, valve/stage-flow, final-window slopes context and corrected-commit telemetry. The comparison is diagnostic-only and must precede any reconsideration of H.30 policy or freezing of I.3 tolerance budgets. H.24/H.28 are not rerun, legacy H.5/H.21 modes remain source-retained, and the 10 ms fixed step, physics, H.9/H.20/H.22 contracts, production selector and persistence semantics remain unchanged.");
}
