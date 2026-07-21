namespace NuclearReactorSimulator.Domain.Physics.Instrumentation;

/// <summary>Canonical definition of one scalar instrument channel.</summary>
public sealed class InstrumentChannelDefinition
{
    public InstrumentChannelDefinition(
        string id,
        string sourceId,
        string engineeringUnitSymbol,
        SignalRange measurementRange,
        LinearSignalScale outputScale,
        TimeSpan lagTimeConstant,
        bool clampToMeasurementRange = true)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Instrument-channel id cannot be empty or whitespace.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(sourceId))
        {
            throw new ArgumentException("Instrument source id cannot be empty or whitespace.", nameof(sourceId));
        }

        if (string.IsNullOrWhiteSpace(engineeringUnitSymbol))
        {
            throw new ArgumentException("Engineering-unit symbol cannot be empty or whitespace.", nameof(engineeringUnitSymbol));
        }

        if (!double.IsFinite(measurementRange.Minimum)
            || !double.IsFinite(measurementRange.Maximum)
            || measurementRange.Maximum <= measurementRange.Minimum)
        {
            throw new ArgumentOutOfRangeException(nameof(measurementRange), measurementRange, "Measurement range must be a valid finite ordered range.");
        }

        if (!double.IsFinite(outputScale.OutputMinimum)
            || !double.IsFinite(outputScale.OutputMaximum)
            || outputScale.OutputMaximum <= outputScale.OutputMinimum)
        {
            throw new ArgumentOutOfRangeException(nameof(outputScale), outputScale, "Output scale must be a valid finite ordered range.");
        }

        if (lagTimeConstant < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(lagTimeConstant), lagTimeConstant, "Lag time constant cannot be negative.");
        }

        Id = id.Trim();
        SourceId = sourceId.Trim();
        EngineeringUnitSymbol = engineeringUnitSymbol.Trim();
        MeasurementRange = measurementRange;
        OutputScale = outputScale;
        LagTimeConstant = lagTimeConstant;
        ClampToMeasurementRange = clampToMeasurementRange;
    }

    public string Id { get; }

    public string SourceId { get; }

    public string EngineeringUnitSymbol { get; }

    public SignalRange MeasurementRange { get; }

    public LinearSignalScale OutputScale { get; }

    public TimeSpan LagTimeConstant { get; }

    public bool ClampToMeasurementRange { get; }
}
