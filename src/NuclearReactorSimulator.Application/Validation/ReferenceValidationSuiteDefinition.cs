using System.Collections.ObjectModel;

namespace NuclearReactorSimulator.Application.Validation;

public sealed class ReferenceValidationSuiteDefinition
{
    public ReferenceValidationSuiteDefinition(
        string suiteId,
        string modelVersion,
        IEnumerable<ReferenceValidationCaseDefinition> cases)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(suiteId);
        ArgumentException.ThrowIfNullOrWhiteSpace(modelVersion);
        var caseArray = (cases ?? throw new ArgumentNullException(nameof(cases))).ToArray();
        if (caseArray.Length == 0)
        {
            throw new ArgumentException("Reference validation suites require at least one case.", nameof(cases));
        }
        if (caseArray.Any(static definition => definition is null))
        {
            throw new ArgumentException("Reference validation suites cannot contain null cases.", nameof(cases));
        }
        if (caseArray.Select(static definition => definition.CaseId).Distinct(StringComparer.Ordinal).Count() != caseArray.Length)
        {
            throw new ArgumentException("Reference validation case IDs must be unique within a suite.", nameof(cases));
        }
        if (caseArray.Any(definition => !string.Equals(definition.ModelVersion, modelVersion, StringComparison.Ordinal)))
        {
            throw new ArgumentException("Every case in a reference suite must declare the suite model version exactly.", nameof(cases));
        }

        SuiteId = suiteId;
        ModelVersion = modelVersion;
        Cases = new ReadOnlyCollection<ReferenceValidationCaseDefinition>(caseArray);
    }

    public string SuiteId { get; }

    public string ModelVersion { get; }

    public IReadOnlyList<ReferenceValidationCaseDefinition> Cases { get; }
}
