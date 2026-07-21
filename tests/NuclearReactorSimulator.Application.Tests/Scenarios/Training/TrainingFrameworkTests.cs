using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Training;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Training;

public sealed class TrainingFrameworkTests
{
    [Fact]
    public void Tracker_ObservesEveryDeterministicStepIndependentOfPresentationPublicationStride()
    {
        var dense = CreateTrainingSession();
        var sparse = CreateTrainingSession();

        dense.Session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.Run));
        sparse.Session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.Run));
        dense.Session.Coordinator.AdvanceRunning(3, publicationStride: 1);
        sparse.Session.Coordinator.AdvanceRunning(3, publicationStride: 3);

        var denseCheckpoint = Assert.Single(dense.Tracker.Assessment.Checkpoints);
        var sparseCheckpoint = Assert.Single(sparse.Tracker.Assessment.Checkpoints);
        Assert.Equal(2L, denseCheckpoint.FirstSatisfiedLogicalStep);
        Assert.Equal(denseCheckpoint.FirstSatisfiedLogicalStep, sparseCheckpoint.FirstSatisfiedLogicalStep);
        Assert.Equal(100, dense.Tracker.Assessment.TotalScore);
        Assert.Equal(dense.Tracker.Assessment.TotalScore, sparse.Tracker.Assessment.TotalScore);
    }

    [Fact]
    public void GuidanceMode_ChangesAssistanceOnlyAndNeverChangesScore()
    {
        var training = CreateTrainingSession();
        training.Session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.Run));
        training.Session.Coordinator.AdvanceRunning(2, publicationStride: 2);
        var guidedScore = training.Tracker.Assessment.TotalScore;

        training.Tracker.GuidanceMode = TrainingGuidanceMode.Hidden;
        var hiddenScore = training.Tracker.Assessment.TotalScore;
        training.Tracker.GuidanceMode = TrainingGuidanceMode.ChecklistOnly;

        Assert.Equal(100, guidedScore);
        Assert.Equal(guidedScore, hiddenScore);
        Assert.Equal(guidedScore, training.Tracker.Assessment.TotalScore);
    }

    [Fact]
    public void OperatorJournal_RecordsOnlyAcceptedScenarioOperatorActionsInLogicalSequence()
    {
        var factory = new FakeInitialConditionFactory();
        var scenario = new ScenarioDefinition(
            "actions",
            "Actions",
            "Action journal test",
            factory.Descriptor.Reference,
            allowedOperatorActions: new[] { ControlRoomCommandKind.ReactorScram });
        var session = new ScenarioSessionFactory(new VersionedInitialConditionRegistry(new[] { factory })).Load(scenario);

        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.Run));
        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.ReactorScram));
        Assert.Throws<InvalidOperationException>(() =>
            session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.TurbineTrip)));

        var action = Assert.Single(session.OperatorActions.Actions);
        Assert.Equal(1, action.Sequence);
        Assert.Equal(0, action.LogicalStep);
        Assert.Equal(ControlRoomCommandKind.ReactorScram, action.Command.Kind);
    }


    [Fact]
    public void Tracker_EvaluatesAcceptedActionOrderAndProcedurePenaltyDeterministically()
    {
        var factory = new FakeInitialConditionFactory();
        var scenario = new ScenarioDefinition(
            "action-training",
            "Action training",
            "Action order training",
            factory.Descriptor.Reference,
            objectives: new[] { new ScenarioObjectiveDefinition("ordered-actions", "Ordered actions", "Execute actions in order") },
            allowedOperatorActions: new[] { ControlRoomCommandKind.ReactorScram, ControlRoomCommandKind.ProtectionReset });
        var plan = new ScenarioTrainingPlan(
            scenario.ScenarioId,
            Array.Empty<TrainingCheckpointDefinition>(),
            new[]
            {
                new TrainingEvaluationCriterionDefinition(
                    "ordered",
                    "Ordered",
                    "SCRAM then reset",
                    TrainingEvaluationCriterionKind.OperatorActionSequenceObserved,
                    operatorActions: new[] { ControlRoomCommandKind.ReactorScram, ControlRoomCommandKind.ProtectionReset }),
            },
            new[] { new TrainingObjectiveEvaluationDefinition("ordered-actions", 100, new[] { "ordered" }) },
            new[] { new TrainingPenaltyDefinition("scram-penalty", "SCRAM deviation", "Training penalty", ControlRoomCommandKind.ReactorScram, 10) });
        var session = new ScenarioSessionFactory(new VersionedInitialConditionRegistry(new[] { factory })).Load(scenario);
        var tracker = new ScenarioTrainingTracker(session, plan, new NeverCalledCheckpointEvaluator());

        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.ProtectionReset));
        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.ReactorScram));
        Assert.False(Assert.Single(tracker.Assessment.Objectives).IsAchieved);

        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.ProtectionReset));

        Assert.True(Assert.Single(tracker.Assessment.Objectives).IsAchieved);
        Assert.Equal(100, tracker.Assessment.ObjectiveScore);
        Assert.Equal(10, tracker.Assessment.PenaltyPoints);
        Assert.Equal(90, tracker.Assessment.TotalScore);
    }

    private static (ScenarioSession Session, ScenarioTrainingTracker Tracker) CreateTrainingSession()
    {
        var factory = new FakeInitialConditionFactory();
        var scenario = new ScenarioDefinition(
            "training",
            "Training",
            "Deterministic training test",
            factory.Descriptor.Reference,
            objectives: new[] { new ScenarioObjectiveDefinition("reach-step", "Reach step", "Reach logical step two") });
        var plan = new ScenarioTrainingPlan(
            scenario.ScenarioId,
            new[] { new TrainingCheckpointDefinition("step-two", "Step two", "Reach logical step two", "step:2") },
            new[]
            {
                new TrainingEvaluationCriterionDefinition(
                    "step-two-reached",
                    "Step two reached",
                    "Step two reached",
                    TrainingEvaluationCriterionKind.CheckpointSatisfied,
                    checkpointId: "step-two"),
            },
            new[] { new TrainingObjectiveEvaluationDefinition("reach-step", 100, new[] { "step-two-reached" }) });
        var session = new ScenarioSessionFactory(new VersionedInitialConditionRegistry(new[] { factory })).Load(scenario);
        var tracker = new ScenarioTrainingTracker(session, plan, new LogicalStepCheckpointEvaluator());
        return (session, tracker);
    }

    private sealed class LogicalStepCheckpointEvaluator : ITrainingCheckpointEvaluator
    {
        public TrainingCheckpointObservation Evaluate(ControlRoomSnapshot snapshot, TrainingCheckpointDefinition checkpoint)
        {
            var requiredStep = long.Parse(checkpoint.SourceCheckId["step:".Length..], System.Globalization.CultureInfo.InvariantCulture);
            return new TrainingCheckpointObservation(
                snapshot.LogicalStep >= requiredStep,
                $"Logical step {snapshot.LogicalStep}; required {requiredStep}.");
        }
    }

    private sealed class NeverCalledCheckpointEvaluator : ITrainingCheckpointEvaluator
    {
        public TrainingCheckpointObservation Evaluate(ControlRoomSnapshot snapshot, TrainingCheckpointDefinition checkpoint)
            => throw new InvalidOperationException("No checkpoint evaluation is expected for this test plan.");
    }

    private sealed class FakeInitialConditionFactory : IVersionedInitialConditionFactory
    {
        public InitialConditionDescriptor Descriptor { get; } = new(
            new InitialConditionReference("training-reference", 1),
            "Training reference",
            "Deterministic training reference");

        public IControlRoomRuntimeEngine CreateRuntimeEngine() => new FakeRuntimeEngine();
    }

    private sealed class FakeRuntimeEngine : IControlRoomRuntimeEngine
    {
        public long LogicalStep { get; private set; }

        public ControlRoomSnapshot CreatePresentationSnapshot(ControlRoomRunState runState) => Snapshot(runState);

        public ControlRoomSnapshot Step(ControlRoomRunState runState)
        {
            LogicalStep++;
            return Snapshot(runState);
        }

        public void QueueOperatorCommand(ControlRoomCommand command)
        {
            ArgumentNullException.ThrowIfNull(command);
        }

        private ControlRoomSnapshot Snapshot(ControlRoomRunState runState)
            => new(LogicalStep, runState, 0, 0, 0, 0, false, false, false);
    }
}
