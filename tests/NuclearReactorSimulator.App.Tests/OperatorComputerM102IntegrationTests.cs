using NuclearReactorSimulator.App.ViewModels;
using NuclearReactorSimulator.Application;
using NuclearReactorSimulator.Application.ControlRoom.OperatorComputer;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Training;
using Xunit;

namespace NuclearReactorSimulator.App.Tests.ViewModels;

public sealed class OperatorComputerM102IntegrationTests
{
    [Fact]
    public void DesktopScenario_ProjectsCanonicalGuidanceInformationAndDiagnosticsIntoTerminalViewModel()
    {
        var registry = new VersionedInitialConditionRegistry(new IVersionedInitialConditionFactory[]
        {
            new DesktopIntegratedOperationsInitialConditionFactory(),
        });
        var session = new ScenarioSessionFactory(registry).Load(DesktopIntegratedOperationsProgram.Scenario);
        var tracker = new ScenarioTrainingTracker(
            session,
            DesktopIntegratedOperationsProgram.TrainingPlan,
            DesktopIntegratedOperationsProgram.CreateCheckpointEvaluator(),
            TrainingGuidanceMode.Guided);
        var viewModel = new MainWindowViewModel(
            ApplicationDescriptor.Current,
            session.SnapshotSource,
            session.CommandDispatcher,
            powerManoeuvringGuidance: DesktopIntegratedOperationsProgram.ProcedureGuidance,
            trainingTracker: tracker);

        viewModel.OperatorComputer.SelectPage(OperatorComputerPageId.Guidance);
        Assert.Contains("POWER MANOEUVRING / NORMAL SHUTDOWN", viewModel.OperatorComputer.SelectedPageContentText);
        Assert.Contains("[OK]", viewModel.OperatorComputer.SelectedPageContentText);
        Assert.Contains("TRAINING CHECKPOINTS", viewModel.OperatorComputer.SelectedPageContentText);
        Assert.Contains("SCORE", viewModel.OperatorComputer.SelectedPageContentText);

        viewModel.OperatorComputer.SelectPage(OperatorComputerPageId.Info);
        Assert.Contains("[REACTOR]", viewModel.OperatorComputer.SelectedPageContentText);
        Assert.Contains("[MEASURED]", viewModel.OperatorComputer.SelectedPageContentText);
        Assert.Contains("GROSS OUTPUT", viewModel.OperatorComputer.SelectedPageContentText);

        viewModel.OperatorComputer.SelectPage(OperatorComputerPageId.Diagnostics);
        Assert.Contains("DIAGNOSTIC: POWER MANOEUVRING / NORMAL SHUTDOWN", viewModel.OperatorComputer.SelectedPageContentText);
        Assert.Contains("CHECKS SATISFIED", viewModel.OperatorComputer.SelectedPageContentText);
        Assert.Contains("[!!]", viewModel.OperatorComputer.SelectedPageContentText);
    }
}
