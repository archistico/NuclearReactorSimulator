using NuclearReactorSimulator.Domain.Physics.Control;
using NuclearReactorSimulator.Domain.Physics.Control.ReactorPrimary;
using NuclearReactorSimulator.Domain.Physics.Control.TurbineSecondary;
using NuclearReactorSimulator.Domain.Physics.Reactor.ControlRods;
using NuclearReactorSimulator.Simulation.Physics.Control;
using NuclearReactorSimulator.Simulation.Physics.Control.Alarms;
using NuclearReactorSimulator.Simulation.Physics.Control.Integration;
using NuclearReactorSimulator.Simulation.Physics.Control.Protection;
using NuclearReactorSimulator.Simulation.Physics.Control.ReactorPrimary;
using NuclearReactorSimulator.Simulation.Physics.Control.TurbineSecondary;
using NuclearReactorSimulator.Simulation.Physics.Electrical;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Integration;

namespace NuclearReactorSimulator.Application.ControlRoom;

/// <summary>
/// Concrete M6.7 adapter from Application operator intents to the validated M5.7 automatic-operation runtime. Persistent
/// controller/setpoint commands modify only immutable per-step input bundles; trip, breaker and annunciator commands are
/// one-step pulses cleared after the next deterministic step.
/// </summary>
public sealed class IntegratedAutomaticOperationRuntimeEngine : IControlRoomRuntimeEngine
{
    private readonly IntegratedAutomaticOperationSolver _solver;
    private readonly TimeSpan _deltaTime;
    private readonly ControlRoomRuntimeCommandPolicy _policy;
    private IntegratedAutomaticOperationState _state;
    private IntegratedAutomaticOperationInputs _persistentInputs;
    private IntegratedAutomaticOperationSnapshot _lastSnapshot;
    private long _logicalStep;

    private bool _manualReactorScram;
    private bool _manualTurbineTrip;
    private bool _manualGeneratorTrip;
    private bool _protectionResetRequested;
    private bool _acknowledgeAll;
    private bool _resetAll;
    private readonly HashSet<string> _acknowledgeAlarmIds = new(StringComparer.Ordinal);
    private readonly HashSet<string> _resetAlarmIds = new(StringComparer.Ordinal);
    private readonly Dictionary<string, bool> _breakerCloseById = new(StringComparer.Ordinal);

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

        var stepInputs = BuildStepInputs();
        var result = _solver.Step(_state, stepInputs, _deltaTime);
        _state = result.CandidateState;
        _lastSnapshot = result.Snapshot;
        _logicalStep++;
        ClearTransientCommands();
        return ControlRoomSnapshotProjector.Project(_logicalStep, runState, _lastSnapshot);
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
                AdjustSecondarySetpoint(command, TurbineSecondaryControlLoopKind.TurbineLoadAdmission, _policy.GeneratorLoadSetpointIncrementWatts);
                break;
            case ControlRoomCommandKind.GeneratorLoadLower:
                AdjustSecondarySetpoint(command, TurbineSecondaryControlLoopKind.TurbineLoadAdmission, -_policy.GeneratorLoadSetpointIncrementWatts);
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
            _persistentInputs.ReactorPrimaryInputs,
            _persistentInputs.TurbineSecondaryInputs,
            protectionInputs,
            alarmInputs,
            _persistentInputs.InstrumentationInputs);
    }

    private IntegratedSecondaryCycleInputs BuildPlantInputs()
    {
        var baseline = _persistentInputs.PlantInputs.GeneratorGridInputs;
        if (_breakerCloseById.Count == 0)
        {
            return _persistentInputs.PlantInputs;
        }

        var generatorInputs = baseline.GeneratorInputs.Select(input =>
        {
            var definition = baseline.Definition.GetGenerator(input.GeneratorId);
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
        var generatorGridInputs = new GeneratorGridInputs(
            baseline.Definition,
            baseline.CondensateFeedwaterInputs,
            generatorInputs);
        return new IntegratedSecondaryCycleInputs(_persistentInputs.PlantInputs.Definition, generatorGridInputs);
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
        ReactorPrimaryControlInputs? reactorInputs = null,
        TurbineSecondaryControlInputs? secondaryInputs = null)
    {
        _persistentInputs = new IntegratedAutomaticOperationInputs(
            _persistentInputs.PlantInputs,
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
