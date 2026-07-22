namespace NuclearReactorSimulator.Application.Scenarios.Training;

/// <summary>Typed presentation/training intent boundary. Guidance mode changes never alter physics or scoring.</summary>
public interface ITrainingAssistanceDispatcher
{
    event EventHandler? GuidanceModeChanged;

    TrainingGuidanceMode GuidanceMode { get; }

    void SetGuidanceMode(TrainingGuidanceMode mode);
}
