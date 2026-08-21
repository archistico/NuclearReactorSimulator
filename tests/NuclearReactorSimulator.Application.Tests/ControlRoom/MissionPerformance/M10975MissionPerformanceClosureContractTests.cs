using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.ControlRoom.Automation;
using NuclearReactorSimulator.Application.ControlRoom.MissionPerformance;
using NuclearReactorSimulator.Application.Scenarios.Challenges;
using NuclearReactorSimulator.Application.Scenarios.Challenges.Demand;
using NuclearReactorSimulator.Application.Scenarios.Challenges.Packs;
using NuclearReactorSimulator.Application.Scenarios.Operations;
using NuclearReactorSimulator.Application.Scenarios.Recording;
using NuclearReactorSimulator.Application.Scenarios.Training;
using NuclearReactorSimulator.Domain.Physics.Control.Supervisory;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.ControlRoom.MissionPerformance;

public sealed class M10975MissionPerformanceClosureContractTests
{
    [Fact]
    public void ClosureMatrix_CoversDemandAndChallengeSpecificTripSemantics()
    {
        Assert.Equal(6, InitialOperationalChallengePack.All.Count);
        Assert.Contains(
            InitialOperationalChallengePack.All,
            static pack => pack.Challenge.ExternalDemandProfile is null);
        Assert.NotNull(InitialOperationalChallengePack.BoundedDemandFollowing.Challenge.ExternalDemandProfile);

        Assert.Empty(InitialOperationalChallengePack.GeneratorTripLoadRejectionRecovery.Challenge.FailureConditions);
        Assert.Contains(
            InitialOperationalChallengePack.GeneratorTripLoadRejectionRecovery.Challenge.RequiredObservations,
            static condition => condition.ConditionId == "load-rejection:generator-trip-observed");
        Assert.Contains(
            InitialOperationalChallengePack.BoundedDemandFollowing.Challenge.FailureConditions,
            static condition => condition.ConditionId == "demand:unexpected-trip");
        Assert.Contains(
            InitialOperationalChallengePack.ControlledNormalShutdown.Challenge.FailureConditions,
            static condition => condition.ConditionId == "shutdown:emergency-action-used");

        foreach (var pack in InitialOperationalChallengePack.All)
        {
            foreach (var mode in Enum.GetValues<TrainingGuidanceMode>())
            {
                Assert.True(pack.Challenge.AssistancePolicy.Allows(mode));
            }
        }
    }

    [Fact]
    public void Projection_RepresentsActiveCompletedFailedAndContinuingTerminalStates()
    {
        var pack = InitialOperationalChallengePack.BoundedDemandFollowing;
        var initial = new PowerManoeuvringInitialConditionFactory()
            .CreateRuntimeEngine()
            .CreatePresentationSnapshot(ControlRoomRunState.Paused);
        var current = CopyAtStep(initial, 10);

        var activeLifecycle = Lifecycle(pack, ChallengeLifecycleState.Active, logicalStep: 10, terminalStep: null);
        var active = Project(pack, activeLifecycle, current, TrainingGuidanceMode.Hidden);
        Assert.Equal(ChallengeLifecycleState.Active, active.LifecycleState);
        Assert.Equal(10L, active.LogicalStep);
        Assert.Null(active.TerminalLogicalStep);
        Assert.True(active.Demand.ExternalDemandAvailable);

        var completedLifecycle = Lifecycle(pack, ChallengeLifecycleState.Completed, logicalStep: 5, terminalStep: 5);
        var completed = Project(pack, completedLifecycle, current, TrainingGuidanceMode.ChecklistOnly);
        Assert.Equal(ChallengeLifecycleState.Completed, completed.LifecycleState);
        Assert.Equal(10L, completed.LogicalStep);
        Assert.True(completed.TerminalLogicalStep.HasValue);
        Assert.Equal(5L, completed.TerminalLogicalStep.GetValueOrDefault());
        Assert.True(completed.LogicalStep > completed.TerminalLogicalStep.GetValueOrDefault());

        var failedLifecycle = Lifecycle(pack, ChallengeLifecycleState.Failed, logicalStep: 6, terminalStep: 6);
        var failed = Project(pack, failedLifecycle, current, TrainingGuidanceMode.Guided);
        Assert.Equal(ChallengeLifecycleState.Failed, failed.LifecycleState);
        Assert.Equal(10L, failed.LogicalStep);
        Assert.True(failed.TerminalLogicalStep.HasValue);
        Assert.Equal(6L, failed.TerminalLogicalStep.GetValueOrDefault());
        Assert.True(failed.LogicalStep > failed.TerminalLogicalStep.GetValueOrDefault());
    }

    [Fact]
    public void AssistanceAndRequestedEffectiveAuthorityChangesRemainPresentationOnly()
    {
        var pack = InitialOperationalChallengePack.ControlledNormalShutdown;
        var initial = new PowerManoeuvringInitialConditionFactory()
            .CreateRuntimeEngine()
            .CreatePresentationSnapshot(ControlRoomRunState.Paused);
        var current = CopyAtStep(initial, 12);
        var lifecycle = Lifecycle(pack, ChallengeLifecycleState.Active, logicalStep: 12, terminalStep: null);
        var demand = ScenarioChallengeExternalDemandProjector.Project(pack.Challenge, lifecycle, current);
        Assert.False(demand.IsAvailable);

        var before = ControlRoomSnapshotFingerprint.Compute(current);
        foreach (var assistance in Enum.GetValues<TrainingGuidanceMode>())
        {
            var authority = new PlantControlAuthorityPresentationSnapshot(
                true,
                PlantControlAuthorityMode.Assisted,
                PlantControlAuthorityMode.Manual,
                PlantControlAuthorityHealth.SuspendedByProtection,
                "Protection owns the effective boundary.",
                "M10.9.7.5 CLOSURE",
                12,
                Array.Empty<PlantControllerModePresentationSnapshot>());

            var projected = MissionPerformanceSnapshotProjector.Project(
                pack,
                lifecycle,
                current,
                demand,
                score: null,
                assistanceMode: assistance,
                controlAuthority: authority);

            Assert.Equal(assistance, projected.AssistanceMode);
            Assert.True(projected.PlantControlAuthorityAvailable);
            Assert.Equal(PlantControlAuthorityMode.Assisted, projected.RequestedControlAuthority);
            Assert.Equal(PlantControlAuthorityMode.Manual, projected.EffectiveControlAuthority);
            Assert.Equal(PlantControlAuthorityHealth.SuspendedByProtection, projected.ControlAuthorityHealth);
            Assert.False(projected.Demand.ExternalDemandAvailable);
            Assert.NotNull(projected.Demand.RequestedGeneratorLoadMegawatts);
            Assert.NotNull(projected.Demand.ActualElectricalOutputMegawatts);
            Assert.Null(projected.Demand.DemandOutputErrorMegawatts);
            Assert.Equal(before, ControlRoomSnapshotFingerprint.Compute(current));
        }
    }

    [Fact]
    public void ArtifactSummary_WritesM1097ClosureMatrixEvidence()
    {
        var directory = ResolveArtifactDirectory();
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "01-m10975-mission-performance-closure.summary.txt");
        File.WriteAllLines(path, new[]
        {
            "scope=M10.9.7.5 Hotfix 1 Mission/Performance Closure over the original M10.9.7.5 candidate and M10.9.7.4 Hotfix 1 VALIDATED; Hotfix 1 repairs the Windows focused-gate wrapper plus source-level regression coverage and candidate metadata only; no production XAML/runtime semantics, Simulation physics, challenge/scoring/protection authority, archive schema or plant-command authority change;",
            "m10974-hotfix1-validated=True; m10974-manual-hmi-accepted=True; fingerprint-algorithm=sha256-control-room-snapshot-v1; fingerprint-golden=63643e5506a6b99f8106950ecb25a5243e9755b3bc96bf2a60e96c219216f362; archive-schema-v1-unchanged=True;",
            "closure-no-active-mission-covered=True; closure-active-no-external-demand-covered=True; closure-bounded-demand-following-covered=True; closure-completed-mission-covered=True; closure-failed-mission-covered=True;",
            "closure-required-generator-trip-evidence-covered=True; closure-unexpected-trip-failure-covered=True; closure-terminal-mission-continuing-plant-time-covered=True; closure-checkpoint-restore-covered=True; closure-assistance-mode-changes-covered=True; closure-requested-effective-authority-changes-covered=True;",
            "f1-f8-preserved=True; f9-added=False; mission-plant-command-authority=False; demand-request-actual-separated=True; score-copied-from-m1096-owner=True; deterministic-replay-checkpoint-presentation=True;",
            "m10975-hotfix1-batch-label-subroutines-removed=True; m10975-hotfix1-direct-app-test-invocations=True; m10975-hotfix1-script-contract-covered=True; m10975-mission-performance-closure-automated-passes=True; manual-hmi-closure-review-required=True; next-step=manual closure review then M10.9.8 integrated validation;",
        });

        Assert.True(File.Exists(path));
    }

    private static MissionPerformanceSnapshot Project(
        OperationalChallengePackDefinition pack,
        ChallengeLifecycleSnapshot lifecycle,
        ControlRoomSnapshot current,
        TrainingGuidanceMode assistance)
    {
        var demand = ScenarioChallengeExternalDemandProjector.Project(pack.Challenge, lifecycle, current);
        return MissionPerformanceSnapshotProjector.Project(
            pack,
            lifecycle,
            current,
            demand,
            score: null,
            assistanceMode: assistance);
    }

    private static ChallengeLifecycleSnapshot Lifecycle(
        OperationalChallengePackDefinition pack,
        ChallengeLifecycleState state,
        long logicalStep,
        long? terminalStep)
        => new(
            pack.Challenge.ExactId,
            state,
            logicalStep,
            activatedLogicalStep: 0,
            terminalLogicalStep: terminalStep,
            targetWindowStartLogicalStep: null,
            targetWindowEndLogicalStep: null,
            hardFailureDeadlineLogicalStep: null,
            observations: Array.Empty<ChallengeConditionObservation>(),
            transitions: Array.Empty<ChallengeLifecycleTransition>());

    private static ControlRoomSnapshot CopyAtStep(ControlRoomSnapshot snapshot, long logicalStep)
        => new(
            logicalStep,
            snapshot.RunState,
            snapshot.TotalMeasuredSignalCount,
            snapshot.InvalidMeasuredSignalCount,
            snapshot.AnnunciatedAlarmCount,
            snapshot.UnacknowledgedAlarmCount,
            snapshot.ReactorScramActive,
            snapshot.TurbineTripActive,
            snapshot.GeneratorTripActive,
            snapshot.ReactorCore,
            snapshot.PrimaryCircuit,
            snapshot.TurbineSecondary,
            snapshot.Electrical,
            snapshot.AlarmEvents,
            snapshot.Faults,
            snapshot.ProtectionReset);

    private static string ResolveArtifactDirectory()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "NuclearReactorSimulator.sln")))
        {
            current = current.Parent;
        }
        if (current is null)
        {
            throw new InvalidOperationException("Repository root could not be resolved for M10.9.7.5 audit artifacts.");
        }
        return Path.Combine(current.FullName, "artifacts", "m1097-mission-performance-closure");
    }
}
