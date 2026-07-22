namespace NuclearReactorSimulator.Application.Validation;

public sealed record ReferenceSensitivityReport(
    string ProbeId,
    string ModelVersion,
    string ParameterId,
    string MetricId,
    double BaselineMetricValue,
    double PerturbedMetricValue,
    double MetricDelta,
    double ParameterDelta,
    double NormalizedSensitivity,
    bool IsPassed,
    string Assessment);
