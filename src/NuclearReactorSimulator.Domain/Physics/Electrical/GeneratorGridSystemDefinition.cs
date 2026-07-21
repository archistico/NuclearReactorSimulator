using System.Collections.ObjectModel;
using NuclearReactorSimulator.Domain.Physics.TurbineIsland.Feedwater;
using NuclearReactorSimulator.Domain.Physics.TurbineIsland.Turbine;

namespace NuclearReactorSimulator.Domain.Physics.Electrical;

/// <summary>
/// Canonical M4.5 generator/grid composition over the validated M4.4 secondary-cycle topology.
/// </summary>
public sealed class GeneratorGridSystemDefinition
{
    public GeneratorGridSystemDefinition(
        string id,
        CondensateFeedwaterSystemDefinition condensateFeedwaterSystem,
        ElectricalGridDefinition grid,
        IEnumerable<SynchronousGeneratorDefinition> generators)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Generator/grid system id cannot be empty or whitespace.", nameof(id));
        }

        CondensateFeedwaterSystem = condensateFeedwaterSystem ?? throw new ArgumentNullException(nameof(condensateFeedwaterSystem));
        Grid = grid ?? throw new ArgumentNullException(nameof(grid));
        ArgumentNullException.ThrowIfNull(generators);

        var canonical = generators
            .Select(item => item ?? throw new ArgumentException("Generator definitions cannot contain null entries.", nameof(generators)))
            .OrderBy(static item => item.Id, StringComparer.Ordinal)
            .ToArray();

        if (canonical.Length == 0)
        {
            throw new ArgumentException("A generator/grid system must contain at least one generator.", nameof(generators));
        }

        if (canonical.Select(static item => item.Id).Distinct(StringComparer.Ordinal).Count() != canonical.Length)
        {
            throw new ArgumentException("Generator ids must be unique.", nameof(generators));
        }

        if (canonical.Select(static item => item.BreakerId).Distinct(StringComparer.Ordinal).Count() != canonical.Length)
        {
            throw new ArgumentException("Generator breaker ids must be unique.", nameof(generators));
        }

        var expectedRotorIds = TurbineExpansionSystem.Rotors.Select(static item => item.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray();
        var actualRotorIds = canonical.Select(static item => item.RotorId).OrderBy(static id => id, StringComparer.Ordinal).ToArray();
        if (actualRotorIds.Distinct(StringComparer.Ordinal).Count() != actualRotorIds.Length
            || !expectedRotorIds.SequenceEqual(actualRotorIds, StringComparer.Ordinal))
        {
            throw new ArgumentException(
                $"M4.5 requires exactly one synchronous generator per turbine rotor. Expected [{string.Join(", ", expectedRotorIds)}], actual [{string.Join(", ", actualRotorIds)}].",
                nameof(generators));
        }

        Id = id.Trim();
        Generators = new ReadOnlyCollection<SynchronousGeneratorDefinition>(canonical);
    }

    public string Id { get; }

    public CondensateFeedwaterSystemDefinition CondensateFeedwaterSystem { get; }

    public TurbineExpansionSystemDefinition TurbineExpansionSystem
        => CondensateFeedwaterSystem.CondenserSystem.TurbineExpansionSystem;

    public ElectricalGridDefinition Grid { get; }

    public IReadOnlyList<SynchronousGeneratorDefinition> Generators { get; }

    public SynchronousGeneratorDefinition GetGenerator(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Generator id cannot be empty or whitespace.", nameof(id));
        }

        return Generators.FirstOrDefault(item => string.Equals(item.Id, id, StringComparison.Ordinal))
            ?? throw new KeyNotFoundException($"Unknown synchronous generator '{id}'.");
    }

    public SynchronousGeneratorDefinition GetGeneratorForRotor(string rotorId)
    {
        if (string.IsNullOrWhiteSpace(rotorId))
        {
            throw new ArgumentException("Rotor id cannot be empty or whitespace.", nameof(rotorId));
        }

        return Generators.FirstOrDefault(item => string.Equals(item.RotorId, rotorId, StringComparison.Ordinal))
            ?? throw new KeyNotFoundException($"No synchronous generator is assigned to turbine rotor '{rotorId}'.");
    }
}
