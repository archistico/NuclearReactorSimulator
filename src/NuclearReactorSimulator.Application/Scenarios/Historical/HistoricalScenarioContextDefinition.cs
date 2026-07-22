namespace NuclearReactorSimulator.Application.Scenarios.Historical;

/// <summary>
/// M9.5 immutable provenance/fidelity envelope for a historical-inspired scenario. It separates sourced facts from
/// educational approximations and simulator-specific assumptions and declares the validated model capabilities required
/// before the scenario may be presented as fidelity-reviewed.
/// </summary>
public sealed class HistoricalScenarioContextDefinition
{
    private readonly IReadOnlyList<HistoricalSourceReference> _sources;
    private readonly IReadOnlyList<HistoricalScenarioClaimDefinition> _claims;
    private readonly IReadOnlyList<string> _requiredModelCapabilities;
    private readonly IReadOnlyList<string> _deliberateNonClaims;

    public HistoricalScenarioContextDefinition(
        string historicalSubject,
        string fidelityStatement,
        IEnumerable<HistoricalSourceReference>? sources,
        IEnumerable<HistoricalScenarioClaimDefinition>? claims,
        IEnumerable<string>? requiredModelCapabilities,
        IEnumerable<string>? deliberateNonClaims)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(historicalSubject);
        ArgumentException.ThrowIfNullOrWhiteSpace(fidelityStatement);

        var sourceArray = (sources ?? Array.Empty<HistoricalSourceReference>()).ToArray();
        if (sourceArray.Any(static source => source is null))
        {
            throw new ArgumentException("Historical sources cannot contain null entries.", nameof(sources));
        }
        if (sourceArray.Select(static source => source.SourceId).Distinct(StringComparer.Ordinal).Count() != sourceArray.Length)
        {
            throw new ArgumentException("Historical source IDs must be unique.", nameof(sources));
        }

        var claimArray = (claims ?? Array.Empty<HistoricalScenarioClaimDefinition>()).ToArray();
        if (claimArray.Any(static claim => claim is null))
        {
            throw new ArgumentException("Historical claims cannot contain null entries.", nameof(claims));
        }
        if (claimArray.Select(static claim => claim.ClaimId).Distinct(StringComparer.Ordinal).Count() != claimArray.Length)
        {
            throw new ArgumentException("Historical claim IDs must be unique.", nameof(claims));
        }
        if (!claimArray.Any(static claim => claim.Kind == HistoricalScenarioClaimKind.DocumentedFact))
        {
            throw new ArgumentException(
                "Historical-inspired scenario metadata must contain at least one documented fact backed by a declared source.",
                nameof(claims));
        }

        var declaredSourceIds = sourceArray.Select(static source => source.SourceId).ToHashSet(StringComparer.Ordinal);
        var unknownSourceId = claimArray
            .SelectMany(static claim => claim.SourceIds)
            .FirstOrDefault(sourceId => !declaredSourceIds.Contains(sourceId));
        if (unknownSourceId is not null)
        {
            throw new ArgumentException(
                $"Historical claim references undeclared source ID '{unknownSourceId}'.",
                nameof(claims));
        }

        var capabilityArray = NormalizeDistinctText(requiredModelCapabilities, nameof(requiredModelCapabilities));
        if (capabilityArray.Length == 0)
        {
            throw new ArgumentException(
                "Historical-inspired scenario metadata must require at least one explicit validated model capability.",
                nameof(requiredModelCapabilities));
        }

        var nonClaimArray = NormalizeDistinctText(deliberateNonClaims, nameof(deliberateNonClaims));
        if (nonClaimArray.Length == 0)
        {
            throw new ArgumentException(
                "Historical-inspired scenario metadata must state at least one deliberate non-claim to prevent fidelity overstatement.",
                nameof(deliberateNonClaims));
        }

        HistoricalSubject = historicalSubject;
        FidelityStatement = fidelityStatement;
        _sources = Array.AsReadOnly(sourceArray);
        _claims = Array.AsReadOnly(claimArray);
        _requiredModelCapabilities = Array.AsReadOnly(capabilityArray);
        _deliberateNonClaims = Array.AsReadOnly(nonClaimArray);
    }

    public string HistoricalSubject { get; }

    public string FidelityStatement { get; }

    public IReadOnlyList<HistoricalSourceReference> Sources => _sources;

    public IReadOnlyList<HistoricalScenarioClaimDefinition> Claims => _claims;

    public IReadOnlyList<string> RequiredModelCapabilities => _requiredModelCapabilities;

    public IReadOnlyList<string> DeliberateNonClaims => _deliberateNonClaims;

    private static string[] NormalizeDistinctText(IEnumerable<string>? values, string parameterName)
    {
        var array = (values ?? Array.Empty<string>()).ToArray();
        if (array.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("Historical metadata values cannot contain blank entries.", parameterName);
        }
        if (array.Distinct(StringComparer.Ordinal).Count() != array.Length)
        {
            throw new ArgumentException("Historical metadata values must be unique.", parameterName);
        }
        return array;
    }
}
