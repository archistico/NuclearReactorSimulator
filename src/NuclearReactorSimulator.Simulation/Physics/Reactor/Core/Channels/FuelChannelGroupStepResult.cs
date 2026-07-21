using NuclearReactorSimulator.Simulation.Plant;

namespace NuclearReactorSimulator.Simulation.Physics.Reactor.Core.Channels;

/// <summary>Immutable channel-group result containing diagnostics and source terms for staged plant integration.</summary>
public sealed record FuelChannelGroupStepResult(
    FuelChannelGroupSetSnapshot Snapshot,
    PlantNetworkSourceTerms SourceTerms);
