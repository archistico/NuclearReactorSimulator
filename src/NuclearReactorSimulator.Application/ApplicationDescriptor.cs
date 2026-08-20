namespace NuclearReactorSimulator.Application;

/// <summary>
/// Describes the currently composed application candidate without coupling the UI to build-time constants.
/// </summary>
public sealed record ApplicationDescriptor(string ProductName, string Milestone, string Status)
{
    public static ApplicationDescriptor Current { get; } = new(
        "Nuclear Reactor Simulator",
        "M10.9.6.3 — Operational Challenge & Energy-Demand Framework / Multidimensional Evaluation & Scoring Contract",
        "M10.9.4.1 / Phase I, M10.9.5 Contextual Command Consequence Model, M10.9.6.1 Challenge Lifecycle & Logical-Time Contract and M10.9.6.2 Deterministic External Energy-Demand Profiles are validated. Authoritative desktop production remains integrated-operations-desktop-stable@4 with CorrelationConsistentInverseDomain thermodynamics and FourNodeBranchContinuityCorrectedCommitOptIn hydraulics at the unchanged 10 ms fixed step. M10.9.6.3 adds deterministic Application-layer observational scoring only. Standard exact policies are general-operations@1 (SAFETY 45, PROCEDURE 30, STABILITY 20, LOGICAL TIME 5) and demand-following@1 (SAFETY 40, PROCEDURE 25, STABILITY 15, DEMAND 15, LOGICAL TIME 5). Pass/proficient/excellent thresholds are 60/75/90 percent. Authored critical safety failure dominates and caps the result at 39 percent; authored critical procedure failure caps at 59 percent; unavailable required evidence makes evaluation incomplete and non-passing. A trip/protection action is not globally classified as challenge failure. Standard guidance and plant-control-authority modifiers are explicitly neutral 1.00; any non-neutral modifier must be versioned policy-owned. ChallengeScoreCalculator owns no dispatcher, controller, protection, wall-clock or Simulation mutation path. No challenge pack, challenge UI, new fault, control retuning, physics or exact-version behavior changes in M10.9.6.3."
    );
}
