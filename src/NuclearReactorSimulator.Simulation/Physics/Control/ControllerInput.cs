using NuclearReactorSimulator.Domain.Physics.Control;

namespace NuclearReactorSimulator.Simulation.Physics.Control;

public sealed record ControllerInput
{
    public ControllerInput(string controllerId, ControllerMode mode, double setpoint, double manualOutput)
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

        ControllerId = controllerId.Trim();
        Mode = mode;
        Setpoint = setpoint;
        ManualOutput = manualOutput;
    }

    public string ControllerId { get; }
    public ControllerMode Mode { get; }
    public double Setpoint { get; }
    public double ManualOutput { get; }
}
