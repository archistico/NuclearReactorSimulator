using System.Collections.ObjectModel;
using NuclearReactorSimulator.Domain.Physics.Control.Protection;

namespace NuclearReactorSimulator.Simulation.Physics.Control.Integration;

public sealed class AutomaticOperationVerificationPhaseResult
{
    public AutomaticOperationVerificationPhaseResult(
        string phaseId,
        int stepCount,
        IntegratedAutomaticOperationStepResult finalStep,
        IEnumerable<AutomaticOperationTrackingResult> trackingResults,
        ProtectionAction expectedLatchedProtectionActions,
        ProtectionInterlockAction expectedActiveInterlocks,
        bool protectionExpectationSatisfied)
    {
        PhaseId = phaseId;
        StepCount = stepCount;
        FinalStep = finalStep ?? throw new ArgumentNullException(nameof(finalStep));
        TrackingResults = new ReadOnlyCollection<AutomaticOperationTrackingResult>(trackingResults.ToArray());
        ExpectedLatchedProtectionActions = expectedLatchedProtectionActions;
        ExpectedActiveInterlocks = expectedActiveInterlocks;
        ProtectionExpectationSatisfied = protectionExpectationSatisfied;
    }

    public string PhaseId { get; }
    public int StepCount { get; }
    public IntegratedAutomaticOperationStepResult FinalStep { get; }
    public IReadOnlyList<AutomaticOperationTrackingResult> TrackingResults { get; }
    public ProtectionAction ExpectedLatchedProtectionActions { get; }
    public ProtectionInterlockAction ExpectedActiveInterlocks { get; }
    public bool ProtectionExpectationSatisfied { get; }
    public bool TrackingSatisfied => TrackingResults.All(static item => item.Satisfied);
    public bool Satisfied => ProtectionExpectationSatisfied && TrackingSatisfied;
}
