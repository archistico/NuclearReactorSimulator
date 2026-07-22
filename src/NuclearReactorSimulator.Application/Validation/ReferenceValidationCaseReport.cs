using System.Collections.ObjectModel;

namespace NuclearReactorSimulator.Application.Validation;

public sealed class ReferenceValidationCaseReport
{
    public ReferenceValidationCaseReport(
        string caseId,
        string modelVersion,
        IEnumerable<ReferenceValidationMetricResult> metrics)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(caseId);
        ArgumentException.ThrowIfNullOrWhiteSpace(modelVersion);
        var metricArray = (metrics ?? throw new ArgumentNullException(nameof(metrics))).ToArray();
        if (metricArray.Any(static metric => metric is null))
        {
            throw new ArgumentException("Reference validation report metrics cannot contain null entries.", nameof(metrics));
        }

        CaseId = caseId;
        ModelVersion = modelVersion;
        Metrics = new ReadOnlyCollection<ReferenceValidationMetricResult>(metricArray);
    }

    public string CaseId { get; }

    public string ModelVersion { get; }

    public IReadOnlyList<ReferenceValidationMetricResult> Metrics { get; }

    public bool IsPassed => Metrics.Count > 0 && Metrics.All(static metric => metric.IsPassed);

    public int FailedMetricCount => Metrics.Count(static metric => metric.Status == ReferenceValidationMetricStatus.Failed);

    public int MissingMetricCount => Metrics.Count(static metric => metric.Status == ReferenceValidationMetricStatus.Missing);
}
