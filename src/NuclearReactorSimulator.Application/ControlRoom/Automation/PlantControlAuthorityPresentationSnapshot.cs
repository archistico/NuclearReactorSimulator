using System.Collections.ObjectModel;
using NuclearReactorSimulator.Domain.Physics.Control.Supervisory;

namespace NuclearReactorSimulator.Application.ControlRoom.Automation;

public sealed class PlantControlAuthorityPresentationSnapshot
{
    public PlantControlAuthorityPresentationSnapshot(
        bool isAvailable,
        PlantControlAuthorityMode requestedAuthority,
        PlantControlAuthorityMode effectiveAuthority,
        PlantControlAuthorityHealth health,
        string? degradationReason,
        string objectiveText,
        long transitionSequence,
        IEnumerable<PlantControllerModePresentationSnapshot> controllerModes)
    {
        if (!Enum.IsDefined(requestedAuthority))
        {
            throw new ArgumentOutOfRangeException(nameof(requestedAuthority));
        }
        if (!Enum.IsDefined(effectiveAuthority))
        {
            throw new ArgumentOutOfRangeException(nameof(effectiveAuthority));
        }
        if (!Enum.IsDefined(health))
        {
            throw new ArgumentOutOfRangeException(nameof(health));
        }
        if (transitionSequence < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(transitionSequence));
        }
        ArgumentNullException.ThrowIfNull(controllerModes);

        IsAvailable = isAvailable;
        RequestedAuthority = requestedAuthority;
        EffectiveAuthority = effectiveAuthority;
        Health = health;
        DegradationReason = string.IsNullOrWhiteSpace(degradationReason) ? null : degradationReason.Trim();
        ObjectiveText = objectiveText ?? string.Empty;
        TransitionSequence = transitionSequence;
        ControllerModes = new ReadOnlyCollection<PlantControllerModePresentationSnapshot>(controllerModes.ToArray());
    }

    public bool IsAvailable { get; }
    public PlantControlAuthorityMode RequestedAuthority { get; }
    public PlantControlAuthorityMode EffectiveAuthority { get; }
    public PlantControlAuthorityHealth Health { get; }
    public string? DegradationReason { get; }
    public string ObjectiveText { get; }
    public long TransitionSequence { get; }
    public IReadOnlyList<PlantControllerModePresentationSnapshot> ControllerModes { get; }
    public bool IsMixedMode => ControllerModes.Select(static item => item.Mode).Distinct().Skip(1).Any();

    public static PlantControlAuthorityPresentationSnapshot Unavailable { get; } = new(
        false,
        PlantControlAuthorityMode.Manual,
        PlantControlAuthorityMode.Manual,
        PlantControlAuthorityHealth.Degraded,
        "No plant-control authority runtime is attached.",
        "NONE",
        0,
        Array.Empty<PlantControllerModePresentationSnapshot>());
}
