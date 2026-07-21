using NuclearReactorSimulator.Application.ControlRoom;

namespace NuclearReactorSimulator.Application.Scenarios.Criticality;

/// <summary>
/// Observes M7.3 approach-to-criticality and low-power criteria only through the immutable control-room presentation
/// boundary. It never changes reactivity, rod motion, protection state or plant inventories.
/// </summary>
public sealed class FirstCriticalityChecklistEvaluator
{
    private const double ClosedValveTolerancePercent = 0.1d;
    private const double ApproachLowerPcm = -20d;
    private const double ApproachUpperPcm = 0d;
    private const double CriticalityTolerancePcm = 2d;
    private const double SourceRangeMaximumMegawatts = 0.01d;
    private const double LowPowerMinimumMegawatts = 0.01d;
    private const double LowPowerMaximumMegawatts = 5d;
    private const double StablePeriodMinimumMagnitudeSeconds = 20d;

    public IReadOnlyList<FirstCriticalityCheckResult> Evaluate(
        ControlRoomSnapshot snapshot,
        IEnumerable<FirstCriticalityCheckDefinition> checks)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(checks);
        return checks.Select(check => Evaluate(snapshot, check ?? throw new ArgumentException(
            "First-criticality checks cannot contain null entries.", nameof(checks)))).ToArray();
    }

    public FirstCriticalityCheckResult Evaluate(
        ControlRoomSnapshot snapshot,
        FirstCriticalityCheckDefinition check)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(check);

        return check.Condition switch
        {
            FirstCriticalityCheckCondition.MeasuredSignalsHealthy => Result(
                check,
                snapshot.TotalMeasuredSignalCount > 0 && snapshot.InvalidMeasuredSignalCount == 0,
                $"Measured signals: {snapshot.ValidMeasuredSignalCount}/{snapshot.TotalMeasuredSignalCount} valid"),
            FirstCriticalityCheckCondition.ProtectionClear => Result(
                check,
                !snapshot.AnyTripActive,
                snapshot.AnyTripActive ? "One or more protection trips are active" : "No reactor/turbine/generator trip active"),
            FirstCriticalityCheckCondition.MainCirculationPumpsRunning => EvaluatePumps(snapshot, check),
            FirstCriticalityCheckCondition.SteamIsolationClosed => EvaluateSteamIsolation(snapshot, check),
            FirstCriticalityCheckCondition.GeneratorBreakersOpen => EvaluateBreakers(snapshot, check),
            FirstCriticalityCheckCondition.RodWithdrawalPermitted => Result(
                check,
                !snapshot.ReactorCore.RodWithdrawalInhibited,
                snapshot.ReactorCore.RodWithdrawalInterlockText),
            FirstCriticalityCheckCondition.SourceRangePowerEstablished => EvaluateSourceRange(snapshot, check),
            FirstCriticalityCheckCondition.ApproachToCriticality => EvaluateReactivityWindow(
                snapshot, check, ApproachLowerPcm, ApproachUpperPcm, upperInclusive: false),
            FirstCriticalityCheckCondition.CriticalityEstablished => EvaluateCriticality(snapshot, check),
            FirstCriticalityCheckCondition.LowPowerBand => EvaluateLowPower(snapshot, check),
            FirstCriticalityCheckCondition.StableLowPowerPeriod => EvaluateStablePeriod(snapshot, check),
            FirstCriticalityCheckCondition.XenonBoundaryExplicit => Result(
                check,
                snapshot.ReactorCore.XenonReactivity.State == ControlRoomVisualState.Unavailable
                    && !snapshot.ReactorCore.XenonReactivity.NumericValue.HasValue,
                snapshot.ReactorCore.XenonReactivity.State == ControlRoomVisualState.Unavailable
                    ? "Xenon reactivity is explicitly unavailable at the M5.7 operational snapshot boundary"
                    : "Unexpected quantitative xenon value published"),
            _ => throw new ArgumentOutOfRangeException(nameof(check), check.Condition, "Unsupported first-criticality check condition."),
        };
    }

    private static FirstCriticalityCheckResult EvaluatePumps(ControlRoomSnapshot snapshot, FirstCriticalityCheckDefinition check)
    {
        var pumps = snapshot.PrimaryCircuit.Pumps;
        var running = pumps.Count(static pump => pump.IsRunning);
        return Result(check, pumps.Count > 0 && running == pumps.Count, $"Main-circulation pumps running: {running}/{pumps.Count}");
    }

    private static FirstCriticalityCheckResult EvaluateSteamIsolation(ControlRoomSnapshot snapshot, FirstCriticalityCheckDefinition check)
    {
        var positions = snapshot.TurbineSecondary.AdmissionTrains.SelectMany(static train => new[]
        {
            train.StopValvePosition.NumericValue,
            train.ControlValvePosition.NumericValue,
            train.AdmissionValvePosition.NumericValue,
        }).ToArray();
        var satisfied = positions.Length > 0
            && positions.All(static position => position.HasValue && Math.Abs(position.Value) <= ClosedValveTolerancePercent);
        var maximum = positions.Where(static position => position.HasValue)
            .Select(static position => Math.Abs(position!.Value))
            .DefaultIfEmpty(double.NaN)
            .Max();
        return Result(check, satisfied, double.IsNaN(maximum)
            ? "Steam-isolation valve positions unavailable"
            : FormattableString.Invariant($"Maximum admission-path valve opening: {maximum:0.###}%"));
    }

    private static FirstCriticalityCheckResult EvaluateBreakers(ControlRoomSnapshot snapshot, FirstCriticalityCheckDefinition check)
    {
        var generators = snapshot.Electrical.Generators;
        var closed = generators.Count(static generator => generator.BreakerClosed);
        return Result(check, generators.Count > 0 && closed == 0, $"Closed generator breakers: {closed}/{generators.Count}");
    }

    private static FirstCriticalityCheckResult EvaluateSourceRange(ControlRoomSnapshot snapshot, FirstCriticalityCheckDefinition check)
    {
        var power = snapshot.ReactorCore.ReactorThermalPower.NumericValue;
        var satisfied = power.HasValue && power.Value > 0d && power.Value <= SourceRangeMaximumMegawatts;
        return Result(check, satisfied, power.HasValue
            ? FormattableString.Invariant($"Reactor thermal power: {power.Value:0.########} MWth")
            : "Reactor thermal power unavailable");
    }

    private static FirstCriticalityCheckResult EvaluateReactivityWindow(
        ControlRoomSnapshot snapshot,
        FirstCriticalityCheckDefinition check,
        double lowerPcm,
        double upperPcm,
        bool upperInclusive)
    {
        var reactivity = TotalReactivityPcm(snapshot);
        var satisfied = reactivity.HasValue
            && reactivity.Value >= lowerPcm
            && (upperInclusive ? reactivity.Value <= upperPcm : reactivity.Value < upperPcm);
        return Result(check, satisfied, reactivity.HasValue
            ? FormattableString.Invariant($"Total modeled reactivity: {reactivity.Value:0.###} pcm")
            : "Total modeled reactivity unavailable");
    }

    private static FirstCriticalityCheckResult EvaluateCriticality(ControlRoomSnapshot snapshot, FirstCriticalityCheckDefinition check)
    {
        var reactivity = TotalReactivityPcm(snapshot);
        var power = snapshot.ReactorCore.ReactorThermalPower.NumericValue;
        var satisfied = reactivity.HasValue
            && Math.Abs(reactivity.Value) <= CriticalityTolerancePcm
            && power.HasValue
            && power.Value > 0d;
        return Result(check, satisfied,
            reactivity.HasValue && power.HasValue
                ? FormattableString.Invariant($"Reactivity {reactivity.Value:0.###} pcm · power {power.Value:0.########} MWth")
                : "Criticality observables unavailable");
    }

    private static FirstCriticalityCheckResult EvaluateLowPower(ControlRoomSnapshot snapshot, FirstCriticalityCheckDefinition check)
    {
        var power = snapshot.ReactorCore.ReactorThermalPower.NumericValue;
        var satisfied = power.HasValue && power.Value >= LowPowerMinimumMegawatts && power.Value <= LowPowerMaximumMegawatts;
        return Result(check, satisfied, power.HasValue
            ? FormattableString.Invariant($"Reactor thermal power: {power.Value:0.###} MWth")
            : "Reactor thermal power unavailable");
    }

    private static FirstCriticalityCheckResult EvaluateStablePeriod(ControlRoomSnapshot snapshot, FirstCriticalityCheckDefinition check)
    {
        var power = snapshot.ReactorCore.ReactorThermalPower.NumericValue;
        var reactivity = TotalReactivityPcm(snapshot);
        var period = snapshot.ReactorCore.ReactorPeriod.NumericValue;
        var nearCritical = reactivity.HasValue && Math.Abs(reactivity.Value) <= CriticalityTolerancePcm;
        var lowPower = power.HasValue && power.Value >= LowPowerMinimumMegawatts && power.Value <= LowPowerMaximumMegawatts;
        var periodStable = !period.HasValue || Math.Abs(period.Value) >= StablePeriodMinimumMagnitudeSeconds;
        var satisfied = nearCritical && lowPower && periodStable;
        var periodText = period.HasValue
            ? FormattableString.Invariant($"{period.Value:0.###} s")
            : "critical-equilibrium / effectively infinite";
        return Result(check, satisfied, $"Reactor period: {periodText}");
    }

    private static double? TotalReactivityPcm(ControlRoomSnapshot snapshot)
    {
        var rod = snapshot.ReactorCore.RodReactivity.NumericValue;
        var nonRod = snapshot.ReactorCore.NonRodReactivity.NumericValue;
        return rod.HasValue && nonRod.HasValue ? rod.Value + nonRod.Value : null;
    }

    private static FirstCriticalityCheckResult Result(
        FirstCriticalityCheckDefinition check,
        bool satisfied,
        string observation)
        => new(check, satisfied, observation);
}
