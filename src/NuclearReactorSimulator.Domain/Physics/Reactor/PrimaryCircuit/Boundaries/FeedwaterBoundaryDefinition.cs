namespace NuclearReactorSimulator.Domain.Physics.Reactor.PrimaryCircuit.Boundaries;

/// <summary>
/// Semantic external feedwater source boundary for one steam drum.
/// The target is an existing canonical plant fluid node; no duplicate inventory is introduced.
/// </summary>
public sealed record FeedwaterBoundaryDefinition
{
    public FeedwaterBoundaryDefinition(string id, string steamDrumId, string targetNodeId)
    {
        Id = ValidateId(id, nameof(id), "Feedwater boundary");
        SteamDrumId = ValidateId(steamDrumId, nameof(steamDrumId), "Steam drum");
        TargetNodeId = ValidateId(targetNodeId, nameof(targetNodeId), "Feedwater target node");
    }

    public string Id { get; }

    public string SteamDrumId { get; }

    public string TargetNodeId { get; }

    private static string ValidateId(string value, string parameterName, string label)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{label} id cannot be empty or whitespace.", parameterName);
        }

        return value.Trim();
    }
}
