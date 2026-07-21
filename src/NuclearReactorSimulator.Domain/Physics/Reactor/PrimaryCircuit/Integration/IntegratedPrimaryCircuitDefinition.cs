using NuclearReactorSimulator.Domain.Physics.Reactor.Core;
using NuclearReactorSimulator.Domain.Physics.Reactor.Core.Channels;
using NuclearReactorSimulator.Domain.Physics.Reactor.PrimaryCircuit.Boundaries;
using NuclearReactorSimulator.Domain.Physics.Reactor.PrimaryCircuit.Circulation;
using NuclearReactorSimulator.Domain.Physics.Reactor.PrimaryCircuit.SteamDrums;
using NuclearReactorSimulator.Domain.Plant;

namespace NuclearReactorSimulator.Domain.Physics.Reactor.PrimaryCircuit.Integration;

/// <summary>
/// Canonical top-level semantic definition for the complete M3 primary circuit.
/// It composes the already validated core-zone, channel-group, circulation, steam-drum and external-boundary definitions
/// without duplicating plant topology or conserved state.
/// </summary>
public sealed class IntegratedPrimaryCircuitDefinition
{
    public IntegratedPrimaryCircuitDefinition(
        string id,
        PrimaryCircuitBoundarySystemDefinition boundarySystem)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Integrated primary-circuit id cannot be empty or whitespace.", nameof(id));
        }

        BoundarySystem = boundarySystem ?? throw new ArgumentNullException(nameof(boundarySystem));
        Id = id.Trim();
    }

    public string Id { get; }

    public PrimaryCircuitBoundarySystemDefinition BoundarySystem { get; }

    public SteamDrumSystemDefinition SteamDrumSystem => BoundarySystem.SteamDrumSystem;

    public MainCirculationSystemDefinition MainCirculationSystem => SteamDrumSystem.MainCirculationSystem;

    public FuelChannelGroupSetDefinition ChannelGroups => MainCirculationSystem.ChannelGroups;

    public AggregatedCoreDefinition CoreDefinition => ChannelGroups.CoreDefinition;

    public PlantDefinition PlantDefinition => CoreDefinition.PlantDefinition;
}
