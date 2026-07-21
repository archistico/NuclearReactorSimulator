namespace NuclearReactorSimulator.Simulation.Physics.Control;

/// <summary>Last command-side demand only; this is not duplicated mechanical/plant actuator state.</summary>
public sealed record ActuatorCommandState
{
    public ActuatorCommandState(string actuatorId, double lastControllerOutput)
    {
        if (string.IsNullOrWhiteSpace(actuatorId))
        {
            throw new ArgumentException("Actuator command-state id cannot be empty or whitespace.", nameof(actuatorId));
        }

        if (!double.IsFinite(lastControllerOutput))
        {
            throw new ArgumentOutOfRangeException(nameof(lastControllerOutput), lastControllerOutput, "Actuator command-state output must be finite.");
        }

        ActuatorId = actuatorId.Trim();
        LastControllerOutput = lastControllerOutput;
    }

    public string ActuatorId { get; }
    public double LastControllerOutput { get; }
}
