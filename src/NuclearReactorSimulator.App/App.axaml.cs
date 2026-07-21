using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using NuclearReactorSimulator.App.Composition;
using NuclearReactorSimulator.App.Views;

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
            var root = CompositionRoot.Create();
            desktop.MainWindow = new MainWindow
            {
                DataContext = root.MainWindowViewModel,
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
