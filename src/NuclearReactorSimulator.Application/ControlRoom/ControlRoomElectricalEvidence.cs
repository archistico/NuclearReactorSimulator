namespace NuclearReactorSimulator.Application.ControlRoom;

/// <summary>Shared read-only electrical evidence aggregation used by challenge and mission presentation projectors.</summary>
internal static class ControlRoomElectricalEvidence
{
    public static double? RequestedGeneratorLoadMegawatts(ControlRoomSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (snapshot.Electrical.Generators.Count == 0)
        {
            return null;
        }

        var total = 0d;
        foreach (var generator in snapshot.Electrical.Generators)
        {
            if (!generator.RequestedElectricalPower.NumericValue.HasValue)
            {
                return null;
            }
            total += generator.RequestedElectricalPower.NumericValue.Value;
        }
        return total;
    }
}
