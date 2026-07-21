using NuclearReactorSimulator.Domain.Physics.TurbineIsland.Integration;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using NuclearReactorSimulator.Simulation.Plant;

namespace NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Integration;

/// <summary>
/// Thin M4.7 full-plant orchestration boundary. It delegates all physical evolution to M4.6 and adds only a canonical
/// cross-domain state/snapshot envelope plus derived performance diagnostics.
/// </summary>
public sealed class FullPlantSolver
{
    private readonly IntegratedSecondaryCycleDefinition _definition;
    private readonly IntegratedSecondaryCycleSolver _integratedSolver;

    public FullPlantSolver(
        IntegratedSecondaryCycleDefinition definition,
        IFluidThermodynamicModel thermodynamicModel)
    {
        _definition = definition ?? throw new ArgumentNullException(nameof(definition));
        ArgumentNullException.ThrowIfNull(thermodynamicModel);
        _integratedSolver = new IntegratedSecondaryCycleSolver(definition, thermodynamicModel);
    }

    public IntegratedSecondaryCycleDefinition Definition => _definition;

    public FullPlantStepResult Step(
        FullPlantState committedState,
        IntegratedSecondaryCycleInputs inputs,
        TimeSpan deltaTime)
        => Step(committedState, inputs, deltaTime, PlantNetworkSourceTerms.Empty);

    public FullPlantStepResult Step(
        FullPlantState committedState,
        IntegratedSecondaryCycleInputs inputs,
        TimeSpan deltaTime,
        PlantNetworkSourceTerms supplementalSourceTerms)
    {
        ArgumentNullException.ThrowIfNull(committedState);
        ArgumentNullException.ThrowIfNull(inputs);
        ArgumentNullException.ThrowIfNull(supplementalSourceTerms);

        if (!ReferenceEquals(committedState.Definition, _definition))
        {
            throw new ArgumentException("Committed full-plant state does not use this solver's canonical definition.", nameof(committedState));
        }

        if (!ReferenceEquals(inputs.Definition, _definition))
        {
            throw new ArgumentException("Integrated inputs do not use this solver's canonical definition.", nameof(inputs));
        }

        var cycleStep = _integratedSolver.Step(
            committedState.PlantState,
            committedState.TurbineState,
            committedState.ElectricalState,
            inputs,
            deltaTime,
            supplementalSourceTerms);
        var candidateState = new FullPlantState(
            _definition,
            cycleStep.CandidatePlantState,
            cycleStep.CandidateTurbineState,
            cycleStep.CandidateElectricalState);
        var performance = FullPlantPerformanceDiagnostics.From(cycleStep.Snapshot);
        var snapshot = new FullPlantSnapshot(
            cycleStep.Snapshot,
            cycleStep.CandidateTurbineState,
            cycleStep.CandidateElectricalState,
            performance);

        return new FullPlantStepResult(cycleStep, candidateState, snapshot);
    }
}
