namespace NuclearReactorSimulator.Application.Scenarios.Training;

public sealed class ScenarioTrainingAssessmentChangedEventArgs : EventArgs
{
    public ScenarioTrainingAssessmentChangedEventArgs(ScenarioTrainingAssessment assessment)
    {
        Assessment = assessment ?? throw new ArgumentNullException(nameof(assessment));
    }

    public ScenarioTrainingAssessment Assessment { get; }
}
