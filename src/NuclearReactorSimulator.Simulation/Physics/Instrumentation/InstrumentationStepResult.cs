namespace NuclearReactorSimulator.Simulation.Physics.Instrumentation;

public sealed class InstrumentationStepResult
{
    public InstrumentationStepResult(
        InstrumentationState candidateState,
        InstrumentationSnapshot snapshot)
    {
        CandidateState = candidateState ?? throw new ArgumentNullException(nameof(candidateState));
        Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
    }

    public InstrumentationState CandidateState { get; }

    public InstrumentationSnapshot Snapshot { get; }
}
