using System.Collections.ObjectModel;
using NuclearReactorSimulator.Domain.Physics.Control;

namespace NuclearReactorSimulator.Simulation.Physics.Control;

public sealed class ControllerSystemSnapshot
{
    public ControllerSystemSnapshot(
        ControlSystemDefinition definition,
        ControllerOutputFrame outputs,
        IEnumerable<ControllerDiagnosticSnapshot> diagnostics)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        Outputs = outputs ?? throw new ArgumentNullException(nameof(outputs));
        Diagnostics = new ReadOnlyCollection<ControllerDiagnosticSnapshot>(diagnostics.OrderBy(static item => item.ControllerId, StringComparer.Ordinal).ToArray());
    }

    public ControlSystemDefinition Definition { get; }
    public ControllerOutputFrame Outputs { get; }
    public IReadOnlyList<ControllerDiagnosticSnapshot> Diagnostics { get; }

    public ControllerDiagnosticSnapshot GetDiagnostic(string id)
        => Diagnostics.FirstOrDefault(item => string.Equals(item.ControllerId, id, StringComparison.Ordinal))
            ?? throw new KeyNotFoundException($"Unknown controller diagnostic '{id}'.");
}
