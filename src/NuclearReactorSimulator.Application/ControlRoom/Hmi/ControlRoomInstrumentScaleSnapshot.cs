using System.Collections.ObjectModel;

namespace NuclearReactorSimulator.Application.ControlRoom.Hmi;

/// <summary>
/// Immutable HMI scale metadata. The contract deliberately separates measurable scale, operating bands,
/// scenario/controller target, setpoint and protection thresholds so the UI never infers "safe" from "in range".
/// </summary>
public sealed class ControlRoomInstrumentScaleSnapshot
{
    public ControlRoomInstrumentScaleSnapshot(
        double minimum,
        double maximum,
        IEnumerable<ControlRoomInstrumentBandSnapshot>? operatingBands = null,
        ControlRoomTargetBandSnapshot? targetBand = null,
        double? setpoint = null,
        IEnumerable<ControlRoomProtectionLimitSnapshot>? protectionLimits = null)
    {
        if (!double.IsFinite(minimum))
        {
            throw new ArgumentOutOfRangeException(nameof(minimum));
        }
        if (!double.IsFinite(maximum) || maximum <= minimum)
        {
            throw new ArgumentOutOfRangeException(nameof(maximum));
        }
        if (setpoint is not null && (!double.IsFinite(setpoint.Value) || setpoint.Value < minimum || setpoint.Value > maximum))
        {
            throw new ArgumentOutOfRangeException(nameof(setpoint));
        }

        var bands = (operatingBands ?? Array.Empty<ControlRoomInstrumentBandSnapshot>()).ToArray();
        if (bands.Any(band => band.Minimum < minimum || band.Maximum > maximum))
        {
            throw new ArgumentOutOfRangeException(nameof(operatingBands), "Operating bands must remain inside the instrument scale.");
        }
        if (targetBand is not null && (targetBand.Minimum < minimum || targetBand.Maximum > maximum))
        {
            throw new ArgumentOutOfRangeException(nameof(targetBand), "Target band must remain inside the instrument scale.");
        }

        var limits = (protectionLimits ?? Array.Empty<ControlRoomProtectionLimitSnapshot>()).ToArray();
        if (limits.Any(limit => limit.Threshold < minimum || limit.Threshold > maximum))
        {
            throw new ArgumentOutOfRangeException(nameof(protectionLimits), "Protection markers must remain inside the displayed instrument scale.");
        }

        Minimum = minimum;
        Maximum = maximum;
        OperatingBands = new ReadOnlyCollection<ControlRoomInstrumentBandSnapshot>(bands);
        TargetBand = targetBand;
        Setpoint = setpoint;
        ProtectionLimits = new ReadOnlyCollection<ControlRoomProtectionLimitSnapshot>(limits);
    }

    public double Minimum { get; }
    public double Maximum { get; }
    public IReadOnlyList<ControlRoomInstrumentBandSnapshot> OperatingBands { get; }
    public ControlRoomTargetBandSnapshot? TargetBand { get; }
    public double? Setpoint { get; }
    public IReadOnlyList<ControlRoomProtectionLimitSnapshot> ProtectionLimits { get; }
}
