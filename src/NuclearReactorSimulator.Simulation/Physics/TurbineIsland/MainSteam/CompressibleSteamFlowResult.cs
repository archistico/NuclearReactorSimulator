using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Simulation.Physics.TurbineIsland.MainSteam;

/// <summary>
/// Immutable result of the educational one-way compressible steam capacity law.
/// </summary>
public sealed record CompressibleSteamFlowResult
{
    internal CompressibleSteamFlowResult(
        Pressure upstreamPressure,
        Pressure downstreamPressure,
        Temperature upstreamTemperature,
        Area effectiveThroatArea,
        double downstreamToUpstreamPressureRatio,
        double criticalPressureRatio,
        bool isChoked,
        MassFlowRate massFlowRate)
    {
        UpstreamPressure = upstreamPressure;
        DownstreamPressure = downstreamPressure;
        UpstreamTemperature = upstreamTemperature;
        EffectiveThroatArea = effectiveThroatArea;
        DownstreamToUpstreamPressureRatio = downstreamToUpstreamPressureRatio;
        CriticalPressureRatio = criticalPressureRatio;
        IsChoked = isChoked;
        MassFlowRate = massFlowRate;
    }

    public Pressure UpstreamPressure { get; }

    public Pressure DownstreamPressure { get; }

    public Temperature UpstreamTemperature { get; }

    public Area EffectiveThroatArea { get; }

    public double DownstreamToUpstreamPressureRatio { get; }

    public double CriticalPressureRatio { get; }

    public bool IsChoked { get; }

    public MassFlowRate MassFlowRate { get; }
}
