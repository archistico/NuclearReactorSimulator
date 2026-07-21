using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Domain.Physics.Reactor.ControlRods;

/// <summary>
/// Immutable definition of one control rod and its integral worth curve endpoints.
/// </summary>
public sealed record ControlRodDefinition
{
    public ControlRodDefinition(
        string id,
        string groupId,
        ControlRodTravelRate travelRate,
        Reactivity fullyInsertedReactivity,
        Reactivity fullyWithdrawnReactivity,
        ControlRodWorthCurveKind worthCurveKind = ControlRodWorthCurveKind.SmoothStep)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Control-rod id cannot be empty.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(groupId))
        {
            throw new ArgumentException("Control-rod group id cannot be empty.", nameof(groupId));
        }

        if (travelRate.FractionPerSecond <= 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(travelRate), travelRate, "Control-rod travel rate must be greater than zero.");
        }

        if (!Enum.IsDefined(typeof(ControlRodWorthCurveKind), worthCurveKind))
        {
            throw new ArgumentOutOfRangeException(nameof(worthCurveKind), worthCurveKind, "Unknown control-rod worth curve kind.");
        }

        Id = id.Trim();
        GroupId = groupId.Trim();
        TravelRate = travelRate;
        FullyInsertedReactivity = fullyInsertedReactivity;
        FullyWithdrawnReactivity = fullyWithdrawnReactivity;
        WorthCurveKind = worthCurveKind;
    }

    public string Id { get; }

    public string GroupId { get; }

    public ControlRodTravelRate TravelRate { get; }

    public Reactivity FullyInsertedReactivity { get; }

    public Reactivity FullyWithdrawnReactivity { get; }

    public ControlRodWorthCurveKind WorthCurveKind { get; }
}
