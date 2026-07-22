namespace NuclearReactorSimulator.Application.Validation;

/// <summary>Pure scalar sensitivity/regression analysis over results produced by canonical model runs.</summary>
public static class ReferenceSensitivityAnalyzer
{
    public static ReferenceSensitivityReport Evaluate(
        ReferenceSensitivityProbeDefinition definition,
        string modelVersion,
        double baselineMetricValue,
        double perturbedMetricValue)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentException.ThrowIfNullOrWhiteSpace(modelVersion);
        if (!double.IsFinite(baselineMetricValue) || !double.IsFinite(perturbedMetricValue))
        {
            throw new ArgumentOutOfRangeException(nameof(perturbedMetricValue));
        }

        var metricDelta = perturbedMetricValue - baselineMetricValue;
        var parameterDelta = definition.PerturbedParameterValue - definition.BaselineParameterValue;
        var magnitude = Math.Abs(metricDelta);
        var magnitudeAccepted = magnitude >= definition.MinimumAbsoluteResponse
            && magnitude <= definition.MaximumAbsoluteResponse;
        var directionAccepted = definition.ExpectedDirection switch
        {
            ReferenceSensitivityDirection.Increase => metricDelta > 0d,
            ReferenceSensitivityDirection.Decrease => metricDelta < 0d,
            ReferenceSensitivityDirection.AnyNonZero => metricDelta != 0d,
            ReferenceSensitivityDirection.NoMaterialChange => magnitude <= definition.MaximumAbsoluteResponse,
            _ => false,
        };
        var passed = magnitudeAccepted && directionAccepted;

        return new ReferenceSensitivityReport(
            definition.ProbeId,
            modelVersion,
            definition.ParameterId,
            definition.MetricId,
            baselineMetricValue,
            perturbedMetricValue,
            metricDelta,
            parameterDelta,
            metricDelta / parameterDelta,
            passed,
            passed
                ? "Observed response satisfies the declared sensitivity/regression budget."
                : "Observed response violates the declared sensitivity direction or magnitude budget.");
    }
}
