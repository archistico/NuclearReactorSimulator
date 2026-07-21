using System.Collections.ObjectModel;
using NuclearReactorSimulator.Domain.Physics.TurbineIsland.Condenser;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Turbine;

namespace NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Condenser;

/// <summary>
/// Complete M4.3 condenser inputs over the validated M4.2 turbine-expansion inputs.
/// </summary>
public sealed class CondenserSystemInputs
{
    public CondenserSystemInputs(
        CondenserSystemDefinition definition,
        TurbineExpansionInputs turbineExpansionInputs,
        IEnumerable<CondenserCoolingBoundaryInput> coolingBoundaryInputs)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        TurbineExpansionInputs = turbineExpansionInputs ?? throw new ArgumentNullException(nameof(turbineExpansionInputs));
        ArgumentNullException.ThrowIfNull(coolingBoundaryInputs);

        if (!ReferenceEquals(turbineExpansionInputs.Definition, definition.TurbineExpansionSystem))
        {
            throw new ArgumentException(
                "Turbine-expansion inputs do not use the condenser system's canonical M4.2 definition.",
                nameof(turbineExpansionInputs));
        }

        var canonical = coolingBoundaryInputs
            .Select(item => item ?? throw new ArgumentException("Cooling-boundary input collections cannot contain null entries.", nameof(coolingBoundaryInputs)))
            .OrderBy(static item => item.BoundaryId, StringComparer.Ordinal)
            .ToArray();

        if (canonical.Select(static item => item.BoundaryId).Distinct(StringComparer.Ordinal).Count() != canonical.Length)
        {
            throw new ArgumentException("Cooling-boundary input ids must be unique.", nameof(coolingBoundaryInputs));
        }

        var expected = definition.CoolingBoundaries.Select(static item => item.Id).OrderBy(static item => item, StringComparer.Ordinal).ToArray();
        var actual = canonical.Select(static item => item.BoundaryId).OrderBy(static item => item, StringComparer.Ordinal).ToArray();
        if (!expected.SequenceEqual(actual, StringComparer.Ordinal))
        {
            throw new ArgumentException(
                $"Condenser inputs must contain exactly one input per cooling boundary. Expected [{string.Join(", ", expected)}], actual [{string.Join(", ", actual)}].",
                nameof(coolingBoundaryInputs));
        }

        CoolingBoundaryInputs = new ReadOnlyCollection<CondenserCoolingBoundaryInput>(canonical);
    }

    public CondenserSystemDefinition Definition { get; }

    public TurbineExpansionInputs TurbineExpansionInputs { get; }

    public IReadOnlyList<CondenserCoolingBoundaryInput> CoolingBoundaryInputs { get; }

    public CondenserCoolingBoundaryInput GetCoolingBoundaryInput(string id)
        => CoolingBoundaryInputs.FirstOrDefault(item => string.Equals(item.BoundaryId, id, StringComparison.Ordinal))
            ?? throw new KeyNotFoundException($"Unknown condenser cooling-boundary input '{id}'.");
}
