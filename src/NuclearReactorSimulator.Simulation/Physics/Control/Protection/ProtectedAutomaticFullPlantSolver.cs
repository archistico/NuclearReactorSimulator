using NuclearReactorSimulator.Domain.Physics.Control.Protection;
using NuclearReactorSimulator.Domain.Physics.Control.ReactorPrimary;
using NuclearReactorSimulator.Domain.Physics.Control.TurbineSecondary;
using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Physics.Control.ReactorPrimary;
using NuclearReactorSimulator.Simulation.Physics.Control.Integration;
using NuclearReactorSimulator.Simulation.Physics.Control.TurbineSecondary;
using NuclearReactorSimulator.Simulation.Physics.Electrical;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using NuclearReactorSimulator.Simulation.Physics.Instrumentation;
using NuclearReactorSimulator.Simulation.Physics.Reactor.PrimaryCircuit.Integration;
using NuclearReactorSimulator.Simulation.Physics.Thermal;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Condenser;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Feedwater;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Integration;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.MainSteam;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Turbine;
using NuclearReactorSimulator.Simulation.Plant;

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
    private readonly IFluidThermodynamicModel _thermodynamicModel;
    private readonly TurbineStageMassFlowResolver _stageMassFlowResolver;

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
        _thermodynamicModel = thermodynamicModel;
        _fullPlantSolver = new FullPlantSolver(secondaryDefinition.PlantDefinition, thermodynamicModel);
        _stageMassFlowResolver = new TurbineStageMassFlowResolver(secondaryDefinition.PlantDefinition.TurbineExpansionSystem);
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
        => Step(measuredSignals, committedPlantState, committedReactorControlState, committedSecondaryControlState,
            committedProtectionState, basePlantInputs, reactorInputs, secondaryInputs, protectionInputs, deltaTime,
            HydraulicComponentFaultInputs.Empty);

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
        TimeSpan deltaTime,
        HydraulicComponentFaultInputs hydraulicFaultInputs)
    {
        ArgumentNullException.ThrowIfNull(basePlantInputs);
        ArgumentNullException.ThrowIfNull(hydraulicFaultInputs);
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
            basePlantInputs,
            deltaTime);

        var protectedPlantState = ApplyPlantCommandOverrides(
            committedPlantState.PlantState,
            secondaryStep.CommandedFullPlantState.PlantState,
            protection,
            out var forcedStopValves);
        var faultedPlantState = ApplyHydraulicFaultOverrides(protectedPlantState, hydraulicFaultInputs);
        var protectedFullPlantState = new FullPlantState(
            _secondaryDefinition.PlantDefinition,
            faultedPlantState,
            secondaryStep.CommandedFullPlantState.TurbineState,
            secondaryStep.CommandedFullPlantState.ElectricalState);

        var effectiveInputs = BuildEffectivePlantInputs(
            basePlantInputs,
            reactorStep.Snapshot.FissionPower.TotalFissionThermalPower,
            faultedPlantState,
            protection,
            deltaTime);
        var leakSourceTerms = BuildHydraulicLeakSourceTerms(
            faultedPlantState,
            hydraulicFaultInputs,
            deltaTime);
        var fullPlantStep = _fullPlantSolver.Step(
            protectedFullPlantState,
            effectiveInputs,
            deltaTime,
            leakSourceTerms);

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

    private static PlantState ApplyHydraulicFaultOverrides(PlantState state, HydraulicComponentFaultInputs faults)
    {
        var pumpById = faults.PumpFaults.ToDictionary(static x => x.PumpId, StringComparer.Ordinal);
        var valveById = faults.ValveFaults.ToDictionary(static x => x.ValveId, StringComparer.Ordinal);
        var restrictions = faults.PathRestrictions
            .GroupBy(static x => x.ValveId, StringComparer.Ordinal)
            .ToDictionary(static g => g.Key, static g => g.Min(static x => x.MaximumOpenFraction), StringComparer.Ordinal);

        foreach (var pumpId in pumpById.Keys)
        {
            _ = state.Definition.GetPump(pumpId);
        }
        foreach (var valveId in valveById.Keys.Concat(restrictions.Keys).Distinct(StringComparer.Ordinal))
        {
            _ = state.Definition.GetValve(valveId);
        }

        var pumps = state.Pumps.Select(pump =>
        {
            if (!pumpById.TryGetValue(pump.PumpId, out var fault))
            {
                return pump;
            }
            var running = pump.IsRunning && !fault.ForceTrip && fault.CapacityFraction > 0d;
            var speed = PumpSpeed.FromFraction(pump.Speed.Fraction * fault.CapacityFraction);
            return new PumpState(pump.PumpId, speed, running);
        }).ToArray();

        var valves = state.Valves.Select(valve =>
        {
            var position = valve.Position;
            if (valveById.TryGetValue(valve.ValveId, out var fault))
            {
                position = fault.Mode switch
                {
                    HydraulicValveFaultMode.FailOpen => ValvePosition.FullyOpen,
                    HydraulicValveFaultMode.FailClosed => ValvePosition.Closed,
                    HydraulicValveFaultMode.Stuck => fault.StuckPosition,
                    _ => throw new ArgumentOutOfRangeException(nameof(faults), fault.Mode, "Unknown hydraulic valve-fault mode."),
                };
            }
            if (restrictions.TryGetValue(valve.ValveId, out var maximumOpenFraction))
            {
                position = ValvePosition.FromFraction(Math.Min(position.Fraction, maximumOpenFraction));
            }
            return position == valve.Position ? valve : new ValveState(valve.ValveId, position, valve.IsFailSafeActive);
        }).ToArray();

        return new PlantState(state.Definition, state.FluidNodes, valves, pumps, state.ThermalBodies, state.HeatSources);
    }

    private PlantNetworkSourceTerms BuildHydraulicLeakSourceTerms(
        PlantState state,
        HydraulicComponentFaultInputs faults,
        TimeSpan deltaTime)
    {
        if (faults.Leaks.Count == 0 && faults.PressureDrivenBreaks.Count == 0)
        {
            return PlantNetworkSourceTerms.Empty;
        }

        var balances = new Dictionary<string, FluidNodeBalance>(StringComparer.Ordinal);
        var totalLeak = MassFlowRate.Zero;
        var externalPower = Power.Zero;

        void AccumulateRemoval(string fluidNodeId, MassFlowRate massFlowRate)
        {
            if (massFlowRate <= MassFlowRate.Zero)
            {
                return;
            }

            var node = state.GetFluidNode(fluidNodeId);
            var removalMass = MassFlowRate.FromKilogramsPerSecond(-massFlowRate.KilogramsPerSecond);
            var removalPower = node.SpecificInternalEnergy * removalMass;
            var balance = new FluidNodeBalance(removalMass, removalPower);
            balances[node.Id] = balances.TryGetValue(node.Id, out var existing) ? existing + balance : balance;
            totalLeak += removalMass;
            externalPower += removalPower;
        }

        foreach (var leak in faults.Leaks.OrderBy(static x => x.FaultId, StringComparer.Ordinal))
        {
            AccumulateRemoval(leak.FluidNodeId, leak.MassFlowRate);
        }

        foreach (var pressureBreak in faults.PressureDrivenBreaks.OrderBy(static x => x.FaultId, StringComparer.Ordinal))
        {
            var node = state.GetFluidNode(pressureBreak.FluidNodeId);
            var drivingPressurePascals = Math.Max(0d, node.Pressure.Pascals - pressureBreak.AmbientPressure.Pascals);
            if (drivingPressurePascals <= 0d)
            {
                continue;
            }

            // Educational bounded break law: flow scales with sqrt(dP) up to the declared reference flow.
            // This is intentionally not a critical-flow/two-phase discharge correlation.
            var normalizedDrivingPressure = Math.Min(
                1d,
                drivingPressurePascals / pressureBreak.ReferencePressureDifference.Pascals);
            var pressureDrivenMassFlow = MassFlowRate.FromKilogramsPerSecond(
                pressureBreak.ReferenceMassFlowRate.KilogramsPerSecond * Math.Sqrt(normalizedDrivingPressure));
            var inventoryBoundMassFlow = (node.Mass * pressureBreak.MaximumInventoryFractionPerStep).Per(deltaTime);
            var declaredBoundMassFlow = MassFlowRate.FromKilogramsPerSecond(Math.Min(
                pressureDrivenMassFlow.KilogramsPerSecond,
                inventoryBoundMassFlow.KilogramsPerSecond));
            var effectiveMassFlow = LimitPressureDrivenBreakToThermodynamicEnvelope(
                node,
                declaredBoundMassFlow,
                deltaTime);

            AccumulateRemoval(pressureBreak.FluidNodeId, effectiveMassFlow);
        }

        return new PlantNetworkSourceTerms(
            balances,
            new Dictionary<string, ThermalEnergyBalance>(StringComparer.Ordinal),
            totalLeak,
            externalPower);
    }

    private MassFlowRate LimitPressureDrivenBreakToThermodynamicEnvelope(
        FluidNodeState committedNode,
        MassFlowRate proposedMassFlow,
        TimeSpan deltaTime)
    {
        if (proposedMassFlow <= MassFlowRate.Zero)
        {
            return MassFlowRate.Zero;
        }

        var proposedRemovalKilograms = proposedMassFlow.KilogramsPerSecond * deltaTime.TotalSeconds;
        if (CanResolvePressureDrivenBreakCandidate(committedNode, proposedRemovalKilograms))
        {
            return proposedMassFlow;
        }

        // Zero loss is the committed, already-resolved state. Bisect only the additional M8.5 removal and keep
        // a deterministic margin inside the simplified water/steam closure envelope.
        var lowerRemovalKilograms = 0d;
        var upperRemovalKilograms = proposedRemovalKilograms;
        for (var iteration = 0; iteration < 64; iteration++)
        {
            var middleRemovalKilograms = (lowerRemovalKilograms + upperRemovalKilograms) / 2d;
            if (CanResolvePressureDrivenBreakCandidate(committedNode, middleRemovalKilograms))
            {
                lowerRemovalKilograms = middleRemovalKilograms;
            }
            else
            {
                upperRemovalKilograms = middleRemovalKilograms;
            }
        }

        var safeRemovalKilograms = lowerRemovalKilograms * 0.5d;
        return safeRemovalKilograms <= 0d
            ? MassFlowRate.Zero
            : MassFlowRate.FromKilogramsPerSecond(safeRemovalKilograms / deltaTime.TotalSeconds);
    }

    private bool CanResolvePressureDrivenBreakCandidate(
        FluidNodeState committedNode,
        double removalKilograms)
    {
        if (!double.IsFinite(removalKilograms) || removalKilograms < 0d)
        {
            return false;
        }

        var candidateMassKilograms = committedNode.Mass.Kilograms - removalKilograms;
        if (!double.IsFinite(candidateMassKilograms) || candidateMassKilograms <= 0d)
        {
            return false;
        }

        // M8.5 removes carried internal energy at the committed node's specific internal energy, so this probe
        // preserves the break source-term semantics while asking only whether the resulting extensive inventory
        // remains resolvable by the existing thermodynamic owner.
        var candidateEnergyJoules = committedNode.InternalEnergy.Joules
            - (committedNode.SpecificInternalEnergy.JoulesPerKilogram * removalKilograms);
        if (!double.IsFinite(candidateEnergyJoules))
        {
            return false;
        }

        var candidateInventory = new FluidNodeInventory(
            Mass.FromKilograms(candidateMassKilograms),
            Energy.FromJoules(candidateEnergyJoules));

        try
        {
            _ = _thermodynamicModel.Resolve(
                committedNode.Definition,
                candidateInventory,
                committedNode.Thermodynamics);
            return true;
        }
        catch (WaterSteamStateOutOfRangeException)
        {
            return false;
        }
    }

    private IntegratedSecondaryCycleInputs BuildEffectivePlantInputs(
        IntegratedSecondaryCycleInputs original,
        Power fissionPower,
        PlantState commandedPlantState,
        ProtectionSystemSnapshot protection,
        TimeSpan deltaTime)
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
            .Select(stage => new TurbineStageGroupInput(stage.Id, _stageMassFlowResolver.Resolve(commandedPlantState, stage, deltaTime)))
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


}
