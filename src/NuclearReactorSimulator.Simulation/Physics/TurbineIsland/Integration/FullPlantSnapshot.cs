using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Simulation.Physics.Electrical;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Turbine;
using NuclearReactorSimulator.Simulation.Plant;

namespace NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Integration;

/// <summary>
/// M4.7 plant-level snapshot boundary intended to become the true-state source for M5 instrumentation and controls.
/// </summary>
public sealed class FullPlantSnapshot
{
    public FullPlantSnapshot(
        IntegratedSecondaryCycleSnapshot integratedCycle,
        TurbineExpansionState candidateTurbineState,
        GeneratorGridState candidateElectricalState,
        FullPlantPerformanceDiagnostics performance)
    {
        IntegratedCycle = integratedCycle ?? throw new ArgumentNullException(nameof(integratedCycle));
        CandidateTurbineState = candidateTurbineState ?? throw new ArgumentNullException(nameof(candidateTurbineState));
        CandidateElectricalState = candidateElectricalState ?? throw new ArgumentNullException(nameof(candidateElectricalState));
        Performance = performance ?? throw new ArgumentNullException(nameof(performance));

        if (!ReferenceEquals(candidateTurbineState.Definition, integratedCycle.Definition.TurbineExpansionSystem))
        {
            throw new ArgumentException("Candidate turbine state does not use the snapshot's canonical turbine definition.", nameof(candidateTurbineState));
        }

        if (!ReferenceEquals(candidateElectricalState.Definition, integratedCycle.Definition.GeneratorGridSystem))
        {
            throw new ArgumentException("Candidate electrical state does not use the snapshot's canonical generator/grid definition.", nameof(candidateElectricalState));
        }
    }

    public IntegratedSecondaryCycleSnapshot IntegratedCycle { get; }

    public PlantSnapshot CandidatePlant => IntegratedCycle.PrimaryCircuit.CandidatePlant;

    public TurbineExpansionState CandidateTurbineState { get; }

    public GeneratorGridState CandidateElectricalState { get; }

    public FullPlantPerformanceDiagnostics Performance { get; }

    public SecondaryCycleHeatBalanceAudit HeatBalance => IntegratedCycle.HeatBalance;

    public Power ReactorThermalPower => Performance.ReactorThermalPower;

    public Power GrossElectricalOutputPower => Performance.GrossElectricalOutputPower;
}
