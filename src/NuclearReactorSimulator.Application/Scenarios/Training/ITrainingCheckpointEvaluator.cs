using NuclearReactorSimulator.Application.ControlRoom;

namespace NuclearReactorSimulator.Application.Scenarios.Training;

/// <summary>Observational adapter from a presentation snapshot to one declared training checkpoint.</summary>
public interface ITrainingCheckpointEvaluator
{
    TrainingCheckpointObservation Evaluate(ControlRoomSnapshot snapshot, TrainingCheckpointDefinition checkpoint);
}
