using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Training;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Training;

public sealed class IntegratedOperationsTrainingProgramTests
{
    [Fact]
    public void Program_ReusesValidatedM76InitialConditionAndDeclaresOneHundredPointEvaluation()
    {
        var scenario = IntegratedOperationsTrainingProgram.Scenario;
        var plan = IntegratedOperationsTrainingProgram.TrainingPlan;

        Assert.Equal("stable-low-load-parallel-operation", scenario.InitialCondition.InitialConditionId);
        Assert.Equal(1, scenario.InitialCondition.Version);
        Assert.Equal(scenario.ScenarioId, plan.ScenarioId);
        Assert.Equal(100, plan.MaximumScore);
        Assert.Equal(4, plan.Objectives.Count);
        Assert.Equal(scenario.Objectives.Select(static objective => objective.ObjectiveId).OrderBy(static id => id),
            plan.Objectives.Select(static objective => objective.ObjectiveId).OrderBy(static id => id));
    }


    [Fact]
    public void InitialAssessment_RecognizesOnlyTheValidatedLowLoadHandoffBeforeTrainingProgression()
    {
        var registry = new VersionedInitialConditionRegistry(new IVersionedInitialConditionFactory[]
        {
            new NuclearReactorSimulator.Application.Scenarios.Operations.PowerManoeuvringInitialConditionFactory(),
        });
        var session = new ScenarioSessionFactory(registry).Load(IntegratedOperationsTrainingProgram.Scenario);
        var tracker = new ScenarioTrainingTracker(
            session,
            IntegratedOperationsTrainingProgram.TrainingPlan,
            IntegratedOperationsTrainingProgram.CreateCheckpointEvaluator());

        Assert.Equal(15, tracker.Assessment.TotalScore);
        Assert.True(tracker.Assessment.Objectives.Single(static objective => objective.Objective.ObjectiveId == "verify-low-load").IsAchieved);
        Assert.False(tracker.Assessment.Objectives.Single(static objective => objective.Objective.ObjectiveId == "manoeuvre-power").IsAchieved);
        Assert.False(tracker.Assessment.Checkpoints.Single(static checkpoint => checkpoint.Definition.CheckpointId == "reduced-load").IsSatisfied);
    }

    [Fact]
    public void Program_PreservesNormalOperationCommandsAndScoresEmergencyActionsAsTrainingDeviations()
    {
        var scenario = IntegratedOperationsTrainingProgram.Scenario;
        var penalties = IntegratedOperationsTrainingProgram.TrainingPlan.Penalties;

        Assert.Contains(ControlRoomCommandKind.GeneratorLoadRaise, scenario.AllowedOperatorActions);
        Assert.Contains(ControlRoomCommandKind.GeneratorLoadLower, scenario.AllowedOperatorActions);
        Assert.Contains(ControlRoomCommandKind.GeneratorBreakerOpen, scenario.AllowedOperatorActions);
        Assert.Contains(ControlRoomCommandKind.ControlRodInsert, scenario.AllowedOperatorActions);
        Assert.Contains(penalties, static penalty => penalty.TriggerAction == ControlRoomCommandKind.ReactorScram);
        Assert.Contains(penalties, static penalty => penalty.TriggerAction == ControlRoomCommandKind.TurbineTrip);
        Assert.Contains(penalties, static penalty => penalty.TriggerAction == ControlRoomCommandKind.GeneratorTrip);
    }
}
