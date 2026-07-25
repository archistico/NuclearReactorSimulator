using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Integration;

/// <summary>
/// M4.7 gross plant-performance diagnostics derived only from already audited M4.6 power paths.
/// Undefined ratios remain null rather than inventing a denominator or hidden correction.
/// </summary>
public sealed record FullPlantPerformanceDiagnostics(
    Power ReactorThermalPower,
    Power TurbineShaftPower,
    Power GeneratorMechanicalInputPower,
    Power GrossElectricalOutputPower,
    Power CondenserHeatRejectionPower,
    Power GeneratorConversionLossPower,
    double? GrossThermalEfficiencyFraction,
    double? TurbineShaftToReactorHeatFraction,
    double? GeneratorConversionEfficiencyFraction,
    double? GrossHeatRateJoulesThermalPerJouleElectrical)
{
    public double? GrossThermalEfficiencyPercent => GrossThermalEfficiencyFraction * 100d;

    public double? TurbineShaftToReactorHeatPercent => TurbineShaftToReactorHeatFraction * 100d;

    public double? GeneratorConversionEfficiencyPercent => GeneratorConversionEfficiencyFraction * 100d;

    public static FullPlantPerformanceDiagnostics From(IntegratedSecondaryCycleSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var audit = snapshot.HeatBalance;
        var reactorThermal = audit.NuclearHeatInputPower;
        var shaft = audit.TurbineShaftPower;
        var generatorMechanical = audit.GeneratorMechanicalInputPower;
        var electrical = audit.ElectricalExportPower;

        return new FullPlantPerformanceDiagnostics(
            reactorThermal,
            shaft,
            generatorMechanical,
            electrical,
            audit.CondenserHeatRejectionPower,
            audit.GeneratorConversionLossPower,
            RatioOrNull(electrical.Watts, reactorThermal.Watts),
            RatioOrNull(shaft.Watts, reactorThermal.Watts),
            RatioOrNull(electrical.Watts, generatorMechanical.Watts),
            reactorThermal.Watts > 0d && electrical.Watts > 0d
                ? reactorThermal.Watts / electrical.Watts
                : null);
    }

    private static double? RatioOrNull(double numerator, double denominator)
        => denominator > 0d ? numerator / denominator : null;
}
