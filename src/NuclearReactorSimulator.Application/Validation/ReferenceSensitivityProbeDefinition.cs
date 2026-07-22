namespace NuclearReactorSimulator.Application.Validation;

/// <summary>Explicit one-parameter sensitivity/regression expectation; it does not mutate model parameters itself.</summary>
public sealed record ReferenceSensitivityProbeDefinition
{
    public ReferenceSensitivityProbeDefinition(
        string probeId,
        string parameterId,
        double baselineParameterValue,
        double perturbedParameterValue,
        string metricId,
        ReferenceSensitivityDirection expectedDirection,
        double minimumAbsoluteResponse = 0d,
        double maximumAbsoluteResponse = double.PositiveInfinity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(probeId);
        ArgumentException.ThrowIfNullOrWhiteSpace(parameterId);
        ArgumentException.ThrowIfNullOrWhiteSpace(metricId);
        if (!double.IsFinite(baselineParameterValue) || !double.IsFinite(perturbedParameterValue))
        {
            throw new ArgumentOutOfRangeException(nameof(perturbedParameterValue));
        }
        if (baselineParameterValue == perturbedParameterValue)
        {
            throw new ArgumentException("Sensitivity probes require a non-zero parameter perturbation.", nameof(perturbedParameterValue));
        }
        if (!Enum.IsDefined(expectedDirection))
        {
            throw new ArgumentOutOfRangeException(nameof(expectedDirection));
        }
        if (!double.IsFinite(minimumAbsoluteResponse) || minimumAbsoluteResponse < 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumAbsoluteResponse));
        }
        if (double.IsNaN(maximumAbsoluteResponse) || maximumAbsoluteResponse < minimumAbsoluteResponse)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumAbsoluteResponse));
        }

        ProbeId = probeId;
        ParameterId = parameterId;
        BaselineParameterValue = baselineParameterValue;
        PerturbedParameterValue = perturbedParameterValue;
        MetricId = metricId;
        ExpectedDirection = expectedDirection;
        MinimumAbsoluteResponse = minimumAbsoluteResponse;
        MaximumAbsoluteResponse = maximumAbsoluteResponse;
    }

    public string ProbeId { get; }
    public string ParameterId { get; }
    public double BaselineParameterValue { get; }
    public double PerturbedParameterValue { get; }
    public string MetricId { get; }
    public ReferenceSensitivityDirection ExpectedDirection { get; }
    public double MinimumAbsoluteResponse { get; }
    public double MaximumAbsoluteResponse { get; }
}
