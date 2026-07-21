using System.Collections.ObjectModel;
using NuclearReactorSimulator.Domain.Physics.Electrical;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Feedwater;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Turbine;

namespace NuclearReactorSimulator.Simulation.Physics.Electrical;

/// <summary>
/// Complete M4.5 inputs over the validated M4.4 secondary-cycle inputs.
/// Legacy M4.2 manual external-load torque must be zero while the generator owns rotor loading.
/// </summary>
public sealed class GeneratorGridInputs
{
    public GeneratorGridInputs(
        GeneratorGridSystemDefinition definition,
        CondensateFeedwaterSystemInputs condensateFeedwaterInputs,
        IEnumerable<SynchronousGeneratorInput> generatorInputs)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        CondensateFeedwaterInputs = condensateFeedwaterInputs ?? throw new ArgumentNullException(nameof(condensateFeedwaterInputs));
        ArgumentNullException.ThrowIfNull(generatorInputs);

        if (!ReferenceEquals(condensateFeedwaterInputs.Definition, definition.CondensateFeedwaterSystem))
        {
            throw new ArgumentException("Condensate/feedwater inputs do not use the generator/grid system's canonical M4.4 definition.", nameof(condensateFeedwaterInputs));
        }

        foreach (var rotorInput in TurbineInputs.RotorInputs)
        {
            if (rotorInput.ExternalLoadTorque != Torque.Zero)
            {
                throw new ArgumentException(
                    $"M4.2 rotor '{rotorInput.RotorId}' external-load torque must be zero while M4.5 generator electromagnetic loading owns the rotor-load seam.",
                    nameof(condensateFeedwaterInputs));
            }
        }

        var canonical = generatorInputs
            .Select(item => item ?? throw new ArgumentException("Generator-input collections cannot contain null entries.", nameof(generatorInputs)))
            .OrderBy(static item => item.GeneratorId, StringComparer.Ordinal)
            .ToArray();
        var expected = definition.Generators.Select(static item => item.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray();
        var actual = canonical.Select(static item => item.GeneratorId).OrderBy(static id => id, StringComparer.Ordinal).ToArray();
        if (actual.Distinct(StringComparer.Ordinal).Count() != actual.Length
            || !expected.SequenceEqual(actual, StringComparer.Ordinal))
        {
            throw new ArgumentException(
                $"Generator/grid inputs must contain exactly one input per generator. Expected [{string.Join(", ", expected)}], actual [{string.Join(", ", actual)}].",
                nameof(generatorInputs));
        }

        foreach (var input in canonical)
        {
            var generator = definition.GetGenerator(input.GeneratorId);
            if (input.RequestedElectricalPower > generator.MaximumElectricalPower)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(generatorInputs),
                    input.RequestedElectricalPower,
                    $"Generator '{generator.Id}' requested electrical power exceeds its configured maximum.");
            }
        }

        GeneratorInputs = new ReadOnlyCollection<SynchronousGeneratorInput>(canonical);
    }

    public GeneratorGridSystemDefinition Definition { get; }

    public CondensateFeedwaterSystemInputs CondensateFeedwaterInputs { get; }

    public TurbineExpansionInputs TurbineInputs => CondensateFeedwaterInputs.CondenserInputs.TurbineExpansionInputs;

    public IReadOnlyList<SynchronousGeneratorInput> GeneratorInputs { get; }

    public SynchronousGeneratorInput GetGeneratorInput(string generatorId)
        => GeneratorInputs.FirstOrDefault(item => string.Equals(item.GeneratorId, generatorId, StringComparison.Ordinal))
            ?? throw new KeyNotFoundException($"Unknown synchronous generator input '{generatorId}'.");
}
