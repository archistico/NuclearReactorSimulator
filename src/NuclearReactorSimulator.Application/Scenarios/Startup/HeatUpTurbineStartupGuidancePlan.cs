namespace NuclearReactorSimulator.Application.Scenarios.Startup;

public sealed class HeatUpTurbineStartupGuidancePlan
{
    private readonly IReadOnlyList<HeatUpTurbineStartupCheckDefinition> _checks;
    private readonly IReadOnlyList<HeatUpTurbineStartupStepDefinition> _steps;

    public HeatUpTurbineStartupGuidancePlan(
        IEnumerable<HeatUpTurbineStartupCheckDefinition> checks,
        IEnumerable<HeatUpTurbineStartupStepDefinition> steps)
    {
        ArgumentNullException.ThrowIfNull(checks);
        ArgumentNullException.ThrowIfNull(steps);

        var checkArray = checks.ToArray();
        if (checkArray.Select(static check => check.CheckId).Distinct(StringComparer.Ordinal).Count() != checkArray.Length)
        {
            throw new ArgumentException("Heat-up/startup check IDs must be unique.", nameof(checks));
        }

        var checkIds = checkArray.Select(static check => check.CheckId).ToHashSet(StringComparer.Ordinal);
        var stepArray = steps.OrderBy(static step => step.Sequence).ToArray();
        if (stepArray.Select(static step => step.StepId).Distinct(StringComparer.Ordinal).Count() != stepArray.Length)
        {
            throw new ArgumentException("Heat-up/startup step IDs must be unique.", nameof(steps));
        }
        if (stepArray.Select(static step => step.Sequence).Distinct().Count() != stepArray.Length)
        {
            throw new ArgumentException("Heat-up/startup step sequence numbers must be unique.", nameof(steps));
        }
        if (stepArray.SelectMany(static step => step.RequiredCheckIds).Any(checkId => !checkIds.Contains(checkId)))
        {
            throw new ArgumentException("Heat-up/startup steps may reference only checks declared by the guidance plan.", nameof(steps));
        }

        _checks = Array.AsReadOnly(checkArray);
        _steps = Array.AsReadOnly(stepArray);
    }

    public IReadOnlyList<HeatUpTurbineStartupCheckDefinition> Checks => _checks;
    public IReadOnlyList<HeatUpTurbineStartupStepDefinition> Steps => _steps;
}
