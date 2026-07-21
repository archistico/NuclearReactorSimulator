using NuclearReactorSimulator.Application.ControlRoom;

namespace NuclearReactorSimulator.Application.Scenarios.Startup;

/// <summary>
/// Built-in M7.4 startup scenario. Heat-up and turbine rolling remain consequences of canonical reactor/steam/turbine
/// physics. Guidance is observational and turbine acceleration is requested only through the validated M6.7/M5.4 speed seam.
/// </summary>
public static class HeatUpTurbineStartupProgram
{
    public static InitialConditionReference InitialCondition { get; } = new("low-power-steam-raising", 1);

    public static ScenarioDefinition Scenario { get; } = new(
        "heat-up-steam-raising-turbine-startup",
        "Heat-Up, Steam Raising & Turbine Startup",
        "Continue from a stable low-power reactor condition, establish usable steam conditions and roll the turbine toward synchronization speed while keeping the generator breaker open and electrical load at zero.",
        InitialCondition,
        new[]
        {
            new ScenarioObjectiveDefinition("verify-hot-handoff", "Verify low-power hot handoff", "Confirm healthy instrumentation, protection clear, main circulation established, usable drum inventory and generator isolation."),
            new ScenarioObjectiveDefinition("raise-steam", "Establish steam conditions", "Maintain controlled reactor heating power while observing steam-drum pressure and inventory through canonical thermofluid behavior."),
            new ScenarioObjectiveDefinition("roll-turbine", "Roll and warm the turbine", "Use the validated turbine-speed command seam to open governing admission and accelerate the rotor deliberately."),
            new ScenarioObjectiveDefinition("approach-sync-speed", "Approach synchronization speed", "Stabilize the turbine near synchronous speed with the generator breaker still open and no electrical load."),
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
            ControlRoomCommandKind.GeneratorBreakerOpen,
            ControlRoomCommandKind.AlarmAcknowledge,
            ControlRoomCommandKind.AlarmReset,
            ControlRoomCommandKind.AlarmAcknowledgeAll,
            ControlRoomCommandKind.AlarmResetAll,
        });

    public static HeatUpTurbineStartupGuidancePlan Guidance { get; } = new(
        new[]
        {
            Check("signals-healthy", "Instrumentation healthy", "All measured channels required by the startup runtime are valid.", HeatUpTurbineStartupCheckCondition.MeasuredSignalsHealthy),
            Check("protection-clear", "Protection clear", "No reactor, turbine or generator trip is active.", HeatUpTurbineStartupCheckCondition.ProtectionClear),
            Check("mcp-running", "Main circulation established", "All modeled main-circulation pumps remain running.", HeatUpTurbineStartupCheckCondition.MainCirculationPumpsRunning),
            Check("heating-power", "Controlled heating power", "Reactor thermal power remains in the educational heat-up envelope.", HeatUpTurbineStartupCheckCondition.ReactorHeatingPowerEstablished),
            Check("steam-pressure", "Steam pressure available", "All modeled steam drums have reached the minimum educational pressure for turbine roll-off.", HeatUpTurbineStartupCheckCondition.SteamRaisingPressureEstablished),
            Check("drum-inventory", "Steam-drum inventory available", "Steam-drum level remains within the broad modeled startup inventory envelope.", HeatUpTurbineStartupCheckCondition.SteamDrumInventoryAvailable),
            Check("startup-lineup", "Turbine startup lineup ready", "The versioned startup lineup has stop/admission availability with the governing control valve initially closed.", HeatUpTurbineStartupCheckCondition.TurbineStartupLineupReady),
            Check("turbine-stopped", "Turbine stopped", "Rotor speed is effectively zero before the first speed-raise command.", HeatUpTurbineStartupCheckCondition.TurbineStopped),
            Check("turbine-rolling", "Turbine rolling", "Rotor speed is above turning/stopped condition and below the synchronization approach window.", HeatUpTurbineStartupCheckCondition.TurbineRolling),
            Check("warmup-speed", "Turbine warm-up band", "Rotor speed is within the educational rolling/warm-up band.", HeatUpTurbineStartupCheckCondition.TurbineWarmupSpeedBand),
            Check("near-sync-speed", "Near synchronous speed", "Rotor speed is within the educational 3000 rpm synchronization approach window.", HeatUpTurbineStartupCheckCondition.TurbineNearSynchronousSpeed),
            Check("breakers-open", "Generator isolated", "All generator breakers remain open throughout M7.4.", HeatUpTurbineStartupCheckCondition.GeneratorBreakersOpen),
            Check("generator-unloaded", "Generator unloaded", "Electrical output remains effectively zero before M7.5 synchronization.", HeatUpTurbineStartupCheckCondition.GeneratorUnloaded),
        },
        new[]
        {
            new HeatUpTurbineStartupStepDefinition(
                "verify-hot-handoff", 1, "Verify low-power hot handoff",
                "Confirm instrumentation, protection, circulation, controlled reactor heating power, drum inventory and electrical isolation before turbine startup actions.",
                new[] { "signals-healthy", "protection-clear", "mcp-running", "heating-power", "drum-inventory", "breakers-open", "generator-unloaded" }),
            new HeatUpTurbineStartupStepDefinition(
                "raise-steam", 2, "Heat up and raise steam",
                "Maintain deliberate low reactor power with small rod corrections and HOLD between movements. Observe drum pressure and inventory; do not force pressure or inventory from the scenario layer.",
                new[] { "heating-power", "steam-pressure", "drum-inventory", "protection-clear", "mcp-running" },
                ControlRoomCommandKind.ControlRodHold),
            new HeatUpTurbineStartupStepDefinition(
                "verify-startup-lineup", 3, "Verify turbine startup lineup",
                "Confirm the versioned M7.4 lineup: stop/admission path available, governing control valve closed, turbine stopped, generator breaker open and zero electrical load.",
                new[] { "startup-lineup", "turbine-stopped", "breakers-open", "generator-unloaded" }),
            new HeatUpTurbineStartupStepDefinition(
                "roll-turbine", 4, "Roll the turbine",
                "Select the canonical turbine rotor and use TURBINE SPEED RAISE in deliberate increments. The validated speed controller governs the control valve; use SPEED LOWER or TURBINE TRIP if the roll is not controlled.",
                new[] { "turbine-rolling", "protection-clear", "steam-pressure", "drum-inventory" },
                ControlRoomCommandKind.TurbineSpeedRaise),
            new HeatUpTurbineStartupStepDefinition(
                "warmup", 5, "Warm and accelerate",
                "Continue staged speed increases while monitoring rotor speed, steam conditions, drum inventory and protection. Stabilize within the warm-up band before approaching synchronous speed.",
                new[] { "warmup-speed", "protection-clear", "steam-pressure", "drum-inventory" },
                ControlRoomCommandKind.TurbineSpeedRaise),
            new HeatUpTurbineStartupStepDefinition(
                "m75-handoff", 6, "Confirm M7.5 handoff",
                "Hold the turbine near synchronous speed with all generator breakers open and electrical output at zero. Synchronization, breaker closure and load increase belong exclusively to M7.5.",
                new[] { "near-sync-speed", "breakers-open", "generator-unloaded", "protection-clear" },
                ControlRoomCommandKind.TurbineSpeedLower),
        });

    private static HeatUpTurbineStartupCheckDefinition Check(
        string id,
        string title,
        string description,
        HeatUpTurbineStartupCheckCondition condition)
        => new(id, title, description, condition);
}
