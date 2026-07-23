using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Simulation.Physics.Fluids;

/// <summary>
/// Educational deterministic water/steam closure for lumped control volumes.
/// It uses the IAPWS-IF97 Region-4 saturation-pressure equation as a reference boundary,
/// combined with deliberately simplified correlations for density and internal energy.
/// It is not a complete IAPWS-IF97 implementation and must not be used for engineering design.
/// </summary>
public sealed class SimplifiedWaterSteamThermodynamicModel : IFluidThermodynamicModel
{
    private const double TriplePointTemperatureKelvins = 273.16d;
    private const double CriticalTemperatureKelvins = 647.096d;
    private const double MaximumSaturationTemperatureKelvins = 640d;
    private const double MaximumSuperheatedTemperatureKelvins = 1_073.15d;
    private const double CriticalDensityKilogramsPerCubicMetre = 322d;
    private const double CriticalPressurePascals = 22_064_000d;
    private const double WaterVaporGasConstantJoulesPerKilogramKelvin = 461.526d;
    private const double LiquidSpecificHeatJoulesPerKilogramKelvin = 4_200d;
    private const double VaporSpecificHeatAtConstantVolumeJoulesPerKilogramKelvin = 1_700d;
    private const double LiquidBulkModulusPascals = 2.2e9d;
    private const double ReferenceLatentEnthalpyJoulesPerKilogram = 2_257_000d;
    private const double ReferenceLatentEnthalpyTemperatureKelvins = 373.15d;
    private const double WatsonExponent = 0.38d;
    private const int SearchSegments = 512;
    private const int BisectionIterations = 80;
    private const double RootRelativeTolerance = 1e-10d;

    public static Temperature MinimumTemperature { get; } = Temperature.FromKelvins(TriplePointTemperatureKelvins);

    public static Temperature MaximumSaturationTemperature { get; } = Temperature.FromKelvins(MaximumSaturationTemperatureKelvins);

    public static Temperature MaximumSuperheatedTemperature { get; } = Temperature.FromKelvins(MaximumSuperheatedTemperatureKelvins);

    public FluidThermodynamicState Resolve(
        FluidNodeDefinition definition,
        FluidNodeInventory inventory,
        FluidThermodynamicState previousState)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(inventory);
        ArgumentNullException.ThrowIfNull(previousState);

        _ = previousState;

        var specificVolume = definition.Volume.CubicMetres / inventory.Mass.Kilograms;
        var specificInternalEnergy = inventory.SpecificInternalEnergy.JoulesPerKilogram;

        if (!double.IsFinite(specificVolume) || specificVolume <= 0d || !double.IsFinite(specificInternalEnergy))
        {
            throw new WaterSteamStateOutOfRangeException(definition.Id, specificVolume, specificInternalEnergy);
        }

        if (TryResolveSaturatedMixture(specificVolume, specificInternalEnergy, out var saturatedState))
        {
            return saturatedState;
        }

        if (TryResolveSubcooledLiquid(specificVolume, specificInternalEnergy, out var liquidState))
        {
            return liquidState;
        }

        if (TryResolveSuperheatedVapor(specificVolume, specificInternalEnergy, out var vaporState))
        {
            return vaporState;
        }

        // The coarse saturated-mixture scan above intentionally preserves the long-validated fast path.
        // Near the saturated-liquid or saturated-vapor boundary, however, the physically valid temperature
        // interval can end between two coarse scan samples. In that case a real two-phase root may exist in
        // the narrow terminal interval even though no sampled sign change was observed. Re-scan only the
        // mathematically valid saturation interval before declaring the conserved state unsupported.
        if (TryResolveBoundaryAwareSaturatedMixture(specificVolume, specificInternalEnergy, out var boundaryState))
        {
            return boundaryState;
        }

        // The superheated branch has the same boundary-sampling hazard as the saturated branch: the first
        // thermodynamically admissible superheated temperature can fall between two coarse scan samples.
        // Inject the exact valid interval endpoints and reuse the existing deterministic root equations rather
        // than widening the envelope, clamping the conserved state or inventing a transition correlation.
        if (TryResolveBoundaryAwareSuperheatedVapor(specificVolume, specificInternalEnergy, out var boundaryVaporState))
        {
            return boundaryVaporState;
        }

        throw new WaterSteamStateOutOfRangeException(definition.Id, specificVolume, specificInternalEnergy);
    }

    public WaterSteamSaturationProperties GetSaturationProperties(Temperature temperature)
    {
        if (temperature.Kelvins < TriplePointTemperatureKelvins || temperature.Kelvins > MaximumSaturationTemperatureKelvins)
        {
            throw new ArgumentOutOfRangeException(
                nameof(temperature),
                temperature,
                $"Simplified saturation properties are supported from {TriplePointTemperatureKelvins} K through {MaximumSaturationTemperatureKelvins} K.");
        }

        return EvaluateSaturation(temperature.Kelvins);
    }

    private static bool TryResolveSaturatedMixture(
        double specificVolume,
        double specificInternalEnergy,
        out FluidThermodynamicState state)
    {
        var minimum = TriplePointTemperatureKelvins;
        var maximum = MaximumSaturationTemperatureKelvins;
        SaturatedEvaluation? previous = null;

        for (var index = 0; index <= SearchSegments; index++)
        {
            var temperature = minimum + ((maximum - minimum) * index / SearchSegments);
            var evaluation = EvaluateSaturatedCandidate(temperature, specificVolume, specificInternalEnergy);

            if (evaluation is null)
            {
                previous = null;
                continue;
            }

            if (IsRoot(evaluation.Value.ResidualJoulesPerKilogram, specificInternalEnergy))
            {
                state = CreateSaturatedState(evaluation.Value);
                return true;
            }

            if (previous is not null && HasSignChange(previous.Value.ResidualJoulesPerKilogram, evaluation.Value.ResidualJoulesPerKilogram))
            {
                var root = BisectSaturated(previous.Value.TemperatureKelvins, evaluation.Value.TemperatureKelvins, specificVolume, specificInternalEnergy);
                state = CreateSaturatedState(root);
                return true;
            }

            previous = evaluation;
        }

        state = null!;
        return false;
    }

    private static bool TryResolveBoundaryAwareSaturatedMixture(
        double specificVolume,
        double specificInternalEnergy,
        out FluidThermodynamicState state)
    {
        if (!TryGetSaturatedTemperatureUpperBound(specificVolume, out var maximum))
        {
            state = null!;
            return false;
        }

        var minimum = TriplePointTemperatureKelvins;
        SaturatedEvaluation? previous = null;

        for (var index = 0; index <= SearchSegments; index++)
        {
            var temperature = minimum + ((maximum - minimum) * index / SearchSegments);
            var evaluation = EvaluateSaturatedCandidate(temperature, specificVolume, specificInternalEnergy);

            if (evaluation is null)
            {
                previous = null;
                continue;
            }

            if (IsRoot(evaluation.Value.ResidualJoulesPerKilogram, specificInternalEnergy))
            {
                state = CreateSaturatedState(evaluation.Value);
                return true;
            }

            if (previous is not null && HasSignChange(previous.Value.ResidualJoulesPerKilogram, evaluation.Value.ResidualJoulesPerKilogram))
            {
                var root = BisectSaturated(
                    previous.Value.TemperatureKelvins,
                    evaluation.Value.TemperatureKelvins,
                    specificVolume,
                    specificInternalEnergy);
                state = CreateSaturatedState(root);
                return true;
            }

            previous = evaluation;
        }

        state = null!;
        return false;
    }

    private static bool TryGetSaturatedTemperatureUpperBound(double specificVolume, out double upperBoundKelvins)
    {
        if (!IsInsideSaturationSpecificVolumeEnvelope(TriplePointTemperatureKelvins, specificVolume))
        {
            upperBoundKelvins = 0d;
            return false;
        }

        if (IsInsideSaturationSpecificVolumeEnvelope(MaximumSaturationTemperatureKelvins, specificVolume))
        {
            upperBoundKelvins = MaximumSaturationTemperatureKelvins;
            return true;
        }

        var lower = TriplePointTemperatureKelvins;
        var upper = MaximumSaturationTemperatureKelvins;

        for (var iteration = 0; iteration < BisectionIterations; iteration++)
        {
            var middle = (lower + upper) / 2d;
            if (IsInsideSaturationSpecificVolumeEnvelope(middle, specificVolume))
            {
                lower = middle;
            }
            else
            {
                upper = middle;
            }
        }

        // Keep the last mathematically valid point rather than the first invalid point. This guarantees that
        // the terminal scan sample is evaluable even when the root lies arbitrarily close to quality 0 or 1.
        upperBoundKelvins = lower;
        return true;
    }

    private static bool IsInsideSaturationSpecificVolumeEnvelope(double temperatureKelvins, double specificVolume)
    {
        var saturation = EvaluateSaturation(temperatureKelvins);
        return specificVolume >= saturation.SaturatedLiquidSpecificVolumeCubicMetresPerKilogram
            && specificVolume <= saturation.SaturatedVaporSpecificVolumeCubicMetresPerKilogram;
    }

    private static bool TryResolveBoundaryAwareSuperheatedVapor(
        double specificVolume,
        double specificInternalEnergy,
        out FluidThermodynamicState state)
    {
        if (!TryGetSuperheatedTemperatureBounds(specificVolume, out var minimum, out var maximum))
        {
            state = null!;
            return false;
        }

        var lower = EvaluateSuperheatedCandidate(minimum, specificVolume, specificInternalEnergy);
        var upper = EvaluateSuperheatedCandidate(maximum, specificVolume, specificInternalEnergy);
        if (lower is null || upper is null)
        {
            state = null!;
            return false;
        }

        if (IsRoot(lower.Value.ResidualJoulesPerKilogram, specificInternalEnergy))
        {
            state = CreateSuperheatedState(lower.Value);
            return true;
        }

        if (IsRoot(upper.Value.ResidualJoulesPerKilogram, specificInternalEnergy))
        {
            state = CreateSuperheatedState(upper.Value);
            return true;
        }

        if (!HasSignChange(lower.Value.ResidualJoulesPerKilogram, upper.Value.ResidualJoulesPerKilogram))
        {
            state = null!;
            return false;
        }

        var root = BisectSuperheated(
            lower.Value.TemperatureKelvins,
            upper.Value.TemperatureKelvins,
            specificVolume,
            specificInternalEnergy);
        state = CreateSuperheatedState(root);
        return true;
    }

    private static bool TryGetSuperheatedTemperatureBounds(
        double specificVolume,
        out double minimumKelvins,
        out double maximumKelvins)
    {
        var maximumSaturationPressure = SaturationPressurePascals(MaximumSaturationTemperatureKelvins);
        var pressureLimitedMaximum = maximumSaturationPressure * specificVolume
            / WaterVaporGasConstantJoulesPerKilogramKelvin;
        maximumKelvins = Math.Min(MaximumSuperheatedTemperatureKelvins, pressureLimitedMaximum);

        if (maximumKelvins < TriplePointTemperatureKelvins
            || !IsSuperheatedTemperatureAdmissible(maximumKelvins, specificVolume))
        {
            minimumKelvins = 0d;
            maximumKelvins = 0d;
            return false;
        }

        if (IsSuperheatedTemperatureAdmissible(TriplePointTemperatureKelvins, specificVolume))
        {
            minimumKelvins = TriplePointTemperatureKelvins;
            return true;
        }

        var lower = TriplePointTemperatureKelvins;
        var upper = Math.Min(MaximumSaturationTemperatureKelvins, maximumKelvins);
        if (!IsSuperheatedTemperatureAdmissible(upper, specificVolume))
        {
            minimumKelvins = 0d;
            maximumKelvins = 0d;
            return false;
        }

        for (var iteration = 0; iteration < BisectionIterations; iteration++)
        {
            var middle = (lower + upper) / 2d;
            if (IsSuperheatedTemperatureAdmissible(middle, specificVolume))
            {
                upper = middle;
            }
            else
            {
                lower = middle;
            }
        }

        minimumKelvins = upper;
        return true;
    }

    private static bool IsSuperheatedTemperatureAdmissible(double temperatureKelvins, double specificVolume)
    {
        var pressurePascals = WaterVaporGasConstantJoulesPerKilogramKelvin * temperatureKelvins / specificVolume;
        if (!double.IsFinite(pressurePascals) || pressurePascals <= 0d || pressurePascals >= CriticalPressurePascals)
        {
            return false;
        }

        var saturationTemperature = SaturationTemperatureFromPressure(pressurePascals);
        return saturationTemperature is not null && temperatureKelvins >= saturationTemperature.Value;
    }

    private static bool TryResolveSubcooledLiquid(
        double specificVolume,
        double specificInternalEnergy,
        out FluidThermodynamicState state)
    {
        if (specificInternalEnergy < 0d)
        {
            state = null!;
            return false;
        }

        var temperatureKelvins = TriplePointTemperatureKelvins + (specificInternalEnergy / LiquidSpecificHeatJoulesPerKilogramKelvin);

        if (temperatureKelvins < TriplePointTemperatureKelvins || temperatureKelvins > MaximumSaturationTemperatureKelvins)
        {
            state = null!;
            return false;
        }

        var saturation = EvaluateSaturation(temperatureKelvins);
        var actualDensity = 1d / specificVolume;
        var saturatedLiquidDensity = saturation.SaturatedLiquidDensity.KilogramsPerCubicMetre;

        if (actualDensity + (saturatedLiquidDensity * 1e-10d) < saturatedLiquidDensity)
        {
            state = null!;
            return false;
        }

        var compressionRatio = Math.Max(0d, (actualDensity / saturatedLiquidDensity) - 1d);
        var pressurePascals = saturation.Pressure.Pascals + (LiquidBulkModulusPascals * compressionRatio);

        // The saturation and vapor correlations are bounded below critical pressure, but compressed liquid
        // remains a valid subcritical-temperature state when its pressure crosses the critical isobar.
        // Rejecting it here creates an artificial gap in the conserved (v, u) envelope at p = pcrit.
        if (!double.IsFinite(pressurePascals) || pressurePascals <= 0d)
        {
            state = null!;
            return false;
        }

        state = new FluidThermodynamicState(
            Pressure.FromPascals(pressurePascals),
            Temperature.FromKelvins(temperatureKelvins),
            FluidPhase.SubcooledLiquid,
            null);
        return true;
    }

    private static bool TryResolveSuperheatedVapor(
        double specificVolume,
        double specificInternalEnergy,
        out FluidThermodynamicState state)
    {
        SuperheatedEvaluation? previous = null;

        for (var index = 0; index <= SearchSegments; index++)
        {
            var temperature = TriplePointTemperatureKelvins
                + ((MaximumSuperheatedTemperatureKelvins - TriplePointTemperatureKelvins) * index / SearchSegments);
            var evaluation = EvaluateSuperheatedCandidate(temperature, specificVolume, specificInternalEnergy);

            if (evaluation is null)
            {
                previous = null;
                continue;
            }

            if (IsRoot(evaluation.Value.ResidualJoulesPerKilogram, specificInternalEnergy))
            {
                state = CreateSuperheatedState(evaluation.Value);
                return true;
            }

            if (previous is not null && HasSignChange(previous.Value.ResidualJoulesPerKilogram, evaluation.Value.ResidualJoulesPerKilogram))
            {
                var root = BisectSuperheated(previous.Value.TemperatureKelvins, evaluation.Value.TemperatureKelvins, specificVolume, specificInternalEnergy);
                state = CreateSuperheatedState(root);
                return true;
            }

            previous = evaluation;
        }

        state = null!;
        return false;
    }

    private static SaturatedEvaluation? EvaluateSaturatedCandidate(
        double temperatureKelvins,
        double specificVolume,
        double targetSpecificInternalEnergy)
    {
        var saturation = EvaluateSaturation(temperatureKelvins);
        var liquidSpecificVolume = saturation.SaturatedLiquidSpecificVolumeCubicMetresPerKilogram;
        var vaporSpecificVolume = saturation.SaturatedVaporSpecificVolumeCubicMetresPerKilogram;

        if (specificVolume < liquidSpecificVolume || specificVolume > vaporSpecificVolume)
        {
            return null;
        }

        var quality = (specificVolume - liquidSpecificVolume) / (vaporSpecificVolume - liquidSpecificVolume);
        quality = Math.Clamp(quality, 0d, 1d);

        var liquidEnergy = saturation.SaturatedLiquidInternalEnergy.JoulesPerKilogram;
        var vaporEnergy = saturation.SaturatedVaporInternalEnergy.JoulesPerKilogram;
        var mixtureEnergy = liquidEnergy + (quality * (vaporEnergy - liquidEnergy));

        return new SaturatedEvaluation(
            temperatureKelvins,
            quality,
            mixtureEnergy - targetSpecificInternalEnergy,
            saturation);
    }

    private static SaturatedEvaluation BisectSaturated(
        double lowerTemperatureKelvins,
        double upperTemperatureKelvins,
        double specificVolume,
        double targetSpecificInternalEnergy)
    {
        var lower = EvaluateSaturatedCandidate(lowerTemperatureKelvins, specificVolume, targetSpecificInternalEnergy)
            ?? throw new InvalidOperationException("Invalid lower saturated-state root bracket.");
        var upper = EvaluateSaturatedCandidate(upperTemperatureKelvins, specificVolume, targetSpecificInternalEnergy)
            ?? throw new InvalidOperationException("Invalid upper saturated-state root bracket.");

        for (var iteration = 0; iteration < BisectionIterations; iteration++)
        {
            var middleTemperature = (lower.TemperatureKelvins + upper.TemperatureKelvins) / 2d;
            var middle = EvaluateSaturatedCandidate(middleTemperature, specificVolume, targetSpecificInternalEnergy)
                ?? throw new InvalidOperationException("Saturated-state root bracket crossed an invalid phase interval.");

            if (IsRoot(middle.ResidualJoulesPerKilogram, targetSpecificInternalEnergy))
            {
                return middle;
            }

            if (HasSignChange(lower.ResidualJoulesPerKilogram, middle.ResidualJoulesPerKilogram))
            {
                upper = middle;
            }
            else
            {
                lower = middle;
            }
        }

        return EvaluateSaturatedCandidate(
            (lower.TemperatureKelvins + upper.TemperatureKelvins) / 2d,
            specificVolume,
            targetSpecificInternalEnergy)
            ?? throw new InvalidOperationException("Could not finalize saturated-state root.");
    }

    private static SuperheatedEvaluation? EvaluateSuperheatedCandidate(
        double temperatureKelvins,
        double specificVolume,
        double targetSpecificInternalEnergy)
    {
        var pressurePascals = WaterVaporGasConstantJoulesPerKilogramKelvin * temperatureKelvins / specificVolume;

        if (!double.IsFinite(pressurePascals) || pressurePascals <= 0d || pressurePascals >= CriticalPressurePascals)
        {
            return null;
        }

        var saturationTemperature = SaturationTemperatureFromPressure(pressurePascals);

        if (saturationTemperature is null || temperatureKelvins < saturationTemperature.Value)
        {
            return null;
        }

        var saturation = EvaluateSaturation(saturationTemperature.Value);
        var modeledSpecificInternalEnergy = saturation.SaturatedVaporInternalEnergy.JoulesPerKilogram
            + (VaporSpecificHeatAtConstantVolumeJoulesPerKilogramKelvin * (temperatureKelvins - saturationTemperature.Value));

        return new SuperheatedEvaluation(
            temperatureKelvins,
            pressurePascals,
            modeledSpecificInternalEnergy - targetSpecificInternalEnergy);
    }

    private static SuperheatedEvaluation BisectSuperheated(
        double lowerTemperatureKelvins,
        double upperTemperatureKelvins,
        double specificVolume,
        double targetSpecificInternalEnergy)
    {
        var lower = EvaluateSuperheatedCandidate(lowerTemperatureKelvins, specificVolume, targetSpecificInternalEnergy)
            ?? throw new InvalidOperationException("Invalid lower superheated-state root bracket.");
        var upper = EvaluateSuperheatedCandidate(upperTemperatureKelvins, specificVolume, targetSpecificInternalEnergy)
            ?? throw new InvalidOperationException("Invalid upper superheated-state root bracket.");

        for (var iteration = 0; iteration < BisectionIterations; iteration++)
        {
            var middleTemperature = (lower.TemperatureKelvins + upper.TemperatureKelvins) / 2d;
            var middle = EvaluateSuperheatedCandidate(middleTemperature, specificVolume, targetSpecificInternalEnergy)
                ?? throw new InvalidOperationException("Superheated-state root bracket crossed an invalid phase interval.");

            if (IsRoot(middle.ResidualJoulesPerKilogram, targetSpecificInternalEnergy))
            {
                return middle;
            }

            if (HasSignChange(lower.ResidualJoulesPerKilogram, middle.ResidualJoulesPerKilogram))
            {
                upper = middle;
            }
            else
            {
                lower = middle;
            }
        }

        return EvaluateSuperheatedCandidate(
            (lower.TemperatureKelvins + upper.TemperatureKelvins) / 2d,
            specificVolume,
            targetSpecificInternalEnergy)
            ?? throw new InvalidOperationException("Could not finalize superheated-state root.");
    }

    private static FluidThermodynamicState CreateSaturatedState(SaturatedEvaluation evaluation)
    {
        return new FluidThermodynamicState(
            evaluation.Saturation.Pressure,
            evaluation.Saturation.Temperature,
            FluidPhase.SaturatedMixture,
            VaporQuality.FromFraction(evaluation.Quality));
    }

    private static FluidThermodynamicState CreateSuperheatedState(SuperheatedEvaluation evaluation)
    {
        return new FluidThermodynamicState(
            Pressure.FromPascals(evaluation.PressurePascals),
            Temperature.FromKelvins(evaluation.TemperatureKelvins),
            FluidPhase.SuperheatedVapor,
            null);
    }

    private static WaterSteamSaturationProperties EvaluateSaturation(double temperatureKelvins)
    {
        var pressurePascals = SaturationPressurePascals(temperatureKelvins);
        var liquidDensity = SaturatedLiquidDensityKilogramsPerCubicMetre(temperatureKelvins);
        var vaporDensity = SaturatedVaporDensityKilogramsPerCubicMetre(temperatureKelvins);
        var liquidInternalEnergy = LiquidSpecificHeatJoulesPerKilogramKelvin * (temperatureKelvins - TriplePointTemperatureKelvins);
        var latentEnthalpy = LatentEnthalpyJoulesPerKilogram(temperatureKelvins);
        var liquidSpecificVolume = 1d / liquidDensity;
        var vaporSpecificVolume = 1d / vaporDensity;
        var latentInternalEnergy = latentEnthalpy - (pressurePascals * (vaporSpecificVolume - liquidSpecificVolume));
        var vaporInternalEnergy = liquidInternalEnergy + latentInternalEnergy;

        return new WaterSteamSaturationProperties(
            Temperature.FromKelvins(temperatureKelvins),
            Pressure.FromPascals(pressurePascals),
            Density.FromKilogramsPerCubicMetre(liquidDensity),
            Density.FromKilogramsPerCubicMetre(vaporDensity),
            SpecificEnergy.FromJoulesPerKilogram(liquidInternalEnergy),
            SpecificEnergy.FromJoulesPerKilogram(vaporInternalEnergy));
    }

    private static double SaturationPressurePascals(double temperatureKelvins)
    {
        const double n1 = 0.11670521452767e4d;
        const double n2 = -0.72421316703206e6d;
        const double n3 = -0.17073846940092e2d;
        const double n4 = 0.12020824702470e5d;
        const double n5 = -0.32325550322333e7d;
        const double n6 = 0.14915108613530e2d;
        const double n7 = -0.48232657361591e4d;
        const double n8 = 0.40511340542057e6d;
        const double n9 = -0.23855557567849d;
        const double n10 = 0.65017534844798e3d;

        var theta = temperatureKelvins + (n9 / (temperatureKelvins - n10));
        var a = (theta * theta) + (n1 * theta) + n2;
        var b = (n3 * theta * theta) + (n4 * theta) + n5;
        var c = (n6 * theta * theta) + (n7 * theta) + n8;
        var pressureMegapascals = Math.Pow((2d * c) / (-b + Math.Sqrt((b * b) - (4d * a * c))), 4d);
        return pressureMegapascals * 1_000_000d;
    }

    private static double SaturatedLiquidDensityKilogramsPerCubicMetre(double temperatureKelvins)
    {
        var tau = 1d - (temperatureKelvins / CriticalTemperatureKelvins);
        var reducedDensity = 1d
            + (1.99274064d * Math.Pow(tau, 1d / 3d))
            + (1.09965342d * Math.Pow(tau, 2d / 3d))
            - (0.510839303d * Math.Pow(tau, 5d / 3d))
            - (1.75493479d * Math.Pow(tau, 16d / 3d))
            - (45.5170352d * Math.Pow(tau, 43d / 3d))
            - (6.74694450e5d * Math.Pow(tau, 110d / 3d));

        return CriticalDensityKilogramsPerCubicMetre * reducedDensity;
    }

    private static double SaturatedVaporDensityKilogramsPerCubicMetre(double temperatureKelvins)
    {
        var tau = 1d - (temperatureKelvins / CriticalTemperatureKelvins);
        var logarithmicReducedDensity =
            (-2.03150240d * Math.Pow(tau, 2d / 6d))
            - (2.68302940d * Math.Pow(tau, 4d / 6d))
            - (5.38626492d * Math.Pow(tau, 8d / 6d))
            - (17.2991605d * Math.Pow(tau, 18d / 6d))
            - (44.7586581d * Math.Pow(tau, 37d / 6d))
            - (63.9201063d * Math.Pow(tau, 71d / 6d));

        return CriticalDensityKilogramsPerCubicMetre * Math.Exp(logarithmicReducedDensity);
    }

    private static double LatentEnthalpyJoulesPerKilogram(double temperatureKelvins)
    {
        var numerator = 1d - (temperatureKelvins / CriticalTemperatureKelvins);
        var denominator = 1d - (ReferenceLatentEnthalpyTemperatureKelvins / CriticalTemperatureKelvins);
        return ReferenceLatentEnthalpyJoulesPerKilogram * Math.Pow(numerator / denominator, WatsonExponent);
    }

    private static double? SaturationTemperatureFromPressure(double pressurePascals)
    {
        var minimumPressure = SaturationPressurePascals(TriplePointTemperatureKelvins);
        var maximumPressure = SaturationPressurePascals(MaximumSaturationTemperatureKelvins);

        if (pressurePascals < minimumPressure)
        {
            return TriplePointTemperatureKelvins;
        }

        if (pressurePascals > maximumPressure)
        {
            return null;
        }

        var lower = TriplePointTemperatureKelvins;
        var upper = MaximumSaturationTemperatureKelvins;

        for (var iteration = 0; iteration < BisectionIterations; iteration++)
        {
            var middle = (lower + upper) / 2d;
            var middlePressure = SaturationPressurePascals(middle);

            if (middlePressure < pressurePascals)
            {
                lower = middle;
            }
            else
            {
                upper = middle;
            }
        }

        return (lower + upper) / 2d;
    }

    private static bool IsRoot(double residual, double target)
    {
        return Math.Abs(residual) <= Math.Max(1d, Math.Abs(target)) * RootRelativeTolerance;
    }

    private static bool HasSignChange(double left, double right)
    {
        return (left <= 0d && right >= 0d) || (left >= 0d && right <= 0d);
    }

    private readonly record struct SaturatedEvaluation(
        double TemperatureKelvins,
        double Quality,
        double ResidualJoulesPerKilogram,
        WaterSteamSaturationProperties Saturation);

    private readonly record struct SuperheatedEvaluation(
        double TemperatureKelvins,
        double PressurePascals,
        double ResidualJoulesPerKilogram);
}
