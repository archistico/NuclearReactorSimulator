using NuclearReactorSimulator.Application.ControlRoom;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.ControlRoom;

public sealed class ControlRoomWorkspaceCatalogTests
{
    [Fact]
    public void DefaultCatalog_ProvidesStableUniqueControlRoomWorkspaces()
    {
        var workspaces = ControlRoomWorkspaceCatalog.Default;

        Assert.Equal(7, workspaces.Count);
        Assert.Equal(ControlRoomWorkspaceId.Overview, workspaces[0].Id);
        Assert.Equal(workspaces.Count, workspaces.Select(static item => item.Id).Distinct().Count());
        Assert.Contains(workspaces, static item => item.Id == ControlRoomWorkspaceId.Reactor);
        Assert.Contains(workspaces, static item => item.Id == ControlRoomWorkspaceId.PrimaryCircuit);
        Assert.Contains(workspaces, static item => item.Id == ControlRoomWorkspaceId.TurbineSecondary);
        Assert.Contains(workspaces, static item => item.Id == ControlRoomWorkspaceId.Electrical);
        Assert.Contains(workspaces, static item => item.Id == ControlRoomWorkspaceId.AlarmsEvents);
        Assert.Contains(workspaces, static item => item.Id == ControlRoomWorkspaceId.OperatorComputer);
    }
}
