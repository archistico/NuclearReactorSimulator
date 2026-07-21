using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Physics.Electrical;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Turbine;

namespace NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Integration;

public sealed class IntegratedSecondaryCycleStepResult
{
    public IntegratedSecondaryCycleStepResult(
        GeneratorGridStepResult generatorGridStep,
        IntegratedSecondaryCycleSnapshot snapshot)
    {
        GeneratorGridStep = generatorGridStep ?? throw new ArgumentNullException(nameof(generatorGridStep));
        Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
    }

    public GeneratorGridStepResult GeneratorGridStep { get; }

    public PlantState CandidatePlantState => GeneratorGridStep.CandidatePlantState;

    public TurbineExpansionState CandidateTurbineState => GeneratorGridStep.CandidateTurbineState;

    public GeneratorGridState CandidateElectricalState => GeneratorGridStep.CandidateElectricalState;

    public IntegratedSecondaryCycleSnapshot Snapshot { get; }
}
