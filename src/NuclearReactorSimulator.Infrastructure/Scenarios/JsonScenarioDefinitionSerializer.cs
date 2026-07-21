using System.Text.Json;
using System.Text.Json.Serialization;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Faults;

namespace NuclearReactorSimulator.Infrastructure.Scenarios;

/// <summary>
/// JSON persistence adapter for versioned scenario definitions. Schema v2 adds explicit deterministic M8.1 fault schedules;
/// v0/v1 migration preserves exact initial-condition identity and yields an empty fault set rather than inventing behavior.
/// </summary>
public sealed class JsonScenarioDefinitionSerializer : IScenarioDefinitionSerializer
{
    public const int CurrentSchemaVersion = 2;

    private static readonly JsonSerializerOptions SerializerOptions = CreateSerializerOptions();

    public string Serialize(ScenarioDefinition scenario)
    {
        ArgumentNullException.ThrowIfNull(scenario);

        var document = new ScenarioDocumentV2
        {
            SchemaVersion = CurrentSchemaVersion,
            ScenarioId = scenario.ScenarioId,
            Title = scenario.Title,
            Description = scenario.Description,
            InitialCondition = new InitialConditionReferenceDocument
            {
                Id = scenario.InitialCondition.InitialConditionId,
                Version = scenario.InitialCondition.Version,
            },
            Objectives = scenario.Objectives.Select(static objective => new ScenarioObjectiveDocument
            {
                Id = objective.ObjectiveId,
                Title = objective.Title,
                Description = objective.Description,
            }).ToArray(),
            AllowedOperatorActions = scenario.AllowedOperatorActions.OrderBy(static action => action).ToArray(),
            Faults = scenario.Faults.Select(static fault => new ScenarioFaultDocumentV2
            {
                FaultId = fault.FaultId,
                FaultTypeId = fault.FaultTypeId,
                TargetId = fault.TargetId,
                Parameters = new SortedDictionary<string, string>(fault.Parameters.ToDictionary(static pair => pair.Key, static pair => pair.Value, StringComparer.Ordinal), StringComparer.Ordinal),
                Activation = ToDocument(fault.Activation),
                Deactivation = fault.Deactivation is null ? null : ToDocument(fault.Deactivation),
            }).ToArray(),
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
            1 => MigrateV1(DeserializeDocument<ScenarioDocumentV1>(content)),
            CurrentSchemaVersion => ToDefinition(DeserializeDocument<ScenarioDocumentV2>(content)),
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
                return new ScenarioObjectiveDefinition($"objective-{index + 1:D3}", text!, text!);
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

    private static ScenarioDefinition MigrateV1(ScenarioDocumentV1 document)
        => ToDefinitionCore(
            document.ScenarioId,
            document.Title,
            document.Description,
            document.InitialCondition,
            document.Objectives,
            document.AllowedOperatorActions,
            Array.Empty<ScenarioFaultDefinition>());

    private static ScenarioDefinition ToDefinition(ScenarioDocumentV2 document)
    {
        var faults = (document.Faults ?? Array.Empty<ScenarioFaultDocumentV2>())
            .Select((fault, index) => ToFaultDefinition(fault, index))
            .ToArray();

        return ToDefinitionCore(
            document.ScenarioId,
            document.Title,
            document.Description,
            document.InitialCondition,
            document.Objectives,
            document.AllowedOperatorActions,
            faults);
    }

    private static ScenarioDefinition ToDefinitionCore(
        string? scenarioId,
        string? title,
        string? description,
        InitialConditionReferenceDocument? initialCondition,
        ScenarioObjectiveDocument[]? objectiveDocuments,
        ControlRoomCommandKind[]? allowedOperatorActions,
        IReadOnlyList<ScenarioFaultDefinition> faults)
    {
        ValidateText(scenarioId, "scenarioId");
        ValidateText(title, "title");
        ValidateText(description, "description");
        if (initialCondition is null)
        {
            throw new InvalidDataException("Scenario document must contain initialCondition.");
        }
        ValidateText(initialCondition.Id, "initialCondition.id");

        var objectives = (objectiveDocuments ?? Array.Empty<ScenarioObjectiveDocument>())
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
            scenarioId!,
            title!,
            description!,
            new InitialConditionReference(initialCondition.Id!, initialCondition.Version),
            objectives,
            allowedOperatorActions ?? Array.Empty<ControlRoomCommandKind>(),
            faults);
    }

    private static ScenarioFaultDefinition ToFaultDefinition(ScenarioFaultDocumentV2? document, int index)
    {
        if (document is null)
        {
            throw new InvalidDataException($"Scenario fault at index {index} cannot be null.");
        }

        ValidateText(document.FaultId, $"faults[{index}].faultId");
        ValidateText(document.FaultTypeId, $"faults[{index}].faultTypeId");
        ValidateText(document.TargetId, $"faults[{index}].targetId");
        if (document.Activation is null)
        {
            throw new InvalidDataException($"Scenario fault at index {index} must contain activation.");
        }

        return new ScenarioFaultDefinition(
            document.FaultId!,
            document.FaultTypeId!,
            document.TargetId!,
            ToTriggerDefinition(document.Activation, $"faults[{index}].activation"),
            document.Deactivation is null
                ? null
                : ToTriggerDefinition(document.Deactivation, $"faults[{index}].deactivation"),
            document.Parameters ?? new SortedDictionary<string, string>(StringComparer.Ordinal));
    }

    private static ScenarioFaultTriggerDefinition ToTriggerDefinition(
        ScenarioFaultTriggerDocumentV2 document,
        string fieldName)
        => document.Kind switch
        {
            ScenarioFaultTriggerKind.LogicalStep when document.LogicalStep is not null && document.ConditionId is null
                => ScenarioFaultTriggerDefinition.AtLogicalStep(document.LogicalStep.Value),
            ScenarioFaultTriggerKind.PlantCondition when document.LogicalStep is null && !string.IsNullOrWhiteSpace(document.ConditionId)
                => ScenarioFaultTriggerDefinition.WhenPlantCondition(document.ConditionId!),
            ScenarioFaultTriggerKind.LogicalStep
                => throw new InvalidDataException($"Scenario trigger '{fieldName}' requires logicalStep and forbids conditionId."),
            ScenarioFaultTriggerKind.PlantCondition
                => throw new InvalidDataException($"Scenario trigger '{fieldName}' requires conditionId and forbids logicalStep."),
            _ => throw new InvalidDataException($"Scenario trigger '{fieldName}' has unsupported kind '{document.Kind}'."),
        };

    private static ScenarioFaultTriggerDocumentV2 ToDocument(ScenarioFaultTriggerDefinition trigger)
        => new()
        {
            Kind = trigger.Kind,
            LogicalStep = trigger.LogicalStep,
            ConditionId = trigger.ConditionId,
        };

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

    private sealed class ScenarioDocumentV2
    {
        public int SchemaVersion { get; set; }
        public string? ScenarioId { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public InitialConditionReferenceDocument? InitialCondition { get; set; }
        public ScenarioObjectiveDocument[]? Objectives { get; set; }
        public ControlRoomCommandKind[]? AllowedOperatorActions { get; set; }
        public ScenarioFaultDocumentV2[]? Faults { get; set; }
    }

    private sealed class ScenarioDocumentV1
    {
        public int SchemaVersion { get; set; }
        public string? ScenarioId { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public InitialConditionReferenceDocument? InitialCondition { get; set; }
        public ScenarioObjectiveDocument[]? Objectives { get; set; }
        public ControlRoomCommandKind[]? AllowedOperatorActions { get; set; }
    }

    private sealed class InitialConditionReferenceDocument
    {
        public string? Id { get; set; }
        public int Version { get; set; }
    }

    private sealed class ScenarioObjectiveDocument
    {
        public string? Id { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
    }

    private sealed class ScenarioFaultDocumentV2
    {
        public string? FaultId { get; set; }
        public string? FaultTypeId { get; set; }
        public string? TargetId { get; set; }
        public SortedDictionary<string, string>? Parameters { get; set; }
        public ScenarioFaultTriggerDocumentV2? Activation { get; set; }
        public ScenarioFaultTriggerDocumentV2? Deactivation { get; set; }
    }

    private sealed class ScenarioFaultTriggerDocumentV2
    {
        public ScenarioFaultTriggerKind Kind { get; set; }
        public long? LogicalStep { get; set; }
        public string? ConditionId { get; set; }
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
