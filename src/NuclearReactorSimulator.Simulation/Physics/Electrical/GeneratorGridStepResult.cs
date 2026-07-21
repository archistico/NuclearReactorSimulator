using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Feedwater;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Turbine;

namespace NuclearReactorSimulator.Simulation.Physics.Electrical;

public sealed class GeneratorGridStepResult
{
    public GeneratorGridStepResult(
        CondensateFeedwaterSystemStepResult condensateFeedwaterStep,
        GeneratorGridState candidateElectricalState,
        GeneratorGridSnapshot snapshot)
    {
        CondensateFeedwaterStep = condensateFeedwaterStep ?? throw new ArgumentNullException(nameof(condensateFeedwaterStep));
        CandidateElectricalState = candidateElectricalState ?? throw new ArgumentNullException(nameof(candidateElectricalState));
        Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
    }

    public CondensateFeedwaterSystemStepResult CondensateFeedwaterStep { get; }

    public PlantState CandidatePlantState => CondensateFeedwaterStep.CandidatePlantState;

    public TurbineExpansionState CandidateTurbineState => CondensateFeedwaterStep.CandidateTurbineState;

    public GeneratorGridState CandidateElectricalState { get; }

    public GeneratorGridSnapshot Snapshot { get; }
}
