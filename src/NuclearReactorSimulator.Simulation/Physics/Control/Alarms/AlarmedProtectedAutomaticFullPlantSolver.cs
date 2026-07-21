using NuclearReactorSimulator.Domain.Physics.Control.Alarms;
using NuclearReactorSimulator.Domain.Physics.Control.Protection;
using NuclearReactorSimulator.Domain.Physics.Control.ReactorPrimary;
using NuclearReactorSimulator.Domain.Physics.Control.TurbineSecondary;
using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Simulation.Physics.Control.Protection;
using NuclearReactorSimulator.Simulation.Physics.Control.Integration;
using NuclearReactorSimulator.Simulation.Physics.Control.ReactorPrimary;
using NuclearReactorSimulator.Simulation.Physics.Control.TurbineSecondary;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using NuclearReactorSimulator.Simulation.Physics.Instrumentation;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Integration;

namespace NuclearReactorSimulator.Simulation.Physics.Control.Alarms;

/// <summary>
/// M5.6 composition: runs the validated M5.5 protected physical path once, then advances alarm/annunciator memory from the same measured frame and resulting protection snapshot.
/// </summary>
public sealed class AlarmedProtectedAutomaticFullPlantSolver
{
    private readonly ProtectedAutomaticFullPlantSolver _protectedSolver;
    private readonly AlarmSystemSolver _alarmSolver;

    public AlarmedProtectedAutomaticFullPlantSolver(
        ReactorPrimaryControlSystemDefinition reactorDefinition,
        TurbineSecondaryControlSystemDefinition secondaryDefinition,
        ProtectionSystemDefinition protectionDefinition,
        AlarmSystemDefinition alarmDefinition,
        IFluidThermodynamicModel thermodynamicModel)
    {
        ArgumentNullException.ThrowIfNull(alarmDefinition);
        if (!ReferenceEquals(alarmDefinition.Protection, protectionDefinition))
        {
            throw new ArgumentException("M5.6 alarm composition must observe the exact canonical M5.5 protection definition.", nameof(alarmDefinition));
        }
        _protectedSolver = new ProtectedAutomaticFullPlantSolver(reactorDefinition, secondaryDefinition, protectionDefinition, thermodynamicModel);
        _alarmSolver = new AlarmSystemSolver(alarmDefinition);
    }

    public AlarmedProtectedAutomaticFullPlantStepResult Step(
        MeasuredSignalFrame measuredSignals,
        FullPlantState committedPlantState,
        ReactorPrimaryControlState committedReactorControlState,
        TurbineSecondaryControlState committedSecondaryControlState,
        ProtectionSystemState committedProtectionState,
        AlarmSystemState committedAlarmState,
        IntegratedSecondaryCycleInputs basePlantInputs,
        ReactorPrimaryControlInputs reactorInputs,
        TurbineSecondaryControlInputs secondaryInputs,
        ProtectionSystemInputs protectionInputs,
        AlarmSystemInputs alarmInputs,
        TimeSpan deltaTime)
        => Step(measuredSignals, committedPlantState, committedReactorControlState, committedSecondaryControlState,
            committedProtectionState, committedAlarmState, basePlantInputs, reactorInputs, secondaryInputs, protectionInputs,
            alarmInputs, deltaTime, HydraulicComponentFaultInputs.Empty);

    public AlarmedProtectedAutomaticFullPlantStepResult Step(
        MeasuredSignalFrame measuredSignals,
        FullPlantState committedPlantState,
        ReactorPrimaryControlState committedReactorControlState,
        TurbineSecondaryControlState committedSecondaryControlState,
        ProtectionSystemState committedProtectionState,
        AlarmSystemState committedAlarmState,
        IntegratedSecondaryCycleInputs basePlantInputs,
        ReactorPrimaryControlInputs reactorInputs,
        TurbineSecondaryControlInputs secondaryInputs,
        ProtectionSystemInputs protectionInputs,
        AlarmSystemInputs alarmInputs,
        TimeSpan deltaTime,
        HydraulicComponentFaultInputs hydraulicFaultInputs)
    {
        var protectedStep = _protectedSolver.Step(
            measuredSignals,
            committedPlantState,
            committedReactorControlState,
            committedSecondaryControlState,
            committedProtectionState,
            basePlantInputs,
            reactorInputs,
            secondaryInputs,
            protectionInputs,
            deltaTime,
            hydraulicFaultInputs);
        var alarmStep = _alarmSolver.Step(measuredSignals, protectedStep.Snapshot.Protection, committedAlarmState, alarmInputs);
        return new AlarmedProtectedAutomaticFullPlantStepResult(
            protectedStep,
            alarmStep,
            new AlarmedProtectedAutomaticControlSnapshot(protectedStep.Snapshot, alarmStep.Snapshot));
    }
}
