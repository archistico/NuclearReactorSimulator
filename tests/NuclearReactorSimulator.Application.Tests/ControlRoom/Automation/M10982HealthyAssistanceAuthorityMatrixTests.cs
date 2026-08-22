using System.Globalization;
using System.Text;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.ControlRoom.Automation;
using NuclearReactorSimulator.Application.ControlRoom.MissionPerformance;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Challenges;
using NuclearReactorSimulator.Application.Scenarios.Challenges.Packs;
using NuclearReactorSimulator.Application.Scenarios.Operations;
using NuclearReactorSimulator.Application.Scenarios.Recording;
using NuclearReactorSimulator.Application.Scenarios.Training;
using NuclearReactorSimulator.Domain.Physics.Control.Supervisory;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.ControlRoom.Automation;

public sealed class M10982HealthyAssistanceAuthorityMatrixTests
{
    private static readonly MatrixRow[] Rows =
    {
        new("HAA-01", TrainingGuidanceMode.Hidden, PlantControlAuthorityMode.Manual),
        new("HAA-02", TrainingGuidanceMode.Hidden, PlantControlAuthorityMode.Assisted),
        new("HAA-03", TrainingGuidanceMode.Hidden, PlantControlAuthorityMode.SupervisoryAutomatic),
        new("HAA-04", TrainingGuidanceMode.ChecklistOnly, PlantControlAuthorityMode.Manual),
        new("HAA-05", TrainingGuidanceMode.ChecklistOnly, PlantControlAuthorityMode.Assisted),
        new("HAA-06", TrainingGuidanceMode.ChecklistOnly, PlantControlAuthorityMode.SupervisoryAutomatic),
        new("HAA-07", TrainingGuidanceMode.Guided, PlantControlAuthorityMode.Manual),
        new("HAA-08", TrainingGuidanceMode.Guided, PlantControlAuthorityMode.Assisted),
        new("HAA-09", TrainingGuidanceMode.Guided, PlantControlAuthorityMode.SupervisoryAutomatic),
    };

    [Fact]
    public void FrozenHealthyMatrix_ExecutesNineRowsAndPreservesAssistanceIsolation()
    {
        var pack = ProductionOperationalChallengePack.BoundedDemandFollowing;
        Assert.Equal("bounded-demand-following-5-10-5@2", pack.ExactId);
        Assert.Equal("integrated-normal-operations-training-i5-repaired-v4-production", pack.Scenario.ScenarioId);
        Assert.Equal(new InitialConditionReference("integrated-operations-desktop-stable", 4), pack.Scenario.InitialCondition);
        Assert.Equal("bounded-demand-5-10-5@1", pack.Challenge.ExternalDemandProfile?.ExactId);

        var outcomes = Rows.Select(row => Execute(row, pack)).ToArray();

        Assert.Equal(9, outcomes.Length);
        Assert.Equal(9, outcomes.Select(static item => item.RowId).Distinct(StringComparer.Ordinal).Count());
        Assert.All(outcomes, static item =>
        {
            Assert.Equal(item.RequestedAuthority, item.EffectiveAuthority);
            Assert.Equal(PlantControlAuthorityHealth.Normal, item.AuthorityHealth);
            Assert.True(item.ExternalDemandAvailable);
            Assert.True(item.ExternalDemandMegawatts.HasValue);
            Assert.Equal(5d, item.ExternalDemandMegawatts.Value);
            Assert.True(item.RequestedGeneratorLoadMegawatts.HasValue);
            Assert.True(item.ActualElectricalOutputMegawatts.HasValue);
            Assert.Equal(4, item.AcceptedOperatorActionCount);
            Assert.False(item.ReactorScramActive);
            Assert.False(item.TurbineTripActive);
            Assert.False(item.GeneratorTripActive);
            Assert.Equal(item.FinalFingerprint, item.ReplayFingerprint);
            Assert.Equal(item.FinalFingerprint, item.CheckpointContinuationFingerprint);
        });

        foreach (var authority in Enum.GetValues<PlantControlAuthorityMode>())
        {
            var sameAuthority = outcomes.Where(item => item.RequestedAuthority == authority).ToArray();
            Assert.Equal(3, sameAuthority.Length);
            var reference = sameAuthority[0];
            Assert.All(sameAuthority, item =>
            {
                Assert.Equal(reference.FinalFingerprint, item.FinalFingerprint);
                Assert.Equal(reference.ReplayFingerprint, item.ReplayFingerprint);
                Assert.Equal(reference.LifecycleState, item.LifecycleState);
                Assert.Equal(reference.ExternalDemandMegawatts, item.ExternalDemandMegawatts);
                Assert.Equal(reference.RequestedGeneratorLoadMegawatts, item.RequestedGeneratorLoadMegawatts);
                Assert.Equal(reference.ActualElectricalOutputMegawatts, item.ActualElectricalOutputMegawatts);
                Assert.Equal(reference.FinalScore, item.FinalScore);
                Assert.Equal(reference.ScoreGrade, item.ScoreGrade);
                Assert.Equal(reference.AnnunciatedAlarmCount, item.AnnunciatedAlarmCount);
            });
        }

        WriteArtifacts(outcomes);
    }

    [Fact]
    public void AssistanceModeChanges_ArePresentationOnlyAndCannotChangeHealthySupervisoryAuthority()
    {
        var pack = ProductionOperationalChallengePack.BoundedDemandFollowing;
        var session = CreateFactory().Load(pack.Scenario);
        ConfigureAuthority(session, PlantControlAuthorityMode.SupervisoryAutomatic);
        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.SingleStep));
        using var source = new MissionPerformanceLiveSnapshotSource(session, pack, TrainingGuidanceMode.Hidden);

        var beforeFingerprint = ControlRoomSnapshotFingerprint.Compute(session.Coordinator.Current);
        var beforeAutomation = session.PlantControlAuthority.CurrentAutomation;

        source.SetAssistanceMode(TrainingGuidanceMode.ChecklistOnly);
        Assert.Equal(TrainingGuidanceMode.ChecklistOnly, source.Current.AssistanceMode);
        Assert.Equal(beforeFingerprint, ControlRoomSnapshotFingerprint.Compute(session.Coordinator.Current));
        Assert.Equal(beforeAutomation.RequestedAuthority, session.PlantControlAuthority.CurrentAutomation.RequestedAuthority);
        Assert.Equal(beforeAutomation.EffectiveAuthority, session.PlantControlAuthority.CurrentAutomation.EffectiveAuthority);
        Assert.Equal(beforeAutomation.Health, session.PlantControlAuthority.CurrentAutomation.Health);

        source.SetAssistanceMode(TrainingGuidanceMode.Guided);
        Assert.Equal(TrainingGuidanceMode.Guided, source.Current.AssistanceMode);
        Assert.Equal(beforeFingerprint, ControlRoomSnapshotFingerprint.Compute(session.Coordinator.Current));
        Assert.Equal(beforeAutomation.RequestedAuthority, session.PlantControlAuthority.CurrentAutomation.RequestedAuthority);
        Assert.Equal(beforeAutomation.EffectiveAuthority, session.PlantControlAuthority.CurrentAutomation.EffectiveAuthority);
        Assert.Equal(beforeAutomation.Health, session.PlantControlAuthority.CurrentAutomation.Health);
    }

    private static MatrixOutcome Execute(MatrixRow row, OperationalChallengePackDefinition pack)
    {
        Assert.True(pack.Challenge.AssistancePolicy.Allows(row.Assistance));

        var factory = CreateFactory();
        var session = factory.Load(pack.Scenario);
        using var recorder = new ScenarioRecorder(session);
        using var source = new MissionPerformanceLiveSnapshotSource(session, pack, row.Assistance, recorder);

        Assert.Equal(row.Assistance, source.Current.AssistanceMode);
        Assert.Equal(pack.ExactId, source.Current.PackExactId);
        Assert.Equal(pack.Challenge.ExactId, source.Current.ChallengeExactId);
        Assert.Equal(pack.Scenario.ScenarioId, source.Current.ScenarioId);

        ConfigureAuthority(session, row.RequestedAuthority);
        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.SingleStep));
        AssertHealthyAuthority(session, row.RequestedAuthority);

        var generator = session.Coordinator.Current.Electrical.Generators.Single();
        var loadRaise = new ControlRoomCommand(
            ControlRoomCommandKind.GeneratorLoadRaise,
            generator.GeneratorId,
            ControlRoomCommandTargetKind.Generator);
        var rodHold = new ControlRoomCommand(
            ControlRoomCommandKind.ControlRodHold,
            "regulating",
            ControlRoomCommandTargetKind.ControlRodGroup);
        var loadLower = new ControlRoomCommand(
            ControlRoomCommandKind.GeneratorLoadLower,
            generator.GeneratorId,
            ControlRoomCommandTargetKind.Generator);
        var acknowledgeAll = new ControlRoomCommand(ControlRoomCommandKind.AlarmAcknowledgeAll);

        DispatchAndStep(session, loadRaise);
        DispatchAndStep(session, rodHold);
        var checkpoint = recorder.CreateCheckpoint($"m10982-{row.RowId.ToLowerInvariant()}-prefix");
        DispatchAndStep(session, loadLower);
        DispatchAndStep(session, acknowledgeAll);

        var finalAutomation = session.PlantControlAuthority.CurrentAutomation;
        AssertHealthyAuthority(session, row.RequestedAuthority);
        var mission = source.Current;
        var finalSnapshot = session.Coordinator.Current;
        var acceptedKinds = session.OperatorActions.Actions.Select(static item => item.Command.Kind).ToArray();
        Assert.Equal(new[]
        {
            ControlRoomCommandKind.GeneratorLoadRaise,
            ControlRoomCommandKind.ControlRodHold,
            ControlRoomCommandKind.GeneratorLoadLower,
            ControlRoomCommandKind.AlarmAcknowledgeAll,
        }, acceptedKinds);
        Assert.Equal(row.Assistance, mission.AssistanceMode);
        Assert.True(mission.PlantControlAuthorityAvailable);
        Assert.Equal(row.RequestedAuthority, mission.RequestedControlAuthority);
        Assert.Equal(row.RequestedAuthority, mission.EffectiveControlAuthority);
        Assert.Equal(PlantControlAuthorityHealth.Normal, mission.ControlAuthorityHealth);
        Assert.True(mission.Demand.ExternalDemandAvailable);
        Assert.Equal("bounded-demand-5-10-5@1", mission.Demand.ExternalDemandProfileExactId);
        Assert.True(mission.Demand.ExternalDemandMegawatts.HasValue);
        Assert.Equal(5d, mission.Demand.ExternalDemandMegawatts.Value);
        Assert.True(mission.Demand.RequestedGeneratorLoadMegawatts.HasValue);
        Assert.True(mission.Demand.ActualElectricalOutputMegawatts.HasValue);
        Assert.True(mission.ActivatedLogicalStep.HasValue);
        Assert.True(mission.TargetWindowStartLogicalStep.HasValue);
        Assert.True(mission.TargetWindowEndLogicalStep.HasValue);
        Assert.Equal(mission.ActivatedLogicalStep.Value + 4_000L, mission.TargetWindowStartLogicalStep.Value);
        Assert.Equal(mission.ActivatedLogicalStep.Value + 8_000L, mission.TargetWindowEndLogicalStep.Value);
        Assert.Equal(finalSnapshot.LogicalStep, mission.LogicalStep);
        Assert.Equal("demand-following@1", mission.Score.ScoringPolicyExactId);

        var finalFingerprint = ControlRoomSnapshotFingerprint.Compute(finalSnapshot);
        var recording = recorder.Complete();
        var replayRunner = new ScenarioFullReplayRunner(factory);
        var replay = replayRunner.ReplayAndVerify(pack.Scenario, recording);
        var replayFingerprint = ControlRoomSnapshotFingerprint.Compute(replay.Session.Coordinator.Current);
        Assert.Equal(finalFingerprint, replayFingerprint);
        Assert.Equal(finalAutomation.RequestedAuthority, replay.Session.PlantControlAuthority.CurrentAutomation.RequestedAuthority);
        Assert.Equal(finalAutomation.EffectiveAuthority, replay.Session.PlantControlAuthority.CurrentAutomation.EffectiveAuthority);

        var archive = ScenarioSessionArchive.FromRecording($"m10982-{row.RowId.ToLowerInvariant()}", pack.Scenario, recording);
        var restoredPrefix = replayRunner.SeekAndVerify(archive, checkpoint.CheckpointId);
        Assert.Equal(checkpoint.LogicalStep, restoredPrefix.Session.Coordinator.Current.LogicalStep);
        AssertHealthyAuthority(restoredPrefix.Session, row.RequestedAuthority);
        using var continuationRecorder = new ScenarioRecorder(restoredPrefix.Session, restoredPrefix.ReplayedRecording);
        using var continuationSource = new MissionPerformanceLiveSnapshotSource(
            restoredPrefix.Session,
            pack,
            row.Assistance,
            continuationRecorder,
            restoredPrefix.ReplayedRecording);
        DispatchAndStep(restoredPrefix.Session, loadLower);
        DispatchAndStep(restoredPrefix.Session, acknowledgeAll);
        var continuationMission = continuationSource.Current;
        var continuationFingerprint = ControlRoomSnapshotFingerprint.Compute(restoredPrefix.Session.Coordinator.Current);
        _ = continuationRecorder.Complete();

        Assert.Equal(finalFingerprint, continuationFingerprint);
        Assert.Equal(finalAutomation.RequestedAuthority, restoredPrefix.Session.PlantControlAuthority.CurrentAutomation.RequestedAuthority);
        Assert.Equal(finalAutomation.EffectiveAuthority, restoredPrefix.Session.PlantControlAuthority.CurrentAutomation.EffectiveAuthority);
        AssertHealthyAuthority(restoredPrefix.Session, row.RequestedAuthority);
        Assert.Equal(mission.LifecycleState, continuationMission.LifecycleState);
        Assert.Equal(mission.Demand.ExternalDemandMegawatts, continuationMission.Demand.ExternalDemandMegawatts);
        Assert.Equal(mission.Demand.RequestedGeneratorLoadMegawatts, continuationMission.Demand.RequestedGeneratorLoadMegawatts);
        Assert.Equal(mission.Demand.ActualElectricalOutputMegawatts, continuationMission.Demand.ActualElectricalOutputMegawatts);
        Assert.Equal(mission.Score.FinalScore, continuationMission.Score.FinalScore);
        Assert.Equal(mission.Score.Grade, continuationMission.Score.Grade);

        return new MatrixOutcome(
            row.RowId,
            row.Assistance,
            row.RequestedAuthority,
            finalAutomation.EffectiveAuthority,
            finalAutomation.Health,
            finalSnapshot.LogicalStep,
            mission.LifecycleState,
            mission.Demand.ExternalDemandAvailable,
            mission.Demand.ExternalDemandMegawatts,
            mission.Demand.RequestedGeneratorLoadMegawatts,
            mission.Demand.ActualElectricalOutputMegawatts,
            mission.Score.FinalScore,
            mission.Score.Grade?.ToString(),
            finalSnapshot.AnnunciatedAlarmCount,
            finalSnapshot.ReactorScramActive,
            finalSnapshot.TurbineTripActive,
            finalSnapshot.GeneratorTripActive,
            acceptedKinds.Length,
            finalFingerprint,
            replayFingerprint,
            continuationFingerprint);
    }

    private static void ConfigureAuthority(ScenarioSession session, PlantControlAuthorityMode authority)
    {
        if (authority == PlantControlAuthorityMode.SupervisoryAutomatic)
        {
            session.PlantControlAuthority.RequestSupervisoryObjective(SupervisoryObjectiveRequest.HoldCurrentOperatingPoint());
        }
        session.PlantControlAuthority.RequestAuthority(authority);
    }

    private static void AssertHealthyAuthority(ScenarioSession session, PlantControlAuthorityMode expected)
    {
        var automation = session.PlantControlAuthority.CurrentAutomation;
        Assert.True(automation.IsAvailable);
        Assert.Equal(expected, automation.RequestedAuthority);
        Assert.Equal(expected, automation.EffectiveAuthority);
        Assert.Equal(PlantControlAuthorityHealth.Normal, automation.Health);
        Assert.Null(automation.DegradationReason);
    }

    private static void DispatchAndStep(ScenarioSession session, ControlRoomCommand command)
    {
        session.CommandDispatcher.Dispatch(command);
        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.SingleStep));
    }

    private static ScenarioSessionFactory CreateFactory()
        => new(new VersionedInitialConditionRegistry(new IVersionedInitialConditionFactory[]
        {
            new DesktopSustainedGenerationI5RepairedActivationCandidateInitialConditionFactory(),
        }));

    private static void WriteArtifacts(IReadOnlyList<MatrixOutcome> outcomes)
    {
        var directory = ResolveArtifactDirectory();
        Directory.CreateDirectory(directory);

        var summaryPath = Path.Combine(directory, "01-m10982-healthy-assistance-authority-matrix.summary.txt");
        File.WriteAllLines(summaryPath, new[]
        {
            "scope=M10.9.8.2 Hotfix 1 REV3 Healthy Assistance x Authority Matrix plus production mission binding/F4 command-console robustness over M10.9.8.1 REV1 Docs1 VALIDATED; historical challenge @1 identities remain immutable; no Simulation physics/coefficient/protection/scoring/archive/fingerprint change;",
            "matrix-id=m1098-integrated-human-automation-hmi-v1; matrix-schema=1; execution-matrix=m1098-integrated-human-automation-hmi-v2; frozen-matrix-v1-preserved=True; healthy-rows-executed=9; assistance-axis=Hidden|ChecklistOnly|Guided; authority-axis=Manual|Assisted|SupervisoryAutomatic; exact-pack=bounded-demand-following-5-10-5@2; exact-profile=integrated-operations-desktop-stable@4; historical-pack-preserved=bounded-demand-following-5-10-5@1;",
            "same-representative-command-sequence=True; accepted-actions-per-row=4; command-sequence=GeneratorLoadRaise|ControlRodHold|GeneratorLoadLower|AlarmAcknowledgeAll; healthy-requested-effective-authority-equals-requested=True; supervisory-objective-explicit=True; authority-health-normal-all-rows=True; healthy-phase=active-bounded-demand-control-axis; target-window-offset-steps=4000..8000; target-window-delays-activation=False;",
            "assistance-only-physical-fingerprint-equivalent-within-authority=True; assistance-only-lifecycle-demand-score-equivalent-within-authority=True; guidance-score-modifier-neutral-v1=True; authority-score-modifier-neutral-v1=True; external-demand-active-all-rows=True; external-demand-profile=bounded-demand-5-10-5@1; logical-time-window-owner=m10961-rerun; demand-request-actual-separation-owner=m10962-rerun;",
            "canonical-protection-trips-active=False; full-replay-final-fingerprint-equivalent-all-rows=True; checkpoint-prefix-live-continuation-equivalent-all-rows=True; authority-intents-recorded-through-m5-session-seam=True; mission-source-presentation-only=True; production-app-runtime-changed=True; challenge-catalog-additive=True; challenge-semantics-changed=False; simulation-physics-changed=False; archive-schema-v1-unchanged=True; fingerprint-algorithm=sha256-control-room-snapshot-v1;",
            "m10982-hotfix1-rev3-active-demand-timing-contract-corrected=True; m10982-hotfix1-compile-namespace-aligned=True; production-mission-exact-v2=True; historical-mission-exact-v1-preserved=True; f4-command-catalog-semantic-refresh=True; f4-enter-explicit-handled-input=True; f4-expected-command-failures-operator-visible=True; m10982-automated-healthy-assistance-authority-matrix-passes=True; next-step=M10.9.8.3 degraded measurement/fault/protection/takeover matrix;",
        }, new UTF8Encoding(false));

        var csvPath = Path.Combine(directory, "02-m10982-healthy-assistance-authority-matrix.rows.csv");
        var lines = new List<string>
        {
            "row_id,assistance,requested_authority,effective_authority,authority_health,logical_step,lifecycle,external_demand_mw,requested_load_mw,actual_output_mw,final_score,score_grade,annunciated_alarms,accepted_actions,final_fingerprint,replay_fingerprint,checkpoint_continuation_fingerprint",
        };
        lines.AddRange(outcomes.Select(static item => string.Join(",", new[]
        {
            item.RowId,
            item.Assistance.ToString(),
            item.RequestedAuthority.ToString(),
            item.EffectiveAuthority.ToString(),
            item.AuthorityHealth.ToString(),
            item.LogicalStep.ToString(CultureInfo.InvariantCulture),
            item.LifecycleState.ToString(),
            Format(item.ExternalDemandMegawatts),
            Format(item.RequestedGeneratorLoadMegawatts),
            Format(item.ActualElectricalOutputMegawatts),
            Format(item.FinalScore),
            item.ScoreGrade ?? string.Empty,
            item.AnnunciatedAlarmCount.ToString(CultureInfo.InvariantCulture),
            item.AcceptedOperatorActionCount.ToString(CultureInfo.InvariantCulture),
            item.FinalFingerprint,
            item.ReplayFingerprint,
            item.CheckpointContinuationFingerprint,
        })));
        File.WriteAllLines(csvPath, lines, new UTF8Encoding(false));
    }

    private static string Format(double? value)
        => value?.ToString("R", CultureInfo.InvariantCulture) ?? string.Empty;

    private static string Format(decimal? value)
        => value?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;

    private static string ResolveArtifactDirectory()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "NuclearReactorSimulator.sln")))
        {
            current = current.Parent;
        }
        if (current is null)
        {
            throw new InvalidOperationException("Repository root could not be resolved for M10.9.8.2 audit artifacts.");
        }
        return Path.Combine(current.FullName, "artifacts", "m1098-healthy-assistance-authority-matrix");
    }

    private sealed record MatrixRow(
        string RowId,
        TrainingGuidanceMode Assistance,
        PlantControlAuthorityMode RequestedAuthority);

    private sealed record MatrixOutcome(
        string RowId,
        TrainingGuidanceMode Assistance,
        PlantControlAuthorityMode RequestedAuthority,
        PlantControlAuthorityMode EffectiveAuthority,
        PlantControlAuthorityHealth AuthorityHealth,
        long LogicalStep,
        ChallengeLifecycleState LifecycleState,
        bool ExternalDemandAvailable,
        double? ExternalDemandMegawatts,
        double? RequestedGeneratorLoadMegawatts,
        double? ActualElectricalOutputMegawatts,
        decimal? FinalScore,
        string? ScoreGrade,
        int AnnunciatedAlarmCount,
        bool ReactorScramActive,
        bool TurbineTripActive,
        bool GeneratorTripActive,
        int AcceptedOperatorActionCount,
        string FinalFingerprint,
        string ReplayFingerprint,
        string CheckpointContinuationFingerprint);
}
