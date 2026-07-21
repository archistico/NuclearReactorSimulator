namespace NuclearReactorSimulator.Simulation.Physics.Reactor.ThermalPower;

/// <summary>
/// Raised when neutron-to-fission-power scaling or heat allocation leaves the supported finite non-negative numerical space.
/// </summary>
public sealed class FissionPowerNumericalException : ArithmeticException
{
    public FissionPowerNumericalException(string message)
        : base(message)
    {
    }
}
