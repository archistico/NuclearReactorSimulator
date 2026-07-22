namespace NuclearReactorSimulator.Application.Validation;

/// <summary>One metric expectation at one exact logical step.</summary>
public sealed record ReferenceValidationTarget
{
    public ReferenceValidationTarget(
        string metricId,
        long logicalStep,
        double referenceValue,
        ReferenceValidationToleranceBudget tolerance)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(metricId);
        if (logicalStep < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(logicalStep));
        }
        if (!double.IsFinite(referenceValue))
        {
            throw new ArgumentOutOfRangeException(nameof(referenceValue));
        }

        MetricId = metricId;
        LogicalStep = logicalStep;
        ReferenceValue = referenceValue;
        Tolerance = tolerance ?? throw new ArgumentNullException(nameof(tolerance));
    }

    public string MetricId { get; }

    public long LogicalStep { get; }

    public double ReferenceValue { get; }

    public ReferenceValidationToleranceBudget Tolerance { get; }
}
