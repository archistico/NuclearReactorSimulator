using NuclearReactorSimulator.Domain.Physics.TurbineIsland.Integration;
using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Physics.Electrical;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Turbine;

namespace NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Integration;

/// <summary>
/// Canonical immutable M4.7 cross-domain committed state envelope.
/// It owns no new physical state: it groups the existing thermofluid, mechanical and electrical state owners.
/// </summary>
public sealed class FullPlantState
{
    public FullPlantState(
        IntegratedSecondaryCycleDefinition definition,
        PlantState plantState,
        TurbineExpansionState turbineState,
        GeneratorGridState electricalState)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        PlantState = plantState ?? throw new ArgumentNullException(nameof(plantState));
        TurbineState = turbineState ?? throw new ArgumentNullException(nameof(turbineState));
        ElectricalState = electricalState ?? throw new ArgumentNullException(nameof(electricalState));

        if (!ReferenceEquals(plantState.Definition, definition.PlantDefinition))
        {
            throw new ArgumentException("Plant state does not use the full plant's canonical plant definition.", nameof(plantState));
        }

        if (!ReferenceEquals(turbineState.Definition, definition.TurbineExpansionSystem))
        {
            throw new ArgumentException("Turbine state does not use the full plant's canonical turbine definition.", nameof(turbineState));
        }

        if (!ReferenceEquals(electricalState.Definition, definition.GeneratorGridSystem))
        {
            throw new ArgumentException("Electrical state does not use the full plant's canonical generator/grid definition.", nameof(electricalState));
        }
    }

    public IntegratedSecondaryCycleDefinition Definition { get; }

    public PlantState PlantState { get; }

    public TurbineExpansionState TurbineState { get; }

    public GeneratorGridState ElectricalState { get; }
}
