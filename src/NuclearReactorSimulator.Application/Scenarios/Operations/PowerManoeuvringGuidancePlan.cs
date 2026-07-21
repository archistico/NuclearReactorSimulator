namespace NuclearReactorSimulator.Application.Scenarios.Operations;

public sealed class PowerManoeuvringGuidancePlan
{
    private readonly IReadOnlyList<PowerManoeuvringCheckDefinition> _checks;
    private readonly IReadOnlyList<PowerManoeuvringStepDefinition> _steps;

    public PowerManoeuvringGuidancePlan(
        IEnumerable<PowerManoeuvringCheckDefinition> checks,
        IEnumerable<PowerManoeuvringStepDefinition> steps)
    {
        ArgumentNullException.ThrowIfNull(checks);
        ArgumentNullException.ThrowIfNull(steps);

        var checkArray = checks.ToArray();
        if (checkArray.Select(static check => check.CheckId).Distinct(StringComparer.Ordinal).Count() != checkArray.Length)
        {
            throw new ArgumentException("Power-manoeuvring check IDs must be unique.", nameof(checks));
        }

        var checkIds = checkArray.Select(static check => check.CheckId).ToHashSet(StringComparer.Ordinal);
        var stepArray = steps.OrderBy(static step => step.Sequence).ToArray();
        if (stepArray.Select(static step => step.StepId).Distinct(StringComparer.Ordinal).Count() != stepArray.Length)
        {
            throw new ArgumentException("Power-manoeuvring step IDs must be unique.", nameof(steps));
        }
        if (stepArray.Select(static step => step.Sequence).Distinct().Count() != stepArray.Length)
        {
            throw new ArgumentException("Power-manoeuvring step sequence numbers must be unique.", nameof(steps));
        }
        if (stepArray.SelectMany(static step => step.RequiredCheckIds).Any(checkId => !checkIds.Contains(checkId)))
        {
            throw new ArgumentException("Power-manoeuvring steps may reference only checks declared by the guidance plan.", nameof(steps));
        }

        _checks = Array.AsReadOnly(checkArray);
        _steps = Array.AsReadOnly(stepArray);
    }

    public IReadOnlyList<PowerManoeuvringCheckDefinition> Checks => _checks;
    public IReadOnlyList<PowerManoeuvringStepDefinition> Steps => _steps;
}
