using System.Collections.ObjectModel;

namespace NuclearReactorSimulator.Application.ControlRoom;

/// <summary>
/// Presentation-only view of canonical M5.5 protection-reset readiness. This does not own or execute reset logic;
/// the protection solver remains authoritative when a reset command is applied on a fixed step.
/// </summary>
public sealed class ProtectionResetPresentationSnapshot
{
    public ProtectionResetPresentationSnapshot(
        bool anyTripActive,
        bool resetConditionsSatisfied,
        bool lastResetRequested,
        bool lastResetAccepted,
        IEnumerable<string>? blockers = null)
    {
        AnyTripActive = anyTripActive;
        ResetConditionsSatisfied = resetConditionsSatisfied;
        LastResetRequested = lastResetRequested;
        LastResetAccepted = lastResetAccepted;
        Blockers = new ReadOnlyCollection<string>((blockers ?? Array.Empty<string>()).ToArray());
    }

    public static ProtectionResetPresentationSnapshot Unavailable { get; } = new(false, false, false, false);

    public bool AnyTripActive { get; }

    public bool ResetConditionsSatisfied { get; }

    public bool LastResetRequested { get; }

    public bool LastResetAccepted { get; }

    public bool LastResetRejected => LastResetRequested && !LastResetAccepted;

    public bool CanResetNow => AnyTripActive && ResetConditionsSatisfied;

    public IReadOnlyList<string> Blockers { get; }

    public ControlRoomVisualState State => !AnyTripActive
        ? ControlRoomVisualState.Unavailable
        : CanResetNow
            ? ControlRoomVisualState.Normal
            : ControlRoomVisualState.Warning;

    public string StatusText
    {
        get
        {
            if (!AnyTripActive)
            {
                return "PROTECTION CLEAR — no latched reactor, turbine or generator trip requires reset.";
            }

            if (CanResetNow)
            {
                return LastResetAccepted
                    ? "RESET ACCEPTED — canonical M5.5 reset conditions and permissives were satisfied."
                    : "RESET AVAILABLE — canonical M5.5 reset conditions and permissives are currently satisfied.";
            }

            var reason = Blockers.Count == 0
                ? "one or more canonical M5.5 reset conditions are not satisfied"
                : string.Join(" · ", Blockers.Take(3));
            return LastResetRejected
                ? $"RESET BLOCKED — {reason}."
                : $"RESET NOT READY — {reason}.";
        }
    }
}
