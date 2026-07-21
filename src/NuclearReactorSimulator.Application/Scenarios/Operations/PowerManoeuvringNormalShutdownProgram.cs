using NuclearReactorSimulator.Application.ControlRoom;

namespace NuclearReactorSimulator.Application.Scenarios.Operations;

/// <summary>Built-in M7.6 power-manoeuvring and normal-shutdown training program.</summary>
public static class PowerManoeuvringNormalShutdownProgram
{
    public static InitialConditionReference InitialCondition { get; } = new("stable-low-load-parallel-operation", 1);

    public static ScenarioDefinition Scenario { get; } = new(
        "power-manoeuvring-normal-shutdown",
        "Power Manoeuvring & Normal Shutdown",
        "Exercise bounded electrical-load changes with coordinated reactor/turbine response, observe available temperature/void feedback, preserve the explicit xenon boundary, then unload, disconnect and perform a controlled normal shutdown with post-shutdown circulation.",
        InitialCondition,
        new[]
        {
            new ScenarioObjectiveDefinition("verify-low-load", "Verify stable low-load operation", "Confirm the M7.5 parallel low-load handoff before changing power."),
            new ScenarioObjectiveDefinition("manoeuvre-power", "Manoeuvre power deliberately", "Use bounded generator load and validated rod/turbine command seams without directly mutating physical outputs."),
            new ScenarioObjectiveDefinition("observe-feedback", "Observe plant feedback", "Track published temperature and void diagnostics while preserving xenon as explicitly unavailable at the current operational snapshot boundary."),
            new ScenarioObjectiveDefinition("normal-shutdown", "Perform normal shutdown", "Unload, open the generator breaker, insert rods and maintain circulation for post-shutdown cooling without using SCRAM as the routine shutdown mechanism."),
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
            ControlRoomCommandKind.GeneratorLoadRaise,
            ControlRoomCommandKind.GeneratorLoadLower,
            ControlRoomCommandKind.AlarmAcknowledge,
            ControlRoomCommandKind.AlarmReset,
            ControlRoomCommandKind.AlarmAcknowledgeAll,
            ControlRoomCommandKind.AlarmResetAll,
        });

    public static PowerManoeuvringGuidancePlan Guidance { get; } = new(
        new[]
        {
            Check("signals-healthy", "Instrumentation healthy", "All measured channels required by the operating runtime are valid.", PowerManoeuvringCheckCondition.MeasuredSignalsHealthy),
            Check("protection-clear", "Protection clear", "No reactor, turbine or generator trip is active.", PowerManoeuvringCheckCondition.ProtectionClear),
            Check("mcp-running", "Main circulation established", "All modeled main-circulation pumps remain running.", PowerManoeuvringCheckCondition.MainCirculationPumpsRunning),
            Check("breakers-closed", "Generator paralleled", "All generator breakers are closed for on-grid manoeuvring.", PowerManoeuvringCheckCondition.GeneratorBreakersClosed),
            Check("low-load", "Stable low-load parallel operation", "Generator is paralleled at low load with rotor near synchronous speed and no trip.", PowerManoeuvringCheckCondition.StableLowLoadParallelOperation),
            Check("load-increased", "Load increase established", "Gross electrical output has increased into the bounded M7.6 manoeuvring band.", PowerManoeuvringCheckCondition.IncreasedElectricalLoadEstablished),
            Check("load-reduced", "Load reduced", "Gross electrical output has been reduced back toward the low-load/unload region.", PowerManoeuvringCheckCondition.ReducedElectricalLoadEstablished),
            Check("temperature-feedback", "Temperature feedback observable", "Published core-zone fuel/coolant temperature diagnostics remain finite and observable.", PowerManoeuvringCheckCondition.TemperatureFeedbackObservable),
            Check("void-feedback", "Void feedback observable", "Published core-zone void diagnostics remain explicit and finite where modeled.", PowerManoeuvringCheckCondition.VoidFeedbackObservable),
            Check("xenon-boundary", "Xenon boundary explicit", "M7.6 does not synthesize xenon because quantitative xenon state is still absent from the M5.7 operational snapshot.", PowerManoeuvringCheckCondition.XenonBoundaryExplicit),
            Check("generator-unloaded", "Generator unloaded", "Gross electrical output is approximately zero before breaker opening.", PowerManoeuvringCheckCondition.GeneratorUnloaded),
            Check("breakers-open", "Generator disconnected", "All generator breakers are open after unloading.", PowerManoeuvringCheckCondition.GeneratorBreakersOpen),
            Check("reactor-shutdown", "Reactor shutdown established", "Reactor thermal power is in the shutdown band with rods essentially inserted.", PowerManoeuvringCheckCondition.ReactorShutdownEstablished),
            Check("post-shutdown-cooling", "Post-shutdown cooling established", "Reactor is shutdown, generator isolated and main circulation remains available for cooling.", PowerManoeuvringCheckCondition.PostShutdownCoolingEstablished),
        },
        new[]
        {
            new PowerManoeuvringStepDefinition("verify-handoff", 1, "Verify M7.5 handoff", "Confirm stable low-load parallel operation, healthy instrumentation/protection and established main circulation before changing load.", new[] { "signals-healthy", "protection-clear", "mcp-running", "breakers-closed", "low-load" }),
            new PowerManoeuvringStepDefinition("raise-load", 2, "Raise electrical load deliberately", "Use GENERATOR LOAD RAISE in bounded 5 MWe request increments. Coordinate rod withdrawal/HOLD and turbine-speed governing only through validated command seams; stabilize after every change.", new[] { "breakers-closed", "load-increased", "protection-clear" }, ControlRoomCommandKind.GeneratorLoadRaise),
            new PowerManoeuvringStepDefinition("observe-feedback", 3, "Observe thermal/void feedback", "Hold the manoeuvred operating point and observe published fuel/coolant temperature and void diagnostics. Xenon remains explicitly unavailable and must not be inferred by the scenario layer.", new[] { "temperature-feedback", "void-feedback", "xenon-boundary", "protection-clear" }, ControlRoomCommandKind.ControlRodHold),
            new PowerManoeuvringStepDefinition("reduce-load", 4, "Reduce load for shutdown", "Use GENERATOR LOAD LOWER in bounded increments while coordinating reactor power downward with deliberate rod insertion. Avoid routine use of trip/SCRAM for a normal shutdown.", new[] { "load-reduced", "protection-clear", "mcp-running" }, ControlRoomCommandKind.GeneratorLoadLower),
            new PowerManoeuvringStepDefinition("unload", 5, "Unload generator", "Reduce requested electrical load to zero and verify measured gross output is approximately zero before disconnecting from the grid.", new[] { "generator-unloaded", "breakers-closed", "protection-clear" }, ControlRoomCommandKind.GeneratorLoadLower),
            new PowerManoeuvringStepDefinition("disconnect", 6, "Open generator breaker", "With the generator unloaded, issue GENERATOR BREAKER OPEN. Breaker state remains owned by M4.5 and the scenario does not rewrite electrical state directly.", new[] { "generator-unloaded", "breakers-open" }, ControlRoomCommandKind.GeneratorBreakerOpen),
            new PowerManoeuvringStepDefinition("shutdown-reactor", 7, "Complete controlled reactor shutdown", "Continue controlled rod insertion/HOLD until the reactor reaches the shutdown band. Keep protection available; SCRAM remains an emergency/protection action, not the routine procedure.", new[] { "breakers-open", "reactor-shutdown", "mcp-running" }, ControlRoomCommandKind.ControlRodInsert),
            new PowerManoeuvringStepDefinition("post-shutdown", 8, "Establish post-shutdown cooling", "Maintain main circulation while monitoring temperatures and protection after shutdown. Turbine rundown may use the validated speed-lower seam; fault scenarios remain M8 ownership.", new[] { "post-shutdown-cooling", "temperature-feedback", "xenon-boundary" }, ControlRoomCommandKind.TurbineSpeedLower),
        });

    private static PowerManoeuvringCheckDefinition Check(string id, string title, string description, PowerManoeuvringCheckCondition condition)
        => new(id, title, description, condition);
}
