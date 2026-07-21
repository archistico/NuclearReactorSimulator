using NuclearReactorSimulator.Simulation.Physics.Control.Alarms;
using NuclearReactorSimulator.Simulation.Physics.Control.Protection;
using NuclearReactorSimulator.Simulation.Physics.Control.ReactorPrimary;
using NuclearReactorSimulator.Simulation.Physics.Control.TurbineSecondary;
using NuclearReactorSimulator.Simulation.Physics.Instrumentation;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Integration;

namespace NuclearReactorSimulator.Simulation.Physics.Control.Integration;

/// <summary>Explicit per-step M5.7 inputs. Setpoint or disturbance changes are represented by replacing this immutable bundle.</summary>
public sealed class IntegratedAutomaticOperationInputs
{
    public IntegratedAutomaticOperationInputs(
        IntegratedSecondaryCycleInputs plantInputs,
        ReactorPrimaryControlInputs reactorPrimaryInputs,
        TurbineSecondaryControlInputs turbineSecondaryInputs,
        ProtectionSystemInputs protectionInputs,
        AlarmSystemInputs alarmInputs,
        InstrumentationInputs instrumentationInputs,
        HydraulicComponentFaultInputs? hydraulicFaultInputs = null)
    {
        PlantInputs = plantInputs ?? throw new ArgumentNullException(nameof(plantInputs));
        ReactorPrimaryInputs = reactorPrimaryInputs ?? throw new ArgumentNullException(nameof(reactorPrimaryInputs));
        TurbineSecondaryInputs = turbineSecondaryInputs ?? throw new ArgumentNullException(nameof(turbineSecondaryInputs));
        ProtectionInputs = protectionInputs ?? throw new ArgumentNullException(nameof(protectionInputs));
        AlarmInputs = alarmInputs ?? throw new ArgumentNullException(nameof(alarmInputs));
        InstrumentationInputs = instrumentationInputs ?? throw new ArgumentNullException(nameof(instrumentationInputs));
        HydraulicFaultInputs = hydraulicFaultInputs ?? HydraulicComponentFaultInputs.Empty;

        if (!ReferenceEquals(reactorPrimaryInputs.Definition.PlantDefinition, plantInputs.Definition)
            || !ReferenceEquals(turbineSecondaryInputs.Definition.PlantDefinition, plantInputs.Definition)
            || !ReferenceEquals(protectionInputs.Definition.PlantDefinition, plantInputs.Definition))
        {
            throw new ArgumentException("M5.7 plant/control/protection inputs must use the same canonical full-plant definition.");
        }

        var instrumentation = reactorPrimaryInputs.Definition.ActuatorSystem.ControlSystem.Instrumentation;
        if (!ReferenceEquals(turbineSecondaryInputs.Definition.ActuatorSystem.ControlSystem.Instrumentation, instrumentation)
            || !ReferenceEquals(protectionInputs.Definition.Instrumentation, instrumentation)
            || !ReferenceEquals(alarmInputs.Definition.Instrumentation, instrumentation)
            || !ReferenceEquals(instrumentationInputs.Definition, instrumentation))
        {
            throw new ArgumentException("M5.7 control/protection/alarm/instrumentation inputs must use one canonical instrumentation definition.");
        }

        if (!ReferenceEquals(alarmInputs.Definition.Protection, protectionInputs.Definition))
        {
            throw new ArgumentException("M5.7 alarm inputs must observe the exact canonical protection definition.");
        }
    }

    public IntegratedSecondaryCycleInputs PlantInputs { get; }
    public ReactorPrimaryControlInputs ReactorPrimaryInputs { get; }
    public TurbineSecondaryControlInputs TurbineSecondaryInputs { get; }
    public ProtectionSystemInputs ProtectionInputs { get; }
    public AlarmSystemInputs AlarmInputs { get; }
    public InstrumentationInputs InstrumentationInputs { get; }
    public HydraulicComponentFaultInputs HydraulicFaultInputs { get; }
}
