using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.ControlRoom.Hmi;
using NuclearReactorSimulator.Application.ControlRoom.OperatorComputer;
using NuclearReactorSimulator.Application.Scenarios.Operations;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.ControlRoom.OperatorComputer;

public sealed class OperatorComputerM10951CommandConsequenceCatalogTests
{
    [Fact]
    public void Catalog_CoversEveryCurrentCommandKindExactlyOnceAndDeterministically()
    {
        var definitions = OperatorComputerCommandConsequenceCatalog.Definitions;
        var kinds = Enum.GetValues<ControlRoomCommandKind>();

        Assert.Equal(kinds.Length, definitions.Count);
        Assert.Equal(kinds, definitions.Select(static item => item.CommandKind).ToArray());
        Assert.Equal(definitions.Count, definitions.Select(static item => item.CommandKind).Distinct().Count());
        Assert.All(definitions, static definition =>
        {
            Assert.False(string.IsNullOrWhiteSpace(definition.DirectIntent));
            Assert.DoesNotContain("SUCCESS", definition.DirectIntent, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("WILL ", definition.DirectIntent, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public void IntegratedCommandConsole_ProjectsOnlyAuthoredConsequenceMapsForEveryExposedCommand()
    {
        var snapshot = new PowerManoeuvringInitialConditionFactory()
            .CreateRuntimeEngine()
            .CreatePresentationSnapshot(ControlRoomRunState.Paused);
        var console = OperatorComputerCommandConsoleProjector.Project(snapshot);

        Assert.NotEmpty(console.Commands);
        foreach (var row in console.Commands)
        {
            var projection = OperatorComputerCommandConsequenceCatalog.Project(row.Command);
            Assert.Equal(OperatorComputerCommandConsequenceMappingStatus.Authored, projection.MappingStatus);
            Assert.True(projection.HasAuthoredMap);
            Assert.NotEqual("NO AUTHORED CONSEQUENCE MAP", projection.DirectIntentText);
        }
    }

    [Fact]
    public void Catalog_ExplicitlyCoversCommandFamiliesNotYetExposedByTheM104Console()
    {
        var commands = new[]
        {
            new ControlRoomCommand(ControlRoomCommandKind.TurbineValveOpen, "stop", ControlRoomCommandTargetKind.Valve),
            new ControlRoomCommand(ControlRoomCommandKind.TurbineValveClose, "admission", ControlRoomCommandTargetKind.Valve),
            new ControlRoomCommand(ControlRoomCommandKind.TurbineControlValveManualMode, "control", ControlRoomCommandTargetKind.Valve),
            new ControlRoomCommand(ControlRoomCommandKind.TurbineControlValveAutomaticMode, "control", ControlRoomCommandTargetKind.Valve),
            new ControlRoomCommand(ControlRoomCommandKind.TurbineControlValveManualDemandSet, "control", ControlRoomCommandTargetKind.Valve, 37.5d),
        };

        Assert.All(commands, static command =>
        {
            var projection = OperatorComputerCommandConsequenceCatalog.Project(command);
            Assert.Equal(OperatorComputerCommandConsequenceMappingStatus.Authored, projection.MappingStatus);
            Assert.NotNull(projection.CanonicalCommandTarget);
        });
        Assert.Contains("37.5%", OperatorComputerCommandConsequenceCatalog.Project(commands[^1]).DirectIntentText, StringComparison.Ordinal);
    }

    [Fact]
    public void InvalidOrFutureCommandShape_FailsClosedAsExplicitlyUnmappedInsteadOfInventingCausality()
    {
        var wrongTarget = OperatorComputerCommandConsequenceCatalog.Project(
            new ControlRoomCommand(ControlRoomCommandKind.GeneratorLoadRaise, "generator-1", ControlRoomCommandTargetKind.Pump));
        var missingTarget = OperatorComputerCommandConsequenceCatalog.Project(
            new ControlRoomCommand(ControlRoomCommandKind.MainCirculationPumpStart));
        var unknownKind = OperatorComputerCommandConsequenceCatalog.Project(
            new ControlRoomCommand((ControlRoomCommandKind)999));

        Assert.Equal(OperatorComputerCommandConsequenceMappingStatus.ExplicitlyUnmapped, wrongTarget.MappingStatus);
        Assert.Equal(OperatorComputerCommandConsequenceMappingStatus.ExplicitlyUnmapped, missingTarget.MappingStatus);
        Assert.Equal(OperatorComputerCommandConsequenceMappingStatus.ExplicitlyUnmapped, unknownKind.MappingStatus);
        Assert.All(new[] { wrongTarget, missingTarget, unknownKind }, static projection =>
        {
            Assert.False(projection.HasAuthoredMap);
            Assert.Equal("NO AUTHORED CONSEQUENCE MAP", projection.DirectIntentText);
            Assert.Empty(projection.ExpectedInfluences);
            Assert.Empty(projection.MonitorTargets);
        });
    }

    [Fact]
    public void AllAuthoredReferences_ResolveToPublishedSnapshotPathsOrCanonicalWholePlantMimicElements()
    {
        var snapshot = new PowerManoeuvringInitialConditionFactory()
            .CreateRuntimeEngine()
            .CreatePresentationSnapshot(ControlRoomRunState.Paused);
        var mimicIds = ControlRoomPlantMimicProjector.Project(snapshot)
            .Elements
            .Select(static element => element.ElementId)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var definition in OperatorComputerCommandConsequenceCatalog.Definitions)
        {
            var references = definition.ExpectedInfluences.Select(static item => item.Target)
                .Concat(definition.PermissiveReferences)
                .Concat(definition.MonitorTargets.Select(static item => item.Target));

            foreach (var reference in references)
            {
                if (reference.Kind == OperatorComputerCommandConsequenceReferenceKind.PlantMimicElement)
                {
                    Assert.Contains(reference.Id, mimicIds);
                    continue;
                }

                Assert.True(
                    ResolvesPublishedPath(typeof(ControlRoomSnapshot), reference.Id),
                    $"Published reference '{reference.Id}' from command '{definition.CommandKind}' does not resolve from ControlRoomSnapshot.");
            }
        }
    }

    [Fact]
    public void SameTypedCommand_ProjectsDeterministicConsequenceContentAndOrdering()
    {
        var command = new ControlRoomCommand(
            ControlRoomCommandKind.TurbineControlValveManualDemandSet,
            "control",
            ControlRoomCommandTargetKind.Valve,
            37.5d);

        var first = OperatorComputerCommandConsequenceCatalog.Project(command);
        var second = OperatorComputerCommandConsequenceCatalog.Project(command);

        Assert.Equal(first.MappingStatus, second.MappingStatus);
        Assert.Equal(first.DirectIntentText, second.DirectIntentText);
        Assert.Equal(first.ExpectedInfluences.ToArray(), second.ExpectedInfluences.ToArray());
        Assert.Equal(first.PermissiveReferences.ToArray(), second.PermissiveReferences.ToArray());
        Assert.Equal(first.MonitorTargets.ToArray(), second.MonitorTargets.ToArray());
        Assert.Equal(first.MappingNote, second.MappingNote);
    }

    [Fact]
    public void ConsequenceProjection_IsQualitativeAndKeepsExpectedInfluenceSeparateFromMonitoredEvidence()
    {
        var projection = OperatorComputerCommandConsequenceCatalog.Project(
            new ControlRoomCommand(ControlRoomCommandKind.GeneratorLoadRaise, "generator", ControlRoomCommandTargetKind.Generator));

        Assert.True(projection.HasAuthoredMap);
        Assert.Contains(projection.ExpectedInfluences, static item => item.Relation == OperatorComputerCommandConsequenceRelation.IncreasesExpectedDemandOn);
        Assert.Contains(projection.ExpectedInfluences, static item => item.Relation == OperatorComputerCommandConsequenceRelation.ProtectionMayOverride);
        Assert.Contains(projection.MonitorTargets, static item => item.Provenance == OperatorComputerInformationProvenance.Measured);
        Assert.DoesNotContain(projection.ExpectedInfluences, static item => item.Explanation.Contains("will produce", StringComparison.OrdinalIgnoreCase));
        Assert.Equal("AUTHORED QUALITATIVE MAP · NOT A NUMERICAL PREDICTION", projection.MappingNote);
    }

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

        var enumerable = type.GetInterfaces()
            .Append(type)
            .FirstOrDefault(static candidate =>
                candidate.IsGenericType
                && candidate.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        return enumerable?.GetGenericArguments()[0] ?? type;
    }
}
