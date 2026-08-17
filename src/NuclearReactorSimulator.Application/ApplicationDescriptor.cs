namespace NuclearReactorSimulator.Application;

/// <summary>
/// Describes the currently composed application baseline without coupling the UI to build-time constants.
/// </summary>
public sealed record ApplicationDescriptor(string ProductName, string Milestone, string Status)
{
    public static ApplicationDescriptor Current { get; } = new(
        "Nuclear Reactor Simulator",
        "M10.9.4.1-H.20 — Four-Node Activation Contract, Rollback & Shadow Telemetry",
        "H.20 candidate on the user-validated H.19 four-node long-horizon/cross-profile shadow qualification baseline. H.19 reproduced the frozen P060/F040 30,000-interval/four-profile census with 3,046 trigger intervals, 92 episodes and the exact 473 frozen representative keys; unchanged H.9 plus unchanged 2% pressure / 5 K bounded previous-phase hysteresis at steam/stop-out/header/turbine-inlet converged 473/473, recovered all 245 H.17 failures, preserved all 228 H.17 successes, retained 120,000 committed phase-state checks with zero committed-selection overrides, exposed no new untargeted late-shadow or selected-phase mismatch node, passed release challenges and exact deterministic repeat, and left production explicit. H.20 freezes the validated H.19 representative/metrics evidence and introduces only a shadow-only fail-closed authority supervisor: the default activation arm is disabled; untriggered or disabled observations always propose ExplicitCommittedState; an armed shadow observation may propose a corrected candidate only when H.19 qualification evidence is accepted, the corrector converged without line-search exhaustion, H.9 pressure/flow residuals remain within 1e-5 / 1e-2 kg/s, closure/ownership remain within 1e-8 kg/s / 1e-3 W and no untargeted branch disagreement is present. Any failed guard proposes immediate explicit rollback with a typed reason. The H.20 supervisor cannot authorize production commit and is not wired into PlantNetworkOrchestrator. current-v2 remains 10 ms ExplicitCommittedState; P060/F040, H.9, the four-node target set, 2%/5 K hysteresis, physical coefficients and production Resolve() remain unchanged. Phase H remains open and Phase I remains deferred." );
}
