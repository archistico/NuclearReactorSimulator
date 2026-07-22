using System.Collections.ObjectModel;

namespace NuclearReactorSimulator.Application.ControlRoom.OperatorComputer;

public sealed record OperatorComputerDiagnosticsSnapshot
{
    public OperatorComputerDiagnosticsSnapshot(
        string title,
        IEnumerable<OperatorComputerDiagnosticItemSnapshot> items)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(items);
        var array = items.ToArray();
        if (array.Any(static item => item is null))
        {
            throw new ArgumentException("Diagnostics cannot contain null items.", nameof(items));
        }

        Title = title;
        Items = new ReadOnlyCollection<OperatorComputerDiagnosticItemSnapshot>(array);
    }

    public string Title { get; }
    public IReadOnlyList<OperatorComputerDiagnosticItemSnapshot> Items { get; }
    public int SatisfiedCount => Items.Count(static item => item.IsSatisfied);
    public int UnsatisfiedCount => Items.Count - SatisfiedCount;
    public bool AllChecksSatisfied => Items.Count > 0 && UnsatisfiedCount == 0;
}
