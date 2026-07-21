using System.Collections.ObjectModel;
using NuclearReactorSimulator.Domain.Physics.Control.Protection;

namespace NuclearReactorSimulator.Simulation.Physics.Control.Integration;

/// <summary>
/// Explicit M5.7 verification phase. Different immutable input bundles represent reference hold, setpoint changes or plant
/// disturbances without introducing a general-purpose scenario scheduler into the control layer.
/// </summary>
public sealed class AutomaticOperationVerificationPhase
{
    public AutomaticOperationVerificationPhase(
        string id,
        int stepCount,
        IntegratedAutomaticOperationInputs inputs,
        IEnumerable<AutomaticOperationTrackingTarget>? trackingTargets = null,
        ProtectionAction expectedLatchedProtectionActions = ProtectionAction.None,
        ProtectionInterlockAction expectedActiveInterlocks = ProtectionInterlockAction.None)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Verification phase id cannot be empty or whitespace.", nameof(id));
        }
        if (stepCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(stepCount), stepCount, "Verification phase step count must be positive.");
        }

        Inputs = inputs ?? throw new ArgumentNullException(nameof(inputs));
        var canonicalTargets = (trackingTargets ?? Array.Empty<AutomaticOperationTrackingTarget>())
            .Select(target => target ?? throw new ArgumentException("Tracking targets cannot contain null entries.", nameof(trackingTargets)))
            .OrderBy(static target => target.ChannelId, StringComparer.Ordinal)
            .ToArray();
        if (canonicalTargets.Select(static target => target.ChannelId).Distinct(StringComparer.Ordinal).Count() != canonicalTargets.Length)
        {
            throw new ArgumentException("A verification phase cannot contain duplicate measured-channel tracking targets.", nameof(trackingTargets));
        }

        foreach (var target in canonicalTargets)
        {
            _ = inputs.InstrumentationInputs.Definition.GetChannel(target.ChannelId);
        }

        Id = id.Trim();
        StepCount = stepCount;
        TrackingTargets = new ReadOnlyCollection<AutomaticOperationTrackingTarget>(canonicalTargets);
        ExpectedLatchedProtectionActions = expectedLatchedProtectionActions;
        ExpectedActiveInterlocks = expectedActiveInterlocks;
    }

    public string Id { get; }
    public int StepCount { get; }
    public IntegratedAutomaticOperationInputs Inputs { get; }
    public IReadOnlyList<AutomaticOperationTrackingTarget> TrackingTargets { get; }
    public ProtectionAction ExpectedLatchedProtectionActions { get; }
    public ProtectionInterlockAction ExpectedActiveInterlocks { get; }
}
