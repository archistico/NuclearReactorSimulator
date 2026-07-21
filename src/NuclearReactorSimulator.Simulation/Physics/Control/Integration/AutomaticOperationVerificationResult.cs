using System.Collections.ObjectModel;

namespace NuclearReactorSimulator.Simulation.Physics.Control.Integration;

public sealed class AutomaticOperationVerificationResult
{
    public AutomaticOperationVerificationResult(
        string planId,
        int totalStepCount,
        TimeSpan simulatedDuration,
        IntegratedAutomaticOperationState finalState,
        IEnumerable<AutomaticOperationVerificationPhaseResult> phases,
        double maximumAbsoluteMassClosureResidualKilograms,
        double maximumAbsoluteFullEnergyPathClosureResidualJoules,
        int maximumInvalidMeasuredSignalCount,
        int maximumUnacknowledgedAlarmCount,
        AutomaticOperationAcceptanceCriteria criteria,
        bool acceptanceCriteriaSatisfied)
    {
        PlanId = planId;
        TotalStepCount = totalStepCount;
        SimulatedDuration = simulatedDuration;
        FinalState = finalState ?? throw new ArgumentNullException(nameof(finalState));
        Phases = new ReadOnlyCollection<AutomaticOperationVerificationPhaseResult>(phases.ToArray());
        MaximumAbsoluteMassClosureResidualKilograms = maximumAbsoluteMassClosureResidualKilograms;
        MaximumAbsoluteFullEnergyPathClosureResidualJoules = maximumAbsoluteFullEnergyPathClosureResidualJoules;
        MaximumInvalidMeasuredSignalCount = maximumInvalidMeasuredSignalCount;
        MaximumUnacknowledgedAlarmCount = maximumUnacknowledgedAlarmCount;
        Criteria = criteria ?? throw new ArgumentNullException(nameof(criteria));
        AcceptanceCriteriaSatisfied = acceptanceCriteriaSatisfied;
    }

    public string PlanId { get; }
    public int TotalStepCount { get; }
    public TimeSpan SimulatedDuration { get; }
    public IntegratedAutomaticOperationState FinalState { get; }
    public IReadOnlyList<AutomaticOperationVerificationPhaseResult> Phases { get; }
    public double MaximumAbsoluteMassClosureResidualKilograms { get; }
    public double MaximumAbsoluteFullEnergyPathClosureResidualJoules { get; }
    public int MaximumInvalidMeasuredSignalCount { get; }
    public int MaximumUnacknowledgedAlarmCount { get; }
    public AutomaticOperationAcceptanceCriteria Criteria { get; }
    public bool AcceptanceCriteriaSatisfied { get; }
    public bool AllPhasesSatisfied => Phases.All(static phase => phase.Satisfied);
    public bool GateSatisfied => AcceptanceCriteriaSatisfied && AllPhasesSatisfied;
}
