namespace NuclearReactorSimulator.Application.Scenarios.Historical;

/// <summary>
/// M9.5 deterministic model-fidelity gate for historical-inspired scenario content. The reviewer checks only explicit,
/// versioned metadata against an explicit set of validated capability IDs and fails closed when any requirement is absent.
/// It never infers historical truth or causal correctness from simulator behavior.
/// </summary>
public static class HistoricalScenarioFidelityReviewer
{
    public static HistoricalScenarioFidelityReviewResult Review(
        ScenarioDefinition scenario,
        IEnumerable<string> validatedModelCapabilities)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        ArgumentNullException.ThrowIfNull(validatedModelCapabilities);

        var historicalContext = scenario.HistoricalContext
            ?? throw new InvalidOperationException(
                $"Scenario '{scenario.ScenarioId}' has no historical context and cannot be fidelity-reviewed as historical-inspired content.");

        var available = validatedModelCapabilities.ToArray();
        if (available.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("Validated model capability IDs cannot contain blank entries.", nameof(validatedModelCapabilities));
        }

        var availableSet = available.ToHashSet(StringComparer.Ordinal);
        var missing = historicalContext.RequiredModelCapabilities.Where(capability => !availableSet.Contains(capability));
        return new HistoricalScenarioFidelityReviewResult(scenario.ScenarioId, missing);
    }
}
