namespace NuclearReactorSimulator.Domain.Physics.TurbineIsland.MainSteam;

/// <summary>
/// Semantic main-steam line that connects one M3 steam-export seam to a canonical steam header through an existing plant pipe.
/// </summary>
public sealed record MainSteamLineDefinition
{
    public MainSteamLineDefinition(
        string id,
        string steamExportBoundaryId,
        string pipeId,
        string headerNodeId)
    {
        Id = ValidateId(id, nameof(id), "Main-steam line");
        SteamExportBoundaryId = ValidateId(steamExportBoundaryId, nameof(steamExportBoundaryId), "Steam-export boundary");
        PipeId = ValidateId(pipeId, nameof(pipeId), "Main-steam pipe");
        HeaderNodeId = ValidateId(headerNodeId, nameof(headerNodeId), "Main-steam header node");
    }

    public string Id { get; }

    public string SteamExportBoundaryId { get; }

    public string PipeId { get; }

    public string HeaderNodeId { get; }

    private static string ValidateId(string value, string parameterName, string label)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{label} id cannot be empty or whitespace.", parameterName);
        }

        return value.Trim();
    }
}
