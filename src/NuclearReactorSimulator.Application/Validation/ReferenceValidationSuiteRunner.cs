namespace NuclearReactorSimulator.Application.Validation;

public static class ReferenceValidationSuiteRunner
{
    public static ReferenceValidationSuiteReport Evaluate(
        ReferenceValidationSuiteDefinition suite,
        IReadOnlyDictionary<string, IReadOnlyList<ReferenceValidationSample>> samplesByCase,
        IEnumerable<ReferenceSensitivityReport>? sensitivities = null)
    {
        ArgumentNullException.ThrowIfNull(suite);
        ArgumentNullException.ThrowIfNull(samplesByCase);
        var caseReports = new List<ReferenceValidationCaseReport>(suite.Cases.Count);

        foreach (var definition in suite.Cases)
        {
            samplesByCase.TryGetValue(definition.CaseId, out var samples);
            caseReports.Add(ReferenceValidationRunner.Evaluate(
                definition,
                samples ?? Array.Empty<ReferenceValidationSample>()));
        }

        return new ReferenceValidationSuiteReport(
            ReferenceValidationSuiteReport.CurrentSchemaVersion,
            suite.SuiteId,
            suite.ModelVersion,
            caseReports,
            sensitivities);
    }
}
