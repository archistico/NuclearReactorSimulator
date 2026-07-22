using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios.Analysis;
using NuclearReactorSimulator.Application.Scenarios.Recording;

namespace NuclearReactorSimulator.Application.ControlRoom.OperatorComputer;

public static class OperatorComputerSnapshotProjector
{
    public static OperatorComputerSnapshot Project(
        ControlRoomSnapshot controlRoomSnapshot,
        OperatorComputerScenarioContentSnapshot? scenarioContent = null,
        ControlRoomOperationalHistorySnapshot? operationalHistory = null,
        IEnumerable<ScenarioRecordingEvent>? sessionEvents = null,
        PostIncidentAnalysisReport? incident = null,
        OperatorComputerModesSnapshot? modes = null,
        OperatorComputerSessionSnapshot? session = null)
    {
        ArgumentNullException.ThrowIfNull(controlRoomSnapshot);

        var runtimeStatus = new OperatorComputerRuntimeStatusSnapshot(
            controlRoomSnapshot.LogicalStep,
            controlRoomSnapshot.RunState,
            controlRoomSnapshot.InvalidMeasuredSignalCount,
            controlRoomSnapshot.AnnunciatedAlarmCount,
            controlRoomSnapshot.UnacknowledgedAlarmCount,
            controlRoomSnapshot.AnyTripActive);

        var pages = OperatorComputerPageCatalog.Default.Select(descriptor =>
            new OperatorComputerPageSnapshot(
                descriptor.Id,
                descriptor.MenuLabel,
                descriptor.Title,
                descriptor.Description,
                ContentState(descriptor.Id, scenarioContent, operationalHistory, modes, session)));

        return new OperatorComputerSnapshot(
            runtimeStatus,
            pages,
            OperatorComputerInformationProjector.Project(controlRoomSnapshot),
            scenarioContent?.Guidance,
            scenarioContent?.Diagnostics,
            OperatorComputerAlarmLogProjector.ProjectAlarms(controlRoomSnapshot, operationalHistory),
            operationalHistory is null ? null : OperatorComputerAlarmLogProjector.ProjectLog(operationalHistory, sessionEvents, incident),
            OperatorComputerCommandConsoleProjector.Project(controlRoomSnapshot),
            modes,
            session);
    }

    private static OperatorComputerPageContentState ContentState(
        OperatorComputerPageId pageId,
        OperatorComputerScenarioContentSnapshot? scenarioContent,
        ControlRoomOperationalHistorySnapshot? operationalHistory,
        OperatorComputerModesSnapshot? modes,
        OperatorComputerSessionSnapshot? session)
        => pageId switch
        {
            OperatorComputerPageId.Info => OperatorComputerPageContentState.Available,
            OperatorComputerPageId.Alarms => OperatorComputerPageContentState.Available,
            OperatorComputerPageId.Log => operationalHistory is null
                ? OperatorComputerPageContentState.Unavailable
                : OperatorComputerPageContentState.Available,
            OperatorComputerPageId.Guidance => scenarioContent is null
                ? OperatorComputerPageContentState.Unavailable
                : OperatorComputerPageContentState.Available,
            OperatorComputerPageId.Diagnostics => scenarioContent is null
                ? OperatorComputerPageContentState.Unavailable
                : OperatorComputerPageContentState.Available,
            OperatorComputerPageId.Commands => OperatorComputerPageContentState.Available,
            OperatorComputerPageId.Modes => modes is null
                ? OperatorComputerPageContentState.ShellOnly
                : OperatorComputerPageContentState.Available,
            OperatorComputerPageId.Session => session is null
                ? OperatorComputerPageContentState.ShellOnly
                : OperatorComputerPageContentState.Available,
            _ => OperatorComputerPageContentState.ShellOnly,
        };
}
