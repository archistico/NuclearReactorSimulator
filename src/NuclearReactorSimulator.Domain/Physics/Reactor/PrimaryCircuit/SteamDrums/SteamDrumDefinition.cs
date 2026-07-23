namespace NuclearReactorSimulator.Domain.Physics.Reactor.PrimaryCircuit.SteamDrums;

/// <summary>
/// Semantic definition of one aggregated steam drum.
/// The drum inventory, steam outlet and circulation suction header remain canonical plant fluid nodes.
/// </summary>
public sealed record SteamDrumDefinition
{
    public SteamDrumDefinition(
        string id,
        string mainCirculationLoopId,
        string inventoryNodeId,
        string steamOutletNodeId,
        SteamDrumLiquidRecirculationMode liquidRecirculationMode = SteamDrumLiquidRecirculationMode.LegacyReturnSplit)
    {
        Id = ValidateId(id, nameof(id), "Steam drum");
        MainCirculationLoopId = ValidateId(mainCirculationLoopId, nameof(mainCirculationLoopId), "Main-circulation loop");
        InventoryNodeId = ValidateId(inventoryNodeId, nameof(inventoryNodeId), "Steam-drum inventory node");
        SteamOutletNodeId = ValidateId(steamOutletNodeId, nameof(steamOutletNodeId), "Steam-outlet node");
        LiquidRecirculationMode = liquidRecirculationMode;

        if (!Enum.IsDefined(liquidRecirculationMode))
        {
            throw new ArgumentOutOfRangeException(nameof(liquidRecirculationMode), liquidRecirculationMode, "Unknown steam-drum liquid-recirculation mode.");
        }

        if (string.Equals(InventoryNodeId, SteamOutletNodeId, StringComparison.Ordinal))
        {
            throw new ArgumentException("Steam-drum inventory and steam-outlet nodes must be distinct.");
        }
    }

    public string Id { get; }

    public string MainCirculationLoopId { get; }

    public string InventoryNodeId { get; }

    public string SteamOutletNodeId { get; }

    public SteamDrumLiquidRecirculationMode LiquidRecirculationMode { get; }

    private static string ValidateId(string value, string parameterName, string label)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{label} id cannot be empty or whitespace.", parameterName);
        }

        return value.Trim();
    }
}
