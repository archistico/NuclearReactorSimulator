using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Integration;

namespace NuclearReactorSimulator.Simulation.Physics.Control.ReactorPrimary;

public sealed class ReactorPrimaryControlledFullPlantStepResult
{
    public ReactorPrimaryControlledFullPlantStepResult(
        ReactorPrimaryControlStepResult controlStep,
        FullPlantStepResult fullPlantStep,
        IntegratedSecondaryCycleInputs effectivePlantInputs,
        ReactorPrimaryControlledFullPlantSnapshot snapshot)
    {
        ControlStep = controlStep ?? throw new ArgumentNullException(nameof(controlStep));
        FullPlantStep = fullPlantStep ?? throw new ArgumentNullException(nameof(fullPlantStep));
        EffectivePlantInputs = effectivePlantInputs ?? throw new ArgumentNullException(nameof(effectivePlantInputs));
        Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
    }

    public ReactorPrimaryControlStepResult ControlStep { get; }
    public FullPlantStepResult FullPlantStep { get; }
    public IntegratedSecondaryCycleInputs EffectivePlantInputs { get; }
    public ReactorPrimaryControlState CandidateControlState => ControlStep.CandidateState;
    public FullPlantState CandidatePlantState => FullPlantStep.CandidateState;
    public ReactorPrimaryControlledFullPlantSnapshot Snapshot { get; }
}
