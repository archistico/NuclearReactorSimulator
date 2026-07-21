using NuclearReactorSimulator.Application.ControlRoom;

namespace NuclearReactorSimulator.Application.Scenarios.PreStartup;

/// <summary>
/// Built-in M7.2 educational cold-shutdown/pre-start scenario metadata and guidance. It references an immutable initial-
/// condition version supplied by the Application composition boundary and deliberately stops before rod withdrawal/criticality (M7.3 ownership).
/// </summary>
public static class ColdShutdownPreStartupProgram
{
    public static InitialConditionReference InitialCondition { get; } = new("cold-shutdown-pre-start", 1);

    public static ScenarioDefinition Scenario { get; } = new(
        "cold-shutdown-pre-start",
        "Cold Shutdown & Pre-Startup",
        "Verify a cold, subcritical, isolated plant and prepare main circulation before first-criticality training.",
        InitialCondition,
        new[]
        {
            new ScenarioObjectiveDefinition("verify-cold-shutdown", "Verify cold shutdown", "Confirm shutdown reactivity, stopped turbine, open generator breaker and healthy instrumentation."),
            new ScenarioObjectiveDefinition("prepare-circulation", "Prepare main circulation", "Establish the modeled main-circulation pump condition while preserving shutdown and isolation boundaries."),
        },
        new[]
        {
            ControlRoomCommandKind.ReactorScram,
            ControlRoomCommandKind.ProtectionReset,
            ControlRoomCommandKind.MainCirculationPumpStart,
            ControlRoomCommandKind.MainCirculationPumpStop,
            ControlRoomCommandKind.TurbineTrip,
            ControlRoomCommandKind.GeneratorTrip,
            ControlRoomCommandKind.GeneratorBreakerOpen,
            ControlRoomCommandKind.AlarmAcknowledge,
            ControlRoomCommandKind.AlarmReset,
            ControlRoomCommandKind.AlarmAcknowledgeAll,
            ControlRoomCommandKind.AlarmResetAll,
        });

    public static PreStartupGuidancePlan Guidance { get; } = new(
        new[]
        {
            Check("signals-healthy", "Instrumentation healthy", "All published measured channels required by the current educational model are valid.", PreStartupCheckCondition.MeasuredSignalsHealthy),
            Check("protection-clear", "Protection clear", "No reactor, turbine or generator trip is active.", PreStartupCheckCondition.ProtectionClear),
            Check("reactor-shutdown", "Reactor shutdown", "Thermal power remains at the modeled shutdown baseline.", PreStartupCheckCondition.ReactorShutdown),
            Check("rods-inserted", "Control rods inserted", "All modeled control rods are fully inserted within the presentation tolerance.", PreStartupCheckCondition.ControlRodsInserted),
            Check("mcp-stopped", "Main circulation stopped", "All modeled main-circulation pumps are stopped in the loaded cold-shutdown condition.", PreStartupCheckCondition.MainCirculationPumpsStopped),
            Check("mcp-running", "Main circulation established", "All modeled main-circulation pumps are running for the pre-criticality handoff.", PreStartupCheckCondition.MainCirculationPumpsRunning),
            Check("turbine-stopped", "Turbine stopped", "All modeled turbine rotors are stationary.", PreStartupCheckCondition.TurbineStopped),
            Check("breakers-open", "Generator isolated", "All generator breakers are open.", PreStartupCheckCondition.GeneratorBreakersOpen),
            Check("steam-isolated", "Steam path isolated", "Stop, control and admission valves are closed.", PreStartupCheckCondition.SteamIsolationClosed),
            Check("alarms-clear", "Annunciator clear", "No alarms are annunciated at the cold-shutdown baseline.", PreStartupCheckCondition.NoAnnunciatedAlarms),
        },
        new[]
        {
            new PreStartupPreparationStepDefinition(
                "verify-safe-baseline", 1, "Verify cold-shutdown baseline",
                "Confirm the reactor is shutdown with rods inserted, turbine stopped, generator isolated, protection clear and healthy measurements.",
                new[] { "signals-healthy", "protection-clear", "reactor-shutdown", "rods-inserted", "turbine-stopped", "breakers-open" }),
            new PreStartupPreparationStepDefinition(
                "verify-isolation", 2, "Verify steam isolation and auxiliaries",
                "Confirm the steam admission path is closed, the annunciator is clear and the main-circulation pump is initially stopped.",
                new[] { "steam-isolated", "alarms-clear", "mcp-stopped" }),
            new PreStartupPreparationStepDefinition(
                "establish-main-circulation", 3, "Establish main circulation",
                "Issue the main-circulation pump start command, then advance deterministic simulation steps until the running indication is established.",
                new[] { "mcp-running" },
                ControlRoomCommandKind.MainCirculationPumpStart),
            new PreStartupPreparationStepDefinition(
                "precriticality-handoff", 4, "Confirm pre-criticality handoff",
                "Before continuing to M7.3 training, confirm circulation is established while shutdown, isolation and electrical separation remain intact.",
                new[] { "signals-healthy", "protection-clear", "reactor-shutdown", "rods-inserted", "mcp-running", "turbine-stopped", "breakers-open", "steam-isolated" }),
        });

    private static PreStartupCheckDefinition Check(
        string id,
        string title,
        string description,
        PreStartupCheckCondition condition)
        => new(id, title, description, condition);
}
