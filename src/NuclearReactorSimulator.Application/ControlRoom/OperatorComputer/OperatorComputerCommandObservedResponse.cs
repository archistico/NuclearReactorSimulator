using System.Collections.ObjectModel;
using NuclearReactorSimulator.Application.ControlRoom;

namespace NuclearReactorSimulator.Application.ControlRoom.OperatorComputer;

public enum OperatorComputerCommandDispatchObservationStatus
{
    None = 0,
    Pending = 1,
    Accepted = 2,
    Rejected = 3,
}

public enum OperatorComputerObservedResponseDirection
{
    Unavailable = 0,
    Unchanged = 1,
    Increased = 2,
    Decreased = 3,
    Changed = 4,
    BecameTrue = 5,
    BecameFalse = 6,
}

public sealed record OperatorComputerCommandObservedMonitorDelta(
    OperatorComputerCommandObservationSample Baseline,
    OperatorComputerCommandObservationSample Latest,
    OperatorComputerObservedResponseDirection Direction,
    double? NumericDelta);

public sealed record OperatorComputerCommandObservedResponseSnapshot(
    OperatorComputerCommandDispatchObservationStatus Status,
    string? EntryId,
    string? DisplayName,
    string? TargetText,
    ControlRoomCommand? Command,
    long DispatchLogicalStep,
    long LatestLogicalStep,
    int ObservationWindowSteps,
    bool WindowComplete,
    string Feedback,
    bool ProtectionActiveAtDispatch,
    bool ProtectionActiveLatest,
    IReadOnlyList<OperatorComputerCommandObservedMonitorDelta> MonitorDeltas)
{
    public static OperatorComputerCommandObservedResponseSnapshot Empty { get; } = new(
        OperatorComputerCommandDispatchObservationStatus.None,
        null,
        null,
        null,
        null,
        0,
        0,
        0,
        false,
        "NO POST-DISPATCH EVIDENCE",
        false,
        false,
        Array.Empty<OperatorComputerCommandObservedMonitorDelta>());

    public bool HasEvidence => Status != OperatorComputerCommandDispatchObservationStatus.None;

    public long ObservedAgeSteps => Math.Max(0, LatestLogicalStep - DispatchLogicalStep);
}

/// <summary>
/// M10.9.5.4 presentation-only accumulator for one bounded post-dispatch observation window. The accumulator compares
/// authored monitor values at the dispatch boundary with later UI-safe snapshots. It does not claim that the command
/// caused a change and it never converts a numeric delta into generic success/failure.
/// </summary>
public sealed class OperatorComputerCommandObservedResponseAccumulator
{
    public const int DefaultObservationWindowSteps = 500;

    private readonly int _observationWindowSteps;
    private IReadOnlyList<OperatorComputerCommandObservationSample> _baselineSamples = Array.Empty<OperatorComputerCommandObservationSample>();
    private string? _entryId;

    public OperatorComputerCommandObservedResponseAccumulator(int observationWindowSteps = DefaultObservationWindowSteps)
    {
        if (observationWindowSteps <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(observationWindowSteps));
        }

        _observationWindowSteps = observationWindowSteps;
    }

    public OperatorComputerCommandObservedResponseSnapshot Current { get; private set; } = OperatorComputerCommandObservedResponseSnapshot.Empty;

    public void BeginAttempt(
        OperatorComputerCommandSnapshot command,
        OperatorComputerRuntimeStatusSnapshot runtimeStatus)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(runtimeStatus);

        _entryId = command.EntryId;
        _baselineSamples = command.ObservationSamples.ToArray();
        Current = new OperatorComputerCommandObservedResponseSnapshot(
            OperatorComputerCommandDispatchObservationStatus.Pending,
            command.EntryId,
            command.DisplayName,
            command.TargetText,
            command.Command,
            runtimeStatus.LogicalStep,
            runtimeStatus.LogicalStep,
            _observationWindowSteps,
            false,
            "DISPATCH PENDING",
            runtimeStatus.AnyTripActive,
            runtimeStatus.AnyTripActive,
            BuildDeltas(_baselineSamples, _baselineSamples));
    }

    public void MarkAccepted(string feedback)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(feedback);
        if (Current.Status != OperatorComputerCommandDispatchObservationStatus.Pending)
        {
            throw new InvalidOperationException("No pending observed-response dispatch attempt is available to accept.");
        }

        Current = Current with
        {
            Status = OperatorComputerCommandDispatchObservationStatus.Accepted,
            Feedback = feedback,
        };
    }

    public void MarkRejected(string feedback)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(feedback);
        if (Current.Status != OperatorComputerCommandDispatchObservationStatus.Pending)
        {
            throw new InvalidOperationException("No pending observed-response dispatch attempt is available to reject.");
        }

        Current = Current with
        {
            Status = OperatorComputerCommandDispatchObservationStatus.Rejected,
            Feedback = feedback,
            WindowComplete = true,
            MonitorDeltas = Array.Empty<OperatorComputerCommandObservedMonitorDelta>(),
        };
        _baselineSamples = Array.Empty<OperatorComputerCommandObservationSample>();
    }

    public void RecordRejected(
        OperatorComputerCommandSnapshot command,
        OperatorComputerRuntimeStatusSnapshot runtimeStatus,
        string feedback)
    {
        BeginAttempt(command, runtimeStatus);
        MarkRejected(feedback);
    }

    public void Observe(OperatorComputerSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        if (Current.Status is not (OperatorComputerCommandDispatchObservationStatus.Pending or OperatorComputerCommandDispatchObservationStatus.Accepted)
            || Current.WindowComplete
            || snapshot.RuntimeStatus.LogicalStep < Current.DispatchLogicalStep)
        {
            return;
        }

        var matching = snapshot.Commands?.Commands.FirstOrDefault(command =>
            string.Equals(command.EntryId, _entryId, StringComparison.Ordinal));
        var latestSamples = matching?.ObservationSamples ?? Array.Empty<OperatorComputerCommandObservationSample>();
        var age = snapshot.RuntimeStatus.LogicalStep - Current.DispatchLogicalStep;

        Current = Current with
        {
            LatestLogicalStep = snapshot.RuntimeStatus.LogicalStep,
            WindowComplete = age >= _observationWindowSteps,
            ProtectionActiveLatest = snapshot.RuntimeStatus.AnyTripActive,
            MonitorDeltas = BuildDeltas(_baselineSamples, latestSamples),
        };
    }

    private static IReadOnlyList<OperatorComputerCommandObservedMonitorDelta> BuildDeltas(
        IReadOnlyList<OperatorComputerCommandObservationSample> baseline,
        IReadOnlyList<OperatorComputerCommandObservationSample> latest)
    {
        var deltas = new List<OperatorComputerCommandObservedMonitorDelta>(baseline.Count);
        foreach (var first in baseline)
        {
            var last = latest.FirstOrDefault(item =>
                string.Equals(item.Target.Id, first.Target.Id, StringComparison.Ordinal)
                && string.Equals(item.Target.Label, first.Target.Label, StringComparison.Ordinal))
                ?? UnavailableLike(first);
            var direction = Direction(first, last, out var numericDelta);
            deltas.Add(new OperatorComputerCommandObservedMonitorDelta(first, last, direction, numericDelta));
        }

        return new ReadOnlyCollection<OperatorComputerCommandObservedMonitorDelta>(deltas);
    }

    private static OperatorComputerObservedResponseDirection Direction(
        OperatorComputerCommandObservationSample baseline,
        OperatorComputerCommandObservationSample latest,
        out double? numericDelta)
    {
        numericDelta = null;
        if (!baseline.IsAvailable || !latest.IsAvailable)
        {
            return OperatorComputerObservedResponseDirection.Unavailable;
        }

        if (baseline.NumericValue is { } firstNumber && latest.NumericValue is { } lastNumber)
        {
            numericDelta = lastNumber - firstNumber;
            return numericDelta.Value > 0d
                ? OperatorComputerObservedResponseDirection.Increased
                : numericDelta.Value < 0d
                    ? OperatorComputerObservedResponseDirection.Decreased
                    : OperatorComputerObservedResponseDirection.Unchanged;
        }

        if (baseline.BooleanValue is { } firstBool && latest.BooleanValue is { } lastBool)
        {
            return firstBool == lastBool
                ? OperatorComputerObservedResponseDirection.Unchanged
                : lastBool
                    ? OperatorComputerObservedResponseDirection.BecameTrue
                    : OperatorComputerObservedResponseDirection.BecameFalse;
        }

        return string.Equals(baseline.ValueText, latest.ValueText, StringComparison.Ordinal)
            ? OperatorComputerObservedResponseDirection.Unchanged
            : OperatorComputerObservedResponseDirection.Changed;
    }

    private static OperatorComputerCommandObservationSample UnavailableLike(OperatorComputerCommandObservationSample sample)
        => sample with
        {
            ValueKind = OperatorComputerCommandObservationValueKind.Unavailable,
            ValueText = "—",
            NumericValue = null,
            BooleanValue = null,
            IsAvailable = false,
        };
}
