namespace NuclearReactorSimulator.Application.ControlRoom;

/// <summary>Presentation-only diagnostic tile for one aggregated core zone.</summary>
public sealed record ReactorCoreZonePresentationSnapshot(
    string ZoneId,
    int Row,
    int Column,
    double PowerMegawatts,
    double PowerFractionPercent,
    double FuelTemperatureCelsius,
    double CoolantTemperatureCelsius,
    double? VoidPercent,
    ControlRoomVisualState State)
{
    public string PowerText => FormattableString.Invariant($"{PowerMegawatts:0.0} MW");
    public string PowerFractionText => FormattableString.Invariant($"{PowerFractionPercent:0.0}% core");
    public string FuelTemperatureText => FormattableString.Invariant($"Fuel {FuelTemperatureCelsius:0.0} °C");
    public string CoolantTemperatureText => FormattableString.Invariant($"Coolant {CoolantTemperatureCelsius:0.0} °C");
    public string VoidText => VoidPercent.HasValue
        ? FormattableString.Invariant($"Void {VoidPercent.Value:0.0}%")
        : "Void —";
}
