using System.Collections.ObjectModel;

namespace NuclearReactorSimulator.Application.Validation;

public sealed class ReferenceValidationSuiteReport
{
    public const int CurrentSchemaVersion = 1;

    public ReferenceValidationSuiteReport(
        int schemaVersion,
        string suiteId,
        string modelVersion,
        IEnumerable<ReferenceValidationCaseReport> cases,
        IEnumerable<ReferenceSensitivityReport>? sensitivities = null)
    {
        if (schemaVersion != CurrentSchemaVersion)
        {
            throw new NotSupportedException($"Reference validation report schema version {schemaVersion} is not supported.");
        }
        ArgumentException.ThrowIfNullOrWhiteSpace(suiteId);
        ArgumentException.ThrowIfNullOrWhiteSpace(modelVersion);
        var caseArray = (cases ?? throw new ArgumentNullException(nameof(cases))).ToArray();
        var sensitivityArray = (sensitivities ?? Array.Empty<ReferenceSensitivityReport>()).ToArray();
        if (caseArray.Any(static report => report is null) || sensitivityArray.Any(static report => report is null))
        {
            throw new ArgumentException("Reference validation reports cannot contain null entries.", nameof(cases));
        }

        SchemaVersion = schemaVersion;
        SuiteId = suiteId;
        ModelVersion = modelVersion;
        Cases = new ReadOnlyCollection<ReferenceValidationCaseReport>(caseArray);
        Sensitivities = new ReadOnlyCollection<ReferenceSensitivityReport>(sensitivityArray);
    }

    public int SchemaVersion { get; }

    public string SuiteId { get; }

    public string ModelVersion { get; }

    public IReadOnlyList<ReferenceValidationCaseReport> Cases { get; }

    public IReadOnlyList<ReferenceSensitivityReport> Sensitivities { get; }

    public bool IsPassed => Cases.Count > 0
        && Cases.All(static report => report.IsPassed)
        && Sensitivities.All(static report => report.IsPassed);
}
