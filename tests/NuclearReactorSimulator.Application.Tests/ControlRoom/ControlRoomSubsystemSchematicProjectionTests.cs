using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.ControlRoom.Hmi;
using NuclearReactorSimulator.Application.Scenarios.Synchronization;
using NuclearReactorSimulator.Application.Scenarios.Training;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.ControlRoom;

public sealed class ControlRoomSubsystemSchematicProjectionTests
{
    [Fact]
    public void Project_PublishesFiveEngineeringSchematicsWithExplicitEndpoints()
    {
        var snapshot = new DesktopIntegratedOperationsInitialConditionFactory()
            .CreateRuntimeEngine()
            .CreatePresentationSnapshot(ControlRoomRunState.Paused);

        var projected = ControlRoomSubsystemSchematicProjector.Project(snapshot);
        var schematics = new[]
        {
            projected.ReactorCore,
            projected.PrimarySteamDrum,
            projected.TurbineSecondary,
            projected.GeneratorGrid,
            projected.InstrumentationProtection,
        };

        Assert.Equal(5, schematics.Select(static item => item.Kind).Distinct().Count());
        Assert.All(schematics, static schematic =>
        {
            Assert.NotEmpty(schematic.Nodes);
            Assert.NotEmpty(schematic.Connections);
            Assert.All(schematic.Nodes, static node =>
            {
                Assert.StartsWith("IN ·", node.InputText);
                Assert.StartsWith("OUT ·", node.OutputText);
                Assert.InRange(node.X, 0d, 1d);
                Assert.InRange(node.Y, 0d, 1d);
                Assert.InRange(node.Width, 0.01d, 1d);
                Assert.InRange(node.Height, 0.01d, 1d);
                Assert.True(node.X + node.Width <= 1.001d);
                Assert.True(node.Y + node.Height <= 1.001d);
            });

            var nodeIds = schematic.Nodes.Select(static node => node.NodeId).ToHashSet(StringComparer.Ordinal);
            Assert.Equal(schematic.Nodes.Count, nodeIds.Count);
            Assert.Equal(schematic.Connections.Count, schematic.Connections.Select(static c => c.ConnectionId).Distinct(StringComparer.Ordinal).Count());
            Assert.All(schematic.Connections, connection =>
            {
                Assert.Contains(connection.FromNodeId, nodeIds);
                Assert.Contains(connection.ToNodeId, nodeIds);
                Assert.True(connection.Route.Count >= 2);
                Assert.InRange(connection.LabelX, 0d, 1d);
                Assert.InRange(connection.LabelY, 0d, 1d);
                Assert.All(connection.Route, static point =>
                {
                    Assert.InRange(point.X, 0d, 1d);
                    Assert.InRange(point.Y, 0d, 1d);
                });
            });
        });
    }

    [Fact]
    public void TurbineAndGeneratorSchematics_SeparateMechanicalElectricalAndProtectionSemantics()
    {
        var snapshot = new DesktopIntegratedOperationsInitialConditionFactory()
            .CreateRuntimeEngine()
            .CreatePresentationSnapshot(ControlRoomRunState.Paused);
        var projected = ControlRoomSubsystemSchematicProjector.Project(snapshot);

        Assert.Contains(projected.TurbineSecondary.Connections, static c => c.Kind == ControlRoomSubsystemSchematicConnectionKind.Steam);
        Assert.Contains(projected.TurbineSecondary.Connections, static c => c.Kind == ControlRoomSubsystemSchematicConnectionKind.Mechanical);
        Assert.Contains(projected.TurbineSecondary.Nodes, static n => n.DisplayName == "STOP VALVE");
        Assert.Contains(projected.TurbineSecondary.Nodes, static n => n.DisplayName == "CONTROL VALVE");
        Assert.Contains(projected.TurbineSecondary.Nodes, static n => n.DisplayName == "ADMISSION VALVE");

        Assert.Contains(projected.GeneratorGrid.Connections, static c => c.Kind == ControlRoomSubsystemSchematicConnectionKind.Mechanical);
        Assert.Contains(projected.GeneratorGrid.Connections, static c => c.Kind == ControlRoomSubsystemSchematicConnectionKind.Electrical);
        Assert.Contains(projected.GeneratorGrid.Connections, static c => c.Kind == ControlRoomSubsystemSchematicConnectionKind.ProtectionOverride);
        Assert.Contains(projected.GeneratorGrid.Nodes, static n => n.DisplayName == "GENERATOR BREAKER");
        Assert.Contains(projected.GeneratorGrid.Nodes, static n => n.DisplayName == "SYNCHRONIZATION CHECK");
    }

    [Fact]
    public void DesktopProjection_DistinguishesUnavailableMeasuredShaftFromFiniteModelShaft()
    {
        var snapshot = new DesktopIntegratedOperationsInitialConditionFactory()
            .CreateRuntimeEngine()
            .CreatePresentationSnapshot(ControlRoomRunState.Paused);
        var generator = Assert.Single(snapshot.Electrical.Generators);
        var rotor = Assert.Single(snapshot.TurbineSecondary.Rotors);

        Assert.InRange(generator.RequestedElectricalPower.NumericValue ?? double.NaN, 4.9d, 5.1d);
        Assert.InRange(generator.ElectricalOutput.NumericValue ?? double.NaN, 4.5d, 5.5d);
        Assert.Null(snapshot.TurbineSecondary.TotalTurbineShaftPower.NumericValue);
        Assert.Equal(ControlRoomVisualState.Unavailable, snapshot.TurbineSecondary.TotalTurbineShaftPower.State);
        Assert.True(double.IsFinite(rotor.ShaftPower.NumericValue ?? double.NaN));

        var diagnostic = ControlRoomSubsystemSchematicProjector.BuildGeneratorPowerPathDiagnostic(snapshot);
        Assert.Contains("TURBINE SHAFT POWER MEASUREMENT UNAVAILABLE", diagnostic, StringComparison.Ordinal);
        Assert.Contains("MODEL rotor shaft", diagnostic, StringComparison.Ordinal);
        Assert.Contains("inspect MAIN STEAM", diagnostic, StringComparison.Ordinal);
        Assert.Contains("amber SHAFT color denotes mechanical energy, not a warning", diagnostic, StringComparison.Ordinal);
    }

    [Fact]
    public void GeneratorPowerPathDiagnostic_ExplainsOpenBreakerAndRequiredOperatorSequence()
    {
        var snapshot = new GridSynchronizationInitialConditionFactory()
            .CreateRuntimeEngine()
            .CreatePresentationSnapshot(ControlRoomRunState.Paused);

        var diagnostic = ControlRoomSubsystemSchematicProjector.BuildGeneratorPowerPathDiagnostic(snapshot);

        Assert.Contains("0 MWe IS EXPECTED", diagnostic, StringComparison.Ordinal);
        Assert.Contains("breaker OPEN", diagnostic, StringComparison.Ordinal);
        Assert.Contains("CLOSE BREAKER", diagnostic, StringComparison.Ordinal);
        Assert.Contains("LOAD RAISE", diagnostic, StringComparison.Ordinal);
    }

    [Fact]
    public void InstrumentationProtectionSchematic_MakesProtectionOverridePriorityExplicit()
    {
        var snapshot = new DesktopIntegratedOperationsInitialConditionFactory()
            .CreateRuntimeEngine()
            .CreatePresentationSnapshot(ControlRoomRunState.Paused);

        var schematic = ControlRoomSubsystemSchematicProjector.Project(snapshot).InstrumentationProtection;

        Assert.Contains(schematic.Connections, static connection =>
            connection.Kind == ControlRoomSubsystemSchematicConnectionKind.ProtectionOverride);
        Assert.Contains(schematic.Connections, static connection =>
            connection.Kind == ControlRoomSubsystemSchematicConnectionKind.MeasurementSignal);
        Assert.Contains(schematic.Connections, static connection =>
            connection.Kind == ControlRoomSubsystemSchematicConnectionKind.AlarmSignal);
    }
}
