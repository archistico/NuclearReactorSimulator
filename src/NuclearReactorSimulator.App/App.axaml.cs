using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using NuclearReactorSimulator.App.Composition;
using NuclearReactorSimulator.App.ViewModels;
using NuclearReactorSimulator.App.Views;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios.Challenges.Packs;

namespace NuclearReactorSimulator.App;

public sealed partial class App : Avalonia.Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            OperationalChallengePackDefinition? missionPack = MissionChallengeStartupSelection.Resolve(
                Environment.GetCommandLineArgs().Skip(1));

            (ControlRoomRuntimeCoordinator Coordinator, MainWindowViewModel ViewModel) CreateDesktopRuntime()
            {
                var root = missionPack is null
                    ? CompositionRoot.Create()
                    : CompositionRoot.CreateMissionChallenge(missionPack);
                return (root.RuntimeCoordinator, root.MainWindowViewModel);
            }

            var runtime = CreateDesktopRuntime();
            var mainWindow = new MainWindow();
            mainWindow.AttachRuntime(runtime.Coordinator, runtime.ViewModel, CreateDesktopRuntime);
            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }
}
