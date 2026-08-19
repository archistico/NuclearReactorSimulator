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
/// M10.9.4.1-H.24 Requalification 1 committed long-horizon/cross-profile regression of the H.28-validated
/// optimized corrected-commit implementation. The H.24 operational domain and numerical contracts are unchanged.
/// Standard current-v2 remains explicit.
/// </summary>
public sealed class FourNodePostH28CommittedLongHorizonRequalificationAuditTests
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

    private static readonly IReadOnlyDictionary<string, string> FrozenH28Fingerprints =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["H28_ValidatedPerformanceCostSoakSummary.txt"] = "C2EC26E3C196CEE32EDB99B67C0C8156704E9D27578E189A97B86D27F357E563",
            ["H28_ValidatedPerformanceBenchmark.csv"] = "17992F497A665EBF7423F4626128AFC37A4C769DE216638D048D047A1C0A3984",
            ["H28_ValidatedOperationalSoakSamples.csv"] = "C318B389C6892B27D3C4A98338A8DDF6D940FE603733C0AE9E5C63AC4C58D119",
            ["H28_ValidatedPerformanceCostSoakMetrics.csv"] = "F9FC9CBE11152BC6FD712E8EFB2BE3555DCEB167371DF38376F18BD19CD16C31",
        };

    [Fact]
    public void FrozenH28Evidence_RetainsValidatedPerformanceCostOperationalSoakQualification()
    {
        var evidenceDirectory = EvidenceDirectory();
        foreach (var expected in FrozenH28Fingerprints)
        {
            var path = Path.Combine(evidenceDirectory, expected.Key);
            Assert.True(File.Exists(path), $"Frozen H.28 evidence file is missing: {expected.Key}");
            Assert.Equal(expected.Value, CanonicalSha256(path));
        }

        var summary = File.ReadAllText(Path.Combine(evidenceDirectory, "H28_ValidatedPerformanceCostSoakSummary.txt"));
        Assert.Contains("median-wall-cost-ratio=4.6214685710690242", summary, StringComparison.Ordinal);
        Assert.Contains("median-wall-cost-ratio-limit=8", summary, StringComparison.Ordinal);
        Assert.Contains("p95-wall-cost-ratio=10.684444741413872", summary, StringComparison.Ordinal);
        Assert.Contains("p95-wall-cost-ratio-limit=12", summary, StringComparison.Ordinal);
        Assert.Contains("median-allocation-ratio=1.1164372201028363", summary, StringComparison.Ordinal);
        Assert.Contains("corrected-performance-class=bounded-but-costly", summary, StringComparison.Ordinal);
        Assert.Contains("soak-triggered=379", summary, StringComparison.Ordinal);
        Assert.Contains("soak-committed=379", summary, StringComparison.Ordinal);
        Assert.Contains("soak-rollbacks=0", summary, StringComparison.Ordinal);
        Assert.Contains("soak-fallback-commit-violations=0", summary, StringComparison.Ordinal);
        Assert.Contains("soak-unsafe-commits=0", summary, StringComparison.Ordinal);
        Assert.Contains("soak-untargeted-branch-disagreements=0", summary, StringComparison.Ordinal);
        Assert.Contains("deterministic-repeat=True", summary, StringComparison.Ordinal);
        Assert.Contains("deterministic-fingerprint=518BA948637F0C270C7F8228AB97FEF9148E29A4F5CE4376319AB5D1CFBE7F38", summary, StringComparison.Ordinal);
        Assert.Contains("four-node-performance-cost-operational-soak-passes=True", summary, StringComparison.Ordinal);
        Assert.Contains("h28-audit-passes=True", summary, StringComparison.Ordinal);
    }

    [Fact(Explicit = true)]
    [Trait("Category", "FourNodePostH28CommittedLongHorizonRequalificationAudit")]
    public void OptInCorrectedCommitRuntime_RequalifiesPostOptimizationLongHorizonAndCrossProfileOperation()
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
        Assert.True(deterministicControlRepeat, "H.24 post-H.28 requalification determinism control did not repeat exactly.");
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
        var maximumMassClosure = rows.Max(static row => row.MassClosureResidualKilograms);
        var maximumEnergyClosure = rows.Max(static row => row.EnergyClosureResidualJoules);
        var maximumBalanceMassRate = rows.Max(static row => row.BalanceMassRateResidualKilogramsPerSecond);
        var maximumBalancePower = rows.Max(static row => row.BalancePowerResidualWatts);
        var telemetryFingerprint = Fingerprint(rows);

        Assert.Equal(30_000, totalQualificationIntervals);
        Assert.Equal(8, actionTransitionSteps);
        Assert.All(profileResults, static result => Assert.True(result.TriggerCount > 0, $"H.24 post-H.28 profile {result.ProfileId} observed no P060/F040 trigger."));
        Assert.All(profileResults, static result => Assert.True(result.CommitCount > 0, $"H.24 post-H.28 profile {result.ProfileId} observed no corrected commit."));
        Assert.Equal(0, fallbackCommitViolations);
        Assert.Equal(0, unsafeCommits);
        Assert.Equal(0, untargetedDisagreements);
        Assert.True(commits > 0);
        Assert.True(authorized >= commits);
        Assert.True(eligible >= authorized);
        Assert.InRange(maximumMassClosure, 0d, MaximumMassClosureResidualKilograms);
        Assert.InRange(maximumEnergyClosure, 0d, MaximumEnergyClosureResidualJoules);
        Assert.InRange(maximumBalanceMassRate, 0d, MaximumBalanceMassRateResidualKilogramsPerSecond);
        Assert.InRange(maximumBalancePower, 0d, MaximumBalancePowerResidualWatts);

        var defaultEngine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            new DesktopSustainedGenerationInitialConditionFactory().CreateRuntimeEngine());
        var defaultMode = CurrentHydraulics(defaultEngine).Mode;
        Assert.Equal(HydraulicNumericalCouplingMode.ExplicitCommittedState, defaultMode);

        var passes = profileResults.All(static result => result.CompletedWithoutTrip)
            && profileResults.All(static result => result.TriggerCount > 0)
            && profileResults.All(static result => result.CommitCount > 0)
            && fallbackCommitViolations == 0
            && unsafeCommits == 0
            && untargetedDisagreements == 0
            && deterministicControlRepeat
            && maximumMassClosure <= MaximumMassClosureResidualKilograms
            && maximumEnergyClosure <= MaximumEnergyClosureResidualJoules
            && maximumBalanceMassRate <= MaximumBalanceMassRateResidualKilogramsPerSecond
            && maximumBalancePower <= MaximumBalancePowerResidualWatts
            && defaultMode == HydraulicNumericalCouplingMode.ExplicitCommittedState;
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
            deterministicControlRepeat,
            telemetryFingerprint,
            maximumMassClosure,
            maximumEnergyClosure,
            maximumBalanceMassRate,
            maximumBalancePower,
            defaultMode,
            passes);
    }

    private static ProfileResult RunProfile(ProfileDefinition profile)
    {
        var engine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            DesktopSustainedGenerationInitialConditionFactory.CreateFourNodeCorrectedCommitEvidenceRuntimeEngine(Step));
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
                Assert.False(transition.AnyTripActive, $"Unexpected H.24 transition-step trip in profile {profile.Id} before interval {interval}.");
                rows.Add(CaptureRow(profile.Id, interval, runtimeStep, isActionTransitionStep: true, transition, engine));
            }

            runtimeStep++;
            var presentation = engine.Step(ControlRoomRunState.Running);
            Assert.False(presentation.AnyTripActive, $"Unexpected H.24 trip in profile {profile.Id} interval {interval}.");
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
            DesktopSustainedGenerationInitialConditionFactory.CreateFourNodeCorrectedCommitEvidenceRuntimeEngine(Step));
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
                    coolingTarget.ActivateCondenserCoolingDegradation("h24-cooling-pulse", "cooling", 0.75d);
                    return true;
                }
                if (intervalIndex == 3_501)
                {
                    coolingTarget.ClearSecondaryTransientFault("h24-cooling-pulse");
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
                    coolingTarget.ActivateCondenserCoolingDegradation("h24-combined-cooling", "cooling", 0.75d);
                    return true;
                }
                if (intervalIndex == 3_501)
                {
                    QueueGeneratorLoad(engine, generatorId, ControlRoomCommandKind.GeneratorLoadRaise);
                    return true;
                }
                if (intervalIndex == 4_001)
                {
                    coolingTarget.ClearSecondaryTransientFault("h24-combined-cooling");
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
        bool deterministicControlRepeat,
        string telemetryFingerprint,
        double maximumMassClosure,
        double maximumEnergyClosure,
        double maximumBalanceMassRate,
        double maximumBalancePower,
        HydraulicNumericalCouplingMode defaultMode,
        bool passes)
    {
        var directory = ReportDirectory();
        Directory.CreateDirectory(directory);
        var summary = new List<string>
        {
            "=== 01-current-v2-post-h28-four-node-committed-long-horizon-cross-profile-requalification ===",
            "H.24 Requalification 1 reruns the unchanged H.24 four-profile committed long-horizon domain after the H.28.1 optimization branch has stabilized and H.28 has passed. It requalifies the optimized corrected-commit implementation without changing H.9 mathematics, H.20 authority, H.22 ownership, P060/F040, branch-continuity limits, physical coefficients or the 10 ms fixed step. Safe H.20 rollback/fallback is permitted; unsafe or fallback commits are not. Standard current-v2 remains ExplicitCommittedState.",
            FormattableString.Invariant($"profiles={profileResults.Count}; profile-ids={string.Join('|', profileResults.Select(static result => result.ProfileId))}; qualification-intervals={totalQualificationIntervals}; action-transition-steps={actionTransitionSteps}; committed-runtime-steps={rows.Count}; production-fixed-step=10.000 ms;"),
            FormattableString.Invariant($"P060-F040-triggered={triggered}; H20-candidate-eligible={eligible}; H22-commit-authorized={authorized}; corrected-candidates-committed={commits}; H20-rollbacks={rollbacks}; safe-fallback-intervals={fallbackIntervals}; fallback-commit-violations={fallbackCommitViolations}; unsafe-corrected-commits={unsafeCommits}; untargeted-branch-disagreements={untargetedDisagreements};"),
            FormattableString.Invariant($"determinism-control-intervals={DeterminismControlIntervals}; deterministic-control-repeat={deterministicControlRepeat}; committed-telemetry-fingerprint={telemetryFingerprint};"),
            FormattableString.Invariant($"max-network-mass-closure-kg={maximumMassClosure:G17}; max-network-energy-closure-j={maximumEnergyClosure:G17}; max-network-balance-mass-rate-kg-s={maximumBalanceMassRate:G17}; max-network-balance-power-w={maximumBalancePower:G17};"),
            FormattableString.Invariant($"default-current-v2-mode={defaultMode}; opt-in-corrected-commit-mode=FourNodeBranchContinuityCorrectedCommitOptIn; H28-prerequisite-frozen=True; post-H28-runtime-requalification=True; H20-contract-replaced=False; H19-operational-profile-domain-changed=False; H19-target-set-changed=False; H9-tolerances-retuned=False; P060-F040-retuned=False; bounded-hysteresis-limits-changed=False; physical-coefficient-retuning=False;"),
            FormattableString.Invariant($"post-h28-four-node-committed-long-horizon-cross-profile-requalification-passes={passes}; h24-post-h28-requalification-audit-passes={passes};"),
            "H.24 post-H.28 recommendation: if this gate is green, the single required post-optimization long-horizon regression is complete. Keep default production explicit and allow H.29 to begin using the already-validated H.24-H.28 evidence chain. This gate does not itself authorize production-default activation.",
        };
        foreach (var result in profileResults)
        {
            summary.Insert(summary.Count - 2, FormattableString.Invariant(
                $"profile={result.ProfileId}; intervals={result.IntervalCount}; action-transition-steps={result.ActionTransitionSteps}; runtime-steps={result.Rows.Count}; triggers={result.TriggerCount}; commits={result.CommitCount}; rollbacks={result.RollbackCount}; safe-fallbacks={result.FallbackCount}; completed-without-trip={result.CompletedWithoutTrip};"));
        }
        File.WriteAllLines(Path.Combine(directory, "01-post-h28-four-node-committed-long-horizon-cross-profile-requalification.summary.txt"), summary, Utf8WithoutBom);

        var csv = new List<string>
        {
            "profile_id,interval,runtime_step,is_action_transition_step,presentation_fingerprint,trigger_observed,shadow_correction_evaluated,h20_proposed_authority,h20_reason,h20_rollback_required,h20_candidate_eligible,h22_commit_arm_enabled,h22_commit_authorized,h22_commit_reason,corrected_candidate_committed,untargeted_branch_disagreement,branch_overrides,previous_phase_holds,hysteresis_releases,shadow_iterations,shadow_converged,shadow_line_search_exhausted,shadow_pressure_residual,shadow_flow_residual_kg_s,shadow_mass_closure_kg_s,shadow_energy_ownership_w,network_mass_closure_kg,network_energy_closure_j,network_balance_mass_rate_kg_s,network_balance_power_w",
        };
        csv.AddRange(rows.Select(static row => FormattableString.Invariant(
            $"{row.ProfileId},{row.Interval},{row.RuntimeStep},{row.IsActionTransitionStep},{row.PresentationFingerprint},{row.TriggerObserved},{row.ShadowCorrectionEvaluated},{row.ProposedAuthority},{row.ActivationReason},{row.RollbackRequired},{row.H20CandidateEligible},{row.CorrectedCommitArmEnabled},{row.CorrectedCommitAuthorized},{row.CommitReason},{row.CorrectedCandidateCommitted},{row.UntargetedBranchDisagreementDetected},{row.BranchOverrideCount},{row.PreviousPhaseHoldCount},{row.HysteresisReleaseCount},{row.ShadowIterationCount},{row.ShadowConverged},{row.ShadowLineSearchExhausted},{row.ShadowMaximumRelativePressureResidual:G17},{row.ShadowMaximumAbsoluteFlowResidualKilogramsPerSecond:G17},{row.ShadowMassClosureKilogramsPerSecond:G17},{row.ShadowEnergyOwnershipResidualWatts:G17},{row.MassClosureResidualKilograms:G17},{row.EnergyClosureResidualJoules:G17},{row.BalanceMassRateResidualKilogramsPerSecond:G17},{row.BalancePowerResidualWatts:G17}")));
        File.WriteAllLines(Path.Combine(directory, "02-post-h28-committed-long-horizon-step-telemetry.csv"), csv, Utf8WithoutBom);

        var profileCsv = new List<string>
        {
            "profile_id,intervals,action_transition_steps,runtime_steps,triggers,commits,rollbacks,safe_fallbacks,completed_without_trip",
        };
        profileCsv.AddRange(profileResults.Select(static result =>
            $"{result.ProfileId},{result.IntervalCount},{result.ActionTransitionSteps},{result.Rows.Count},{result.TriggerCount},{result.CommitCount},{result.RollbackCount},{result.FallbackCount},{result.CompletedWithoutTrip}"));
        File.WriteAllLines(Path.Combine(directory, "03-post-h28-profile-qualification-metrics.csv"), profileCsv, Utf8WithoutBom);

        File.WriteAllLines(
            Path.Combine(directory, "04-post-h28-four-node-committed-long-horizon-requalification-metrics.csv"),
            new[]
            {
                "metric,value",
                FormattableString.Invariant($"profiles,{profileResults.Count}"),
                FormattableString.Invariant($"qualification_intervals,{totalQualificationIntervals}"),
                FormattableString.Invariant($"action_transition_steps,{actionTransitionSteps}"),
                FormattableString.Invariant($"committed_runtime_steps,{rows.Count}"),
                FormattableString.Invariant($"triggered,{triggered}"),
                FormattableString.Invariant($"h20_candidate_eligible,{eligible}"),
                FormattableString.Invariant($"h22_commit_authorized,{authorized}"),
                FormattableString.Invariant($"corrected_commits,{commits}"),
                FormattableString.Invariant($"h20_rollbacks,{rollbacks}"),
                FormattableString.Invariant($"safe_fallback_intervals,{fallbackIntervals}"),
                FormattableString.Invariant($"fallback_commit_violations,{fallbackCommitViolations}"),
                FormattableString.Invariant($"unsafe_commits,{unsafeCommits}"),
                FormattableString.Invariant($"untargeted_disagreements,{untargetedDisagreements}"),
                FormattableString.Invariant($"deterministic_control_repeat,{deterministicControlRepeat}"),
                FormattableString.Invariant($"committed_telemetry_fingerprint,{telemetryFingerprint}"),
                FormattableString.Invariant($"max_mass_closure_kg,{maximumMassClosure:G17}"),
                FormattableString.Invariant($"max_energy_closure_j,{maximumEnergyClosure:G17}"),
                FormattableString.Invariant($"max_balance_mass_rate_kg_s,{maximumBalanceMassRate:G17}"),
                FormattableString.Invariant($"max_balance_power_w,{maximumBalancePower:G17}"),
                FormattableString.Invariant($"h24_post_h28_requalification_audit_passes,{passes}"),
            },
            Utf8WithoutBom);
    }

    private static string EvidenceDirectory()
        => Path.Combine(
            FindRepositoryRoot(),
            "eng",
            "frozen-evidence",
            "ordinary");

    private static string ReportDirectory()
        => Path.Combine(FindRepositoryRoot(), "artifacts", "h24-post-h28-four-node-committed-long-horizon-cross-profile-requalification");

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

    private static string CanonicalSha256(string path)
    {
        var text = File.ReadAllText(path).Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
    }

    private static void ResetProgress()
    {
        var directory = ReportDirectory();
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
        Directory.CreateDirectory(directory);
        WriteProgress("H.24 post-H.28 requalification long-horizon/cross-profile qualification started");
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
