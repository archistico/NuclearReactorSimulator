using NuclearReactorSimulator.Domain.Physics.Control.Protection;
using NuclearReactorSimulator.Domain.Physics.Control.ReactorPrimary;
using NuclearReactorSimulator.Domain.Physics.Control.TurbineSecondary;
using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Physics.Control.ReactorPrimary;
using NuclearReactorSimulator.Simulation.Physics.Control.TurbineSecondary;
using NuclearReactorSimulator.Simulation.Physics.Electrical;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using NuclearReactorSimulator.Simulation.Physics.Instrumentation;
using NuclearReactorSimulator.Simulation.Physics.Reactor.PrimaryCircuit.Integration;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Condenser;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Feedwater;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Integration;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.MainSteam;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Turbine;

namespace NuclearReactorSimulator.Simulation.Physics.Control.Protection;

/// <summary>
/// M5.5 protected automatic composition. Normal M5.3/M5.4 controls and protection evaluate one measured frame;
/// protection then has explicit command priority before the one authoritative M4.7 physical step.
/// </summary>
public sealed class ProtectedAutomaticFullPlantSolver
{
    private readonly TurbineSecondaryControlSystemDefinition _secondaryDefinition;
    private readonly ReactorPrimaryControlSolver _reactorControlSolver;
    private readonly TurbineSecondaryControlSolver _secondaryControlSolver;
    private readonly ProtectionSystemSolver _protectionSolver;
    private readonly FullPlantSolver _fullPlantSolver;
    private readonly ValveFlowSolver _valveFlowSolver = new();

    public ProtectedAutomaticFullPlantSolver(
        ReactorPrimaryControlSystemDefinition reactorDefinition,
        TurbineSecondaryControlSystemDefinition secondaryDefinition,
        ProtectionSystemDefinition protectionDefinition,
        IFluidThermodynamicModel thermodynamicModel)
    {
        ArgumentNullException.ThrowIfNull(reactorDefinition);
        _secondaryDefinition = secondaryDefinition ?? throw new ArgumentNullException(nameof(secondaryDefinition));
        ArgumentNullException.ThrowIfNull(protectionDefinition);
        ArgumentNullException.ThrowIfNull(thermodynamicModel);

        if (!ReferenceEquals(reactorDefinition.PlantDefinition, secondaryDefinition.PlantDefinition)
            || !ReferenceEquals(secondaryDefinition.PlantDefinition, protectionDefinition.PlantDefinition))
        {
            throw new ArgumentException("M5.3, M5.4 and M5.5 must use the same canonical full-plant definition.");
        }

        var instrumentation = reactorDefinition.ActuatorSystem.ControlSystem.Instrumentation;
        if (!ReferenceEquals(instrumentation, secondaryDefinition.ActuatorSystem.ControlSystem.Instrumentation)
            || !ReferenceEquals(instrumentation, protectionDefinition.Instrumentation))
        {
            throw new ArgumentException("M5.3, M5.4 and M5.5 must consume the same canonical measured-signal frame definition.");
        }

        _reactorControlSolver = new ReactorPrimaryControlSolver(reactorDefinition);
        _secondaryControlSolver = new TurbineSecondaryControlSolver(secondaryDefinition);
        _protectionSolver = new ProtectionSystemSolver(protectionDefinition);
        _fullPlantSolver = new FullPlantSolver(secondaryDefinition.PlantDefinition, thermodynamicModel);
    }

    public ProtectedAutomaticFullPlantStepResult Step(
        MeasuredSignalFrame measuredSignals,
        FullPlantState committedPlantState,
        ReactorPrimaryControlState committedReactorControlState,
        TurbineSecondaryControlState committedSecondaryControlState,
        ProtectionSystemState committedProtectionState,
        IntegratedSecondaryCycleInputs basePlantInputs,
        ReactorPrimaryControlInputs reactorInputs,
        TurbineSecondaryControlInputs secondaryInputs,
        ProtectionSystemInputs protectionInputs,
        TimeSpan deltaTime)
    {
        ArgumentNullException.ThrowIfNull(basePlantInputs);
        if (!ReferenceEquals(basePlantInputs.Definition, _secondaryDefinition.PlantDefinition))
        {
            throw new ArgumentException("Plant inputs do not use the M5.5 canonical full-plant definition.", nameof(basePlantInputs));
        }

        var protectionStep = _protectionSolver.Step(measuredSignals, committedProtectionState, protectionInputs);
        var protection = protectionStep.Snapshot;

        var reactorStep = _reactorControlSolver.Step(
            measuredSignals,
            committedPlantState,
            committedReactorControlState,
            reactorInputs,
            deltaTime,
            protection.ReactorScramActive,
            protection.RodWithdrawalInhibited);
        var secondaryStep = _secondaryControlSolver.Step(
            measuredSignals,
            reactorStep.CommandedFullPlantState,
            committedSecondaryControlState,
            secondaryInputs,
            deltaTime);

        var protectedPlantState = ApplyPlantCommandOverrides(
            committedPlantState.PlantState,
            secondaryStep.CommandedFullPlantState.PlantState,
            protection,
            out var forcedStopValves);
        var protectedFullPlantState = new FullPlantState(
            _secondaryDefinition.PlantDefinition,
            protectedPlantState,
            secondaryStep.CommandedFullPlantState.TurbineState,
            secondaryStep.CommandedFullPlantState.ElectricalState);

        var effectiveInputs = BuildEffectivePlantInputs(
            basePlantInputs,
            reactorStep.Snapshot.FissionPower.TotalFissionThermalPower,
            protectedPlantState,
            protection);
        var fullPlantStep = _fullPlantSolver.Step(protectedFullPlantState, effectiveInputs, deltaTime);

        var arbitration = new ProtectionArbitrationSnapshot(
            protection.ReactorScramActive,
            protection.TurbineTripActive,
            protection.GeneratorTripActive,
            protection.RodWithdrawalInhibited,
            protection.TurbineAdmissionOpeningInhibited,
            protection.GeneratorBreakerCloseInhibited,
            forcedStopValves);
        var snapshot = new ProtectedAutomaticControlSnapshot(
            fullPlantStep.Snapshot,
            reactorStep.Snapshot,
            secondaryStep.Snapshot,
            protection,
            arbitration);

        return new ProtectedAutomaticFullPlantStepResult(
            protectionStep,
            reactorStep,
            secondaryStep,
            fullPlantStep,
            effectiveInputs,
            snapshot);
    }

    private PlantState ApplyPlantCommandOverrides(
        PlantState committedPlantState,
        PlantState normalCommandedPlantState,
        ProtectionSystemSnapshot protection,
        out IReadOnlyList<string> forcedStopValves)
    {
        var mainSteam = _secondaryDefinition.PlantDefinition.TurbineExpansionSystem.MainSteamNetwork;
        var stopValveIds = mainSteam.AdmissionTrains.Select(static item => item.StopValveId).ToHashSet(StringComparer.Ordinal);
        var normalAdmissionValveIds = mainSteam.AdmissionTrains
            .SelectMany(static item => new[] { item.ControlValveId, item.AdmissionValveId })
            .ToHashSet(StringComparer.Ordinal);
        var forced = new List<string>();

        var valves = normalCommandedPlantState.Valves.Select(state =>
        {
            if (protection.TurbineTripActive && stopValveIds.Contains(state.ValveId))
            {
                forced.Add(state.ValveId);
                return new ValveState(state.ValveId, ValvePosition.Closed, state.IsFailSafeActive);
            }

            if (protection.TurbineAdmissionOpeningInhibited && normalAdmissionValveIds.Contains(state.ValveId))
            {
                var committed = committedPlantState.GetValve(state.ValveId);
                var position = ValvePosition.FromFraction(Math.Min(committed.Position.Fraction, state.Position.Fraction));
                return new ValveState(state.ValveId, position, state.IsFailSafeActive);
            }

            return state;
        }).ToArray();

        forcedStopValves = forced.OrderBy(static id => id, StringComparer.Ordinal).ToArray();
        return new PlantState(
            normalCommandedPlantState.Definition,
            normalCommandedPlantState.FluidNodes,
            valves,
            normalCommandedPlantState.Pumps,
            normalCommandedPlantState.ThermalBodies,
            normalCommandedPlantState.HeatSources);
    }

    private IntegratedSecondaryCycleInputs BuildEffectivePlantInputs(
        IntegratedSecondaryCycleInputs original,
        Power fissionPower,
        PlantState commandedPlantState,
        ProtectionSystemSnapshot protection)
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
        var protectedRotorInputs = turbine.RotorInputs
            .Select(input => new TurbineRotorInput(
                input.RotorId,
                input.ExternalLoadTorque,
                input.TripCommand || protection.TurbineTripActive))
            .ToArray();
        var rewrittenTurbine = new TurbineExpansionInputs(
            turbine.Definition,
            rewrittenMainSteam,
            automaticStageInputs,
            protectedRotorInputs);
        var rewrittenCondenser = new CondenserSystemInputs(
            condenser.Definition,
            rewrittenTurbine,
            condenser.CoolingBoundaryInputs);
        var rewrittenFeedwater = new CondensateFeedwaterSystemInputs(
            feedwater.Definition,
            rewrittenCondenser,
            feedwater.TrainInputs);
        var protectedGeneratorInputs = generator.GeneratorInputs.Select(input =>
        {
            var open = input.OpenBreakerCommand || protection.GeneratorTripActive;
            var close = !open && !protection.GeneratorBreakerCloseInhibited && input.CloseBreakerCommand;
            return new SynchronousGeneratorInput(
                input.GeneratorId,
                input.TerminalLineVoltage,
                input.RequestedElectricalPower,
                close,
                open);
        }).ToArray();
        var rewrittenGenerator = new GeneratorGridInputs(
            generator.Definition,
            rewrittenFeedwater,
            protectedGeneratorInputs);

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
}
