using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Infrastructure.Scenarios;
using Xunit;

namespace NuclearReactorSimulator.Infrastructure.Tests.Scenarios;

public sealed class JsonScenarioDefinitionSerializerTests
{
    [Fact]
    public void SerializeDeserialize_RoundTripsCanonicalVersionedScenarioDocument()
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
            new[] { ControlRoomCommandKind.ReactorScram, ControlRoomCommandKind.MainCirculationPumpStart });

        var json = serializer.Serialize(scenario);
        var loaded = serializer.Deserialize(json);

        Assert.Contains("\"schemaVersion\": 1", json, StringComparison.Ordinal);
        Assert.Equal(scenario.ScenarioId, loaded.ScenarioId);
        Assert.Equal(scenario.InitialCondition, loaded.InitialCondition);
        Assert.Equal(scenario.Objectives, loaded.Objectives);
        Assert.True(loaded.AllowedOperatorActions.SetEquals(scenario.AllowedOperatorActions));
    }

    [Fact]
    public void Deserialize_MigratesLegacyV0WithoutChangingInitialConditionVersion()
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
