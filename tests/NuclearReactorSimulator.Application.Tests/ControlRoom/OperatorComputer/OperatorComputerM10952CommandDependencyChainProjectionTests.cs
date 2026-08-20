using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.ControlRoom.Hmi;
using NuclearReactorSimulator.Application.ControlRoom.OperatorComputer;
using NuclearReactorSimulator.Application.Scenarios.Operations;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.ControlRoom.OperatorComputer;

public sealed class OperatorComputerM10952CommandDependencyChainProjectionTests
{
    [Fact]
    public void EveryCurrentCommandKind_ProjectsAnAuthoredBoundedChainForItsCataloguedTargetShape()
    {
        foreach (var definition in OperatorComputerCommandConsequenceCatalog.Definitions)
        {
            var command = Representative(definition);
            var chain = OperatorComputerCommandDependencyChainCatalog.Project(command);

            Assert.True(chain.HasAuthoredChain, $"{definition.CommandKind} did not project an authored chain.");
            Assert.NotEmpty(chain.Steps);
            Assert.Equal(OperatorComputerCommandDependencyStepKind.CommandIntent, chain.Steps[0].Kind);
            Assert.Contains(chain.Steps, static step => step.Kind == OperatorComputerCommandDependencyStepKind.ControlOrActuatorState);
            Assert.Contains(chain.Steps, static step => step.Kind == OperatorComputerCommandDependencyStepKind.MeasurementOrModelObservation);
            Assert.Equal(Enumerable.Range(1, chain.Steps.Count).ToArray(), chain.Steps.Select(static step => step.Sequence).ToArray());
            Assert.All(chain.Steps, static step => Assert.False(string.IsNullOrWhiteSpace(step.Explanation)));
            Assert.Equal("AUTHORED BOUNDED DEPENDENCY CHAIN · PRESENTATION ONLY · NO AUTOMATIC GRAPH TRAVERSAL", chain.MappingNote);
        }
    }

    [Fact]
    public void AllStaticChainReferences_ResolveToCurrentMimicConnectionsElementsOrPublishedSnapshotPaths()
    {
        var snapshot = new PowerManoeuvringInitialConditionFactory()
            .CreateRuntimeEngine()
            .CreatePresentationSnapshot(ControlRoomRunState.Paused);
        var mimic = ControlRoomPlantMimicProjector.Project(snapshot);
        var elementIds = mimic.Elements.Select(static item => item.ElementId).ToHashSet(StringComparer.Ordinal);
        var connectionIds = mimic.Connections.Select(static item => item.ConnectionId).ToHashSet(StringComparer.Ordinal);

        foreach (var definition in OperatorComputerCommandConsequenceCatalog.Definitions)
        {
            var command = Representative(definition);
            var chain = OperatorComputerCommandDependencyChainCatalog.Project(command);

            foreach (var step in chain.Steps)
            {
                if (step.Reference is null)
                {
                    continue;
                }

                switch (step.Reference.Kind)
                {
                    case OperatorComputerCommandConsequenceReferenceKind.CommandTarget:
                        Assert.Equal(command.TargetId, step.Reference.Id);
                        break;
                    case OperatorComputerCommandConsequenceReferenceKind.PlantMimicElement:
                        Assert.Contains(step.Reference.Id, elementIds);
                        break;
                    case OperatorComputerCommandConsequenceReferenceKind.PlantMimicConnection:
                        Assert.Contains(step.Reference.Id, connectionIds);
                        break;
                    case OperatorComputerCommandConsequenceReferenceKind.PublishedState:
                        Assert.True(ResolvesPublishedPath(typeof(ControlRoomSnapshot), step.Reference.Id), step.Reference.Id);
                        break;
                    default:
                        throw new InvalidOperationException($"Unhandled reference kind {step.Reference.Kind}.");
                }
            }
        }
    }

    [Fact]
    public void UnknownOrInvalidCommandShape_FailsClosedWithoutPartialChain()
    {
        var invalid = OperatorComputerCommandDependencyChainCatalog.Project(
            new ControlRoomCommand(ControlRoomCommandKind.GeneratorLoadRaise, "generator", ControlRoomCommandTargetKind.Pump));
        var unknown = OperatorComputerCommandDependencyChainCatalog.Project(new ControlRoomCommand((ControlRoomCommandKind)999));

        Assert.All(new[] { invalid, unknown }, static chain =>
        {
            Assert.False(chain.HasAuthoredChain);
            Assert.Empty(chain.Steps);
            Assert.Contains("NO AUTHORED DEPENDENCY CHAIN", chain.MappingNote, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void SameCommand_ProjectsIdenticalOrderedChainWithoutRuntimeSideEffects()
    {
        var command = new ControlRoomCommand(ControlRoomCommandKind.GeneratorLoadRaise, "generator", ControlRoomCommandTargetKind.Generator);
        var first = OperatorComputerCommandDependencyChainCatalog.Project(command);
        var second = OperatorComputerCommandDependencyChainCatalog.Project(command);

        Assert.Equal(first.MappingStatus, second.MappingStatus);
        Assert.Equal(first.Steps.ToArray(), second.Steps.ToArray());
        Assert.Equal(first.MappingNote, second.MappingNote);
        Assert.Contains(first.Steps, static step => step.Reference is { Kind: OperatorComputerCommandConsequenceReferenceKind.PlantMimicConnection, Id: "turbine-generator" });
        Assert.Contains(first.Steps, static step => step.Reference is { Kind: OperatorComputerCommandConsequenceReferenceKind.PlantMimicConnection, Id: "generator-grid" });
    }

    private static ControlRoomCommand Representative(OperatorComputerCommandConsequenceDefinition definition)
        => definition.SupportedTargetKinds.Count == 0
            ? new ControlRoomCommand(definition.CommandKind)
            : new ControlRoomCommand(definition.CommandKind, "test-target", definition.SupportedTargetKinds[0],
                definition.CommandKind == ControlRoomCommandKind.TurbineControlValveManualDemandSet ? 37.5d : null);

    private static bool ResolvesPublishedPath(Type rootType, string path)
    {
        var current = rootType;
        foreach (var segment in path.Split('.', StringSplitOptions.RemoveEmptyEntries))
        {
            var property = current.GetProperty(segment);
            if (property is null)
            {
                return false;
            }
            current = ResolvePropertyType(property.PropertyType);
        }
        return true;
    }

    private static Type ResolvePropertyType(Type type)
    {
        if (type.IsArray)
        {
            return type.GetElementType()!;
        }
        var enumerable = type.GetInterfaces().Append(type).FirstOrDefault(static candidate =>
            candidate.IsGenericType && candidate.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        return enumerable?.GetGenericArguments()[0] ?? type;
    }
}
