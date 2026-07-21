using System.Collections.ObjectModel;

namespace NuclearReactorSimulator.Application.ControlRoom;

/// <summary>
/// M6.3 presentation contract for the reactor/core workspace. It contains formatted/semantic data only and exposes no
/// authoritative simulation or domain state.
/// </summary>
public sealed class ReactorCorePanelSnapshot
{
    public ReactorCorePanelSnapshot(
        ControlRoomValueSnapshot reactorThermalPower,
        ControlRoomValueSnapshot reactorPeriod,
        ControlRoomValueSnapshot totalReactivity,
        ControlRoomValueSnapshot rodReactivity,
        ControlRoomValueSnapshot nonRodReactivity,
        ControlRoomValueSnapshot averageRodWithdrawal,
        ControlRoomValueSnapshot xenonReactivity,
        IEnumerable<ReactorCoreZonePresentationSnapshot> zones,
        IEnumerable<ReactorRodPresentationSnapshot> rods,
        IEnumerable<ReactorRodTargetPresentationSnapshot> rodTargets,
        bool reactorScramActive,
        bool rodWithdrawalInhibited)
    {
        ReactorThermalPower = reactorThermalPower ?? throw new ArgumentNullException(nameof(reactorThermalPower));
        ReactorPeriod = reactorPeriod ?? throw new ArgumentNullException(nameof(reactorPeriod));
        TotalReactivity = totalReactivity ?? throw new ArgumentNullException(nameof(totalReactivity));
        RodReactivity = rodReactivity ?? throw new ArgumentNullException(nameof(rodReactivity));
        NonRodReactivity = nonRodReactivity ?? throw new ArgumentNullException(nameof(nonRodReactivity));
        AverageRodWithdrawal = averageRodWithdrawal ?? throw new ArgumentNullException(nameof(averageRodWithdrawal));
        XenonReactivity = xenonReactivity ?? throw new ArgumentNullException(nameof(xenonReactivity));
        Zones = new ReadOnlyCollection<ReactorCoreZonePresentationSnapshot>((zones ?? throw new ArgumentNullException(nameof(zones))).ToArray());
        Rods = new ReadOnlyCollection<ReactorRodPresentationSnapshot>((rods ?? throw new ArgumentNullException(nameof(rods))).ToArray());
        RodTargets = new ReadOnlyCollection<ReactorRodTargetPresentationSnapshot>((rodTargets ?? throw new ArgumentNullException(nameof(rodTargets))).ToArray());
        ReactorScramActive = reactorScramActive;
        RodWithdrawalInhibited = rodWithdrawalInhibited;
    }

    public static ReactorCorePanelSnapshot Unavailable { get; } = new(
        ControlRoomValueSnapshot.Unavailable("MWth"),
        ControlRoomValueSnapshot.Unavailable("s"),
        ControlRoomValueSnapshot.Unavailable("¢"),
        ControlRoomValueSnapshot.Unavailable("pcm"),
        ControlRoomValueSnapshot.Unavailable("pcm"),
        ControlRoomValueSnapshot.Unavailable("%"),
        ControlRoomValueSnapshot.Unavailable("pcm"),
        Array.Empty<ReactorCoreZonePresentationSnapshot>(),
        Array.Empty<ReactorRodPresentationSnapshot>(),
        Array.Empty<ReactorRodTargetPresentationSnapshot>(),
        false,
        false);

    public ControlRoomValueSnapshot ReactorThermalPower { get; }
    public ControlRoomValueSnapshot ReactorPeriod { get; }
    public ControlRoomValueSnapshot TotalReactivity { get; }
    public ControlRoomValueSnapshot RodReactivity { get; }
    public ControlRoomValueSnapshot NonRodReactivity { get; }
    public ControlRoomValueSnapshot AverageRodWithdrawal { get; }
    public ControlRoomValueSnapshot XenonReactivity { get; }
    public IReadOnlyList<ReactorCoreZonePresentationSnapshot> Zones { get; }
    public IReadOnlyList<ReactorRodPresentationSnapshot> Rods { get; }
    public IReadOnlyList<ReactorRodTargetPresentationSnapshot> RodTargets { get; }
    public bool ReactorScramActive { get; }
    public bool RodWithdrawalInhibited { get; }
    public int ZoneCount => Zones.Count;
    public int RodCount => Rods.Count;
    public ControlRoomVisualState ProtectionState => ReactorScramActive ? ControlRoomVisualState.Trip : ControlRoomVisualState.Normal;
    public ControlRoomVisualState RodWithdrawalInterlockState => RodWithdrawalInhibited ? ControlRoomVisualState.Warning : ControlRoomVisualState.Normal;
    public string ProtectionText => ReactorScramActive ? "REACTOR SCRAM ACTIVE" : "Reactor protection not tripped";
    public string RodWithdrawalInterlockText => RodWithdrawalInhibited ? "ROD WITHDRAWAL INHIBITED" : "Rod withdrawal permitted by protection interlock state";
}
