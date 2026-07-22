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
