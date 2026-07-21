using NuclearReactorSimulator.Application.ControlRoom;

namespace NuclearReactorSimulator.Application.Scenarios.Synchronization;

/// <summary>Built-in M7.5 synchronization and first-load training program.</summary>
public static class GridSynchronizationLoadProgram
{
    public static InitialConditionReference InitialCondition { get; } = new("pre-synchronization-grid-loading", 1);

    public static ScenarioDefinition Scenario { get; } = new(
        "grid-synchronization-initial-loading",
        "Grid Synchronization & Load Increase",
        "Synchronize the generator through the canonical M4.5 close-check, close the breaker deliberately and establish an initial electrical load while coordinating reactor power and turbine governing through validated command seams.",
        InitialCondition,
        new[]
        {
            new ScenarioObjectiveDefinition("verify-sync-handoff", "Verify synchronization handoff", "Confirm healthy plant conditions, synchronous turbine speed, open breaker and zero electrical load."),
            new ScenarioObjectiveDefinition("synchronize", "Synchronize and parallel", "Use the published canonical synchronization permissive and close the generator breaker without bypassing M4.5 close checks."),
            new ScenarioObjectiveDefinition("take-initial-load", "Take initial electrical load", "Raise requested generator load in deliberate increments while maintaining turbine speed and sufficient reactor thermal power."),
            new ScenarioObjectiveDefinition("stabilize-low-load", "Stabilize low-load operation", "Reach a stable low-load parallel condition suitable for M7.6 power manoeuvring."),
        },
        new[]
        {
            ControlRoomCommandKind.ReactorScram,
            ControlRoomCommandKind.ProtectionReset,
            ControlRoomCommandKind.ControlRodInsert,
            ControlRoomCommandKind.ControlRodHold,
            ControlRoomCommandKind.ControlRodWithdraw,
            ControlRoomCommandKind.MainCirculationPumpStart,
            ControlRoomCommandKind.MainCirculationPumpStop,
            ControlRoomCommandKind.TurbineSpeedRaise,
            ControlRoomCommandKind.TurbineSpeedLower,
            ControlRoomCommandKind.TurbineTrip,
            ControlRoomCommandKind.GeneratorTrip,
            ControlRoomCommandKind.GeneratorBreakerClose,
            ControlRoomCommandKind.GeneratorBreakerOpen,
            ControlRoomCommandKind.GeneratorLoadRaise,
            ControlRoomCommandKind.GeneratorLoadLower,
            ControlRoomCommandKind.AlarmAcknowledge,
            ControlRoomCommandKind.AlarmReset,
            ControlRoomCommandKind.AlarmAcknowledgeAll,
            ControlRoomCommandKind.AlarmResetAll,
        });

    public static GridSynchronizationGuidancePlan Guidance { get; } = new(
        new[]
        {
            Check("signals-healthy", "Instrumentation healthy", "All measured channels required by the synchronization runtime are valid.", GridSynchronizationCheckCondition.MeasuredSignalsHealthy),
            Check("protection-clear", "Protection clear", "No reactor, turbine or generator trip is active.", GridSynchronizationCheckCondition.ProtectionClear),
            Check("mcp-running", "Main circulation established", "All modeled main-circulation pumps remain running.", GridSynchronizationCheckCondition.MainCirculationPumpsRunning),
            Check("reactor-power", "Reactor power available", "Positive controlled reactor thermal power is available before loading.", GridSynchronizationCheckCondition.ReactorPowerAvailable),
            Check("sync-speed", "Turbine at synchronous speed", "Rotor speed is within the narrow educational 3000 rpm synchronization band.", GridSynchronizationCheckCondition.TurbineAtSynchronousSpeed),
            Check("sync-window", "Synchronization window satisfied", "M4.5 frequency, phase and voltage close-check conditions are all satisfied.", GridSynchronizationCheckCondition.SynchronizationWindowSatisfied),
            Check("breakers-open", "Generator isolated", "All generator breakers are open before synchronization.", GridSynchronizationCheckCondition.GeneratorBreakersOpen),
            Check("breakers-closed", "Generator paralleled", "All generator breakers are closed after an accepted synchronization command.", GridSynchronizationCheckCondition.GeneratorBreakersClosed),
            Check("generator-unloaded", "Generator unloaded", "Electrical output is effectively zero before first load pickup.", GridSynchronizationCheckCondition.GeneratorUnloaded),
            Check("initial-load", "Initial electrical load established", "Gross electrical output is positive and remains within the educational low-load band.", GridSynchronizationCheckCondition.InitialElectricalLoadEstablished),
            Check("power-coordinated", "Thermal/electrical power coordinated", "Reactor thermal power exceeds gross electrical output while initial load is carried.", GridSynchronizationCheckCondition.ReactorPowerSupportsElectricalLoad),
            Check("m76-handoff", "Stable M7.6 handoff", "Breaker remains closed, turbine remains near synchronous speed, low load is carried and no trip is active.", GridSynchronizationCheckCondition.StableLowLoadHandoff),
        },
        new[]
        {
            new GridSynchronizationStepDefinition("verify-handoff", 1, "Verify pre-synchronization handoff", "Confirm instrumentation, protection, circulation, reactor power, synchronous rotor speed, open breaker and zero electrical output.", new[] { "signals-healthy", "protection-clear", "mcp-running", "reactor-power", "sync-speed", "breakers-open", "generator-unloaded" }),
            new GridSynchronizationStepDefinition("trim-sync", 2, "Establish synchronization window", "Observe the canonical M4.5 synchronization indication. Use small TURBINE SPEED RAISE/LOWER corrections only if needed; never infer or force phase/frequency from the scenario layer.", new[] { "sync-speed", "sync-window", "breakers-open", "protection-clear" }, ControlRoomCommandKind.TurbineSpeedRaise),
            new GridSynchronizationStepDefinition("close-breaker", 3, "Close generator breaker", "With the synchronization window satisfied, issue CLOSE BREAKER once. M4.5 remains authoritative and rejects closure outside its frequency/phase/voltage limits.", new[] { "sync-window", "breakers-closed", "protection-clear" }, ControlRoomCommandKind.GeneratorBreakerClose),
            new GridSynchronizationStepDefinition("verify-parallel", 4, "Verify parallel unloaded operation", "Confirm the breaker is closed and electrical output remains at approximately zero before applying load.", new[] { "breakers-closed", "generator-unloaded", "sync-speed", "protection-clear" }),
            new GridSynchronizationStepDefinition("take-load", 5, "Take initial electrical load", "Use GENERATOR LOAD RAISE in 5 MWe request increments. Coordinate small rod withdrawals/HOLD as required so reactor thermal power supports the requested electrical load while the validated turbine-speed governor maintains rotor speed.", new[] { "breakers-closed", "initial-load", "reactor-power", "protection-clear" }, ControlRoomCommandKind.GeneratorLoadRaise),
            new GridSynchronizationStepDefinition("coordinate-power", 6, "Coordinate reactor and turbine power", "Stabilize after each load increment. Use rod HOLD/withdrawal and generator load raise/lower deliberately; monitor turbine speed, trips and the thermal-to-electrical power relationship.", new[] { "initial-load", "power-coordinated", "sync-speed", "protection-clear" }, ControlRoomCommandKind.ControlRodHold),
            new GridSynchronizationStepDefinition("m76-handoff", 7, "Confirm M7.6 handoff", "Hold a stable low electrical load with the generator paralleled, turbine near synchronous speed and no active trip. Broader load manoeuvring and normal shutdown belong to M7.6.", new[] { "m76-handoff", "power-coordinated", "mcp-running" }, ControlRoomCommandKind.GeneratorLoadLower),
        });

    private static GridSynchronizationCheckDefinition Check(string id, string title, string description, GridSynchronizationCheckCondition condition)
        => new(id, title, description, condition);
}
