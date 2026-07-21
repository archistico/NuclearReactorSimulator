using System.Text.Json;
using System.Text.Json.Serialization;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios;

namespace NuclearReactorSimulator.Infrastructure.Scenarios;

/// <summary>
/// JSON persistence adapter for M7.1 scenario definitions. Schema version 1 is canonical. The embedded initial-condition
/// reference is always exact-versioned. A deterministic v0-to-v1 migration is retained so persisted definitions can evolve
/// without silently reinterpreting an initial condition.
/// </summary>
public sealed class JsonScenarioDefinitionSerializer : IScenarioDefinitionSerializer
{
    public const int CurrentSchemaVersion = 1;

    private static readonly JsonSerializerOptions SerializerOptions = CreateSerializerOptions();

    public string Serialize(ScenarioDefinition scenario)
    {
        ArgumentNullException.ThrowIfNull(scenario);

        var document = new ScenarioDocumentV1
        {
            SchemaVersion = CurrentSchemaVersion,
            ScenarioId = scenario.ScenarioId,
            Title = scenario.Title,
            Description = scenario.Description,
            InitialCondition = new InitialConditionReferenceDocumentV1
            {
                Id = scenario.InitialCondition.InitialConditionId,
                Version = scenario.InitialCondition.Version,
            },
            Objectives = scenario.Objectives.Select(static objective => new ScenarioObjectiveDocumentV1
            {
                Id = objective.ObjectiveId,
                Title = objective.Title,
                Description = objective.Description,
            }).ToArray(),
            AllowedOperatorActions = scenario.AllowedOperatorActions.OrderBy(static action => action).ToArray(),
        };

        return JsonSerializer.Serialize(document, SerializerOptions);
    }

    public ScenarioDefinition Deserialize(string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        using var parsed = JsonDocument.Parse(content);
        if (!parsed.RootElement.TryGetProperty("schemaVersion", out var schemaVersionElement)
            || !schemaVersionElement.TryGetInt32(out var schemaVersion))
        {
            throw new InvalidDataException("Scenario document must contain an integer schemaVersion.");
        }

        return schemaVersion switch
        {
            0 => MigrateV0(DeserializeDocument<ScenarioDocumentV0>(content)),
            CurrentSchemaVersion => ToDefinition(DeserializeDocument<ScenarioDocumentV1>(content)),
            _ => throw new NotSupportedException(
                $"Scenario schema version {schemaVersion} is not supported. Current version is {CurrentSchemaVersion}."),
        };
    }

    private static ScenarioDefinition MigrateV0(ScenarioDocumentV0 document)
    {
        ValidateText(document.Id, "id");
        ValidateText(document.Name, "name");
        ValidateText(document.Description, "description");
        ValidateText(document.InitialConditionId, "initialConditionId");

        var objectives = (document.Objectives ?? Array.Empty<string>())
            .Select((text, index) =>
            {
                ValidateText(text, $"objectives[{index}]");
                return new ScenarioObjectiveDefinition($"objective-{index + 1:D3}", text, text);
            })
            .ToArray();

        return new ScenarioDefinition(
            document.Id!,
            document.Name!,
            document.Description!,
            new InitialConditionReference(document.InitialConditionId!, document.InitialConditionVersion),
            objectives,
            document.AllowedOperatorActions ?? Array.Empty<ControlRoomCommandKind>());
    }

    private static ScenarioDefinition ToDefinition(ScenarioDocumentV1 document)
    {
        ValidateText(document.ScenarioId, "scenarioId");
        ValidateText(document.Title, "title");
        ValidateText(document.Description, "description");
        if (document.InitialCondition is null)
        {
            throw new InvalidDataException("Scenario document must contain initialCondition.");
        }
        ValidateText(document.InitialCondition.Id, "initialCondition.id");

        var objectives = (document.Objectives ?? Array.Empty<ScenarioObjectiveDocumentV1>())
            .Select((objective, index) =>
            {
                if (objective is null)
                {
                    throw new InvalidDataException($"Scenario objective at index {index} cannot be null.");
                }
                ValidateText(objective.Id, $"objectives[{index}].id");
                ValidateText(objective.Title, $"objectives[{index}].title");
                ValidateText(objective.Description, $"objectives[{index}].description");
                return new ScenarioObjectiveDefinition(objective.Id!, objective.Title!, objective.Description!);
            })
            .ToArray();

        return new ScenarioDefinition(
            document.ScenarioId!,
            document.Title!,
            document.Description!,
            new InitialConditionReference(document.InitialCondition.Id!, document.InitialCondition.Version),
            objectives,
            document.AllowedOperatorActions ?? Array.Empty<ControlRoomCommandKind>());
    }

    private static TDocument DeserializeDocument<TDocument>(string content)
        where TDocument : class
        => JsonSerializer.Deserialize<TDocument>(content, SerializerOptions)
           ?? throw new InvalidDataException("Scenario document could not be deserialized.");

    private static void ValidateText(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidDataException($"Scenario document field '{fieldName}' is required.");
        }
    }

    private static JsonSerializerOptions CreateSerializerOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
        };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    private sealed class ScenarioDocumentV1
    {
        public int SchemaVersion { get; set; }
        public string? ScenarioId { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public InitialConditionReferenceDocumentV1? InitialCondition { get; set; }
        public ScenarioObjectiveDocumentV1[]? Objectives { get; set; }
        public ControlRoomCommandKind[]? AllowedOperatorActions { get; set; }
    }

    private sealed class InitialConditionReferenceDocumentV1
    {
        public string? Id { get; set; }
        public int Version { get; set; }
    }

    private sealed class ScenarioObjectiveDocumentV1
    {
        public string? Id { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
    }

    private sealed class ScenarioDocumentV0
    {
        public int SchemaVersion { get; set; }
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? InitialConditionId { get; set; }
        public int InitialConditionVersion { get; set; }
        public string[]? Objectives { get; set; }
        public ControlRoomCommandKind[]? AllowedOperatorActions { get; set; }
    }
}
