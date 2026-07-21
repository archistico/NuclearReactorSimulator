namespace NuclearReactorSimulator.Domain.Physics.TurbineIsland.Condenser;

/// <summary>
/// Replaceable M4.3 heat-rejection seam between one condenser and an external cooling-water/environment model.
/// </summary>
public sealed class CondenserCoolingBoundaryDefinition
{
    public CondenserCoolingBoundaryDefinition(string id, string condenserId)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Condenser cooling-boundary id cannot be empty or whitespace.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(condenserId))
        {
            throw new ArgumentException("Condenser id cannot be empty or whitespace.", nameof(condenserId));
        }

        Id = id.Trim();
        CondenserId = condenserId.Trim();
    }

    public string Id { get; }

    public string CondenserId { get; }
}
