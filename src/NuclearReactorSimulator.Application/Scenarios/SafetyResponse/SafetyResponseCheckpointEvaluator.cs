using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios.Faults;
using NuclearReactorSimulator.Application.Scenarios.Training;

namespace NuclearReactorSimulator.Application.Scenarios.SafetyResponse;

/// <summary>
/// M8.7 observational acceptance-check evaluator. It consumes presentation-safe committed snapshots only and never
/// changes fault, protection, control or physical state.
/// </summary>
public sealed class SafetyResponseCheckpointEvaluator : ITrainingCheckpointEvaluator
{
    public const string AnyTripActiveCheckId = "protection:any-trip-active";
    public const string ReactorScramActiveCheckId = "protection:reactor-scram-active";
    public const string GeneratorBreakerOpenCheckId = "electrical:generator-breaker-open";
    public const string InvalidMeasurementPresentCheckId = "instrumentation:invalid-measurement-present";
    public const string AlarmAnnunciatedCheckId = "alarms:annunciated";
    public const string AlarmsAcknowledgedCheckId = "alarms:all-annunciated-acknowledged";
    public const string FaultActivePrefix = "fault-active:";

    public TrainingCheckpointObservation Evaluate(ControlRoomSnapshot snapshot, TrainingCheckpointDefinition checkpoint)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(checkpoint);

        if (checkpoint.SourceCheckId.StartsWith(FaultActivePrefix, StringComparison.Ordinal))
        {
            var faultId = checkpoint.SourceCheckId[FaultActivePrefix.Length..];
            var fault = snapshot.Faults.Faults.FirstOrDefault(item => string.Equals(item.FaultId, faultId, StringComparison.Ordinal));
            var active = fault?.Lifecycle == ScenarioFaultLifecycleState.Active;
            return new TrainingCheckpointObservation(
                active,
                fault is null
                    ? $"Fault '{faultId}' is not present in the committed scenario fault snapshot."
                    : $"Fault '{faultId}' lifecycle is {fault.Lifecycle} at logical step {snapshot.LogicalStep}.");
        }

        return checkpoint.SourceCheckId switch
        {
            AnyTripActiveCheckId => Observation(snapshot.AnyTripActive, snapshot.AnyTripActive
                ? "At least one canonical protection trip is active."
                : "No canonical protection trip is active."),
            ReactorScramActiveCheckId => Observation(snapshot.ReactorScramActive, snapshot.ReactorScramActive
                ? "Canonical reactor SCRAM latch is active."
                : "Canonical reactor SCRAM latch is not active."),
            GeneratorBreakerOpenCheckId => EvaluateGeneratorBreakerOpen(snapshot),
            InvalidMeasurementPresentCheckId => Observation(snapshot.InvalidMeasuredSignalCount > 0,
                $"Invalid measured signals: {snapshot.InvalidMeasuredSignalCount} of {snapshot.TotalMeasuredSignalCount}."),
            AlarmAnnunciatedCheckId => Observation(snapshot.AnnunciatedAlarmCount > 0,
                $"Annunciated alarms: {snapshot.AnnunciatedAlarmCount}."),
            AlarmsAcknowledgedCheckId => Observation(
                snapshot.AnnunciatedAlarmCount > 0 && snapshot.UnacknowledgedAlarmCount == 0,
                $"Annunciated alarms: {snapshot.AnnunciatedAlarmCount}; unacknowledged: {snapshot.UnacknowledgedAlarmCount}."),
            _ => throw new KeyNotFoundException(
                $"Safety-response checkpoint '{checkpoint.CheckpointId}' references unknown source check '{checkpoint.SourceCheckId}'."),
        };
    }

    private static TrainingCheckpointObservation EvaluateGeneratorBreakerOpen(ControlRoomSnapshot snapshot)
    {
        var generators = snapshot.Electrical.Generators;
        var satisfied = generators.Count > 0 && generators.All(static generator => !generator.BreakerClosed);
        return Observation(
            satisfied,
            generators.Count == 0
                ? "No generator presentation target is available."
                : satisfied
                    ? "All published canonical generator breakers are open."
                    : "At least one published canonical generator breaker remains closed.");
    }

    private static TrainingCheckpointObservation Observation(bool satisfied, string text)
        => new(satisfied, text);
}
