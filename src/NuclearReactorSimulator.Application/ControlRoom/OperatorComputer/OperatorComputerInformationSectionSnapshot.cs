using System.Collections.ObjectModel;

namespace NuclearReactorSimulator.Application.ControlRoom.OperatorComputer;

public sealed record OperatorComputerInformationSectionSnapshot
{
    public OperatorComputerInformationSectionSnapshot(string sectionId, string title, IEnumerable<OperatorComputerInformationItemSnapshot> items)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(items);

        var array = items.ToArray();
        if (array.Any(static item => item is null))
        {
            throw new ArgumentException("Information sections cannot contain null items.", nameof(items));
        }

        SectionId = sectionId;
        Title = title;
        Items = new ReadOnlyCollection<OperatorComputerInformationItemSnapshot>(array);
    }

    public string SectionId { get; }
    public string Title { get; }
    public IReadOnlyList<OperatorComputerInformationItemSnapshot> Items { get; }
}
