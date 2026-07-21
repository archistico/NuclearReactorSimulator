using NuclearReactorSimulator.App.ViewModels;
using NuclearReactorSimulator.Application;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Criticality;
using NuclearReactorSimulator.Application.Scenarios.Operations;
using NuclearReactorSimulator.Application.Scenarios.PreStartup;
using NuclearReactorSimulator.Application.Scenarios.Startup;
using NuclearReactorSimulator.Application.Scenarios.Synchronization;

namespace NuclearReactorSimulator.App.Composition;

/// <summary>
/// The only place responsible for constructing the application object graph.
/// </summary>
internal static class CompositionRoot
{
    public static ApplicationRoot Create()
    {
        var descriptor = ApplicationDescriptor.Current;
        var registry = new VersionedInitialConditionRegistry(new IVersionedInitialConditionFactory[]
        {
            new ColdShutdownInitialConditionFactory(),
            new FirstCriticalityInitialConditionFactory(),
            new HeatUpTurbineStartupInitialConditionFactory(),
            new GridSynchronizationInitialConditionFactory(),
            new PowerManoeuvringInitialConditionFactory(),
        });
        var session = new ScenarioSessionFactory(registry).Load(PowerManoeuvringNormalShutdownProgram.Scenario);
        var mainWindowViewModel = new MainWindowViewModel(
            descriptor,
            session.SnapshotSource,
            session.CommandDispatcher,
            powerManoeuvringGuidance: PowerManoeuvringNormalShutdownProgram.Guidance);

        return new ApplicationRoot(mainWindowViewModel);
    }
}

internal sealed record ApplicationRoot(MainWindowViewModel MainWindowViewModel);
