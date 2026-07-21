using NuclearReactorSimulator.Application.ControlRoom;

namespace NuclearReactorSimulator.Application.Scenarios.Operations;

/// <summary>Observational M7.6 manoeuvring/shutdown checks over immutable control-room presentation snapshots.</summary>
public sealed class PowerManoeuvringChecklistEvaluator
{
    private const double SynchronousSpeedMinimumRpm = 2_980d;
    private const double SynchronousSpeedMaximumRpm = 3_020d;
    private const double LowLoadMinimumMWe = 0.1d;
    private const double LowLoadMaximumMWe = 10d;
    private const double IncreasedLoadMinimumMWe = 9.5d;
    private const double IncreasedLoadMaximumMWe = 100d;
    private const double ReducedLoadMaximumMWe = 5.5d;
    private const double UnloadedToleranceMWe = 0.001d;
    private const double ShutdownPowerMaximumMWth = 0.1d;
    private const double InsertedRodMaximumPercent = 5d;

    public IReadOnlyList<PowerManoeuvringCheckResult> Evaluate(
        ControlRoomSnapshot snapshot,
        IEnumerable<PowerManoeuvringCheckDefinition> checks)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(checks);
        return checks.Select(check => Evaluate(snapshot, check ?? throw new ArgumentException(
            "Power-manoeuvring checks cannot contain null entries.", nameof(checks)))).ToArray();
    }

    public PowerManoeuvringCheckResult Evaluate(ControlRoomSnapshot snapshot, PowerManoeuvringCheckDefinition check)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(check);

        return check.Condition switch
        {
            PowerManoeuvringCheckCondition.MeasuredSignalsHealthy => Result(check,
                snapshot.TotalMeasuredSignalCount > 0 && snapshot.InvalidMeasuredSignalCount == 0,
                $"Measured signals: {snapshot.ValidMeasuredSignalCount}/{snapshot.TotalMeasuredSignalCount} valid"),
            PowerManoeuvringCheckCondition.ProtectionClear => Result(check, !snapshot.AnyTripActive,
                snapshot.AnyTripActive ? "One or more protection trips are active" : "No reactor/turbine/generator trip active"),
            PowerManoeuvringCheckCondition.MainCirculationPumpsRunning => EvaluatePumps(snapshot, check),
            PowerManoeuvringCheckCondition.GeneratorBreakersClosed => EvaluateBreakers(snapshot, check, expectedClosed: true),
            PowerManoeuvringCheckCondition.StableLowLoadParallelOperation => EvaluateStableLowLoad(snapshot, check),
            PowerManoeuvringCheckCondition.IncreasedElectricalLoadEstablished => EvaluateLoadBand(snapshot, check, IncreasedLoadMinimumMWe, IncreasedLoadMaximumMWe, "Increased"),
            PowerManoeuvringCheckCondition.ReducedElectricalLoadEstablished => EvaluateLoadBand(snapshot, check, 0d, ReducedLoadMaximumMWe, "Reduced"),
            PowerManoeuvringCheckCondition.TemperatureFeedbackObservable => EvaluateTemperatureFeedback(snapshot, check),
            PowerManoeuvringCheckCondition.VoidFeedbackObservable => EvaluateVoidFeedback(snapshot, check),
            PowerManoeuvringCheckCondition.XenonBoundaryExplicit => Result(check,
                snapshot.ReactorCore.XenonReactivity.State == ControlRoomVisualState.Unavailable
                    && !snapshot.ReactorCore.XenonReactivity.NumericValue.HasValue,
                snapshot.ReactorCore.XenonReactivity.State == ControlRoomVisualState.Unavailable
                    ? "Xenon reactivity remains explicitly unavailable at the M5.7 operational snapshot boundary"
                    : "Unexpected quantitative xenon value published"),
            PowerManoeuvringCheckCondition.GeneratorUnloaded => EvaluateUnloaded(snapshot, check),
            PowerManoeuvringCheckCondition.GeneratorBreakersOpen => EvaluateBreakers(snapshot, check, expectedClosed: false),
            PowerManoeuvringCheckCondition.ReactorShutdownEstablished => EvaluateReactorShutdown(snapshot, check),
            PowerManoeuvringCheckCondition.PostShutdownCoolingEstablished => EvaluatePostShutdownCooling(snapshot, check),
            _ => throw new ArgumentOutOfRangeException(nameof(check), check.Condition, "Unsupported power-manoeuvring check condition."),
        };
    }

    private static PowerManoeuvringCheckResult EvaluatePumps(ControlRoomSnapshot snapshot, PowerManoeuvringCheckDefinition check)
    {
        var pumps = snapshot.PrimaryCircuit.Pumps;
        var running = pumps.Count(static pump => pump.IsRunning);
        return Result(check, pumps.Count > 0 && running == pumps.Count, $"Main-circulation pumps running: {running}/{pumps.Count}");
    }

    private static PowerManoeuvringCheckResult EvaluateBreakers(ControlRoomSnapshot snapshot, PowerManoeuvringCheckDefinition check, bool expectedClosed)
    {
        var generators = snapshot.Electrical.Generators;
        var matching = generators.Count(generator => generator.BreakerClosed == expectedClosed);
        return Result(check, generators.Count > 0 && matching == generators.Count,
            expectedClosed ? $"Closed generator breakers: {matching}/{generators.Count}" : $"Open generator breakers: {matching}/{generators.Count}");
    }

    private static PowerManoeuvringCheckResult EvaluateStableLowLoad(ControlRoomSnapshot snapshot, PowerManoeuvringCheckDefinition check)
    {
        var breakerClosed = snapshot.Electrical.Generators.Count > 0 && snapshot.Electrical.Generators.All(static generator => generator.BreakerClosed);
        var output = snapshot.Electrical.GrossElectricalOutput.NumericValue;
        var speeds = snapshot.TurbineSecondary.Rotors.Select(static rotor => rotor.Speed.NumericValue).Where(static speed => speed.HasValue).Select(static speed => speed!.Value).ToArray();
        var speedStable = speeds.Length > 0 && speeds.All(static speed => speed >= SynchronousSpeedMinimumRpm && speed <= SynchronousSpeedMaximumRpm);
        var satisfied = !snapshot.AnyTripActive && breakerClosed && speedStable && output.HasValue
            && output.Value >= LowLoadMinimumMWe && output.Value <= LowLoadMaximumMWe;
        return Result(check, satisfied, output.HasValue
            ? FormattableString.Invariant($"Parallel low load {output.Value:0.###} MWe · speed stable={speedStable}")
            : "Low-load electrical output unavailable");
    }

    private static PowerManoeuvringCheckResult EvaluateLoadBand(
        ControlRoomSnapshot snapshot,
        PowerManoeuvringCheckDefinition check,
        double minimumMWe,
        double maximumMWe,
        string label)
    {
        var output = snapshot.Electrical.GrossElectricalOutput.NumericValue;
        return Result(check, output.HasValue && output.Value >= minimumMWe && output.Value <= maximumMWe,
            output.HasValue ? FormattableString.Invariant($"{label} gross electrical load: {output.Value:0.###} MWe") : "Gross electrical output unavailable");
    }

    private static PowerManoeuvringCheckResult EvaluateTemperatureFeedback(ControlRoomSnapshot snapshot, PowerManoeuvringCheckDefinition check)
    {
        var zones = snapshot.ReactorCore.Zones;
        var satisfied = zones.Count > 0 && zones.All(static zone => double.IsFinite(zone.FuelTemperatureCelsius) && double.IsFinite(zone.CoolantTemperatureCelsius));
        return Result(check, satisfied, zones.Count == 0
            ? "No core-zone temperature diagnostics published"
            : FormattableString.Invariant($"Core-zone temperatures observable across {zones.Count} zone(s); fuel {zones.Min(static zone => zone.FuelTemperatureCelsius):0.0}–{zones.Max(static zone => zone.FuelTemperatureCelsius):0.0} °C"));
    }

    private static PowerManoeuvringCheckResult EvaluateVoidFeedback(ControlRoomSnapshot snapshot, PowerManoeuvringCheckDefinition check)
    {
        var zones = snapshot.ReactorCore.Zones;
        var values = zones.Where(static zone => zone.VoidPercent.HasValue).Select(static zone => zone.VoidPercent!.Value).ToArray();
        var satisfied = zones.Count > 0 && values.Length == zones.Count && values.All(double.IsFinite);
        return Result(check, satisfied, values.Length == 0
            ? "Core-zone void diagnostics unavailable"
            : FormattableString.Invariant($"Core-zone void observable: {values.Min():0.###}–{values.Max():0.###}%"));
    }

    private static PowerManoeuvringCheckResult EvaluateUnloaded(ControlRoomSnapshot snapshot, PowerManoeuvringCheckDefinition check)
    {
        var output = snapshot.Electrical.GrossElectricalOutput.NumericValue;
        return Result(check, output.HasValue && Math.Abs(output.Value) <= UnloadedToleranceMWe,
            output.HasValue ? FormattableString.Invariant($"Gross electrical output: {output.Value:0.######} MWe") : "Gross electrical output unavailable");
    }

    private static PowerManoeuvringCheckResult EvaluateReactorShutdown(ControlRoomSnapshot snapshot, PowerManoeuvringCheckDefinition check)
    {
        var power = snapshot.ReactorCore.ReactorThermalPower.NumericValue;
        var rods = snapshot.ReactorCore.Rods;
        var maximumRod = rods.Count == 0 ? double.NaN : rods.Max(static rod => rod.PercentWithdrawn);
        var satisfied = power.HasValue && power.Value <= ShutdownPowerMaximumMWth
            && rods.Count > 0 && maximumRod <= InsertedRodMaximumPercent;
        return Result(check, satisfied, power.HasValue
            ? FormattableString.Invariant($"Reactor {power.Value:0.######} MWth · maximum rod withdrawal {maximumRod:0.###}%")
            : "Reactor shutdown state unavailable");
    }

    private static PowerManoeuvringCheckResult EvaluatePostShutdownCooling(ControlRoomSnapshot snapshot, PowerManoeuvringCheckDefinition check)
    {
        var power = snapshot.ReactorCore.ReactorThermalPower.NumericValue;
        var pumps = snapshot.PrimaryCircuit.Pumps;
        var running = pumps.Count(static pump => pump.IsRunning);
        var breakersOpen = snapshot.Electrical.Generators.Count > 0 && snapshot.Electrical.Generators.All(static generator => !generator.BreakerClosed);
        var satisfied = power.HasValue && power.Value <= ShutdownPowerMaximumMWth
            && pumps.Count > 0 && running == pumps.Count && breakersOpen;
        return Result(check, satisfied, power.HasValue
            ? FormattableString.Invariant($"Post-shutdown cooling: reactor {power.Value:0.######} MWth · MCP {running}/{pumps.Count} · breakers open={breakersOpen}")
            : "Post-shutdown cooling state unavailable");
    }

    private static PowerManoeuvringCheckResult Result(PowerManoeuvringCheckDefinition definition, bool satisfied, string observation)
        => new(definition, satisfied, observation);
}
