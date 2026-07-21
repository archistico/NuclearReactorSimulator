using System.Collections.ObjectModel;
using NuclearReactorSimulator.Domain.Physics.Control;

namespace NuclearReactorSimulator.Simulation.Physics.Control;

/// <summary>Typed M5.2 command boundary. Applying these commands to physical plant inputs belongs to M5.3/M5.4.</summary>
public sealed class ActuatorCommandFrame
{
    public ActuatorCommandFrame(
        ActuatorSystemDefinition definition,
        IEnumerable<ValveActuatorCommand> valves,
        IEnumerable<PumpActuatorCommand> pumps,
        IEnumerable<RodActuatorCommand> rods)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        ValveCommands = new ReadOnlyCollection<ValveActuatorCommand>(valves.OrderBy(static item => item.ActuatorId, StringComparer.Ordinal).ToArray());
        PumpCommands = new ReadOnlyCollection<PumpActuatorCommand>(pumps.OrderBy(static item => item.ActuatorId, StringComparer.Ordinal).ToArray());
        RodCommands = new ReadOnlyCollection<RodActuatorCommand>(rods.OrderBy(static item => item.ActuatorId, StringComparer.Ordinal).ToArray());
    }

    public ActuatorSystemDefinition Definition { get; }
    public IReadOnlyList<ValveActuatorCommand> ValveCommands { get; }
    public IReadOnlyList<PumpActuatorCommand> PumpCommands { get; }
    public IReadOnlyList<RodActuatorCommand> RodCommands { get; }
}
