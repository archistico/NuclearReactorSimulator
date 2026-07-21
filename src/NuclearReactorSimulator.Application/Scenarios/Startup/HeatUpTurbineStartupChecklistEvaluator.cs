using NuclearReactorSimulator.Application.ControlRoom;

namespace NuclearReactorSimulator.Application.Scenarios.Startup;

/// <summary>
/// Observational M7.4 readiness/progress checks over immutable presentation snapshots. These checks never open valves,
/// accelerate the turbine, modify reactor power or alter conserved inventories.
/// </summary>
public sealed class HeatUpTurbineStartupChecklistEvaluator
{
    private const double ValveClosedTolerancePercent = 0.1d;
    private const double ValveOpenMinimumPercent = 99d;
    private const double HeatingPowerMinimumMegawatts = 0.01d;
    private const double HeatingPowerMaximumMegawatts = 20d;
    private const double SteamPressureMinimumMegapascals = 0.1d;
    private const double MinimumDrumLevelPercent = 5d;
    private const double MaximumDrumLevelPercent = 105d;
    private const double StoppedSpeedMaximumRpm = 1d;
    private const double WarmupSpeedMinimumRpm = 100d;
    private const double WarmupSpeedMaximumRpm = 2_800d;
    private const double NearSynchronousSpeedMinimumRpm = 2_850d;
    private const double NearSynchronousSpeedMaximumRpm = 3_050d;
    private const double UnloadedElectricalOutputToleranceMWe = 0.001d;

    public IReadOnlyList<HeatUpTurbineStartupCheckResult> Evaluate(
        ControlRoomSnapshot snapshot,
        IEnumerable<HeatUpTurbineStartupCheckDefinition> checks)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(checks);
        return checks.Select(check => Evaluate(snapshot, check ?? throw new ArgumentException(
            "Heat-up/startup checks cannot contain null entries.", nameof(checks)))).ToArray();
    }

    public HeatUpTurbineStartupCheckResult Evaluate(
        ControlRoomSnapshot snapshot,
        HeatUpTurbineStartupCheckDefinition check)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(check);

        return check.Condition switch
        {
            HeatUpTurbineStartupCheckCondition.MeasuredSignalsHealthy => Result(
                check,
                snapshot.TotalMeasuredSignalCount > 0 && snapshot.InvalidMeasuredSignalCount == 0,
                $"Measured signals: {snapshot.ValidMeasuredSignalCount}/{snapshot.TotalMeasuredSignalCount} valid"),
            HeatUpTurbineStartupCheckCondition.ProtectionClear => Result(
                check,
                !snapshot.AnyTripActive,
                snapshot.AnyTripActive ? "One or more protection trips are active" : "No reactor/turbine/generator trip active"),
            HeatUpTurbineStartupCheckCondition.MainCirculationPumpsRunning => EvaluatePumps(snapshot, check),
            HeatUpTurbineStartupCheckCondition.ReactorHeatingPowerEstablished => EvaluateHeatingPower(snapshot, check),
            HeatUpTurbineStartupCheckCondition.SteamRaisingPressureEstablished => EvaluateSteamPressure(snapshot, check),
            HeatUpTurbineStartupCheckCondition.SteamDrumInventoryAvailable => EvaluateDrumInventory(snapshot, check),
            HeatUpTurbineStartupCheckCondition.TurbineStartupLineupReady => EvaluateStartupLineup(snapshot, check),
            HeatUpTurbineStartupCheckCondition.TurbineStopped => EvaluateTurbineSpeed(snapshot, check, 0d, StoppedSpeedMaximumRpm),
            HeatUpTurbineStartupCheckCondition.TurbineRolling => EvaluateTurbineSpeed(snapshot, check, StoppedSpeedMaximumRpm, NearSynchronousSpeedMinimumRpm),
            HeatUpTurbineStartupCheckCondition.TurbineWarmupSpeedBand => EvaluateTurbineSpeed(snapshot, check, WarmupSpeedMinimumRpm, WarmupSpeedMaximumRpm),
            HeatUpTurbineStartupCheckCondition.TurbineNearSynchronousSpeed => EvaluateTurbineSpeed(snapshot, check, NearSynchronousSpeedMinimumRpm, NearSynchronousSpeedMaximumRpm),
            HeatUpTurbineStartupCheckCondition.GeneratorBreakersOpen => EvaluateBreakers(snapshot, check),
            HeatUpTurbineStartupCheckCondition.GeneratorUnloaded => EvaluateGeneratorLoad(snapshot, check),
            _ => throw new ArgumentOutOfRangeException(nameof(check), check.Condition, "Unsupported heat-up/startup check condition."),
        };
    }

    private static HeatUpTurbineStartupCheckResult EvaluatePumps(ControlRoomSnapshot snapshot, HeatUpTurbineStartupCheckDefinition check)
    {
        var pumps = snapshot.PrimaryCircuit.Pumps;
        var running = pumps.Count(static pump => pump.IsRunning);
        return Result(check, pumps.Count > 0 && running == pumps.Count, $"Main-circulation pumps running: {running}/{pumps.Count}");
    }

    private static HeatUpTurbineStartupCheckResult EvaluateHeatingPower(ControlRoomSnapshot snapshot, HeatUpTurbineStartupCheckDefinition check)
    {
        var power = snapshot.ReactorCore.ReactorThermalPower.NumericValue;
        var satisfied = power.HasValue && power.Value >= HeatingPowerMinimumMegawatts && power.Value <= HeatingPowerMaximumMegawatts;
        return Result(check, satisfied, power.HasValue
            ? FormattableString.Invariant($"Reactor thermal power: {power.Value:0.###} MWth")
            : "Reactor thermal power unavailable");
    }

    private static HeatUpTurbineStartupCheckResult EvaluateSteamPressure(ControlRoomSnapshot snapshot, HeatUpTurbineStartupCheckDefinition check)
    {
        var pressures = snapshot.PrimaryCircuit.SteamDrums
            .Select(static drum => drum.Pressure.NumericValue)
            .Where(static value => value.HasValue)
            .Select(static value => value!.Value)
            .ToArray();
        var minimum = pressures.DefaultIfEmpty(double.NaN).Min();
        return Result(check, pressures.Length > 0 && pressures.All(static pressure => pressure >= SteamPressureMinimumMegapascals),
            double.IsNaN(minimum)
                ? "Steam-drum pressure unavailable"
                : FormattableString.Invariant($"Minimum steam-drum pressure: {minimum:0.###} MPa"));
    }

    private static HeatUpTurbineStartupCheckResult EvaluateDrumInventory(ControlRoomSnapshot snapshot, HeatUpTurbineStartupCheckDefinition check)
    {
        var levels = snapshot.PrimaryCircuit.SteamDrums
            .Select(static drum => drum.Level.NumericValue)
            .Where(static value => value.HasValue)
            .Select(static value => value!.Value)
            .ToArray();
        var satisfied = levels.Length > 0 && levels.All(static level => level >= MinimumDrumLevelPercent && level <= MaximumDrumLevelPercent);
        return Result(check, satisfied, levels.Length == 0
            ? "Steam-drum level unavailable"
            : FormattableString.Invariant($"Steam-drum level range: {levels.Min():0.##}–{levels.Max():0.##}%"));
    }

    private static HeatUpTurbineStartupCheckResult EvaluateStartupLineup(ControlRoomSnapshot snapshot, HeatUpTurbineStartupCheckDefinition check)
    {
        var trains = snapshot.TurbineSecondary.AdmissionTrains;
        var satisfied = trains.Count > 0 && trains.All(static train =>
            (train.StopValvePosition.NumericValue ?? double.NaN) >= ValveOpenMinimumPercent
            && Math.Abs(train.ControlValvePosition.NumericValue ?? double.NaN) <= ValveClosedTolerancePercent
            && (train.AdmissionValvePosition.NumericValue ?? double.NaN) >= ValveOpenMinimumPercent);
        return Result(check, satisfied, trains.Count == 0
            ? "No turbine-admission train published"
            : "Stop/admission valves available; governing control valve closed for roll-off");
    }

    private static HeatUpTurbineStartupCheckResult EvaluateTurbineSpeed(
        ControlRoomSnapshot snapshot,
        HeatUpTurbineStartupCheckDefinition check,
        double exclusiveMinimumRpm,
        double inclusiveMaximumRpm)
    {
        var speeds = snapshot.TurbineSecondary.Rotors
            .Select(static rotor => rotor.Speed.NumericValue)
            .Where(static value => value.HasValue)
            .Select(static value => value!.Value)
            .ToArray();
        var satisfied = speeds.Length > 0 && speeds.All(speed => speed > exclusiveMinimumRpm && speed <= inclusiveMaximumRpm);
        if (exclusiveMinimumRpm == 0d)
        {
            satisfied = speeds.Length > 0 && speeds.All(speed => speed >= 0d && speed <= inclusiveMaximumRpm);
        }
        return Result(check, satisfied, speeds.Length == 0
            ? "Turbine speed unavailable"
            : FormattableString.Invariant($"Turbine speed range: {speeds.Min():0.###}–{speeds.Max():0.###} rpm"));
    }

    private static HeatUpTurbineStartupCheckResult EvaluateBreakers(ControlRoomSnapshot snapshot, HeatUpTurbineStartupCheckDefinition check)
    {
        var generators = snapshot.Electrical.Generators;
        var closed = generators.Count(static generator => generator.BreakerClosed);
        return Result(check, generators.Count > 0 && closed == 0, $"Closed generator breakers: {closed}/{generators.Count}");
    }

    private static HeatUpTurbineStartupCheckResult EvaluateGeneratorLoad(ControlRoomSnapshot snapshot, HeatUpTurbineStartupCheckDefinition check)
    {
        var generators = snapshot.Electrical.Generators;
        var breakersOpen = generators.Count > 0 && generators.All(static generator => !generator.BreakerClosed);
        var outputs = generators
            .Select(static generator => generator.ElectricalOutput.NumericValue)
            .Where(static value => value.HasValue)
            .Select(static value => Math.Abs(value!.Value))
            .ToArray();
        var measuredOutputsAcceptable = outputs.Length == 0 || outputs.All(static output => output <= UnloadedElectricalOutputToleranceMWe);
        var maximum = outputs.DefaultIfEmpty(double.NaN).Max();
        var observation = double.IsNaN(maximum)
            ? "Generator breakers open; electrical-output measurement unavailable and not inferred"
            : FormattableString.Invariant($"Generator breakers open; maximum measured electrical output: {maximum:0.######} MWe");
        return Result(check, breakersOpen && measuredOutputsAcceptable, observation);
    }

    private static HeatUpTurbineStartupCheckResult Result(
        HeatUpTurbineStartupCheckDefinition definition,
        bool satisfied,
        string observation)
        => new(definition, satisfied, observation);
}
