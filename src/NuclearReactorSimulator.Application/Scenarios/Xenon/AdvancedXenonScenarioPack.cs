using NuclearReactorSimulator.Application.ControlRoom;

namespace NuclearReactorSimulator.Application.Scenarios.Xenon;

/// <summary>
/// M9.3 built-in scenarios over the canonical M2.8 iodine/xenon owner. Scenario data defines only initial conditions,
/// objectives and allowed operator actions; it never prescribes poison trajectories, reactivity curves or outcomes.
/// </summary>
public static class AdvancedXenonScenarioPack
{
    private static readonly ControlRoomCommandKind[] ReactorOnlyActions =
    {
        ControlRoomCommandKind.ReactorScram,
        ControlRoomCommandKind.ProtectionReset,
        ControlRoomCommandKind.ControlRodInsert,
        ControlRoomCommandKind.ControlRodHold,
        ControlRoomCommandKind.ControlRodWithdraw,
        ControlRoomCommandKind.MainCirculationPumpStart,
        ControlRoomCommandKind.MainCirculationPumpStop,
        ControlRoomCommandKind.AlarmAcknowledge,
        ControlRoomCommandKind.AlarmReset,
        ControlRoomCommandKind.AlarmAcknowledgeAll,
        ControlRoomCommandKind.AlarmResetAll,
    };

    public static InitialConditionReference RestartInitialCondition { get; } = new("post-shutdown-xenon-restart-window", 1);

    public static InitialConditionReference LowPowerInitialCondition { get; } = new("poisoned-low-power-operation", 1);

    public static ScenarioDefinition RestartAfterShutdown { get; } = new(
        "xenon-restart-after-shutdown",
        "Restart After Shutdown — Xenon Challenge",
        "Attempt a controlled restart from a versioned post-shutdown poison state while observing canonical xenon reactivity, reactor period and rod margin. Xenon evolves only from M2.8 state and kinetics history.",
        RestartInitialCondition,
        new[]
        {
            new ScenarioObjectiveDefinition("observe-poison", "Observe xenon poisoning", "Use the promoted canonical xenon-reactivity indication; do not infer a scripted recovery time or target curve."),
            new ScenarioObjectiveDefinition("manage-reactivity", "Manage reactivity margin", "Use deliberate rod motion and HOLD while monitoring total reactivity and period as iodine/xenon state evolves."),
            new ScenarioObjectiveDefinition("establish-low-power", "Establish controlled low power", "Reach and stabilize educational low power only when the modeled poison/reactivity state permits it."),
            new ScenarioObjectiveDefinition("preserve-safety", "Preserve protection and cooling", "Keep main circulation available and retain SCRAM/protection as the fail-safe response to an uncontrolled approach."),
        },
        ReactorOnlyActions);

    public static ScenarioDefinition LowPowerPoisoningChallenge { get; } = new(
        "low-power-xenon-poisoning-challenge",
        "Low-Power Xenon Manoeuvring Challenge",
        "Hold and manoeuvre a poisoned low-power reactor while canonical I-135/Xe-135 memory changes the non-rod reactivity contribution. No target power or xenon trajectory is forced by the scenario.",
        LowPowerInitialCondition,
        new[]
        {
            new ScenarioObjectiveDefinition("observe-xenon", "Track xenon reactivity", "Observe the signed canonical xenon contribution together with rod and total reactivity."),
            new ScenarioObjectiveDefinition("hold-low-power", "Maintain controlled low power", "Use small rod corrections and HOLD rather than chasing a predetermined poison curve."),
            new ScenarioObjectiveDefinition("recognize-history", "Recognize history dependence", "Relate changing xenon worth to prior poison inventory and current neutron population without treating time alone as a causal script."),
            new ScenarioObjectiveDefinition("avoid-excursion", "Avoid uncontrolled excursion", "Respect reactor period, protection status and rod-withdrawal interlocks throughout the manoeuvre."),
        },
        ReactorOnlyActions);

    public static IReadOnlyList<ScenarioDefinition> All { get; } = new[]
    {
        RestartAfterShutdown,
        LowPowerPoisoningChallenge,
    };


}
