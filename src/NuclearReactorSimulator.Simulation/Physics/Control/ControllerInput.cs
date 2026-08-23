using NuclearReactorSimulator.Domain.Physics.Control;

namespace NuclearReactorSimulator.Simulation.Physics.Control;

public sealed record ControllerInput
{
    public ControllerInput(
        string controllerId,
        ControllerMode mode,
        double setpoint,
        double manualOutput,
        double? integralSetpoint = null)
    {
        if (string.IsNullOrWhiteSpace(controllerId))
        {
            throw new ArgumentException("Controller-input id cannot be empty or whitespace.", nameof(controllerId));
        }

        if (!Enum.IsDefined(typeof(ControllerMode), mode))
        {
            throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown controller mode.");
        }

        if (!double.IsFinite(setpoint) || !double.IsFinite(manualOutput))
        {
            throw new ArgumentOutOfRangeException(nameof(setpoint), "Controller setpoint and manual output must be finite.");
        }

        if (integralSetpoint.HasValue && !double.IsFinite(integralSetpoint.Value))
        {
            throw new ArgumentOutOfRangeException(nameof(integralSetpoint), integralSetpoint, "Optional controller integral setpoint must be finite.");
        }

        ControllerId = controllerId.Trim();
        Mode = mode;
        Setpoint = setpoint;
        ManualOutput = manualOutput;
        IntegralSetpoint = integralSetpoint;
    }

    public string ControllerId { get; }
    public ControllerMode Mode { get; }
    public double Setpoint { get; }
    public double ManualOutput { get; }

    /// <summary>
    /// Optional independent reference for integral action. Null preserves historical behavior and integrates
    /// against <see cref="Setpoint"/>.
    /// </summary>
    public double? IntegralSetpoint { get; }
}
