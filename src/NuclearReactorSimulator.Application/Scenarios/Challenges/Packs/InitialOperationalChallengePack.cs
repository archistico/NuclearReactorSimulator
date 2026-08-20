using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Challenges.Demand;
using NuclearReactorSimulator.Application.Scenarios.Challenges.Scoring;
using NuclearReactorSimulator.Application.Scenarios.Faults.SecondaryTransients;
using NuclearReactorSimulator.Application.Scenarios.Operations;
using NuclearReactorSimulator.Application.Scenarios.PreStartup;
using NuclearReactorSimulator.Application.Scenarios.Synchronization;
using NuclearReactorSimulator.Application.Scenarios.Training;

namespace NuclearReactorSimulator.Application.Scenarios.Challenges.Packs;

/// <summary>
/// M10.9.6.4 initial normal-operation challenge catalog. Every entry composes previously validated scenario/plant owners;
/// the pack adds authored training contracts and read-only evidence only.
/// </summary>
public static class InitialOperationalChallengePack
{
    private static readonly TrainingGuidanceMode[] AllGuidanceModes = Enum.GetValues<TrainingGuidanceMode>();
    private static readonly StandardOperationalChallengeConditionEvaluator SharedEvaluator = new();

    public static OperationalChallengePackDefinition PreStartupPreparation { get; } = Create(
        "pre-start-circulation-preparation",
        ColdShutdownPreStartupProgram.Scenario,
        new ChallengeDefinition(
            "pre-start-circulation-preparation",
            1,
            ColdShutdownPreStartupProgram.Scenario.ScenarioId,
            "prepare-circulation",
            "Prepare Main Circulation",
            "Verify the validated cold-shutdown baseline, start main circulation through the canonical command seam and preserve shutdown/isolation for the pre-criticality handoff.",
            Condition("prestart:safe-baseline", "Safe cold-shutdown baseline", "Validated M7.2 cold-shutdown, protection, isolation and instrumentation checks are satisfied."),
            new[] { Condition("prestart:pump-start-observed", "Pump-start action observed", "An accepted MAIN CIRCULATION PUMP START operator action is present in deterministic action history.") },
            new[] { Condition("prestart:precriticality-handoff", "Pre-criticality handoff established", "Main circulation is running while shutdown, steam isolation and generator isolation remain intact.") },
            new[] { Condition("prestart:unexpected-trip", "Unexpected trip during normal preparation", "A canonical protection trip is active during a normal pre-start preparation challenge.") },
            Window(20, 1_000),
            Assistance(StandardChallengeScoringPolicies.GeneralOperationsV1)),
        StandardChallengeScoringPolicies.GeneralOperationsV1,
        GeneralBindings("pre-start-circulation-preparation"));

    public static OperationalChallengePackDefinition SynchronizationInitialLoading { get; } = Create(
        "synchronization-initial-loading",
        GridSynchronizationLoadProgram.Scenario,
        new ChallengeDefinition(
            "synchronization-initial-loading",
            1,
            GridSynchronizationLoadProgram.Scenario.ScenarioId,
            "stabilize-low-load",
            "Synchronize and Establish Initial Load",
            "Use the validated synchronization close-check and generator load command seams to establish a stable 5 MWe initial-load handoff without bypassing canonical electrical ownership.",
            Condition("sync:ready-to-synchronize", "Synchronization handoff ready", "Validated M7.5 pre-synchronization checks, including the canonical synchronization window, are satisfied."),
            new[] { Condition("sync:breaker-close-observed", "Breaker-close action observed", "An accepted GENERATOR BREAKER CLOSE action is present in deterministic action history.") },
            new[] { Condition("sync:stable-low-load-handoff", "Stable low-load handoff", "The validated M7.5 stable low-load handoff and coordinated power checks are satisfied.") },
            new[] { Condition("sync:unexpected-trip", "Unexpected trip during normal synchronization", "A canonical protection trip is active during the normal synchronization/initial-loading challenge.") },
            Window(20, 3_000),
            Assistance(StandardChallengeScoringPolicies.GeneralOperationsV1)),
        StandardChallengeScoringPolicies.GeneralOperationsV1,
        GeneralBindings("synchronization-initial-loading"));

    public static OperationalChallengePackDefinition BoundedDemandFollowing { get; } = Create(
        "bounded-demand-following-5-10-5",
        PowerManoeuvringNormalShutdownProgram.Scenario,
        new ChallengeDefinition(
            "bounded-demand-following-5-10-5",
            1,
            PowerManoeuvringNormalShutdownProgram.Scenario.ScenarioId,
            "manoeuvre-power",
            "Follow a Bounded 5→10→5 MWe Demand Profile",
            "Follow a deterministic external demand reference through validated operator command seams. External demand remains training evidence and never writes generator requested load.",
            Condition("demand:stable-low-load-start", "Stable low-load start", "The validated M7.6 low-load parallel handoff is established before the external-demand sequence begins."),
            new[] { Condition("demand:raise-and-lower-actions-observed", "Raise/lower actions observed", "Accepted generator-load raise and lower actions are both present in deterministic operator-action history.") },
            new[] { Condition("demand:return-to-five-mwe-stable", "Return to stable 5 MWe operation", "Gross output has returned to the validated 5 MWe band with synchronous-speed stability, closed breaker and no trip.") },
            new[] { Condition("demand:unexpected-trip", "Unexpected trip during normal demand-following", "A canonical protection trip is active during normal demand-following.") },
            Window(4_000, 8_000),
            Assistance(StandardChallengeScoringPolicies.DemandFollowingV1),
            ExternalEnergyDemandProfileDefinition.Piecewise(
                "bounded-demand-5-10-5",
                1,
                0d,
                10d,
                new[]
                {
                    new ExternalEnergyDemandControlPoint(0, 5d),
                    new ExternalEnergyDemandControlPoint(500, 10d),
                    new ExternalEnergyDemandControlPoint(3_000, 5d),
                },
                exposeNextScheduledChange: true)),
        StandardChallengeScoringPolicies.DemandFollowingV1,
        DemandBindings("bounded-demand-following-5-10-5"));

    public static OperationalChallengePackDefinition PostLoadChangeStabilization { get; } = Create(
        "post-load-change-stabilization",
        PowerManoeuvringNormalShutdownProgram.Scenario,
        new ChallengeDefinition(
            "post-load-change-stabilization",
            1,
            PowerManoeuvringNormalShutdownProgram.Scenario.ScenarioId,
            "observe-feedback",
            "Stabilize After a 5 MWe Load Raise",
            "After an accepted generator-load raise, stabilize the existing validated plant near 10 MWe while observing published thermal/void feedback. The challenge does not command the load change itself.",
            Condition("stabilize:load-raise-observed", "Load-raise action observed", "An accepted GENERATOR LOAD RAISE action activates the stabilization challenge."),
            new[] { Condition("stabilize:thermal-feedback-observed", "Thermal and void feedback observable", "Validated M7.6 temperature and void diagnostics are both observable.") },
            new[] { Condition("stabilize:ten-mwe-stable", "10 MWe stabilized", "Gross output is in the validated 10 MWe band with synchronous-speed stability, closed breaker and no trip.") },
            new[] { Condition("stabilize:unexpected-trip", "Unexpected trip during stabilization", "A canonical protection trip is active during normal post-load-change stabilization.") },
            Window(100, 3_000),
            Assistance(StandardChallengeScoringPolicies.DemandFollowingV1),
            ExternalEnergyDemandProfileDefinition.Constant(
                "post-load-change-10mwe-target",
                1,
                10d,
                10.1d,
                exposeNextScheduledChange: false)),
        StandardChallengeScoringPolicies.DemandFollowingV1,
        DemandBindings("post-load-change-stabilization"));

    public static OperationalChallengePackDefinition ControlledNormalShutdown { get; } = Create(
        "controlled-normal-shutdown",
        PowerManoeuvringNormalShutdownProgram.Scenario,
        new ChallengeDefinition(
            "controlled-normal-shutdown",
            1,
            PowerManoeuvringNormalShutdownProgram.Scenario.ScenarioId,
            "normal-shutdown",
            "Perform a Controlled Normal Shutdown",
            "Unload, disconnect, insert rods and preserve post-shutdown circulation through validated command seams without substituting emergency trip actions for routine procedure.",
            Condition("shutdown:stable-low-load-start", "Stable low-load start", "The validated M7.6 low-load parallel handoff is established before shutdown actions begin."),
            new[] { Condition("shutdown:normal-action-sequence-observed", "Normal shutdown actions observed", "Accepted load-lower, breaker-open and rod-insert actions are all present in deterministic action history.") },
            new[] { Condition("shutdown:post-shutdown-cooling", "Post-shutdown cooling established", "The validated M7.6 post-shutdown cooling and temperature-observation checks are satisfied.") },
            new[] { Condition("shutdown:emergency-action-used", "Emergency action substituted for routine shutdown", "SCRAM, turbine trip or generator trip was used as an accepted operator action during this normal-procedure challenge.") },
            Window(500, 8_000),
            Assistance(StandardChallengeScoringPolicies.GeneralOperationsV1)),
        StandardChallengeScoringPolicies.GeneralOperationsV1,
        GeneralBindings("controlled-normal-shutdown"));

    public static OperationalChallengePackDefinition GeneratorTripLoadRejectionRecovery { get; } = Create(
        "generator-trip-load-rejection-recovery",
        SecondaryTransientScenarioPack.GeneratorTripLoadRejection,
        new ChallengeDefinition(
            "generator-trip-load-rejection-recovery",
            1,
            SecondaryTransientScenarioPack.GeneratorTripLoadRejection.ScenarioId,
            "observe-load-rejection",
            "Stabilize the Generator-Trip / Load-Rejection Response",
            "Recognize the already-supported deterministic generator-trip transient, verify canonical generator isolation and record alarm acknowledgement while preserving challenge/protection ownership boundaries.",
            Condition("load-rejection:fault-active", "Generator-trip fault active", "The existing M8.4 generator-trip fault is active in the committed fault snapshot."),
            new[]
            {
                Condition("load-rejection:generator-trip-observed", "Generator trip observed", "The canonical generator-trip latch is active."),
                Condition("load-rejection:breaker-open", "Generator isolation observed", "All canonical generator breakers are open."),
                Condition("load-rejection:alarm-acknowledgement-observed", "Alarm acknowledgement recorded", "An accepted alarm acknowledgement action is present in deterministic action history."),
            },
            new[] { Condition("load-rejection:isolated-response-stable", "Isolated post-trip response established", "Generator trip remains explicit, generator isolation is established and alarm acknowledgement has been recorded.") },
            Array.Empty<ChallengeConditionDefinition>(),
            Window(1, 1_500),
            Assistance(StandardChallengeScoringPolicies.GeneralOperationsV1)),
        StandardChallengeScoringPolicies.GeneralOperationsV1,
        GeneralBindings("generator-trip-load-rejection-recovery"));

    public static IReadOnlyList<OperationalChallengePackDefinition> All { get; } = new[]
    {
        PreStartupPreparation,
        SynchronizationInitialLoading,
        BoundedDemandFollowing,
        PostLoadChangeStabilization,
        ControlledNormalShutdown,
        GeneratorTripLoadRejectionRecovery,
    };

    private static OperationalChallengePackDefinition Create(
        string id,
        ScenarioDefinition scenario,
        ChallengeDefinition challenge,
        ChallengeScoringPolicyDefinition policy,
        IEnumerable<OperationalChallengeScoreEvidenceBinding> bindings)
        => new(id, 1, scenario, challenge, SharedEvaluator, policy, bindings);

    private static ChallengeConditionDefinition Condition(string id, string title, string description)
        => new(id, title, description);

    private static ChallengeLogicalTimeContract Window(long start, long end)
        => new(0, start, end, null);

    private static ChallengeAssistancePolicy Assistance(ChallengeScoringPolicyDefinition policy)
        => new(AllGuidanceModes, policy.ExactId);

    private static OperationalChallengeScoreEvidenceBinding[] GeneralBindings(string packId)
        => new[]
        {
            Binding(ChallengeScoreDimensionKind.SafetyProtectionDiscipline, packId, "safety", "Committed protection/trip state plus challenge-authored safety failure conditions."),
            Binding(ChallengeScoreDimensionKind.ProcedureRequiredActions, packId, "procedure", "Deterministic accepted operator-action history plus authored required/completion conditions."),
            Binding(ChallengeScoreDimensionKind.StabilityOperatingQuality, packId, "stability", "Immutable control-room snapshot stability and operating-quality observations."),
            Binding(ChallengeScoreDimensionKind.LogicalTimeCompletionEfficiency, packId, "logical-time", "Challenge lifecycle logical-step activation, target window and terminal-step evidence."),
        };

    private static OperationalChallengeScoreEvidenceBinding[] DemandBindings(string packId)
        => GeneralBindings(packId)
            .Append(Binding(
                ChallengeScoreDimensionKind.DemandTracking,
                packId,
                "demand",
                "Versioned external-demand profile versus immutable actual electrical-output timeline; never generator-request mutation."))
            .ToArray();

    private static OperationalChallengeScoreEvidenceBinding Binding(
        ChallengeScoreDimensionKind kind,
        string packId,
        string suffix,
        string description)
        => new(kind, $"{packId}:{suffix}", description);
}
