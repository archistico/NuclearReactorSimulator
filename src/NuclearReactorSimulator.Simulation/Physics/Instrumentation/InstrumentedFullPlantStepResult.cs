using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Integration;

namespace NuclearReactorSimulator.Simulation.Physics.Instrumentation;

public sealed class InstrumentedFullPlantStepResult
{
    public InstrumentedFullPlantStepResult(
        FullPlantStepResult fullPlantStep,
        InstrumentationStepResult instrumentationStep,
        InstrumentedFullPlantState candidateState,
        InstrumentedFullPlantSnapshot snapshot)
    {
        FullPlantStep = fullPlantStep ?? throw new ArgumentNullException(nameof(fullPlantStep));
        InstrumentationStep = instrumentationStep ?? throw new ArgumentNullException(nameof(instrumentationStep));
        CandidateState = candidateState ?? throw new ArgumentNullException(nameof(candidateState));
        Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
    }

    public FullPlantStepResult FullPlantStep { get; }

    public InstrumentationStepResult InstrumentationStep { get; }

    public InstrumentedFullPlantState CandidateState { get; }

    public InstrumentedFullPlantSnapshot Snapshot { get; }
}
