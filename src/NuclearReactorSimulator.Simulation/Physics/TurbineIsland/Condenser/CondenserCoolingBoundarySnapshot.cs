using System.Text.Json.Serialization;
using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Condenser;

public sealed record CondenserCoolingBoundarySnapshot(
    string BoundaryId,
    string CondenserId,
    Temperature CoolantTemperature,
    Power AvailableHeatRejectionPower,
    Power SurfaceHeatTransferLimitedPower,
    Power EffectiveHeatRejectionCapacity,
    Power UsedHeatRejectionPower,
    [property: JsonIgnore] Power InstalledHeatRejectionCapacity,
    [property: JsonIgnore] bool ExplicitInstalledCapacityModelActive,
    [property: JsonIgnore] bool SurfaceHeatTransferModelActive)
{
    private const double PowerToleranceWatts = 1e-6d;

    public Power UnusedHeatRejectionPower => AvailableHeatRejectionPower - UsedHeatRejectionPower;

    [JsonIgnore]
    public bool EffectiveCapacityFullyUsed =>
        Math.Abs(EffectiveHeatRejectionCapacity.Watts - UsedHeatRejectionPower.Watts) <= PowerToleranceWatts;

    [JsonIgnore]
    public bool InstalledCoolingCapacityLimitActive =>
        EffectiveCapacityFullyUsed
        && InstalledHeatRejectionCapacity.Watts <= AvailableHeatRejectionPower.Watts + PowerToleranceWatts
        && (!SurfaceHeatTransferModelActive
            || InstalledHeatRejectionCapacity.Watts <= SurfaceHeatTransferLimitedPower.Watts + PowerToleranceWatts);

    [JsonIgnore]
    public bool AvailableCoolingCapacityLimitActive =>
        ExplicitInstalledCapacityModelActive
        && EffectiveCapacityFullyUsed
        && AvailableHeatRejectionPower.Watts <= InstalledHeatRejectionCapacity.Watts + PowerToleranceWatts
        && (!SurfaceHeatTransferModelActive
            || AvailableHeatRejectionPower.Watts <= SurfaceHeatTransferLimitedPower.Watts + PowerToleranceWatts);

    [JsonIgnore]
    public bool SurfaceHeatTransferLimitActive =>
        SurfaceHeatTransferModelActive
        && EffectiveCapacityFullyUsed
        && SurfaceHeatTransferLimitedPower.Watts <= InstalledHeatRejectionCapacity.Watts + PowerToleranceWatts
        && SurfaceHeatTransferLimitedPower.Watts <= AvailableHeatRejectionPower.Watts + PowerToleranceWatts;

    [JsonIgnore]
    public Power EffectiveCapacityMargin => Power.FromWatts(Math.Max(
        0d,
        EffectiveHeatRejectionCapacity.Watts - UsedHeatRejectionPower.Watts));

    [JsonIgnore]
    public Power InstalledCapacityMargin => Power.FromWatts(Math.Max(
        0d,
        InstalledHeatRejectionCapacity.Watts - UsedHeatRejectionPower.Watts));

    [JsonIgnore]
    public Power AvailableCapacityMargin => Power.FromWatts(Math.Max(
        0d,
        AvailableHeatRejectionPower.Watts - UsedHeatRejectionPower.Watts));

    [JsonIgnore]
    public Power SurfaceTransferMargin => Power.FromWatts(Math.Max(
        0d,
        SurfaceHeatTransferLimitedPower.Watts - UsedHeatRejectionPower.Watts));

    [JsonIgnore]
    public string ActiveHeatRejectionLimits
    {
        get
        {
            if (!EffectiveCapacityFullyUsed)
            {
                return "CAPACITY HEADROOM";
            }

            var limits = new List<string>(3);
            if (InstalledCoolingCapacityLimitActive) limits.Add("INSTALLED CAPACITY");
            if (AvailableCoolingCapacityLimitActive) limits.Add("AVAILABLE COOLING");
            if (SurfaceHeatTransferLimitActive) limits.Add("SURFACE UA");
            return limits.Count == 0 ? "NONE" : string.Join(" + ", limits);
        }
    }
}
