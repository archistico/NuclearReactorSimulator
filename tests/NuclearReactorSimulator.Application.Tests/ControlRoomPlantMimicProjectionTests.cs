using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.ControlRoom.Hmi;
using NuclearReactorSimulator.Application.Scenarios.PreStartup;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.ControlRoom;

public sealed class ControlRoomPlantMimicProjectionTests
{
    [Fact]
    public void ColdShutdownProjection_BuildsWholePlantFlowPathFromPresentationContracts()
    {
        var controlRoom = new ColdShutdownInitialConditionFactory()
            .CreateRuntimeEngine()
            .CreatePresentationSnapshot(ControlRoomRunState.Paused);

        var mimic = ControlRoomPlantMimicProjector.Project(controlRoom);

        Assert.Equal(8, mimic.Elements.Count);
        Assert.Equal(9, mimic.Connections.Count);
        Assert.Contains(mimic.Elements, static element => element.Kind == ControlRoomPlantMimicElementKind.Reactor);
        Assert.Contains(mimic.Elements, static element => element.Kind == ControlRoomPlantMimicElementKind.SteamDrums);
        Assert.Contains(mimic.Elements, static element => element.Kind == ControlRoomPlantMimicElementKind.Turbine);
        Assert.Contains(mimic.Elements, static element => element.Kind == ControlRoomPlantMimicElementKind.Generator);
        Assert.Contains(mimic.Elements, static element => element.Kind == ControlRoomPlantMimicElementKind.Grid);
        Assert.Contains(mimic.Elements, static element => element.Kind == ControlRoomPlantMimicElementKind.Condenser);
        Assert.Contains(mimic.Elements, static element => element.Kind == ControlRoomPlantMimicElementKind.Feedwater);

        Assert.All(mimic.Elements, static element =>
        {
            Assert.False(string.IsNullOrWhiteSpace(element.InputText));
            Assert.False(string.IsNullOrWhiteSpace(element.OutputText));
            Assert.False(string.IsNullOrWhiteSpace(element.PrimaryValueText));
            Assert.InRange(element.X, 0d, 1d);
            Assert.InRange(element.Y, 0d, 1d);
        });

        var elementIds = mimic.Elements.Select(static element => element.ElementId).ToHashSet(StringComparer.Ordinal);
        Assert.Equal(mimic.Elements.Count, elementIds.Count);
        Assert.Equal(mimic.Connections.Count, mimic.Connections.Select(static connection => connection.ConnectionId).Distinct(StringComparer.Ordinal).Count());

        Assert.All(mimic.Connections, connection =>
        {
            Assert.InRange(connection.Route.Count, 2, int.MaxValue);
            Assert.False(string.IsNullOrWhiteSpace(connection.MediumText));
            Assert.False(string.IsNullOrWhiteSpace(connection.PrimaryText));
            Assert.Contains(connection.FromElementId, elementIds);
            Assert.Contains(connection.ToElementId, elementIds);
            Assert.InRange(connection.LabelX, 0d, 1d);
            Assert.InRange(connection.LabelY, 0d, 1d);
            Assert.All(connection.Route, static point =>
            {
                Assert.InRange(point.X, 0d, 1d);
                Assert.InRange(point.Y, 0d, 1d);
            });
        });
    }

    [Fact]
    public void Mimic_PreservesDistinctFluidMechanicalAndElectricalSemantics()
    {
        var controlRoom = new ColdShutdownInitialConditionFactory()
            .CreateRuntimeEngine()
            .CreatePresentationSnapshot(ControlRoomRunState.Paused);

        var mimic = ControlRoomPlantMimicProjector.Project(controlRoom);

        Assert.Contains(mimic.Connections, static connection => connection.Medium == ControlRoomPlantMimicMedium.PrimaryCoolant);
        Assert.Contains(mimic.Connections, static connection => connection.Medium == ControlRoomPlantMimicMedium.Steam);
        Assert.Contains(mimic.Connections, static connection => connection.Medium == ControlRoomPlantMimicMedium.Condensate);
        Assert.Contains(mimic.Connections, static connection => connection.Medium == ControlRoomPlantMimicMedium.Feedwater);
        Assert.Contains(mimic.Connections, static connection => connection.Medium == ControlRoomPlantMimicMedium.Mechanical);
        Assert.Contains(mimic.Connections, static connection => connection.Medium == ControlRoomPlantMimicMedium.Electrical);

        var electricalExport = Assert.Single(mimic.Connections, static connection => connection.ConnectionId == "generator-grid");
        Assert.Equal("generator", electricalExport.FromElementId);
        Assert.Equal("grid", electricalExport.ToElementId);
        Assert.Equal(ControlRoomPlantMimicMedium.Electrical, electricalExport.Medium);
    }

    [Fact]
    public void ShellOnlyProjection_RemainsExplicitlyUnavailableInsteadOfFabricatingValues()
    {
        var mimic = ControlRoomPlantMimicProjector.Project(ControlRoomSnapshot.ShellOnly);

        Assert.Equal(8, mimic.Elements.Count);
        Assert.Contains(mimic.Elements, static element => element.State == ControlRoomVisualState.Unavailable);
        Assert.Contains(mimic.Elements, static element => element.PrimaryValueText.Contains("—", StringComparison.Ordinal));
    }
}
