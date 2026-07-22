using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using NuclearReactorSimulator.App.Composition;
using NuclearReactorSimulator.App.ViewModels;
using NuclearReactorSimulator.App.Views;
using NuclearReactorSimulator.Application.ControlRoom.OperatorComputer;

namespace NuclearReactorSimulator.App.Controls;

public sealed partial class ControlRoomComputerControl : UserControl
{
    private static readonly FilePickerFileType SessionArchiveFileType = new("NRS Session Archive")
    {
        Patterns = new[] { "*.nrs-session.json", "*.json" },
    };

    public ControlRoomComputerControl()
    {
        InitializeComponent();
    }

    private void StartRecordedSession_Click(object? sender, RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is not MainWindow window)
        {
            return;
        }
        var root = CompositionRoot.Create(enableSessionRecording: true);
        window.ReplaceRuntime(root.RuntimeCoordinator, root.MainWindowViewModel);
        root.MainWindowViewModel.OperatorComputer.SelectPage(OperatorComputerPageId.Session);
        root.MainWindowViewModel.OperatorComputer.ReportSessionWorkspaceStatus(
            "RECORDED SESSION STARTED — exact initial condition reloaded at STEP 0 with M9.1 recorder active.");
    }

    private async void SaveSessionArchive_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not OperatorComputerViewModel viewModel || TopLevel.GetTopLevel(this) is not TopLevel topLevel)
        {
            return;
        }
        try
        {
            var content = viewModel.ExportSessionArchive();
            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save replay-backed reactor session",
                SuggestedFileName = "nuclear-reactor-session.nrs-session.json",
                FileTypeChoices = new[] { SessionArchiveFileType },
                DefaultExtension = "json",
            });
            if (file is null)
            {
                viewModel.ReportSessionWorkspaceStatus("SAVE CANCELLED — session remains unchanged.");
                return;
            }
            await using var stream = await file.OpenWriteAsync();
            stream.SetLength(0);
            await using var writer = new StreamWriter(stream);
            await writer.WriteAsync(content);
            await writer.FlushAsync();
            viewModel.ReportSessionWorkspaceStatus($"ARCHIVE SAVED — {file.Name}. Exact restoration remains replay/fingerprint verified.");
        }
        catch (Exception exception) when (exception is InvalidOperationException or IOException or UnauthorizedAccessException or NotSupportedException)
        {
            viewModel.ReportSessionWorkspaceStatus($"SAVE FAILED/BLOCKED — {exception.Message}");
        }
    }

    private async void LoadSessionArchive_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not OperatorComputerViewModel viewModel
            || TopLevel.GetTopLevel(this) is not MainWindow window)
        {
            return;
        }
        try
        {
            var files = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Load replay-backed reactor session",
                AllowMultiple = false,
                FileTypeFilter = new[] { SessionArchiveFileType },
            });
            var file = files.SingleOrDefault();
            if (file is null)
            {
                viewModel.ReportSessionWorkspaceStatus("LOAD CANCELLED — session remains unchanged.");
                return;
            }
            await using var stream = await file.OpenReadAsync();
            using var reader = new StreamReader(stream);
            var content = await reader.ReadToEndAsync();
            var root = CompositionRoot.CreateFromSessionArchive(content);
            window.ReplaceRuntime(root.RuntimeCoordinator, root.MainWindowViewModel);
            root.MainWindowViewModel.OperatorComputer.SelectPage(OperatorComputerPageId.Session);
            root.MainWindowViewModel.OperatorComputer.ReportSessionWorkspaceStatus(
                $"ARCHIVE LOADED & VERIFIED — {file.Name}. Recording resumed from the verified final state.");
        }
        catch (Exception exception) when (exception is InvalidOperationException or InvalidDataException or IOException or UnauthorizedAccessException or NotSupportedException)
        {
            viewModel.ReportSessionWorkspaceStatus($"LOAD FAILED — {exception.Message}");
        }
    }

    private void RestoreSelectedCheckpoint_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not OperatorComputerViewModel viewModel
            || TopLevel.GetTopLevel(this) is not MainWindow window)
        {
            return;
        }
        try
        {
            var checkpointId = viewModel.SelectedSessionCheckpointId
                ?? throw new InvalidOperationException("Select a replay-backed checkpoint before restore.");
            var archive = viewModel.ExportSessionArchive();
            var root = CompositionRoot.CreateFromSessionArchive(archive, checkpointId);
            window.ReplaceRuntime(root.RuntimeCoordinator, root.MainWindowViewModel);
            root.MainWindowViewModel.OperatorComputer.SelectPage(OperatorComputerPageId.Session);
            root.MainWindowViewModel.OperatorComputer.ReportSessionWorkspaceStatus(
                $"CHECKPOINT RESTORED & VERIFIED — {checkpointId}. Recording resumed from that deterministic prefix.");
        }
        catch (Exception exception) when (exception is InvalidOperationException or InvalidDataException or KeyNotFoundException or NotSupportedException)
        {
            viewModel.ReportSessionWorkspaceStatus($"RESTORE FAILED/BLOCKED — {exception.Message}");
        }
    }
}
