namespace NuclearReactorSimulator.Simulation.Runtime;

/// <summary>
/// Signals that execution of a physical step failed and the runtime entered the terminal Faulted state.
/// </summary>
public sealed class SimulationRuntimeFaultException : Exception
{
    public SimulationRuntimeFaultException(SimulationFaultSnapshot fault, Exception innerException)
        : base(
            $"Simulation runtime faulted while executing step {fault.FailedStepIndex}: {fault.Message}",
            innerException)
    {
        ArgumentNullException.ThrowIfNull(fault);
        ArgumentNullException.ThrowIfNull(innerException);
        Fault = fault;
    }

    public SimulationFaultSnapshot Fault { get; }
}
