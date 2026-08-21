using System.Text.Json;
using System.Text.Json.Nodes;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Recording;
using NuclearReactorSimulator.Infrastructure.Scenarios.Recording;
using Xunit;

namespace NuclearReactorSimulator.Infrastructure.Tests.Scenarios.Recording;

public sealed class JsonScenarioCheckpointSerializerTests
{
    [Fact]
    public void VersionOneCheckpoint_RoundTripsExactIdentityAndFingerprint()
    {
        var serializer = new JsonScenarioCheckpointSerializer();
        var checkpoint = CreateCheckpoint();

        var json = serializer.Serialize(checkpoint);
        var restored = serializer.Deserialize(json);

        Assert.Equal(checkpoint, restored);
        Assert.Contains("\"schemaVersion\": 1", json);
    }


    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void BlankCheckpoint_IsNormalizedToInvalidDataException(string content)
    {
        var serializer = new JsonScenarioCheckpointSerializer();

        var exception = Assert.Throws<InvalidDataException>(() => serializer.Deserialize(content));

        Assert.IsAssignableFrom<ArgumentException>(exception.InnerException);
    }

    [Fact]
    public void TruncatedCheckpointJson_IsNormalizedToInvalidDataException()
    {
        var serializer = new JsonScenarioCheckpointSerializer();
        const string content = "{ \"schemaVersion\": 1, \"checkpointId\": \"broken\",";

        var exception = Assert.Throws<InvalidDataException>(() => serializer.Deserialize(content));

        Assert.IsType<JsonException>(exception.InnerException);
    }

    [Fact]
    public void StructurallyInvalidCheckpoint_IsNormalizedToInvalidDataException()
    {
        var serializer = new JsonScenarioCheckpointSerializer();
        var document = JsonNode.Parse(serializer.Serialize(CreateCheckpoint()))!.AsObject();
        document["logicalStep"] = -1;

        var exception = Assert.Throws<InvalidDataException>(() => serializer.Deserialize(document.ToJsonString()));

        Assert.IsType<ArgumentOutOfRangeException>(exception.InnerException);
    }

    [Fact]
    public void UnsupportedCheckpointSchema_FailsClosed()
    {
        var serializer = new JsonScenarioCheckpointSerializer();
        var json = serializer.Serialize(CreateCheckpoint()).Replace(
            "\"schemaVersion\": 1",
            "\"schemaVersion\": 99",
            StringComparison.Ordinal);

        Assert.Throws<NotSupportedException>(() => serializer.Deserialize(json));
    }

    private static ScenarioCheckpoint CreateCheckpoint()
        => new(
            "checkpoint-a",
            ScenarioCheckpoint.CurrentSchemaVersion,
            "scenario-a",
            new InitialConditionReference("initial-a", 3),
            logicalStep: 42,
            lastAppliedOperatorActionSequence: 7,
            fingerprintAlgorithmId: ControlRoomSnapshotFingerprint.AlgorithmId,
            snapshotFingerprint: new string('a', 64));
}
