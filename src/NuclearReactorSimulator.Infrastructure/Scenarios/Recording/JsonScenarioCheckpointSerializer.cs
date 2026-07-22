using System.Text.Json;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Recording;

namespace NuclearReactorSimulator.Infrastructure.Scenarios.Recording;

/// <summary>JSON persistence adapter for M9.1 replay-backed checkpoint schema v1.</summary>
public sealed class JsonScenarioCheckpointSerializer : IScenarioCheckpointSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
    };

    public string Serialize(ScenarioCheckpoint checkpoint)
    {
        ArgumentNullException.ThrowIfNull(checkpoint);
        if (checkpoint.SchemaVersion != ScenarioCheckpoint.CurrentSchemaVersion)
        {
            throw new NotSupportedException($"Checkpoint schema version {checkpoint.SchemaVersion} cannot be serialized by this adapter.");
        }

        return JsonSerializer.Serialize(new CheckpointDocument
        {
            SchemaVersion = checkpoint.SchemaVersion,
            CheckpointId = checkpoint.CheckpointId,
            ScenarioId = checkpoint.ScenarioId,
            InitialConditionId = checkpoint.InitialCondition.InitialConditionId,
            InitialConditionVersion = checkpoint.InitialCondition.Version,
            LogicalStep = checkpoint.LogicalStep,
            LastAppliedOperatorActionSequence = checkpoint.LastAppliedOperatorActionSequence,
            FingerprintAlgorithmId = checkpoint.FingerprintAlgorithmId,
            SnapshotFingerprint = checkpoint.SnapshotFingerprint,
        }, Options);
    }

    public ScenarioCheckpoint Deserialize(string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        var document = JsonSerializer.Deserialize<CheckpointDocument>(content, Options)
            ?? throw new InvalidDataException("Checkpoint document could not be deserialized.");
        if (document.SchemaVersion != ScenarioCheckpoint.CurrentSchemaVersion)
        {
            throw new NotSupportedException(
                $"Checkpoint schema version {document.SchemaVersion} is not supported. Current version is {ScenarioCheckpoint.CurrentSchemaVersion}.");
        }
        ValidateText(document.CheckpointId, "checkpointId");
        ValidateText(document.ScenarioId, "scenarioId");
        ValidateText(document.InitialConditionId, "initialConditionId");
        ValidateText(document.FingerprintAlgorithmId, "fingerprintAlgorithmId");
        ValidateText(document.SnapshotFingerprint, "snapshotFingerprint");

        return new ScenarioCheckpoint(
            document.CheckpointId!,
            document.SchemaVersion,
            document.ScenarioId!,
            new InitialConditionReference(document.InitialConditionId!, document.InitialConditionVersion),
            document.LogicalStep,
            document.LastAppliedOperatorActionSequence,
            document.FingerprintAlgorithmId!,
            document.SnapshotFingerprint!);
    }

    private static void ValidateText(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidDataException($"Checkpoint document field '{fieldName}' is required.");
        }
    }

    private sealed class CheckpointDocument
    {
        public int SchemaVersion { get; set; }
        public string? CheckpointId { get; set; }
        public string? ScenarioId { get; set; }
        public string? InitialConditionId { get; set; }
        public int InitialConditionVersion { get; set; }
        public long LogicalStep { get; set; }
        public long LastAppliedOperatorActionSequence { get; set; }
        public string? FingerprintAlgorithmId { get; set; }
        public string? SnapshotFingerprint { get; set; }
    }
}
