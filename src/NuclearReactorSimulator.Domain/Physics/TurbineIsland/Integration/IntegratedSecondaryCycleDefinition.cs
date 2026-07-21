using NuclearReactorSimulator.Domain.Physics.Electrical;
using NuclearReactorSimulator.Domain.Physics.Reactor.PrimaryCircuit.Integration;
using NuclearReactorSimulator.Domain.Physics.TurbineIsland.Feedwater;
using NuclearReactorSimulator.Domain.Physics.TurbineIsland.Turbine;
using NuclearReactorSimulator.Domain.Plant;

namespace NuclearReactorSimulator.Domain.Physics.TurbineIsland.Integration;

/// <summary>
/// Canonical M4.6 top-level composition for the complete manually commanded secondary cycle and generator/grid domain.
/// It introduces no parallel inventories or component physics; it owns the validated M4.5 composition as one audit boundary.
/// </summary>
public sealed class IntegratedSecondaryCycleDefinition
{
    public IntegratedSecondaryCycleDefinition(string id, GeneratorGridSystemDefinition generatorGridSystem)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Integrated secondary-cycle id cannot be empty or whitespace.", nameof(id));
        }

        GeneratorGridSystem = generatorGridSystem ?? throw new ArgumentNullException(nameof(generatorGridSystem));
        Id = id.Trim();
    }

    public string Id { get; }

    public GeneratorGridSystemDefinition GeneratorGridSystem { get; }

    public CondensateFeedwaterSystemDefinition CondensateFeedwaterSystem => GeneratorGridSystem.CondensateFeedwaterSystem;

    public TurbineExpansionSystemDefinition TurbineExpansionSystem => GeneratorGridSystem.TurbineExpansionSystem;

    public IntegratedPrimaryCircuitDefinition PrimaryCircuit
        => TurbineExpansionSystem.MainSteamNetwork.PrimaryCircuit;

    public PlantDefinition PlantDefinition => CondensateFeedwaterSystem.PlantDefinition;
}
