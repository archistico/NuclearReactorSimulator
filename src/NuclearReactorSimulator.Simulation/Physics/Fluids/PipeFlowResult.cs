using NuclearReactorSimulator.Domain.Physics.Fluids;
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
        FluidEnergyTransportMode energyTransportMode,
        Power internalEnergyFlowRate,
        Power flowWorkRate,
        Power enthalpyFlowRate,
        Power advectedEnergyFlowRate)
    {
        PressureDifference = pressureDifference;
        MassFlowRate = massFlowRate;
        EnergyTransportMode = energyTransportMode;
        InternalEnergyFlowRate = internalEnergyFlowRate;
        FlowWorkRate = flowWorkRate;
        EnthalpyFlowRate = enthalpyFlowRate;
        AdvectedEnergyFlowRate = advectedEnergyFlowRate;
        FromNodeBalance = new FluidNodeBalance(-massFlowRate, -advectedEnergyFlowRate);
        ToNodeBalance = new FluidNodeBalance(massFlowRate, advectedEnergyFlowRate);
    }

    public PressureDifference PressureDifference { get; }

    public MassFlowRate MassFlowRate { get; }

    public FluidEnergyTransportMode EnergyTransportMode { get; }

    /// <summary>
    /// Signed upstream specific-internal-energy rate u*m_dot. This remains available as diagnostic
    /// evidence even when <see cref="AdvectedEnergyFlowRate"/> uses the enthalpy convention.
    /// </summary>
    public Power InternalEnergyFlowRate { get; }

    /// <summary>Signed explicit flow-work rate (p/rho)*m_dot.</summary>
    public Power FlowWorkRate { get; }

    /// <summary>Signed open-control-volume enthalpy rate h*m_dot.</summary>
    public Power EnthalpyFlowRate { get; }

    /// <summary>
    /// Signed energy rate actually applied to the endpoint balances according to
    /// <see cref="EnergyTransportMode"/>.
    /// </summary>
    public Power AdvectedEnergyFlowRate { get; }

    public FluidNodeBalance FromNodeBalance { get; }

    public FluidNodeBalance ToNodeBalance { get; }
}
