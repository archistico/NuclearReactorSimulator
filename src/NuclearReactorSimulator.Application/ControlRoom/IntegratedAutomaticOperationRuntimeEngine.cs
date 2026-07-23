using NuclearReactorSimulator.Application.ControlRoom.Automation;
using NuclearReactorSimulator.Application.Scenarios.Faults.ElectricalLoss;
using NuclearReactorSimulator.Application.Scenarios.Faults.Hydraulics;
using NuclearReactorSimulator.Application.Scenarios.Faults.InstrumentationControl;
using NuclearReactorSimulator.Application.Scenarios.Faults.LossOfCoolant;
using NuclearReactorSimulator.Application.Scenarios.Faults.SecondaryTransients;
using NuclearReactorSimulator.Domain.Physics.Control;
using NuclearReactorSimulator.Domain.Physics.Control.ReactorPrimary;
using NuclearReactorSimulator.Domain.Physics.Control.Supervisory;
using NuclearReactorSimulator.Domain.Physics.Control.TurbineSecondary;
using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Instrumentation;
using NuclearReactorSimulator.Domain.Physics.Reactor.ControlRods;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Simulation.Physics.Control;
using NuclearReactorSimulator.Simulation.Physics.Control.Alarms;
using NuclearReactorSimulator.Simulation.Physics.Control.Integration;
using NuclearReactorSimulator.Simulation.Physics.Control.Protection;
using NuclearReactorSimulator.Simulation.Physics.Control.ReactorPrimary;
using NuclearReactorSimulator.Simulation.Physics.Control.Supervisory;
using NuclearReactorSimulator.Simulation.Physics.Control.TurbineSecondary;
using NuclearReactorSimulator.Simulation.Physics.Electrical;
using NuclearReactorSimulator.Simulation.Physics.Instrumentation;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Condenser;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Feedwater;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Integration;

namespace NuclearReactorSimulator.Application.ControlRoom;

/// <summary>
/// Concrete Application adapter from operator intents to the validated M5.7 automatic-operation runtime. Persistent
/// controller setpoints and M4.5 requested electrical load modify only immutable per-step input bundles; trip, breaker and
/// annunciator commands are one-step pulses cleared after the next deterministic step.
/// </summary>
public sealed class IntegratedAutomaticOperationRuntimeEngine :
    IControlRoomRuntimeEngine,
    IPlantControlAuthorityRuntimeEngine,
    IElectricalLossFaultTarget,
    IHydraulicComponentFaultTarget,
    IInstrumentationControlFaultTarget,
    ISecondaryTransientFaultTarget,
    ILossOfCoolantFaultTarget
{
    private sealed record ControlCommandFaultOverride(
        string FaultId,
        string ControllerId,
        double ManualOutput);

    private sealed record CondenserCoolingFaultOverride(
        string FaultId,
        string BoundaryId,
        double CapacityFraction);

    private sealed record ExternalSupplyLossOverride(
        string FaultId,
        string GridId);

    private readonly IntegratedAutomaticOperationSolver _solver;
    private readonly TimeSpan _deltaTime;
    private readonly ControlRoomRuntimeCommandPolicy _policy;
    private readonly SupervisoryOperationCoordinator _supervisoryCoordinator = new();
    private IntegratedAutomaticOperationState _state;
    private IntegratedAutomaticOperationInputs _persistentInputs;
    private IntegratedAutomaticOperationSnapshot _lastSnapshot;
    private long _logicalStep;
    private PlantControlAuthorityMode _requestedPlantControlAuthority;
    private SupervisoryOperatingObjective? _requestedSupervisoryObjective;
    private SupervisoryOperationState _supervisoryState;

    private bool _manualReactorScram;
    private bool _manualTurbineTrip;
    private bool _manualGeneratorTrip;
    private bool _protectionResetRequested;
    private bool _acknowledgeAll;
    private bool _resetAll;
    private readonly HashSet<string> _acknowledgeAlarmIds = new(StringComparer.Ordinal);
    private readonly HashSet<string> _resetAlarmIds = new(StringComparer.Ordinal);
    private readonly Dictionary<string, bool> _breakerCloseById = new(StringComparer.Ordinal);
    private readonly Dictionary<string, PumpHydraulicFaultInput> _hydraulicPumpFaults = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ValveHydraulicFaultInput> _hydraulicValveFaults = new(StringComparer.Ordinal);
    private readonly Dictionary<string, HydraulicPathRestrictionInput> _hydraulicPathRestrictions = new(StringComparer.Ordinal);
    private readonly Dictionary<string, HydraulicLeakInput> _hydraulicLeaks = new(StringComparer.Ordinal);
    private readonly Dictionary<string, PressureDrivenBreakInput> _pressureDrivenBreaks = new(StringComparer.Ordinal);
    private readonly Dictionary<string, SensorFaultInput> _instrumentationFaults = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ControlCommandFaultOverride> _controlCommandFaults = new(StringComparer.Ordinal);
    private readonly Dictionary<string, CondenserCoolingFaultOverride> _condenserCoolingFaults = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ExternalSupplyLossOverride> _externalSupplyLosses = new(StringComparer.Ordinal);

    public IntegratedAutomaticOperationRuntimeEngine(
        IntegratedAutomaticOperationSolver solver,
        IntegratedAutomaticOperationState initialState,
        IntegratedAutomaticOperationInputs persistentInputs,
        IntegratedAutomaticOperationSnapshot initialSnapshot,
        TimeSpan deltaTime,
        long initialLogicalStep = 0,
        ControlRoomRuntimeCommandPolicy? commandPolicy = null)
    {
        _solver = solver ?? throw new ArgumentNullException(nameof(solver));
        _state = initialState ?? throw new ArgumentNullException(nameof(initialState));
        _persistentInputs = persistentInputs ?? throw new ArgumentNullException(nameof(persistentInputs));
        _lastSnapshot = initialSnapshot ?? throw new ArgumentNullException(nameof(initialSnapshot));
        if (deltaTime <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(deltaTime));
        }
        if (initialLogicalStep < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(initialLogicalStep));
        }

        ValidatePersistentBaseline(persistentInputs);
        _deltaTime = deltaTime;
        _logicalStep = initialLogicalStep;
        _policy = commandPolicy ?? ControlRoomRuntimeCommandPolicy.Default;
        _requestedPlantControlAuthority = InferInitialAuthority(persistentInputs);
        _supervisoryState = SupervisoryOperationState.CreateInitial(_requestedPlantControlAuthority);
    }

    public long LogicalStep => _logicalStep;

    public IntegratedAutomaticOperationState CurrentState => _state;

    public IntegratedAutomaticOperationInputs PersistentInputs => _persistentInputs;

    public ControlRoomSnapshot CreatePresentationSnapshot(ControlRoomRunState runState)
        => ControlRoomSnapshotProjector.Project(_logicalStep, runState, _lastSnapshot);

    public ControlRoomSnapshot Step(ControlRoomRunState runState)
    {
        if (runState == ControlRoomRunState.ShellOnly)
        {
            throw new ArgumentException("A connected automatic-operation runtime cannot step in ShellOnly state.", nameof(runState));
        }

        ApplySupervisoryDecision();
        var stepInputs = BuildStepInputs();
        var result = _solver.Step(_state, stepInputs, _deltaTime);
        _state = result.CandidateState;
        _lastSnapshot = result.Snapshot;
        _logicalStep++;
        ClearTransientCommands();
        return ControlRoomSnapshotProjector.Project(_logicalStep, runState, _lastSnapshot);
    }

    public PlantControlAuthorityPresentationSnapshot CreateAutomationSnapshot()
    {
        var controllerModes = _persistentInputs.ReactorPrimaryInputs.Controllers.Controllers
            .Select(input => CreateControllerModeSnapshot(
                "REACTOR / PRIMARY",
                _persistentInputs.ReactorPrimaryInputs.Controllers.Definition,
                input))
            .Concat(_persistentInputs.TurbineSecondaryInputs.Controllers.Controllers.Select(input => CreateControllerModeSnapshot(
                "TURBINE / SECONDARY",
                _persistentInputs.TurbineSecondaryInputs.Controllers.Definition,
                input)))
            .OrderBy(static item => item.Area, StringComparer.Ordinal)
            .ThenBy(static item => item.ControllerId, StringComparer.Ordinal)
            .ToArray();

        return new PlantControlAuthorityPresentationSnapshot(
            true,
            _requestedPlantControlAuthority,
            _supervisoryState.EffectiveAuthority,
            _supervisoryState.Health,
            _supervisoryState.DegradationReason,
            FormatObjective(_requestedSupervisoryObjective),
            _supervisoryState.TransitionSequence,
            controllerModes);
    }

    public void RequestPlantControlAuthority(PlantControlAuthorityMode mode)
    {
        if (!Enum.IsDefined(mode))
        {
            throw new ArgumentOutOfRangeException(nameof(mode));
        }

        _requestedPlantControlAuthority = mode;
        if (mode == PlantControlAuthorityMode.Manual)
        {
            // Manual takeover is an explicit supervisory disengagement. Re-entry into Supervisory must therefore use a
            // fresh bounded objective rather than silently resuming a stale pre-takeover target.
            _requestedSupervisoryObjective = null;
        }
    }

    public void RequestSupervisoryObjective(SupervisoryObjectiveRequest objective)
    {
        ArgumentNullException.ThrowIfNull(objective);
        _requestedSupervisoryObjective = ResolveObjective(objective);
    }

    public void QueueOperatorCommand(ControlRoomCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        switch (command.Kind)
        {
            case ControlRoomCommandKind.ReactorScram:
                RequireUntargeted(command);
                _manualReactorScram = true;
                break;
            case ControlRoomCommandKind.ProtectionReset:
                RequireUntargeted(command);
                _protectionResetRequested = true;
                break;
            case ControlRoomCommandKind.TurbineTrip:
                RequireUntargeted(command);
                _manualTurbineTrip = true;
                break;
            case ControlRoomCommandKind.GeneratorTrip:
                RequireUntargeted(command);
                _manualGeneratorTrip = true;
                break;
            case ControlRoomCommandKind.ControlRodInsert:
                SetRodManualCommand(command, ControlRodMotion.Insert);
                break;
            case ControlRoomCommandKind.ControlRodHold:
                SetRodManualCommand(command, ControlRodMotion.Hold);
                break;
            case ControlRoomCommandKind.ControlRodWithdraw:
                SetRodManualCommand(command, ControlRodMotion.Withdraw);
                break;
            case ControlRoomCommandKind.MainCirculationPumpStart:
                SetPumpManualCommand(command, run: true);
                break;
            case ControlRoomCommandKind.MainCirculationPumpStop:
                SetPumpManualCommand(command, run: false);
                break;
            case ControlRoomCommandKind.TurbineSpeedRaise:
                AdjustSecondarySetpoint(command, TurbineSecondaryControlLoopKind.TurbineSpeedAdmission, _policy.TurbineSpeedSetpointIncrementRpm);
                break;
            case ControlRoomCommandKind.TurbineSpeedLower:
                AdjustSecondarySetpoint(command, TurbineSecondaryControlLoopKind.TurbineSpeedAdmission, -_policy.TurbineSpeedSetpointIncrementRpm);
                break;
            case ControlRoomCommandKind.GeneratorLoadRaise:
                AdjustGeneratorLoadRequest(command, _policy.GeneratorLoadSetpointIncrementWatts);
                break;
            case ControlRoomCommandKind.GeneratorLoadLower:
                AdjustGeneratorLoadRequest(command, -_policy.GeneratorLoadSetpointIncrementWatts);
                break;
            case ControlRoomCommandKind.GeneratorBreakerClose:
                SetBreakerCommand(command, close: true);
                break;
            case ControlRoomCommandKind.GeneratorBreakerOpen:
                SetBreakerCommand(command, close: false);
                break;
            case ControlRoomCommandKind.AlarmAcknowledge:
                _acknowledgeAlarmIds.Add(RequireTarget(command, ControlRoomCommandTargetKind.Alarm));
                break;
            case ControlRoomCommandKind.AlarmReset:
                _resetAlarmIds.Add(RequireTarget(command, ControlRoomCommandTargetKind.Alarm));
                break;
            case ControlRoomCommandKind.AlarmAcknowledgeAll:
                RequireUntargeted(command);
                _acknowledgeAll = true;
                break;
            case ControlRoomCommandKind.AlarmResetAll:
                RequireUntargeted(command);
                _resetAll = true;
                break;
            case ControlRoomCommandKind.Run:
            case ControlRoomCommandKind.Pause:
            case ControlRoomCommandKind.SingleStep:
                throw new InvalidOperationException($"Runtime-control command '{command.Kind}' is owned by {nameof(ControlRoomRuntimeCoordinator)}.");
            default:
                throw new ArgumentOutOfRangeException(nameof(command), command.Kind, "Unsupported control-room command kind.");
        }
    }

    public void ActivatePumpTrip(string faultId, string pumpId)
        => SetPumpFault(faultId, pumpId, forceTrip: true, capacityFraction: 0d);

    public void ActivatePumpDegradation(string faultId, string pumpId, double capacityFraction)
        => SetPumpFault(faultId, pumpId, forceTrip: false, capacityFraction);

    public void ActivateValveFailOpen(string faultId, string valveId)
        => SetValveFault(faultId, valveId, HydraulicValveFaultMode.FailOpen, ValvePosition.FullyOpen);

    public void ActivateValveFailClosed(string faultId, string valveId)
        => SetValveFault(faultId, valveId, HydraulicValveFaultMode.FailClosed, ValvePosition.Closed);

    public void ActivateValveStuck(string faultId, string valveId)
    {
        ValidateFaultId(faultId);
        var valve = _state.PlantState.PlantState.GetValve(valveId);
        SetValveFault(faultId, valveId, HydraulicValveFaultMode.Stuck, valve.Position);
    }

    public void ActivatePathRestriction(string faultId, string valveId, double maximumOpenFraction)
    {
        ValidateFaultId(faultId);
        _ = _state.PlantState.PlantState.Definition.GetValve(valveId);
        _hydraulicPathRestrictions[faultId] = new HydraulicPathRestrictionInput(faultId, valveId, maximumOpenFraction);
    }

    public void ActivateLeak(string faultId, string fluidNodeId, MassFlowRate massFlowRate)
    {
        ValidateFaultId(faultId);
        _ = _state.PlantState.PlantState.GetFluidNode(fluidNodeId);
        EnsureNoOtherLossBoundary(faultId, fluidNodeId);
        _hydraulicLeaks[faultId] = new HydraulicLeakInput(faultId, fluidNodeId, massFlowRate);
    }

    public void ClearHydraulicFault(string faultId)
    {
        ValidateFaultId(faultId);
        _hydraulicPumpFaults.Remove(faultId);
        _hydraulicValveFaults.Remove(faultId);
        _hydraulicPathRestrictions.Remove(faultId);
        _hydraulicLeaks.Remove(faultId);
    }

    public void ActivatePressureDrivenBreak(
        string faultId,
        string fluidNodeId,
        MassFlowRate referenceMassFlowRate,
        Pressure ambientPressure,
        PressureDifference referencePressureDifference,
        double maximumInventoryFractionPerStep)
    {
        ValidateFaultId(faultId);
        _ = _state.PlantState.PlantState.GetFluidNode(fluidNodeId);
        EnsureNoOtherLossBoundary(faultId, fluidNodeId);
        _pressureDrivenBreaks[faultId] = new PressureDrivenBreakInput(
            faultId,
            fluidNodeId,
            referenceMassFlowRate,
            ambientPressure,
            referencePressureDifference,
            maximumInventoryFractionPerStep);
    }

    public void ClearLossOfCoolantFault(string faultId)
    {
        ValidateFaultId(faultId);
        _pressureDrivenBreaks.Remove(faultId);
    }

    public void ActivateSensorBias(string faultId, string channelId, double biasEngineeringUnits)
        => SetSensorFault(faultId, channelId, SensorFaultMode.Bias, biasEngineeringUnits);

    public void ActivateSensorFreeze(string faultId, string channelId)
        => SetSensorFault(faultId, channelId, SensorFaultMode.Freeze);

    public void ActivateSensorFailedLow(string faultId, string channelId)
        => SetSensorFault(faultId, channelId, SensorFaultMode.FailedLow);

    public void ActivateSensorFailedHigh(string faultId, string channelId)
        => SetSensorFault(faultId, channelId, SensorFaultMode.FailedHigh);

    public void ActivateSensorUnavailable(string faultId, string channelId)
        => SetSensorFault(faultId, channelId, SensorFaultMode.Unavailable);

    public void ActivateControllerOutputFreeze(string faultId, string controllerId)
    {
        var (_, state) = ResolveController(controllerId);
        SetControlCommandFault(faultId, controllerId, state.LastOutput);
    }

    public void ActivateControllerOutputFailLow(string faultId, string controllerId)
    {
        var (definition, _) = ResolveController(controllerId);
        SetControlCommandFault(faultId, controllerId, definition.OutputRange.Minimum);
    }

    public void ActivateControllerOutputFailHigh(string faultId, string controllerId)
    {
        var (definition, _) = ResolveController(controllerId);
        SetControlCommandFault(faultId, controllerId, definition.OutputRange.Maximum);
    }

    public void ActivateActuatorCommandFreeze(string faultId, string actuatorId)
    {
        var (definition, state, siblingCount) = ResolveActuator(actuatorId);
        EnsureActuatorSpecificControlFaultIsUnambiguous(actuatorId, definition.ControllerId, siblingCount);
        SetControlCommandFault(faultId, definition.ControllerId, state.LastControllerOutput);
    }

    public void ActivateActuatorCommandFailLow(string faultId, string actuatorId)
    {
        var (definition, _, siblingCount) = ResolveActuator(actuatorId);
        EnsureActuatorSpecificControlFaultIsUnambiguous(actuatorId, definition.ControllerId, siblingCount);
        SetControlCommandFault(faultId, definition.ControllerId, definition.InputRange.Minimum);
    }

    public void ActivateActuatorCommandFailHigh(string faultId, string actuatorId)
    {
        var (definition, _, siblingCount) = ResolveActuator(actuatorId);
        EnsureActuatorSpecificControlFaultIsUnambiguous(actuatorId, definition.ControllerId, siblingCount);
        SetControlCommandFault(faultId, definition.ControllerId, definition.InputRange.Maximum);
    }

    public void ClearInstrumentationControlFault(string faultId)
    {
        ValidateFaultId(faultId);
        _instrumentationFaults.Remove(faultId);
        _controlCommandFaults.Remove(faultId);
    }

    public void ActivateExternalSupplyLoss(string faultId, string gridId)
    {
        ValidateFaultId(faultId);
        ArgumentException.ThrowIfNullOrWhiteSpace(gridId);
        var canonicalGridId = _persistentInputs.PlantInputs.GeneratorGridInputs.Definition.Grid.Id;
        if (!string.Equals(canonicalGridId, gridId, StringComparison.Ordinal))
        {
            throw new KeyNotFoundException($"Unknown canonical external electrical grid '{gridId}'.");
        }

        var conflict = _externalSupplyLosses.Values.FirstOrDefault(effect =>
            !string.Equals(effect.FaultId, faultId, StringComparison.Ordinal)
            && string.Equals(effect.GridId, gridId, StringComparison.Ordinal));
        if (conflict is not null)
        {
            throw new InvalidOperationException(
                $"External electrical grid '{gridId}' already has active supply-loss fault '{conflict.FaultId}'.");
        }

        _externalSupplyLosses[faultId] = new ExternalSupplyLossOverride(faultId, gridId);
    }

    public void ClearElectricalLossFault(string faultId)
    {
        ValidateFaultId(faultId);
        _externalSupplyLosses.Remove(faultId);
    }

    public void ActivateTurbineTrip(string faultId, string turbineRotorId)
    {
        ValidateFaultId(faultId);
        ArgumentException.ThrowIfNullOrWhiteSpace(turbineRotorId);
        _ = _persistentInputs.PlantInputs.Definition.GeneratorGridSystem.CondensateFeedwaterSystem.CondenserSystem
            .TurbineExpansionSystem.GetRotor(turbineRotorId);
        _manualTurbineTrip = true;
    }

    public void ActivateGeneratorTrip(string faultId, string generatorId)
    {
        ValidateFaultId(faultId);
        ArgumentException.ThrowIfNullOrWhiteSpace(generatorId);
        _ = _persistentInputs.PlantInputs.GeneratorGridInputs.Definition.GetGenerator(generatorId);
        _manualGeneratorTrip = true;
    }

    public void ActivateCondenserCoolingDegradation(string faultId, string coolingBoundaryId, double capacityFraction)
        => SetCondenserCoolingFault(faultId, coolingBoundaryId, capacityFraction);

    public void ActivateCondenserCoolingLoss(string faultId, string coolingBoundaryId)
        => SetCondenserCoolingFault(faultId, coolingBoundaryId, 0d);

    public void ClearSecondaryTransientFault(string faultId)
    {
        ValidateFaultId(faultId);
        _condenserCoolingFaults.Remove(faultId);
    }

    private void SetCondenserCoolingFault(string faultId, string coolingBoundaryId, double capacityFraction)
    {
        ValidateFaultId(faultId);
        ArgumentException.ThrowIfNullOrWhiteSpace(coolingBoundaryId);
        if (!double.IsFinite(capacityFraction) || capacityFraction < 0d || capacityFraction > 1d)
        {
            throw new ArgumentOutOfRangeException(nameof(capacityFraction));
        }

        var condenserInputs = _persistentInputs.PlantInputs.GeneratorGridInputs.CondensateFeedwaterInputs.CondenserInputs;
        _ = condenserInputs.Definition.GetCoolingBoundary(coolingBoundaryId);
        var conflict = _condenserCoolingFaults.Values.FirstOrDefault(effect =>
            !string.Equals(effect.FaultId, faultId, StringComparison.Ordinal)
            && string.Equals(effect.BoundaryId, coolingBoundaryId, StringComparison.Ordinal));
        if (conflict is not null)
        {
            throw new InvalidOperationException(
                $"Condenser cooling boundary '{coolingBoundaryId}' already has active transient '{conflict.FaultId}'.");
        }

        _condenserCoolingFaults[faultId] = new CondenserCoolingFaultOverride(
            faultId,
            coolingBoundaryId,
            capacityFraction);
    }

    private static void EnsureActuatorSpecificControlFaultIsUnambiguous(
        string actuatorId,
        string controllerId,
        int siblingCount)
    {
        if (siblingCount != 1)
        {
            throw new InvalidOperationException(
                $"Actuator '{actuatorId}' shares controller '{controllerId}' with another actuator; an actuator-specific command fault would be ambiguous.");
        }
    }

    private void SetSensorFault(
        string faultId,
        string channelId,
        SensorFaultMode mode,
        double biasEngineeringUnits = 0d)
    {
        ValidateFaultId(faultId);
        ArgumentException.ThrowIfNullOrWhiteSpace(channelId);
        _ = _persistentInputs.InstrumentationInputs.Definition.GetChannel(channelId);
        var baseline = _persistentInputs.InstrumentationInputs.GetSensorFault(channelId);
        if (baseline.Mode != SensorFaultMode.None)
        {
            throw new InvalidOperationException(
                $"Instrumentation channel '{channelId}' already has persistent sensor fault mode '{baseline.Mode}'.");
        }

        var conflict = _instrumentationFaults.Values.FirstOrDefault(input =>
            string.Equals(input.ChannelId, channelId, StringComparison.Ordinal));
        if (conflict is not null && !_instrumentationFaults.ContainsKey(faultId))
        {
            throw new InvalidOperationException(
                $"Instrumentation channel '{channelId}' already has an active scenario sensor fault.");
        }

        _instrumentationFaults[faultId] = new SensorFaultInput(channelId, mode, biasEngineeringUnits);
    }

    private void SetControlCommandFault(string faultId, string controllerId, double manualOutput)
    {
        ValidateFaultId(faultId);
        if (!double.IsFinite(manualOutput))
        {
            throw new ArgumentOutOfRangeException(nameof(manualOutput), manualOutput, "Control-fault manual output must be finite.");
        }

        var (definition, _) = ResolveController(controllerId);
        var conflict = _controlCommandFaults.Values.FirstOrDefault(effect =>
            !string.Equals(effect.FaultId, faultId, StringComparison.Ordinal)
            && string.Equals(effect.ControllerId, controllerId, StringComparison.Ordinal));
        if (conflict is not null)
        {
            throw new InvalidOperationException(
                $"Controller '{controllerId}' already has active control fault '{conflict.FaultId}'.");
        }

        _controlCommandFaults[faultId] = new ControlCommandFaultOverride(
            faultId,
            controllerId,
            definition.OutputRange.Clamp(manualOutput));
    }

    private (PidControllerDefinition Definition, ControllerChannelState State) ResolveController(string controllerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(controllerId);
        var reactorDefinitions = _persistentInputs.ReactorPrimaryInputs.Definition.ActuatorSystem.ControlSystem.Controllers
            .Where(item => string.Equals(item.Id, controllerId, StringComparison.Ordinal))
            .ToArray();
        var secondaryDefinitions = _persistentInputs.TurbineSecondaryInputs.Definition.ActuatorSystem.ControlSystem.Controllers
            .Where(item => string.Equals(item.Id, controllerId, StringComparison.Ordinal))
            .ToArray();
        if (reactorDefinitions.Length + secondaryDefinitions.Length == 0)
        {
            throw new KeyNotFoundException($"Unknown canonical controller '{controllerId}'.");
        }
        if (reactorDefinitions.Length + secondaryDefinitions.Length != 1)
        {
            throw new InvalidOperationException($"Controller id '{controllerId}' is not globally unique across canonical control systems.");
        }

        if (reactorDefinitions.Length == 1)
        {
            return (
                reactorDefinitions[0],
                _state.ReactorPrimaryControlState.ControlAndActuator.Controllers.GetController(controllerId));
        }

        return (
            secondaryDefinitions[0],
            _state.TurbineSecondaryControlState.ControlAndActuator.Controllers.GetController(controllerId));
    }

    private (ActuatorDefinition Definition, ActuatorCommandState State, int SiblingCount) ResolveActuator(string actuatorId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actuatorId);
        var reactorActuators = _persistentInputs.ReactorPrimaryInputs.Definition.ActuatorSystem.Actuators
            .Where(item => string.Equals(item.Id, actuatorId, StringComparison.Ordinal))
            .ToArray();
        var secondaryActuators = _persistentInputs.TurbineSecondaryInputs.Definition.ActuatorSystem.Actuators
            .Where(item => string.Equals(item.Id, actuatorId, StringComparison.Ordinal))
            .ToArray();
        if (reactorActuators.Length + secondaryActuators.Length == 0)
        {
            throw new KeyNotFoundException($"Unknown canonical actuator '{actuatorId}'.");
        }
        if (reactorActuators.Length + secondaryActuators.Length != 1)
        {
            throw new InvalidOperationException($"Actuator id '{actuatorId}' is not globally unique across canonical actuator systems.");
        }

        if (reactorActuators.Length == 1)
        {
            var definition = reactorActuators[0];
            var state = _state.ReactorPrimaryControlState.ControlAndActuator.Actuators.Actuators.Single(item =>
                string.Equals(item.ActuatorId, actuatorId, StringComparison.Ordinal));
            var siblingCount = _persistentInputs.ReactorPrimaryInputs.Definition.ActuatorSystem.Actuators.Count(item =>
                string.Equals(item.ControllerId, definition.ControllerId, StringComparison.Ordinal));
            return (definition, state, siblingCount);
        }

        var secondaryDefinition = secondaryActuators[0];
        var secondaryState = _state.TurbineSecondaryControlState.ControlAndActuator.Actuators.Actuators.Single(item =>
            string.Equals(item.ActuatorId, actuatorId, StringComparison.Ordinal));
        var secondarySiblingCount = _persistentInputs.TurbineSecondaryInputs.Definition.ActuatorSystem.Actuators.Count(item =>
            string.Equals(item.ControllerId, secondaryDefinition.ControllerId, StringComparison.Ordinal));
        return (secondaryDefinition, secondaryState, secondarySiblingCount);
    }

    private void SetPumpFault(string faultId, string pumpId, bool forceTrip, double capacityFraction)
    {
        ValidateFaultId(faultId);
        _ = _state.PlantState.PlantState.Definition.GetPump(pumpId);
        EnsureNoOtherPumpFault(faultId, pumpId);
        _hydraulicPumpFaults[faultId] = new PumpHydraulicFaultInput(faultId, pumpId, forceTrip, capacityFraction);
    }

    private void SetValveFault(string faultId, string valveId, HydraulicValveFaultMode mode, ValvePosition stuckPosition)
    {
        ValidateFaultId(faultId);
        _ = _state.PlantState.PlantState.Definition.GetValve(valveId);
        EnsureNoOtherValveFault(faultId, valveId);
        _hydraulicValveFaults[faultId] = new ValveHydraulicFaultInput(faultId, valveId, mode, stuckPosition);
    }

    private void EnsureNoOtherPumpFault(string faultId, string pumpId)
    {
        var conflict = _hydraulicPumpFaults.Values.FirstOrDefault(x => !string.Equals(x.FaultId, faultId, StringComparison.Ordinal)
            && string.Equals(x.PumpId, pumpId, StringComparison.Ordinal));
        if (conflict is not null)
        {
            throw new InvalidOperationException($"Pump '{pumpId}' already has active hydraulic fault '{conflict.FaultId}'.");
        }
    }

    private void EnsureNoOtherValveFault(string faultId, string valveId)
    {
        var conflict = _hydraulicValveFaults.Values.FirstOrDefault(x => !string.Equals(x.FaultId, faultId, StringComparison.Ordinal)
            && string.Equals(x.ValveId, valveId, StringComparison.Ordinal));
        if (conflict is not null)
        {
            throw new InvalidOperationException($"Valve '{valveId}' already has active hydraulic fault '{conflict.FaultId}'.");
        }
    }

    private static void ValidateFaultId(string faultId) => ArgumentException.ThrowIfNullOrWhiteSpace(faultId);

    private void EnsureNoOtherLossBoundary(string faultId, string fluidNodeId)
    {
        var fixedLeakConflict = _hydraulicLeaks.Values.FirstOrDefault(x =>
            !string.Equals(x.FaultId, faultId, StringComparison.Ordinal)
            && string.Equals(x.FluidNodeId, fluidNodeId, StringComparison.Ordinal));
        if (fixedLeakConflict is not null)
        {
            throw new InvalidOperationException(
                $"Fluid node '{fluidNodeId}' already has active prescribed leak '{fixedLeakConflict.FaultId}'.");
        }

        var breakConflict = _pressureDrivenBreaks.Values.FirstOrDefault(x =>
            !string.Equals(x.FaultId, faultId, StringComparison.Ordinal)
            && string.Equals(x.FluidNodeId, fluidNodeId, StringComparison.Ordinal));
        if (breakConflict is not null)
        {
            throw new InvalidOperationException(
                $"Fluid node '{fluidNodeId}' already has active pressure-driven break '{breakConflict.FaultId}'.");
        }
    }

    private HydraulicComponentFaultInputs BuildHydraulicFaultInputs()
        => new(
            _hydraulicPumpFaults.Values,
            _hydraulicValveFaults.Values,
            _hydraulicPathRestrictions.Values,
            _hydraulicLeaks.Values,
            _pressureDrivenBreaks.Values);

    private void ApplySupervisoryDecision()
    {
        if (CanSkipStableAssistedDecision() || CanSkipStableManualDecision())
        {
            return;
        }

        var result = _supervisoryCoordinator.Step(
            _supervisoryState,
            _requestedPlantControlAuthority,
            _requestedSupervisoryObjective,
            _state.MeasuredSignals,
            _lastSnapshot.Control.ProtectedControl.Protection,
            _state.ReactorPrimaryControlState,
            _state.TurbineSecondaryControlState,
            _persistentInputs.ReactorPrimaryInputs,
            _persistentInputs.TurbineSecondaryInputs);

        _supervisoryState = result.CandidateState;
        if (!ReferenceEquals(result.ReactorPrimaryInputs, _persistentInputs.ReactorPrimaryInputs)
            || !ReferenceEquals(result.TurbineSecondaryInputs, _persistentInputs.TurbineSecondaryInputs))
        {
            ReplacePersistentInputs(
                reactorInputs: result.ReactorPrimaryInputs,
                secondaryInputs: result.TurbineSecondaryInputs);
        }
    }

    private bool CanSkipStableAssistedDecision()
        => _requestedPlantControlAuthority == PlantControlAuthorityMode.Assisted
            && _supervisoryState.RequestedAuthority == PlantControlAuthorityMode.Assisted
            && _supervisoryState.EffectiveAuthority == PlantControlAuthorityMode.Assisted
            && _supervisoryState.Health == PlantControlAuthorityHealth.Normal
            && _supervisoryState.Objective == _requestedSupervisoryObjective;

    private bool CanSkipStableManualDecision()
        => _requestedPlantControlAuthority == PlantControlAuthorityMode.Manual
            && _supervisoryState.RequestedAuthority == PlantControlAuthorityMode.Manual
            && _supervisoryState.EffectiveAuthority == PlantControlAuthorityMode.Manual
            && _supervisoryState.Health == PlantControlAuthorityHealth.Normal
            && _requestedSupervisoryObjective is null
            && _persistentInputs.ReactorPrimaryInputs.Controllers.Controllers.All(static input => input.Mode == ControllerMode.Manual)
            && _persistentInputs.TurbineSecondaryInputs.Controllers.Controllers.All(static input => input.Mode == ControllerMode.Manual);

    private SupervisoryOperatingObjective ResolveObjective(SupervisoryObjectiveRequest objective)
    {
        if (objective.CaptureCurrentOperatingPoint)
        {
            var reactorLoop = _persistentInputs.ReactorPrimaryInputs.Definition.Loops
                .Single(static item => item.Kind == ReactorPrimaryControlLoopKind.ReactorPowerRodRegulation);
            var speedLoop = _persistentInputs.TurbineSecondaryInputs.Definition.Loops
                .Single(static item => item.Kind == TurbineSecondaryControlLoopKind.TurbineSpeedAdmission);
            var reactorController = _persistentInputs.ReactorPrimaryInputs.Controllers.Definition.GetController(reactorLoop.ControllerId);
            var speedController = _persistentInputs.TurbineSecondaryInputs.Controllers.Definition.GetController(speedLoop.ControllerId);
            var reactorPower = RequireValidMeasuredValue(reactorController.MeasurementChannelId);
            var turbineSpeed = RequireValidMeasuredValue(speedController.MeasurementChannelId);
            return SupervisoryOperatingObjective.HoldOperatingPoint(reactorPower, turbineSpeed);
        }

        return objective.Kind switch
        {
            SupervisoryOperatingObjectiveKind.HoldReactorPower => SupervisoryOperatingObjective.HoldReactorPower(
                objective.ReactorPowerSetpointWatts ?? throw new InvalidOperationException("Reactor-power objective requires an explicit setpoint.")),
            SupervisoryOperatingObjectiveKind.HoldTurbineSpeed => SupervisoryOperatingObjective.HoldTurbineSpeed(
                objective.TurbineSpeedSetpointRpm ?? throw new InvalidOperationException("Turbine-speed objective requires an explicit setpoint.")),
            SupervisoryOperatingObjectiveKind.HoldOperatingPoint => SupervisoryOperatingObjective.HoldOperatingPoint(
                objective.ReactorPowerSetpointWatts ?? throw new InvalidOperationException("Operating-point objective requires a reactor-power setpoint."),
                objective.TurbineSpeedSetpointRpm ?? throw new InvalidOperationException("Operating-point objective requires a turbine-speed setpoint.")),
            _ => throw new ArgumentOutOfRangeException(nameof(objective), objective.Kind, "Unsupported supervisory objective request."),
        };
    }

    private double RequireValidMeasuredValue(string channelId)
    {
        var signal = _state.MeasuredSignals.GetSignal(channelId);
        if (signal.Validity != SignalValidity.Valid
            || !signal.EngineeringValue.HasValue
            || !double.IsFinite(signal.EngineeringValue.Value))
        {
            throw new InvalidOperationException($"Cannot capture supervisory objective: measured signal '{channelId}' is unavailable or invalid.");
        }

        return signal.EngineeringValue.Value;
    }

    private static PlantControllerModePresentationSnapshot CreateControllerModeSnapshot(
        string area,
        ControlSystemDefinition definition,
        ControllerInput input)
    {
        var controller = definition.GetController(input.ControllerId);
        var channel = definition.Instrumentation.GetChannel(controller.MeasurementChannelId);
        return new PlantControllerModePresentationSnapshot(
            input.ControllerId,
            area,
            input.Mode,
            input.Setpoint,
            channel.EngineeringUnitSymbol);
    }

    private static string FormatObjective(SupervisoryOperatingObjective? objective)
        => objective?.Kind switch
        {
            SupervisoryOperatingObjectiveKind.HoldReactorPower => $"HOLD REACTOR POWER · {objective.ReactorPowerSetpointWatts!.Value:0.###} W",
            SupervisoryOperatingObjectiveKind.HoldTurbineSpeed => $"HOLD TURBINE SPEED · {objective.TurbineSpeedSetpointRpm!.Value:0.###} rpm",
            SupervisoryOperatingObjectiveKind.HoldOperatingPoint => $"HOLD OPERATING POINT · {objective.ReactorPowerSetpointWatts!.Value:0.###} W · {objective.TurbineSpeedSetpointRpm!.Value:0.###} rpm",
            _ => "NONE",
        };

    private static PlantControlAuthorityMode InferInitialAuthority(IntegratedAutomaticOperationInputs inputs)
        => inputs.ReactorPrimaryInputs.Controllers.Controllers
            .Concat(inputs.TurbineSecondaryInputs.Controllers.Controllers)
            .Any(static input => input.Mode == ControllerMode.Automatic)
            ? PlantControlAuthorityMode.Assisted
            : PlantControlAuthorityMode.Manual;

    private IntegratedAutomaticOperationInputs BuildStepInputs()
    {
        var plantInputs = BuildPlantInputs();
        var protectionInputs = new ProtectionSystemInputs(
            _persistentInputs.ProtectionInputs.Definition,
            _manualReactorScram,
            _manualTurbineTrip,
            _manualGeneratorTrip,
            _protectionResetRequested);
        var alarmInputs = new AlarmSystemInputs(
            _persistentInputs.AlarmInputs.Definition,
            _acknowledgeAlarmIds,
            _resetAlarmIds,
            _acknowledgeAll,
            _resetAll);

        return new IntegratedAutomaticOperationInputs(
            plantInputs,
            BuildReactorPrimaryInputs(),
            BuildTurbineSecondaryInputs(),
            protectionInputs,
            alarmInputs,
            BuildInstrumentationInputs(),
            BuildHydraulicFaultInputs());
    }

    private InstrumentationInputs BuildInstrumentationInputs()
    {
        var baseline = _persistentInputs.InstrumentationInputs;
        if (_instrumentationFaults.Count == 0)
        {
            return baseline;
        }

        var activeByChannel = _instrumentationFaults.Values.ToDictionary(
            static input => input.ChannelId,
            StringComparer.Ordinal);
        return new InstrumentationInputs(
            baseline.Definition,
            baseline.SensorFaults.Select(input => activeByChannel.GetValueOrDefault(input.ChannelId) ?? input));
    }

    private ReactorPrimaryControlInputs BuildReactorPrimaryInputs()
    {
        var baseline = _persistentInputs.ReactorPrimaryInputs;
        return new ReactorPrimaryControlInputs(
            baseline.Definition,
            BuildControllerInputs(baseline.Controllers),
            baseline.NonRodReactivity);
    }

    private TurbineSecondaryControlInputs BuildTurbineSecondaryInputs()
    {
        var baseline = _persistentInputs.TurbineSecondaryInputs;
        return new TurbineSecondaryControlInputs(
            baseline.Definition,
            BuildControllerInputs(baseline.Controllers));
    }

    private ControllerInputs BuildControllerInputs(ControllerInputs baseline)
    {
        if (_controlCommandFaults.Count == 0)
        {
            return baseline;
        }

        var activeByController = _controlCommandFaults.Values.ToDictionary(
            static effect => effect.ControllerId,
            StringComparer.Ordinal);
        return new ControllerInputs(
            baseline.Definition,
            baseline.Controllers.Select(input =>
            {
                if (!activeByController.TryGetValue(input.ControllerId, out var effect))
                {
                    return input;
                }

                return new ControllerInput(
                    input.ControllerId,
                    ControllerMode.Manual,
                    input.Setpoint,
                    effect.ManualOutput);
            }));
    }

    private IntegratedSecondaryCycleInputs BuildPlantInputs()
    {
        var plantInputs = _persistentInputs.PlantInputs;
        var gridInputs = plantInputs.GeneratorGridInputs;

        if (_condenserCoolingFaults.Count > 0)
        {
            var feedwaterInputs = gridInputs.CondensateFeedwaterInputs;
            var condenserInputs = feedwaterInputs.CondenserInputs;
            var activeByBoundary = _condenserCoolingFaults.Values.ToDictionary(
                static effect => effect.BoundaryId,
                StringComparer.Ordinal);
            var coolingInputs = condenserInputs.CoolingBoundaryInputs.Select(input =>
            {
                if (!activeByBoundary.TryGetValue(input.BoundaryId, out var effect))
                {
                    return input;
                }

                return new CondenserCoolingBoundaryInput(
                    input.BoundaryId,
                    Power.FromWatts(input.AvailableHeatRejectionPower.Watts * effect.CapacityFraction),
                    input.CoolantTemperature);
            }).ToArray();
            var faultedCondenserInputs = new CondenserSystemInputs(
                condenserInputs.Definition,
                condenserInputs.TurbineExpansionInputs,
                coolingInputs);
            var faultedFeedwaterInputs = new CondensateFeedwaterSystemInputs(
                feedwaterInputs.Definition,
                faultedCondenserInputs,
                feedwaterInputs.TrainInputs);
            gridInputs = new GeneratorGridInputs(
                gridInputs.Definition,
                faultedFeedwaterInputs,
                gridInputs.GeneratorInputs);
        }

        if (_breakerCloseById.Count > 0)
        {
            var generatorInputs = gridInputs.GeneratorInputs.Select(input =>
            {
                var definition = gridInputs.Definition.GetGenerator(input.GeneratorId);
                if (!_breakerCloseById.TryGetValue(definition.BreakerId, out var close))
                {
                    return input;
                }

                return new SynchronousGeneratorInput(
                    input.GeneratorId,
                    input.TerminalLineVoltage,
                    input.RequestedElectricalPower,
                    closeBreakerCommand: close,
                    openBreakerCommand: !close);
            }).ToArray();
            gridInputs = new GeneratorGridInputs(
                gridInputs.Definition,
                gridInputs.CondensateFeedwaterInputs,
                generatorInputs);
        }

        if (_externalSupplyLosses.Count > 0)
        {
            var canonicalGridId = gridInputs.Definition.Grid.Id;
            if (_externalSupplyLosses.Values.Any(effect => string.Equals(effect.GridId, canonicalGridId, StringComparison.Ordinal)))
            {
                var generatorInputs = gridInputs.GeneratorInputs
                    .Select(input => new SynchronousGeneratorInput(
                        input.GeneratorId,
                        input.TerminalLineVoltage,
                        input.RequestedElectricalPower,
                        closeBreakerCommand: false,
                        openBreakerCommand: true))
                    .ToArray();
                gridInputs = new GeneratorGridInputs(
                    gridInputs.Definition,
                    gridInputs.CondensateFeedwaterInputs,
                    generatorInputs);
            }
        }

        return ReferenceEquals(gridInputs, plantInputs.GeneratorGridInputs)
            ? plantInputs
            : new IntegratedSecondaryCycleInputs(plantInputs.Definition, gridInputs);
    }

    private void SetRodManualCommand(ControlRoomCommand command, ControlRodMotion motion)
    {
        var targetKind = command.TargetKind switch
        {
            ControlRoomCommandTargetKind.ControlRod => ControlRodCommandTargetKind.Rod,
            ControlRoomCommandTargetKind.ControlRodGroup => ControlRodCommandTargetKind.Group,
            _ => throw new ArgumentException("Control-rod commands require a ControlRod or ControlRodGroup target.", nameof(command)),
        };
        var targetId = RequireTarget(command, command.TargetKind!.Value);
        var definition = _persistentInputs.ReactorPrimaryInputs.Definition;
        var actuator = definition.ActuatorSystem.Actuators.SingleOrDefault(item =>
            item.TargetKind == ActuatorTargetKind.ControlRod
            && item.RodTargetKind == targetKind
            && string.Equals(item.TargetId, targetId, StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"Rod target '{targetId}' is not bound to a canonical M5.3 operator-command actuator.");

        var normalized = motion switch
        {
            ControlRodMotion.Hold => 0.5d,
            ControlRodMotion.Withdraw => actuator.PositiveRodOutputWithdraws ? 1d : 0d,
            ControlRodMotion.Insert => actuator.PositiveRodOutputWithdraws ? 0d : 1d,
            _ => throw new ArgumentOutOfRangeException(nameof(motion), motion, "Unsupported rod motion."),
        };
        var manualOutput = actuator.InputRange.Minimum + (normalized * actuator.InputRange.Span);
        ReplaceReactorController(actuator.ControllerId, input =>
            new ControllerInput(input.ControllerId, ControllerMode.Manual, input.Setpoint, manualOutput));
    }

    private void SetPumpManualCommand(ControlRoomCommand command, bool run)
    {
        var pumpId = RequireTarget(command, ControlRoomCommandTargetKind.Pump);
        var definition = _persistentInputs.ReactorPrimaryInputs.Definition;
        var actuator = definition.ActuatorSystem.Actuators.SingleOrDefault(item =>
            item.TargetKind == ActuatorTargetKind.Pump
            && string.Equals(item.TargetId, pumpId, StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"Pump '{pumpId}' is not bound to a canonical M5.3 operator-command actuator.");
        var manualOutput = run ? actuator.InputRange.Maximum : actuator.InputRange.Minimum;
        ReplaceReactorController(actuator.ControllerId, input =>
            new ControllerInput(input.ControllerId, ControllerMode.Manual, input.Setpoint, manualOutput));
    }

    private void AdjustSecondarySetpoint(
        ControlRoomCommand command,
        TurbineSecondaryControlLoopKind loopKind,
        double increment)
    {
        var targetKind = loopKind == TurbineSecondaryControlLoopKind.TurbineSpeedAdmission
            ? ControlRoomCommandTargetKind.TurbineRotor
            : ControlRoomCommandTargetKind.Generator;
        var targetId = RequireTarget(command, targetKind);
        var definition = _persistentInputs.TurbineSecondaryInputs.Definition;
        var expectedSource = loopKind == TurbineSecondaryControlLoopKind.TurbineSpeedAdmission
            ? $"turbine-rotor/{targetId}/speed"
            : $"generator/{targetId}/electrical-output";
        var loop = definition.Loops.SingleOrDefault(item =>
            item.Kind == loopKind
            && string.Equals(
                definition.ActuatorSystem.ControlSystem.Instrumentation.GetChannel(
                    definition.ActuatorSystem.ControlSystem.GetController(item.ControllerId).MeasurementChannelId).SourceId,
                expectedSource,
                StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"No canonical M5.4 {loopKind} loop is bound to '{targetId}'.");

        ReplaceSecondaryController(loop.ControllerId, input =>
        {
            var setpoint = Math.Max(0d, input.Setpoint + increment);
            return new ControllerInput(input.ControllerId, input.Mode, setpoint, input.ManualOutput);
        });
    }

    private void AdjustGeneratorLoadRequest(ControlRoomCommand command, double incrementWatts)
    {
        var generatorId = RequireTarget(command, ControlRoomCommandTargetKind.Generator);
        var existingPlant = _persistentInputs.PlantInputs;
        var existingGrid = existingPlant.GeneratorGridInputs;
        var generator = existingGrid.Definition.GetGenerator(generatorId);
        var found = false;
        var generatorInputs = existingGrid.GeneratorInputs.Select(input =>
        {
            if (!string.Equals(input.GeneratorId, generatorId, StringComparison.Ordinal))
            {
                return input;
            }

            found = true;
            var requestedWatts = Math.Clamp(
                input.RequestedElectricalPower.Watts + incrementWatts,
                0d,
                generator.MaximumElectricalPower.Watts);
            return new SynchronousGeneratorInput(
                input.GeneratorId,
                input.TerminalLineVoltage,
                Power.FromWatts(requestedWatts),
                input.CloseBreakerCommand,
                input.OpenBreakerCommand);
        }).ToArray();

        if (!found)
        {
            throw new InvalidOperationException($"No canonical M4.5 generator input is bound to '{generatorId}'.");
        }

        var generatorGridInputs = new GeneratorGridInputs(
            existingGrid.Definition,
            existingGrid.CondensateFeedwaterInputs,
            generatorInputs);
        ReplacePersistentInputs(plantInputs: new IntegratedSecondaryCycleInputs(existingPlant.Definition, generatorGridInputs));
    }

    private void SetBreakerCommand(ControlRoomCommand command, bool close)
    {
        var breakerId = RequireTarget(command, ControlRoomCommandTargetKind.Breaker);
        var exists = _persistentInputs.PlantInputs.Definition.GeneratorGridSystem.Generators.Any(
            item => string.Equals(item.BreakerId, breakerId, StringComparison.Ordinal));
        if (!exists)
        {
            throw new InvalidOperationException($"Unknown canonical generator breaker '{breakerId}'.");
        }

        _breakerCloseById[breakerId] = close;
    }

    private void ReplaceReactorController(string controllerId, Func<ControllerInput, ControllerInput> replace)
    {
        var existing = _persistentInputs.ReactorPrimaryInputs;
        var controllers = existing.Controllers.Controllers.Select(input =>
            string.Equals(input.ControllerId, controllerId, StringComparison.Ordinal) ? replace(input) : input).ToArray();
        var reactorInputs = new ReactorPrimaryControlInputs(
            existing.Definition,
            new ControllerInputs(existing.Controllers.Definition, controllers),
            existing.NonRodReactivity);
        ReplacePersistentInputs(reactorInputs: reactorInputs);
    }

    private void ReplaceSecondaryController(string controllerId, Func<ControllerInput, ControllerInput> replace)
    {
        var existing = _persistentInputs.TurbineSecondaryInputs;
        var controllers = existing.Controllers.Controllers.Select(input =>
            string.Equals(input.ControllerId, controllerId, StringComparison.Ordinal) ? replace(input) : input).ToArray();
        var secondaryInputs = new TurbineSecondaryControlInputs(
            existing.Definition,
            new ControllerInputs(existing.Controllers.Definition, controllers));
        ReplacePersistentInputs(secondaryInputs: secondaryInputs);
    }

    private void ReplacePersistentInputs(
        IntegratedSecondaryCycleInputs? plantInputs = null,
        ReactorPrimaryControlInputs? reactorInputs = null,
        TurbineSecondaryControlInputs? secondaryInputs = null)
    {
        _persistentInputs = new IntegratedAutomaticOperationInputs(
            plantInputs ?? _persistentInputs.PlantInputs,
            reactorInputs ?? _persistentInputs.ReactorPrimaryInputs,
            secondaryInputs ?? _persistentInputs.TurbineSecondaryInputs,
            _persistentInputs.ProtectionInputs,
            _persistentInputs.AlarmInputs,
            _persistentInputs.InstrumentationInputs);
    }

    private void ClearTransientCommands()
    {
        _manualReactorScram = false;
        _manualTurbineTrip = false;
        _manualGeneratorTrip = false;
        _protectionResetRequested = false;
        _acknowledgeAll = false;
        _resetAll = false;
        _acknowledgeAlarmIds.Clear();
        _resetAlarmIds.Clear();
        _breakerCloseById.Clear();
    }

    private static string RequireTarget(ControlRoomCommand command, ControlRoomCommandTargetKind targetKind)
    {
        var targetId = command.TargetId;
        if (command.TargetKind != targetKind || string.IsNullOrWhiteSpace(targetId))
        {
            throw new ArgumentException($"Command '{command.Kind}' requires a non-empty {targetKind} target.", nameof(command));
        }
        return targetId.Trim();
    }

    private static void RequireUntargeted(ControlRoomCommand command)
    {
        if (command.TargetKind is not null || command.TargetId is not null)
        {
            throw new ArgumentException($"Command '{command.Kind}' must not specify a target.", nameof(command));
        }
    }

    private static void ValidatePersistentBaseline(IntegratedAutomaticOperationInputs inputs)
    {
        var protection = inputs.ProtectionInputs;
        if (protection.ManualReactorScram || protection.ManualTurbineTrip || protection.ManualGeneratorTrip || protection.ResetRequested)
        {
            throw new ArgumentException("M6.7 persistent runtime inputs cannot contain one-shot protection commands.", nameof(inputs));
        }

        var alarms = inputs.AlarmInputs;
        if (alarms.AcknowledgeAll || alarms.ResetAll || alarms.AcknowledgeAlarmIds.Count != 0 || alarms.ResetAlarmIds.Count != 0)
        {
            throw new ArgumentException("M6.7 persistent runtime inputs cannot contain one-shot alarm commands.", nameof(inputs));
        }

        if (inputs.PlantInputs.GeneratorGridInputs.GeneratorInputs.Any(static item => item.CloseBreakerCommand || item.OpenBreakerCommand))
        {
            throw new ArgumentException("M6.7 persistent runtime inputs cannot contain one-shot generator-breaker commands.", nameof(inputs));
        }
    }
}
