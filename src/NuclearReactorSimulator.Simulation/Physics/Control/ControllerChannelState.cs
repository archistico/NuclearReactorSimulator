using NuclearReactorSimulator.Domain.Physics.Control;

namespace NuclearReactorSimulator.Simulation.Physics.Control;

/// <summary>Committed controller memory only; it is not physical plant state.</summary>
public sealed record ControllerChannelState
{
    public ControllerChannelState(
        string controllerId,
        bool isInitialized,
        ControllerMode lastMode,
        double integralTerm,
        double previousError,
        double lastOutput)
    {
        if (string.IsNullOrWhiteSpace(controllerId))
        {
            throw new ArgumentException("Controller-state id cannot be empty or whitespace.", nameof(controllerId));
        }

        if (!Enum.IsDefined(typeof(ControllerMode), lastMode))
        {
            throw new ArgumentOutOfRangeException(nameof(lastMode), lastMode, "Unknown controller mode.");
        }

        if (!double.IsFinite(integralTerm) || !double.IsFinite(previousError) || !double.IsFinite(lastOutput))
        {
            throw new ArgumentOutOfRangeException(nameof(integralTerm), "Controller-state values must be finite.");
        }

        ControllerId = controllerId.Trim();
        IsInitialized = isInitialized;
        LastMode = lastMode;
        IntegralTerm = integralTerm;
        PreviousError = previousError;
        LastOutput = lastOutput;
    }

    public string ControllerId { get; }
    public bool IsInitialized { get; }
    public ControllerMode LastMode { get; }
    public double IntegralTerm { get; }
    public double PreviousError { get; }
    public double LastOutput { get; }
}
