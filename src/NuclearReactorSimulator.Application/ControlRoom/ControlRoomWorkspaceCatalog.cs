using System.Collections.ObjectModel;

namespace NuclearReactorSimulator.Application.ControlRoom;

public static class ControlRoomWorkspaceCatalog
{
    public static IReadOnlyList<ControlRoomWorkspaceDescriptor> Default { get; } =
        new ReadOnlyCollection<ControlRoomWorkspaceDescriptor>(new[]
        {
            new ControlRoomWorkspaceDescriptor(ControlRoomWorkspaceId.Overview, "Plant Overview", "Overview", "Whole-plant operating context, headline status and navigation entry point."),
            new ControlRoomWorkspaceDescriptor(ControlRoomWorkspaceId.Reactor, "Reactor & Core", "Reactor", "Reactor instrumentation, rod/group controls and reactor-protection context."),
            new ControlRoomWorkspaceDescriptor(ControlRoomWorkspaceId.PrimaryCircuit, "Primary Circuit", "Primary", "Main circulation, headers, channel groups, steam drums and primary-system controls."),
            new ControlRoomWorkspaceDescriptor(ControlRoomWorkspaceId.TurbineSecondary, "Turbine & Secondary Cycle", "Turbine", "Main steam, turbine, condenser, hotwell and feedwater operating workspace."),
            new ControlRoomWorkspaceDescriptor(ControlRoomWorkspaceId.Electrical, "Generator & Electrical", "Electrical", "Generator synchronization, breaker state, electrical output and grid-facing controls."),
            new ControlRoomWorkspaceDescriptor(ControlRoomWorkspaceId.AlarmsEvents, "Alarms & Events", "Alarms", "Annunciator, first-out state and deterministic logical-step event review."),
            new ControlRoomWorkspaceDescriptor(ControlRoomWorkspaceId.OperatorComputer, "Operator Computer", "Computer", "Unified fixed-page operator terminal shell over canonical control-room, training and session contracts."),
        });
}
