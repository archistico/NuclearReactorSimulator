namespace NuclearReactorSimulator.Application.Scenarios.Historical;

/// <summary>
/// Immutable bibliographic/source reference carried as scenario metadata. M9.5 stores the citation supplied by the scenario
/// author; it does not fetch, reinterpret or silently upgrade external source material at runtime.
/// </summary>
public sealed record HistoricalSourceReference
{
    public HistoricalSourceReference(string sourceId, string citation, string? locator = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(citation);
        if (locator is not null && string.IsNullOrWhiteSpace(locator))
        {
            throw new ArgumentException("Historical source locator cannot be whitespace when supplied.", nameof(locator));
        }

        SourceId = sourceId;
        Citation = citation;
        Locator = locator;
    }

    public string SourceId { get; }

    public string Citation { get; }

    public string? Locator { get; }
}
