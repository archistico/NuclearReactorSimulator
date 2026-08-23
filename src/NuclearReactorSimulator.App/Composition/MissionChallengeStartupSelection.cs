using NuclearReactorSimulator.Application.Scenarios.Challenges.Packs;

namespace NuclearReactorSimulator.App.Composition;

/// <summary>
/// M10.9.7.3 explicit startup binding for manual/live MISSION validation. It accepts only an exact authored pack id and
/// never infers a mission from scenario identity. The normal desktop startup remains mission-unbound.
/// </summary>
internal static class MissionChallengeStartupSelection
{
    private const string Prefix = "--mission-pack=";

    public static bool IsSelectionArgument(string arg)
    {
        ArgumentNullException.ThrowIfNull(arg);
        return arg.StartsWith(Prefix, StringComparison.Ordinal);
    }

    public static OperationalChallengePackDefinition? Resolve(IEnumerable<string> args)
    {
        ArgumentNullException.ThrowIfNull(args);
        var selections = args
            .Where(IsSelectionArgument)
            .ToArray();
        if (selections.Length == 0)
        {
            return null;
        }
        if (selections.Length != 1)
        {
            throw new ArgumentException("Specify at most one --mission-pack=<exact-pack-id> startup option.", nameof(args));
        }

        var exactId = selections[0][Prefix.Length..];
        if (string.IsNullOrWhiteSpace(exactId))
        {
            throw new ArgumentException("The --mission-pack option requires an exact pack id such as bounded-demand-following-5-10-5@3.", nameof(args));
        }

        return ResolveExactId(exactId);
    }

    public static OperationalChallengePackDefinition ResolveExactId(string exactId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(exactId);
        var normalized = exactId.Trim();
        return InitialOperationalChallengePack.All
            .Concat(ProductionOperationalChallengePack.All)
            .SingleOrDefault(pack => string.Equals(pack.ExactId, normalized, StringComparison.Ordinal))
            ?? throw new ArgumentException($"Unknown operational challenge pack '{exactId}'.", nameof(exactId));
    }
}
