using NuclearReactorSimulator.Application.ControlRoom;

namespace NuclearReactorSimulator.Application.Scenarios.PreStartup;

/// <summary>
/// Evaluates M7.2 readiness only from the immutable UI-safe presentation snapshot. The evaluator is observational: it
/// cannot change controller inputs, protection state or physical inventories.
/// </summary>
public sealed class PreStartupChecklistEvaluator
{
    private const double ShutdownThermalPowerToleranceMegawatts = 0.01d;
    private const double InsertedRodTolerancePercent = 0.1d;
    private const double StoppedRotorToleranceRpm = 1d;
    private const double ClosedValveTolerancePercent = 0.1d;

    public IReadOnlyList<PreStartupCheckResult> Evaluate(
        ControlRoomSnapshot snapshot,
        IEnumerable<PreStartupCheckDefinition> checks)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(checks);

        return checks.Select(check => Evaluate(snapshot, check ?? throw new ArgumentException(
            "Pre-start checks cannot contain null entries.", nameof(checks)))).ToArray();
    }

    public PreStartupCheckResult Evaluate(ControlRoomSnapshot snapshot, PreStartupCheckDefinition check)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(check);

        return check.Condition switch
        {
            PreStartupCheckCondition.MeasuredSignalsHealthy => Result(
                check,
                snapshot.TotalMeasuredSignalCount > 0 && snapshot.InvalidMeasuredSignalCount == 0,
                $"Measured signals: {snapshot.ValidMeasuredSignalCount}/{snapshot.TotalMeasuredSignalCount} valid"),
            PreStartupCheckCondition.ProtectionClear => Result(
                check,
                !snapshot.AnyTripActive,
                snapshot.AnyTripActive ? "One or more protection trips are active" : "No reactor/turbine/generator trip active"),
            PreStartupCheckCondition.ReactorShutdown => EvaluateReactorShutdown(snapshot, check),
            PreStartupCheckCondition.ControlRodsInserted => EvaluateRods(snapshot, check),
            PreStartupCheckCondition.MainCirculationPumpsStopped => EvaluatePumps(snapshot, check, expectRunning: false),
            PreStartupCheckCondition.MainCirculationPumpsRunning => EvaluatePumps(snapshot, check, expectRunning: true),
            PreStartupCheckCondition.TurbineStopped => EvaluateTurbine(snapshot, check),
            PreStartupCheckCondition.GeneratorBreakersOpen => EvaluateBreakers(snapshot, check),
            PreStartupCheckCondition.SteamIsolationClosed => EvaluateSteamIsolation(snapshot, check),
            PreStartupCheckCondition.NoAnnunciatedAlarms => Result(
                check,
                snapshot.AnnunciatedAlarmCount == 0,
                $"Annunciated alarms: {snapshot.AnnunciatedAlarmCount}"),
            _ => throw new ArgumentOutOfRangeException(nameof(check), check.Condition, "Unsupported pre-start check condition."),
        };
    }

    private static PreStartupCheckResult EvaluateReactorShutdown(ControlRoomSnapshot snapshot, PreStartupCheckDefinition check)
    {
        var value = snapshot.ReactorCore.ReactorThermalPower.NumericValue;
        var satisfied = value.HasValue && Math.Abs(value.Value) <= ShutdownThermalPowerToleranceMegawatts;
        return Result(check, satisfied, value.HasValue
            ? FormattableString.Invariant($"Reactor thermal power: {value.Value:0.###} MWth")
            : "Reactor thermal power unavailable");
    }

    private static PreStartupCheckResult EvaluateRods(ControlRoomSnapshot snapshot, PreStartupCheckDefinition check)
    {
        var rods = snapshot.ReactorCore.Rods;
        var satisfied = rods.Count > 0 && rods.All(static rod => rod.PercentWithdrawn <= InsertedRodTolerancePercent);
        var maximum = rods.Count == 0 ? (double?)null : rods.Max(static rod => rod.PercentWithdrawn);
        return Result(check, satisfied, maximum.HasValue
            ? FormattableString.Invariant($"Maximum rod withdrawal: {maximum.Value:0.###}%")
            : "Control-rod presentation unavailable");
    }

    private static PreStartupCheckResult EvaluatePumps(
        ControlRoomSnapshot snapshot,
        PreStartupCheckDefinition check,
        bool expectRunning)
    {
        var pumps = snapshot.PrimaryCircuit.Pumps;
        var satisfied = pumps.Count > 0 && pumps.All(pump => pump.IsRunning == expectRunning);
        var running = pumps.Count(static pump => pump.IsRunning);
        return Result(check, satisfied, $"Main-circulation pumps running: {running}/{pumps.Count}");
    }

    private static PreStartupCheckResult EvaluateTurbine(ControlRoomSnapshot snapshot, PreStartupCheckDefinition check)
    {
        var rotors = snapshot.TurbineSecondary.Rotors;
        var speeds = rotors.Select(static rotor => rotor.Speed.NumericValue).ToArray();
        var satisfied = speeds.Length > 0 && speeds.All(static speed => speed.HasValue && Math.Abs(speed.Value) <= StoppedRotorToleranceRpm);
        var maximum = speeds.Where(static speed => speed.HasValue).Select(static speed => Math.Abs(speed!.Value)).DefaultIfEmpty(double.NaN).Max();
        return Result(check, satisfied, double.IsNaN(maximum)
            ? "Turbine speed unavailable"
            : FormattableString.Invariant($"Maximum turbine speed: {maximum:0.###} rpm"));
    }

    private static PreStartupCheckResult EvaluateBreakers(ControlRoomSnapshot snapshot, PreStartupCheckDefinition check)
    {
        var generators = snapshot.Electrical.Generators;
        var closed = generators.Count(static generator => generator.BreakerClosed);
        return Result(check, generators.Count > 0 && closed == 0, $"Closed generator breakers: {closed}/{generators.Count}");
    }

    private static PreStartupCheckResult EvaluateSteamIsolation(ControlRoomSnapshot snapshot, PreStartupCheckDefinition check)
    {
        var trains = snapshot.TurbineSecondary.AdmissionTrains;
        var positions = trains.SelectMany(static train => new[]
        {
            train.StopValvePosition.NumericValue,
            train.ControlValvePosition.NumericValue,
            train.AdmissionValvePosition.NumericValue,
        }).ToArray();
        var satisfied = trains.Count > 0
            && positions.All(static position => position.HasValue && Math.Abs(position.Value) <= ClosedValveTolerancePercent);
        var maximum = positions.Where(static position => position.HasValue).Select(static position => Math.Abs(position!.Value)).DefaultIfEmpty(double.NaN).Max();
        return Result(check, satisfied, double.IsNaN(maximum)
            ? "Steam-isolation valve positions unavailable"
            : FormattableString.Invariant($"Maximum admission-path valve opening: {maximum:0.###}%"));
    }

    private static PreStartupCheckResult Result(PreStartupCheckDefinition check, bool satisfied, string observation)
        => new(check, satisfied, observation);
}
