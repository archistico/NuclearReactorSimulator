using NuclearReactorSimulator.Domain.Physics.Instrumentation;
using NuclearReactorSimulator.Domain.Physics.TurbineIsland.Integration;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Integration;

namespace NuclearReactorSimulator.Simulation.Physics.Instrumentation;

/// <summary>
/// M5.1 top-level state envelope: validated full-plant physical state plus separate instrumentation dynamics.
/// Instrumentation state is observational and never becomes a conserved physical inventory.
/// </summary>
public sealed class InstrumentedFullPlantState
{
    public InstrumentedFullPlantState(
        IntegratedSecondaryCycleDefinition plantDefinition,
        InstrumentationSystemDefinition instrumentationDefinition,
        FullPlantState plantState,
        InstrumentationState instrumentationState)
    {
        PlantDefinition = plantDefinition ?? throw new ArgumentNullException(nameof(plantDefinition));
        InstrumentationDefinition = instrumentationDefinition ?? throw new ArgumentNullException(nameof(instrumentationDefinition));
        PlantState = plantState ?? throw new ArgumentNullException(nameof(plantState));
        InstrumentationState = instrumentationState ?? throw new ArgumentNullException(nameof(instrumentationState));

        if (!ReferenceEquals(plantState.Definition, plantDefinition))
        {
            throw new ArgumentException("Full-plant state does not use the canonical plant definition.", nameof(plantState));
        }

        if (!ReferenceEquals(instrumentationState.Definition, instrumentationDefinition))
        {
            throw new ArgumentException("Instrumentation state does not use the canonical instrumentation definition.", nameof(instrumentationState));
        }
    }

    public IntegratedSecondaryCycleDefinition PlantDefinition { get; }

    public InstrumentationSystemDefinition InstrumentationDefinition { get; }

    public FullPlantState PlantState { get; }

    public InstrumentationState InstrumentationState { get; }
}
