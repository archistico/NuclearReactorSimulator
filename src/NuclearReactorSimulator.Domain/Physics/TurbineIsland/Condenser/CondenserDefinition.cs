using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Domain.Physics.TurbineIsland.Condenser;

/// <summary>
/// Canonical M4.3 lumped surface condenser bound to one turbine exhaust steam-space node and one hotwell node.
/// </summary>
public sealed class CondenserDefinition
{
    public CondenserDefinition(
        string id,
        string turbineStageGroupId,
        string steamSpaceNodeId,
        string hotwellNodeId,
        string coolingBoundaryId,
        MassFlowRate maximumCondensationMassFlowRate,
        ThermalConductance? overallHeatTransferConductance = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Condenser id cannot be empty or whitespace.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(turbineStageGroupId))
        {
            throw new ArgumentException("Turbine stage-group id cannot be empty or whitespace.", nameof(turbineStageGroupId));
        }

        if (string.IsNullOrWhiteSpace(steamSpaceNodeId))
        {
            throw new ArgumentException("Condenser steam-space node id cannot be empty or whitespace.", nameof(steamSpaceNodeId));
        }

        if (string.IsNullOrWhiteSpace(hotwellNodeId))
        {
            throw new ArgumentException("Condenser hotwell node id cannot be empty or whitespace.", nameof(hotwellNodeId));
        }

        if (string.IsNullOrWhiteSpace(coolingBoundaryId))
        {
            throw new ArgumentException("Cooling-boundary id cannot be empty or whitespace.", nameof(coolingBoundaryId));
        }

        if (string.Equals(steamSpaceNodeId.Trim(), hotwellNodeId.Trim(), StringComparison.Ordinal))
        {
            throw new ArgumentException("Condenser steam-space and hotwell nodes must be different.", nameof(hotwellNodeId));
        }

        if (maximumCondensationMassFlowRate <= MassFlowRate.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumCondensationMassFlowRate),
                maximumCondensationMassFlowRate,
                "Maximum condenser condensation mass flow must be greater than zero.");
        }

        if (overallHeatTransferConductance is { } conductance && conductance <= ThermalConductance.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(overallHeatTransferConductance),
                overallHeatTransferConductance,
                "Condenser overall heat-transfer conductance must be greater than zero when supplied.");
        }

        Id = id.Trim();
        TurbineStageGroupId = turbineStageGroupId.Trim();
        SteamSpaceNodeId = steamSpaceNodeId.Trim();
        HotwellNodeId = hotwellNodeId.Trim();
        CoolingBoundaryId = coolingBoundaryId.Trim();
        MaximumCondensationMassFlowRate = maximumCondensationMassFlowRate;
        OverallHeatTransferConductance = overallHeatTransferConductance;
    }

    public string Id { get; }

    public string TurbineStageGroupId { get; }

    public string SteamSpaceNodeId { get; }

    public string HotwellNodeId { get; }

    public string CoolingBoundaryId { get; }

    public MassFlowRate MaximumCondensationMassFlowRate { get; }

    /// <summary>
    /// Optional canonical UA conductance for pressure/temperature-responsive surface-condensing behavior.
    /// Null preserves the historical capacity-only condenser law for isolated legacy definitions.
    /// </summary>
    public ThermalConductance? OverallHeatTransferConductance { get; }
}
