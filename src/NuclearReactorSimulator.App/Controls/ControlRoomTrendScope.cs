using Avalonia;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.ControlRoom.Hmi;

namespace NuclearReactorSimulator.App.Controls;

/// <summary>
/// Inherited logical-step context used by presentation controls to derive deterministic recent-sample trends.
/// </summary>
public sealed class ControlRoomTrendScope : AvaloniaObject
{
    public static readonly AttachedProperty<long> LogicalStepProperty =
        AvaloniaProperty.RegisterAttached<ControlRoomTrendScope, AvaloniaObject, long>(
            "LogicalStep",
            defaultValue: -1,
            inherits: true);

    public static void SetLogicalStep(AvaloniaObject element, long value)
        => element.SetValue(LogicalStepProperty, value);

    public static long GetLogicalStep(AvaloniaObject element)
        => element.GetValue(LogicalStepProperty);
}

internal sealed class ControlRoomAutomaticTrendTracker
{
    private long? _lastLogicalStep;
    private ControlRoomValueSnapshot? _lastSnapshot;

    public ControlRoomInstrumentTrendSnapshot Current { get; private set; } =
        ControlRoomInstrumentTrendSnapshot.Unavailable;

    public bool HasBaseline => _lastLogicalStep.HasValue && _lastSnapshot is not null;

    public void Observe(long logicalStep, ControlRoomValueSnapshot? snapshot)
    {
        if (logicalStep < 0 || snapshot is null)
        {
            Current = ControlRoomInstrumentTrendSnapshot.Unavailable;
            return;
        }

        if (!_lastLogicalStep.HasValue || _lastSnapshot is null || logicalStep <= _lastLogicalStep.Value)
        {
            if (!_lastLogicalStep.HasValue || logicalStep < _lastLogicalStep.Value)
            {
                _lastLogicalStep = logicalStep;
                _lastSnapshot = snapshot;
                Current = ControlRoomInstrumentTrendSnapshot.Unavailable;
            }

            return;
        }

        var scale = snapshot.InstrumentScale ?? _lastSnapshot.InstrumentScale;
        var span = scale is null ? (double?)null : scale.Maximum - scale.Minimum;
        var referenceMagnitude = Math.Max(
            1d,
            Math.Max(
                Math.Abs(_lastSnapshot.NumericValue.GetValueOrDefault()),
                Math.Abs(snapshot.NumericValue.GetValueOrDefault())));
        var steadyTolerance = span.HasValue
            ? span.Value * 0.000001d
            : referenceMagnitude * 0.000001d;
        var rapidTolerance = span.HasValue
            ? span.Value * 0.001d
            : (double?)null;

        Current = ControlRoomInstrumentTrendSnapshot.Between(
            _lastLogicalStep.Value,
            _lastSnapshot.NumericValue,
            logicalStep,
            snapshot.NumericValue,
            steadyTolerance,
            snapshot.Unit,
            rapidTolerance);
        _lastLogicalStep = logicalStep;
        _lastSnapshot = snapshot;
    }
}
