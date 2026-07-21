namespace NuclearReactorSimulator.Simulation.Physics.Reactor.Neutronics;

/// <summary>
/// Raised when point-kinetics integration leaves the supported finite, non-negative numerical state space.
/// </summary>
public sealed class NeutronKineticsNumericalException : Exception
{
    public NeutronKineticsNumericalException(string message)
        : base(message)
    {
    }
}
