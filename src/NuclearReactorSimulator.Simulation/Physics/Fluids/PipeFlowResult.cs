using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Simulation.Physics.Fluids;

/// <summary>
/// Instantaneous conservative transfer solved for one passive pipe.
/// Positive flow is from the pipe's reference from-node toward its to-node.
/// </summary>
public sealed record PipeFlowResult
{
    internal PipeFlowResult(
        PressureDifference pressureDifference,
        MassFlowRate massFlowRate,
        Power internalEnergyFlowRate)
    {
        PressureDifference = pressureDifference;
        MassFlowRate = massFlowRate;
        InternalEnergyFlowRate = internalEnergyFlowRate;
        FromNodeBalance = new FluidNodeBalance(-massFlowRate, -internalEnergyFlowRate);
        ToNodeBalance = new FluidNodeBalance(massFlowRate, internalEnergyFlowRate);
    }

    public PressureDifference PressureDifference { get; }

    public MassFlowRate MassFlowRate { get; }

    /// <summary>
    /// Signed advected internal-energy rate. Positive is from reference from-node to to-node.
    /// </summary>
    public Power InternalEnergyFlowRate { get; }

    public FluidNodeBalance FromNodeBalance { get; }

    public FluidNodeBalance ToNodeBalance { get; }
}
