namespace NuclearReactorSimulator.Domain.Physics.Reactor.Core;

/// <summary>
/// Logical, zero-based core-map coordinate. Coordinates provide deterministic placement only;
/// the simulation engine does not assume a fixed grid size or rectangular occupancy.
/// </summary>
public readonly record struct CoreZoneCoordinate : IComparable<CoreZoneCoordinate>
{
    public CoreZoneCoordinate(int row, int column)
    {
        if (row < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(row), row, "Core-zone row cannot be negative.");
        }

        if (column < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(column), column, "Core-zone column cannot be negative.");
        }

        Row = row;
        Column = column;
    }

    public int Row { get; }

    public int Column { get; }

    public int CompareTo(CoreZoneCoordinate other)
    {
        var rowComparison = Row.CompareTo(other.Row);
        return rowComparison != 0 ? rowComparison : Column.CompareTo(other.Column);
    }
}
