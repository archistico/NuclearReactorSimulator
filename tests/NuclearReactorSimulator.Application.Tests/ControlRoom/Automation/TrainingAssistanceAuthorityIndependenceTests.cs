using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Training;
using NuclearReactorSimulator.Domain.Physics.Control.Supervisory;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.ControlRoom.Automation;

public sealed class TrainingAssistanceAuthorityIndependenceTests
{
    [Fact]
    public void TrainingGuidanceChanges_DoNotChangePhysicalPlantControlAuthority()
    {
        var session = new ScenarioSessionFactory(new VersionedInitialConditionRegistry(new IVersionedInitialConditionFactory[]
        {
            new DesktopSustainedGenerationInitialConditionFactory(),
        })).Load(DesktopIntegratedOperationsProgram.Scenario);
        var tracker = new ScenarioTrainingTracker(
            session,
            DesktopIntegratedOperationsProgram.TrainingPlan,
            DesktopIntegratedOperationsProgram.CreateCheckpointEvaluator(),
            TrainingGuidanceMode.Guided);
        var before = session.PlantControlAuthority.CurrentAutomation;

        tracker.SetGuidanceMode(TrainingGuidanceMode.Hidden);
        var afterHidden = session.PlantControlAuthority.CurrentAutomation;
        tracker.SetGuidanceMode(TrainingGuidanceMode.ChecklistOnly);
        var afterChecklist = session.PlantControlAuthority.CurrentAutomation;

        Assert.Equal(TrainingGuidanceMode.ChecklistOnly, tracker.GuidanceMode);
        Assert.Equal(before.RequestedAuthority, afterHidden.RequestedAuthority);
        Assert.Equal(before.EffectiveAuthority, afterHidden.EffectiveAuthority);
        Assert.Equal(before.RequestedAuthority, afterChecklist.RequestedAuthority);
        Assert.Equal(before.EffectiveAuthority, afterChecklist.EffectiveAuthority);
        Assert.Equal(before.ControllerModes.Select(static item => item.Mode), afterChecklist.ControllerModes.Select(static item => item.Mode));
        Assert.NotEqual(PlantControlAuthorityMode.SupervisoryAutomatic, afterChecklist.EffectiveAuthority);
    }
}
