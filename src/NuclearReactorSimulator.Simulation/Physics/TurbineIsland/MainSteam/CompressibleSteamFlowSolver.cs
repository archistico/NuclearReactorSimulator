using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.TurbineIsland.MainSteam;

namespace NuclearReactorSimulator.Simulation.Physics.TurbineIsland.MainSteam;

/// <summary>
/// Educational ideal-vapor, isentropic, one-way nozzle/orifice capacity law.
/// It resolves both subcritical and sonic/choked mass flow from an upstream reservoir state and a downstream pressure.
/// It is intentionally not a two-phase critical-flow model and does not yet mutate plant inventories.
/// </summary>
public sealed class CompressibleSteamFlowSolver
{
    public CompressibleSteamFlowResult Solve(
        CompressibleSteamFlowDefinition definition,
        Pressure upstreamPressure,
        Temperature upstreamTemperature,
        Pressure downstreamPressure,
        double effectiveAreaFraction = 1d)
    {
        ArgumentNullException.ThrowIfNull(definition);

        if (upstreamPressure <= Pressure.Vacuum)
        {
            throw new ArgumentOutOfRangeException(
                nameof(upstreamPressure),
                upstreamPressure,
                "Compressible steam-flow upstream pressure must be greater than zero.");
        }

        if (upstreamTemperature <= Temperature.AbsoluteZero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(upstreamTemperature),
                upstreamTemperature,
                "Compressible steam-flow upstream temperature must be greater than absolute zero.");
        }

        if (!double.IsFinite(effectiveAreaFraction)
            || effectiveAreaFraction < 0d
            || effectiveAreaFraction > 1d)
        {
            throw new ArgumentOutOfRangeException(
                nameof(effectiveAreaFraction),
                effectiveAreaFraction,
                "Compressible steam-flow effective-area fraction must be finite and between zero and one.");
        }

        var effectiveArea = definition.FullOpenThroatArea * effectiveAreaFraction;
        var criticalPressureRatio = definition.CriticalDownstreamToUpstreamPressureRatio;

        if (effectiveArea == Area.Zero || downstreamPressure >= upstreamPressure)
        {
            return new CompressibleSteamFlowResult(
                upstreamPressure,
                downstreamPressure,
                upstreamTemperature,
                effectiveArea,
                1d,
                criticalPressureRatio,
                false,
                MassFlowRate.Zero);
        }

        var pressureRatio = downstreamPressure.Pascals / upstreamPressure.Pascals;
        var isChoked = pressureRatio <= criticalPressureRatio;
        var gamma = definition.HeatCapacityRatio;
        var gasConstant = definition.SpecificGasConstant.JoulesPerKilogramKelvin;
        var upstreamTemperatureKelvins = upstreamTemperature.Kelvins;

        var nondimensionalFlowFactor = isChoked
            ? CalculateChokedFlowFactor(gamma, gasConstant)
            : CalculateSubcriticalFlowFactor(gamma, gasConstant, pressureRatio);

        var kilogramsPerSecond = definition.DischargeCoefficient
            * effectiveArea.SquareMetres
            * upstreamPressure.Pascals
            / Math.Sqrt(upstreamTemperatureKelvins)
            * nondimensionalFlowFactor;

        if (!double.IsFinite(kilogramsPerSecond) || kilogramsPerSecond < 0d)
        {
            throw new ArithmeticException("Compressible steam-flow mass-flow result is non-finite or negative.");
        }

        return new CompressibleSteamFlowResult(
            upstreamPressure,
            downstreamPressure,
            upstreamTemperature,
            effectiveArea,
            pressureRatio,
            criticalPressureRatio,
            isChoked,
            MassFlowRate.FromKilogramsPerSecond(kilogramsPerSecond));
    }

    private static double CalculateChokedFlowFactor(double gamma, double gasConstant)
        => Math.Sqrt(gamma / gasConstant)
           * Math.Pow(
               2d / (gamma + 1d),
               (gamma + 1d) / (2d * (gamma - 1d)));

    private static double CalculateSubcriticalFlowFactor(
        double gamma,
        double gasConstant,
        double pressureRatio)
    {
        var firstPower = Math.Pow(pressureRatio, 2d / gamma);
        var secondPower = Math.Pow(pressureRatio, (gamma + 1d) / gamma);
        var radicand = 2d * gamma / (gasConstant * (gamma - 1d))
            * Math.Max(0d, firstPower - secondPower);
        return Math.Sqrt(radicand);
    }
}
