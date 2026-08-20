using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Faults;
using NuclearReactorSimulator.Application.Scenarios.Operations;
using NuclearReactorSimulator.Application.Scenarios.PreStartup;
using NuclearReactorSimulator.Application.Scenarios.Synchronization;

namespace NuclearReactorSimulator.Application.Scenarios.Challenges.Packs;

/// <summary>
/// Read-only M10.9.6.4 evaluator for the initial challenge pack. It deliberately reuses validated M7/M8 checklist and
/// committed-snapshot evidence rather than defining new plant physics, protection behavior or command authority.
/// </summary>
public sealed class StandardOperationalChallengeConditionEvaluator : IChallengeConditionEvaluator
{
    private const double SynchronousSpeedMinimumRpm = 2_980d;
    private const double SynchronousSpeedMaximumRpm = 3_020d;

    private static readonly PreStartupChecklistEvaluator PreStartupEvaluator = new();
    private static readonly GridSynchronizationChecklistEvaluator SynchronizationEvaluator = new();
    private static readonly PowerManoeuvringChecklistEvaluator ManoeuvringEvaluator = new();

    public ChallengeConditionObservation Evaluate(
        ChallengeConditionDefinition condition,
        ControlRoomSnapshot snapshot,
        IReadOnlyList<ScenarioOperatorActionRecord> acceptedOperatorActions)
    {
        ArgumentNullException.ThrowIfNull(condition);
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(acceptedOperatorActions);

        var result = condition.ConditionId switch
        {
            "prestart:safe-baseline" => PreStartupChecks(snapshot, "signals-healthy", "protection-clear", "reactor-shutdown", "rods-inserted", "turbine-stopped", "breakers-open", "steam-isolated"),
            "prestart:pump-start-observed" => ActionObserved(acceptedOperatorActions, ControlRoomCommandKind.MainCirculationPumpStart),
            "prestart:precriticality-handoff" => PreStartupChecks(snapshot, "signals-healthy", "protection-clear", "reactor-shutdown", "rods-inserted", "mcp-running", "turbine-stopped", "breakers-open", "steam-isolated"),
            "prestart:unexpected-trip" => Flag(snapshot.AnyTripActive, snapshot.AnyTripActive ? "A canonical protection trip is active during normal pre-start preparation." : "No canonical protection trip is active."),

            "sync:ready-to-synchronize" => SynchronizationChecks(snapshot, "signals-healthy", "protection-clear", "mcp-running", "reactor-power", "sync-speed", "sync-window", "breakers-open", "generator-unloaded"),
            "sync:breaker-close-observed" => ActionObserved(acceptedOperatorActions, ControlRoomCommandKind.GeneratorBreakerClose),
            "sync:stable-low-load-handoff" => SynchronizationChecks(snapshot, "m76-handoff", "power-coordinated", "mcp-running"),
            "sync:unexpected-trip" => Flag(snapshot.AnyTripActive, snapshot.AnyTripActive ? "A canonical protection trip is active during the normal synchronization challenge." : "No canonical protection trip is active."),

            "demand:stable-low-load-start" => ManoeuvringChecks(snapshot, "low-load", "protection-clear", "mcp-running", "breakers-closed"),
            "demand:raise-and-lower-actions-observed" => ActionsObserved(acceptedOperatorActions, ControlRoomCommandKind.GeneratorLoadRaise, ControlRoomCommandKind.GeneratorLoadLower),
            "demand:return-to-five-mwe-stable" => StableElectricalOutput(snapshot, 4.5d, 5.5d, requireNoTrip: true),
            "demand:unexpected-trip" => Flag(snapshot.AnyTripActive, snapshot.AnyTripActive ? "A canonical protection trip is active during normal demand-following." : "No canonical protection trip is active."),

            "stabilize:load-raise-observed" => ActionObserved(acceptedOperatorActions, ControlRoomCommandKind.GeneratorLoadRaise),
            "stabilize:thermal-feedback-observed" => ManoeuvringChecks(snapshot, "temperature-feedback", "void-feedback"),
            "stabilize:ten-mwe-stable" => StableElectricalOutput(snapshot, 9.5d, 10.5d, requireNoTrip: true),
            "stabilize:unexpected-trip" => Flag(snapshot.AnyTripActive, snapshot.AnyTripActive ? "A canonical protection trip is active during normal post-load-change stabilization." : "No canonical protection trip is active."),

            "shutdown:stable-low-load-start" => ManoeuvringChecks(snapshot, "low-load", "protection-clear", "mcp-running", "breakers-closed"),
            "shutdown:normal-action-sequence-observed" => ActionsObserved(acceptedOperatorActions, ControlRoomCommandKind.GeneratorLoadLower, ControlRoomCommandKind.GeneratorBreakerOpen, ControlRoomCommandKind.ControlRodInsert),
            "shutdown:post-shutdown-cooling" => ManoeuvringChecks(snapshot, "post-shutdown-cooling", "temperature-feedback"),
            "shutdown:emergency-action-used" => AnyActionObserved(acceptedOperatorActions, ControlRoomCommandKind.ReactorScram, ControlRoomCommandKind.TurbineTrip, ControlRoomCommandKind.GeneratorTrip),

            "load-rejection:fault-active" => FaultActive(snapshot, "m84-generator-trip-event"),
            "load-rejection:generator-trip-observed" => Flag(snapshot.GeneratorTripActive, snapshot.GeneratorTripActive ? "Canonical generator-trip latch is active." : "Generator-trip latch is not active."),
            "load-rejection:breaker-open" => GeneratorBreakersOpen(snapshot),
            "load-rejection:alarm-acknowledgement-observed" => AnyActionObserved(acceptedOperatorActions, ControlRoomCommandKind.AlarmAcknowledge, ControlRoomCommandKind.AlarmAcknowledgeAll),
            "load-rejection:isolated-response-stable" => IsolatedGeneratorTripResponse(snapshot, acceptedOperatorActions),

            _ => throw new KeyNotFoundException($"Initial operational challenge condition '{condition.ConditionId}' is not authored by M10.9.6.4."),
        };

        return new ChallengeConditionObservation(condition.ConditionId, result.IsSatisfied, snapshot.LogicalStep, result.Evidence);
    }

    private static ConditionResult PreStartupChecks(ControlRoomSnapshot snapshot, params string[] ids)
    {
        var definitions = ids.Select(id => FindPreStartupCheck(id)).ToArray();
        var results = PreStartupEvaluator.Evaluate(snapshot, definitions);
        return Aggregate(results.Select(static item => (item.IsSatisfied, item.Observation)));
    }

    private static ConditionResult SynchronizationChecks(ControlRoomSnapshot snapshot, params string[] ids)
    {
        var definitions = ids.Select(id => FindSynchronizationCheck(id)).ToArray();
        var results = SynchronizationEvaluator.Evaluate(snapshot, definitions);
        return Aggregate(results.Select(static item => (item.IsSatisfied, item.Observation)));
    }

    private static ConditionResult ManoeuvringChecks(ControlRoomSnapshot snapshot, params string[] ids)
    {
        var definitions = ids.Select(id => FindManoeuvringCheck(id)).ToArray();
        var results = ManoeuvringEvaluator.Evaluate(snapshot, definitions);
        return Aggregate(results.Select(static item => (item.IsSatisfied, item.Observation)));
    }

    private static ConditionResult StableElectricalOutput(ControlRoomSnapshot snapshot, double minimumMWe, double maximumMWe, bool requireNoTrip)
    {
        var output = snapshot.Electrical.GrossElectricalOutput.NumericValue;
        var speeds = snapshot.TurbineSecondary.Rotors
            .Select(static rotor => rotor.Speed.NumericValue)
            .Where(static value => value.HasValue)
            .Select(static value => value!.Value)
            .ToArray();
        var speedStable = speeds.Length > 0 && speeds.All(static value => value >= SynchronousSpeedMinimumRpm && value <= SynchronousSpeedMaximumRpm);
        var breakersClosed = snapshot.Electrical.Generators.Count > 0 && snapshot.Electrical.Generators.All(static generator => generator.BreakerClosed);
        var tripSatisfied = !requireNoTrip || !snapshot.AnyTripActive;
        var satisfied = output.HasValue
            && output.Value >= minimumMWe
            && output.Value <= maximumMWe
            && speedStable
            && breakersClosed
            && tripSatisfied;
        var text = output.HasValue
            ? FormattableString.Invariant($"Gross output {output.Value:0.###} MWe; speed-stable={speedStable}; breakers-closed={breakersClosed}; no-trip={!snapshot.AnyTripActive}.")
            : "Gross electrical output unavailable.";
        return Flag(satisfied, text);
    }

    private static ConditionResult FaultActive(ControlRoomSnapshot snapshot, string faultId)
    {
        var fault = snapshot.Faults.Faults.FirstOrDefault(item => string.Equals(item.FaultId, faultId, StringComparison.Ordinal));
        var active = fault?.Lifecycle == ScenarioFaultLifecycleState.Active;
        return Flag(active, fault is null
            ? $"Fault '{faultId}' is not present in the committed fault snapshot."
            : $"Fault '{faultId}' lifecycle is {fault.Lifecycle}.");
    }

    private static ConditionResult GeneratorBreakersOpen(ControlRoomSnapshot snapshot)
    {
        var generators = snapshot.Electrical.Generators;
        var open = generators.Count > 0 && generators.All(static generator => !generator.BreakerClosed);
        return Flag(open, generators.Count == 0
            ? "No generator presentation target is available."
            : open ? "All canonical generator breakers are open." : "At least one canonical generator breaker remains closed.");
    }

    private static ConditionResult IsolatedGeneratorTripResponse(
        ControlRoomSnapshot snapshot,
        IReadOnlyList<ScenarioOperatorActionRecord> actions)
    {
        var breakers = GeneratorBreakersOpen(snapshot);
        var acknowledgement = AnyActionObserved(actions, ControlRoomCommandKind.AlarmAcknowledge, ControlRoomCommandKind.AlarmAcknowledgeAll);
        var satisfied = snapshot.GeneratorTripActive && breakers.IsSatisfied && acknowledgement.IsSatisfied;
        return Flag(
            satisfied,
            $"Generator-trip-active={snapshot.GeneratorTripActive}; {breakers.Evidence}; alarm-acknowledgement-observed={acknowledgement.IsSatisfied}.");
    }

    private static ConditionResult ActionObserved(IReadOnlyList<ScenarioOperatorActionRecord> actions, ControlRoomCommandKind kind)
        => AnyActionObserved(actions, kind);

    private static ConditionResult ActionsObserved(IReadOnlyList<ScenarioOperatorActionRecord> actions, params ControlRoomCommandKind[] requiredKinds)
    {
        var missing = requiredKinds.Where(kind => !actions.Any(action => action.Command.Kind == kind)).ToArray();
        return Flag(
            missing.Length == 0,
            missing.Length == 0
                ? $"Accepted operator-action history contains: {string.Join(", ", requiredKinds)}."
                : $"Accepted operator-action history is missing: {string.Join(", ", missing)}.");
    }

    private static ConditionResult AnyActionObserved(IReadOnlyList<ScenarioOperatorActionRecord> actions, params ControlRoomCommandKind[] kinds)
    {
        var match = actions.LastOrDefault(action => kinds.Contains(action.Command.Kind));
        return Flag(
            match is not null,
            match is null
                ? $"No accepted operator action observed for: {string.Join(", ", kinds)}."
                : $"Accepted action {match.Command.Kind} observed at logical step {match.LogicalStep} (sequence {match.Sequence}).");
    }

    private static PreStartupCheckDefinition FindPreStartupCheck(string id)
        => ColdShutdownPreStartupProgram.Guidance.Checks.FirstOrDefault(item => string.Equals(item.CheckId, id, StringComparison.Ordinal))
            ?? throw new KeyNotFoundException($"Pre-start check '{id}' is not defined by the validated M7.2 guidance contract.");

    private static GridSynchronizationCheckDefinition FindSynchronizationCheck(string id)
        => GridSynchronizationLoadProgram.Guidance.Checks.FirstOrDefault(item => string.Equals(item.CheckId, id, StringComparison.Ordinal))
            ?? throw new KeyNotFoundException($"Synchronization check '{id}' is not defined by the validated M7.5 guidance contract.");

    private static PowerManoeuvringCheckDefinition FindManoeuvringCheck(string id)
        => PowerManoeuvringNormalShutdownProgram.Guidance.Checks.FirstOrDefault(item => string.Equals(item.CheckId, id, StringComparison.Ordinal))
            ?? throw new KeyNotFoundException($"Power-manoeuvring check '{id}' is not defined by the validated M7.6 guidance contract.");

    private static ConditionResult Aggregate(IEnumerable<(bool IsSatisfied, string Evidence)> items)
    {
        var materialized = items.ToArray();
        return Flag(materialized.All(static item => item.IsSatisfied), string.Join(" | ", materialized.Select(static item => item.Evidence)));
    }

    private static ConditionResult Flag(bool satisfied, string evidence) => new(satisfied, evidence);

    private readonly record struct ConditionResult(bool IsSatisfied, string Evidence);
}
