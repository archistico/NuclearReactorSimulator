using NuclearReactorSimulator.Application.ControlRoom.OperatorComputer;

namespace NuclearReactorSimulator.Application.ControlRoom.MissionPerformance;

/// <summary>
/// Presentation-only navigation target for M10.9.7.4 timeline drill-down. It selects an existing workspace/page and never
/// crosses the plant-command boundary.
/// </summary>
public sealed record MissionPerformanceDrillDownTarget
{
    public MissionPerformanceDrillDownTarget(
        ControlRoomWorkspaceId workspaceId,
        string label,
        OperatorComputerPageId? operatorComputerPageId = null)
    {
        if (!Enum.IsDefined(workspaceId))
        {
            throw new ArgumentOutOfRangeException(nameof(workspaceId));
        }
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        if (operatorComputerPageId.HasValue && !Enum.IsDefined(operatorComputerPageId.Value))
        {
            throw new ArgumentOutOfRangeException(nameof(operatorComputerPageId));
        }
        if (operatorComputerPageId.HasValue && workspaceId != ControlRoomWorkspaceId.OperatorComputer)
        {
            throw new ArgumentException("An operator-computer page target must select the OPERATOR COMPUTER workspace.", nameof(operatorComputerPageId));
        }

        WorkspaceId = workspaceId;
        Label = label.Trim();
        OperatorComputerPageId = operatorComputerPageId;
    }

    public ControlRoomWorkspaceId WorkspaceId { get; }
    public string Label { get; }
    public OperatorComputerPageId? OperatorComputerPageId { get; }
}
