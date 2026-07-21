namespace NuclearReactorSimulator.Application.Scenarios.Criticality;

public sealed class FirstCriticalityGuidancePlan
{
    private readonly IReadOnlyList<FirstCriticalityCheckDefinition> _checks;
    private readonly IReadOnlyList<FirstCriticalityStepDefinition> _steps;

    public FirstCriticalityGuidancePlan(
        IEnumerable<FirstCriticalityCheckDefinition> checks,
        IEnumerable<FirstCriticalityStepDefinition> steps)
    {
        ArgumentNullException.ThrowIfNull(checks);
        ArgumentNullException.ThrowIfNull(steps);

        var checkArray = checks.ToArray();
        if (checkArray.Select(static check => check.CheckId).Distinct(StringComparer.Ordinal).Count() != checkArray.Length)
        {
            throw new ArgumentException("First-criticality check IDs must be unique.", nameof(checks));
        }

        var checkIds = checkArray.Select(static check => check.CheckId).ToHashSet(StringComparer.Ordinal);
        var stepArray = steps.OrderBy(static step => step.Sequence).ToArray();
        if (stepArray.Select(static step => step.StepId).Distinct(StringComparer.Ordinal).Count() != stepArray.Length)
        {
            throw new ArgumentException("First-criticality step IDs must be unique.", nameof(steps));
        }
        if (stepArray.Select(static step => step.Sequence).Distinct().Count() != stepArray.Length)
        {
            throw new ArgumentException("First-criticality step sequence numbers must be unique.", nameof(steps));
        }
        if (stepArray.SelectMany(static step => step.RequiredCheckIds).Any(checkId => !checkIds.Contains(checkId)))
        {
            throw new ArgumentException("First-criticality steps may reference only checks declared by the guidance plan.", nameof(steps));
        }

        _checks = Array.AsReadOnly(checkArray);
        _steps = Array.AsReadOnly(stepArray);
    }

    public IReadOnlyList<FirstCriticalityCheckDefinition> Checks => _checks;
    public IReadOnlyList<FirstCriticalityStepDefinition> Steps => _steps;
}
