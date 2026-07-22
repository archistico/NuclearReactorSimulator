namespace NuclearReactorSimulator.Application.Validation;

/// <summary>Pure deterministic comparison of observed samples against an explicit versioned tolerance budget.</summary>
public static class ReferenceValidationRunner
{
    public static ReferenceValidationCaseReport Evaluate(
        ReferenceValidationCaseDefinition definition,
        IEnumerable<ReferenceValidationSample> samples)
    {
        ArgumentNullException.ThrowIfNull(definition);
        var sampleArray = (samples ?? throw new ArgumentNullException(nameof(samples))).ToArray();
        if (sampleArray.GroupBy(static sample => sample.LogicalStep).Any(static group => group.Count() > 1))
        {
            throw new ArgumentException("Reference validation samples must have unique logical steps.", nameof(samples));
        }

        var byStep = sampleArray.ToDictionary(static sample => sample.LogicalStep);
        var results = new List<ReferenceValidationMetricResult>(definition.Targets.Count);

        foreach (var target in definition.Targets)
        {
            var allowedError = target.Tolerance.AllowedError(target.ReferenceValue);
            if (!byStep.TryGetValue(target.LogicalStep, out var sample)
                || !sample.Metrics.TryGetValue(target.MetricId, out var observed)
                || !observed.HasValue)
            {
                results.Add(new ReferenceValidationMetricResult(
                    target.MetricId,
                    target.LogicalStep,
                    target.ReferenceValue,
                    null,
                    allowedError,
                    null,
                    ReferenceValidationMetricStatus.Missing));
                continue;
            }

            var absoluteError = Math.Abs(observed.Value - target.ReferenceValue);
            results.Add(new ReferenceValidationMetricResult(
                target.MetricId,
                target.LogicalStep,
                target.ReferenceValue,
                observed.Value,
                allowedError,
                absoluteError,
                absoluteError <= allowedError
                    ? ReferenceValidationMetricStatus.Passed
                    : ReferenceValidationMetricStatus.Failed));
        }

        return new ReferenceValidationCaseReport(definition.CaseId, definition.ModelVersion, results);
    }
}
