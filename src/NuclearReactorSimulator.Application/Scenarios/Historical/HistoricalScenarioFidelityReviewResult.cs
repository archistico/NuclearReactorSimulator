namespace NuclearReactorSimulator.Application.Scenarios.Historical;

/// <summary>
/// Immutable fail-closed M9.5 fidelity-review result. Approval means only that all explicitly declared model capabilities
/// are available; it is not a claim that the simulator reproduces historical reality quantitatively.
/// </summary>
public sealed class HistoricalScenarioFidelityReviewResult
{
    private readonly IReadOnlyList<string> _missingCapabilities;

    internal HistoricalScenarioFidelityReviewResult(string scenarioId, IEnumerable<string> missingCapabilities)
    {
        ScenarioId = scenarioId;
        var missing = missingCapabilities.OrderBy(static value => value, StringComparer.Ordinal).ToArray();
        _missingCapabilities = Array.AsReadOnly(missing);
    }

    public string ScenarioId { get; }

    public bool IsApproved => _missingCapabilities.Count == 0;

    public IReadOnlyList<string> MissingCapabilities => _missingCapabilities;
}
