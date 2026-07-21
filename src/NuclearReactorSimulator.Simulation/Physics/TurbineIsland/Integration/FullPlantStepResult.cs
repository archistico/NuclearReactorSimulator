namespace NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Integration;

/// <summary>One deterministic M4.7 full-plant step over the existing M4.6 physical owners.</summary>
public sealed class FullPlantStepResult
{
    public FullPlantStepResult(
        IntegratedSecondaryCycleStepResult integratedCycleStep,
        FullPlantState candidateState,
        FullPlantSnapshot snapshot)
    {
        IntegratedCycleStep = integratedCycleStep ?? throw new ArgumentNullException(nameof(integratedCycleStep));
        CandidateState = candidateState ?? throw new ArgumentNullException(nameof(candidateState));
        Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
    }

    public IntegratedSecondaryCycleStepResult IntegratedCycleStep { get; }

    public FullPlantState CandidateState { get; }

    public FullPlantSnapshot Snapshot { get; }
}
