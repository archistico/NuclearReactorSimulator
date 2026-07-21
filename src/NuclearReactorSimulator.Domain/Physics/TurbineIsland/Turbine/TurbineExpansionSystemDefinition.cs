using System.Collections.ObjectModel;
using NuclearReactorSimulator.Domain.Physics.TurbineIsland.MainSteam;
using NuclearReactorSimulator.Domain.Plant;

namespace NuclearReactorSimulator.Domain.Physics.TurbineIsland.Turbine;

/// <summary>
/// Canonical M4.2 composition of M4.1 admission seams, lumped expansion groups and mechanical rotors.
/// </summary>
public sealed class TurbineExpansionSystemDefinition
{
    public TurbineExpansionSystemDefinition(
        string id,
        MainSteamNetworkDefinition mainSteamNetwork,
        IEnumerable<TurbineRotorDefinition> rotors,
        IEnumerable<TurbineStageGroupDefinition> stageGroups)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Turbine expansion-system id cannot be empty or whitespace.", nameof(id));
        }

        MainSteamNetwork = mainSteamNetwork ?? throw new ArgumentNullException(nameof(mainSteamNetwork));
        ArgumentNullException.ThrowIfNull(rotors);
        ArgumentNullException.ThrowIfNull(stageGroups);

        var canonicalRotors = Canonicalize(rotors, static item => item.Id, nameof(rotors));
        var canonicalStageGroups = Canonicalize(stageGroups, static item => item.Id, nameof(stageGroups));

        if (canonicalRotors.Length == 0)
        {
            throw new ArgumentException("A turbine expansion system must contain at least one rotor.", nameof(rotors));
        }

        if (canonicalStageGroups.Length == 0)
        {
            throw new ArgumentException("A turbine expansion system must contain at least one stage group.", nameof(stageGroups));
        }

        ValidateTopology(mainSteamNetwork, canonicalRotors, canonicalStageGroups);

        Id = id.Trim();
        Rotors = new ReadOnlyCollection<TurbineRotorDefinition>(canonicalRotors);
        StageGroups = new ReadOnlyCollection<TurbineStageGroupDefinition>(canonicalStageGroups);
    }

    public string Id { get; }

    public MainSteamNetworkDefinition MainSteamNetwork { get; }

    public PlantDefinition PlantDefinition => MainSteamNetwork.PlantDefinition;

    public IReadOnlyList<TurbineRotorDefinition> Rotors { get; }

    public IReadOnlyList<TurbineStageGroupDefinition> StageGroups { get; }

    public TurbineRotorDefinition GetRotor(string id)
        => GetById(Rotors, id, static item => item.Id, "turbine rotor");

    public TurbineStageGroupDefinition GetStageGroup(string id)
        => GetById(StageGroups, id, static item => item.Id, "turbine stage group");

    private static void ValidateTopology(
        MainSteamNetworkDefinition mainSteamNetwork,
        IReadOnlyList<TurbineRotorDefinition> rotors,
        IReadOnlyList<TurbineStageGroupDefinition> stageGroups)
    {
        var rotorIds = rotors.Select(static item => item.Id).ToHashSet(StringComparer.Ordinal);
        var usedRotorIds = new HashSet<string>(StringComparer.Ordinal);
        var assignedAdmissionBoundaryIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var stageGroup in stageGroups)
        {
            var admissionBoundary = mainSteamNetwork.GetTurbineAdmissionBoundary(stageGroup.AdmissionBoundaryId);
            _ = mainSteamNetwork.PlantDefinition.GetFluidNode(stageGroup.ExhaustNodeId);

            if (!rotorIds.Contains(stageGroup.RotorId))
            {
                throw new ArgumentException(
                    $"Turbine stage group '{stageGroup.Id}' references unknown rotor '{stageGroup.RotorId}'.",
                    nameof(stageGroups));
            }

            if (!assignedAdmissionBoundaryIds.Add(stageGroup.AdmissionBoundaryId))
            {
                throw new ArgumentException(
                    $"Turbine-admission boundary '{stageGroup.AdmissionBoundaryId}' is assigned to more than one stage group.",
                    nameof(stageGroups));
            }

            if (string.Equals(admissionBoundary.SourceNodeId, stageGroup.ExhaustNodeId, StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    $"Turbine stage group '{stageGroup.Id}' exhaust node must differ from admission node '{admissionBoundary.SourceNodeId}'.",
                    nameof(stageGroups));
            }

            usedRotorIds.Add(stageGroup.RotorId);
        }

        var expectedAdmissionBoundaryIds = mainSteamNetwork.TurbineAdmissionBoundaries
            .Select(static item => item.Id)
            .ToHashSet(StringComparer.Ordinal);
        if (!expectedAdmissionBoundaryIds.SetEquals(assignedAdmissionBoundaryIds))
        {
            var missing = expectedAdmissionBoundaryIds
                .Except(assignedAdmissionBoundaryIds, StringComparer.Ordinal)
                .OrderBy(static item => item, StringComparer.Ordinal);
            throw new ArgumentException(
                $"Every M4.1 turbine-admission boundary must feed exactly one M4.2 stage group. Missing: {string.Join(", ", missing)}.",
                nameof(stageGroups));
        }

        if (!rotorIds.SetEquals(usedRotorIds))
        {
            var unused = rotorIds.Except(usedRotorIds, StringComparer.Ordinal).OrderBy(static item => item, StringComparer.Ordinal);
            throw new ArgumentException(
                $"Every turbine rotor must be driven by at least one stage group. Unused: {string.Join(", ", unused)}.",
                nameof(stageGroups));
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
