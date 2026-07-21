using NuclearReactorSimulator.Application.ControlRoom;

namespace NuclearReactorSimulator.Application.Scenarios.Criticality;

/// <summary>
/// Built-in M7.3 scenario metadata and declarative first-criticality guidance. All rod motion remains operator-commanded
/// through the M5.3 seam; the program never auto-withdraws rods, forces reactivity or substitutes hidden true-state data.
/// </summary>
public static class FirstCriticalityLowPowerProgram
{
    public static InitialConditionReference InitialCondition { get; } = new("pre-criticality-source-range", 1);

    public static ScenarioDefinition Scenario { get; } = new(
        "first-criticality-low-power",
        "First Criticality & Low-Power Operation",
        "Approach first criticality from a prepared source-range condition, then stabilize the reactor at educational low power while preserving turbine/grid isolation.",
        InitialCondition,
        new[]
        {
            new ScenarioObjectiveDefinition("verify-precriticality-handoff", "Verify pre-criticality handoff", "Confirm healthy measurements, clear protection, established main circulation, steam isolation and generator disconnection before rod withdrawal."),
            new ScenarioObjectiveDefinition("approach-criticality", "Approach criticality deliberately", "Use controlled rod motion while observing modeled reactivity and reactor period; stop withdrawal before an excessive positive-reactivity excursion."),
            new ScenarioObjectiveDefinition("establish-first-criticality", "Establish first criticality", "Hold rod motion near zero modeled reactivity with a non-zero neutron population."),
            new ScenarioObjectiveDefinition("stabilize-low-power", "Stabilize low-power operation", "Use small insert/hold/withdraw corrections to reach and maintain the educational low-power band with a long or effectively infinite period."),
            new ScenarioObjectiveDefinition("respect-xenon-boundary", "Respect the xenon observation boundary", "Recognize that quantitative xenon reactivity remains explicitly unavailable in the current M5.7 operational snapshot and must not be inferred or fabricated."),
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
            ControlRoomCommandKind.TurbineTrip,
            ControlRoomCommandKind.GeneratorTrip,
            ControlRoomCommandKind.GeneratorBreakerOpen,
            ControlRoomCommandKind.AlarmAcknowledge,
            ControlRoomCommandKind.AlarmReset,
            ControlRoomCommandKind.AlarmAcknowledgeAll,
            ControlRoomCommandKind.AlarmResetAll,
        });

    public static FirstCriticalityGuidancePlan Guidance { get; } = new(
        new[]
        {
            Check("signals-healthy", "Instrumentation healthy", "All published measured channels required by the educational runtime are valid.", FirstCriticalityCheckCondition.MeasuredSignalsHealthy),
            Check("protection-clear", "Protection clear", "No reactor, turbine or generator trip is active.", FirstCriticalityCheckCondition.ProtectionClear),
            Check("mcp-running", "Main circulation established", "All modeled main-circulation pumps are running at the M7.2 handoff.", FirstCriticalityCheckCondition.MainCirculationPumpsRunning),
            Check("steam-isolated", "Steam path isolated", "Stop, control and admission valves remain closed during M7.3.", FirstCriticalityCheckCondition.SteamIsolationClosed),
            Check("breakers-open", "Generator isolated", "All generator breakers remain open.", FirstCriticalityCheckCondition.GeneratorBreakersOpen),
            Check("withdrawal-permitted", "Rod withdrawal permitted", "Protection interlocks do not currently inhibit rod withdrawal.", FirstCriticalityCheckCondition.RodWithdrawalPermitted),
            Check("source-range", "Source-range power present", "A small deterministic non-zero neutron population is observable before the approach.", FirstCriticalityCheckCondition.SourceRangePowerEstablished),
            Check("approach-window", "Near-critical approach window", "Modeled total reactivity is negative but within the final approach window.", FirstCriticalityCheckCondition.ApproachToCriticality),
            Check("critical", "First criticality established", "Modeled total reactivity is approximately zero while neutron population remains non-zero.", FirstCriticalityCheckCondition.CriticalityEstablished),
            Check("low-power", "Educational low-power band", "Reactor thermal power is between 0.01 and 5 MWth.", FirstCriticalityCheckCondition.LowPowerBand),
            Check("stable-period", "Low-power period stabilized", "At low power, modeled reactivity is near zero and reactor period is long or effectively infinite.", FirstCriticalityCheckCondition.StableLowPowerPeriod),
            Check("xenon-boundary", "Xenon boundary explicit", "Quantitative xenon reactivity remains unavailable rather than being reconstructed outside its authoritative owner.", FirstCriticalityCheckCondition.XenonBoundaryExplicit),
        },
        new[]
        {
            new FirstCriticalityStepDefinition(
                "verify-precriticality-handoff", 1, "Verify pre-criticality handoff",
                "Before rod motion, confirm circulation, isolation, electrical separation, protection status, source-range indication and rod-withdrawal permissive.",
                new[] { "signals-healthy", "protection-clear", "mcp-running", "steam-isolated", "breakers-open", "withdrawal-permitted", "source-range" }),
            new FirstCriticalityStepDefinition(
                "controlled-approach", 2, "Withdraw toward criticality",
                "Select the canonical rod/group target and withdraw in controlled increments. Observe total reactivity and reactor period; use HOLD between increments and INSERT or SCRAM if the approach is not controlled.",
                new[] { "approach-window" },
                ControlRoomCommandKind.ControlRodWithdraw),
            new FirstCriticalityStepDefinition(
                "first-criticality", 3, "Hold at first criticality",
                "Issue HOLD as modeled reactivity approaches zero. Confirm a non-zero neutron population and approximately zero total reactivity without opening the steam path or connecting the generator.",
                new[] { "critical", "mcp-running", "steam-isolated", "breakers-open" },
                ControlRoomCommandKind.ControlRodHold),
            new FirstCriticalityStepDefinition(
                "low-power-stabilization", 4, "Raise and stabilize low power",
                "Use small deliberate rod corrections with HOLD between movements to enter the educational low-power band, then return reactivity near zero and verify a long or effectively infinite reactor period.",
                new[] { "low-power", "stable-period", "protection-clear", "mcp-running" },
                ControlRoomCommandKind.ControlRodWithdraw),
            new FirstCriticalityStepDefinition(
                "m74-handoff", 5, "Confirm M7.4 handoff",
                "Confirm stable low-power operation with circulation established, steam admission still isolated and generator disconnected. Quantitative xenon remains explicitly unavailable at this boundary.",
                new[] { "low-power", "stable-period", "steam-isolated", "breakers-open", "xenon-boundary" }),
        });

    private static FirstCriticalityCheckDefinition Check(
        string id,
        string title,
        string description,
        FirstCriticalityCheckCondition condition)
        => new(id, title, description, condition);
}
