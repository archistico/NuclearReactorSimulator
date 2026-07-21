namespace NuclearReactorSimulator.Application.Scenarios.Training;

public enum TrainingEvaluationCriterionKind
{
    CheckpointSatisfied = 0,
    OperatorActionObserved = 1,
    OperatorActionSequenceObserved = 2,
    OperatorActionNotObserved = 3,
}
