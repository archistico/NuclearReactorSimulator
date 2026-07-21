using NuclearReactorSimulator.Application.ControlRoom;

namespace NuclearReactorSimulator.Application.Scenarios.Synchronization;

/// <summary>Observational M7.5 synchronization/load checks over immutable presentation snapshots.</summary>
public sealed class GridSynchronizationChecklistEvaluator
{
    private const double SynchronousSpeedMinimumRpm = 2_990d;
    private const double SynchronousSpeedMaximumRpm = 3_010d;
    private const double ReactorPowerMinimumMegawatts = 0.01d;
    private const double UnloadedToleranceMWe = 0.001d;
    private const double InitialLoadMinimumMWe = 0.1d;
    private const double InitialLoadMaximumMWe = 100d;

    public IReadOnlyList<GridSynchronizationCheckResult> Evaluate(
        ControlRoomSnapshot snapshot,
        IEnumerable<GridSynchronizationCheckDefinition> checks)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(checks);
        return checks.Select(check => Evaluate(snapshot, check ?? throw new ArgumentException(
            "Synchronization checks cannot contain null entries.", nameof(checks)))).ToArray();
    }

    public GridSynchronizationCheckResult Evaluate(ControlRoomSnapshot snapshot, GridSynchronizationCheckDefinition check)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(check);

        return check.Condition switch
        {
            GridSynchronizationCheckCondition.MeasuredSignalsHealthy => Result(check,
                snapshot.TotalMeasuredSignalCount > 0 && snapshot.InvalidMeasuredSignalCount == 0,
                $"Measured signals: {snapshot.ValidMeasuredSignalCount}/{snapshot.TotalMeasuredSignalCount} valid"),
            GridSynchronizationCheckCondition.ProtectionClear => Result(check, !snapshot.AnyTripActive,
                snapshot.AnyTripActive ? "One or more protection trips are active" : "No reactor/turbine/generator trip active"),
            GridSynchronizationCheckCondition.MainCirculationPumpsRunning => EvaluatePumps(snapshot, check),
            GridSynchronizationCheckCondition.ReactorPowerAvailable => EvaluateReactorPower(snapshot, check),
            GridSynchronizationCheckCondition.TurbineAtSynchronousSpeed => EvaluateTurbineSpeed(snapshot, check),
            GridSynchronizationCheckCondition.SynchronizationWindowSatisfied => EvaluateSynchronization(snapshot, check),
            GridSynchronizationCheckCondition.GeneratorBreakersOpen => EvaluateBreakers(snapshot, check, expectedClosed: false),
            GridSynchronizationCheckCondition.GeneratorBreakersClosed => EvaluateBreakers(snapshot, check, expectedClosed: true),
            GridSynchronizationCheckCondition.GeneratorUnloaded => EvaluateUnloaded(snapshot, check),
            GridSynchronizationCheckCondition.InitialElectricalLoadEstablished => EvaluateInitialLoad(snapshot, check),
            GridSynchronizationCheckCondition.ReactorPowerSupportsElectricalLoad => EvaluatePowerCoordination(snapshot, check),
            GridSynchronizationCheckCondition.StableLowLoadHandoff => EvaluateStableHandoff(snapshot, check),
            _ => throw new ArgumentOutOfRangeException(nameof(check), check.Condition, "Unsupported synchronization check condition."),
        };
    }

    private static GridSynchronizationCheckResult EvaluatePumps(ControlRoomSnapshot snapshot, GridSynchronizationCheckDefinition check)
    {
        var pumps = snapshot.PrimaryCircuit.Pumps;
        var running = pumps.Count(static pump => pump.IsRunning);
        return Result(check, pumps.Count > 0 && running == pumps.Count, $"Main-circulation pumps running: {running}/{pumps.Count}");
    }

    private static GridSynchronizationCheckResult EvaluateReactorPower(ControlRoomSnapshot snapshot, GridSynchronizationCheckDefinition check)
    {
        var power = snapshot.ReactorCore.ReactorThermalPower.NumericValue;
        return Result(check, power.HasValue && power.Value >= ReactorPowerMinimumMegawatts,
            power.HasValue ? FormattableString.Invariant($"Reactor thermal power: {power.Value:0.###} MWth") : "Reactor thermal power unavailable");
    }

    private static GridSynchronizationCheckResult EvaluateTurbineSpeed(ControlRoomSnapshot snapshot, GridSynchronizationCheckDefinition check)
    {
        var speeds = snapshot.TurbineSecondary.Rotors.Select(static rotor => rotor.Speed.NumericValue).Where(static value => value.HasValue).Select(static value => value!.Value).ToArray();
        var satisfied = speeds.Length > 0 && speeds.All(static speed => speed >= SynchronousSpeedMinimumRpm && speed <= SynchronousSpeedMaximumRpm);
        return Result(check, satisfied, speeds.Length == 0 ? "Turbine speed unavailable" : FormattableString.Invariant($"Turbine speed range: {speeds.Min():0.###}–{speeds.Max():0.###} rpm"));
    }

    private static GridSynchronizationCheckResult EvaluateSynchronization(ControlRoomSnapshot snapshot, GridSynchronizationCheckDefinition check)
    {
        var generators = snapshot.Electrical.Generators;
        var satisfied = generators.Count > 0 && generators.All(static generator => generator.SynchronizationConditionsSatisfied);
        var ready = generators.Count(static generator => generator.SynchronizationConditionsSatisfied);
        return Result(check, satisfied, $"Generators within canonical synchronization window: {ready}/{generators.Count}");
    }

    private static GridSynchronizationCheckResult EvaluateBreakers(ControlRoomSnapshot snapshot, GridSynchronizationCheckDefinition check, bool expectedClosed)
    {
        var generators = snapshot.Electrical.Generators;
        var matching = generators.Count(generator => generator.BreakerClosed == expectedClosed);
        return Result(check, generators.Count > 0 && matching == generators.Count,
            expectedClosed ? $"Closed generator breakers: {matching}/{generators.Count}" : $"Open generator breakers: {matching}/{generators.Count}");
    }

    private static GridSynchronizationCheckResult EvaluateUnloaded(ControlRoomSnapshot snapshot, GridSynchronizationCheckDefinition check)
    {
        var outputs = snapshot.Electrical.Generators.Select(static generator => generator.ElectricalOutput.NumericValue).Where(static value => value.HasValue).Select(static value => Math.Abs(value!.Value)).ToArray();
        var satisfied = outputs.Length > 0 && outputs.All(static output => output <= UnloadedToleranceMWe);
        return Result(check, satisfied, outputs.Length == 0 ? "Generator electrical output unavailable" : FormattableString.Invariant($"Maximum electrical output: {outputs.Max():0.######} MWe"));
    }

    private static GridSynchronizationCheckResult EvaluateInitialLoad(ControlRoomSnapshot snapshot, GridSynchronizationCheckDefinition check)
    {
        var outputs = snapshot.Electrical.Generators.Select(static generator => generator.ElectricalOutput.NumericValue).Where(static value => value.HasValue).Select(static value => value!.Value).ToArray();
        var satisfied = outputs.Length > 0 && outputs.All(static output => output >= InitialLoadMinimumMWe && output <= InitialLoadMaximumMWe);
        return Result(check, satisfied, outputs.Length == 0 ? "Generator electrical output unavailable" : FormattableString.Invariant($"Electrical output range: {outputs.Min():0.###}–{outputs.Max():0.###} MWe"));
    }

    private static GridSynchronizationCheckResult EvaluatePowerCoordination(ControlRoomSnapshot snapshot, GridSynchronizationCheckDefinition check)
    {
        var thermal = snapshot.ReactorCore.ReactorThermalPower.NumericValue;
        var electrical = snapshot.Electrical.GrossElectricalOutput.NumericValue;
        var satisfied = thermal.HasValue && electrical.HasValue && electrical.Value > 0d && thermal.Value > electrical.Value;
        return Result(check, satisfied, thermal.HasValue && electrical.HasValue
            ? FormattableString.Invariant($"Reactor {thermal.Value:0.###} MWth · gross electrical {electrical.Value:0.###} MWe")
            : "Thermal/electrical power comparison unavailable");
    }

    private static GridSynchronizationCheckResult EvaluateStableHandoff(ControlRoomSnapshot snapshot, GridSynchronizationCheckDefinition check)
    {
        var breakerClosed = snapshot.Electrical.Generators.Count > 0 && snapshot.Electrical.Generators.All(static generator => generator.BreakerClosed);
        var speeds = snapshot.TurbineSecondary.Rotors.Select(static rotor => rotor.Speed.NumericValue).Where(static value => value.HasValue).Select(static value => value!.Value).ToArray();
        var speedStable = speeds.Length > 0 && speeds.All(static speed => speed >= SynchronousSpeedMinimumRpm && speed <= SynchronousSpeedMaximumRpm);
        var output = snapshot.Electrical.GrossElectricalOutput.NumericValue;
        var loaded = output.HasValue && output.Value >= InitialLoadMinimumMWe && output.Value <= InitialLoadMaximumMWe;
        var satisfied = !snapshot.AnyTripActive && breakerClosed && speedStable && loaded;
        return Result(check, satisfied, output.HasValue
            ? FormattableString.Invariant($"Breaker parallel · gross output {output.Value:0.###} MWe · trip active={snapshot.AnyTripActive}")
            : "Low-load handoff not yet established");
    }

    private static GridSynchronizationCheckResult Result(GridSynchronizationCheckDefinition definition, bool satisfied, string observation)
        => new(definition, satisfied, observation);
}
