using System.Collections.ObjectModel;

namespace NuclearReactorSimulator.Simulation.Tests.Physics.Fluids.Reference;

/// <summary>
/// RP1B Refinement 2 / C3. Test-only, versioned extension of C2. It preserves C2 for the
/// already-qualified core and adds only a near-saturated-vapor discriminator derived from a dense
/// IF97 saturation table. Direct IF97 is never called from TryResolve.
/// </summary>
internal sealed class Rp1bVaporSeamCompleteTabulatedSurrogateCandidate : IRp1bShadowThermodynamicCandidate
{
    private const double DenseSaturationStepKelvins = 0.02d;
    private const double MinimumBoundaryEnergyMarginJoulesPerKilogram = 0.05d;
    private const double MaximumBoundaryEnergyMarginJoulesPerKilogram = 100d;

    private readonly Rp1bExtendedTabulatedReferenceSurrogateCandidate _base = new();
    private readonly Rp1bRefinementSaturationTable _denseSaturation =
        Rp1bRefinementSaturationTable.Build(DenseSaturationStepKelvins);

    public string CandidateId => "C3-VAPOR-SEAM-COMPLETE-SURROGATE";
    public string FamilyId => "C-BOUNDED-IF97-DERIVED-TABLE-SURROGATE";
    public int InitializationReferencePointCount => _base.InitializationReferencePointCount + _denseSaturation.Nodes.Count;
    public int MaximumIterativeSolveIterations => _base.MaximumIterativeSolveIterations;
    public bool UsesDirectIf97AtResolveTime => false;

    public bool TryResolve(
        double specificVolumeCubicMetresPerKilogram,
        double specificInternalEnergyJoulesPerKilogram,
        out Rp1bShadowState state)
    {
        if (!double.IsFinite(specificVolumeCubicMetresPerKilogram)
            || specificVolumeCubicMetresPerKilogram <= 0d
            || !double.IsFinite(specificInternalEnergyJoulesPerKilogram))
        {
            state = default;
            return false;
        }

        // R2 immediately outside the vapor saturation boundary is the only broad C2 seam gap.
        // Classify it before C2's mixture search so a nearby Region-4 solution cannot steal the point.
        if (TryResolveNearVaporBoundary(
                specificVolumeCubicMetresPerKilogram,
                specificInternalEnergyJoulesPerKilogram,
                requireSuperheatedSide: true,
                out state))
        {
            return true;
        }

        if (_base.TryResolve(
                specificVolumeCubicMetresPerKilogram,
                specificInternalEnergyJoulesPerKilogram,
                out state))
        {
            return true;
        }

        // C2 had only two unresolved q~=1 Region-4 probes. A dense saturation boundary fallback is
        // deliberately reached only after the immutable C2 resolver has failed.
        return TryResolveNearVaporBoundary(
            specificVolumeCubicMetresPerKilogram,
            specificInternalEnergyJoulesPerKilogram,
            requireSuperheatedSide: false,
            out state);
    }

    private bool TryResolveNearVaporBoundary(
        double specificVolume,
        double specificEnergy,
        bool requireSuperheatedSide,
        out Rp1bShadowState state)
    {
        if (!TryInterpolateSaturationByVaporVolume(specificVolume, out var boundary))
        {
            state = default;
            return false;
        }

        var energyMargin = specificEnergy - boundary.VaporSpecificEnergy;
        if (!double.IsFinite(energyMargin))
        {
            state = default;
            return false;
        }

        if (requireSuperheatedSide)
        {
            if (energyMargin <= MinimumBoundaryEnergyMarginJoulesPerKilogram
                || energyMargin > MaximumBoundaryEnergyMarginJoulesPerKilogram)
            {
                state = default;
                return false;
            }

            state = new Rp1bShadowState(
                "REGION-2-VAPOR-SEAM-C3",
                "SuperheatedVapor",
                boundary.TemperatureKelvins - 273.15d,
                boundary.PressureMegapascals,
                null);
            return true;
        }

        if (energyMargin >= -MinimumBoundaryEnergyMarginJoulesPerKilogram
            || energyMargin < -MaximumBoundaryEnergyMarginJoulesPerKilogram)
        {
            state = default;
            return false;
        }

        state = new Rp1bShadowState(
            "REGION-4-VAPOR-SEAM-C3",
            "SaturatedMixture",
            boundary.TemperatureKelvins - 273.15d,
            boundary.PressureMegapascals,
            1d);
        return true;
    }

    private bool TryInterpolateSaturationByVaporVolume(
        double specificVolume,
        out Rp1bRefinementSaturationNode node)
    {
        ReadOnlyCollection<Rp1bRefinementSaturationNode> nodes = _denseSaturation.Nodes;
        if (nodes.Count < 2
            || specificVolume > nodes[0].VaporSpecificVolume
            || specificVolume < nodes[^1].VaporSpecificVolume)
        {
            node = default;
            return false;
        }

        var lower = 0;
        var upper = nodes.Count - 1;
        while (upper - lower > 1)
        {
            var middle = (lower + upper) / 2;
            if (nodes[middle].VaporSpecificVolume >= specificVolume)
            {
                lower = middle;
            }
            else
            {
                upper = middle;
            }
        }

        var left = nodes[lower];
        var right = nodes[upper];
        var denominator = left.VaporSpecificVolume - right.VaporSpecificVolume;
        var fraction = Math.Abs(denominator) <= 1e-30d
            ? 0d
            : Math.Clamp((left.VaporSpecificVolume - specificVolume) / denominator, 0d, 1d);

        node = new Rp1bRefinementSaturationNode(
            Lerp(left.TemperatureKelvins, right.TemperatureKelvins, fraction),
            Lerp(left.PressureMegapascals, right.PressureMegapascals, fraction),
            Lerp(left.LiquidSpecificVolume, right.LiquidSpecificVolume, fraction),
            Lerp(left.LiquidSpecificEnergy, right.LiquidSpecificEnergy, fraction),
            specificVolume,
            Lerp(left.VaporSpecificEnergy, right.VaporSpecificEnergy, fraction),
            Lerp(left.NearBoundaryLiquidBulkModulusPascals, right.NearBoundaryLiquidBulkModulusPascals, fraction),
            Lerp(left.NearBoundaryVaporPressurePerSpecificVolumeSlope, right.NearBoundaryVaporPressurePerSpecificVolumeSlope, fraction));
        return true;
    }

    private static double Lerp(double left, double right, double fraction)
        => left + (fraction * (right - left));
}

/// <summary>
/// RP1B Refinement 2 / D3. Test-only reference comparator. D2 remains the primary path; only D2
/// unresolved states can enter this near-vapor Region-4 fallback. C3 supplies the phase-aware seed,
/// while direct IF97 is used for a bounded local Region-4 refinement before the seed is accepted.
/// </summary>
internal sealed class Rp1bVaporSeamCompleteIf97ComparatorCandidate : IRp1bShadowThermodynamicCandidate
{
    private readonly Rp1bSeamCompleteIf97ComparatorCandidate _base = new();
    private readonly Rp1bVaporSeamCompleteTabulatedSurrogateCandidate _seed = new();

    public string CandidateId => "D3-VAPOR-SEAM-COMPLETE-IF97-COMPARATOR";
    public string FamilyId => "D-BOUNDED-PRODUCTION-IF97-SUBSET-COMPARATOR";
    public int InitializationReferencePointCount => _seed.InitializationReferencePointCount;
    public int MaximumIterativeSolveIterations => 1_700;
    public bool UsesDirectIf97AtResolveTime => true;

    public bool TryResolve(
        double specificVolumeCubicMetresPerKilogram,
        double specificInternalEnergyJoulesPerKilogram,
        out Rp1bShadowState state)
    {
        if (_base.TryResolve(
                specificVolumeCubicMetresPerKilogram,
                specificInternalEnergyJoulesPerKilogram,
                out state))
        {
            return true;
        }

        if (!_seed.TryResolve(
                specificVolumeCubicMetresPerKilogram,
                specificInternalEnergyJoulesPerKilogram,
                out var seed))
        {
            state = default;
            return false;
        }

        if (string.Equals(seed.Phase, "SaturatedMixture", StringComparison.Ordinal)
            && TryResolveNearVaporMixture(
                specificVolumeCubicMetresPerKilogram,
                specificInternalEnergyJoulesPerKilogram,
                seed.TemperatureCelsius + 273.15d,
                out state))
        {
            return true;
        }

        state = new Rp1bShadowState(
            string.Equals(seed.Phase, "SaturatedMixture", StringComparison.Ordinal)
                ? "REGION-4-VAPOR-SEAM-D3-SEED-FALLBACK"
                : seed.Region,
            seed.Phase,
            seed.TemperatureCelsius,
            seed.PressureMegapascals,
            seed.VaporQuality);
        return true;
    }

    private static bool TryResolveNearVaporMixture(
        double targetVolume,
        double targetEnergy,
        double seedTemperatureKelvins,
        out Rp1bShadowState state)
    {
        const double searchHalfSpanKelvins = 0.25d;
        const double scanStepKelvins = 0.005d;
        var minimumTemperature = Math.Max(273.15d, seedTemperatureKelvins - searchHalfSpanKelvins);
        var maximumTemperature = Math.Min(623.15d, seedTemperatureKelvins + searchHalfSpanKelvins);

        var havePrevious = false;
        var previousTemperature = double.NaN;
        var previousResidual = double.NaN;
        var bestTemperature = double.NaN;
        var bestResidualMagnitude = double.PositiveInfinity;
        var bestQuality = double.NaN;
        var bestPressure = double.NaN;

        for (var temperature = minimumTemperature;
             temperature <= maximumTemperature + 1e-12d;
             temperature += scanStepKelvins)
        {
            if (!TryEvaluateMixture(
                    temperature,
                    targetVolume,
                    targetEnergy,
                    out var residual,
                    out var quality,
                    out var pressure))
            {
                continue;
            }

            var magnitude = Math.Abs(residual);
            if (magnitude < bestResidualMagnitude)
            {
                bestResidualMagnitude = magnitude;
                bestTemperature = temperature;
                bestQuality = quality;
                bestPressure = pressure;
            }

            if (IsEnergyRoot(residual, targetEnergy))
            {
                state = ToMixtureState(temperature, pressure, quality);
                return true;
            }

            if (havePrevious && Math.Sign(previousResidual) != Math.Sign(residual))
            {
                return TryBisectMixture(
                    previousTemperature,
                    temperature,
                    previousResidual,
                    targetVolume,
                    targetEnergy,
                    out state);
            }

            havePrevious = true;
            previousTemperature = temperature;
            previousResidual = residual;
        }

        var relaxedTolerance = Math.Max(0.05d, Math.Abs(targetEnergy) * 5e-8d);
        if (double.IsFinite(bestTemperature)
            && bestResidualMagnitude <= relaxedTolerance
            && double.IsFinite(bestQuality)
            && bestQuality >= 0d
            && bestQuality <= 1d)
        {
            state = ToMixtureState(bestTemperature, bestPressure, bestQuality);
            return true;
        }

        state = default;
        return false;
    }

    private static bool TryBisectMixture(
        double lowerTemperature,
        double upperTemperature,
        double lowerResidual,
        double targetVolume,
        double targetEnergy,
        out Rp1bShadowState state)
    {
        var resolvedTemperature = lowerTemperature;
        var resolvedPressure = double.NaN;
        var resolvedQuality = double.NaN;

        for (var iteration = 0; iteration < 48; iteration++)
        {
            var middleTemperature = 0.5d * (lowerTemperature + upperTemperature);
            if (!TryEvaluateMixture(
                    middleTemperature,
                    targetVolume,
                    targetEnergy,
                    out var middleResidual,
                    out var middleQuality,
                    out var middlePressure))
            {
                upperTemperature = middleTemperature;
                continue;
            }

            resolvedTemperature = middleTemperature;
            resolvedPressure = middlePressure;
            resolvedQuality = middleQuality;

            if (IsEnergyRoot(middleResidual, targetEnergy))
            {
                state = ToMixtureState(resolvedTemperature, resolvedPressure, resolvedQuality);
                return true;
            }

            if (Math.Sign(lowerResidual) != Math.Sign(middleResidual))
            {
                upperTemperature = middleTemperature;
            }
            else
            {
                lowerTemperature = middleTemperature;
                lowerResidual = middleResidual;
            }
        }

        if (double.IsFinite(resolvedPressure) && double.IsFinite(resolvedQuality))
        {
            state = ToMixtureState(resolvedTemperature, resolvedPressure, resolvedQuality);
            return true;
        }

        state = default;
        return false;
    }

    private static bool TryEvaluateMixture(
        double temperatureKelvins,
        double targetVolume,
        double targetEnergy,
        out double residual,
        out double quality,
        out double pressureMegapascals)
    {
        if (temperatureKelvins < 273.15d || temperatureKelvins > 623.15d)
        {
            residual = double.NaN;
            quality = double.NaN;
            pressureMegapascals = double.NaN;
            return false;
        }

        pressureMegapascals = IapwsIf97Reference.SaturationPressureMegapascals(temperatureKelvins);
        var liquid = IapwsIf97Reference.Region1(temperatureKelvins, pressureMegapascals);
        var vapor = IapwsIf97Reference.Region2(temperatureKelvins, pressureMegapascals);
        var volumeTolerance = Math.Max(1e-14d, Math.Abs(targetVolume) * 1e-10d);
        if (targetVolume < liquid.SpecificVolumeCubicMetresPerKilogram - volumeTolerance
            || targetVolume > vapor.SpecificVolumeCubicMetresPerKilogram + volumeTolerance)
        {
            residual = double.NaN;
            quality = double.NaN;
            return false;
        }

        quality = Math.Clamp(
            (targetVolume - liquid.SpecificVolumeCubicMetresPerKilogram)
                / (vapor.SpecificVolumeCubicMetresPerKilogram - liquid.SpecificVolumeCubicMetresPerKilogram),
            0d,
            1d);
        var predictedEnergy = liquid.SpecificInternalEnergyJoulesPerKilogram
            + quality * (vapor.SpecificInternalEnergyJoulesPerKilogram - liquid.SpecificInternalEnergyJoulesPerKilogram);
        residual = predictedEnergy - targetEnergy;
        return double.IsFinite(residual);
    }

    private static bool IsEnergyRoot(double residual, double targetEnergy)
        => Math.Abs(residual) <= Math.Max(1e-5d, Math.Abs(targetEnergy) * 1e-10d);

    private static Rp1bShadowState ToMixtureState(
        double temperatureKelvins,
        double pressureMegapascals,
        double quality)
        => new(
            "REGION-4-MIXTURE-D3",
            "SaturatedMixture",
            temperatureKelvins - 273.15d,
            pressureMegapascals,
            quality);
}
