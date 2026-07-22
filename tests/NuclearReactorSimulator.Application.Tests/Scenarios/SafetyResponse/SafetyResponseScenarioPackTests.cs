using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios.Faults;
using NuclearReactorSimulator.Application.Scenarios.Operations;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.SafetyResponse;
using NuclearReactorSimulator.Application.Scenarios.Training;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.SafetyResponse;

public sealed class SafetyResponseScenarioPackTests
{
    [Fact]
    public void Pack_DeclaresThreeOneHundredPointCapstoneExercisesWithoutNewPhysicalOwnership()
    {
        Assert.Equal(3, SafetyResponseScenarioPack.All.Count);
        Assert.All(SafetyResponseScenarioPack.All, exercise =>
        {
            Assert.Equal(100, exercise.TrainingPlan.MaximumScore);
            Assert.Equal(exercise.Scenario.ScenarioId, exercise.TrainingPlan.ScenarioId);
            Assert.Equal(
                exercise.Scenario.Objectives.Select(static objective => objective.ObjectiveId).OrderBy(static id => id),
                exercise.TrainingPlan.Objectives.Select(static objective => objective.ObjectiveId).OrderBy(static id => id));
            Assert.NotEmpty(exercise.Scenario.Faults);
        });
    }

    [Fact]
    public void Pack_ReusesExactPriorFaultDeclarationsInsteadOfDuplicatingFaultPhysics()
    {
        Assert.Contains(SafetyResponseScenarioPack.ProtectionFailSafe.Scenario.Faults,
            static fault => fault.FaultTypeId == "instrumentation.sensor-unavailable");
        Assert.Contains(SafetyResponseScenarioPack.LargeBreakClass.Scenario.Faults,
            static fault => fault.FaultTypeId == "loca.pressure-driven-break");
        Assert.Contains(SafetyResponseScenarioPack.StationBlackoutClass.Scenario.Faults,
            static fault => fault.FaultTypeId == "electrical.external-supply-loss");
    }

    [Fact]
    public void EvaluationSession_ExposesAcceptedOperatorActionsAsDeterministicDebriefTimeline()
    {
        var registry = new VersionedInitialConditionRegistry(new IVersionedInitialConditionFactory[]
        {
            new PowerManoeuvringInitialConditionFactory(),
        });
        var exercise = SafetyResponseScenarioPack.ProtectionFailSafe;
        var session = new ScenarioSessionFactory(registry).Load(exercise.Scenario);
        var evaluation = exercise.Attach(session);

        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.AlarmAcknowledgeAll));

        var action = Assert.Single(evaluation.OperatorActionTimeline);
        Assert.Equal(1, action.Sequence);
        Assert.Equal(session.Coordinator.Current.LogicalStep, action.LogicalStep);
        Assert.Equal(ControlRoomCommandKind.AlarmAcknowledgeAll, action.Command.Kind);
    }

    [Fact]
    public void CheckpointEvaluator_UsesPresentationSnapshotForFaultProtectionAndIsolationState()
    {
        var evaluator = new SafetyResponseCheckpointEvaluator();
        var snapshot = CreateSnapshot();

        Assert.True(Evaluate(evaluator, snapshot, "fault-active:test-fault").IsSatisfied);
        Assert.True(Evaluate(evaluator, snapshot, SafetyResponseCheckpointEvaluator.ReactorScramActiveCheckId).IsSatisfied);
        Assert.True(Evaluate(evaluator, snapshot, SafetyResponseCheckpointEvaluator.AnyTripActiveCheckId).IsSatisfied);
        Assert.True(Evaluate(evaluator, snapshot, SafetyResponseCheckpointEvaluator.GeneratorBreakerOpenCheckId).IsSatisfied);
        Assert.True(Evaluate(evaluator, snapshot, SafetyResponseCheckpointEvaluator.InvalidMeasurementPresentCheckId).IsSatisfied);
        Assert.True(Evaluate(evaluator, snapshot, SafetyResponseCheckpointEvaluator.AlarmAnnunciatedCheckId).IsSatisfied);
        Assert.True(Evaluate(evaluator, snapshot, SafetyResponseCheckpointEvaluator.AlarmsAcknowledgedCheckId).IsSatisfied);
    }

    private static TrainingCheckpointObservation Evaluate(
        SafetyResponseCheckpointEvaluator evaluator,
        ControlRoomSnapshot snapshot,
        string sourceCheckId)
        => evaluator.Evaluate(snapshot, new TrainingCheckpointDefinition("check", "Check", "Check", sourceCheckId));

    private static ControlRoomSnapshot CreateSnapshot()
    {
        var unavailable = ControlRoomValueSnapshot.Unavailable();
        var electrical = new ElectricalPanelSnapshot(
            ElectricalGridPresentationSnapshot.Unavailable,
            new[]
            {
                new GeneratorPresentationSnapshot(
                    "generator",
                    "rotor",
                    "breaker",
                    unavailable,
                    unavailable,
                    unavailable,
                    unavailable,
                    unavailable,
                    unavailable,
                    unavailable,
                    false,
                    false,
                    false,
                    false),
            },
            unavailable,
            true);
        var faults = new ControlRoomFaultStateSnapshot(new[]
        {
            new ControlRoomFaultStatusSnapshot(
                "test-fault",
                "test.type",
                "target",
                ScenarioFaultLifecycleState.Active,
                10,
                null,
                1),
        });

        return new ControlRoomSnapshot(
            12,
            ControlRoomRunState.Paused,
            5,
            1,
            2,
            0,
            true,
            false,
            true,
            electrical: electrical,
            faults: faults);
    }
}
