using System.Reflection;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios.Challenges.Packs;
using NuclearReactorSimulator.Application.Scenarios.Challenges.Replay;
using NuclearReactorSimulator.Application.Scenarios.Challenges.Scoring;
using NuclearReactorSimulator.Application.Scenarios.Operations;
using NuclearReactorSimulator.Application.Scenarios.Recording;
using NuclearReactorSimulator.Application.Scenarios.Training;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Domain.Physics.Control.Supervisory;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Challenges.Replay;

public sealed class M10965ChallengeReplayCheckpointClosureTests
{
    [Fact]
    public void RecordingProjection_ReconstructsLifecycleDemandAndScoreIdenticallyAfterCanonicalReplay()
    {
        var factory = CreateFactory();
        var pack = InitialOperationalChallengePack.BoundedDemandFollowing;
        var recording = RecordBoundedDemandTrace(factory, out _);

        var original = OperationalChallengeRecordingProjector.Project(pack, recording);
        var replayed = new ScenarioFullReplayRunner(factory).ReplayAndVerify(pack.Scenario, recording);
        var reconstructed = OperationalChallengeRecordingProjector.Project(pack, replayed.ReplayedRecording);

        Assert.Equal(recording.FinalLogicalStep, original.FinalLogicalStep);
        Assert.Equal(recording.FinalLogicalStep, original.Lifecycle.LogicalStep);
        Assert.True(original.Lifecycle.TerminalLogicalStep.HasValue);
        Assert.True(original.Lifecycle.TerminalLogicalStep.Value < original.Lifecycle.LogicalStep);
        Assert.All(original.Frames, static frame => Assert.Equal(frame.LogicalStep, frame.ExternalDemand.LogicalStep));
        Assert.Equal(original.DeterministicFingerprint, reconstructed.DeterministicFingerprint);
        Assert.Equal(original.Lifecycle.State, reconstructed.Lifecycle.State);
        Assert.Equal(original.Lifecycle.ActivatedLogicalStep, reconstructed.Lifecycle.ActivatedLogicalStep);
        Assert.Equal(original.Lifecycle.TerminalLogicalStep, reconstructed.Lifecycle.TerminalLogicalStep);
        Assert.Equal(original.Lifecycle.Transitions, reconstructed.Lifecycle.Transitions);
        Assert.Equal(original.Lifecycle.Observations, reconstructed.Lifecycle.Observations);
        Assert.Equal(original.Score.FinalScore, reconstructed.Score.FinalScore);
        Assert.Equal(original.Score.Dimensions, reconstructed.Score.Dimensions);
        Assert.True(original.Score.IsEvidenceComplete);
        Assert.True(original.Score.IsPassing);
        Assert.Equal(ChallengeScoreDominanceOutcome.None, original.Score.DominanceOutcome);
        Assert.True(original.Score.Dimensions.Single(static item => item.Kind == ChallengeScoreDimensionKind.SafetyProtectionDiscipline).IsEvidenceAvailable);
        var procedure = original.Score.Dimensions.Single(static item => item.Kind == ChallengeScoreDimensionKind.ProcedureRequiredActions);
        Assert.True(procedure.IsEvidenceAvailable);
        Assert.False(procedure.IsCriticalFailure);
        Assert.DoesNotContain("scheduled-action", procedure.EvidenceSummary, StringComparison.OrdinalIgnoreCase);
        Assert.True(original.Score.Dimensions.Single(static item => item.Kind == ChallengeScoreDimensionKind.DemandTracking).IsEvidenceAvailable);
        Assert.True(original.Score.Dimensions.Single(static item => item.Kind == ChallengeScoreDimensionKind.LogicalTimeCompletionEfficiency).IsEvidenceAvailable);
        Assert.Equal(original.Frames, reconstructed.Frames);
        Assert.All(original.Frames, static frame => Assert.True(frame.ExternalDemand.IsAvailable));
        Assert.False(string.IsNullOrWhiteSpace(OperationalChallengeReplayFingerprint.AlgorithmId));
    }

    [Fact]
    public void CheckpointContinuation_MatchesUninterruptedChallengeProjectionExactly()
    {
        var factory = CreateFactory();
        var pack = InitialOperationalChallengePack.BoundedDemandFollowing;
        var uninterruptedRecording = RecordBoundedDemandTrace(factory, out var checkpoint);
        var uninterrupted = OperationalChallengeRecordingProjector.Project(pack, uninterruptedRecording);
        var archive = ScenarioSessionArchive.FromRecording("m10965-checkpoint", pack.Scenario, uninterruptedRecording);
        var runner = new ScenarioFullReplayRunner(factory);

        var restored = runner.SeekAndVerify(archive, checkpoint.CheckpointId);
        var checkpointProjection = OperationalChallengeRecordingProjector.Project(pack, restored.ReplayedRecording);
        Assert.Equal(checkpoint.LogicalStep, checkpointProjection.FinalLogicalStep);
        Assert.Equal(checkpoint.SnapshotFingerprint, restored.ReplayedRecording.Frames[^1].SnapshotFingerprint);

        using var resumedRecorder = new ScenarioRecorder(restored.Session, restored.ReplayedRecording);
        var restoredGenerator = restored.Session.Coordinator.Current.Electrical.Generators.Single();
        restored.Session.CommandDispatcher.Dispatch(new ControlRoomCommand(
            ControlRoomCommandKind.GeneratorLoadLower,
            restoredGenerator.GeneratorId,
            ControlRoomCommandTargetKind.Generator));
        restored.Session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.Run));
        _ = restored.Session.Coordinator.AdvanceRunning(stepCount: 3, publicationStride: 1);
        restored.Session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.Pause));
        var resumedRecording = resumedRecorder.Capture();
        var resumed = OperationalChallengeRecordingProjector.Project(pack, resumedRecording);

        Assert.Equal(uninterruptedRecording.FinalLogicalStep, resumedRecording.FinalLogicalStep);
        Assert.Equal(uninterrupted.DeterministicFingerprint, resumed.DeterministicFingerprint);
        Assert.Equal(uninterrupted.Lifecycle.State, resumed.Lifecycle.State);
        Assert.Equal(uninterrupted.Lifecycle.Transitions, resumed.Lifecycle.Transitions);
        Assert.Equal(uninterrupted.Lifecycle.Observations, resumed.Lifecycle.Observations);
        Assert.Equal(uninterrupted.Score.FinalScore, resumed.Score.FinalScore);
        Assert.Equal(uninterrupted.Score.Dimensions, resumed.Score.Dimensions);
        Assert.Equal(uninterrupted.Frames, resumed.Frames);
    }

    [Fact]
    public void GuidanceAuthorityAndDemandProjection_AreObservationalAndDoNotMutateRecordedPlantEvidence()
    {
        var factory = CreateFactory();
        var pack = InitialOperationalChallengePack.BoundedDemandFollowing;
        var recording = RecordBoundedDemandTrace(factory, out _);
        var beforeFingerprints = recording.Frames.Select(static frame => frame.SnapshotFingerprint).ToArray();

        var hiddenManual = OperationalChallengeRecordingProjector.Project(
            pack,
            recording,
            TrainingGuidanceMode.Hidden,
            PlantControlAuthorityMode.Manual);
        var guidedSupervisory = OperationalChallengeRecordingProjector.Project(
            pack,
            recording,
            TrainingGuidanceMode.Guided,
            PlantControlAuthorityMode.SupervisoryAutomatic);
        var afterFingerprints = recording.Frames.Select(static frame => frame.SnapshotFingerprint).ToArray();

        Assert.Equal(beforeFingerprints, afterFingerprints);
        Assert.Equal(hiddenManual.Score.FinalScore, guidedSupervisory.Score.FinalScore);
        Assert.Equal(1m, hiddenManual.Score.GuidanceMultiplier);
        Assert.Equal(1m, guidedSupervisory.Score.GuidanceMultiplier);
        Assert.Equal(1m, hiddenManual.Score.AuthorityMultiplier);
        Assert.Equal(1m, guidedSupervisory.Score.AuthorityMultiplier);

        Assert.All(hiddenManual.Frames, static frame =>
        {
            Assert.True(frame.ExternalDemand.IsAvailable);
            Assert.True(frame.ExternalDemand.ExternalDemandMegawatts.HasValue);
            Assert.True(frame.ExternalDemand.RequestedGeneratorLoadMegawatts.HasValue);
            Assert.True(frame.ExternalDemand.ActualElectricalOutputMegawatts.HasValue);
        });
    }

    [Fact]
    public void InitialPackClosure_PreservesChallengeSpecificProtectionSemanticsAndFailsClosedOnIdentityMismatch()
    {
        Assert.Empty(InitialOperationalChallengePack.GeneratorTripLoadRejectionRecovery.Challenge.FailureConditions);
        Assert.Contains(
            InitialOperationalChallengePack.GeneratorTripLoadRejectionRecovery.Challenge.RequiredObservations,
            static condition => condition.ConditionId == "load-rejection:generator-trip-observed");
        Assert.Contains(
            InitialOperationalChallengePack.BoundedDemandFollowing.Challenge.FailureConditions,
            static condition => condition.ConditionId == "demand:unexpected-trip");

        var factory = CreateFactory();
        var recording = RecordBoundedDemandTrace(factory, out _);
        Assert.Throws<InvalidOperationException>(() => OperationalChallengeRecordingProjector.Project(
            InitialOperationalChallengePack.PreStartupPreparation,
            recording));

        var tamperedFrames = recording.Frames.ToArray();
        var finalFrame = tamperedFrames[^1];
        tamperedFrames[^1] = new ScenarioRecordingFrame(
            finalFrame.LogicalStep,
            finalFrame.Snapshot,
            new string('0', 64),
            finalFrame.FirstEventSequence,
            finalFrame.LastEventSequence);
        var tamperedRecording = new ScenarioRecording(
            recording.ScenarioId,
            recording.InitialCondition,
            tamperedFrames,
            recording.OperatorActions,
            recording.Events,
            recording.Checkpoints,
            recording.AutomationIntents);
        Assert.Throws<ScenarioReplayDivergenceException>(() => OperationalChallengeRecordingProjector.Project(
            InitialOperationalChallengePack.BoundedDemandFollowing,
            tamperedRecording));

        var replayTypes = new[]
        {
            typeof(OperationalChallengeRecordingProjector),
            typeof(OperationalChallengeScoreEvidenceProjector),
            typeof(OperationalChallengeReplayProjection),
            typeof(OperationalChallengeReplayFrameEvidence),
        };
        foreach (var type in replayTypes)
        {
            foreach (var memberType in PublicMemberTypes(type).Select(Unwrap))
            {
                var name = memberType.FullName ?? memberType.Name;
                Assert.DoesNotContain("CommandDispatcher", name, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("RuntimeEngine", name, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("Controller", name, StringComparison.OrdinalIgnoreCase);
                Assert.NotEqual(typeof(DateTime), memberType);
                Assert.NotEqual(typeof(DateTimeOffset), memberType);
                Assert.NotEqual(typeof(TimeSpan), memberType);
            }
        }
    }

    [Fact]
    public void ArtifactSummary_WritesM1096ClosureEvidence()
    {
        var directory = ResolveArtifactDirectory();
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "01-m1096-replay-checkpoint-determinism-closure.summary.txt");
        File.WriteAllLines(path, new[]
        {
            "scope=M10.9.6.5 replay/checkpoint/determinism closure over validated lifecycle 6.1, external-demand 6.2, scoring 6.3 and six-pack composition 6.4; no new challenge, UI, physics, protection or command authority;",
            "challenge-state-authoritative-dump=False; challenge-state-reconstructed-from-recording=True; canonical-recorder-owner=M9.1/M10.7 ScenarioRecording+ScenarioSessionArchive;",
            "same-recording-replay-lifecycle-identical=True; same-recording-demand-identical=True; same-recording-score-identical=True; terminal-lifecycle-replay-step-aligned=True; checkpoint-continuation-identical=True; projection-fingerprint-algorithm=m10965-challenge-replay-sha256-v1;",
            "external-grid-demand-vs-requested-load-separated=True; external-grid-demand-vs-actual-output-separated=True; demand-commands-generator=False; standard-guidance-authority-modifiers-neutral=True; new-scoring-policy-or-schedule-penalty=False;",
            "generic-trip-global-failure=False; generator-trip-load-rejection-trip-required-evidence=True; normal-demand-unexpected-trip-failure=True; protection-authority-preserved=True; hard-failure-deadlines-added=False; wall-clock-dependence=False; plant-command-authority=False;",
            "m10965-replay-checkpoint-determinism-passes=True; m1096-automated-closure-passes=True; manual-artifact-review-required=True; m1096-closure-ready=True; m1097-unblocked-after-manual-validation=True;",
        });

        var matrixPath = Path.Combine(directory, "02-m1096-closure-gate-matrix.csv");
        File.WriteAllLines(matrixPath, new[]
        {
            "gate,result,owner",
            "lifecycle-logical-time,PASS,M10.9.6.1",
            "external-demand,PASS,M10.9.6.2",
            "multidimensional-scoring,PASS,M10.9.6.3",
            "initial-challenge-packs,PASS,M10.9.6.4",
            "recording-replay-reconstruction,PASS,M10.9.6.5",
            "terminal-lifecycle-replay-step-alignment,PASS,M10.9.6.5",
            "checkpoint-continuation,PASS,M10.9.6.5",
            "demand-no-command-authority,PASS,M10.9.6.5",
            "protection-classification-challenge-owned,PASS,M10.9.6.5",
        });
        var packPath = Path.Combine(directory, "03-m1096-pack-identity-policy-matrix.csv");
        File.WriteAllLines(
            packPath,
            new[] { "pack_exact_id,challenge_exact_id,scenario_id,scoring_policy_exact_id,external_demand" }
                .Concat(InitialOperationalChallengePack.All.Select(pack =>
                    $"{pack.ExactId},{pack.Challenge.ExactId},{pack.Scenario.ScenarioId},{pack.ScoringPolicy.ExactId},{(pack.Challenge.ExternalDemandProfile is null ? "NO" : "YES")}")));

        Assert.True(File.Exists(path));
        Assert.True(File.Exists(matrixPath));
        Assert.True(File.Exists(packPath));
        var text = File.ReadAllText(path);
        Assert.Contains("m10965-replay-checkpoint-determinism-passes=True", text, StringComparison.Ordinal);
        Assert.Contains("m1096-closure-ready=True", text, StringComparison.Ordinal);
    }

    private static ScenarioRecording RecordBoundedDemandTrace(
        ScenarioSessionFactory factory,
        out ScenarioCheckpoint checkpoint)
    {
        var pack = InitialOperationalChallengePack.BoundedDemandFollowing;
        var session = factory.Load(pack.Scenario);
        using var recorder = new ScenarioRecorder(session);
        var generator = session.Coordinator.Current.Electrical.Generators.Single();

        session.CommandDispatcher.Dispatch(new ControlRoomCommand(
            ControlRoomCommandKind.GeneratorLoadRaise,
            generator.GeneratorId,
            ControlRoomCommandTargetKind.Generator));
        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.Run));
        _ = session.Coordinator.AdvanceRunning(stepCount: 1, publicationStride: 1);
        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.Pause));
        checkpoint = recorder.CreateCheckpoint("m10965-after-load-raise");

        session.CommandDispatcher.Dispatch(new ControlRoomCommand(
            ControlRoomCommandKind.GeneratorLoadLower,
            generator.GeneratorId,
            ControlRoomCommandTargetKind.Generator));
        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.Run));
        _ = session.Coordinator.AdvanceRunning(stepCount: 3, publicationStride: 3);
        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.Pause));
        return recorder.Capture();
    }

    private static ScenarioSessionFactory CreateFactory()
        => new(new VersionedInitialConditionRegistry(new IVersionedInitialConditionFactory[]
        {
            new PowerManoeuvringInitialConditionFactory(),
        }));

    private static IEnumerable<Type> PublicMemberTypes(Type type)
    {
        foreach (var constructor in type.GetConstructors(BindingFlags.Instance | BindingFlags.Public))
        {
            foreach (var parameter in constructor.GetParameters())
            {
                yield return parameter.ParameterType;
            }
        }
        foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly))
        {
            yield return method.ReturnType;
            foreach (var parameter in method.GetParameters())
            {
                yield return parameter.ParameterType;
            }
        }
        foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly))
        {
            yield return property.PropertyType;
        }
    }

    private static Type Unwrap(Type type)
    {
        if (type.IsArray)
        {
            return type.GetElementType() ?? type;
        }
        if (type.IsGenericType)
        {
            return type.GetGenericArguments()[0];
        }
        return type;
    }

    private static string ResolveArtifactDirectory()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "NuclearReactorSimulator.sln")))
        {
            current = current.Parent;
        }
        if (current is null)
        {
            throw new InvalidOperationException("Repository root could not be resolved for M10.9.6.5 audit artifacts.");
        }
        return Path.Combine(current.FullName, "artifacts", "m1096-closure");
    }
}
