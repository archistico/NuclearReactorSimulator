namespace NuclearReactorSimulator.Application.Scenarios.Faults.InstrumentationControl;

public static class InstrumentationControlFaultTypeIds
{
    public const string SensorBias = "instrumentation.sensor-bias";
    public const string SensorFreeze = "instrumentation.sensor-freeze";
    public const string SensorFailedLow = "instrumentation.sensor-failed-low";
    public const string SensorFailedHigh = "instrumentation.sensor-failed-high";
    public const string SensorUnavailable = "instrumentation.sensor-unavailable";
    public const string ControllerOutputFreeze = "control.controller-output-freeze";
    public const string ControllerOutputFailLow = "control.controller-output-fail-low";
    public const string ControllerOutputFailHigh = "control.controller-output-fail-high";
    public const string ActuatorCommandFreeze = "control.actuator-command-freeze";
    public const string ActuatorCommandFailLow = "control.actuator-command-fail-low";
    public const string ActuatorCommandFailHigh = "control.actuator-command-fail-high";

    public static IReadOnlyList<string> All { get; } = new[]
    {
        SensorBias,
        SensorFreeze,
        SensorFailedLow,
        SensorFailedHigh,
        SensorUnavailable,
        ControllerOutputFreeze,
        ControllerOutputFailLow,
        ControllerOutputFailHigh,
        ActuatorCommandFreeze,
        ActuatorCommandFailLow,
        ActuatorCommandFailHigh,
    };
}
