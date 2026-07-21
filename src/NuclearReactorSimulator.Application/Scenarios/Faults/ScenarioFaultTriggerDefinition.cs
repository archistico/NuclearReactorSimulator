namespace NuclearReactorSimulator.Application.Scenarios.Faults;

/// <summary>
/// Immutable M8.1 fault transition trigger. Logical-step triggers fire at the exact committed step boundary; plant-condition
/// triggers are evaluated only against the committed presentation snapshot and never against wall clock or hidden state.
/// </summary>
public sealed record ScenarioFaultTriggerDefinition
{
    private ScenarioFaultTriggerDefinition(
        ScenarioFaultTriggerKind kind,
        long? logicalStep,
        string? conditionId)
    {
        Kind = kind;
        LogicalStep = logicalStep;
        ConditionId = conditionId;
    }

    public ScenarioFaultTriggerKind Kind { get; }

    public long? LogicalStep { get; }

    public string? ConditionId { get; }

    public static ScenarioFaultTriggerDefinition AtLogicalStep(long logicalStep)
    {
        if (logicalStep < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(logicalStep));
        }

        return new ScenarioFaultTriggerDefinition(ScenarioFaultTriggerKind.LogicalStep, logicalStep, null);
    }

    public static ScenarioFaultTriggerDefinition WhenPlantCondition(string conditionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(conditionId);
        return new ScenarioFaultTriggerDefinition(
            ScenarioFaultTriggerKind.PlantCondition,
            null,
            conditionId.Trim());
    }
}
