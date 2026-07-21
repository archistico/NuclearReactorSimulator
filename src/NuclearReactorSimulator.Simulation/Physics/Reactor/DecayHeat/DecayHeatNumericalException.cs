namespace NuclearReactorSimulator.Simulation.Physics.Reactor.DecayHeat;

public sealed class DecayHeatNumericalException : InvalidOperationException
{
    public DecayHeatNumericalException(string message)
        : base(message)
    {
    }
}
