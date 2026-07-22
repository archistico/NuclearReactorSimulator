namespace NuclearReactorSimulator.Application.Scenarios.Historical;

/// <summary>
/// Raised when historical-inspired content declares model-fidelity requirements that are not available in the active build.
/// </summary>
public sealed class HistoricalScenarioFidelityException : InvalidOperationException
{
    public HistoricalScenarioFidelityException(string scenarioId, IEnumerable<string> missingCapabilities)
        : this(ValidateScenarioId(scenarioId), Normalize(missingCapabilities))
    {
    }

    private HistoricalScenarioFidelityException(string scenarioId, string[] normalizedMissingCapabilities)
        : base($"Historical-inspired scenario '{scenarioId}' cannot be loaded because required validated model capabilities are missing: {string.Join(", ", normalizedMissingCapabilities)}.")
    {
        ScenarioId = scenarioId;
        MissingCapabilities = Array.AsReadOnly(normalizedMissingCapabilities);
    }

    public string ScenarioId { get; }

    public IReadOnlyList<string> MissingCapabilities { get; }

    private static string ValidateScenarioId(string scenarioId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scenarioId);
        return scenarioId;
    }

    private static string[] Normalize(IEnumerable<string> missingCapabilities)
    {
        ArgumentNullException.ThrowIfNull(missingCapabilities);
        return missingCapabilities.OrderBy(static value => value, StringComparer.Ordinal).ToArray();
    }
}
