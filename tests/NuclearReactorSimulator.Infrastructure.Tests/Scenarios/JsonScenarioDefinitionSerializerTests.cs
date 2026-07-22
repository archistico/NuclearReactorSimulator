using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Faults;
using NuclearReactorSimulator.Application.Scenarios.Historical;
using NuclearReactorSimulator.Infrastructure.Scenarios;
using Xunit;

namespace NuclearReactorSimulator.Infrastructure.Tests.Scenarios;

public sealed class JsonScenarioDefinitionSerializerTests
{
    [Fact]
    public void SerializeDeserialize_RoundTripsCanonicalVersionedScenarioDocumentIncludingFaultScheduleAndHistoricalContext()
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
            },
            new HistoricalScenarioContextDefinition(
                "Synthetic historical subject",
                "Historical inspiration only; exact reconstruction is not claimed.",
                new[]
                {
                    new HistoricalSourceReference("source-1", "Synthetic citation", "section 1"),
                },
                new[]
                {
                    new HistoricalScenarioClaimDefinition(
                        "fact-1",
                        HistoricalScenarioClaimKind.DocumentedFact,
                        "Synthetic documented statement.",
                        new[] { "source-1" }),
                    new HistoricalScenarioClaimDefinition(
                        "approximation-1",
                        HistoricalScenarioClaimKind.EducationalApproximation,
                        "Synthetic educational approximation.",
                        rationale: "Reduced-model test boundary."),
                },
                new[] { HistoricalModelCapabilityIds.DeterministicFullPlantRuntime },
                new[] { "Not an exact historical reconstruction." }));

        var json = serializer.Serialize(scenario);
        var loaded = serializer.Deserialize(json);

        Assert.Contains("\"schemaVersion\": 3", json);
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
        var historical = Assert.IsType<HistoricalScenarioContextDefinition>(loaded.HistoricalContext);
        Assert.Equal("Synthetic historical subject", historical.HistoricalSubject);
        Assert.Equal(2, historical.Claims.Count);
        Assert.Equal(HistoricalScenarioClaimKind.DocumentedFact, historical.Claims[0].Kind);
        Assert.Equal(HistoricalModelCapabilityIds.DeterministicFullPlantRuntime, Assert.Single(historical.RequiredModelCapabilities));
        Assert.Equal("Not an exact historical reconstruction.", Assert.Single(historical.DeliberateNonClaims));
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
    public void Deserialize_MigratesV2WithoutInventingHistoricalContext()
    {
        const string version2 = """
        {
          "schemaVersion": 2,
          "scenarioId": "v2-training",
          "title": "V2 training",
          "description": "Fault-aware pre-historical schema",
          "initialCondition": {
            "id": "reference-state",
            "version": 6
          },
          "objectives": [],
          "allowedOperatorActions": ["ReactorScram"],
          "faults": [
            {
              "faultId": "fault-v2",
              "faultTypeId": "hydraulic.pump-trip",
              "targetId": "mcp-1",
              "parameters": {
                "severity": "1.0"
              },
              "activation": {
                "kind": "LogicalStep",
                "logicalStep": 10
              }
            }
          ]
        }
        """;
        var serializer = new JsonScenarioDefinitionSerializer();

        var migrated = serializer.Deserialize(version2);

        Assert.Equal("v2-training", migrated.ScenarioId);
        Assert.Equal(new InitialConditionReference("reference-state", 6), migrated.InitialCondition);
        var fault = Assert.Single(migrated.Faults);
        Assert.Equal("fault-v2", fault.FaultId);
        Assert.Equal(ScenarioFaultTriggerDefinition.AtLogicalStep(10), fault.Activation);
        Assert.Equal("1.0", fault.Parameters["severity"]);
        Assert.Null(migrated.HistoricalContext);
    }

    [Fact]
    public void Deserialize_V3HistoricalClaimWithoutExplicitKindFailsClosed()
    {
        const string invalid = """
        {
          "schemaVersion": 3,
          "scenarioId": "historical-invalid",
          "title": "Historical invalid",
          "description": "Invalid historical metadata",
          "initialCondition": {
            "id": "reference-state",
            "version": 1
          },
          "objectives": [],
          "allowedOperatorActions": [],
          "faults": [],
          "historicalContext": {
            "historicalSubject": "Synthetic subject",
            "fidelityStatement": "Educational inspiration only.",
            "sources": [
              {
                "sourceId": "source-1",
                "citation": "Synthetic citation"
              }
            ],
            "claims": [
              {
                "claimId": "fact-1",
                "statement": "Synthetic statement.",
                "sourceIds": ["source-1"]
              }
            ],
            "requiredModelCapabilities": ["runtime.deterministic-full-plant"],
            "deliberateNonClaims": ["Not an exact reconstruction."]
          }
        }
        """;
        var serializer = new JsonScenarioDefinitionSerializer();

        Assert.Throws<InvalidDataException>(() => serializer.Deserialize(invalid));
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
