using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Simulation.Physics.Fluids;

/// <summary>
/// Immutable diagnostic result for one signed advective transfer between two open fluid control volumes.
/// The current plant runtime still transports specific internal energy; Phase G.1 exposes the enthalpy
/// convention and its explicit flow-work gap without mutating any committed inventory.
/// </summary>
public sealed record OpenControlVolumeEnergyTransportResult
{
    internal OpenControlVolumeEnergyTransportResult(
        string fromNodeId,
        string toNodeId,
        string upstreamNodeId,
        string downstreamNodeId,
        MassFlowRate referenceMassFlowRate,
        Pressure upstreamPressure,
        Density upstreamDensity,
        SpecificEnergy upstreamSpecificInternalEnergy,
        SpecificEnergy upstreamSpecificFlowWork,
        SpecificEnergy upstreamSpecificEnthalpy,
        Power signedInternalEnergyAdvectionRate,
        Power signedFlowWorkRate,
        Power signedEnthalpyTransportRate,
        FluidNodeBalance legacyFromNodeBalance,
        FluidNodeBalance legacyToNodeBalance,
        FluidNodeBalance enthalpyFromNodeBalance,
        FluidNodeBalance enthalpyToNodeBalance)
    {
        FromNodeId = fromNodeId;
        ToNodeId = toNodeId;
        UpstreamNodeId = upstreamNodeId;
        DownstreamNodeId = downstreamNodeId;
        ReferenceMassFlowRate = referenceMassFlowRate;
        UpstreamPressure = upstreamPressure;
        UpstreamDensity = upstreamDensity;
        UpstreamSpecificInternalEnergy = upstreamSpecificInternalEnergy;
        UpstreamSpecificFlowWork = upstreamSpecificFlowWork;
        UpstreamSpecificEnthalpy = upstreamSpecificEnthalpy;
        SignedInternalEnergyAdvectionRate = signedInternalEnergyAdvectionRate;
        SignedFlowWorkRate = signedFlowWorkRate;
        SignedEnthalpyTransportRate = signedEnthalpyTransportRate;
        LegacyFromNodeBalance = legacyFromNodeBalance;
        LegacyToNodeBalance = legacyToNodeBalance;
        EnthalpyFromNodeBalance = enthalpyFromNodeBalance;
        EnthalpyToNodeBalance = enthalpyToNodeBalance;
    }

    public string FromNodeId { get; }

    public string ToNodeId { get; }

    public string UpstreamNodeId { get; }

    public string DownstreamNodeId { get; }

    /// <summary>
    /// Signed reference-direction mass flow. Positive is from <see cref="FromNodeId"/> to
    /// <see cref="ToNodeId"/>; negative is the reverse direction.
    /// </summary>
    public MassFlowRate ReferenceMassFlowRate { get; }

    public Pressure UpstreamPressure { get; }

    public Density UpstreamDensity { get; }

    public SpecificEnergy UpstreamSpecificInternalEnergy { get; }

    /// <summary>Specific flow work p/rho carried by the upstream state.</summary>
    public SpecificEnergy UpstreamSpecificFlowWork { get; }

    /// <summary>Specific enthalpy h = u + p/rho carried by the upstream state.</summary>
    public SpecificEnergy UpstreamSpecificEnthalpy { get; }

    /// <summary>Signed legacy advected internal-energy rate u*m_dot.</summary>
    public Power SignedInternalEnergyAdvectionRate { get; }

    /// <summary>Signed explicit flow-work rate (p/rho)*m_dot.</summary>
    public Power SignedFlowWorkRate { get; }

    /// <summary>Signed accepted open-control-volume transport rate h*m_dot.</summary>
    public Power SignedEnthalpyTransportRate { get; }

    public FluidNodeBalance LegacyFromNodeBalance { get; }

    public FluidNodeBalance LegacyToNodeBalance { get; }

    public FluidNodeBalance EnthalpyFromNodeBalance { get; }

    public FluidNodeBalance EnthalpyToNodeBalance { get; }
}
