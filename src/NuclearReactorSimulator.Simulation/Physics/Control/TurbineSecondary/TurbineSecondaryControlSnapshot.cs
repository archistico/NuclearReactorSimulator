using System.Collections.ObjectModel;
using NuclearReactorSimulator.Domain.Physics.Control.TurbineSecondary;
using NuclearReactorSimulator.Simulation.Physics.Control;

namespace NuclearReactorSimulator.Simulation.Physics.Control.TurbineSecondary;

public sealed class TurbineSecondaryControlSnapshot
{
    public TurbineSecondaryControlSnapshot(
        TurbineSecondaryControlSystemDefinition definition,
        ControlAndActuatorSnapshot controlAndActuator,
        IEnumerable<TurbineSecondaryLoopDiagnosticSnapshot> loops)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        ControlAndActuator = controlAndActuator ?? throw new ArgumentNullException(nameof(controlAndActuator));
        ArgumentNullException.ThrowIfNull(loops);
        Loops = new ReadOnlyCollection<TurbineSecondaryLoopDiagnosticSnapshot>(
            loops.OrderBy(static item => item.LoopId, StringComparer.Ordinal).ToArray());
    }

    public TurbineSecondaryControlSystemDefinition Definition { get; }
    public ControlAndActuatorSnapshot ControlAndActuator { get; }
    public IReadOnlyList<TurbineSecondaryLoopDiagnosticSnapshot> Loops { get; }
}
