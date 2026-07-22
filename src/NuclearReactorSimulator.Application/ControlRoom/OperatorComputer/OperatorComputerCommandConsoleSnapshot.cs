using System.Collections.ObjectModel;

namespace NuclearReactorSimulator.Application.ControlRoom.OperatorComputer;

public sealed record OperatorComputerCommandConsoleSnapshot
{
    public OperatorComputerCommandConsoleSnapshot(IEnumerable<OperatorComputerCommandSnapshot> commands)
    {
        ArgumentNullException.ThrowIfNull(commands);
        var commandArray = commands.ToArray();
        if (commandArray.Select(static command => command.EntryId).Distinct(StringComparer.Ordinal).Count() != commandArray.Length)
        {
            throw new ArgumentException("Operator-computer command entry identifiers must be unique.", nameof(commands));
        }

        Commands = new ReadOnlyCollection<OperatorComputerCommandSnapshot>(commandArray);
    }

    public IReadOnlyList<OperatorComputerCommandSnapshot> Commands { get; }

    public int AvailableCount => Commands.Count(static command => command.Availability == OperatorComputerCommandAvailability.Available);

    public int BlockedCount => Commands.Count(static command => command.Availability == OperatorComputerCommandAvailability.Blocked);

    public int UnavailableCount => Commands.Count(static command => command.Availability == OperatorComputerCommandAvailability.Unavailable);
}
