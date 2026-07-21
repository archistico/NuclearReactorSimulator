using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Simulation.Physics.Reactor.PrimaryCircuit.Circulation;

public sealed record MainCirculationBranchSnapshot(
    string FuelChannelGroupId,
    int RepresentedChannelCount,
    string ChannelPipeId,
    string ReturnPipeId,
    MassFlowRate ChannelMassFlowRate,
    MassFlowRate ReturnMassFlowRate,
    MassFlowRate PerChannelMassFlowRate,
    MassFlowRate BranchContinuityResidual,
    PressureDifference ChannelPressureDifference,
    PressureDifference ReturnPressureDifference,
    FluidPhase OutletPhase,
    VaporQuality? OutletVaporQuality,
    VoidFraction? OutletVoidFraction);
