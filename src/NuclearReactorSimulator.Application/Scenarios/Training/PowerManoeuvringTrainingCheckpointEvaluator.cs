using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios.Operations;

namespace NuclearReactorSimulator.Application.Scenarios.Training;

/// <summary>Adapts the already observational M7.6 checklist vocabulary into generic M7.7 historical checkpoints.</summary>
public sealed class PowerManoeuvringTrainingCheckpointEvaluator : ITrainingCheckpointEvaluator
{
    private readonly IReadOnlyDictionary<string, PowerManoeuvringCheckDefinition> _checks;
    private readonly PowerManoeuvringChecklistEvaluator _evaluator = new();

    public PowerManoeuvringTrainingCheckpointEvaluator(PowerManoeuvringGuidancePlan guidance)
    {
        ArgumentNullException.ThrowIfNull(guidance);
        _checks = guidance.Checks.ToDictionary(static check => check.CheckId, StringComparer.Ordinal);
    }

    public TrainingCheckpointObservation Evaluate(ControlRoomSnapshot snapshot, TrainingCheckpointDefinition checkpoint)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(checkpoint);
        if (!_checks.TryGetValue(checkpoint.SourceCheckId, out var sourceCheck))
        {
            throw new KeyNotFoundException($"Training checkpoint '{checkpoint.CheckpointId}' references unknown source check '{checkpoint.SourceCheckId}'.");
        }

        var result = _evaluator.Evaluate(snapshot, sourceCheck);
        return new TrainingCheckpointObservation(result.IsSatisfied, result.Observation);
    }
}
