namespace NuclearReactorSimulator.Application;

/// <summary>
/// Describes the currently composed application candidate without coupling the UI to build-time constants.
/// </summary>
public sealed record ApplicationDescriptor(string ProductName, string Milestone, string Status)
{
    public static ApplicationDescriptor Current { get; } = new(
        "Nuclear Reactor Simulator",
        "M10.9.6.4 — Operational Challenge & Energy-Demand Framework / Initial Challenge Packs",
        "M10.9.4.1 / Phase I and M10.9.5 are validated and closed. M10.9.6.1 Challenge Lifecycle & Logical-Time Contract, M10.9.6.2 Deterministic External Energy-Demand Profiles and M10.9.6.3 Multidimensional Evaluation & Scoring Contract are validated. Authoritative desktop production remains integrated-operations-desktop-stable@4 with CorrelationConsistentInverseDomain thermodynamics and FourNodeBranchContinuityCorrectedCommitOptIn hydraulics at the unchanged 10 ms fixed step. M10.9.6.4 composes six versioned Application-layer operational challenge packs from existing M7.2/M7.5/M7.6 scenario/check owners and the existing M8.4 generator-trip/load-rejection fault owner. Exact pack identities cover pre-start circulation preparation, synchronization/initial loading, bounded 5-to-10-to-5 MWe demand-following, post-load-change 10 MWe stabilization, controlled normal shutdown and generator-trip/load-rejection response. Only bounded demand-following exposes the next scheduled demand change; post-load-change stabilization exposes only its current target and synchronization owns no demand profile. External demand remains evidence only and never writes generator requested load. Score evidence bindings document one source per exact policy dimension but perform no score arithmetic. Normal-operation trip/procedure failure semantics are challenge-owned; the generator trip is required evidence rather than failure in the load-rejection challenge. No hard failure deadlines are authored before M10.9.6.5 runtime qualification. The pack/evaluator layer owns no dispatcher, runtime engine, controller, protection mutation, wall-clock or Simulation authority and adds no new fault or physics."
    );
}
