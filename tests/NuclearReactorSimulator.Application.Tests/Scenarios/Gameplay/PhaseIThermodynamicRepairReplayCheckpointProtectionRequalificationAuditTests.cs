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
/// I.5 REV1 Hotfix 13 repaired-closure replay/checkpoint/protection requalification stage 3.
/// Routes an audit-only exact-version factory through CorrelationConsistentInverseDomain and real H.29 corrected ownership,
/// then verifies full replay, checkpoint continuation and evidence-derived reverse-power protection. Production remains unchanged.
/// </summary>
public sealed class PhaseIThermodynamicRepairReplayCheckpointProtectionRequalificationAuditTests
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

    [Fact(Explicit = true)]
    [Trait("Category", "PhaseIThermodynamicRepairReplayCheckpointProtectionRequalification")]
    public void OptInCommittedTrajectory_ReplaysCheckpointAndPreservesReversePowerProtectionFailClosed()
    {
        ResetProgress();
        var evidenceFactory = new I5RepairCorrectedCommitInitialConditionFactory();
        var scenario = CreateEvidenceScenario(evidenceFactory.Descriptor.Reference);
        var sessionFactory = new ScenarioSessionFactory(
            new VersionedInitialConditionRegistry(new IVersionedInitialConditionFactory[] { evidenceFactory }));
        var session = sessionFactory.Load(scenario);
        var recordingEngine = evidenceFactory.LastCreated
            ?? throw new InvalidOperationException("I.5 repair Stage 3 evidence factory did not retain its created runtime.");
        using var recordingTrace = new I5RepairTraceCollector(recordingEngine, session.Coordinator);

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
                $"I.5 repair Stage 3 did not observe an in-flight reverse-power pickup within {MaximumPickupSearchSteps} steps.");
            Assert.False(session.Coordinator.Current.GeneratorTripActive);
            pickupCheckpoint = recorder.CreateCheckpoint("i5-repair-reverse-power-pickup-in-flight");
            WriteProgress($"reverse-power-checkpoint logical-step={pickupCheckpoint.LogicalStep} pickup-search-steps={pickupSearchSteps}");

            stepsFromCheckpointToTrip = 0;
            while (!session.Coordinator.Current.GeneratorTripActive && stepsFromCheckpointToTrip < MaximumTripSearchSteps)
            {
                AdvanceRunning(session.Coordinator, 1);
                stepsFromCheckpointToTrip++;
            }

            Assert.True(session.Coordinator.Current.GeneratorTripActive,
                $"I.5 repair Stage 3 reverse-power protection did not trip within {MaximumTripSearchSteps} steps after the in-flight checkpoint.");
            AssertReversePowerTripState(recordingEngine);
            expectedFinalFingerprint = ControlRoomSnapshotFingerprint.Compute(session.Coordinator.Current);
            recording = recorder.Complete();
        }

        var originalTrace = recordingTrace.Trace.ToArray();
        Assert.Equal(recording.FinalLogicalStep, originalTrace[^1].LogicalStep);
        Assert.Contains(originalTrace, static row => row.CorrectedCandidateCommitted);
        Assert.All(originalTrace, AssertFailClosedCommitSafety);

        var archive = ScenarioSessionArchive.FromRecording("i5-repair-corrected-commit-replay-protection", scenario, recording);
        var runner = new ScenarioFullReplayRunner(sessionFactory);

        I5RepairTraceCollector? replayCollector = null;
        var replay = runner.ReplayAndVerify(
            archive,
            replaySession =>
            {
                var replayRuntime = evidenceFactory.LastCreated
                    ?? throw new InvalidOperationException("I.5 repair Stage 3 full replay did not create an evidence runtime.");
                replayCollector = new I5RepairTraceCollector(replayRuntime, replaySession.Coordinator);
            });
        using var replayTrace = replayCollector
            ?? throw new InvalidOperationException("I.5 repair Stage 3 full replay trace observer was not attached.");
        var replayEngine = replayTrace.Engine;
        Assert.NotSame(recordingEngine, replayEngine);
        Assert.Equal(expectedFinalFingerprint, ControlRoomSnapshotFingerprint.Compute(replay.Session.Coordinator.Current));
        Assert.Equal(originalTrace, replayTrace.Trace.ToArray());
        AssertReversePowerTripState(replayEngine);
        WriteProgress($"full-replay-verified frames={recording.Frames.Count} trace={replayTrace.Trace.Count}");

        I5RepairTraceCollector? restoredCollector = null;
        var restored = runner.SeekAndVerify(
            archive,
            pickupCheckpoint.CheckpointId,
            restoredSession =>
            {
                var restoredRuntime = evidenceFactory.LastCreated
                    ?? throw new InvalidOperationException("I.5 repair Stage 3 checkpoint replay did not create an evidence runtime.");
                restoredCollector = new I5RepairTraceCollector(restoredRuntime, restoredSession.Coordinator);
            });
        using var restoredTrace = restoredCollector
            ?? throw new InvalidOperationException("I.5 repair Stage 3 checkpoint trace observer was not attached.");
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
            "I.5 repair Stage 3 Corrected Commit Replay / Protection Evidence",
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

    private static void AssertFailClosedCommitSafety(I5RepairTraceRow row)
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

    private static string TraceFingerprint(IReadOnlyList<I5RepairTraceRow> rows)
    {
        var canonical = string.Join(
            "||",
            rows.Select(static row => FormattableString.Invariant(
                $"{row.LogicalStep}:{row.TriggerObserved}:{row.H20CandidateEligible}:{row.RollbackRequired}:{row.ProposedAuthority}:{row.ActivationReason}:{row.CorrectedCommitArmEnabled}:{row.CorrectedCommitAuthorized}:{row.CommitReason}:{row.CorrectedCandidateCommitted}:{row.UntargetedBranchDisagreementDetected}:{row.ShadowCorrectionEvaluated}:{row.ShadowConverged}:{row.ShadowLineSearchExhausted}:{row.ShadowMaximumRelativePressureResidual:G17}:{row.ShadowMaximumAbsoluteFlowResidualKilogramsPerSecond:G17}:{row.ShadowMassClosureKilogramsPerSecond:G17}:{row.ShadowEnergyOwnershipResidualWatts:G17}:{row.BranchOverrideCount}:{row.PreviousPhaseHoldCount}:{row.HysteresisReleaseCount}:{row.MassClosureResidualKilograms:G17}:{row.EnergyClosureResidualJoules:G17}:{row.BalanceMassRateResidualKilogramsPerSecond:G17}:{row.BalancePowerResidualWatts:G17}:{row.ReversePowerPickupTicks}:{row.ReversePowerLatched}:{row.GeneratorTripActive}:{row.GeneratorBreakerClosed}")));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }

    private static void WriteReports(
        IReadOnlyList<I5RepairTraceRow> rows,
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
            "=== 01-i5-thermodynamic-repair-replay-checkpoint-protection-requalification-stage3 ===",
            "I.5 repair Stage 3 routes an audit-only scenario identity through the validated CorrelationConsistentInverseDomain closure and real corrected-commit ownership, then requalifies full replay, checkpoint continuation and reverse-power protection. Registered/default production identities remain unchanged.",
            FormattableString.Invariant($"recorded-steps={rows.Count}; production-fixed-step=10.000 ms; in-flight-checkpoint-logical-step={checkpointLogicalStep}; steps-checkpoint-to-generator-trip={stepsFromCheckpointToTrip}; generator-trip-final={final.GeneratorTripActive}; generator-breaker-finally-closed={final.GeneratorBreakerClosed}; reverse-power-latched={final.ReversePowerLatched};"),
            FormattableString.Invariant($"corrected-candidates-committed={commits}; H20-rollbacks={rollbacks}; fallback-commit-violations={fallbackCommitViolations}; unsafe-corrected-commits={unsafeCommits}; untargeted-branch-disagreements={untargetedDisagreements};"),
            FormattableString.Invariant($"max-network-mass-closure-kg={maximumMassClosure:G17}; max-network-energy-closure-j={maximumEnergyClosure:G17}; max-network-balance-mass-rate-kg-s={maximumBalanceMassRate:G17}; max-network-balance-power-w={maximumBalancePower:G17};"),
            FormattableString.Invariant($"full-replay-trace-equivalent={string.Equals(originalTraceFingerprint, replayTraceFingerprint, StringComparison.Ordinal)}; checkpoint-prefix-and-continuation-equivalent={string.Equals(originalTraceFingerprint, restoredTraceFingerprint, StringComparison.Ordinal)}; deterministic-trace-repeat={deterministicTraceRepeat}; telemetry-protection-trace-fingerprint={originalTraceFingerprint};"),
            FormattableString.Invariant($"default-current-v2-mode={defaultMode}; repaired-closure=CorrelationConsistentInverseDomain; opt-in-corrected-commit-mode=FourNodeBranchContinuityCorrectedCommitOptIn; production-activation=False; H20-contract-replaced=False; H19-target-set-changed=False; H9-tolerances-retuned=False; P060-F040-retuned=False; bounded-hysteresis-limits-changed=False; physical-coefficient-retuning=False;"),
            FormattableString.Invariant($"thermodynamic-repair-replay-checkpoint-protection-requalification-passes={passes}; i5-repair-replay-protection-stage3-passes={passes};"),
            "I.5 repair Stage 3 recommendation: if this replay/protection gate and the companion repaired off-design gate are green, preserve both as Stage-3 evidence and move next to repaired performance/cost/operational-soak qualification. Do not create a registered production identity yet.",
        };
        File.WriteAllLines(Path.Combine(directory, "01-i5-thermodynamic-repair-replay-checkpoint-protection-stage3.summary.txt"), summary, Utf8WithoutBom);

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
        File.WriteAllLines(Path.Combine(directory, "02-repaired-replay-protection-trace.csv"), csv, Utf8WithoutBom);

        File.WriteAllLines(
            Path.Combine(directory, "03-i5-thermodynamic-repair-replay-protection-stage3-metrics.csv"),
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
                FormattableString.Invariant($"i5_repair_replay_protection_stage3_passes,{passes}"),
            },
            Utf8WithoutBom);
    }


    private static string ReportDirectory()
        => Path.Combine(FindRepositoryRoot(), "artifacts", "i5-thermodynamic-repair-replay-checkpoint-protection-requalification-stage3");

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
        WriteProgress("I.5 repair Stage 3 committed replay/checkpoint/protection qualification started");
    }

    private static void WriteProgress(string message)
        => File.WriteAllText(
            Path.Combine(ReportDirectory(), "00-progress.txt"),
            $"{DateTimeOffset.UtcNow:O} {message}{Environment.NewLine}",
            Utf8WithoutBom);

    private sealed class I5RepairCorrectedCommitInitialConditionFactory : IVersionedInitialConditionFactory
    {
        public static InitialConditionReference Reference { get; } = new("desktop-sustained-generation-i5-repair-corrected-commit-evidence", 1);

        public InitialConditionDescriptor Descriptor { get; } = new(
            Reference,
            "I.5 Thermodynamic Repair Replay / Protection Evidence",
            "Audit-only exact-version factory delegating to the validated thermodynamic repair seam with H.29 corrected ownership. It is registered only by I.5 repair Stage 3 tests and cannot become a standard production factory.");

        public IntegratedAutomaticOperationRuntimeEngine? LastCreated { get; private set; }

        public IControlRoomRuntimeEngine CreateRuntimeEngine()
        {
            LastCreated = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
                DesktopSustainedGenerationInitialConditionFactory.CreateThermodynamicInverseDomainRepairEvidenceRuntimeEngine(Step, useFourNodeCorrectedCommit: true));
            return LastCreated;
        }
    }

    private sealed class I5RepairTraceCollector : IDisposable
    {
        private readonly List<I5RepairTraceRow> _trace = new();
        private readonly ControlRoomRuntimeCoordinator _coordinator;
        private bool _disposed;

        public I5RepairTraceCollector(
            IntegratedAutomaticOperationRuntimeEngine engine,
            ControlRoomRuntimeCoordinator coordinator)
        {
            Engine = engine ?? throw new ArgumentNullException(nameof(engine));
            _coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
            _coordinator.DeterministicStepCompleted += OnDeterministicStepCompleted;
        }

        public IntegratedAutomaticOperationRuntimeEngine Engine { get; }

        public IReadOnlyList<I5RepairTraceRow> Trace => _trace;

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

        private static I5RepairTraceRow CaptureTrace(IntegratedAutomaticOperationRuntimeEngine engine)
        {
            var protectedControl = engine.LatestCanonicalSnapshot.Control.ProtectedControl;
            var numerics = protectedControl.FullPlant.IntegratedCycle.PrimaryCircuit.HydraulicNumerics;
            Assert.Equal(HydraulicNumericalCouplingMode.FourNodeBranchContinuityCorrectedCommitOptIn, numerics.Mode);
            var telemetry = Assert.IsType<FourNodeBranchContinuityIntegrationTelemetry>(numerics.FourNodeBranchContinuity);
            var audit = protectedControl.FullPlant.IntegratedCycle.PrimaryCircuit.Audit;
            var reverse = protectedControl.Protection.Functions.Single(static function => function.FunctionId == "generator-reverse-power");
            var generator = Assert.Single(protectedControl.FullPlant.IntegratedCycle.GeneratorGrid.Generators);

            return new I5RepairTraceRow(
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

    private sealed record I5RepairTraceRow(
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
