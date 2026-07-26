using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Domain.Physics.TurbineIsland.MainSteam;

/// <summary>
/// Educational one-way ideal-vapor nozzle/orifice capacity definition used by the Phase-F relief/bypass seam.
/// The definition owns only the full-open throat geometry and ideal-gas coefficients. Topology, valve authority,
/// phase eligibility and conservative source-term integration remain separate later responsibilities.
/// </summary>
public sealed class CompressibleSteamFlowDefinition
{
    public CompressibleSteamFlowDefinition(
        Area fullOpenThroatArea,
        double dischargeCoefficient,
        SpecificGasConstant specificGasConstant,
        double heatCapacityRatio)
    {
        if (fullOpenThroatArea <= Area.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(fullOpenThroatArea),
                fullOpenThroatArea,
                "Compressible steam-flow throat area must be greater than zero.");
        }

        if (!double.IsFinite(dischargeCoefficient)
            || dischargeCoefficient <= 0d
            || dischargeCoefficient > 1d)
        {
            throw new ArgumentOutOfRangeException(
                nameof(dischargeCoefficient),
                dischargeCoefficient,
                "Compressible steam-flow discharge coefficient must be finite, greater than zero and no greater than one.");
        }

        if (specificGasConstant <= SpecificGasConstant.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(specificGasConstant),
                specificGasConstant,
                "Compressible steam-flow specific gas constant must be greater than zero.");
        }

        if (!double.IsFinite(heatCapacityRatio)
            || heatCapacityRatio <= 1d
            || heatCapacityRatio > 2d)
        {
            throw new ArgumentOutOfRangeException(
                nameof(heatCapacityRatio),
                heatCapacityRatio,
                "Compressible steam-flow heat-capacity ratio must be finite, greater than one and no greater than two.");
        }

        FullOpenThroatArea = fullOpenThroatArea;
        DischargeCoefficient = dischargeCoefficient;
        SpecificGasConstant = specificGasConstant;
        HeatCapacityRatio = heatCapacityRatio;
    }

    public Area FullOpenThroatArea { get; }

    public double DischargeCoefficient { get; }

    public SpecificGasConstant SpecificGasConstant { get; }

    public double HeatCapacityRatio { get; }

    public double CriticalDownstreamToUpstreamPressureRatio
        => Math.Pow(
            2d / (HeatCapacityRatio + 1d),
            HeatCapacityRatio / (HeatCapacityRatio - 1d));
}
