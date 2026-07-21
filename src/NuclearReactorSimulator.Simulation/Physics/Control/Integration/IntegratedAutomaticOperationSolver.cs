using NuclearReactorSimulator.Domain.Physics.Control.Alarms;
using NuclearReactorSimulator.Domain.Physics.Control.Protection;
using NuclearReactorSimulator.Domain.Physics.Control.ReactorPrimary;
using NuclearReactorSimulator.Domain.Physics.Control.TurbineSecondary;
using NuclearReactorSimulator.Simulation.Physics.Control.Alarms;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using NuclearReactorSimulator.Simulation.Physics.Instrumentation;

namespace NuclearReactorSimulator.Simulation.Physics.Control.Integration;

/// <summary>
/// M5.7 integrated automatic-operation boundary. One committed measured frame drives control/protection/alarm decisions,
/// one protected physical step advances the plant, and instrumentation observes only that candidate true-state snapshot to
/// produce the measured frame for the next logical step. No physical state is integrated twice.
/// </summary>
public sealed class IntegratedAutomaticOperationSolver
{
    private readonly ReactorPrimaryControlSystemDefinition _reactorDefinition;
    private readonly TurbineSecondaryControlSystemDefinition _secondaryDefinition;
    private readonly ProtectionSystemDefinition _protectionDefinition;
    private readonly AlarmSystemDefinition _alarmDefinition;
    private readonly AlarmedProtectedAutomaticFullPlantSolver _controlledSolver;
    private readonly InstrumentationSolver _instrumentationSolver;

    public IntegratedAutomaticOperationSolver(
        ReactorPrimaryControlSystemDefinition reactorDefinition,
        TurbineSecondaryControlSystemDefinition secondaryDefinition,
        ProtectionSystemDefinition protectionDefinition,
        AlarmSystemDefinition alarmDefinition,
        IFluidThermodynamicModel thermodynamicModel,
        InstrumentSignalSourceCatalog signalSources)
    {
        _reactorDefinition = reactorDefinition ?? throw new ArgumentNullException(nameof(reactorDefinition));
        _secondaryDefinition = secondaryDefinition ?? throw new ArgumentNullException(nameof(secondaryDefinition));
        _protectionDefinition = protectionDefinition ?? throw new ArgumentNullException(nameof(protectionDefinition));
        _alarmDefinition = alarmDefinition ?? throw new ArgumentNullException(nameof(alarmDefinition));
        ArgumentNullException.ThrowIfNull(thermodynamicModel);
        ArgumentNullException.ThrowIfNull(signalSources);

        if (!ReferenceEquals(reactorDefinition.PlantDefinition, secondaryDefinition.PlantDefinition)
            || !ReferenceEquals(secondaryDefinition.PlantDefinition, protectionDefinition.PlantDefinition))
        {
            throw new ArgumentException("M5.7 requires one canonical full-plant definition across M5.3, M5.4 and M5.5.");
        }

        var instrumentation = reactorDefinition.ActuatorSystem.ControlSystem.Instrumentation;
        if (!ReferenceEquals(secondaryDefinition.ActuatorSystem.ControlSystem.Instrumentation, instrumentation)
            || !ReferenceEquals(protectionDefinition.Instrumentation, instrumentation)
            || !ReferenceEquals(alarmDefinition.Instrumentation, instrumentation)
            || !ReferenceEquals(alarmDefinition.Protection, protectionDefinition))
        {
            throw new ArgumentException("M5.7 requires one canonical instrumentation/protection/alarm ownership chain.");
        }

        if (signalSources.FullPlantDefinition is not null
            && !ReferenceEquals(signalSources.FullPlantDefinition, reactorDefinition.PlantDefinition))
        {
            throw new ArgumentException("M5.7 signal-source catalog must observe the canonical full-plant definition.", nameof(signalSources));
        }

        _controlledSolver = new AlarmedProtectedAutomaticFullPlantSolver(
            reactorDefinition,
            secondaryDefinition,
            protectionDefinition,
            alarmDefinition,
            thermodynamicModel);
        _instrumentationSolver = new InstrumentationSolver(instrumentation, signalSources);
    }

    public IntegratedAutomaticOperationStepResult Step(
        IntegratedAutomaticOperationState committedState,
        IntegratedAutomaticOperationInputs inputs,
        TimeSpan deltaTime)
    {
        ArgumentNullException.ThrowIfNull(committedState);
        ArgumentNullException.ThrowIfNull(inputs);
        if (deltaTime <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(deltaTime), deltaTime, "Automatic-operation timestep must be positive.");
        }

        ValidateState(committedState);
        ValidateInputs(inputs);

        var controlledStep = _controlledSolver.Step(
            committedState.MeasuredSignals,
            committedState.PlantState,
            committedState.ReactorPrimaryControlState,
            committedState.TurbineSecondaryControlState,
            committedState.ProtectionState,
            committedState.AlarmState,
            inputs.PlantInputs,
            inputs.ReactorPrimaryInputs,
            inputs.TurbineSecondaryInputs,
            inputs.ProtectionInputs,
            inputs.AlarmInputs,
            deltaTime);

        var physicalSnapshot = controlledStep.ProtectedStep.FullPlantStep.Snapshot;
        var instrumentationStep = _instrumentationSolver.Step(
            physicalSnapshot,
            committedState.InstrumentationState,
            inputs.InstrumentationInputs,
            deltaTime);

        var candidateState = new IntegratedAutomaticOperationState(
            _reactorDefinition.PlantDefinition,
            _reactorDefinition.ActuatorSystem.ControlSystem.Instrumentation,
            controlledStep.ProtectedStep.FullPlantStep.CandidateState,
            instrumentationStep.CandidateState,
            instrumentationStep.Snapshot.MeasuredSignals,
            controlledStep.ProtectedStep.ReactorPrimaryControlStep.CandidateState,
            controlledStep.ProtectedStep.TurbineSecondaryControlStep.CandidateState,
            controlledStep.ProtectedStep.ProtectionStep.CandidateState,
            controlledStep.AlarmStep.CandidateState);
        var snapshot = new IntegratedAutomaticOperationSnapshot(
            committedState.MeasuredSignals,
            controlledStep.Snapshot,
            instrumentationStep.Snapshot);

        return new IntegratedAutomaticOperationStepResult(controlledStep, instrumentationStep, candidateState, snapshot);
    }

    private void ValidateState(IntegratedAutomaticOperationState state)
    {
        if (!ReferenceEquals(state.PlantDefinition, _reactorDefinition.PlantDefinition)
            || !ReferenceEquals(state.ReactorPrimaryControlState.Definition, _reactorDefinition)
            || !ReferenceEquals(state.TurbineSecondaryControlState.Definition, _secondaryDefinition)
            || !ReferenceEquals(state.ProtectionState.Definition, _protectionDefinition)
            || !ReferenceEquals(state.AlarmState.Definition, _alarmDefinition))
        {
            throw new ArgumentException("Committed M5.7 state does not use this solver's canonical definitions.", nameof(state));
        }
    }

    private void ValidateInputs(IntegratedAutomaticOperationInputs inputs)
    {
        if (!ReferenceEquals(inputs.PlantInputs.Definition, _reactorDefinition.PlantDefinition)
            || !ReferenceEquals(inputs.ReactorPrimaryInputs.Definition, _reactorDefinition)
            || !ReferenceEquals(inputs.TurbineSecondaryInputs.Definition, _secondaryDefinition)
            || !ReferenceEquals(inputs.ProtectionInputs.Definition, _protectionDefinition)
            || !ReferenceEquals(inputs.AlarmInputs.Definition, _alarmDefinition))
        {
            throw new ArgumentException("M5.7 inputs do not use this solver's canonical definitions.", nameof(inputs));
        }
    }
}
