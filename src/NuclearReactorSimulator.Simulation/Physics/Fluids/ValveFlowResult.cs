using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Simulation.Physics.Fluids;

/// <summary>
/// Hydraulic result for a valve-controlled passive connection.
/// </summary>
public sealed record ValveFlowResult
{
    internal ValveFlowResult(
        ValvePosition effectivePosition,
        ValveFlowCoefficient flowCoefficient,
        PipeFlowResult hydraulicFlow)
    {
        EffectivePosition = effectivePosition;
        FlowCoefficient = flowCoefficient;
        HydraulicFlow = hydraulicFlow;
    }

    public ValvePosition EffectivePosition { get; }

    public ValveFlowCoefficient FlowCoefficient { get; }

    public PipeFlowResult HydraulicFlow { get; }

    public PressureDifference PressureDifference => HydraulicFlow.PressureDifference;

    public MassFlowRate MassFlowRate => HydraulicFlow.MassFlowRate;

    public FluidEnergyTransportMode EnergyTransportMode => HydraulicFlow.EnergyTransportMode;

    public Power InternalEnergyFlowRate => HydraulicFlow.InternalEnergyFlowRate;

    public Power FlowWorkRate => HydraulicFlow.FlowWorkRate;

    public Power EnthalpyFlowRate => HydraulicFlow.EnthalpyFlowRate;

    public Power AdvectedEnergyFlowRate => HydraulicFlow.AdvectedEnergyFlowRate;

    public FluidNodeBalance FromNodeBalance => HydraulicFlow.FromNodeBalance;

    public FluidNodeBalance ToNodeBalance => HydraulicFlow.ToNodeBalance;
}
