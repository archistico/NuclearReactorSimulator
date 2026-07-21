using System.Collections.ObjectModel;
using NuclearReactorSimulator.Domain.Physics.TurbineIsland.Turbine;
using NuclearReactorSimulator.Domain.Plant;

namespace NuclearReactorSimulator.Domain.Physics.TurbineIsland.Condenser;

/// <summary>
/// Canonical M4.3 composition of turbine exhaust seams, lumped condensers, hotwells and replaceable cooling boundaries.
/// </summary>
public sealed class CondenserSystemDefinition
{
    public CondenserSystemDefinition(
        string id,
        TurbineExpansionSystemDefinition turbineExpansionSystem,
        IEnumerable<CondenserDefinition> condensers,
        IEnumerable<CondenserCoolingBoundaryDefinition> coolingBoundaries)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Condenser-system id cannot be empty or whitespace.", nameof(id));
        }

        TurbineExpansionSystem = turbineExpansionSystem ?? throw new ArgumentNullException(nameof(turbineExpansionSystem));
        ArgumentNullException.ThrowIfNull(condensers);
        ArgumentNullException.ThrowIfNull(coolingBoundaries);

        var canonicalCondensers = Canonicalize(condensers, static item => item.Id, nameof(condensers));
        var canonicalCoolingBoundaries = Canonicalize(coolingBoundaries, static item => item.Id, nameof(coolingBoundaries));

        if (canonicalCondensers.Length == 0)
        {
            throw new ArgumentException("A condenser system must contain at least one condenser.", nameof(condensers));
        }

        if (canonicalCoolingBoundaries.Length == 0)
        {
            throw new ArgumentException("A condenser system must contain at least one cooling boundary.", nameof(coolingBoundaries));
        }

        ValidateTopology(turbineExpansionSystem, canonicalCondensers, canonicalCoolingBoundaries);

        Id = id.Trim();
        Condensers = new ReadOnlyCollection<CondenserDefinition>(canonicalCondensers);
        CoolingBoundaries = new ReadOnlyCollection<CondenserCoolingBoundaryDefinition>(canonicalCoolingBoundaries);
    }

    public string Id { get; }

    public TurbineExpansionSystemDefinition TurbineExpansionSystem { get; }

    public PlantDefinition PlantDefinition => TurbineExpansionSystem.PlantDefinition;

    public IReadOnlyList<CondenserDefinition> Condensers { get; }

    public IReadOnlyList<CondenserCoolingBoundaryDefinition> CoolingBoundaries { get; }

    public CondenserDefinition GetCondenser(string id)
        => GetById(Condensers, id, static item => item.Id, "condenser");

    public CondenserCoolingBoundaryDefinition GetCoolingBoundary(string id)
        => GetById(CoolingBoundaries, id, static item => item.Id, "condenser cooling boundary");

    private static void ValidateTopology(
        TurbineExpansionSystemDefinition turbineExpansionSystem,
        IReadOnlyList<CondenserDefinition> condensers,
        IReadOnlyList<CondenserCoolingBoundaryDefinition> coolingBoundaries)
    {
        var plant = turbineExpansionSystem.PlantDefinition;
        var assignedStageIds = new HashSet<string>(StringComparer.Ordinal);
        var assignedCoolingBoundaryIds = new HashSet<string>(StringComparer.Ordinal);
        var condenserIds = condensers.Select(static item => item.Id).ToHashSet(StringComparer.Ordinal);

        foreach (var condenser in condensers)
        {
            var stage = turbineExpansionSystem.GetStageGroup(condenser.TurbineStageGroupId);
            _ = plant.GetFluidNode(condenser.SteamSpaceNodeId);
            _ = plant.GetFluidNode(condenser.HotwellNodeId);

            if (!string.Equals(stage.ExhaustNodeId, condenser.SteamSpaceNodeId, StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    $"Condenser '{condenser.Id}' steam-space node '{condenser.SteamSpaceNodeId}' must match turbine stage-group '{stage.Id}' exhaust node '{stage.ExhaustNodeId}'.",
                    nameof(condensers));
            }

            if (!assignedStageIds.Add(stage.Id))
            {
                throw new ArgumentException(
                    $"Turbine stage group '{stage.Id}' is assigned to more than one condenser.",
                    nameof(condensers));
            }

            if (!assignedCoolingBoundaryIds.Add(condenser.CoolingBoundaryId))
            {
                throw new ArgumentException(
                    $"Cooling boundary '{condenser.CoolingBoundaryId}' is assigned to more than one condenser.",
                    nameof(condensers));
            }
        }

        var expectedStageIds = turbineExpansionSystem.StageGroups
            .Select(static item => item.Id)
            .ToHashSet(StringComparer.Ordinal);
        if (!expectedStageIds.SetEquals(assignedStageIds))
        {
            var missing = expectedStageIds
                .Except(assignedStageIds, StringComparer.Ordinal)
                .OrderBy(static item => item, StringComparer.Ordinal);
            throw new ArgumentException(
                $"Every M4.2 turbine stage group must discharge to exactly one M4.3 condenser. Missing: {string.Join(", ", missing)}.",
                nameof(condensers));
        }

        var coolingBoundaryIds = coolingBoundaries.Select(static item => item.Id).ToHashSet(StringComparer.Ordinal);
        if (!coolingBoundaryIds.SetEquals(assignedCoolingBoundaryIds))
        {
            var missing = assignedCoolingBoundaryIds
                .Except(coolingBoundaryIds, StringComparer.Ordinal)
                .OrderBy(static item => item, StringComparer.Ordinal);
            var unused = coolingBoundaryIds
                .Except(assignedCoolingBoundaryIds, StringComparer.Ordinal)
                .OrderBy(static item => item, StringComparer.Ordinal);
            throw new ArgumentException(
                $"Condenser cooling-boundary coverage must be exact. Missing definitions: {string.Join(", ", missing)}. Unused definitions: {string.Join(", ", unused)}.",
                nameof(coolingBoundaries));
        }

        foreach (var boundary in coolingBoundaries)
        {
            if (!condenserIds.Contains(boundary.CondenserId))
            {
                throw new ArgumentException(
                    $"Cooling boundary '{boundary.Id}' references unknown condenser '{boundary.CondenserId}'.",
                    nameof(coolingBoundaries));
            }

            var condenser = condensers.First(item => string.Equals(item.Id, boundary.CondenserId, StringComparison.Ordinal));
            if (!string.Equals(condenser.CoolingBoundaryId, boundary.Id, StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    $"Cooling boundary '{boundary.Id}' and condenser '{condenser.Id}' must reference each other canonically.",
                    nameof(coolingBoundaries));
            }
        }
    }

    private static T[] Canonicalize<T>(IEnumerable<T> items, Func<T, string> idSelector, string parameterName)
        where T : class
    {
        var canonical = items
            .Select(item => item ?? throw new ArgumentException("Definition collections cannot contain null entries.", parameterName))
            .OrderBy(idSelector, StringComparer.Ordinal)
            .ToArray();

        if (canonical.Select(idSelector).Distinct(StringComparer.Ordinal).Count() != canonical.Length)
        {
            throw new ArgumentException("Definition ids must be unique within their collection.", parameterName);
        }

        return canonical;
    }

    private static T GetById<T>(IEnumerable<T> items, string id, Func<T, string> idSelector, string label)
        where T : class
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException($"{label} id cannot be empty or whitespace.", nameof(id));
        }

        return items.FirstOrDefault(item => string.Equals(idSelector(item), id, StringComparison.Ordinal))
            ?? throw new KeyNotFoundException($"Unknown {label} '{id}'.");
    }
}
