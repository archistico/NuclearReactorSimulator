using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Faults;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios.Operations;

namespace NuclearReactorSimulator.Application.Scenarios.Faults.Hydraulics;

/// <summary>
/// M8.2 deterministic hydraulic-fault exercise over the validated M7.6 stable low-load initial condition.
/// Faults are intentionally separated in logical time so each concrete effect can be observed without overlapping ownership.
/// </summary>
public static class HydraulicComponentFaultScenarioPack
{
    public static ScenarioDefinition Demonstration { get; } = new(
        "hydraulic-component-fault-demonstration",
        "Hydraulic Component Fault Demonstration",
        "Deterministic M8.2 exercise covering pump degradation/trip, valve failure/stuck behaviour, valve-controlled path restriction/blockage and a selected inventory leak.",
        PowerManoeuvringNormalShutdownProgram.InitialCondition,
        new[]
        {
            new ScenarioObjectiveDefinition("recognize-pump-faults", "Recognize pump faults", "Observe commanded operation diverging from degraded or tripped hydraulic capability."),
            new ScenarioObjectiveDefinition("recognize-valve-faults", "Recognize valve/path faults", "Observe fail-open/fail-closed/stuck and restricted valve-controlled path behaviour."),
            new ScenarioObjectiveDefinition("recognize-leak", "Recognize inventory leak", "Observe deterministic mass/energy loss through the canonical plant-network audit boundary."),
        },
        PowerManoeuvringNormalShutdownProgram.Scenario.AllowedOperatorActions,
        new[]
        {
            Fault("m82-pump-degradation", HydraulicFaultTypeIds.PumpDegradation, "pump", 20, 60,
                ("capacityFraction", "0.50")),
            Fault("m82-pump-trip", HydraulicFaultTypeIds.PumpTrip, "pump", 80, 100),
            Fault("m82-valve-fail-closed", HydraulicFaultTypeIds.ValveFailClosed, "stop", 120, 140),
            Fault("m82-valve-fail-open", HydraulicFaultTypeIds.ValveFailOpen, "stop", 160, 180),
            Fault("m82-valve-stuck", HydraulicFaultTypeIds.ValveStuck, "control", 200, 220),
            Fault("m82-path-restriction", HydraulicFaultTypeIds.PathRestriction, "stop", 240, 260,
                ("maximumOpenFraction", "0.25")),
            Fault("m82-path-blockage", HydraulicFaultTypeIds.PathBlockage, "stop", 280, 300),
            Fault("m82-selected-leak", HydraulicFaultTypeIds.NodeLeak, "drum", 320, 340,
                ("massFlowKgPerSecond", "0.02")),
        });

    private static ScenarioFaultDefinition Fault(
        string faultId,
        string faultTypeId,
        string targetId,
        long activationStep,
        long deactivationStep,
        params (string Key, string Value)[] parameters)
        => new(
            faultId,
            faultTypeId,
            targetId,
            ScenarioFaultTriggerDefinition.AtLogicalStep(activationStep),
            ScenarioFaultTriggerDefinition.AtLogicalStep(deactivationStep),
            parameters.ToDictionary(static x => x.Key, static x => x.Value, StringComparer.Ordinal));
}
