using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Faults;
using NuclearReactorSimulator.Infrastructure.Scenarios;
using Xunit;

namespace NuclearReactorSimulator.Infrastructure.Tests.Scenarios;

public sealed class JsonScenarioDefinitionSerializerTests
{
    [Fact]
    public void SerializeDeserialize_RoundTripsCanonicalVersionedScenarioDocumentIncludingFaultSchedule()
    {
        var serializer = new JsonScenarioDefinitionSerializer();
        var scenario = new ScenarioDefinition(
            "startup-training",
            "Startup training",
            "Reference startup training flow",
            new InitialConditionReference("cold-shutdown", 3),
            new[]
            {
                new ScenarioObjectiveDefinition("objective-1", "Prepare", "Complete the preparation objective."),
            },
            new[] { ControlRoomCommandKind.ReactorScram, ControlRoomCommandKind.MainCirculationPumpStart },
            new[]
            {
                new ScenarioFaultDefinition(
                    "fault-1",
                    "hydraulic.pump-trip",
                    "mcp-1",
                    ScenarioFaultTriggerDefinition.AtLogicalStep(10),
                    ScenarioFaultTriggerDefinition.WhenPlantCondition("pump-recovered"),
                    new Dictionary<string, string> { ["severity"] = "1.0" }),
            });

        var json = serializer.Serialize(scenario);
        var loaded = serializer.Deserialize(json);

        Assert.Contains("\"schemaVersion\": 2", json);
        Assert.Equal(scenario.ScenarioId, loaded.ScenarioId);
        Assert.Equal(scenario.InitialCondition, loaded.InitialCondition);
        Assert.Equal(scenario.Objectives, loaded.Objectives);
        Assert.True(loaded.AllowedOperatorActions.SetEquals(scenario.AllowedOperatorActions));
        var fault = Assert.Single(loaded.Faults);
        Assert.Equal("fault-1", fault.FaultId);
        Assert.Equal("hydraulic.pump-trip", fault.FaultTypeId);
        Assert.Equal("mcp-1", fault.TargetId);
        Assert.Equal(ScenarioFaultTriggerDefinition.AtLogicalStep(10), fault.Activation);
        Assert.Equal(ScenarioFaultTriggerDefinition.WhenPlantCondition("pump-recovered"), fault.Deactivation);
        Assert.Equal("1.0", fault.Parameters["severity"]);
    }

    [Fact]
    public void Deserialize_MigratesLegacyV0WithoutChangingInitialConditionVersionOrInventingFaults()
    {
        const string legacy = """
        {
          "schemaVersion": 0,
          "id": "legacy-training",
          "name": "Legacy training",
          "description": "Legacy schema document",
          "initialConditionId": "reference-state",
          "initialConditionVersion": 7,
          "objectives": ["Hold stable power"],
          "allowedOperatorActions": ["ReactorScram"]
        }
        """;
        var serializer = new JsonScenarioDefinitionSerializer();

        var migrated = serializer.Deserialize(legacy);

        Assert.Equal("legacy-training", migrated.ScenarioId);
        Assert.Equal(new InitialConditionReference("reference-state", 7), migrated.InitialCondition);
        Assert.Equal("objective-001", Assert.Single(migrated.Objectives).ObjectiveId);
        Assert.Contains(ControlRoomCommandKind.ReactorScram, migrated.AllowedOperatorActions);
        Assert.Empty(migrated.Faults);
    }

    [Fact]
    public void Deserialize_MigratesV1WithoutInventingFaults()
    {
        const string version1 = """
        {
          "schemaVersion": 1,
          "scenarioId": "v1-training",
          "title": "V1 training",
          "description": "Pre-fault schema",
          "initialCondition": {
            "id": "reference-state",
            "version": 5
          },
          "objectives": [],
          "allowedOperatorActions": ["ReactorScram"]
        }
        """;
        var serializer = new JsonScenarioDefinitionSerializer();

        var migrated = serializer.Deserialize(version1);

        Assert.Equal("v1-training", migrated.ScenarioId);
        Assert.Equal(new InitialConditionReference("reference-state", 5), migrated.InitialCondition);
        Assert.Empty(migrated.Faults);
    }

    [Fact]
    public void Deserialize_RejectsUnknownFutureSchemaVersion()
    {
        const string future = """
        {
          "schemaVersion": 99
        }
        """;
        var serializer = new JsonScenarioDefinitionSerializer();

        Assert.Throws<NotSupportedException>(() => serializer.Deserialize(future));
    }
}
