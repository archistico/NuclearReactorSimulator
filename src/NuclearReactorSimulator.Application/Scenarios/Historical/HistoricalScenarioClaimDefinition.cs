namespace NuclearReactorSimulator.Application.Scenarios.Historical;

/// <summary>
/// One explicitly classified statement in a historical-inspired scenario. Documented facts require source references;
/// educational approximations and simulator-specific assumptions require an explicit rationale.
/// </summary>
public sealed class HistoricalScenarioClaimDefinition
{
    private readonly IReadOnlyList<string> _sourceIds;

    public HistoricalScenarioClaimDefinition(
        string claimId,
        HistoricalScenarioClaimKind kind,
        string statement,
        IEnumerable<string>? sourceIds = null,
        string? rationale = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(claimId);
        ArgumentException.ThrowIfNullOrWhiteSpace(statement);
        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        var sourceArray = (sourceIds ?? Array.Empty<string>()).ToArray();
        if (sourceArray.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("Historical claim source IDs cannot contain blank values.", nameof(sourceIds));
        }
        if (sourceArray.Distinct(StringComparer.Ordinal).Count() != sourceArray.Length)
        {
            throw new ArgumentException("Historical claim source IDs must be unique.", nameof(sourceIds));
        }

        if (kind == HistoricalScenarioClaimKind.DocumentedFact && sourceArray.Length == 0)
        {
            throw new ArgumentException("A documented historical fact must reference at least one declared source.", nameof(sourceIds));
        }

        if (kind != HistoricalScenarioClaimKind.DocumentedFact && string.IsNullOrWhiteSpace(rationale))
        {
            throw new ArgumentException(
                "Educational approximations and simulator-specific assumptions require an explicit rationale.",
                nameof(rationale));
        }

        ClaimId = claimId;
        Kind = kind;
        Statement = statement;
        _sourceIds = Array.AsReadOnly(sourceArray);
        Rationale = string.IsNullOrWhiteSpace(rationale) ? null : rationale;
    }

    public string ClaimId { get; }

    public HistoricalScenarioClaimKind Kind { get; }

    public string Statement { get; }

    public IReadOnlyList<string> SourceIds => _sourceIds;

    public string? Rationale { get; }
}
