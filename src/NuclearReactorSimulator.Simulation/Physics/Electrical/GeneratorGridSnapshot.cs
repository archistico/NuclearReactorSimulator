using System.Collections.ObjectModel;
using NuclearReactorSimulator.Domain.Physics.Electrical;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Feedwater;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Turbine;

namespace NuclearReactorSimulator.Simulation.Physics.Electrical;

public sealed class GeneratorGridSnapshot
{
    public GeneratorGridSnapshot(
        GeneratorGridSystemDefinition definition,
        CondensateFeedwaterSystemSnapshot condensateFeedwater,
        ElectricalGridSnapshot grid,
        IEnumerable<SynchronousGeneratorSnapshot> generators,
        GeneratorElectricalAudit electricalAudit)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        CondensateFeedwater = condensateFeedwater ?? throw new ArgumentNullException(nameof(condensateFeedwater));
        Grid = grid ?? throw new ArgumentNullException(nameof(grid));
        ArgumentNullException.ThrowIfNull(generators);
        ElectricalAudit = electricalAudit ?? throw new ArgumentNullException(nameof(electricalAudit));

        if (!ReferenceEquals(condensateFeedwater.Definition, definition.CondensateFeedwaterSystem))
        {
            throw new ArgumentException("Condensate/feedwater snapshot does not use the generator/grid system's canonical M4.4 definition.", nameof(condensateFeedwater));
        }

        var canonical = generators.OrderBy(static item => item.GeneratorId, StringComparer.Ordinal).ToArray();
        var expected = definition.Generators.Select(static item => item.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray();
        var actual = canonical.Select(static item => item.GeneratorId).OrderBy(static id => id, StringComparer.Ordinal).ToArray();
        if (actual.Distinct(StringComparer.Ordinal).Count() != actual.Length
            || !expected.SequenceEqual(actual, StringComparer.Ordinal))
        {
            throw new ArgumentException(
                $"Generator/grid snapshot must contain exactly one snapshot per generator. Expected [{string.Join(", ", expected)}], actual [{string.Join(", ", actual)}].",
                nameof(generators));
        }

        Generators = new ReadOnlyCollection<SynchronousGeneratorSnapshot>(canonical);
    }

    public GeneratorGridSystemDefinition Definition { get; }

    public CondensateFeedwaterSystemSnapshot CondensateFeedwater { get; }

    public TurbineExpansionSnapshot TurbineExpansion => CondensateFeedwater.CondenserSnapshot.TurbineExpansion;

    public ElectricalGridSnapshot Grid { get; }

    public IReadOnlyList<SynchronousGeneratorSnapshot> Generators { get; }

    public GeneratorElectricalAudit ElectricalAudit { get; }

    public Power TotalElectricalOutputPower => ElectricalAudit.ElectricalExportPower;

    public Power TotalGeneratorLossPower => ElectricalAudit.ConversionLossPower;
}
