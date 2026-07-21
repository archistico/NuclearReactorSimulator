using NuclearReactorSimulator.Simulation.Physics.Control;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Integration;

namespace NuclearReactorSimulator.Simulation.Physics.Control.ReactorPrimary;

public sealed class ReactorPrimaryControlStepResult
{
    public ReactorPrimaryControlStepResult(
        ControlAndActuatorStepResult controlAndActuatorStep,
        ReactorPrimaryControlState candidateState,
        FullPlantState commandedFullPlantState,
        ReactorPrimaryControlSnapshot snapshot)
    {
        ControlAndActuatorStep = controlAndActuatorStep ?? throw new ArgumentNullException(nameof(controlAndActuatorStep));
        CandidateState = candidateState ?? throw new ArgumentNullException(nameof(candidateState));
        CommandedFullPlantState = commandedFullPlantState ?? throw new ArgumentNullException(nameof(commandedFullPlantState));
        Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
    }

    public ControlAndActuatorStepResult ControlAndActuatorStep { get; }
    public ReactorPrimaryControlState CandidateState { get; }
    public FullPlantState CommandedFullPlantState { get; }
    public ReactorPrimaryControlSnapshot Snapshot { get; }
}
