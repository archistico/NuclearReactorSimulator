using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using NuclearReactorSimulator.App.Composition;
using NuclearReactorSimulator.App.ViewModels;
using NuclearReactorSimulator.App.Views;
using NuclearReactorSimulator.Application.ControlRoom;

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
            static (ControlRoomRuntimeCoordinator Coordinator, MainWindowViewModel ViewModel) CreateDesktopRuntime()
            {
                var root = CompositionRoot.Create();
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
