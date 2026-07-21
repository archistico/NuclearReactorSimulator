using System.Collections.ObjectModel;

namespace NuclearReactorSimulator.Domain.Physics.Reactor.ControlRods;

/// <summary>
/// Canonical immutable definition of all rods and command groups in one control-rod system.
/// </summary>
public sealed class ControlRodSystemDefinition
{
    public ControlRodSystemDefinition(
        IEnumerable<ControlRodDefinition> rods,
        IEnumerable<ControlRodGroupDefinition> groups)
    {
        ArgumentNullException.ThrowIfNull(rods);
        ArgumentNullException.ThrowIfNull(groups);

        var canonicalRods = rods
            .Select(static rod => rod ?? throw new ArgumentException("Control-rod definitions cannot contain null entries.", "rods"))
            .OrderBy(static rod => rod.Id, StringComparer.Ordinal)
            .ToArray();
        var canonicalGroups = groups
            .Select(static group => group ?? throw new ArgumentException("Control-rod group definitions cannot contain null entries.", "groups"))
            .OrderBy(static group => group.Id, StringComparer.Ordinal)
            .ToArray();

        if (canonicalRods.Length == 0)
        {
            throw new ArgumentException("A control-rod system must contain at least one rod.", "rods");
        }

        EnsureUniqueIds(canonicalRods.Select(static rod => rod.Id), "control-rod", "rods");
        EnsureUniqueIds(canonicalGroups.Select(static group => group.Id), "control-rod group", "groups");

        var rodsById = canonicalRods.ToDictionary(static rod => rod.Id, StringComparer.Ordinal);
        var groupsById = canonicalGroups.ToDictionary(static group => group.Id, StringComparer.Ordinal);

        foreach (var rod in canonicalRods)
        {
            if (!groupsById.TryGetValue(rod.GroupId, out var group))
            {
                throw new ArgumentException($"Control rod '{rod.Id}' references unknown group '{rod.GroupId}'.", "groups");
            }

            if (!group.RodIds.Contains(rod.Id, StringComparer.Ordinal))
            {
                throw new ArgumentException($"Control rod '{rod.Id}' is not listed in its declared group '{rod.GroupId}'.", "groups");
            }
        }

        foreach (var group in canonicalGroups)
        {
            foreach (var rodId in group.RodIds)
            {
                if (!rodsById.TryGetValue(rodId, out var rod))
                {
                    throw new ArgumentException($"Control-rod group '{group.Id}' references unknown rod '{rodId}'.", "groups");
                }

                if (!string.Equals(rod.GroupId, group.Id, StringComparison.Ordinal))
                {
                    throw new ArgumentException($"Control rod '{rod.Id}' belongs to '{rod.GroupId}', not group '{group.Id}'.", "groups");
                }
            }
        }

        Rods = new ReadOnlyCollection<ControlRodDefinition>(canonicalRods);
        Groups = new ReadOnlyCollection<ControlRodGroupDefinition>(canonicalGroups);
    }

    public IReadOnlyList<ControlRodDefinition> Rods { get; }

    public IReadOnlyList<ControlRodGroupDefinition> Groups { get; }

    public ControlRodDefinition GetRod(string id)
        => Rods.FirstOrDefault(rod => string.Equals(rod.Id, id, StringComparison.Ordinal))
            ?? throw new KeyNotFoundException($"Unknown control rod '{id}'.");

    public ControlRodGroupDefinition GetGroup(string id)
        => Groups.FirstOrDefault(group => string.Equals(group.Id, id, StringComparison.Ordinal))
            ?? throw new KeyNotFoundException($"Unknown control-rod group '{id}'.");

    private static void EnsureUniqueIds(IEnumerable<string> ids, string label, string parameterName)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var id in ids)
        {
            if (!seen.Add(id))
            {
                throw new ArgumentException($"Duplicate {label} id '{id}'.", parameterName);
            }
        }
    }
}
