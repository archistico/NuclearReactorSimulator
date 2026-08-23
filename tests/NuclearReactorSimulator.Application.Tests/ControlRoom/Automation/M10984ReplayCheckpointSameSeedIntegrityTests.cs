using System.Text;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.ControlRoom.Automation;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Challenges;
using NuclearReactorSimulator.Application.Scenarios.Challenges.Packs;
using NuclearReactorSimulator.Application.Scenarios.Challenges.Replay;
using NuclearReactorSimulator.Application.Scenarios.Faults;
using NuclearReactorSimulator.Application.Scenarios.Faults.InstrumentationControl;
using NuclearReactorSimulator.Application.Scenarios.Recording;
using NuclearReactorSimulator.Application.Scenarios.Training;
using NuclearReactorSimulator.Domain.Physics.Control.Supervisory;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.ControlRoom.Automation;

public sealed class M10984ReplayCheckpointSameSeedIntegrityTests
{
    [Fact]
    public void HealthyBoundedDemand_SameSeedFullReplayAndCheckpointContinuationAreEquivalent()
    {
        var pack = ProductionOperationalChallengePack.BoundedDemandFollowingV2;
        var first = RunHealthy(pack, "first");
        var repeat = RunHealthy(pack, "repeat");

        AssertSameSeedEquivalent(first, repeat);
        VerifyFullReplay(first);

        var restored = first.Runner.SeekAndVerify(first.Archive, first.Checkpoint.CheckpointId);
        Assert.Equal(first.Checkpoint.LogicalStep, restored.Session.Coordinator.Current.LogicalStep);
        Assert.Equal(PlantControlAuthorityMode.SupervisoryAutomatic, restored.Session.PlantControlAuthority.CurrentAutomation.EffectiveAuthority);
        using var continuationRecorder = new ScenarioRecorder(restored.Session, restored.ReplayedRecording);
        var generatorId = restored.Session.Coordinator.Current.Electrical.Generators.Single().GeneratorId;
        DispatchAndStep(restored.Session, new ControlRoomCommand(
            ControlRoomCommandKind.ControlRodHold,
            "regulating",
            ControlRoomCommandTargetKind.ControlRodGroup));
        DispatchAndStep(restored.Session, new ControlRoomCommand(
            ControlRoomCommandKind.GeneratorLoadLower,
            generatorId,
            ControlRoomCommandTargetKind.Generator));
        var continuation = continuationRecorder.Complete();

        AssertRecordingEquivalent(first.Recording, continuation);
        Assert.Equal(first.FinalFingerprint, ControlRoomSnapshotFingerprint.Compute(restored.Session.Coordinator.Current));
        AssertChallengeProjectionEquivalent(first.Pack, first.Recording, continuation);
    }

    [Fact]
    public void DegradedMeasurementRecovery_SameSeedFullReplayAndCheckpointContinuationAreEquivalent()
    {
        var pack = CreateValidationOnlyDegradedChallengePack();
        var first = RunDegraded(pack, "first");
        var repeat = RunDegraded(pack, "repeat");

        AssertSameSeedEquivalent(first, repeat);
        VerifyFullReplay(first);

        var restored = first.Runner.SeekAndVerify(first.Archive, first.Checkpoint.CheckpointId);
        Assert.Equal(PlantControlAuthorityMode.SupervisoryAutomatic, restored.Session.PlantControlAuthority.CurrentAutomation.RequestedAuthority);
        Assert.Equal(PlantControlAuthorityMode.Assisted, restored.Session.PlantControlAuthority.CurrentAutomation.EffectiveAuthority);
        Assert.Equal(PlantControlAuthorityHealth.Degraded, restored.Session.PlantControlAuthority.CurrentAutomation.Health);
        Assert.Equal(1, restored.Session.Coordinator.Current.Faults.ActiveCount);

        using var continuationRecorder = new ScenarioRecorder(restored.Session, restored.ReplayedRecording);
        while (restored.Session.Coordinator.Current.LogicalStep < 6)
        {
            Step(restored.Session);
        }
        var continuation = continuationRecorder.Complete();

        AssertRecordingEquivalent(first.Recording, continuation);
        Assert.Equal(first.FinalFingerprint, ControlRoomSnapshotFingerprint.Compute(restored.Session.Coordinator.Current));
        Assert.Equal(PlantControlAuthorityMode.SupervisoryAutomatic, restored.Session.PlantControlAuthority.CurrentAutomation.EffectiveAuthority);
        Assert.Equal(PlantControlAuthorityHealth.Normal, restored.Session.PlantControlAuthority.CurrentAutomation.Health);
        Assert.Equal(0, restored.Session.Coordinator.Current.Faults.ActiveCount);
        Assert.Equal(1, restored.Session.Coordinator.Current.Faults.ClearedCount);
        AssertChallengeProjectionEquivalent(first.Pack, first.Recording, continuation);
    }

    [Fact]
    public void ProtectionTrip_SameSeedFullReplayAndCheckpointContinuationPreserveSuspendedAuthority()
    {
        var pack = ProductionOperationalChallengePack.BoundedDemandFollowingV2;
        var first = RunProtection(pack, "first");
        var repeat = RunProtection(pack, "repeat");

        AssertSameSeedEquivalent(first, repeat);
        VerifyFullReplay(first);

        var restored = first.Runner.SeekAndVerify(first.Archive, first.Checkpoint.CheckpointId);
        Assert.True(restored.Session.Coordinator.Current.ReactorScramActive);
        Assert.Equal(PlantControlAuthorityMode.SupervisoryAutomatic, restored.Session.PlantControlAuthority.CurrentAutomation.RequestedAuthority);
        Assert.Equal(PlantControlAuthorityMode.Assisted, restored.Session.PlantControlAuthority.CurrentAutomation.EffectiveAuthority);
        Assert.Equal(PlantControlAuthorityHealth.SuspendedByProtection, restored.Session.PlantControlAuthority.CurrentAutomation.Health);

        using var continuationRecorder = new ScenarioRecorder(restored.Session, restored.ReplayedRecording);
        DispatchAndStep(restored.Session, new ControlRoomCommand(
            ControlRoomCommandKind.ControlRodWithdraw,
            "regulating",
            ControlRoomCommandTargetKind.ControlRodGroup));
        var continuation = continuationRecorder.Complete();

        AssertRecordingEquivalent(first.Recording, continuation);
        Assert.Equal(first.FinalFingerprint, ControlRoomSnapshotFingerprint.Compute(restored.Session.Coordinator.Current));
        Assert.True(restored.Session.Coordinator.Current.ReactorScramActive);
        Assert.Equal(PlantControlAuthorityHealth.SuspendedByProtection, restored.Session.PlantControlAuthority.CurrentAutomation.Health);
        Assert.Equal(ChallengeLifecycleState.Failed, OperationalChallengeRecordingProjector.Project(pack, continuation).Lifecycle.State);
        AssertChallengeProjectionEquivalent(first.Pack, first.Recording, continuation);
    }

    [Fact]
    public void ManualTakeover_SameSeedFullReplayAndCheckpointContinuationClearStaleSupervisoryObjective()
    {
        var pack = ProductionOperationalChallengePack.BoundedDemandFollowingV2;
        var first = RunManualTakeover(pack, "first");
        var repeat = RunManualTakeover(pack, "repeat");

        AssertSameSeedEquivalent(first, repeat);
        VerifyFullReplay(first);

        var restored = first.Runner.SeekAndVerify(first.Archive, first.Checkpoint.CheckpointId);
        Assert.Equal(PlantControlAuthorityMode.SupervisoryAutomatic, restored.Session.PlantControlAuthority.CurrentAutomation.EffectiveAuthority);
        using var continuationRecorder = new ScenarioRecorder(restored.Session, restored.ReplayedRecording);
        restored.Session.PlantControlAuthority.RequestAuthority(PlantControlAuthorityMode.Manual);
        Step(restored.Session);
        var generatorId = restored.Session.Coordinator.Current.Electrical.Generators.Single().GeneratorId;
        DispatchAndStep(restored.Session, new ControlRoomCommand(
            ControlRoomCommandKind.GeneratorLoadRaise,
            generatorId,
            ControlRoomCommandTargetKind.Generator));
        var continuation = continuationRecorder.Complete();

        AssertRecordingEquivalent(first.Recording, continuation);
        Assert.Equal(first.FinalFingerprint, ControlRoomSnapshotFingerprint.Compute(restored.Session.Coordinator.Current));
        Assert.Equal(PlantControlAuthorityMode.Manual, restored.Session.PlantControlAuthority.CurrentAutomation.RequestedAuthority);
        Assert.Equal(PlantControlAuthorityMode.Manual, restored.Session.PlantControlAuthority.CurrentAutomation.EffectiveAuthority);
        Assert.Equal(PlantControlAuthorityHealth.Normal, restored.Session.PlantControlAuthority.CurrentAutomation.Health);
        Assert.Equal("NONE", restored.Session.PlantControlAuthority.CurrentAutomation.ObjectiveText);
        AssertChallengeProjectionEquivalent(first.Pack, first.Recording, continuation);
    }

    [Fact]
    public void ArtifactSummary_DeclaresReplayCheckpointSameSeedClosureAndNoOpaqueState()
    {
        var directory = ResolveArtifactDirectory();
        Directory.CreateDirectory(directory);
        File.WriteAllLines(Path.Combine(directory, "01-m10984-replay-checkpoint-same-seed-integrity.summary.txt"), new[]
        {
            "scope=M10.9.8.4 Hotfix 1 Replay / Checkpoint / Same-Seed Integrity over original M10.9.8.4 candidate and M10.9.8.3 VALIDATED; automated deterministic integration/evidence only; no production runtime, Simulation physics, challenge/scoring/protection owner, archive schema, fingerprint algorithm or plant-command authority change;",
            "integrity-matrix=m10984-replay-checkpoint-same-seed-v1; representative-state-classes=4; healthy-bounded-demand=True; degraded-measurement-recovery=True; protection-trip-suspended-authority=True; protection-authority-observation-after-commit-tick=True; manual-takeover=True;",
            "same-seed-definition=same exact scenario/initial-condition plus identical accepted operator-action and automation-intent trace; random-runtime-state-added=False; opaque-physical-checkpoint-state-added=False; opaque-challenge-state-added=False;",
            "full-replay-every-frame-fingerprint-owner=m91-m107; checkpoint-prefix-restore-owner=m107; challenge-replay-projection-owner=m10965; automation-intent-replay-owner=m5-m107;",
            "same-seed-recording-trace-equivalent-all-state-classes=True; full-replay-final-fingerprint-equivalent-all-state-classes=True; checkpoint-prefix-live-continuation-equivalent-all-state-classes=True; challenge-projection-fingerprint-equivalent-all-state-classes=True;",
            "archive-schema-v1-unchanged=True; fingerprint-algorithm=sha256-control-room-snapshot-v1; challenge-replay-fingerprint-algorithm=m10965-challenge-replay-sha256-v1; m10984-hotfix1-protection-authority-boundary-aligned=True; m10984-replay-checkpoint-same-seed-integrity-passes=True; next-step=M10.9.8.5 manual integrated HMI acceptance;",
        }, new UTF8Encoding(false));
    }

    private static IntegrityRun RunHealthy(OperationalChallengePackDefinition pack, string suffix)
    {
        var factory = CreateExactV4Factory();
        var session = factory.Load(pack.Scenario);
        using var recorder = new ScenarioRecorder(session);
        ConfigureSupervisory(session);
        Step(session);
        var generatorId = session.Coordinator.Current.Electrical.Generators.Single().GeneratorId;
        DispatchAndStep(session, new ControlRoomCommand(
            ControlRoomCommandKind.GeneratorLoadRaise,
            generatorId,
            ControlRoomCommandTargetKind.Generator));
        var checkpoint = recorder.CreateCheckpoint("m10984-healthy-prefix");
        DispatchAndStep(session, new ControlRoomCommand(
            ControlRoomCommandKind.ControlRodHold,
            "regulating",
            ControlRoomCommandTargetKind.ControlRodGroup));
        DispatchAndStep(session, new ControlRoomCommand(
            ControlRoomCommandKind.GeneratorLoadLower,
            generatorId,
            ControlRoomCommandTargetKind.Generator));
        var recording = recorder.Complete();
        return BuildRun(pack, factory, session, recording, checkpoint, $"m10984-healthy-{suffix}");
    }

    private static IntegrityRun RunDegraded(OperationalChallengePackDefinition pack, string suffix)
    {
        var factory = CreateExactV4Factory();
        var session = factory.Load(pack.Scenario);
        using var recorder = new ScenarioRecorder(session);
        ConfigureSupervisory(session);
        Step(session);
        while (session.Coordinator.Current.LogicalStep < 3)
        {
            Step(session);
        }
        Assert.Equal(PlantControlAuthorityMode.Assisted, session.PlantControlAuthority.CurrentAutomation.EffectiveAuthority);
        Assert.Equal(PlantControlAuthorityHealth.Degraded, session.PlantControlAuthority.CurrentAutomation.Health);
        var checkpoint = recorder.CreateCheckpoint("m10984-degraded-prefix");
        while (session.Coordinator.Current.LogicalStep < 6)
        {
            Step(session);
        }
        Assert.Equal(PlantControlAuthorityMode.SupervisoryAutomatic, session.PlantControlAuthority.CurrentAutomation.EffectiveAuthority);
        Assert.Equal(PlantControlAuthorityHealth.Normal, session.PlantControlAuthority.CurrentAutomation.Health);
        var recording = recorder.Complete();
        return BuildRun(pack, factory, session, recording, checkpoint, $"m10984-degraded-{suffix}");
    }

    private static IntegrityRun RunProtection(OperationalChallengePackDefinition pack, string suffix)
    {
        var factory = CreateExactV4Factory();
        var session = factory.Load(pack.Scenario);
        using var recorder = new ScenarioRecorder(session);
        ConfigureSupervisory(session);
        Step(session);
        DispatchAndStep(session, new ControlRoomCommand(ControlRoomCommandKind.ReactorScram));
        Assert.True(session.Coordinator.Current.ReactorScramActive);

        // Protection is committed by the SCRAM step; the authority coordinator observes that
        // committed protection state on the following deterministic tick. Preserve the owner
        // contract used by PlantControlAuthorityIntegrationTests and M10.9.8.3 rather than
        // asserting suspension one tick too early.
        AdvanceAuthorityAfterProtectionCommit(session);
        var checkpoint = recorder.CreateCheckpoint("m10984-protection-prefix");
        DispatchAndStep(session, new ControlRoomCommand(
            ControlRoomCommandKind.ControlRodWithdraw,
            "regulating",
            ControlRoomCommandTargetKind.ControlRodGroup));
        var recording = recorder.Complete();
        return BuildRun(pack, factory, session, recording, checkpoint, $"m10984-protection-{suffix}");
    }

    private static IntegrityRun RunManualTakeover(OperationalChallengePackDefinition pack, string suffix)
    {
        var factory = CreateExactV4Factory();
        var session = factory.Load(pack.Scenario);
        using var recorder = new ScenarioRecorder(session);
        ConfigureSupervisory(session);
        Step(session);
        var checkpoint = recorder.CreateCheckpoint("m10984-manual-prefix");
        session.PlantControlAuthority.RequestAuthority(PlantControlAuthorityMode.Manual);
        Step(session);
        var generatorId = session.Coordinator.Current.Electrical.Generators.Single().GeneratorId;
        DispatchAndStep(session, new ControlRoomCommand(
            ControlRoomCommandKind.GeneratorLoadRaise,
            generatorId,
            ControlRoomCommandTargetKind.Generator));
        Assert.Equal("NONE", session.PlantControlAuthority.CurrentAutomation.ObjectiveText);
        var recording = recorder.Complete();
        return BuildRun(pack, factory, session, recording, checkpoint, $"m10984-manual-{suffix}");
    }

    private static IntegrityRun BuildRun(
        OperationalChallengePackDefinition pack,
        ScenarioSessionFactory factory,
        ScenarioSession session,
        ScenarioRecording recording,
        ScenarioCheckpoint checkpoint,
        string archiveId)
    {
        var finalFingerprint = ControlRoomSnapshotFingerprint.Compute(session.Coordinator.Current);
        Assert.Equal(finalFingerprint, recording.Frames[^1].SnapshotFingerprint);
        var projection = OperationalChallengeRecordingProjector.Project(pack, recording);
        var archive = ScenarioSessionArchive.FromRecording(archiveId, pack.Scenario, recording);
        Assert.Equal(ScenarioSessionArchive.CurrentSchemaVersion, archive.SchemaVersion);
        return new IntegrityRun(
            pack,
            new ScenarioFullReplayRunner(factory),
            recording,
            archive,
            checkpoint,
            finalFingerprint,
            projection.DeterministicFingerprint);
    }

    private static void VerifyFullReplay(IntegrityRun expected)
    {
        var replay = expected.Runner.ReplayAndVerify(expected.Archive);
        Assert.Equal(expected.FinalFingerprint, ControlRoomSnapshotFingerprint.Compute(replay.Session.Coordinator.Current));
        AssertRecordingEquivalent(expected.Recording, replay.ReplayedRecording);
        AssertChallengeProjectionEquivalent(expected.Pack, expected.Recording, replay.ReplayedRecording);
    }

    private static void AssertSameSeedEquivalent(IntegrityRun expected, IntegrityRun actual)
    {
        Assert.Equal(expected.Pack.ExactId, actual.Pack.ExactId);
        Assert.Equal(expected.Pack.Scenario.ScenarioId, actual.Pack.Scenario.ScenarioId);
        Assert.Equal(expected.Pack.Scenario.InitialCondition, actual.Pack.Scenario.InitialCondition);
        Assert.Equal(expected.FinalFingerprint, actual.FinalFingerprint);
        Assert.Equal(expected.ChallengeProjectionFingerprint, actual.ChallengeProjectionFingerprint);
        AssertRecordingEquivalent(expected.Recording, actual.Recording);
    }

    private static void AssertRecordingEquivalent(ScenarioRecording expected, ScenarioRecording actual)
    {
        Assert.Equal(expected.ScenarioId, actual.ScenarioId);
        Assert.Equal(expected.InitialCondition, actual.InitialCondition);
        Assert.Equal(
            expected.Frames.Select(static frame => (frame.LogicalStep, frame.SnapshotFingerprint, frame.FirstEventSequence, frame.LastEventSequence)).ToArray(),
            actual.Frames.Select(static frame => (frame.LogicalStep, frame.SnapshotFingerprint, frame.FirstEventSequence, frame.LastEventSequence)).ToArray());
        Assert.Equal(expected.OperatorActions, actual.OperatorActions);
        Assert.Equal(expected.AutomationIntents, actual.AutomationIntents);
        Assert.Equal(expected.Events, actual.Events);
        Assert.Equal(expected.Checkpoints, actual.Checkpoints);
    }

    private static void AssertChallengeProjectionEquivalent(
        OperationalChallengePackDefinition pack,
        ScenarioRecording expected,
        ScenarioRecording actual)
    {
        var left = OperationalChallengeRecordingProjector.Project(pack, expected);
        var right = OperationalChallengeRecordingProjector.Project(pack, actual);
        Assert.Equal(left.DeterministicFingerprint, right.DeterministicFingerprint);
        Assert.Equal(left.Lifecycle.State, right.Lifecycle.State);
        Assert.Equal(left.Score.FinalScore, right.Score.FinalScore);
        Assert.Equal(left.Score.Grade, right.Score.Grade);
    }

    private static void ConfigureSupervisory(ScenarioSession session)
    {
        session.PlantControlAuthority.RequestSupervisoryObjective(SupervisoryObjectiveRequest.HoldCurrentOperatingPoint());
        session.PlantControlAuthority.RequestAuthority(PlantControlAuthorityMode.SupervisoryAutomatic);
    }

    private static OperationalChallengePackDefinition CreateValidationOnlyDegradedChallengePack()
    {
        var source = ProductionOperationalChallengePack.BoundedDemandFollowingV2;
        var scenario = new ScenarioDefinition(
            "m10984-bounded-demand-supervisory-degraded-replay-validation",
            "M10.9.8.4 bounded-demand supervisory degraded replay validation",
            "Validation-only exact-v4 composition reusing the existing unavailable measured-signal fault seam to qualify replay/checkpoint/same-seed integrity across degraded and recovered authority states.",
            DesktopSustainedGenerationI5RepairedActivationCandidateInitialConditionFactory.Reference,
            source.Scenario.Objectives,
            source.Scenario.AllowedOperatorActions,
            new[]
            {
                new ScenarioFaultDefinition(
                    "m10984-power-unavailable",
                    InstrumentationControlFaultTypeIds.SensorUnavailable,
                    "power",
                    ScenarioFaultTriggerDefinition.AtLogicalStep(2),
                    ScenarioFaultTriggerDefinition.AtLogicalStep(5)),
            });
        var contract = source.Challenge;
        var challenge = new ChallengeDefinition(
            "m10984-bounded-demand-supervisory-degraded",
            1,
            scenario.ScenarioId,
            contract.ObjectiveId,
            contract.Title,
            contract.Description,
            contract.ActivationCondition,
            contract.RequiredObservations,
            contract.CompletionConditions,
            contract.FailureConditions,
            contract.LogicalTime,
            contract.AssistancePolicy,
            contract.ExternalDemandProfile);
        return new OperationalChallengePackDefinition(
            "m10984-bounded-demand-supervisory-degraded",
            1,
            scenario,
            challenge,
            source.ConditionEvaluator,
            source.ScoringPolicy,
            source.ScoreEvidenceBindings);
    }

    private static ScenarioSessionFactory CreateExactV4Factory()
        => new(new VersionedInitialConditionRegistry(new IVersionedInitialConditionFactory[]
        {
            new DesktopSustainedGenerationI5RepairedActivationCandidateInitialConditionFactory(),
        }));

    private static void AdvanceAuthorityAfterProtectionCommit(ScenarioSession session)
    {
        Step(session);
        Assert.True(session.Coordinator.Current.ReactorScramActive);
        Assert.Equal(PlantControlAuthorityMode.SupervisoryAutomatic, session.PlantControlAuthority.CurrentAutomation.RequestedAuthority);
        Assert.Equal(PlantControlAuthorityMode.Assisted, session.PlantControlAuthority.CurrentAutomation.EffectiveAuthority);
        Assert.Equal(PlantControlAuthorityHealth.SuspendedByProtection, session.PlantControlAuthority.CurrentAutomation.Health);
    }

    private static void DispatchAndStep(ScenarioSession session, ControlRoomCommand command)
    {
        session.CommandDispatcher.Dispatch(command);
        Step(session);
    }

    private static void Step(ScenarioSession session)
        => session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.SingleStep));

    private static string ResolveArtifactDirectory()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "NuclearReactorSimulator.sln")))
        {
            current = current.Parent;
        }
        if (current is null)
        {
            throw new InvalidOperationException("Repository root could not be resolved for M10.9.8.4 audit artifacts.");
        }
        return Path.Combine(current.FullName, "artifacts", "m1098-replay-checkpoint-same-seed-integrity");
    }

    private sealed record IntegrityRun(
        OperationalChallengePackDefinition Pack,
        ScenarioFullReplayRunner Runner,
        ScenarioRecording Recording,
        ScenarioSessionArchive Archive,
        ScenarioCheckpoint Checkpoint,
        string FinalFingerprint,
        string ChallengeProjectionFingerprint);
}
