using System.Collections.ObjectModel;

namespace NuclearReactorSimulator.Application.Validation;

/// <summary>
/// Versioned deterministic reference case. ReferenceSource is provenance text only; it never changes physics or runtime behavior.
/// </summary>
public sealed class ReferenceValidationCaseDefinition
{
    public ReferenceValidationCaseDefinition(
        string caseId,
        string title,
        string description,
        ReferenceValidationCaseKind kind,
        string modelVersion,
        string referenceSource,
        IEnumerable<ReferenceValidationTarget> targets)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(caseId);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ArgumentException.ThrowIfNullOrWhiteSpace(modelVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(referenceSource);
        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        var targetArray = (targets ?? throw new ArgumentNullException(nameof(targets))).ToArray();
        if (targetArray.Length == 0)
        {
            throw new ArgumentException("Reference validation cases require at least one quantitative target.", nameof(targets));
        }
        if (targetArray.Any(static target => target is null))
        {
            throw new ArgumentException("Reference validation targets cannot contain null entries.", nameof(targets));
        }
        if (targetArray
            .GroupBy(static target => (target.MetricId, target.LogicalStep))
            .Any(static group => group.Count() > 1))
        {
            throw new ArgumentException("Reference validation targets must be unique by metric ID and logical step.", nameof(targets));
        }

        CaseId = caseId;
        Title = title;
        Description = description;
        Kind = kind;
        ModelVersion = modelVersion;
        ReferenceSource = referenceSource;
        Targets = new ReadOnlyCollection<ReferenceValidationTarget>(targetArray);
    }

    public string CaseId { get; }

    public string Title { get; }

    public string Description { get; }

    public ReferenceValidationCaseKind Kind { get; }

    public string ModelVersion { get; }

    public string ReferenceSource { get; }

    public IReadOnlyList<ReferenceValidationTarget> Targets { get; }
}
