using NuclearReactorSimulator.Domain.Physics.Control.ReactorPrimary;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor.Core;
using NuclearReactorSimulator.Simulation.Physics.Electrical;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using NuclearReactorSimulator.Simulation.Physics.Instrumentation;
using NuclearReactorSimulator.Simulation.Physics.Reactor.PrimaryCircuit.Integration;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Condenser;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Feedwater;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Integration;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.MainSteam;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Turbine;

namespace NuclearReactorSimulator.Simulation.Physics.Control.ReactorPrimary;

/// <summary>
/// M5.3 thin composition boundary: measured signals drive control, existing M2 point kinetics produces the effective fission-power
/// input, typed commands are applied to existing rod/pump owners, then M4.7 performs the one authoritative physical full-plant step.
/// </summary>
public sealed class ReactorPrimaryControlledFullPlantSolver
{
    private readonly ReactorPrimaryControlSystemDefinition _definition;
    private readonly ReactorPrimaryControlSolver _controlSolver;
    private readonly FullPlantSolver _fullPlantSolver;

    public ReactorPrimaryControlledFullPlantSolver(
        ReactorPrimaryControlSystemDefinition definition,
        IFluidThermodynamicModel thermodynamicModel)
    {
        _definition = definition ?? throw new ArgumentNullException(nameof(definition));
        ArgumentNullException.ThrowIfNull(thermodynamicModel);
        _controlSolver = new ReactorPrimaryControlSolver(definition);
        _fullPlantSolver = new FullPlantSolver(definition.PlantDefinition, thermodynamicModel);
    }

    public ReactorPrimaryControlledFullPlantStepResult Step(
        MeasuredSignalFrame measuredSignals,
        FullPlantState committedPlantState,
        ReactorPrimaryControlState committedControlState,
        IntegratedSecondaryCycleInputs basePlantInputs,
        ReactorPrimaryControlInputs controlInputs,
        TimeSpan deltaTime)
    {
        ArgumentNullException.ThrowIfNull(basePlantInputs);
        if (!ReferenceEquals(basePlantInputs.Definition, _definition.PlantDefinition))
        {
            throw new ArgumentException("Plant inputs do not use the M5.3 canonical full-plant definition.", nameof(basePlantInputs));
        }

        var controlStep = _controlSolver.Step(
            measuredSignals,
            committedPlantState,
            committedControlState,
            controlInputs,
            deltaTime);
        var effectivePlantInputs = WithFissionPower(
            basePlantInputs,
            controlStep.Snapshot.FissionPower.TotalFissionThermalPower,
            _definition.QuasiSpatialCoreFeedbackDefinition is null ? null : committedControlState.CoreState);
        var fullPlantStep = _fullPlantSolver.Step(controlStep.CommandedFullPlantState, effectivePlantInputs, deltaTime);
        var snapshot = new ReactorPrimaryControlledFullPlantSnapshot(fullPlantStep.Snapshot, controlStep.Snapshot);
        return new ReactorPrimaryControlledFullPlantStepResult(controlStep, fullPlantStep, effectivePlantInputs, snapshot);
    }

    private static IntegratedSecondaryCycleInputs WithFissionPower(
        IntegratedSecondaryCycleInputs original,
        Power fissionPower,
        AggregatedCoreState? committedCoreState)
    {
        var generator = original.GeneratorGridInputs;
        var feedwater = generator.CondensateFeedwaterInputs;
        var condenser = feedwater.CondenserInputs;
        var turbine = condenser.TurbineExpansionInputs;
        var mainSteam = turbine.MainSteamInputs;
        var primary = mainSteam.PrimaryCircuitInputs;

        var rewrittenPrimary = new IntegratedPrimaryCircuitInputs(
            primary.Definition,
            committedCoreState ?? primary.CoreState,
            fissionPower,
            primary.TotalDecayHeatPower,
            primary.BoundaryInputs);
        var rewrittenMainSteam = new MainSteamNetworkInputs(
            mainSteam.Definition,
            rewrittenPrimary,
            mainSteam.TurbineAdmissionBoundaryInputs);
        var rewrittenTurbine = new TurbineExpansionInputs(
            turbine.Definition,
            rewrittenMainSteam,
            turbine.StageGroupInputs,
            turbine.RotorInputs);
        var rewrittenCondenser = new CondenserSystemInputs(
            condenser.Definition,
            rewrittenTurbine,
            condenser.CoolingBoundaryInputs);
        var rewrittenFeedwater = new CondensateFeedwaterSystemInputs(
            feedwater.Definition,
            rewrittenCondenser,
            feedwater.TrainInputs);
        var rewrittenGenerator = new GeneratorGridInputs(
            generator.Definition,
            rewrittenFeedwater,
            generator.GeneratorInputs);

        return new IntegratedSecondaryCycleInputs(original.Definition, rewrittenGenerator);
    }
}
