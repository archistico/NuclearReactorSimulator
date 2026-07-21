namespace NuclearReactorSimulator.Domain.Physics.Reactor.PrimaryCircuit.Boundaries;

/// <summary>
/// Semantic external steam-export sink boundary for one steam drum.
/// The source is an existing canonical steam-outlet fluid node; no duplicate inventory is introduced.
/// </summary>
public sealed record SteamExportBoundaryDefinition
{
    public SteamExportBoundaryDefinition(string id, string steamDrumId, string sourceNodeId)
    {
        Id = ValidateId(id, nameof(id), "Steam-export boundary");
        SteamDrumId = ValidateId(steamDrumId, nameof(steamDrumId), "Steam drum");
        SourceNodeId = ValidateId(sourceNodeId, nameof(sourceNodeId), "Steam-export source node");
    }

    public string Id { get; }

    public string SteamDrumId { get; }

    public string SourceNodeId { get; }

    private static string ValidateId(string value, string parameterName, string label)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{label} id cannot be empty or whitespace.", parameterName);
        }

        return value.Trim();
    }
}
