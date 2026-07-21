using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Integration;

namespace NuclearReactorSimulator.Simulation.Physics.Instrumentation;

/// <summary>Deterministic scalar extractor from the immutable M4.7 true-state snapshot boundary.</summary>
public sealed class InstrumentSignalSource
{
    private readonly Func<FullPlantSnapshot, double> _read;

    public InstrumentSignalSource(
        string id,
        string engineeringUnitSymbol,
        Func<FullPlantSnapshot, double> read)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Signal-source id cannot be empty or whitespace.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(engineeringUnitSymbol))
        {
            throw new ArgumentException("Signal-source engineering-unit symbol cannot be empty or whitespace.", nameof(engineeringUnitSymbol));
        }

        Id = id.Trim();
        EngineeringUnitSymbol = engineeringUnitSymbol.Trim();
        _read = read ?? throw new ArgumentNullException(nameof(read));
    }

    public string Id { get; }

    public string EngineeringUnitSymbol { get; }

    public double Read(FullPlantSnapshot trueStateSnapshot)
    {
        ArgumentNullException.ThrowIfNull(trueStateSnapshot);
        var value = _read(trueStateSnapshot);
        if (!double.IsFinite(value))
        {
            throw new InvalidOperationException($"Signal source '{Id}' produced a non-finite value.");
        }

        return value;
    }
}
