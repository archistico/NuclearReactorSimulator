using NuclearReactorSimulator.Application.Scenarios.Operations;

namespace NuclearReactorSimulator.Application.Scenarios.Faults.InstrumentationControl;

/// <summary>
/// Deterministic M8.3 diagnostic pack over the validated M7.6 stable low-load initial condition. The faults are separated
/// in logical time so measured-signal, controller and actuator-command consequences can be inspected independently.
/// </summary>
public static class InstrumentationControlFaultScenarioPack
{
    public static ScenarioDefinition Demonstration { get; } = new(
        "instrumentation-control-fault-demonstration",
        "Instrumentation & Control Fault Demonstration",
        "Deterministic M8.3 exercise covering sensor bias/freeze/failure, controller-output failures and an actuator-command freeze while retaining M5 instrumentation/control/protection ownership.",
        PowerManoeuvringNormalShutdownProgram.InitialCondition,
        new[]
        {
            new ScenarioObjectiveDefinition("recognize-sensor-faults", "Recognize sensor faults", "Observe measured-value bias, freeze and invalid/unavailable quality without true-state substitution."),
            new ScenarioObjectiveDefinition("recognize-control-faults", "Recognize control-loop faults", "Observe controller outputs held or driven to canonical range limits while physical ownership remains below the scenario layer."),
            new ScenarioObjectiveDefinition("diagnose-control-impact", "Diagnose control impact", "Observe how faulted measurements and command-path failures propagate through the same canonical M5 control seams without true-state substitution."),
        },
        PowerManoeuvringNormalShutdownProgram.Scenario.AllowedOperatorActions,
        new[]
        {
            Fault("m83-power-bias", InstrumentationControlFaultTypeIds.SensorBias, "power", 20, 40,
                ("biasEngineeringUnits", "5000000")),
            Fault("m83-flow-freeze", InstrumentationControlFaultTypeIds.SensorFreeze, "flow", 60, 80),
            Fault("m83-level-failed-low", InstrumentationControlFaultTypeIds.SensorFailedLow, "level", 100, 120),
            Fault("m83-generator-output-failed-high", InstrumentationControlFaultTypeIds.SensorFailedHigh, "generator-output", 140, 160),
            Fault("m83-hotwell-unavailable", InstrumentationControlFaultTypeIds.SensorUnavailable, "hotwell", 180, 200),
            Fault("m83-speed-controller-freeze", InstrumentationControlFaultTypeIds.ControllerOutputFreeze, "speed-control", 220, 240),
            Fault("m83-level-controller-low", InstrumentationControlFaultTypeIds.ControllerOutputFailLow, "level-control", 260, 280),
            Fault("m83-power-controller-high", InstrumentationControlFaultTypeIds.ControllerOutputFailHigh, "power-control", 300, 320),
            Fault("m83-speed-actuator-command-freeze", InstrumentationControlFaultTypeIds.ActuatorCommandFreeze, "speed-actuator", 340, 360),
            Fault("m83-feedwater-actuator-command-low", InstrumentationControlFaultTypeIds.ActuatorCommandFailLow, "feedwater-actuator", 380, 400),
            Fault("m83-condensate-actuator-command-high", InstrumentationControlFaultTypeIds.ActuatorCommandFailHigh, "condensate-actuator", 420, 440),
        });

    public static ScenarioDefinition ProtectionDiagnostic { get; } = new(
        "instrumentation-protection-fail-safe-diagnostic",
        "Instrumentation Protection Fail-Safe Diagnostic",
        "Deterministic M8.3 diagnostic showing that an unavailable protection measurement propagates through the committed measured-signal boundary and invokes the existing M5.5 fail-safe trip policy on the following logical step.",
        PowerManoeuvringNormalShutdownProgram.InitialCondition,
        new[]
        {
            new ScenarioObjectiveDefinition("diagnose-invalid-protection-signal", "Diagnose invalid protection signal", "Observe sensor unavailability, committed-frame ordering and the resulting canonical protection response."),
        },
        PowerManoeuvringNormalShutdownProgram.Scenario.AllowedOperatorActions,
        new[]
        {
            Fault("m83-pressure-unavailable-diagnostic", InstrumentationControlFaultTypeIds.SensorUnavailable, "pressure", 20, 40),
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
