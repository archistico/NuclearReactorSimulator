using System.Collections.ObjectModel;

namespace NuclearReactorSimulator.Application.ControlRoom.OperatorComputer;

public sealed record OperatorComputerInformationSnapshot
{
    public OperatorComputerInformationSnapshot(IEnumerable<OperatorComputerInformationSectionSnapshot> sections)
    {
        ArgumentNullException.ThrowIfNull(sections);
        var array = sections.ToArray();
        if (array.Length == 0 || array.Any(static section => section is null))
        {
            throw new ArgumentException("Operator computer information requires at least one non-null section.", nameof(sections));
        }
        if (array.Select(static section => section.SectionId).Distinct(StringComparer.Ordinal).Count() != array.Length)
        {
            throw new ArgumentException("Operator computer information section IDs must be unique.", nameof(sections));
        }

        Sections = new ReadOnlyCollection<OperatorComputerInformationSectionSnapshot>(array);
    }

    public IReadOnlyList<OperatorComputerInformationSectionSnapshot> Sections { get; }
}
