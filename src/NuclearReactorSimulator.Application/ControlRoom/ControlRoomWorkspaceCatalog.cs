using System.Collections.ObjectModel;

namespace NuclearReactorSimulator.Application.ControlRoom;

public static class ControlRoomWorkspaceCatalog
{
    public static IReadOnlyList<ControlRoomWorkspaceDescriptor> Default { get; } =
        new ReadOnlyCollection<ControlRoomWorkspaceDescriptor>(new[]
        {
            new ControlRoomWorkspaceDescriptor(ControlRoomWorkspaceId.Overview, "Plant Overview", "PLANT", "Whole-plant operating context and primary situation-awareness entry point."),
            new ControlRoomWorkspaceDescriptor(ControlRoomWorkspaceId.Reactor, "Reactor & Core", "REACTOR", "Reactor instrumentation, reactivity, rod/group controls and protection context."),
            new ControlRoomWorkspaceDescriptor(ControlRoomWorkspaceId.PrimaryCircuit, "Primary Circuit", "PRIMARY", "Main circulation, headers, channel groups, steam drums and primary-system controls."),
            new ControlRoomWorkspaceDescriptor(ControlRoomWorkspaceId.TurbineSecondary, "Turbine & Secondary Cycle", "TURBINE", "Main steam, turbine, condenser, hotwell and feedwater operating workspace."),
            new ControlRoomWorkspaceDescriptor(ControlRoomWorkspaceId.Electrical, "Generator & Grid", "GRID", "Generator synchronization, breaker state, electrical output and grid-facing controls."),
            new ControlRoomWorkspaceDescriptor(ControlRoomWorkspaceId.AlarmsEvents, "Alarms & Events", "ALARMS", "Annunciator, first-out state and deterministic logical-step event review."),
            new ControlRoomWorkspaceDescriptor(ControlRoomWorkspaceId.OperatorComputer, "Operator Computer", "COMPUTER", "Unified fixed-page utility workstation for guidance, diagnostics, commands, modes, log and session tools."),
        });
}
