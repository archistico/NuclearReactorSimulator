using System.Collections.ObjectModel;
using NuclearReactorSimulator.Domain.Physics.Control.TurbineSecondary;
using NuclearReactorSimulator.Simulation.Physics.Control;

namespace NuclearReactorSimulator.Simulation.Physics.Control.TurbineSecondary;

/// <summary>Per-step M5.4 controller inputs. Protection overrides remain deferred to M5.5.</summary>
public sealed class TurbineSecondaryControlInputs
{
    public TurbineSecondaryControlInputs(
        TurbineSecondaryControlSystemDefinition definition,
        ControllerInputs controllers,
        IEnumerable<TurbineIsolationValveCommand>? isolationValveCommands = null)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        Controllers = controllers ?? throw new ArgumentNullException(nameof(controllers));
        if (!ReferenceEquals(controllers.Definition, definition.ActuatorSystem.ControlSystem))
        {
            throw new ArgumentException("Controller inputs do not use the turbine/secondary control system's canonical controller definition.", nameof(controllers));
        }

        var commands = (isolationValveCommands ?? Array.Empty<TurbineIsolationValveCommand>())
            .Select(command => command ?? throw new ArgumentException("Isolation-valve commands cannot contain null entries.", nameof(isolationValveCommands)))
            .ToArray();
        var duplicate = commands
            .GroupBy(static command => command.ValveId, StringComparer.Ordinal)
            .FirstOrDefault(static group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new ArgumentException($"Duplicate isolation-valve command for '{duplicate.Key}'.", nameof(isolationValveCommands));
        }

        var stopValveIds = definition.PlantDefinition.TurbineExpansionSystem.MainSteamNetwork.AdmissionTrains
            .Select(static train => train.StopValveId)
            .ToHashSet(StringComparer.Ordinal);
        var invalid = commands.FirstOrDefault(command => !stopValveIds.Contains(command.ValveId));
        if (invalid is not null)
        {
            throw new ArgumentException(
                $"Valve '{invalid.ValveId}' is not a canonical turbine stop/isolation valve.",
                nameof(isolationValveCommands));
        }

        IsolationValveCommands = new ReadOnlyCollection<TurbineIsolationValveCommand>(commands);
    }

    public TurbineSecondaryControlSystemDefinition Definition { get; }
    public ControllerInputs Controllers { get; }
    public IReadOnlyList<TurbineIsolationValveCommand> IsolationValveCommands { get; }
}
