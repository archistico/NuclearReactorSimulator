using System.Text.Json;
using System.Text.Json.Nodes;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Analysis;
using NuclearReactorSimulator.Application.Scenarios.Recording;
using NuclearReactorSimulator.Infrastructure.Scenarios.Analysis;
using Xunit;

namespace NuclearReactorSimulator.Infrastructure.Tests.Scenarios.Analysis;

public sealed class JsonPostIncidentAnalysisSerializerTests
{
    [Fact]
    public void SchemaV1_RoundTripsDeterministicAnalysisReport()
    {
        var serializer = new JsonPostIncidentAnalysisSerializer();
        var original = CreateReport();

        var json = serializer.Serialize(original);
        var restored = serializer.Deserialize(json);

        Assert.Equal(PostIncidentAnalysisReport.CurrentSchemaVersion, restored.SchemaVersion);
        Assert.Equal(original.ScenarioId, restored.ScenarioId);
        Assert.Equal(original.InitialCondition, restored.InitialCondition);
        Assert.Equal(original.AnchorEventSequence, restored.AnchorEventSequence);
        Assert.Equal(original.AnchorKind, restored.AnchorKind);
        Assert.Equal(original.Timeline.ToArray(), restored.Timeline.ToArray());
        Assert.Equal(original.Trends.ToArray(), restored.Trends.ToArray());
        Assert.Equal(original.Metrics, restored.Metrics);
        Assert.Equal(original.PrecedingCheckpointId, restored.PrecedingCheckpointId);
        var operatorCommand = Assert.Single(restored.Timeline, static item => item.OperatorCommand is not null).OperatorCommand!;
        Assert.Equal(ControlRoomCommandKind.TurbineControlValveManualDemandSet, operatorCommand.Kind);
        Assert.True(operatorCommand.TargetKind.HasValue);
        Assert.Equal(ControlRoomCommandTargetKind.Valve, operatorCommand.TargetKind.GetValueOrDefault());
        Assert.True(operatorCommand.NumericValue.HasValue);
        Assert.Equal(37.5d, operatorCommand.NumericValue.GetValueOrDefault());
        Assert.Contains("\"numericValue\": 37.5", json, StringComparison.Ordinal);
    }


    [Fact]
    public void TruncatedJson_IsNormalizedToInvalidDataException()
    {
        var serializer = new JsonPostIncidentAnalysisSerializer();
        const string content = "{ \"schemaVersion\": 1, \"scenarioId\": \"broken\",";

        var exception = Assert.Throws<InvalidDataException>(() => serializer.Deserialize(content));

        Assert.IsType<JsonException>(exception.InnerException);
    }

    [Fact]
    public void UnknownStringEnum_IsNormalizedToInvalidDataException()
    {
        var serializer = new JsonPostIncidentAnalysisSerializer();
        var document = JsonNode.Parse(serializer.Serialize(CreateReport()))!.AsObject();
        document["timeline"]!.AsArray()[1]!.AsObject()["relation"] = "Frobnicare";

        var exception = Assert.Throws<InvalidDataException>(() => serializer.Deserialize(document.ToJsonString()));

        Assert.IsType<JsonException>(exception.InnerException);
    }


    [Fact]
    public void UndefinedOperatorCommandKind_FailsAtPostIncidentAdapterBoundary()
    {
        var serializer = new JsonPostIncidentAnalysisSerializer();
        var document = JsonNode.Parse(serializer.Serialize(CreateReport()))!.AsObject();
        document["timeline"]!.AsArray()[1]!.AsObject()["operatorCommand"]!.AsObject()["kind"] = 9999;

        var exception = Assert.Throws<InvalidDataException>(() => serializer.Deserialize(document.ToJsonString()));

        Assert.Contains(".kind", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void UnknownSchema_FailsClosed()
    {
        var serializer = new JsonPostIncidentAnalysisSerializer();
        var json = serializer.Serialize(CreateReport())
            .Replace("\"schemaVersion\": 1", "\"schemaVersion\": 99", StringComparison.Ordinal);

        Assert.Throws<NotSupportedException>(() => serializer.Deserialize(json));
    }

    private static PostIncidentAnalysisReport CreateReport()
        => new(
            "scenario",
            new InitialConditionReference("ic", 1),
            PostIncidentAnalysisReport.CurrentSchemaVersion,
            2,
            10,
            PostIncidentAnalysisAnchorKind.FaultTransition,
            "fault-a",
            "Active",
            8,
            12,
            new[]
            {
                new PostIncidentAnalysisTimelineEntry(
                    2, 10, 0, PostIncidentTemporalRelation.Anchor,
                    ScenarioRecordingEventKind.FaultTransition, "fault-a", "Active"),
                new PostIncidentAnalysisTimelineEntry(
                    3,
                    11,
                    1,
                    PostIncidentTemporalRelation.AfterAnchor,
                    ScenarioRecordingEventKind.OperatorAction,
                    "control-valve-a",
                    ControlRoomCommandKind.TurbineControlValveManualDemandSet.ToString(),
                    new ControlRoomCommand(
                        ControlRoomCommandKind.TurbineControlValveManualDemandSet,
                        "control-valve-a",
                        ControlRoomCommandTargetKind.Valve,
                        37.5d)),
            },
            new[]
            {
                PostIncidentTrendSample.FromValues(8, -2, null, null, null, null, null, null, null, null, 0, 0, 0, 0, false, false, false),
                PostIncidentTrendSample.FromValues(9, -1, null, null, null, null, null, null, null, null, 0, 0, 0, 0, false, false, false),
                PostIncidentTrendSample.FromValues(10, 0, null, null, null, null, null, null, null, null, 1, 1, 1, 1, false, false, false),
                PostIncidentTrendSample.FromValues(11, 1, null, null, null, null, null, null, null, null, 1, 2, 1, 1, true, false, false),
                PostIncidentTrendSample.FromValues(12, 2, null, null, null, null, null, null, null, null, 0, 1, 0, 0, true, false, false),
            },
            PostIncidentStateSummary.FromValues(8, 0, 0, 0, false, false, false, 0),
            PostIncidentStateSummary.FromValues(10, 1, 1, 1, false, false, false, 1),
            PostIncidentStateSummary.FromValues(12, 0, 1, 0, true, false, false, 0),
            new PostIncidentResponseMetrics(1, 2, 3, 4, 1, 2, 1, 1),
            "checkpoint-8");
}
