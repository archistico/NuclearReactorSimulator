namespace NuclearReactorSimulator.Application.Validation;

public sealed record ReferenceValidationMetricResult(
    string MetricId,
    long LogicalStep,
    double ReferenceValue,
    double? ObservedValue,
    double AllowedError,
    double? AbsoluteError,
    ReferenceValidationMetricStatus Status)
{
    public bool IsPassed => Status == ReferenceValidationMetricStatus.Passed;
}
