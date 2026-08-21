using Avalonia;
using NuclearReactorSimulator.App.Composition;

namespace NuclearReactorSimulator.App;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var avaloniaArgs = args
            .Where(static arg => !MissionChallengeStartupSelection.IsSelectionArgument(arg))
            .ToArray();
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(avaloniaArgs);
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder
            .Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
    }
}
