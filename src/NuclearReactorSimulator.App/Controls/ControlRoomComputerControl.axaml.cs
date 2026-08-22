using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using NuclearReactorSimulator.App.Composition;
using NuclearReactorSimulator.App.Persistence;
using NuclearReactorSimulator.App.Runtime;
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

    private readonly DesktopSessionArchiveSaveCoordinator _archiveSaveCoordinator = new();

    public ControlRoomComputerControl()
    {
        InitializeComponent();
    }

    private void CommandCatalog_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || DataContext is not OperatorComputerViewModel viewModel)
        {
            return;
        }

        viewModel.ExecuteSelectedCommandCommand.Execute(null);
        e.Handled = true;
    }

    private void StartRecordedSession_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not OperatorComputerViewModel viewModel
            || TopLevel.GetTopLevel(this) is not MainWindow window)
        {
            return;
        }

        ApplicationRoot root;
        try
        {
            var missionPack = (window.DataContext as MainWindowViewModel)?.MissionPerformancePackExactId is { } exactPackId
                ? MissionChallengeStartupSelection.ResolveExactId(exactPackId)
                : null;
            root = missionPack is null
                ? CompositionRoot.Create(enableSessionRecording: true)
                : CompositionRoot.CreateMissionChallenge(missionPack, enableSessionRecording: true);
        }
        catch (Exception exception) when (DesktopHostFailurePolicy.IsExpectedRuntimeConstructionFailure(exception))
        {
            viewModel.ReportSessionWorkspaceStatus($"START RECORDED SESSION FAILED/BLOCKED — {exception.Message}");
            return;
        }

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
            var savedTarget = await _archiveSaveCoordinator.SaveAsync(
                async () =>
                {
                    var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                    {
                        Title = "Save replay-backed reactor session",
                        SuggestedFileName = "nuclear-reactor-session.nrs-session.json",
                        FileTypeChoices = new[] { SessionArchiveFileType },
                        DefaultExtension = "json",
                    });
                    if (file is null)
                    {
                        return null;
                    }

                    return DesktopSessionArchiveSaveCoordinator.RequireLocalTarget(file.TryGetLocalPath(), file.Name);
                },
                viewModel.ExportSessionArchive);

            if (savedTarget is null)
            {
                viewModel.ReportSessionWorkspaceStatus("SAVE CANCELLED — session remains unchanged; archive export was not requested.");
                return;
            }

            viewModel.ReportSessionWorkspaceStatus(
                $"ARCHIVE SAVED — {savedTarget.DisplayName}. Non-destructive local replacement completed; restoration remains replay/fingerprint verified.");
        }
        catch (Exception exception) when (DesktopHostFailurePolicy.IsExpectedArchiveOperationFailure(exception))
        {
            viewModel.ReportSessionWorkspaceStatus(
                $"SAVE FAILED/BLOCKED — {exception.Message} Existing archive was not truncate-written by NRS.");
        }
    }

    private async void LoadSessionArchive_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not OperatorComputerViewModel viewModel
            || TopLevel.GetTopLevel(this) is not MainWindow window)
        {
            return;
        }

        ApplicationRoot root;
        string fileName;
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
            var missionPack = (window.DataContext as MainWindowViewModel)?.MissionPerformancePackExactId is { } exactPackId
                ? MissionChallengeStartupSelection.ResolveExactId(exactPackId)
                : null;
            root = CompositionRoot.CreateFromSessionArchive(content, missionPack: missionPack);
            fileName = file.Name;
        }
        catch (Exception exception) when (DesktopHostFailurePolicy.IsExpectedArchiveOperationFailure(exception))
        {
            viewModel.ReportSessionWorkspaceStatus($"LOAD FAILED — {exception.Message}");
            return;
        }

        window.ReplaceRuntime(root.RuntimeCoordinator, root.MainWindowViewModel);
        root.MainWindowViewModel.OperatorComputer.SelectPage(OperatorComputerPageId.Session);
        root.MainWindowViewModel.OperatorComputer.ReportSessionWorkspaceStatus(
            $"ARCHIVE LOADED & VERIFIED — {fileName}. Recording resumed from the verified final state.");
    }

    private void RestoreSelectedCheckpoint_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not OperatorComputerViewModel viewModel
            || TopLevel.GetTopLevel(this) is not MainWindow window)
        {
            return;
        }

        ApplicationRoot root;
        string checkpointId;
        try
        {
            checkpointId = viewModel.SelectedSessionCheckpointId
                ?? throw new InvalidOperationException("Select a replay-backed checkpoint before restore.");
            var archive = viewModel.ExportSessionArchive();
            var missionPack = (window.DataContext as MainWindowViewModel)?.MissionPerformancePackExactId is { } exactPackId
                ? MissionChallengeStartupSelection.ResolveExactId(exactPackId)
                : null;
            root = CompositionRoot.CreateFromSessionArchive(archive, checkpointId, missionPack);
        }
        catch (Exception exception) when (DesktopHostFailurePolicy.IsExpectedArchiveOperationFailure(exception))
        {
            viewModel.ReportSessionWorkspaceStatus($"RESTORE FAILED/BLOCKED — {exception.Message}");
            return;
        }

        window.ReplaceRuntime(root.RuntimeCoordinator, root.MainWindowViewModel);
        root.MainWindowViewModel.OperatorComputer.SelectPage(OperatorComputerPageId.Session);
        root.MainWindowViewModel.OperatorComputer.ReportSessionWorkspaceStatus(
            $"CHECKPOINT RESTORED & VERIFIED — {checkpointId}. Recording resumed from that deterministic prefix.");
    }
}
