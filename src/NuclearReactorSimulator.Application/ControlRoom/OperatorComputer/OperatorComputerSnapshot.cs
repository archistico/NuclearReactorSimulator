using System.Collections.ObjectModel;

namespace NuclearReactorSimulator.Application.ControlRoom.OperatorComputer;

public sealed record OperatorComputerSnapshot
{
    public OperatorComputerSnapshot(
        OperatorComputerRuntimeStatusSnapshot runtimeStatus,
        IEnumerable<OperatorComputerPageSnapshot> pages,
        OperatorComputerInformationSnapshot? information = null,
        OperatorComputerGuidanceSnapshot? guidance = null,
        OperatorComputerDiagnosticsSnapshot? diagnostics = null,
        OperatorComputerAlarmSnapshot? alarms = null,
        OperatorComputerLogSnapshot? log = null,
        OperatorComputerCommandConsoleSnapshot? commands = null,
        OperatorComputerModesSnapshot? modes = null,
        OperatorComputerSessionSnapshot? session = null)
    {
        RuntimeStatus = runtimeStatus ?? throw new ArgumentNullException(nameof(runtimeStatus));
        ArgumentNullException.ThrowIfNull(pages);

        var pageArray = pages.ToArray();
        if (pageArray.Length != OperatorComputerPageCatalog.Default.Count)
        {
            throw new ArgumentException("Operator computer snapshot must contain every fixed terminal page exactly once.", nameof(pages));
        }

        if (pageArray.Select(static page => page.Id).Distinct().Count() != pageArray.Length)
        {
            throw new ArgumentException("Operator computer snapshot contains duplicate terminal page identifiers.", nameof(pages));
        }

        var expectedOrder = OperatorComputerPageCatalog.Default.Select(static page => page.Id).ToArray();
        if (!expectedOrder.SequenceEqual(pageArray.Select(static page => page.Id)))
        {
            throw new ArgumentException("Operator computer snapshot page order does not match the fixed M10 terminal catalog.", nameof(pages));
        }

        Pages = new ReadOnlyCollection<OperatorComputerPageSnapshot>(pageArray);
        Information = information;
        Guidance = guidance;
        Diagnostics = diagnostics;
        Alarms = alarms;
        Log = log;
        Commands = commands;
        Modes = modes;
        Session = session;
    }

    public OperatorComputerRuntimeStatusSnapshot RuntimeStatus { get; }

    public IReadOnlyList<OperatorComputerPageSnapshot> Pages { get; }

    public OperatorComputerInformationSnapshot? Information { get; }

    public OperatorComputerGuidanceSnapshot? Guidance { get; }

    public OperatorComputerDiagnosticsSnapshot? Diagnostics { get; }

    public OperatorComputerAlarmSnapshot? Alarms { get; }

    public OperatorComputerLogSnapshot? Log { get; }

    public OperatorComputerCommandConsoleSnapshot? Commands { get; }

    public OperatorComputerModesSnapshot? Modes { get; }

    public OperatorComputerSessionSnapshot? Session { get; }
}
