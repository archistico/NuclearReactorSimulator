using System.Security.Cryptography;
using System.Text;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios.Recording;
using NuclearReactorSimulator.Application.Scenarios.Faults.SecondaryTransients;
using NuclearReactorSimulator.Application.Scenarios.Training;
using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Plant;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay;

/// <summary>
/// I.5 REV1 Hotfix 12 repaired-closure long-horizon/cross-profile requalification stage 2.
/// Reuses the validated H.19/H.24 four-profile 30,000-interval domain through the Hotfix 10
/// CorrelationConsistentInverseDomain evidence seam with real corrected-commit ownership.
/// No registered exact-version identity or production selector is changed.
/// </summary>
public sealed class PhaseIThermodynamicRepairLongHorizonCrossProfileRequalificationAuditTests
{
    private static readonly TimeSpan Step = TimeSpan.FromMilliseconds(10d);
    private static readonly Encoding Utf8WithoutBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    private const int DeterminismControlIntervals = 256;
    private const double MaximumMassClosureResidualKilograms = 1e-6d;
    private const double MaximumEnergyClosureResidualJoules = 1e-2d;
    private const double MaximumBalanceMassRateResidualKilogramsPerSecond = 1e-8d;
    private const double MaximumBalancePowerResidualWatts = 1e-3d;

    private static readonly ProfileDefinition[] Profiles =
    {
        new("steady-long", 12_000, ProfileKind.Steady),
        new("load-pulse", 6_000, ProfileKind.LoadPulse),
        new("cooling-pulse", 6_000, ProfileKind.CoolingPulse),
        new("combined-load-cooling", 6_000, ProfileKind.CombinedLoadCooling),
    };


    [Fact(Explicit = true)]
    [Trait("Category", "PhaseIThermodynamicRepairLongHorizonCrossProfileRequalification")]
    public void RepairedClosure_CorrectedCommitRuntime_RequalifiesLongHorizonAndCrossProfileOperation()
    {
        ResetProgress();
        var profileResults = new List<ProfileResult>(Profiles.Length);

        foreach (var profile in Profiles)
        {
            WriteProgress($"profile-start id={profile.Id} intervals={profile.IntervalCount}");
            profileResults.Add(RunProfile(profile));
            WriteProgress($"profile-complete id={profile.Id}");
        }

        WriteProgress($"determinism-control-start intervals={DeterminismControlIntervals}");
        var determinismFirst = RunDeterminismControl();
        var determinismSecond = RunDeterminismControl();
        var deterministicControlRepeat = string.Equals(
            Fingerprint(determinismFirst),
            Fingerprint(determinismSecond),
            StringComparison.Ordinal);
        Assert.True(deterministicControlRepeat, "I.5 repaired-closure Stage-2 determinism control did not repeat exactly.");
        WriteProgress($"determinism-control-complete repeat={deterministicControlRepeat}");

        var rows = profileResults.SelectMany(static result => result.Rows).ToArray();
        var totalQualificationIntervals = Profiles.Sum(static profile => profile.IntervalCount);
        var actionTransitionSteps = rows.Count(static row => row.IsActionTransitionStep);
        var triggered = rows.Count(static row => row.TriggerObserved);
        var eligible = rows.Count(static row => row.H20CandidateEligible);
        var authorized = rows.Count(static row => row.CorrectedCommitAuthorized);
        var commits = rows.Count(static row => row.CorrectedCandidateCommitted);
        var rollbacks = rows.Count(static row => row.RollbackRequired);
        var fallbackIntervals = rows.Count(static row => row.TriggerObserved && !row.CorrectedCandidateCommitted);
        var fallbackCommitViolations = rows.Count(static row => (!row.H20CandidateEligible || row.RollbackRequired) && row.CorrectedCandidateCommitted);
        var unsafeCommits = rows.Count(static row => row.CorrectedCandidateCommitted && !CommitIsQualified(row));
        var untargetedDisagreements = rows.Count(static row => row.UntargetedBranchDisagreementDetected);
        var branchOverridesTotal = rows.Sum(static row => (long)row.BranchOverrideCount);
        var previousPhaseHoldsTotal = rows.Sum(static row => (long)row.PreviousPhaseHoldCount);
        var hysteresisReleasesTotal = rows.Sum(static row => (long)row.HysteresisReleaseCount);
        var continuityActiveSteps = rows.Count(static row =>
            row.BranchOverrideCount > 0 || row.PreviousPhaseHoldCount > 0 || row.HysteresisReleaseCount > 0);
        var postStartupContinuityActiveSteps = rows.Count(static row =>
            row.Interval > 2
            && (row.BranchOverrideCount > 0 || row.PreviousPhaseHoldCount > 0 || row.HysteresisReleaseCount > 0));
        var triggersAfterStartup = rows.Count(static row => row.Interval > 2 && row.TriggerObserved);
        var commitsAfterStartup = rows.Count(static row => row.Interval > 2 && row.CorrectedCandidateCommitted);
        var maximumMassClosure = rows.Max(static row => row.MassClosureResidualKilograms);
        var maximumEnergyClosure = rows.Max(static row => row.EnergyClosureResidualJoules);
        var maximumBalanceMassRate = rows.Max(static row => row.BalanceMassRateResidualKilogramsPerSecond);
        var maximumBalancePower = rows.Max(static row => row.BalancePowerResidualWatts);
        var telemetryFingerprint = Fingerprint(rows);

        Assert.Equal(30_000, totalQualificationIntervals);
        Assert.Equal(8, actionTransitionSteps);
        Assert.Equal(0, fallbackCommitViolations);
        Assert.Equal(0, unsafeCommits);
        Assert.Equal(0, untargetedDisagreements);
        Assert.True(triggered > 0, "The repaired long-horizon domain never exercised the P060/F040 authority seam.");
        Assert.True(commits > 0, "The repaired long-horizon domain never exercised corrected ownership.");
        Assert.True(authorized >= commits);
        Assert.True(eligible >= authorized);
        Assert.InRange(maximumMassClosure, 0d, MaximumMassClosureResidualKilograms);
        Assert.InRange(maximumEnergyClosure, 0d, MaximumEnergyClosureResidualJoules);
        Assert.InRange(maximumBalanceMassRate, 0d, MaximumBalanceMassRateResidualKilogramsPerSecond);
        Assert.InRange(maximumBalancePower, 0d, MaximumBalancePowerResidualWatts);

        var passes = profileResults.All(static result => result.CompletedWithoutTrip)
            && triggered > 0
            && commits > 0
            && fallbackCommitViolations == 0
            && unsafeCommits == 0
            && untargetedDisagreements == 0
            && deterministicControlRepeat
            && maximumMassClosure <= MaximumMassClosureResidualKilograms
            && maximumEnergyClosure <= MaximumEnergyClosureResidualJoules
            && maximumBalanceMassRate <= MaximumBalanceMassRateResidualKilogramsPerSecond
            && maximumBalancePower <= MaximumBalancePowerResidualWatts;
        Assert.True(passes);

        WriteReports(
            profileResults,
            rows,
            totalQualificationIntervals,
            actionTransitionSteps,
            triggered,
            eligible,
            authorized,
            commits,
            rollbacks,
            fallbackIntervals,
            fallbackCommitViolations,
            unsafeCommits,
            untargetedDisagreements,
            branchOverridesTotal,
            previousPhaseHoldsTotal,
            hysteresisReleasesTotal,
            continuityActiveSteps,
            postStartupContinuityActiveSteps,
            triggersAfterStartup,
            commitsAfterStartup,
            deterministicControlRepeat,
            telemetryFingerprint,
            maximumMassClosure,
            maximumEnergyClosure,
            maximumBalanceMassRate,
            maximumBalancePower,
            passes);
    }

    private static ProfileResult RunProfile(ProfileDefinition profile)
    {
        var engine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            DesktopSustainedGenerationInitialConditionFactory.CreateThermodynamicInverseDomainRepairEvidenceRuntimeEngine(Step, useFourNodeCorrectedCommit: true));
        Assert.Equal(HydraulicNumericalCouplingMode.FourNodeBranchContinuityCorrectedCommitOptIn, CurrentHydraulics(engine).Mode);

        var initialPresentation = engine.CreatePresentationSnapshot(ControlRoomRunState.Running);
        var generatorId = Assert.Single(initialPresentation.Electrical.Generators).GeneratorId;
        var coolingTarget = Assert.IsAssignableFrom<ISecondaryTransientFaultTarget>(engine);
        var rows = new List<StepTelemetryRow>(profile.IntervalCount + 4);
        var runtimeStep = 0;

        for (var interval = 1; interval <= profile.IntervalCount; interval++)
        {
            if (ApplyProfileAction(profile.Kind, interval, engine, generatorId, coolingTarget))
            {
                runtimeStep++;
                var transition = engine.Step(ControlRoomRunState.Running);
                Assert.False(transition.AnyTripActive, $"Unexpected repaired-closure Stage-2 transition-step trip in profile {profile.Id} before interval {interval}.");
                rows.Add(CaptureRow(profile.Id, interval, runtimeStep, isActionTransitionStep: true, transition, engine));
            }

            runtimeStep++;
            var presentation = engine.Step(ControlRoomRunState.Running);
            Assert.False(presentation.AnyTripActive, $"Unexpected repaired-closure Stage-2 trip in profile {profile.Id} interval {interval}.");
            rows.Add(CaptureRow(profile.Id, interval, runtimeStep, isActionTransitionStep: false, presentation, engine));

            if (interval % 500 == 0 || interval == profile.IntervalCount)
            {
                WriteProgress($"profile-progress id={profile.Id} interval={interval}/{profile.IntervalCount}");
            }
        }

        var triggerCount = rows.Count(static row => row.TriggerObserved);
        var commitCount = rows.Count(static row => row.CorrectedCandidateCommitted);
        var rollbackCount = rows.Count(static row => row.RollbackRequired);
        var fallbackCount = rows.Count(static row => row.TriggerObserved && !row.CorrectedCandidateCommitted);
        Assert.All(rows, AssertFailClosedSafety);

        return new ProfileResult(
            profile.Id,
            profile.IntervalCount,
            rows.Count(static row => row.IsActionTransitionStep),
            rows,
            triggerCount,
            commitCount,
            rollbackCount,
            fallbackCount,
            CompletedWithoutTrip: true);
    }

    private static IReadOnlyList<StepTelemetryRow> RunDeterminismControl()
    {
        var engine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            DesktopSustainedGenerationInitialConditionFactory.CreateThermodynamicInverseDomainRepairEvidenceRuntimeEngine(Step, useFourNodeCorrectedCommit: true));
        var rows = new List<StepTelemetryRow>(DeterminismControlIntervals);
        for (var interval = 1; interval <= DeterminismControlIntervals; interval++)
        {
            var presentation = engine.Step(ControlRoomRunState.Running);
            Assert.False(presentation.AnyTripActive);
            var row = CaptureRow("determinism-control", interval, interval, isActionTransitionStep: false, presentation, engine);
            AssertFailClosedSafety(row);
            rows.Add(row);
        }
        return rows;
    }

    private static bool ApplyProfileAction(
        ProfileKind kind,
        int intervalIndex,
        IControlRoomRuntimeEngine engine,
        string generatorId,
        ISecondaryTransientFaultTarget coolingTarget)
    {
        switch (kind)
        {
            case ProfileKind.Steady:
                return false;
            case ProfileKind.LoadPulse:
                if (intervalIndex == 501)
                {
                    QueueGeneratorLoad(engine, generatorId, ControlRoomCommandKind.GeneratorLoadLower);
                    return true;
                }
                if (intervalIndex == 3_501)
                {
                    QueueGeneratorLoad(engine, generatorId, ControlRoomCommandKind.GeneratorLoadRaise);
                    return true;
                }
                return false;
            case ProfileKind.CoolingPulse:
                if (intervalIndex == 501)
                {
                    coolingTarget.ActivateCondenserCoolingDegradation("i5-repair-cooling-pulse", "cooling", 0.75d);
                    return true;
                }
                if (intervalIndex == 3_501)
                {
                    coolingTarget.ClearSecondaryTransientFault("i5-repair-cooling-pulse");
                    return true;
                }
                return false;
            case ProfileKind.CombinedLoadCooling:
                if (intervalIndex == 501)
                {
                    QueueGeneratorLoad(engine, generatorId, ControlRoomCommandKind.GeneratorLoadLower);
                    return true;
                }
                if (intervalIndex == 1_001)
                {
                    coolingTarget.ActivateCondenserCoolingDegradation("i5-repair-combined-cooling", "cooling", 0.75d);
                    return true;
                }
                if (intervalIndex == 3_501)
                {
                    QueueGeneratorLoad(engine, generatorId, ControlRoomCommandKind.GeneratorLoadRaise);
                    return true;
                }
                if (intervalIndex == 4_001)
                {
                    coolingTarget.ClearSecondaryTransientFault("i5-repair-combined-cooling");
                    return true;
                }
                return false;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind));
        }
    }

    private static void QueueGeneratorLoad(IControlRoomRuntimeEngine engine, string generatorId, ControlRoomCommandKind kind)
        => engine.QueueOperatorCommand(new ControlRoomCommand(kind, generatorId, ControlRoomCommandTargetKind.Generator));

    private static StepTelemetryRow CaptureRow(
        string profileId,
        int interval,
        int runtimeStep,
        bool isActionTransitionStep,
        ControlRoomSnapshot presentation,
        IntegratedAutomaticOperationRuntimeEngine engine)
    {
        var numerics = CurrentHydraulics(engine);
        Assert.Equal(HydraulicNumericalCouplingMode.FourNodeBranchContinuityCorrectedCommitOptIn, numerics.Mode);
        var telemetry = Assert.IsType<FourNodeBranchContinuityIntegrationTelemetry>(numerics.FourNodeBranchContinuity);
        var audit = CurrentAudit(engine);

        return new StepTelemetryRow(
            profileId,
            interval,
            runtimeStep,
            isActionTransitionStep,
            ControlRoomSnapshotFingerprint.Compute(presentation),
            telemetry.TriggerObserved,
            telemetry.ShadowCorrectionEvaluated,
            telemetry.ProposedAuthority,
            telemetry.Reason,
            telemetry.RollbackRequired,
            telemetry.ShadowCorrectedCandidateEligible,
            telemetry.CorrectedCommitArmEnabled,
            telemetry.CorrectedCommitAuthorized,
            telemetry.CorrectedCommitReason,
            telemetry.CorrectedCandidateCommitted,
            telemetry.UntargetedBranchDisagreementDetected,
            telemetry.BranchOverrideCount,
            telemetry.PreviousPhaseHoldCount,
            telemetry.HysteresisReleaseCount,
            telemetry.ShadowIterationCount,
            telemetry.ShadowConverged,
            telemetry.ShadowLineSearchExhausted,
            telemetry.ShadowMaximumRelativePressureResidual,
            telemetry.ShadowMaximumAbsoluteFlowResidualKilogramsPerSecond,
            telemetry.ShadowMassClosureKilogramsPerSecond,
            telemetry.ShadowEnergyOwnershipResidualWatts,
            Math.Abs(audit.MassClosureResidualKilograms),
            Math.Abs(audit.EnergyClosureResidualJoules),
            Math.Abs(audit.BalanceMassRateResidualKilogramsPerSecond),
            Math.Abs(audit.BalancePowerResidualWatts));
    }

    private static void AssertFailClosedSafety(StepTelemetryRow row)
    {
        Assert.True(row.CorrectedCommitArmEnabled);
        Assert.InRange(row.MassClosureResidualKilograms, 0d, MaximumMassClosureResidualKilograms);
        Assert.InRange(row.EnergyClosureResidualJoules, 0d, MaximumEnergyClosureResidualJoules);
        Assert.InRange(row.BalanceMassRateResidualKilogramsPerSecond, 0d, MaximumBalanceMassRateResidualKilogramsPerSecond);
        Assert.InRange(row.BalancePowerResidualWatts, 0d, MaximumBalancePowerResidualWatts);

        if (!row.TriggerObserved)
        {
            Assert.False(row.CorrectedCandidateCommitted);
            Assert.Equal(FourNodeBranchContinuityActivationReason.NotTriggered, row.ActivationReason);
            Assert.Equal(FourNodeBranchContinuityCorrectedCommitReason.NotTriggered, row.CommitReason);
        }

        if (row.RollbackRequired)
        {
            Assert.False(row.CorrectedCandidateCommitted);
        }

        if (row.CorrectedCommitAuthorized)
        {
            Assert.True(row.CorrectedCandidateCommitted);
        }

        if (row.CorrectedCandidateCommitted)
        {
            Assert.True(CommitIsQualified(row));
        }
    }

    private static bool CommitIsQualified(StepTelemetryRow row)
        => row.H20CandidateEligible
            && row.CorrectedCommitAuthorized
            && !row.RollbackRequired
            && !row.UntargetedBranchDisagreementDetected
            && row.ShadowCorrectionEvaluated
            && row.ShadowConverged
            && !row.ShadowLineSearchExhausted
            && row.ShadowMaximumRelativePressureResidual <= 1e-5d
            && row.ShadowMaximumAbsoluteFlowResidualKilogramsPerSecond <= 1e-2d
            && row.ShadowMassClosureKilogramsPerSecond <= 1e-8d
            && row.ShadowEnergyOwnershipResidualWatts <= 1e-3d
            && row.ProposedAuthority == FourNodeBranchContinuityProposedAuthority.CorrectedCandidate
            && row.ActivationReason == FourNodeBranchContinuityActivationReason.QualifiedTriggeredCorrection
            && row.CommitReason == FourNodeBranchContinuityCorrectedCommitReason.QualifiedH20Authority;

    private static PlantNetworkHydraulicNumericalSnapshot CurrentHydraulics(IntegratedAutomaticOperationRuntimeEngine engine)
        => engine.LatestCanonicalSnapshot.Control.ProtectedControl.FullPlant.IntegratedCycle.PrimaryCircuit.HydraulicNumerics;

    private static PlantNetworkAudit CurrentAudit(IntegratedAutomaticOperationRuntimeEngine engine)
        => engine.LatestCanonicalSnapshot.Control.ProtectedControl.FullPlant.IntegratedCycle.PrimaryCircuit.Audit;

    private static string Fingerprint(IReadOnlyList<StepTelemetryRow> rows)
    {
        var canonical = string.Join(
            "||",
            rows.Select(static row => FormattableString.Invariant(
                $"{row.ProfileId}:{row.Interval}:{row.RuntimeStep}:{row.IsActionTransitionStep}:{row.PresentationFingerprint}:{row.TriggerObserved}:{row.ShadowCorrectionEvaluated}:{row.ProposedAuthority}:{row.ActivationReason}:{row.RollbackRequired}:{row.H20CandidateEligible}:{row.CorrectedCommitArmEnabled}:{row.CorrectedCommitAuthorized}:{row.CommitReason}:{row.CorrectedCandidateCommitted}:{row.UntargetedBranchDisagreementDetected}:{row.BranchOverrideCount}:{row.PreviousPhaseHoldCount}:{row.HysteresisReleaseCount}:{row.ShadowIterationCount}:{row.ShadowConverged}:{row.ShadowLineSearchExhausted}:{row.ShadowMaximumRelativePressureResidual:G17}:{row.ShadowMaximumAbsoluteFlowResidualKilogramsPerSecond:G17}:{row.ShadowMassClosureKilogramsPerSecond:G17}:{row.ShadowEnergyOwnershipResidualWatts:G17}:{row.MassClosureResidualKilograms:G17}:{row.EnergyClosureResidualJoules:G17}:{row.BalanceMassRateResidualKilogramsPerSecond:G17}:{row.BalancePowerResidualWatts:G17}")));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }

    private static void WriteReports(
        IReadOnlyList<ProfileResult> profileResults,
        IReadOnlyList<StepTelemetryRow> rows,
        int totalQualificationIntervals,
        int actionTransitionSteps,
        int triggered,
        int eligible,
        int authorized,
        int commits,
        int rollbacks,
        int fallbackIntervals,
        int fallbackCommitViolations,
        int unsafeCommits,
        int untargetedDisagreements,
        long branchOverridesTotal,
        long previousPhaseHoldsTotal,
        long hysteresisReleasesTotal,
        int continuityActiveSteps,
        int postStartupContinuityActiveSteps,
        int triggersAfterStartup,
        int commitsAfterStartup,
        bool deterministicControlRepeat,
        string telemetryFingerprint,
        double maximumMassClosure,
        double maximumEnergyClosure,
        double maximumBalanceMassRate,
        double maximumBalancePower,
        bool passes)
    {
        var directory = ReportDirectory();
        Directory.CreateDirectory(directory);

        var summary = new List<string>
        {
            "=== 01-i5-thermodynamic-repair-long-horizon-cross-profile-requalification-stage2 ===",
            "scope=validated Hotfix 10 CorrelationConsistentInverseDomain evidence seam; validated H.19/H.24 four-profile 30000-interval domain under real H.29 corrected-commit ownership; no registered exact-version or production-selector activation;",
            FormattableString.Invariant($"profiles={profileResults.Count}; profile-ids={string.Join('|', profileResults.Select(static result => result.ProfileId))}; qualification-intervals={totalQualificationIntervals}; action-transition-steps={actionTransitionSteps}; corrected-runtime-steps={rows.Count}; fixed-step=10.000 ms;"),
            FormattableString.Invariant($"corrected-triggers={triggered}; eligible={eligible}; authorized={authorized}; commits={commits}; rollbacks={rollbacks}; safe-fallback-intervals={fallbackIntervals}; fallback-commit-violations={fallbackCommitViolations}; unsafe-commits={unsafeCommits}; untargeted-disagreements={untargetedDisagreements};"),
            FormattableString.Invariant($"branch-overrides-total={branchOverridesTotal}; previous-phase-holds-total={previousPhaseHoldsTotal}; hysteresis-releases-total={hysteresisReleasesTotal}; continuity-active-steps={continuityActiveSteps}; post-startup-continuity-active-steps={postStartupContinuityActiveSteps}; triggers-after-startup={triggersAfterStartup}; commits-after-startup={commitsAfterStartup};"),
            FormattableString.Invariant($"determinism-control-intervals={DeterminismControlIntervals}; deterministic-repeat={deterministicControlRepeat}; telemetry-fingerprint={telemetryFingerprint};"),
            FormattableString.Invariant($"max-conservation=mass:{maximumMassClosure:G17} kg; energy:{maximumEnergyClosure:G17} J; balance-mass-rate:{maximumBalanceMassRate:G17} kg/s; balance-power:{maximumBalancePower:G17} W;"),
            $"stage2-long-horizon-safety-passes={passes}; production-activation=False;",
            "interpretation=green requalifies the repaired closure over the historical H.19/H.24 nominal long-horizon/cross-profile domain with real corrected ownership. Continuity counts are classification evidence, not inherited H.17/H.19 acceptance floors; post-startup activity determines whether bounded previous-phase hysteresis remains materially required after the vapor seam became single-root.",
            "next-step=if green, preserve this repaired long-horizon evidence and move to repaired replay/checkpoint/protection plus off-design/performance requalification before creating any new exact production identity. Do not rerun cumulative Phase-I closure yet;",
        };

        foreach (var result in profileResults)
        {
            var profileBranchOverrides = result.Rows.Sum(static row => (long)row.BranchOverrideCount);
            var profilePreviousPhaseHolds = result.Rows.Sum(static row => (long)row.PreviousPhaseHoldCount);
            var profileHysteresisReleases = result.Rows.Sum(static row => (long)row.HysteresisReleaseCount);
            var profilePostStartupActivity = result.Rows.Count(static row =>
                row.Interval > 2
                && (row.BranchOverrideCount > 0 || row.PreviousPhaseHoldCount > 0 || row.HysteresisReleaseCount > 0));

            summary.Insert(summary.Count - 2, FormattableString.Invariant(
                $"profile={result.ProfileId}; intervals={result.IntervalCount}; action-transition-steps={result.ActionTransitionSteps}; runtime-steps={result.Rows.Count}; triggers={result.TriggerCount}; commits={result.CommitCount}; rollbacks={result.RollbackCount}; safe-fallbacks={result.FallbackCount}; branch-overrides={profileBranchOverrides}; previous-phase-holds={profilePreviousPhaseHolds}; hysteresis-releases={profileHysteresisReleases}; post-startup-continuity-active-steps={profilePostStartupActivity}; completed-without-trip={result.CompletedWithoutTrip};"));
        }

        File.WriteAllLines(
            Path.Combine(directory, "01-i5-thermodynamic-repair-long-horizon-cross-profile-requalification-stage2.summary.txt"),
            summary,
            Utf8WithoutBom);

        var csv = new List<string>
        {
            "profile_id,interval,runtime_step,is_action_transition_step,presentation_fingerprint,trigger_observed,shadow_correction_evaluated,h20_proposed_authority,h20_reason,h20_rollback_required,h20_candidate_eligible,commit_arm_enabled,commit_authorized,commit_reason,corrected_candidate_committed,untargeted_branch_disagreement,branch_overrides,previous_phase_holds,hysteresis_releases,shadow_iterations,shadow_converged,shadow_line_search_exhausted,shadow_pressure_residual,shadow_flow_residual_kg_s,shadow_mass_closure_kg_s,shadow_energy_ownership_w,network_mass_closure_kg,network_energy_closure_j,network_balance_mass_rate_kg_s,network_balance_power_w",
        };
        csv.AddRange(rows.Select(static row => FormattableString.Invariant(
            $"{row.ProfileId},{row.Interval},{row.RuntimeStep},{row.IsActionTransitionStep},{row.PresentationFingerprint},{row.TriggerObserved},{row.ShadowCorrectionEvaluated},{row.ProposedAuthority},{row.ActivationReason},{row.RollbackRequired},{row.H20CandidateEligible},{row.CorrectedCommitArmEnabled},{row.CorrectedCommitAuthorized},{row.CommitReason},{row.CorrectedCandidateCommitted},{row.UntargetedBranchDisagreementDetected},{row.BranchOverrideCount},{row.PreviousPhaseHoldCount},{row.HysteresisReleaseCount},{row.ShadowIterationCount},{row.ShadowConverged},{row.ShadowLineSearchExhausted},{row.ShadowMaximumRelativePressureResidual:G17},{row.ShadowMaximumAbsoluteFlowResidualKilogramsPerSecond:G17},{row.ShadowMassClosureKilogramsPerSecond:G17},{row.ShadowEnergyOwnershipResidualWatts:G17},{row.MassClosureResidualKilograms:G17},{row.EnergyClosureResidualJoules:G17},{row.BalanceMassRateResidualKilogramsPerSecond:G17},{row.BalancePowerResidualWatts:G17}")));
        File.WriteAllLines(Path.Combine(directory, "02-repaired-long-horizon-step-telemetry.csv"), csv, Utf8WithoutBom);

        var profileCsv = new List<string>
        {
            "profile_id,intervals,action_transition_steps,runtime_steps,triggers,commits,rollbacks,safe_fallbacks,branch_overrides,previous_phase_holds,hysteresis_releases,post_startup_continuity_active_steps,completed_without_trip",
        };
        profileCsv.AddRange(profileResults.Select(static result =>
        {
            var profileBranchOverrides = result.Rows.Sum(static row => (long)row.BranchOverrideCount);
            var profilePreviousPhaseHolds = result.Rows.Sum(static row => (long)row.PreviousPhaseHoldCount);
            var profileHysteresisReleases = result.Rows.Sum(static row => (long)row.HysteresisReleaseCount);
            var profilePostStartupActivity = result.Rows.Count(static row =>
                row.Interval > 2
                && (row.BranchOverrideCount > 0 || row.PreviousPhaseHoldCount > 0 || row.HysteresisReleaseCount > 0));
            return $"{result.ProfileId},{result.IntervalCount},{result.ActionTransitionSteps},{result.Rows.Count},{result.TriggerCount},{result.CommitCount},{result.RollbackCount},{result.FallbackCount},{profileBranchOverrides},{profilePreviousPhaseHolds},{profileHysteresisReleases},{profilePostStartupActivity},{result.CompletedWithoutTrip}";
        }));
        File.WriteAllLines(Path.Combine(directory, "03-repaired-profile-qualification-metrics.csv"), profileCsv, Utf8WithoutBom);

        File.WriteAllLines(
            Path.Combine(directory, "04-i5-thermodynamic-repair-long-horizon-cross-profile-requalification-stage2-metrics.csv"),
            new[]
            {
                "metric,value",
                FormattableString.Invariant($"profiles,{profileResults.Count}"),
                FormattableString.Invariant($"qualification_intervals,{totalQualificationIntervals}"),
                FormattableString.Invariant($"action_transition_steps,{actionTransitionSteps}"),
                FormattableString.Invariant($"corrected_runtime_steps,{rows.Count}"),
                FormattableString.Invariant($"corrected_triggers,{triggered}"),
                FormattableString.Invariant($"candidate_eligible,{eligible}"),
                FormattableString.Invariant($"commit_authorized,{authorized}"),
                FormattableString.Invariant($"corrected_commits,{commits}"),
                FormattableString.Invariant($"rollbacks,{rollbacks}"),
                FormattableString.Invariant($"safe_fallback_intervals,{fallbackIntervals}"),
                FormattableString.Invariant($"fallback_commit_violations,{fallbackCommitViolations}"),
                FormattableString.Invariant($"unsafe_commits,{unsafeCommits}"),
                FormattableString.Invariant($"untargeted_disagreements,{untargetedDisagreements}"),
                FormattableString.Invariant($"branch_overrides_total,{branchOverridesTotal}"),
                FormattableString.Invariant($"previous_phase_holds_total,{previousPhaseHoldsTotal}"),
                FormattableString.Invariant($"hysteresis_releases_total,{hysteresisReleasesTotal}"),
                FormattableString.Invariant($"continuity_active_steps,{continuityActiveSteps}"),
                FormattableString.Invariant($"post_startup_continuity_active_steps,{postStartupContinuityActiveSteps}"),
                FormattableString.Invariant($"triggers_after_startup,{triggersAfterStartup}"),
                FormattableString.Invariant($"commits_after_startup,{commitsAfterStartup}"),
                FormattableString.Invariant($"deterministic_repeat,{deterministicControlRepeat}"),
                FormattableString.Invariant($"telemetry_fingerprint,{telemetryFingerprint}"),
                FormattableString.Invariant($"max_mass_closure_kg,{maximumMassClosure:G17}"),
                FormattableString.Invariant($"max_energy_closure_j,{maximumEnergyClosure:G17}"),
                FormattableString.Invariant($"max_balance_mass_rate_kg_s,{maximumBalanceMassRate:G17}"),
                FormattableString.Invariant($"max_balance_power_w,{maximumBalancePower:G17}"),
                FormattableString.Invariant($"stage2_long_horizon_safety_passes,{passes}"),
            },
            Utf8WithoutBom);
    }

    private static string ReportDirectory()
        => Path.Combine(FindRepositoryRoot(), "artifacts", "i5-thermodynamic-repair-long-horizon-cross-profile-requalification-stage2");

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "NuclearReactorSimulator.sln")))
            {
                return current.FullName;
            }
            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate NuclearReactorSimulator.sln from the test output directory.");
    }

    private static void ResetProgress()
    {
        var directory = ReportDirectory();
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
        Directory.CreateDirectory(directory);
        WriteProgress("I.5 repaired-closure long-horizon/cross-profile requalification stage 2 started");
    }

    private static void WriteProgress(string message)
        => File.WriteAllText(
            Path.Combine(ReportDirectory(), "00-progress.txt"),
            $"{DateTimeOffset.UtcNow:O} {message}{Environment.NewLine}",
            Utf8WithoutBom);

    private enum ProfileKind
    {
        Steady = 0,
        LoadPulse = 1,
        CoolingPulse = 2,
        CombinedLoadCooling = 3,
    }

    private sealed record ProfileDefinition(string Id, int IntervalCount, ProfileKind Kind);

    private sealed record ProfileResult(
        string ProfileId,
        int IntervalCount,
        int ActionTransitionSteps,
        IReadOnlyList<StepTelemetryRow> Rows,
        int TriggerCount,
        int CommitCount,
        int RollbackCount,
        int FallbackCount,
        bool CompletedWithoutTrip);

    private sealed record StepTelemetryRow(
        string ProfileId,
        int Interval,
        int RuntimeStep,
        bool IsActionTransitionStep,
        string PresentationFingerprint,
        bool TriggerObserved,
        bool ShadowCorrectionEvaluated,
        FourNodeBranchContinuityProposedAuthority ProposedAuthority,
        FourNodeBranchContinuityActivationReason ActivationReason,
        bool RollbackRequired,
        bool H20CandidateEligible,
        bool CorrectedCommitArmEnabled,
        bool CorrectedCommitAuthorized,
        FourNodeBranchContinuityCorrectedCommitReason CommitReason,
        bool CorrectedCandidateCommitted,
        bool UntargetedBranchDisagreementDetected,
        int BranchOverrideCount,
        int PreviousPhaseHoldCount,
        int HysteresisReleaseCount,
        int ShadowIterationCount,
        bool ShadowConverged,
        bool ShadowLineSearchExhausted,
        double ShadowMaximumRelativePressureResidual,
        double ShadowMaximumAbsoluteFlowResidualKilogramsPerSecond,
        double ShadowMassClosureKilogramsPerSecond,
        double ShadowEnergyOwnershipResidualWatts,
        double MassClosureResidualKilograms,
        double EnergyClosureResidualJoules,
        double BalanceMassRateResidualKilogramsPerSecond,
        double BalancePowerResidualWatts);
}
