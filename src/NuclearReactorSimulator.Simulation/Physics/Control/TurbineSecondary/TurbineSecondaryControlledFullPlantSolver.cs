using NuclearReactorSimulator.Domain.Physics.Control.ReactorPrimary;
using NuclearReactorSimulator.Domain.Physics.Control.TurbineSecondary;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Physics.Control.ReactorPrimary;
using NuclearReactorSimulator.Simulation.Physics.Electrical;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using NuclearReactorSimulator.Simulation.Physics.Instrumentation;
using NuclearReactorSimulator.Simulation.Physics.Reactor.PrimaryCircuit.Integration;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Condenser;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Feedwater;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Integration;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.MainSteam;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Turbine;

namespace NuclearReactorSimulator.Simulation.Physics.Control.TurbineSecondary;

/// <summary>
/// M5.4 integrated automatic-control composition. Reactor/primary and secondary controllers read the same measured frame,
/// commands are applied to their existing physical owners, M4.2 turbine stage demand is derived from the commanded canonical
/// admission-valve path, and M4.7 still performs the one authoritative physical full-plant step.
/// </summary>
public sealed class TurbineSecondaryControlledFullPlantSolver
{
    private readonly TurbineSecondaryControlSystemDefinition _secondaryDefinition;
    private readonly ReactorPrimaryControlSolver _reactorControlSolver;
    private readonly TurbineSecondaryControlSolver _secondaryControlSolver;
    private readonly FullPlantSolver _fullPlantSolver;
    private readonly ValveFlowSolver _valveFlowSolver = new();

    public TurbineSecondaryControlledFullPlantSolver(
        ReactorPrimaryControlSystemDefinition reactorDefinition,
        TurbineSecondaryControlSystemDefinition secondaryDefinition,
        IFluidThermodynamicModel thermodynamicModel)
    {
        ArgumentNullException.ThrowIfNull(reactorDefinition);
        _secondaryDefinition = secondaryDefinition ?? throw new ArgumentNullException(nameof(secondaryDefinition));
        ArgumentNullException.ThrowIfNull(thermodynamicModel);

        if (!ReferenceEquals(reactorDefinition.PlantDefinition, secondaryDefinition.PlantDefinition))
        {
            throw new ArgumentException("M5.3 and M5.4 control systems must use the same canonical full-plant definition.");
        }
        if (!ReferenceEquals(
                reactorDefinition.ActuatorSystem.ControlSystem.Instrumentation,
                secondaryDefinition.ActuatorSystem.ControlSystem.Instrumentation))
        {
            throw new ArgumentException("M5.3 and M5.4 control systems must consume the same canonical measured-signal frame definition.");
        }

        ValidateDisjointPhysicalTargets(reactorDefinition, secondaryDefinition);
        _reactorControlSolver = new ReactorPrimaryControlSolver(reactorDefinition);
        _secondaryControlSolver = new TurbineSecondaryControlSolver(secondaryDefinition);
        _fullPlantSolver = new FullPlantSolver(secondaryDefinition.PlantDefinition, thermodynamicModel);
    }

    public TurbineSecondaryControlledFullPlantStepResult Step(
        MeasuredSignalFrame measuredSignals,
        FullPlantState committedPlantState,
        ReactorPrimaryControlState committedReactorControlState,
        TurbineSecondaryControlState committedSecondaryControlState,
        IntegratedSecondaryCycleInputs basePlantInputs,
        ReactorPrimaryControlInputs reactorInputs,
        TurbineSecondaryControlInputs secondaryInputs,
        TimeSpan deltaTime)
    {
        ArgumentNullException.ThrowIfNull(basePlantInputs);
        if (!ReferenceEquals(basePlantInputs.Definition, _secondaryDefinition.PlantDefinition))
        {
            throw new ArgumentException("Plant inputs do not use the M5.4 canonical full-plant definition.", nameof(basePlantInputs));
        }

        var reactorStep = _reactorControlSolver.Step(
            measuredSignals,
            committedPlantState,
            committedReactorControlState,
            reactorInputs,
            deltaTime);
        var secondaryStep = _secondaryControlSolver.Step(
            measuredSignals,
            reactorStep.CommandedFullPlantState,
            committedSecondaryControlState,
            secondaryInputs,
            deltaTime);

        var effectiveInputs = BuildEffectivePlantInputs(
            basePlantInputs,
            reactorStep.Snapshot.FissionPower.TotalFissionThermalPower,
            secondaryStep.CommandedFullPlantState.PlantState);
        var fullPlantStep = _fullPlantSolver.Step(secondaryStep.CommandedFullPlantState, effectiveInputs, deltaTime);
        var snapshot = new IntegratedAutomaticControlSnapshot(
            fullPlantStep.Snapshot,
            reactorStep.Snapshot,
            secondaryStep.Snapshot);

        return new TurbineSecondaryControlledFullPlantStepResult(
            reactorStep,
            secondaryStep,
            fullPlantStep,
            effectiveInputs,
            snapshot);
    }

    private IntegratedSecondaryCycleInputs BuildEffectivePlantInputs(
        IntegratedSecondaryCycleInputs original,
        Power fissionPower,
        PlantState commandedPlantState)
    {
        var generator = original.GeneratorGridInputs;
        var feedwater = generator.CondensateFeedwaterInputs;
        var condenser = feedwater.CondenserInputs;
        var turbine = condenser.TurbineExpansionInputs;
        var mainSteam = turbine.MainSteamInputs;
        var primary = mainSteam.PrimaryCircuitInputs;

        var rewrittenPrimary = new IntegratedPrimaryCircuitInputs(
            primary.Definition,
            primary.CoreState,
            fissionPower,
            primary.TotalDecayHeatPower,
            primary.BoundaryInputs);
        var rewrittenMainSteam = new MainSteamNetworkInputs(
            mainSteam.Definition,
            rewrittenPrimary,
            mainSteam.TurbineAdmissionBoundaryInputs);
        var automaticStageInputs = turbine.Definition.StageGroups
            .Select(stage => new TurbineStageGroupInput(stage.Id, ResolveAdmissionMassFlow(commandedPlantState, stage.AdmissionBoundaryId)))
            .ToArray();
        var rewrittenTurbine = new TurbineExpansionInputs(
            turbine.Definition,
            rewrittenMainSteam,
            automaticStageInputs,
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

    private MassFlowRate ResolveAdmissionMassFlow(PlantState commandedPlantState, string admissionBoundaryId)
    {
        var mainSteam = _secondaryDefinition.PlantDefinition.TurbineExpansionSystem.MainSteamNetwork;
        var boundary = mainSteam.GetTurbineAdmissionBoundary(admissionBoundaryId);
        var train = mainSteam.GetAdmissionTrain(boundary.AdmissionTrainId);
        var stopFlow = SolvePositiveValveFlow(commandedPlantState, train.StopValveId);
        var controlFlow = SolvePositiveValveFlow(commandedPlantState, train.ControlValveId);
        var admissionFlow = SolvePositiveValveFlow(commandedPlantState, train.AdmissionValveId);
        return MassFlowRate.FromKilogramsPerSecond(Math.Min(stopFlow, Math.Min(controlFlow, admissionFlow)));
    }

    private double SolvePositiveValveFlow(PlantState state, string valveId)
    {
        var definition = state.Definition.GetValve(valveId);
        var flow = _valveFlowSolver.Solve(
            definition,
            state.GetValve(valveId),
            state.GetFluidNode(definition.Pipe.FromNodeId),
            state.GetFluidNode(definition.Pipe.ToNodeId));
        return Math.Max(0d, flow.MassFlowRate.KilogramsPerSecond);
    }

    private static void ValidateDisjointPhysicalTargets(
        ReactorPrimaryControlSystemDefinition reactorDefinition,
        TurbineSecondaryControlSystemDefinition secondaryDefinition)
    {
        var reactorTargets = reactorDefinition.ActuatorSystem.Actuators
            .Select(static actuator => $"{actuator.TargetKind}:{actuator.TargetId}")
            .ToHashSet(StringComparer.Ordinal);
        var conflict = secondaryDefinition.ActuatorSystem.Actuators
            .Select(static actuator => $"{actuator.TargetKind}:{actuator.TargetId}")
            .FirstOrDefault(reactorTargets.Contains);
        if (conflict is not null)
        {
            throw new ArgumentException($"M5.3 and M5.4 cannot command the same physical actuator target '{conflict}' in the same automatic-control composition.");
        }
    }
}
