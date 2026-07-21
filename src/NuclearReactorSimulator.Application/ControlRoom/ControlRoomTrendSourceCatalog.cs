using System.Collections.ObjectModel;

namespace NuclearReactorSimulator.Application.ControlRoom;

/// <summary>Stable configurable M6.6 trend sources resolved only from presentation snapshots.</summary>
public static class ControlRoomTrendSourceCatalog
{
    public const string ReactorThermalPower = "reactor.thermal-power";
    public const string PrimaryFeedwaterFlow = "primary.feedwater-flow";
    public const string PrimarySteamExportFlow = "primary.steam-export-flow";
    public const string TurbineShaftPower = "turbine.shaft-power";
    public const string GrossElectricalOutput = "electrical.gross-output";
    public const string UnacknowledgedAlarms = "alarms.unacknowledged";

    public static IReadOnlyList<ControlRoomTrendSourceDescriptor> Default { get; } =
        new ReadOnlyCollection<ControlRoomTrendSourceDescriptor>(new[]
        {
            new ControlRoomTrendSourceDescriptor(ReactorThermalPower, "Reactor thermal power", "MWth", "MEASURED"),
            new ControlRoomTrendSourceDescriptor(PrimaryFeedwaterFlow, "Total feedwater flow", "kg/s", "MODEL DIAGNOSTIC"),
            new ControlRoomTrendSourceDescriptor(PrimarySteamExportFlow, "Total steam export flow", "kg/s", "MODEL DIAGNOSTIC"),
            new ControlRoomTrendSourceDescriptor(TurbineShaftPower, "Turbine shaft power", "MW", "MEASURED"),
            new ControlRoomTrendSourceDescriptor(GrossElectricalOutput, "Gross electrical output", "MWe", "MEASURED"),
            new ControlRoomTrendSourceDescriptor(UnacknowledgedAlarms, "Unacknowledged alarms", "count", "ANNUNCIATOR"),
        });

    public static IReadOnlyList<string> DefaultEnabledSourceIds { get; } = new ReadOnlyCollection<string>(new[]
    {
        ReactorThermalPower,
        PrimarySteamExportFlow,
        TurbineShaftPower,
        GrossElectricalOutput,
    });

    public static ControlRoomTrendSourceDescriptor Get(string id)
        => Default.FirstOrDefault(item => string.Equals(item.Id, id, StringComparison.Ordinal))
            ?? throw new KeyNotFoundException($"Unknown control-room trend source '{id}'.");
}
