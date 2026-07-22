using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using NuclearReactorSimulator.App.Runtime;
using NuclearReactorSimulator.App.ViewModels;
using NuclearReactorSimulator.Application.ControlRoom;

namespace NuclearReactorSimulator.App.Views;

public sealed partial class MainWindow : Window
{
    private static readonly TimeSpan RuntimePumpInterval = TimeSpan.FromMilliseconds(50d);
    private DispatcherTimer? _runtimeTimer;
    private Func<(ControlRoomRuntimeCoordinator Coordinator, MainWindowViewModel ViewModel)>? _runtimeFactory;

    public MainWindow()
    {
        InitializeComponent();
        Closed += (_, _) =>
        {
            _runtimeTimer?.Stop();
            if (DataContext is MainWindowViewModel viewModel)
            {
                viewModel.DetachRuntimeSubscriptions();
            }
        };
    }

    internal void AttachRuntime(
        ControlRoomRuntimeCoordinator coordinator,
        MainWindowViewModel viewModel,
        Func<(ControlRoomRuntimeCoordinator Coordinator, MainWindowViewModel ViewModel)> runtimeFactory)
    {
        ArgumentNullException.ThrowIfNull(runtimeFactory);
        _runtimeFactory = runtimeFactory;
        ReplaceRuntime(coordinator, viewModel);
    }

    internal void ReplaceRuntime(
        ControlRoomRuntimeCoordinator coordinator,
        MainWindowViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(coordinator);
        ArgumentNullException.ThrowIfNull(viewModel);

        _runtimeTimer?.Stop();
        if (DataContext is MainWindowViewModel previous && !ReferenceEquals(previous, viewModel))
        {
            previous.DetachRuntimeSubscriptions();
        }

        DataContext = viewModel;
        var pump = new DesktopControlRoomRuntimePump(coordinator, viewModel.ReportRuntimeHostFailure);
        _runtimeTimer = new DispatcherTimer
        {
            Interval = RuntimePumpInterval,
        };
        _runtimeTimer.Tick += (_, _) => pump.Tick();
        _runtimeTimer.Start();
    }

    private void ResetSession_Click(object? sender, RoutedEventArgs e)
    {
        if (_runtimeFactory is null)
        {
            return;
        }

        var replacement = _runtimeFactory();
        ReplaceRuntime(replacement.Coordinator, replacement.ViewModel);
    }
}
