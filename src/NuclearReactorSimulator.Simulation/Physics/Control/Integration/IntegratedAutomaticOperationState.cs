using NuclearReactorSimulator.Domain.Physics.Control.Alarms;
using NuclearReactorSimulator.Domain.Physics.Control.Protection;
using NuclearReactorSimulator.Domain.Physics.Control.ReactorPrimary;
using NuclearReactorSimulator.Domain.Physics.Control.TurbineSecondary;
using NuclearReactorSimulator.Domain.Physics.Instrumentation;
using NuclearReactorSimulator.Domain.Physics.TurbineIsland.Integration;
using NuclearReactorSimulator.Simulation.Physics.Control.Alarms;
using NuclearReactorSimulator.Simulation.Physics.Control.Protection;
using NuclearReactorSimulator.Simulation.Physics.Control.ReactorPrimary;
using NuclearReactorSimulator.Simulation.Physics.Control.TurbineSecondary;
using NuclearReactorSimulator.Simulation.Physics.Instrumentation;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Integration;

namespace NuclearReactorSimulator.Simulation.Physics.Control.Integration;

/// <summary>
/// M5.7 canonical automatic-operation state envelope. It contains only the already-owned physical, instrumentation,
/// controller, protection and annunciator states plus the committed measured frame used by the next logical step.
/// </summary>
public sealed class IntegratedAutomaticOperationState
{
    public IntegratedAutomaticOperationState(
        IntegratedSecondaryCycleDefinition plantDefinition,
        InstrumentationSystemDefinition instrumentationDefinition,
        FullPlantState plantState,
        InstrumentationState instrumentationState,
        MeasuredSignalFrame measuredSignals,
        ReactorPrimaryControlState reactorPrimaryControlState,
        TurbineSecondaryControlState turbineSecondaryControlState,
        ProtectionSystemState protectionState,
        AlarmSystemState alarmState)
    {
        PlantDefinition = plantDefinition ?? throw new ArgumentNullException(nameof(plantDefinition));
        InstrumentationDefinition = instrumentationDefinition ?? throw new ArgumentNullException(nameof(instrumentationDefinition));
        PlantState = plantState ?? throw new ArgumentNullException(nameof(plantState));
        InstrumentationState = instrumentationState ?? throw new ArgumentNullException(nameof(instrumentationState));
        MeasuredSignals = measuredSignals ?? throw new ArgumentNullException(nameof(measuredSignals));
        ReactorPrimaryControlState = reactorPrimaryControlState ?? throw new ArgumentNullException(nameof(reactorPrimaryControlState));
        TurbineSecondaryControlState = turbineSecondaryControlState ?? throw new ArgumentNullException(nameof(turbineSecondaryControlState));
        ProtectionState = protectionState ?? throw new ArgumentNullException(nameof(protectionState));
        AlarmState = alarmState ?? throw new ArgumentNullException(nameof(alarmState));

        if (!ReferenceEquals(plantState.Definition, plantDefinition)
            || !ReferenceEquals(reactorPrimaryControlState.Definition.PlantDefinition, plantDefinition)
            || !ReferenceEquals(turbineSecondaryControlState.Definition.PlantDefinition, plantDefinition)
            || !ReferenceEquals(protectionState.Definition.PlantDefinition, plantDefinition))
        {
            throw new ArgumentException("All M5.7 physical/control/protection states must use the same canonical full-plant definition.");
        }

        var reactorInstrumentation = reactorPrimaryControlState.Definition.ActuatorSystem.ControlSystem.Instrumentation;
        var secondaryInstrumentation = turbineSecondaryControlState.Definition.ActuatorSystem.ControlSystem.Instrumentation;
        if (!ReferenceEquals(instrumentationState.Definition, instrumentationDefinition)
            || !ReferenceEquals(measuredSignals.Definition, instrumentationDefinition)
            || !ReferenceEquals(reactorInstrumentation, instrumentationDefinition)
            || !ReferenceEquals(secondaryInstrumentation, instrumentationDefinition)
            || !ReferenceEquals(protectionState.Definition.Instrumentation, instrumentationDefinition)
            || !ReferenceEquals(alarmState.Definition.Instrumentation, instrumentationDefinition))
        {
            throw new ArgumentException("All M5.7 observation/control/protection/alarm states must use the same canonical instrumentation definition.");
        }

        if (!ReferenceEquals(alarmState.Definition.Protection, protectionState.Definition))
        {
            throw new ArgumentException("M5.7 alarm state must observe the exact canonical M5.5 protection definition.");
        }
    }

    public IntegratedSecondaryCycleDefinition PlantDefinition { get; }
    public InstrumentationSystemDefinition InstrumentationDefinition { get; }
    public FullPlantState PlantState { get; }
    public InstrumentationState InstrumentationState { get; }
    public MeasuredSignalFrame MeasuredSignals { get; }
    public ReactorPrimaryControlState ReactorPrimaryControlState { get; }
    public TurbineSecondaryControlState TurbineSecondaryControlState { get; }
    public ProtectionSystemState ProtectionState { get; }
    public AlarmSystemState AlarmState { get; }
}
