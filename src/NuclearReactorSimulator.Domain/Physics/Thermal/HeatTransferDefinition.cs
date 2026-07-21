using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Domain.Physics.Thermal;

/// <summary>
/// Immutable lumped thermal link between two named thermal domains.
/// Endpoint order defines only the positive sign convention.
/// </summary>
public sealed record HeatTransferDefinition
{
    public HeatTransferDefinition(
        string id,
        string fromDomainId,
        string toDomainId,
        ThermalConductance conductance)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Heat-transfer id cannot be empty.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(fromDomainId))
        {
            throw new ArgumentException("From-domain id cannot be empty.", nameof(fromDomainId));
        }

        if (string.IsNullOrWhiteSpace(toDomainId))
        {
            throw new ArgumentException("To-domain id cannot be empty.", nameof(toDomainId));
        }

        if (string.Equals(fromDomainId, toDomainId, StringComparison.Ordinal))
        {
            throw new ArgumentException("Heat-transfer endpoints must be different domains.", nameof(toDomainId));
        }

        if (conductance <= ThermalConductance.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(conductance), conductance, "Thermal conductance must be greater than zero.");
        }

        Id = id;
        FromDomainId = fromDomainId;
        ToDomainId = toDomainId;
        Conductance = conductance;
    }

    public string Id { get; }

    public string FromDomainId { get; }

    public string ToDomainId { get; }

    public ThermalConductance Conductance { get; }
}
