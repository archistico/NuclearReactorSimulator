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
/// M10.9.4.1-H.23 qualification of the user-validated H.22 corrected-commit path through the existing
/// versioned scenario recorder/full-replay/checkpoint authority and evidence-derived electrical protections.
/// H.23 changes no numerical runtime behavior and leaves every standard current-v2 factory explicit.
/// </summary>
public sealed class FourNodeCommittedReplayProtectionQualificationAuditTests
{
    private static readonly TimeSpan Step = TimeSpan.FromMilliseconds(10d);
    private static readonly Encoding Utf8WithoutBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    private const int PreTripSteadySteps = 500;
    private const int MaximumPickupSearchSteps = 1_000;
    private const int MaximumTripSearchSteps = 1_000;
    private const double MaximumMassClosureResidualKilograms = 1e-6d;
    private const double MaximumEnergyClosureResidualJoules = 1e-2d;
    private const double MaximumBalanceMassRateResidualKilogramsPerSecond = 1e-8d;
    private const double MaximumBalancePowerResidualWatts = 1e-3d;

    private static readonly IReadOnlyDictionary<string, string> FrozenH22Fingerprints =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["H22_ValidatedCorrectedCommitSeamSummary.txt"] = "1328E3EC5D22336F2AB8412AE764F0873B0A5721F26C610C12865831A34463D6",
            ["H22_ValidatedCorrectedCommitSeamTelemetry.csv"] = "DE2EA4CA5042BB7F5A1BA9442C923ADA767AF237E9AD670DDD5485712B133F9B",
            ["H22_ValidatedCorrectedCommitSeamMetrics.csv"] = "78DCCC34D3B5BFB0AB0C96F13027E1E1D6832D6578AC2F032708361139623DB3",
        };

    [Fact]
    public void FrozenH22Evidence_RetainsValidatedCorrectedCommitSeam()
    {
        var evidenceDirectory = EvidenceDirectory();
        foreach (var expected in FrozenH22Fingerprints)
        {
            var path = Path.Combine(evidenceDirectory, expected.Key);
            Assert.True(File.Exists(path), $"Frozen H.22 evidence file is missing: {expected.Key}");
            Assert.Equal(expected.Value, CanonicalSha256(path));
        }

        var summary = File.ReadAllText(Path.Combine(evidenceDirectory, "H22_ValidatedCorrectedCommitSeamSummary.txt"));
        Assert.Contains("P060-F040-triggered=443", summary, StringComparison.Ordinal);
        Assert.Contains("H20-candidate-eligible=443", summary, StringComparison.Ordinal);
        Assert.Contains("H22-commit-authorized=443", summary, StringComparison.Ordinal);
        Assert.Contains("corrected-candidates-committed=443", summary, StringComparison.Ordinal);
        Assert.Contains("fallback-commit-violations=0", summary, StringComparison.Ordinal);
        Assert.Contains("unsafe-corrected-commits=0", summary, StringComparison.Ordinal);
        Assert.Contains("deterministic-repeat=True", summary, StringComparison.Ordinal);
        Assert.Contains("four-node-corrected-candidate-commit-seam-passes=True", summary, StringComparison.Ordinal);
        Assert.Contains("h22-audit-passes=True", summary, StringComparison.Ordinal);
    }

    [Fact(Explicit = true)]
    [Trait("Category", "FourNodeCommittedReplayProtectionQualificationAudit")]
    public void OptInCommittedTrajectory_ReplaysCheckpointAndPreservesReversePowerProtectionFailClosed()
    {
        ResetProgress();
        var evidenceFactory = new H23CorrectedCommitInitialConditionFactory();
        var scenario = CreateEvidenceScenario(evidenceFactory.Descriptor.Reference);
        var sessionFactory = new ScenarioSessionFactory(
            new VersionedInitialConditionRegistry(new IVersionedInitialConditionFactory[] { evidenceFactory }));
        var session = sessionFactory.Load(scenario);
        var recordingEngine = evidenceFactory.LastCreated
            ?? throw new InvalidOperationException("H.23 evidence factory did not retain its created runtime.");
        using var recordingTrace = new H23TraceCollector(recordingEngine, session.Coordinator);

        Assert.Equal(HydraulicNumericalCouplingMode.FourNodeBranchContinuityCorrectedCommitOptIn, CurrentHydraulics(recordingEngine).Mode);
        Assert.Empty(recordingTrace.Trace);

        ScenarioRecording recording;
        ScenarioCheckpoint pickupCheckpoint;
        int stepsFromCheckpointToTrip;
        string expectedFinalFingerprint;

        using (var recorder = new ScenarioRecorder(session))
        {
            session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.Run));
            AdvanceRunning(session.Coordinator, PreTripSteadySteps);
            Assert.False(session.Coordinator.Current.AnyTripActive);
            Assert.Contains(recordingTrace.Trace, static row => row.CorrectedCandidateCommitted);
            WriteProgress($"steady-control-complete steps={PreTripSteadySteps} commits={recordingTrace.Trace.Count(static row => row.CorrectedCandidateCommitted)}");

            var generator = Assert.Single(session.Coordinator.Current.Electrical.Generators);
            session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.TurbineTrip));
            session.CommandDispatcher.Dispatch(new ControlRoomCommand(
                ControlRoomCommandKind.GeneratorLoadLower,
                generator.GeneratorId,
                ControlRoomCommandTargetKind.Generator));

            var pickupSearchSteps = 0;
            while (!ReversePowerPickupIsInFlight(recordingEngine) && pickupSearchSteps < MaximumPickupSearchSteps)
            {
                AdvanceRunning(session.Coordinator, 1);
                pickupSearchSteps++;
            }

            Assert.True(ReversePowerPickupIsInFlight(recordingEngine),
                $"H.23 did not observe an in-flight reverse-power pickup within {MaximumPickupSearchSteps} steps.");
            Assert.False(session.Coordinator.Current.GeneratorTripActive);
            pickupCheckpoint = recorder.CreateCheckpoint("h23-reverse-power-pickup-in-flight");
            WriteProgress($"reverse-power-checkpoint logical-step={pickupCheckpoint.LogicalStep} pickup-search-steps={pickupSearchSteps}");

            stepsFromCheckpointToTrip = 0;
            while (!session.Coordinator.Current.GeneratorTripActive && stepsFromCheckpointToTrip < MaximumTripSearchSteps)
            {
                AdvanceRunning(session.Coordinator, 1);
                stepsFromCheckpointToTrip++;
            }

            Assert.True(session.Coordinator.Current.GeneratorTripActive,
                $"H.23 reverse-power protection did not trip within {MaximumTripSearchSteps} steps after the in-flight checkpoint.");
            AssertReversePowerTripState(recordingEngine);
            expectedFinalFingerprint = ControlRoomSnapshotFingerprint.Compute(session.Coordinator.Current);
            recording = recorder.Complete();
        }

        var originalTrace = recordingTrace.Trace.ToArray();
        Assert.Equal(recording.FinalLogicalStep, originalTrace[^1].LogicalStep);
        Assert.Contains(originalTrace, static row => row.CorrectedCandidateCommitted);
        Assert.All(originalTrace, AssertFailClosedCommitSafety);

        var archive = ScenarioSessionArchive.FromRecording("h23-corrected-commit-replay-protection", scenario, recording);
        var runner = new ScenarioFullReplayRunner(sessionFactory);

        H23TraceCollector? replayCollector = null;
        var replay = runner.ReplayAndVerify(
            archive,
            replaySession =>
            {
                var replayRuntime = evidenceFactory.LastCreated
                    ?? throw new InvalidOperationException("H.23 full replay did not create an evidence runtime.");
                replayCollector = new H23TraceCollector(replayRuntime, replaySession.Coordinator);
            });
        using var replayTrace = replayCollector
            ?? throw new InvalidOperationException("H.23 full replay trace observer was not attached.");
        var replayEngine = replayTrace.Engine;
        Assert.NotSame(recordingEngine, replayEngine);
        Assert.Equal(expectedFinalFingerprint, ControlRoomSnapshotFingerprint.Compute(replay.Session.Coordinator.Current));
        Assert.Equal(originalTrace, replayTrace.Trace.ToArray());
        AssertReversePowerTripState(replayEngine);
        WriteProgress($"full-replay-verified frames={recording.Frames.Count} trace={replayTrace.Trace.Count}");

        H23TraceCollector? restoredCollector = null;
        var restored = runner.SeekAndVerify(
            archive,
            pickupCheckpoint.CheckpointId,
            restoredSession =>
            {
                var restoredRuntime = evidenceFactory.LastCreated
                    ?? throw new InvalidOperationException("H.23 checkpoint replay did not create an evidence runtime.");
                restoredCollector = new H23TraceCollector(restoredRuntime, restoredSession.Coordinator);
            });
        using var restoredTrace = restoredCollector
            ?? throw new InvalidOperationException("H.23 checkpoint trace observer was not attached.");
        var restoredEngine = restoredTrace.Engine;
        Assert.NotSame(replayEngine, restoredEngine);
        Assert.Equal(pickupCheckpoint.LogicalStep, restored.Session.Coordinator.Current.LogicalStep);
        Assert.Equal(pickupCheckpoint.SnapshotFingerprint, ControlRoomSnapshotFingerprint.Compute(restored.Session.Coordinator.Current));
        Assert.False(restored.Session.Coordinator.Current.GeneratorTripActive);
        Assert.True(ReversePowerPickupIsInFlight(restoredEngine));
        Assert.Equal(originalTrace.Take(restoredTrace.Trace.Count).ToArray(), restoredTrace.Trace.ToArray());

        restored.Session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.Run));
        AdvanceRunning(restored.Session.Coordinator, stepsFromCheckpointToTrip);
        Assert.True(restored.Session.Coordinator.Current.GeneratorTripActive);
        Assert.Equal(expectedFinalFingerprint, ControlRoomSnapshotFingerprint.Compute(restored.Session.Coordinator.Current));
        Assert.Equal(originalTrace, restoredTrace.Trace.ToArray());
        AssertReversePowerTripState(restoredEngine);
        WriteProgress($"checkpoint-continuation-verified continuation-steps={stepsFromCheckpointToTrip}");

        var standardEngine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            new DesktopSustainedGenerationInitialConditionFactory().CreateRuntimeEngine());
        var defaultMode = CurrentHydraulics(standardEngine).Mode;
        Assert.Equal(HydraulicNumericalCouplingMode.ExplicitCommittedState, defaultMode);

        var commits = originalTrace.Count(static row => row.CorrectedCandidateCommitted);
        var rollbacks = originalTrace.Count(static row => row.RollbackRequired);
        var fallbackCommitViolations = originalTrace.Count(static row => row.RollbackRequired && row.CorrectedCandidateCommitted);
        var unsafeCommits = originalTrace.Count(static row => row.CorrectedCandidateCommitted
            && (!row.H20CandidateEligible
                || !row.CorrectedCommitAuthorized
                || row.RollbackRequired
                || row.UntargetedBranchDisagreementDetected
                || !row.ShadowCorrectionEvaluated
                || !row.ShadowConverged
                || row.ShadowLineSearchExhausted
                || row.ShadowMaximumRelativePressureResidual > 1e-5d
                || row.ShadowMaximumAbsoluteFlowResidualKilogramsPerSecond > 1e-2d
                || row.ShadowMassClosureKilogramsPerSecond > 1e-8d
                || row.ShadowEnergyOwnershipResidualWatts > 1e-3d));
        var untargetedDisagreements = originalTrace.Count(static row => row.UntargetedBranchDisagreementDetected);
        var maximumMassClosure = originalTrace.Max(static row => row.MassClosureResidualKilograms);
        var maximumEnergyClosure = originalTrace.Max(static row => row.EnergyClosureResidualJoules);
        var maximumBalanceMassRate = originalTrace.Max(static row => row.BalanceMassRateResidualKilogramsPerSecond);
        var maximumBalancePower = originalTrace.Max(static row => row.BalancePowerResidualWatts);
        var traceFingerprint = TraceFingerprint(originalTrace);
        var replayTraceFingerprint = TraceFingerprint(replayTrace.Trace);
        var restoredTraceFingerprint = TraceFingerprint(restoredTrace.Trace);
        var deterministicTraceRepeat = string.Equals(traceFingerprint, replayTraceFingerprint, StringComparison.Ordinal)
            && string.Equals(traceFingerprint, restoredTraceFingerprint, StringComparison.Ordinal);
        var passes = commits > 0
            && fallbackCommitViolations == 0
            && unsafeCommits == 0
            && deterministicTraceRepeat
            && replay.Session.Coordinator.Current.GeneratorTripActive
            && restored.Session.Coordinator.Current.GeneratorTripActive
            && defaultMode == HydraulicNumericalCouplingMode.ExplicitCommittedState;

        Assert.True(passes);
        WriteReports(
            originalTrace,
            pickupCheckpoint.LogicalStep,
            stepsFromCheckpointToTrip,
            commits,
            rollbacks,
            fallbackCommitViolations,
            unsafeCommits,
            untargetedDisagreements,
            maximumMassClosure,
            maximumEnergyClosure,
            maximumBalanceMassRate,
            maximumBalancePower,
            traceFingerprint,
            replayTraceFingerprint,
            restoredTraceFingerprint,
            deterministicTraceRepeat,
            defaultMode,
            passes);
    }

    private static ScenarioDefinition CreateEvidenceScenario(InitialConditionReference initialCondition)
        => new(
            "h23-four-node-corrected-commit-replay-protection-evidence",
            "H.23 Corrected Commit Replay / Protection Evidence",
            "Audit-only scenario identity using the H.22 corrected-commit runtime through the existing deterministic recording, replay, checkpoint and protection seams.",
            initialCondition,
            DesktopIntegratedOperationsProgram.Scenario.Objectives,
            DesktopIntegratedOperationsProgram.Scenario.AllowedOperatorActions);

    private static void AdvanceRunning(ControlRoomRuntimeCoordinator coordinator, int stepCount)
    {
        for (var index = 0; index < stepCount; index++)
        {
            var result = coordinator.AdvanceRunning(1, publicationStride: 1);
            Assert.Equal(1, result.ExecutedStepCount);
        }
    }

    private static bool ReversePowerPickupIsInFlight(IntegratedAutomaticOperationRuntimeEngine engine)
    {
        var protection = engine.LatestCanonicalSnapshot.Control.ProtectedControl.Protection;
        var reverse = protection.Functions.Single(static function => function.FunctionId == "generator-reverse-power");
        return reverse.PickupElapsed > TimeSpan.Zero && !reverse.IsLatched && !protection.GeneratorTripActive;
    }

    private static void AssertReversePowerTripState(IntegratedAutomaticOperationRuntimeEngine engine)
    {
        var protectedControl = engine.LatestCanonicalSnapshot.Control.ProtectedControl;
        var reverse = protectedControl.Protection.Functions.Single(static function => function.FunctionId == "generator-reverse-power");
        var generator = Assert.Single(protectedControl.FullPlant.IntegratedCycle.GeneratorGrid.Generators);
        Assert.True(protectedControl.Protection.GeneratorTripActive);
        Assert.True(reverse.IsLatched);
        Assert.False(generator.BreakerFinallyClosed);
    }

    private static void AssertFailClosedCommitSafety(H23TraceRow row)
    {
        Assert.True(row.CorrectedCommitArmEnabled);
        Assert.Equal(row.CorrectedCommitAuthorized, row.CorrectedCandidateCommitted);
        Assert.InRange(row.MassClosureResidualKilograms, 0d, MaximumMassClosureResidualKilograms);
        Assert.InRange(row.EnergyClosureResidualJoules, 0d, MaximumEnergyClosureResidualJoules);
        Assert.InRange(row.BalanceMassRateResidualKilogramsPerSecond, 0d, MaximumBalanceMassRateResidualKilogramsPerSecond);
        Assert.InRange(row.BalancePowerResidualWatts, 0d, MaximumBalancePowerResidualWatts);

        if (!row.H20CandidateEligible)
        {
            Assert.False(row.CorrectedCommitAuthorized);
            Assert.False(row.CorrectedCandidateCommitted);
        }

        if (row.CorrectedCandidateCommitted)
        {
            Assert.True(row.H20CandidateEligible);
            Assert.True(row.ShadowCorrectionEvaluated);
            Assert.True(row.ShadowConverged);
            Assert.False(row.ShadowLineSearchExhausted);
            Assert.True(row.CorrectedCommitAuthorized);
            Assert.False(row.RollbackRequired);
            Assert.False(row.UntargetedBranchDisagreementDetected);
            Assert.Equal(FourNodeBranchContinuityProposedAuthority.CorrectedCandidate, row.ProposedAuthority);
            Assert.Equal(FourNodeBranchContinuityActivationReason.QualifiedTriggeredCorrection, row.ActivationReason);
            Assert.Equal(FourNodeBranchContinuityCorrectedCommitReason.QualifiedH20Authority, row.CommitReason);
            Assert.InRange(row.ShadowMaximumRelativePressureResidual, 0d, 1e-5d);
            Assert.InRange(row.ShadowMaximumAbsoluteFlowResidualKilogramsPerSecond, 0d, 1e-2d);
            Assert.InRange(row.ShadowMassClosureKilogramsPerSecond, 0d, 1e-8d);
            Assert.InRange(row.ShadowEnergyOwnershipResidualWatts, 0d, 1e-3d);
        }

        if (row.RollbackRequired)
        {
            Assert.False(row.CorrectedCandidateCommitted);
            Assert.False(row.CorrectedCommitAuthorized);
        }
    }

    private static PlantNetworkHydraulicNumericalSnapshot CurrentHydraulics(IntegratedAutomaticOperationRuntimeEngine engine)
        => engine.LatestCanonicalSnapshot.Control.ProtectedControl.FullPlant.IntegratedCycle.PrimaryCircuit.HydraulicNumerics;

    private static string TraceFingerprint(IReadOnlyList<H23TraceRow> rows)
    {
        var canonical = string.Join(
            "||",
            rows.Select(static row => FormattableString.Invariant(
                $"{row.LogicalStep}:{row.TriggerObserved}:{row.H20CandidateEligible}:{row.RollbackRequired}:{row.ProposedAuthority}:{row.ActivationReason}:{row.CorrectedCommitArmEnabled}:{row.CorrectedCommitAuthorized}:{row.CommitReason}:{row.CorrectedCandidateCommitted}:{row.UntargetedBranchDisagreementDetected}:{row.ShadowCorrectionEvaluated}:{row.ShadowConverged}:{row.ShadowLineSearchExhausted}:{row.ShadowMaximumRelativePressureResidual:G17}:{row.ShadowMaximumAbsoluteFlowResidualKilogramsPerSecond:G17}:{row.ShadowMassClosureKilogramsPerSecond:G17}:{row.ShadowEnergyOwnershipResidualWatts:G17}:{row.BranchOverrideCount}:{row.PreviousPhaseHoldCount}:{row.HysteresisReleaseCount}:{row.MassClosureResidualKilograms:G17}:{row.EnergyClosureResidualJoules:G17}:{row.BalanceMassRateResidualKilogramsPerSecond:G17}:{row.BalancePowerResidualWatts:G17}:{row.ReversePowerPickupTicks}:{row.ReversePowerLatched}:{row.GeneratorTripActive}:{row.GeneratorBreakerClosed}")));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }

    private static void WriteReports(
        IReadOnlyList<H23TraceRow> rows,
        long checkpointLogicalStep,
        int stepsFromCheckpointToTrip,
        int commits,
        int rollbacks,
        int fallbackCommitViolations,
        int unsafeCommits,
        int untargetedDisagreements,
        double maximumMassClosure,
        double maximumEnergyClosure,
        double maximumBalanceMassRate,
        double maximumBalancePower,
        string originalTraceFingerprint,
        string replayTraceFingerprint,
        string restoredTraceFingerprint,
        bool deterministicTraceRepeat,
        HydraulicNumericalCouplingMode defaultMode,
        bool passes)
    {
        var directory = ReportDirectory();
        Directory.CreateDirectory(directory);
        var final = rows[^1];
        var summary = new[]
        {
            "=== 01-current-v2-four-node-committed-replay-checkpoint-protection-qualification ===",
            "H.23 keeps the validated H.22 runtime unchanged and qualifies its separately opt-in corrected-commit trajectory through the existing exact-version scenario recorder/full-replay/checkpoint authority and evidence-derived reverse-power generator protection. Default current-v2 remains ExplicitCommittedState.",
            FormattableString.Invariant($"recorded-steps={rows.Count}; production-fixed-step=10.000 ms; in-flight-checkpoint-logical-step={checkpointLogicalStep}; steps-checkpoint-to-generator-trip={stepsFromCheckpointToTrip}; generator-trip-final={final.GeneratorTripActive}; generator-breaker-finally-closed={final.GeneratorBreakerClosed}; reverse-power-latched={final.ReversePowerLatched};"),
            FormattableString.Invariant($"corrected-candidates-committed={commits}; H20-rollbacks={rollbacks}; fallback-commit-violations={fallbackCommitViolations}; unsafe-corrected-commits={unsafeCommits}; untargeted-branch-disagreements={untargetedDisagreements};"),
            FormattableString.Invariant($"max-network-mass-closure-kg={maximumMassClosure:G17}; max-network-energy-closure-j={maximumEnergyClosure:G17}; max-network-balance-mass-rate-kg-s={maximumBalanceMassRate:G17}; max-network-balance-power-w={maximumBalancePower:G17};"),
            FormattableString.Invariant($"full-replay-trace-equivalent={string.Equals(originalTraceFingerprint, replayTraceFingerprint, StringComparison.Ordinal)}; checkpoint-prefix-and-continuation-equivalent={string.Equals(originalTraceFingerprint, restoredTraceFingerprint, StringComparison.Ordinal)}; deterministic-trace-repeat={deterministicTraceRepeat}; telemetry-protection-trace-fingerprint={originalTraceFingerprint};"),
            FormattableString.Invariant($"default-current-v2-mode={defaultMode}; opt-in-corrected-commit-mode=FourNodeBranchContinuityCorrectedCommitOptIn; H22-runtime-changed=False; H20-contract-replaced=False; H19-target-set-changed=False; H9-tolerances-retuned=False; P060-F040-retuned=False; bounded-hysteresis-limits-changed=False; physical-coefficient-retuning=False;"),
            FormattableString.Invariant($"four-node-committed-replay-checkpoint-protection-qualification-passes={passes}; h23-audit-passes={passes};"),
            "H.23 recommendation: if this gate is green, retain default production explicit and move next to committed long-horizon/cross-profile qualification. Replay/checkpoint and reverse-power protection interaction are then qualified for the opt-in H.22 ownership path, but off-design robustness remains unqualified.",
        };
        File.WriteAllLines(Path.Combine(directory, "01-four-node-committed-replay-checkpoint-protection.summary.txt"), summary, Utf8WithoutBom);

        var csv = new List<string>
        {
            "logical_step,trigger_observed,h20_candidate_eligible,h20_rollback_required,h20_proposed_authority,h20_reason,h22_commit_arm_enabled,h22_commit_authorized,h22_commit_reason,corrected_candidate_committed,untargeted_branch_disagreement,shadow_correction_evaluated,shadow_converged,shadow_line_search_exhausted,shadow_pressure_residual,shadow_flow_residual_kg_s,shadow_mass_closure_kg_s,shadow_energy_ownership_w,branch_overrides,previous_phase_holds,hysteresis_releases,mass_closure_kg,energy_closure_j,balance_mass_rate_kg_s,balance_power_w,reverse_power_pickup_ticks,reverse_power_latched,generator_trip_active,generator_breaker_closed",
        };
        csv.AddRange(rows.Select(static row => string.Join(",",
            row.LogicalStep,
            row.TriggerObserved,
            row.H20CandidateEligible,
            row.RollbackRequired,
            row.ProposedAuthority,
            row.ActivationReason,
            row.CorrectedCommitArmEnabled,
            row.CorrectedCommitAuthorized,
            row.CommitReason,
            row.CorrectedCandidateCommitted,
            row.UntargetedBranchDisagreementDetected,
            row.ShadowCorrectionEvaluated,
            row.ShadowConverged,
            row.ShadowLineSearchExhausted,
            FormattableString.Invariant($"{row.ShadowMaximumRelativePressureResidual:G17}"),
            FormattableString.Invariant($"{row.ShadowMaximumAbsoluteFlowResidualKilogramsPerSecond:G17}"),
            FormattableString.Invariant($"{row.ShadowMassClosureKilogramsPerSecond:G17}"),
            FormattableString.Invariant($"{row.ShadowEnergyOwnershipResidualWatts:G17}"),
            row.BranchOverrideCount,
            row.PreviousPhaseHoldCount,
            row.HysteresisReleaseCount,
            FormattableString.Invariant($"{row.MassClosureResidualKilograms:G17}"),
            FormattableString.Invariant($"{row.EnergyClosureResidualJoules:G17}"),
            FormattableString.Invariant($"{row.BalanceMassRateResidualKilogramsPerSecond:G17}"),
            FormattableString.Invariant($"{row.BalancePowerResidualWatts:G17}"),
            row.ReversePowerPickupTicks,
            row.ReversePowerLatched,
            row.GeneratorTripActive,
            row.GeneratorBreakerClosed)));
        File.WriteAllLines(Path.Combine(directory, "02-replay-protection-trace.csv"), csv, Utf8WithoutBom);

        File.WriteAllLines(
            Path.Combine(directory, "03-four-node-committed-replay-protection-metrics.csv"),
            new[]
            {
                "metric,value",
                FormattableString.Invariant($"recorded_steps,{rows.Count}"),
                FormattableString.Invariant($"checkpoint_logical_step,{checkpointLogicalStep}"),
                FormattableString.Invariant($"steps_checkpoint_to_trip,{stepsFromCheckpointToTrip}"),
                FormattableString.Invariant($"corrected_commits,{commits}"),
                FormattableString.Invariant($"rollbacks,{rollbacks}"),
                FormattableString.Invariant($"fallback_commit_violations,{fallbackCommitViolations}"),
                FormattableString.Invariant($"unsafe_commits,{unsafeCommits}"),
                FormattableString.Invariant($"untargeted_disagreements,{untargetedDisagreements}"),
                FormattableString.Invariant($"max_mass_closure_kg,{maximumMassClosure:G17}"),
                FormattableString.Invariant($"max_energy_closure_j,{maximumEnergyClosure:G17}"),
                FormattableString.Invariant($"max_balance_mass_rate_kg_s,{maximumBalanceMassRate:G17}"),
                FormattableString.Invariant($"max_balance_power_w,{maximumBalancePower:G17}"),
                FormattableString.Invariant($"trace_fingerprint,{originalTraceFingerprint}"),
                FormattableString.Invariant($"replay_trace_fingerprint,{replayTraceFingerprint}"),
                FormattableString.Invariant($"restored_trace_fingerprint,{restoredTraceFingerprint}"),
                FormattableString.Invariant($"deterministic_trace_repeat,{deterministicTraceRepeat}"),
                FormattableString.Invariant($"generator_trip_final,{final.GeneratorTripActive}"),
                FormattableString.Invariant($"reverse_power_latched,{final.ReversePowerLatched}"),
                FormattableString.Invariant($"h23_audit_passes,{passes}"),
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
        => Path.Combine(FindRepositoryRoot(), "artifacts", "h23-four-node-committed-replay-protection-qualification");

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
        WriteProgress("H.23 committed replay/checkpoint/protection qualification started");
    }

    private static void WriteProgress(string message)
        => File.WriteAllText(
            Path.Combine(ReportDirectory(), "00-progress.txt"),
            $"{DateTimeOffset.UtcNow:O} {message}{Environment.NewLine}",
            Utf8WithoutBom);

    private sealed class H23CorrectedCommitInitialConditionFactory : IVersionedInitialConditionFactory
    {
        public static InitialConditionReference Reference { get; } = new("desktop-sustained-generation-h23-corrected-commit-evidence", 1);

        public InitialConditionDescriptor Descriptor { get; } = new(
            Reference,
            "H.23 Four-Node Corrected Commit Replay / Protection Evidence",
            "Audit-only exact-version factory delegating to the validated H.22 corrected-commit opt-in desktop runtime. It is registered only by H.23 tests and cannot become a standard production factory.");

        public IntegratedAutomaticOperationRuntimeEngine? LastCreated { get; private set; }

        public IControlRoomRuntimeEngine CreateRuntimeEngine()
        {
            LastCreated = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
                DesktopSustainedGenerationInitialConditionFactory.CreateFourNodeCorrectedCommitEvidenceRuntimeEngine(Step));
            return LastCreated;
        }
    }

    private sealed class H23TraceCollector : IDisposable
    {
        private readonly List<H23TraceRow> _trace = new();
        private readonly ControlRoomRuntimeCoordinator _coordinator;
        private bool _disposed;

        public H23TraceCollector(
            IntegratedAutomaticOperationRuntimeEngine engine,
            ControlRoomRuntimeCoordinator coordinator)
        {
            Engine = engine ?? throw new ArgumentNullException(nameof(engine));
            _coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
            _coordinator.DeterministicStepCompleted += OnDeterministicStepCompleted;
        }

        public IntegratedAutomaticOperationRuntimeEngine Engine { get; }

        public IReadOnlyList<H23TraceRow> Trace => _trace;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _coordinator.DeterministicStepCompleted -= OnDeterministicStepCompleted;
            _disposed = true;
        }

        private void OnDeterministicStepCompleted(object? sender, ControlRoomSnapshotChangedEventArgs args)
        {
            Assert.Equal(args.Snapshot.LogicalStep, Engine.LogicalStep);
            _trace.Add(CaptureTrace(Engine));
        }

        private static H23TraceRow CaptureTrace(IntegratedAutomaticOperationRuntimeEngine engine)
        {
            var protectedControl = engine.LatestCanonicalSnapshot.Control.ProtectedControl;
            var numerics = protectedControl.FullPlant.IntegratedCycle.PrimaryCircuit.HydraulicNumerics;
            Assert.Equal(HydraulicNumericalCouplingMode.FourNodeBranchContinuityCorrectedCommitOptIn, numerics.Mode);
            var telemetry = Assert.IsType<FourNodeBranchContinuityIntegrationTelemetry>(numerics.FourNodeBranchContinuity);
            var audit = protectedControl.FullPlant.IntegratedCycle.PrimaryCircuit.Audit;
            var reverse = protectedControl.Protection.Functions.Single(static function => function.FunctionId == "generator-reverse-power");
            var generator = Assert.Single(protectedControl.FullPlant.IntegratedCycle.GeneratorGrid.Generators);

            return new H23TraceRow(
                engine.LogicalStep,
                telemetry.TriggerObserved,
                telemetry.ShadowCorrectedCandidateEligible,
                telemetry.RollbackRequired,
                telemetry.ProposedAuthority,
                telemetry.Reason,
                telemetry.CorrectedCommitArmEnabled,
                telemetry.CorrectedCommitAuthorized,
                telemetry.CorrectedCommitReason,
                telemetry.CorrectedCandidateCommitted,
                telemetry.UntargetedBranchDisagreementDetected,
                telemetry.ShadowCorrectionEvaluated,
                telemetry.ShadowConverged,
                telemetry.ShadowLineSearchExhausted,
                telemetry.ShadowMaximumRelativePressureResidual,
                telemetry.ShadowMaximumAbsoluteFlowResidualKilogramsPerSecond,
                telemetry.ShadowMassClosureKilogramsPerSecond,
                telemetry.ShadowEnergyOwnershipResidualWatts,
                telemetry.BranchOverrideCount,
                telemetry.PreviousPhaseHoldCount,
                telemetry.HysteresisReleaseCount,
                Math.Abs(audit.MassClosureResidualKilograms),
                Math.Abs(audit.EnergyClosureResidualJoules),
                Math.Abs(audit.BalanceMassRateResidualKilogramsPerSecond),
                Math.Abs(audit.BalancePowerResidualWatts),
                reverse.PickupElapsed.Ticks,
                reverse.IsLatched,
                protectedControl.Protection.GeneratorTripActive,
                generator.BreakerFinallyClosed);
        }
    }

    private sealed record H23TraceRow(
        long LogicalStep,
        bool TriggerObserved,
        bool H20CandidateEligible,
        bool RollbackRequired,
        FourNodeBranchContinuityProposedAuthority ProposedAuthority,
        FourNodeBranchContinuityActivationReason ActivationReason,
        bool CorrectedCommitArmEnabled,
        bool CorrectedCommitAuthorized,
        FourNodeBranchContinuityCorrectedCommitReason CommitReason,
        bool CorrectedCandidateCommitted,
        bool UntargetedBranchDisagreementDetected,
        bool ShadowCorrectionEvaluated,
        bool ShadowConverged,
        bool ShadowLineSearchExhausted,
        double ShadowMaximumRelativePressureResidual,
        double ShadowMaximumAbsoluteFlowResidualKilogramsPerSecond,
        double ShadowMassClosureKilogramsPerSecond,
        double ShadowEnergyOwnershipResidualWatts,
        int BranchOverrideCount,
        int PreviousPhaseHoldCount,
        int HysteresisReleaseCount,
        double MassClosureResidualKilograms,
        double EnergyClosureResidualJoules,
        double BalanceMassRateResidualKilogramsPerSecond,
        double BalancePowerResidualWatts,
        long ReversePowerPickupTicks,
        bool ReversePowerLatched,
        bool GeneratorTripActive,
        bool GeneratorBreakerClosed);
}
