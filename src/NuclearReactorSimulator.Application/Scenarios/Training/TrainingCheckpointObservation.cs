namespace NuclearReactorSimulator.Application.Scenarios.Training;

public sealed record TrainingCheckpointObservation
{
    public TrainingCheckpointObservation(bool isSatisfied, string observation)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(observation);
        IsSatisfied = isSatisfied;
        Observation = observation;
    }

    public bool IsSatisfied { get; }
    public string Observation { get; }
}
