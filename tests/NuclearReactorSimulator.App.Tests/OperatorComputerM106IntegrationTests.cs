using NuclearReactorSimulator.App.ViewModels;
using NuclearReactorSimulator.Application;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.ControlRoom.OperatorComputer;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Training;
using NuclearReactorSimulator.Domain.Physics.Control;
using NuclearReactorSimulator.Domain.Physics.Control.Supervisory;
using Xunit;

namespace NuclearReactorSimulator.App.Tests.ViewModels;

public sealed class OperatorComputerM106IntegrationTests
{
    [Fact]
    public void ModesPage_KeepsTrainingAssistanceIndependentFromPhysicalControlAuthority()
    {
        var session = CreateSession();
        var tracker = CreateTracker(session);
        var viewModel = CreateViewModel(session, tracker);
        var authorityBefore = session.PlantControlAuthority.CurrentAutomation;

        viewModel.OperatorComputer.SelectPage(OperatorComputerPageId.Modes);
        viewModel.OperatorComputer.SetTrainingNoneCommand.Execute(null);

        Assert.Equal(TrainingGuidanceMode.Hidden, tracker.GuidanceMode);
        Assert.Equal(authorityBefore.RequestedAuthority, session.PlantControlAuthority.CurrentAutomation.RequestedAuthority);
        Assert.Equal(authorityBefore.EffectiveAuthority, session.PlantControlAuthority.CurrentAutomation.EffectiveAuthority);
        Assert.Contains("TRAINING ASSISTANCE", viewModel.OperatorComputer.SelectedPageContentText);
        Assert.Contains("HIDDEN", viewModel.OperatorComputer.SelectedPageContentText);
    }

    [Fact]
    public void HoldCurrentOperatingPoint_RequestsSupervisoryAndReflectsEffectiveStateAfterDeterministicStep()
    {
        var session = CreateSession();
        var tracker = CreateTracker(session);
        var viewModel = CreateViewModel(session, tracker);

        viewModel.OperatorComputer.SelectPage(OperatorComputerPageId.Modes);
        viewModel.OperatorComputer.SetPlantManualCommand.Execute(null);
        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.SingleStep));
        Assert.All(session.PlantControlAuthority.CurrentAutomation.ControllerModes, static controller => Assert.Equal(ControllerMode.Manual, controller.Mode));

        viewModel.OperatorComputer.HoldCurrentOperatingPointCommand.Execute(null);
        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.SingleStep));

        var automation = session.PlantControlAuthority.CurrentAutomation;
        Assert.Equal(PlantControlAuthorityMode.SupervisoryAutomatic, automation.RequestedAuthority);
        Assert.Equal(PlantControlAuthorityMode.SupervisoryAutomatic, automation.EffectiveAuthority);
        Assert.Equal(PlantControlAuthorityHealth.Normal, automation.Health);
        Assert.Equal(2, automation.ControllerModes.Count(static controller => controller.Mode == ControllerMode.Automatic));
        Assert.Contains("SUPERVISORYAUTOMATIC", viewModel.OperatorComputer.SelectedPageContentText.Replace(" ", string.Empty, StringComparison.Ordinal));
        Assert.Contains("HOLD OPERATING POINT", viewModel.OperatorComputer.SelectedPageContentText);
    }

    [Fact]
    public void ManualTakeoverAfterSupervisory_ReturnsAllLocalControllersToManualWithoutChangingGuidanceMode()
    {
        var session = CreateSession();
        var tracker = CreateTracker(session);
        var viewModel = CreateViewModel(session, tracker);

        viewModel.OperatorComputer.HoldCurrentOperatingPointCommand.Execute(null);
        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.SingleStep));
        Assert.Equal(PlantControlAuthorityMode.SupervisoryAutomatic, session.PlantControlAuthority.CurrentAutomation.EffectiveAuthority);

        viewModel.OperatorComputer.SetPlantManualCommand.Execute(null);
        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.SingleStep));

        Assert.Equal(TrainingGuidanceMode.Guided, tracker.GuidanceMode);
        Assert.Equal(PlantControlAuthorityMode.Manual, session.PlantControlAuthority.CurrentAutomation.EffectiveAuthority);
        Assert.All(session.PlantControlAuthority.CurrentAutomation.ControllerModes, static controller => Assert.Equal(ControllerMode.Manual, controller.Mode));
    }

    private static MainWindowViewModel CreateViewModel(ScenarioSession session, ScenarioTrainingTracker tracker)
        => new(
            ApplicationDescriptor.Current,
            session.SnapshotSource,
            session.CommandDispatcher,
            powerManoeuvringGuidance: DesktopIntegratedOperationsProgram.ProcedureGuidance,
            trainingTracker: tracker,
            plantControlAuthorityDispatcher: session.PlantControlAuthority);

    private static ScenarioSession CreateSession()
        => new ScenarioSessionFactory(new VersionedInitialConditionRegistry(new IVersionedInitialConditionFactory[]
        {
            new DesktopSustainedGenerationInitialConditionFactory(),
        })).Load(DesktopIntegratedOperationsProgram.Scenario);

    private static ScenarioTrainingTracker CreateTracker(ScenarioSession session)
        => new(
            session,
            DesktopIntegratedOperationsProgram.TrainingPlan,
            DesktopIntegratedOperationsProgram.CreateCheckpointEvaluator(),
            TrainingGuidanceMode.Guided);
}
