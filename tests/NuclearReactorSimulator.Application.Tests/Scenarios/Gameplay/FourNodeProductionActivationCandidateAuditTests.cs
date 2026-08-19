using System.Security.Cryptography;
using System.Text;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Recording;
using NuclearReactorSimulator.Application.Scenarios.Training;
using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Plant;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay;

/// <summary>
/// M10.9.4.1-H.29 production activation candidate. This gate does not change H.9/H.20/H.22 mathematics and does not make
/// corrected ownership authoritative by itself. It qualifies an exact-version v3 candidate, explicit deployment kill/rollback,
/// internal production telemetry, deterministic operation and save/replay/checkpoint compatibility before the H.30 decision.
/// </summary>
public sealed class FourNodeProductionActivationCandidateAuditTests
{
    private static readonly Encoding Utf8WithoutBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    private const int QualificationIntervals = 1_024;
    private const int DeterminismRepeatIntervals = 256;
    private const int ReplaySteps = 128;
    private const int ReplayCheckpointStep = 64;

    private static readonly IReadOnlyDictionary<string, string> FrozenPrerequisiteFingerprints =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["H23_ValidatedCommittedReplayProtectionSummary.txt"] = "933ED5D40C0329D14EBF2F757F87F631118485221B4ED272AF092AEA60E0CB25",
            ["H24_PostH28_ValidatedRequalificationSummary.txt"] = "246BE859B7B59B8A208932E7C07035A5F80DCB2960F32A73891BDDDB669ACB71",
            ["H24_PostH28_ValidatedProfileQualificationMetrics.csv"] = "EE700B48F53D5DD39B419A2294C61655F9E48944A82FF5B058CFD4ABAE68F974",
            ["H24_PostH28_ValidatedRequalificationMetrics.csv"] = "7567390C73763B06A68227D50C8B30E7E3947B00A931172CCDC77BE246BF4B18",
            ["H24_PostH28_ValidatedEvidenceManifest.txt"] = "7B64CB3638E7221619D9F6845921802C91213E07D1C3EFE224C3F78C8550B26B",
            ["H25_ValidatedProtectionTransientMatrixSummary.txt"] = "09112868F26AAD1F007820F27BBED6BF48462FC5E8A61C47E0D786D079090E85",
            ["H26_ValidatedIntegratedRollbackSummary.txt"] = "4DDC10F4F084C392969E26D9C5B5C4203A30F93DFB2F8BABB81D7807DFFBD7EC",
            ["H27_ValidatedOffDesignQualificationSummary.txt"] = "DDEAC9E8987FC7C12483A792067B4134BB1D87BDDD052F5E0D46E9AEAD3107AE",
            ["H28_ValidatedPerformanceCostSoakSummary.txt"] = "C2EC26E3C196CEE32EDB99B67C0C8156704E9D27578E189A97B86D27F357E563",
        };

    [Fact]
    public void FrozenPrerequisites_RetainValidatedH23ThroughH28AndPostOptimizationH24Evidence()
    {
        var evidenceDirectory = EvidenceDirectory();
        foreach (var expected in FrozenPrerequisiteFingerprints)
        {
            var path = Path.Combine(evidenceDirectory, expected.Key);
            Assert.True(File.Exists(path), $"Frozen H.29 prerequisite evidence file is missing: {expected.Key}");
            Assert.Equal(expected.Value, CanonicalSha256(path));
        }

        var h23 = File.ReadAllText(Path.Combine(evidenceDirectory, "H23_ValidatedCommittedReplayProtectionSummary.txt"));
        Assert.Contains("full-replay-trace-equivalent=True", h23, StringComparison.Ordinal);
        Assert.Contains("checkpoint-prefix-and-continuation-equivalent=True", h23, StringComparison.Ordinal);
        Assert.Contains("h23-audit-passes=True", h23, StringComparison.Ordinal);

        var h24 = File.ReadAllText(Path.Combine(evidenceDirectory, "H24_PostH28_ValidatedRequalificationSummary.txt"));
        Assert.Contains("qualification-intervals=30000", h24, StringComparison.Ordinal);
        Assert.Contains("corrected-candidates-committed=9626", h24, StringComparison.Ordinal);
        Assert.Contains("H20-rollbacks=0", h24, StringComparison.Ordinal);
        Assert.Contains("post-h28-four-node-committed-long-horizon-cross-profile-requalification-passes=True", h24, StringComparison.Ordinal);
        Assert.Contains("h24-post-h28-requalification-audit-passes=True", h24, StringComparison.Ordinal);

        var h24Manifest = File.ReadAllText(Path.Combine(evidenceDirectory, "H24_PostH28_ValidatedEvidenceManifest.txt"));
        Assert.Contains("full-telemetry-data-rows=30008", h24Manifest, StringComparison.Ordinal);
        Assert.Contains("full-telemetry-canonical-sha256=1A56CEE2E1FF976448B3EAB5CB7CF90E4EA8D9940607900B05516B4F5DA7E98A", h24Manifest, StringComparison.Ordinal);
        Assert.Contains("committed-telemetry-fingerprint=7AF233CE51A866B3E00C2C032AA58EEFBD7290DE0940725E5F4B7860EA6287BE", h24Manifest, StringComparison.Ordinal);

        var h25 = File.ReadAllText(Path.Combine(evidenceDirectory, "H25_ValidatedProtectionTransientMatrixSummary.txt"));
        Assert.Contains("four-node-committed-protection-operational-transient-matrix-passes=True", h25, StringComparison.Ordinal);

        var h26 = File.ReadAllText(Path.Combine(evidenceDirectory, "H26_ValidatedIntegratedRollbackSummary.txt"));
        Assert.Contains("explicit-fallback-equivalent=12/12", h26, StringComparison.Ordinal);
        Assert.Contains("rollback-challenges=8", h26, StringComparison.Ordinal);
        Assert.Contains("h26-audit-passes=True", h26, StringComparison.Ordinal);

        var h27 = File.ReadAllText(Path.Combine(evidenceDirectory, "H27_ValidatedOffDesignQualificationSummary.txt"));
        Assert.Contains("four-node-off-design-robustness-qualification-envelope-passes=True", h27, StringComparison.Ordinal);
        Assert.Contains("fallback-commit-violations=0", h27, StringComparison.Ordinal);

        var h28 = File.ReadAllText(Path.Combine(evidenceDirectory, "H28_ValidatedPerformanceCostSoakSummary.txt"));
        Assert.Contains("corrected-performance-class=bounded-but-costly", h28, StringComparison.Ordinal);
        Assert.Contains("median-wall-cost-ratio=4.6214685710690242", h28, StringComparison.Ordinal);
        Assert.Contains("p95-wall-cost-ratio=10.684444741413872", h28, StringComparison.Ordinal);
        Assert.Contains("four-node-performance-cost-operational-soak-passes=True", h28, StringComparison.Ordinal);
    }

    [Fact(Explicit = true)]
    [Trait("Category", "FourNodeProductionActivationCandidateAudit")]
    public void VersionedCorrectedCandidate_QualifiesSelectionKillTelemetryDeterminismAndReplayWithoutChangingDefault()
    {
        ResetReportDirectory();

        var defaultDecision = DesktopHydraulicProductionPolicySelector.Resolve(
            DesktopHydraulicProductionPolicy.ExplicitCommittedState);
        var candidateDecision = DesktopHydraulicProductionPolicySelector.Resolve(
            DesktopHydraulicProductionPolicySelector.H29ActivationCandidatePolicy);
        var killDecision = DesktopHydraulicProductionPolicySelector.Resolve(
            DesktopHydraulicProductionPolicySelector.H29ActivationCandidatePolicy,
            explicitKillRequested: true);

        Assert.Equal(DesktopSustainedGenerationInitialConditionFactory.Reference, defaultDecision.InitialCondition);
        Assert.Equal(DesktopSustainedGenerationH29ActivationCandidateInitialConditionFactory.Reference, candidateDecision.InitialCondition);
        Assert.Equal(DesktopSustainedGenerationInitialConditionFactory.Reference, killDecision.InitialCondition);
        Assert.True(killDecision.ExplicitKillApplied);

        var defaultEngine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            DesktopHydraulicProductionPolicySelector.CreateFactory(defaultDecision).CreateRuntimeEngine());
        var killEngine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            DesktopHydraulicProductionPolicySelector.CreateFactory(killDecision).CreateRuntimeEngine());
        Assert.Equal(HydraulicNumericalCouplingMode.ExplicitCommittedState, CurrentHydraulics(defaultEngine).Mode);
        Assert.Equal(HydraulicNumericalCouplingMode.ExplicitCommittedState, CurrentHydraulics(killEngine).Mode);
        Assert.Equal(TimeSpan.FromMilliseconds(10d), defaultEngine.FixedDeltaTime);
        Assert.Equal(TimeSpan.FromMilliseconds(10d), killEngine.FixedDeltaTime);

        WriteProgress("candidate-primary-run-start");
        var primary = RunCandidate(QualificationIntervals);
        WriteProgress($"candidate-primary-run-complete steps={primary.Rows.Count} commits={primary.Telemetry.CorrectedCommittedSteps}");
        Assert.True(primary.Telemetry.TriggeredSteps > 0);
        Assert.True(primary.Telemetry.CorrectedCommittedSteps > 0);
        Assert.Equal(primary.Telemetry.TriggeredSteps, primary.Telemetry.CandidateEligibleSteps);
        Assert.Equal(primary.Telemetry.TriggeredSteps, primary.Telemetry.CommitAuthorizedSteps);
        Assert.Equal(primary.Telemetry.TriggeredSteps, primary.Telemetry.CorrectedCommittedSteps);
        Assert.Equal(0, primary.Telemetry.RollbackSteps);
        Assert.Equal(0, primary.Telemetry.ExplicitFallbackSteps);
        Assert.Equal(0, primary.Telemetry.FallbackCommitViolations);
        Assert.Equal(0, primary.Telemetry.UnsafeCommitViolations);
        Assert.Equal(0, primary.Telemetry.UntargetedBranchDisagreementSteps);
        Assert.Empty(primary.Telemetry.RollbackReasonCounts);
        Assert.All(primary.Rows, AssertFailClosedSafety);

        WriteProgress("candidate-determinism-control-start");
        var deterministicFirst = RunCandidate(DeterminismRepeatIntervals);
        var deterministicSecond = RunCandidate(DeterminismRepeatIntervals);
        var deterministicRepeat = string.Equals(
            Fingerprint(deterministicFirst.Rows),
            Fingerprint(deterministicSecond.Rows),
            StringComparison.Ordinal);
        Assert.True(deterministicRepeat);
        WriteProgress($"candidate-determinism-control-complete repeat={deterministicRepeat}");

        WriteProgress("candidate-versioned-replay-start");
        var replay = VerifyVersionedReplayAndCheckpoint();
        WriteProgress($"candidate-versioned-replay-complete final-step={replay.FinalLogicalStep}");

        var operatorDiagnosticsExposed = typeof(ControlRoomSnapshot).GetProperties().Any(static property =>
            property.Name.Contains("HydraulicNumerical", StringComparison.Ordinal)
            || property.Name.Contains("FourNode", StringComparison.Ordinal)
            || property.PropertyType.FullName?.Contains("FourNodeBranchContinuity", StringComparison.Ordinal) == true
            || property.PropertyType.FullName?.Contains("ProductionActivationTelemetry", StringComparison.Ordinal) == true);
        Assert.False(operatorDiagnosticsExposed);

        var candidateMode = primary.Mode;
        var passes = candidateMode == HydraulicNumericalCouplingMode.FourNodeBranchContinuityCorrectedCommitOptIn
            && CurrentHydraulics(defaultEngine).Mode == HydraulicNumericalCouplingMode.ExplicitCommittedState
            && CurrentHydraulics(killEngine).Mode == HydraulicNumericalCouplingMode.ExplicitCommittedState
            && primary.Telemetry.TriggeredSteps > 0
            && primary.Telemetry.CorrectedCommittedSteps > 0
            && primary.Telemetry.RollbackSteps == 0
            && primary.Telemetry.ExplicitFallbackSteps == 0
            && primary.Telemetry.FallbackCommitViolations == 0
            && primary.Telemetry.UnsafeCommitViolations == 0
            && primary.Telemetry.UntargetedBranchDisagreementSteps == 0
            && deterministicRepeat
            && replay.FullReplayEquivalent
            && replay.CheckpointEquivalent
            && replay.CandidateVersionPreserved
            && replay.ExplicitVersionStillLoadable
            && !operatorDiagnosticsExposed;
        Assert.True(passes);

        WriteReports(
            primary,
            deterministicRepeat,
            replay,
            defaultDecision,
            candidateDecision,
            killDecision,
            operatorDiagnosticsExposed,
            passes);
    }

    private static CandidateRunResult RunCandidate(int intervals)
    {
        var factory = new DesktopSustainedGenerationH29ActivationCandidateInitialConditionFactory();
        var engine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(factory.CreateRuntimeEngine());
        Assert.Equal(TimeSpan.FromMilliseconds(10d), engine.FixedDeltaTime);
        Assert.Equal(HydraulicNumericalCouplingMode.FourNodeBranchContinuityCorrectedCommitOptIn, CurrentHydraulics(engine).Mode);

        var probe = new DesktopHydraulicProductionTelemetryProbe();
        var generatorId = Assert.Single(engine.CreatePresentationSnapshot(ControlRoomRunState.Running).Electrical.Generators).GeneratorId;
        var rows = new List<ActivationRow>(intervals + 2);
        var runtimeStep = 0;

        for (var interval = 1; interval <= intervals; interval++)
        {
            if (interval == 257 && intervals >= 257)
            {
                QueueGeneratorLoad(engine, generatorId, ControlRoomCommandKind.GeneratorLoadLower);
                runtimeStep++;
                CaptureStep(interval, runtimeStep, isActionTransition: true);
            }
            else if (interval == 769 && intervals >= 769)
            {
                QueueGeneratorLoad(engine, generatorId, ControlRoomCommandKind.GeneratorLoadRaise);
                runtimeStep++;
                CaptureStep(interval, runtimeStep, isActionTransition: true);
            }

            runtimeStep++;
            CaptureStep(interval, runtimeStep, isActionTransition: false);
        }

        var telemetry = probe.Snapshot();
        Assert.Equal(rows.Count, telemetry.ObservedSteps);
        Assert.Equal(rows.Count, telemetry.FourNodeTelemetrySteps);
        Assert.Equal(rows.Count(static row => row.TriggerObserved), telemetry.TriggeredSteps);
        Assert.Equal(rows.Count(static row => row.CandidateEligible), telemetry.CandidateEligibleSteps);
        Assert.Equal(rows.Count(static row => row.CommitAuthorized), telemetry.CommitAuthorizedSteps);
        Assert.Equal(rows.Count(static row => row.CorrectedCommitted), telemetry.CorrectedCommittedSteps);
        Assert.Equal(rows.Count(static row => row.TriggerObserved && !row.CorrectedCommitted), telemetry.ExplicitFallbackSteps);
        Assert.Equal(rows.Count(static row => row.RollbackRequired), telemetry.RollbackSteps);
        Assert.Equal(rows.Count(static row => row.UntargetedBranchDisagreement), telemetry.UntargetedBranchDisagreementSteps);

        return new CandidateRunResult(CurrentHydraulics(engine).Mode, rows, telemetry);

        void CaptureStep(int interval, int step, bool isActionTransition)
        {
            var presentation = engine.Step(ControlRoomRunState.Running);
            Assert.False(presentation.AnyTripActive, $"Unexpected H.29 candidate trip at interval {interval}, runtime step {step}.");
            probe.Observe(engine);
            var numerics = CurrentHydraulics(engine);
            var item = Assert.IsType<FourNodeBranchContinuityIntegrationTelemetry>(numerics.FourNodeBranchContinuity);
            rows.Add(new ActivationRow(
                interval,
                step,
                isActionTransition,
                ControlRoomSnapshotFingerprint.Compute(presentation),
                item.TriggerObserved,
                item.ShadowCorrectedCandidateEligible,
                item.RollbackRequired,
                item.ProposedAuthority,
                item.Reason,
                item.CorrectedCommitArmEnabled,
                item.CorrectedCommitAuthorized,
                item.CorrectedCommitReason,
                item.CorrectedCandidateCommitted,
                item.UntargetedBranchDisagreementDetected,
                item.ShadowCorrectionEvaluated,
                item.ShadowConverged,
                item.ShadowLineSearchExhausted,
                item.ShadowMaximumRelativePressureResidual,
                item.ShadowMaximumAbsoluteFlowResidualKilogramsPerSecond,
                item.ShadowMassClosureKilogramsPerSecond,
                item.ShadowEnergyOwnershipResidualWatts));
        }
    }

    private static VersionedReplayResult VerifyVersionedReplayAndCheckpoint()
    {
        var explicitFactory = new DesktopSustainedGenerationInitialConditionFactory();
        var candidateFactory = new DesktopSustainedGenerationH29ActivationCandidateInitialConditionFactory();
        var registry = new VersionedInitialConditionRegistry(new IVersionedInitialConditionFactory[]
        {
            explicitFactory,
            candidateFactory,
        });
        var sessionFactory = new ScenarioSessionFactory(registry);

        var explicitSession = sessionFactory.Load(DesktopIntegratedOperationsProgram.Scenario);
        var explicitVersionStillLoadable = explicitSession.InitialCondition.Reference
            == DesktopSustainedGenerationInitialConditionFactory.Reference;
        Assert.True(explicitVersionStillLoadable);

        var session = sessionFactory.Load(DesktopIntegratedOperationsH29ActivationCandidateProgram.Scenario);
        Assert.Equal(DesktopSustainedGenerationH29ActivationCandidateInitialConditionFactory.Reference, session.InitialCondition.Reference);

        ScenarioRecording recording;
        ScenarioCheckpoint checkpoint;
        string finalFingerprint;
        using (var recorder = new ScenarioRecorder(session))
        {
            session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.Run));
            var first = session.Coordinator.AdvanceRunning(ReplayCheckpointStep, publicationStride: ReplayCheckpointStep);
            Assert.Equal(ReplayCheckpointStep, first.ExecutedStepCount);
            Assert.False(session.Coordinator.Current.AnyTripActive);
            checkpoint = recorder.CreateCheckpoint("h29-v3-midpoint");

            var second = session.Coordinator.AdvanceRunning(ReplaySteps - ReplayCheckpointStep, publicationStride: ReplaySteps - ReplayCheckpointStep);
            Assert.Equal(ReplaySteps - ReplayCheckpointStep, second.ExecutedStepCount);
            Assert.False(session.Coordinator.Current.AnyTripActive);
            finalFingerprint = ControlRoomSnapshotFingerprint.Compute(session.Coordinator.Current);
            recording = recorder.Complete();
        }

        Assert.Equal(DesktopSustainedGenerationH29ActivationCandidateInitialConditionFactory.Reference, recording.InitialCondition);
        var archive = ScenarioSessionArchive.FromRecording(
            "h29-production-activation-candidate-v3",
            DesktopIntegratedOperationsH29ActivationCandidateProgram.Scenario,
            recording);
        var runner = new ScenarioFullReplayRunner(sessionFactory);
        var fullReplay = runner.ReplayAndVerify(archive);
        var checkpointReplay = runner.SeekAndVerify(archive, checkpoint.CheckpointId);

        var fullReplayEquivalent = string.Equals(
            finalFingerprint,
            ControlRoomSnapshotFingerprint.Compute(fullReplay.Session.Coordinator.Current),
            StringComparison.Ordinal);
        var checkpointEquivalent = string.Equals(
            checkpoint.SnapshotFingerprint,
            ControlRoomSnapshotFingerprint.Compute(checkpointReplay.Session.Coordinator.Current),
            StringComparison.Ordinal);
        var candidateVersionPreserved = fullReplay.Session.InitialCondition.Reference
                == DesktopSustainedGenerationH29ActivationCandidateInitialConditionFactory.Reference
            && checkpointReplay.Session.InitialCondition.Reference
                == DesktopSustainedGenerationH29ActivationCandidateInitialConditionFactory.Reference;

        Assert.True(fullReplayEquivalent);
        Assert.True(checkpointEquivalent);
        Assert.True(candidateVersionPreserved);

        return new VersionedReplayResult(
            recording.FinalLogicalStep,
            recording.Frames.Count,
            checkpoint.LogicalStep,
            fullReplayEquivalent,
            checkpointEquivalent,
            candidateVersionPreserved,
            explicitVersionStillLoadable,
            finalFingerprint);
    }

    private static void AssertFailClosedSafety(ActivationRow row)
    {
        if (!row.TriggerObserved)
        {
            Assert.False(row.CorrectedCommitted);
            Assert.Equal(FourNodeBranchContinuityActivationReason.NotTriggered, row.ActivationReason);
            Assert.Equal(FourNodeBranchContinuityCorrectedCommitReason.NotTriggered, row.CommitReason);
        }

        if (row.RollbackRequired)
        {
            Assert.False(row.CorrectedCommitted);
        }

        if (row.CommitAuthorized)
        {
            Assert.True(row.CorrectedCommitted);
        }

        if (row.CorrectedCommitted)
        {
            Assert.True(row.CandidateEligible);
            Assert.False(row.RollbackRequired);
            Assert.Equal(FourNodeBranchContinuityProposedAuthority.CorrectedCandidate, row.ProposedAuthority);
            Assert.Equal(FourNodeBranchContinuityActivationReason.QualifiedTriggeredCorrection, row.ActivationReason);
            Assert.True(row.ShadowCorrectionEvaluated);
            Assert.True(row.ShadowConverged);
            Assert.False(row.ShadowLineSearchExhausted);
            Assert.InRange(row.ShadowMaximumRelativePressureResidual, 0d, 1e-5d);
            Assert.InRange(row.ShadowMaximumAbsoluteFlowResidualKilogramsPerSecond, 0d, 1e-2d);
            Assert.InRange(Math.Abs(row.ShadowMassClosureKilogramsPerSecond), 0d, 1e-8d);
            Assert.InRange(Math.Abs(row.ShadowEnergyOwnershipResidualWatts), 0d, 1e-3d);
            Assert.Equal(FourNodeBranchContinuityCorrectedCommitReason.QualifiedH20Authority, row.CommitReason);
            Assert.False(row.UntargetedBranchDisagreement);
        }
    }

    private static PlantNetworkHydraulicNumericalSnapshot CurrentHydraulics(IntegratedAutomaticOperationRuntimeEngine engine)
        => engine.LatestCanonicalSnapshot.Control.ProtectedControl.FullPlant.IntegratedCycle.PrimaryCircuit.HydraulicNumerics;

    private static void QueueGeneratorLoad(IControlRoomRuntimeEngine engine, string generatorId, ControlRoomCommandKind kind)
        => engine.QueueOperatorCommand(new ControlRoomCommand(kind, generatorId, ControlRoomCommandTargetKind.Generator));

    private static string Fingerprint(IReadOnlyList<ActivationRow> rows)
    {
        var canonical = string.Join(
            "||",
            rows.Select(static row => FormattableString.Invariant(
                $"{row.Interval}:{row.RuntimeStep}:{row.IsActionTransition}:{row.PresentationFingerprint}:{row.TriggerObserved}:{row.CandidateEligible}:{row.RollbackRequired}:{row.ProposedAuthority}:{row.ActivationReason}:{row.CommitArmEnabled}:{row.CommitAuthorized}:{row.CommitReason}:{row.CorrectedCommitted}:{row.UntargetedBranchDisagreement}:{row.ShadowCorrectionEvaluated}:{row.ShadowConverged}:{row.ShadowLineSearchExhausted}:{row.ShadowMaximumRelativePressureResidual:G17}:{row.ShadowMaximumAbsoluteFlowResidualKilogramsPerSecond:G17}:{row.ShadowMassClosureKilogramsPerSecond:G17}:{row.ShadowEnergyOwnershipResidualWatts:G17}")));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }

    private static void WriteReports(
        CandidateRunResult primary,
        bool deterministicRepeat,
        VersionedReplayResult replay,
        DesktopHydraulicProductionPolicyDecision defaultDecision,
        DesktopHydraulicProductionPolicyDecision candidateDecision,
        DesktopHydraulicProductionPolicyDecision killDecision,
        bool operatorDiagnosticsExposed,
        bool passes)
    {
        var directory = ReportDirectory();
        Directory.CreateDirectory(directory);
        var telemetryFingerprint = Fingerprint(primary.Rows);
        var transitionSteps = primary.Rows.Count(static row => row.IsActionTransition);
        var summary = new[]
        {
            "=== 01-current-v2-four-node-production-activation-candidate ===",
            "H.29 integrates the already-qualified H.20/H.22 corrected-commit path as an exact-version production-default candidate without changing H.9 mathematics, H.20 authority, H.22 ownership, P060/F040, branch-continuity limits, physical coefficients or the 10 ms fixed step. H.28 remains bounded-but-costly. H.29 does not itself change the authoritative current-v2 default; H.30 owns the final ACTIVATE / OPT-IN ONLY / REMAIN EXPLICIT decision.",
            FormattableString.Invariant($"qualification-intervals={QualificationIntervals}; action-transition-steps={transitionSteps}; runtime-steps={primary.Rows.Count}; production-fixed-step=10.000 ms; P060-F040-triggered={primary.Telemetry.TriggeredSteps}; H20-candidate-eligible={primary.Telemetry.CandidateEligibleSteps}; H22-commit-authorized={primary.Telemetry.CommitAuthorizedSteps}; corrected-candidates-committed={primary.Telemetry.CorrectedCommittedSteps}; H20-rollbacks={primary.Telemetry.RollbackSteps}; safe-explicit-fallbacks={primary.Telemetry.ExplicitFallbackSteps}; fallback-commit-violations={primary.Telemetry.FallbackCommitViolations}; unsafe-corrected-commits={primary.Telemetry.UnsafeCommitViolations}; untargeted-branch-disagreements={primary.Telemetry.UntargetedBranchDisagreementSteps};"),
            FormattableString.Invariant($"telemetry-observed-steps={primary.Telemetry.ObservedSteps}; telemetry-four-node-steps={primary.Telemetry.FourNodeTelemetrySteps}; rollback-reason-counter-entries={primary.Telemetry.RollbackReasonCounts.Count}; commit-reason-counter-entries={primary.Telemetry.CommitReasonCounts.Count}; deterministic-control-intervals={DeterminismRepeatIntervals}; deterministic-repeat={deterministicRepeat}; activation-telemetry-fingerprint={telemetryFingerprint};"),
            FormattableString.Invariant($"authoritative-default-policy={defaultDecision.EffectivePolicy}; authoritative-default-initial-condition-version={defaultDecision.InitialCondition.Version}; h29-candidate-policy={candidateDecision.EffectivePolicy}; h29-candidate-initial-condition-version={candidateDecision.InitialCondition.Version}; explicit-kill-requested={killDecision.ExplicitKillApplied}; explicit-kill-effective-policy={killDecision.EffectivePolicy}; explicit-kill-initial-condition-version={killDecision.InitialCondition.Version}; default-runtime-mode=ExplicitCommittedState; candidate-runtime-mode={primary.Mode};"),
            FormattableString.Invariant($"versioned-replay-steps={replay.FinalLogicalStep}; versioned-replay-frames={replay.FrameCount}; checkpoint-logical-step={replay.CheckpointLogicalStep}; full-replay-equivalent={replay.FullReplayEquivalent}; checkpoint-prefix-equivalent={replay.CheckpointEquivalent}; candidate-v3-version-preserved={replay.CandidateVersionPreserved}; explicit-v2-still-loadable={replay.ExplicitVersionStillLoadable}; final-presentation-fingerprint={replay.FinalPresentationFingerprint};"),
            FormattableString.Invariant($"operator-facing-numerical-diagnostics-exposed={operatorDiagnosticsExposed}; internal-production-telemetry-available=True; H23-H28-prerequisite-chain-frozen=True; post-H28-H24-requalification-frozen=True; H28-performance-class=bounded-but-costly; H20-contract-replaced=False; H22-commit-seam-replaced=False; H9-tolerances-retuned=False; P060-F040-retuned=False; bounded-hysteresis-limits-changed=False; physical-coefficient-retuning=False;"),
            FormattableString.Invariant($"four-node-production-activation-candidate-passes={passes}; h29-audit-passes={passes}; h30-closure-review-unblocked={passes};"),
            "H.29 recommendation: if this gate is green, promote H.29 only as a reviewed production activation candidate and proceed to H.30 for the evidence-derived closure decision. Keep the authoritative default ExplicitCommittedState until H.30 explicitly chooses ACTIVATE; preserve v2 as the rollback/reference mode under every H.30 outcome.",
        };
        File.WriteAllLines(Path.Combine(directory, "01-four-node-production-activation-candidate.summary.txt"), summary, Utf8WithoutBom);

        var csv = new List<string>
        {
            "interval,runtime_step,is_action_transition,presentation_fingerprint,trigger_observed,h20_candidate_eligible,h20_rollback_required,h20_proposed_authority,h20_reason,h22_commit_arm_enabled,h22_commit_authorized,h22_commit_reason,corrected_candidate_committed,untargeted_branch_disagreement,shadow_correction_evaluated,shadow_converged,shadow_line_search_exhausted,shadow_pressure_residual,shadow_flow_residual_kg_s,shadow_mass_closure_kg_s,shadow_energy_ownership_w",
        };
        csv.AddRange(primary.Rows.Select(static row => FormattableString.Invariant(
            $"{row.Interval},{row.RuntimeStep},{row.IsActionTransition},{row.PresentationFingerprint},{row.TriggerObserved},{row.CandidateEligible},{row.RollbackRequired},{row.ProposedAuthority},{row.ActivationReason},{row.CommitArmEnabled},{row.CommitAuthorized},{row.CommitReason},{row.CorrectedCommitted},{row.UntargetedBranchDisagreement},{row.ShadowCorrectionEvaluated},{row.ShadowConverged},{row.ShadowLineSearchExhausted},{row.ShadowMaximumRelativePressureResidual:G17},{row.ShadowMaximumAbsoluteFlowResidualKilogramsPerSecond:G17},{row.ShadowMassClosureKilogramsPerSecond:G17},{row.ShadowEnergyOwnershipResidualWatts:G17}")));
        File.WriteAllLines(Path.Combine(directory, "02-production-activation-candidate-step-telemetry.csv"), csv, Utf8WithoutBom);

        File.WriteAllLines(
            Path.Combine(directory, "03-production-activation-candidate-metrics.csv"),
            new[]
            {
                "metric,value",
                $"qualification_intervals,{QualificationIntervals}",
                $"runtime_steps,{primary.Rows.Count}",
                $"triggered,{primary.Telemetry.TriggeredSteps}",
                $"candidate_eligible,{primary.Telemetry.CandidateEligibleSteps}",
                $"commit_authorized,{primary.Telemetry.CommitAuthorizedSteps}",
                $"corrected_commits,{primary.Telemetry.CorrectedCommittedSteps}",
                $"rollbacks,{primary.Telemetry.RollbackSteps}",
                $"explicit_fallbacks,{primary.Telemetry.ExplicitFallbackSteps}",
                $"fallback_commit_violations,{primary.Telemetry.FallbackCommitViolations}",
                $"unsafe_commits,{primary.Telemetry.UnsafeCommitViolations}",
                $"untargeted_disagreements,{primary.Telemetry.UntargetedBranchDisagreementSteps}",
                $"deterministic_repeat,{deterministicRepeat}",
                $"activation_telemetry_fingerprint,{telemetryFingerprint}",
                $"candidate_initial_condition_version,{candidateDecision.InitialCondition.Version}",
                $"explicit_rollback_initial_condition_version,{killDecision.InitialCondition.Version}",
                $"full_replay_equivalent,{replay.FullReplayEquivalent}",
                $"checkpoint_equivalent,{replay.CheckpointEquivalent}",
                $"candidate_version_preserved,{replay.CandidateVersionPreserved}",
                $"explicit_version_still_loadable,{replay.ExplicitVersionStillLoadable}",
                $"operator_diagnostics_exposed,{operatorDiagnosticsExposed}",
                $"h29_audit_passes,{passes}",
            },
            Utf8WithoutBom);
    }

    private static string EvidenceDirectory()
        => Path.Combine(
            FindRepositoryRoot(),
            "tests",
            "NuclearReactorSimulator.Application.Tests",
            "Scenarios",
            "Gameplay",
            "Evidence");

    private static string ReportDirectory()
        => Path.Combine(FindRepositoryRoot(), "artifacts", "h29-four-node-production-activation-candidate");

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

    private static void ResetReportDirectory()
    {
        var directory = ReportDirectory();
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
        Directory.CreateDirectory(directory);
        WriteProgress("H.29 production activation candidate audit started");
    }

    private static void WriteProgress(string message)
        => File.WriteAllText(
            Path.Combine(ReportDirectory(), "00-progress.txt"),
            $"{DateTimeOffset.UtcNow:O} {message}{Environment.NewLine}",
            Utf8WithoutBom);

    private sealed record CandidateRunResult(
        HydraulicNumericalCouplingMode Mode,
        IReadOnlyList<ActivationRow> Rows,
        FourNodeProductionActivationTelemetrySnapshot Telemetry);

    private sealed record VersionedReplayResult(
        long FinalLogicalStep,
        int FrameCount,
        long CheckpointLogicalStep,
        bool FullReplayEquivalent,
        bool CheckpointEquivalent,
        bool CandidateVersionPreserved,
        bool ExplicitVersionStillLoadable,
        string FinalPresentationFingerprint);

    private sealed record ActivationRow(
        int Interval,
        int RuntimeStep,
        bool IsActionTransition,
        string PresentationFingerprint,
        bool TriggerObserved,
        bool CandidateEligible,
        bool RollbackRequired,
        FourNodeBranchContinuityProposedAuthority ProposedAuthority,
        FourNodeBranchContinuityActivationReason ActivationReason,
        bool CommitArmEnabled,
        bool CommitAuthorized,
        FourNodeBranchContinuityCorrectedCommitReason CommitReason,
        bool CorrectedCommitted,
        bool UntargetedBranchDisagreement,
        bool ShadowCorrectionEvaluated,
        bool ShadowConverged,
        bool ShadowLineSearchExhausted,
        double ShadowMaximumRelativePressureResidual,
        double ShadowMaximumAbsoluteFlowResidualKilogramsPerSecond,
        double ShadowMassClosureKilogramsPerSecond,
        double ShadowEnergyOwnershipResidualWatts);
}
