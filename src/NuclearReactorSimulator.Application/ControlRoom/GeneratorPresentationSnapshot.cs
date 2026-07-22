using System.Text.Json.Serialization;

namespace NuclearReactorSimulator.Application.ControlRoom;

public sealed record GeneratorPresentationSnapshot(
    string GeneratorId,
    string RotorId,
    string BreakerId,
    ControlRoomValueSnapshot Frequency,
    ControlRoomValueSnapshot ElectricalOutput,
    ControlRoomValueSnapshot TerminalVoltage,
    ControlRoomValueSnapshot GridVoltage,
    ControlRoomValueSnapshot PhaseDifference,
    ControlRoomValueSnapshot MechanicalInputPower,
    ControlRoomValueSnapshot ConversionLossPower,
    bool SynchronizationConditionsSatisfied,
    bool BreakerClosed,
    bool CloseCommandAccepted,
    bool CloseCommandRejected,
    [property: JsonIgnore] double CloseCheckFrequencyDifferenceHz = 0d,
    [property: JsonIgnore] double MaximumSynchronizationFrequencyDifferenceHz = 0d,
    [property: JsonIgnore] double CloseCheckPhaseDifferenceDegrees = 0d,
    [property: JsonIgnore] double MaximumSynchronizationPhaseDifferenceDegrees = 0d,
    [property: JsonIgnore] double CloseCheckVoltageDifferenceKilovolts = 0d,
    [property: JsonIgnore] double MaximumSynchronizationVoltageDifferenceKilovolts = 0d)
{
    // Fingerprint-v1 compatibility: these legacy computed properties intentionally preserve
    // their M10.7 serialization semantics. The breaker-aware operator presentation lives in
    // JsonIgnored Display* properties below so replay/archive fingerprints remain stable.
    public ControlRoomVisualState SynchronizationState => SynchronizationConditionsSatisfied
        ? ControlRoomVisualState.Normal
        : ControlRoomVisualState.Warning;

    public ControlRoomVisualState BreakerState => CloseCommandRejected
        ? ControlRoomVisualState.Warning
        : ControlRoomVisualState.Normal;

    public string SynchronizationText => SynchronizationConditionsSatisfied
        ? "SYNCHRONIZATION WINDOW SATISFIED"
        : "OUTSIDE SYNCHRONIZATION WINDOW";

    [JsonIgnore]
    public ControlRoomVisualState DisplaySynchronizationState => BreakerClosed || SynchronizationConditionsSatisfied
        ? ControlRoomVisualState.Normal
        : ControlRoomVisualState.Warning;

    [JsonIgnore]
    public string SynchronizationLabel => BreakerClosed ? "PARALLELED" : "SYNC";

    [JsonIgnore]
    public string DisplaySynchronizationText => BreakerClosed
        ? "PARALLELED — breaker closed; the pre-close synchronization permissive is no longer an operator warning."
        : SynchronizationConditionsSatisfied
            ? "SYNC READY — canonical frequency, phase and voltage close-check limits are satisfied."
            : "SYNC NOT READY — inspect the close-check differences below before closing the breaker.";

    [JsonIgnore]
    public string SynchronizationDetailText
    {
        get
        {
            if (BreakerClosed)
            {
                return "GRID CONNECTION: PARALLELED · BREAKER CLOSED";
            }

            if (MaximumSynchronizationFrequencyDifferenceHz <= 0d
                || MaximumSynchronizationPhaseDifferenceDegrees <= 0d
                || MaximumSynchronizationVoltageDifferenceKilovolts <= 0d)
            {
                return "CLOSE-CHECK DETAIL UNAVAILABLE · use the canonical SYNC READY / NOT READY permissive.";
            }

            var frequencyOk = CloseCheckFrequencyDifferenceHz <= MaximumSynchronizationFrequencyDifferenceHz;
            var phaseOk = CloseCheckPhaseDifferenceDegrees <= MaximumSynchronizationPhaseDifferenceDegrees;
            var voltageOk = CloseCheckVoltageDifferenceKilovolts <= MaximumSynchronizationVoltageDifferenceKilovolts;

            return string.Join(
                " · ",
                FormattableString.Invariant($"Δf {CloseCheckFrequencyDifferenceHz:0.000}/{MaximumSynchronizationFrequencyDifferenceHz:0.000} Hz [{Status(frequencyOk)}]"),
                FormattableString.Invariant($"Δphase {CloseCheckPhaseDifferenceDegrees:0.00}/{MaximumSynchronizationPhaseDifferenceDegrees:0.00}° [{Status(phaseOk)}]"),
                FormattableString.Invariant($"ΔV {CloseCheckVoltageDifferenceKilovolts:0.0}/{MaximumSynchronizationVoltageDifferenceKilovolts:0.0} kV [{Status(voltageOk)}]"));
        }
    }

    public string BreakerText => BreakerClosed ? "BREAKER CLOSED" : "BREAKER OPEN";

    private static string Status(bool satisfied) => satisfied ? "OK" : "WAIT";
}
