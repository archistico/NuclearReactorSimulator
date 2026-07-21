namespace NuclearReactorSimulator.Application.Scenarios.Faults;

/// <summary>Exact-ID registry for M8.1 plant-condition trigger evaluators.</summary>
public sealed class ScenarioFaultConditionRegistry
{
    private readonly IReadOnlyDictionary<string, IScenarioFaultConditionEvaluator> _evaluators;

    public ScenarioFaultConditionRegistry(IEnumerable<IScenarioFaultConditionEvaluator>? evaluators = null)
    {
        var byId = new Dictionary<string, IScenarioFaultConditionEvaluator>(StringComparer.Ordinal);
        foreach (var evaluator in evaluators ?? Array.Empty<IScenarioFaultConditionEvaluator>())
        {
            ArgumentNullException.ThrowIfNull(evaluator);
            ArgumentException.ThrowIfNullOrWhiteSpace(evaluator.ConditionId);
            if (!byId.TryAdd(evaluator.ConditionId, evaluator))
            {
                throw new ArgumentException($"Duplicate scenario fault condition evaluator '{evaluator.ConditionId}'.", nameof(evaluators));
            }
        }

        _evaluators = byId;
    }

    public IScenarioFaultConditionEvaluator Resolve(string conditionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(conditionId);
        return _evaluators.TryGetValue(conditionId, out var evaluator)
            ? evaluator
            : throw new KeyNotFoundException($"No scenario fault condition evaluator is registered for '{conditionId}'.");
    }

    public static ScenarioFaultConditionRegistry Empty { get; } = new();
}
