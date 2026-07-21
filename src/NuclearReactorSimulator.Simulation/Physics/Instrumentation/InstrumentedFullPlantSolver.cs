using NuclearReactorSimulator.Domain.Physics.Instrumentation;
using NuclearReactorSimulator.Domain.Physics.TurbineIsland.Integration;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Integration;

namespace NuclearReactorSimulator.Simulation.Physics.Instrumentation;

/// <summary>
/// M5.1 composition boundary. Physical evolution remains owned exclusively by M4.7; instrumentation observes the resulting
/// immutable true-state snapshot and evolves only sensor/filter state.
/// </summary>
public sealed class InstrumentedFullPlantSolver
{
    private readonly IntegratedSecondaryCycleDefinition _plantDefinition;
    private readonly InstrumentationSystemDefinition _instrumentationDefinition;
    private readonly FullPlantSolver _fullPlantSolver;
    private readonly InstrumentationSolver _instrumentationSolver;

    public InstrumentedFullPlantSolver(
        IntegratedSecondaryCycleDefinition plantDefinition,
        InstrumentationSystemDefinition instrumentationDefinition,
        IFluidThermodynamicModel thermodynamicModel,
        InstrumentSignalSourceCatalog signalSources)
    {
        _plantDefinition = plantDefinition ?? throw new ArgumentNullException(nameof(plantDefinition));
        _instrumentationDefinition = instrumentationDefinition ?? throw new ArgumentNullException(nameof(instrumentationDefinition));
        ArgumentNullException.ThrowIfNull(thermodynamicModel);
        ArgumentNullException.ThrowIfNull(signalSources);

        _fullPlantSolver = new FullPlantSolver(plantDefinition, thermodynamicModel);
        _instrumentationSolver = new InstrumentationSolver(instrumentationDefinition, signalSources);
    }

    public InstrumentedFullPlantStepResult Step(
        InstrumentedFullPlantState committedState,
        IntegratedSecondaryCycleInputs plantInputs,
        InstrumentationInputs instrumentationInputs,
        TimeSpan deltaTime)
    {
        ArgumentNullException.ThrowIfNull(committedState);
        ArgumentNullException.ThrowIfNull(plantInputs);
        ArgumentNullException.ThrowIfNull(instrumentationInputs);

        if (!ReferenceEquals(committedState.PlantDefinition, _plantDefinition)
            || !ReferenceEquals(committedState.InstrumentationDefinition, _instrumentationDefinition))
        {
            throw new ArgumentException("Committed instrumented state does not use this solver's canonical definitions.", nameof(committedState));
        }

        var fullPlantStep = _fullPlantSolver.Step(committedState.PlantState, plantInputs, deltaTime);
        var instrumentationStep = _instrumentationSolver.Step(
            fullPlantStep.Snapshot,
            committedState.InstrumentationState,
            instrumentationInputs,
            deltaTime);
        var candidateState = new InstrumentedFullPlantState(
            _plantDefinition,
            _instrumentationDefinition,
            fullPlantStep.CandidateState,
            instrumentationStep.CandidateState);
        var snapshot = new InstrumentedFullPlantSnapshot(fullPlantStep.Snapshot, instrumentationStep.Snapshot);

        return new InstrumentedFullPlantStepResult(fullPlantStep, instrumentationStep, candidateState, snapshot);
    }
}
