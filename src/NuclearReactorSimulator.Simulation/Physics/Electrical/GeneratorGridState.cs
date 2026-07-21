using System.Collections.ObjectModel;
using NuclearReactorSimulator.Domain.Physics.Electrical;
using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Simulation.Physics.Electrical;

/// <summary>
/// Canonical committed electrical state for the M4.5 generator/grid system.
/// Grid phase is explicit so synchronization and replay remain deterministic without wall-clock time.
/// </summary>
public sealed class GeneratorGridState
{
    public GeneratorGridState(
        GeneratorGridSystemDefinition definition,
        PhaseAngle gridPhaseAngle,
        IEnumerable<SynchronousGeneratorState> generators)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        ArgumentNullException.ThrowIfNull(generators);

        var canonical = generators
            .Select(item => item ?? throw new ArgumentException("Generator-state collections cannot contain null entries.", nameof(generators)))
            .OrderBy(static item => item.GeneratorId, StringComparer.Ordinal)
            .ToArray();

        var expected = definition.Generators.Select(static item => item.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray();
        var actual = canonical.Select(static item => item.GeneratorId).OrderBy(static id => id, StringComparer.Ordinal).ToArray();
        if (actual.Distinct(StringComparer.Ordinal).Count() != actual.Length
            || !expected.SequenceEqual(actual, StringComparer.Ordinal))
        {
            throw new ArgumentException(
                $"Generator/grid state must contain exactly one state per generator. Expected [{string.Join(", ", expected)}], actual [{string.Join(", ", actual)}].",
                nameof(generators));
        }

        GridPhaseAngle = gridPhaseAngle;
        Generators = new ReadOnlyCollection<SynchronousGeneratorState>(canonical);
    }

    public GeneratorGridSystemDefinition Definition { get; }

    public PhaseAngle GridPhaseAngle { get; }

    public IReadOnlyList<SynchronousGeneratorState> Generators { get; }

    public SynchronousGeneratorState GetGenerator(string generatorId)
        => Generators.FirstOrDefault(item => string.Equals(item.GeneratorId, generatorId, StringComparison.Ordinal))
            ?? throw new KeyNotFoundException($"Unknown synchronous generator state '{generatorId}'.");

    public static GeneratorGridState CreateDisconnected(
        GeneratorGridSystemDefinition definition,
        PhaseAngle initialGridPhaseAngle,
        PhaseAngle initialGeneratorPhaseAngle)
    {
        ArgumentNullException.ThrowIfNull(definition);
        return new GeneratorGridState(
            definition,
            initialGridPhaseAngle,
            definition.Generators.Select(generator => new SynchronousGeneratorState(generator.Id, initialGeneratorPhaseAngle, breakerClosed: false)));
    }
}
