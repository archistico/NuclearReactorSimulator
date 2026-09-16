using System.Collections.ObjectModel;

namespace NuclearReactorSimulator.Simulation.Tests.Physics.Fluids.Reference;

/// <summary>
/// RP1B Refinement 1 / C2. Versioned test-only extension of C1. It keeps IF97 out of resolve-time
/// execution, expands Region-1 coverage, adds near-boundary pressure nodes and uses an explicit
/// reachability-aware Region-4 table. C1 remains immutable evidence.
/// </summary>
internal sealed class Rp1bExtendedTabulatedReferenceSurrogateCandidate : IRp1bShadowThermodynamicCandidate
{
    private static readonly double[] LiquidPressureNodesMegapascals =
    [
        0.02d, 0.05d, 0.1d, 0.2d, 0.5d, 1d, 2d, 5d, 7d, 10d, 15d, 20d, 30d, 50d, 75d, 100d,
    ];

    private static readonly double[] LiquidBoundaryOffsetsMegapascals =
    [
        0.001d, 0.005d, 0.02d, 0.1d, 0.5d, 2d, 5d,
    ];

    private static readonly double[] VaporPressureNodesMegapascals =
    [
        0.001d, 0.002d, 0.005d, 0.01d, 0.02d, 0.05d, 0.1d, 0.2d, 0.5d, 1d, 2d, 5d, 10d, 15d, 20d,
    ];

    private static readonly double[] VaporBoundaryFractions =
    [
        0.999999d, 0.9999d, 0.999d, 0.99d, 0.95d, 0.9d, 0.75d, 0.5d, 0.25d, 0.1d,
    ];

    private readonly Rp1bRefinementSaturationTable _saturation;
    private readonly ReadOnlyCollection<SurrogateTemperatureRow> _liquidRows;
    private readonly ReadOnlyCollection<SurrogateTemperatureRow> _vaporRows;
    private readonly int _referencePointCount;

    public Rp1bExtendedTabulatedReferenceSurrogateCandidate()
    {
        _saturation = Rp1bRefinementSaturationTable.Build(stepKelvins: 0.5d);
        _liquidRows = BuildLiquidRows();
        _vaporRows = BuildVaporRows();
        _referencePointCount = _saturation.Nodes.Count
            + _liquidRows.Sum(static row => row.Points.Count)
            + _vaporRows.Sum(static row => row.Points.Count);
    }

    public string CandidateId => "C2-EXTENDED-TABULATED-SURROGATE";
    public string FamilyId => "C-BOUNDED-IF97-DERIVED-TABLE-SURROGATE";
    public int InitializationReferencePointCount => _referencePointCount;
    public int MaximumIterativeSolveIterations => 48;
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

        if (_saturation.TryResolveMixture(
                specificVolumeCubicMetresPerKilogram,
                specificInternalEnergyJoulesPerKilogram,
                out state))
        {
            return true;
        }

        if (TryResolveFromTable(
                _liquidRows,
                specificVolumeCubicMetresPerKilogram,
                specificInternalEnergyJoulesPerKilogram,
                "REGION-1-TABLE-C2",
                "SubcooledLiquid",
                maximumTemperatureGapKelvins: 3.1d,
                out state))
        {
            return true;
        }

        if (TryResolveNearBoundaryLiquid(
                specificVolumeCubicMetresPerKilogram,
                specificInternalEnergyJoulesPerKilogram,
                out state))
        {
            return true;
        }

        if (TryResolveFromTable(
                _vaporRows,
                specificVolumeCubicMetresPerKilogram,
                specificInternalEnergyJoulesPerKilogram,
                "REGION-2-TABLE-C2",
                "SuperheatedVapor",
                maximumTemperatureGapKelvins: 6.1d,
                out state))
        {
            return true;
        }

        return TryResolveNearBoundaryVapor(
            specificVolumeCubicMetresPerKilogram,
            specificInternalEnergyJoulesPerKilogram,
            out state);
    }

    private bool TryResolveNearBoundaryLiquid(double specificVolume, double specificEnergy, out Rp1bShadowState state)
    {
        if (!_saturation.TryInterpolateByLiquidEnergy(specificEnergy, out var saturation))
        {
            state = default;
            return false;
        }

        var volumeTolerance = Math.Max(1e-14d, saturation.LiquidSpecificVolume * 1e-10d);
        if (specificVolume > saturation.LiquidSpecificVolume + volumeTolerance)
        {
            state = default;
            return false;
        }

        var density = 1d / specificVolume;
        var saturationDensity = 1d / saturation.LiquidSpecificVolume;
        var compressionRatio = (density / saturationDensity) - 1d;
        if (!double.IsFinite(compressionRatio) || compressionRatio < -1e-10d || compressionRatio > 0.02d)
        {
            state = default;
            return false;
        }

        var pressure = saturation.PressureMegapascals
            + ((saturation.NearBoundaryLiquidBulkModulusPascals * Math.Max(0d, compressionRatio)) / 1_000_000d);
        if (!double.IsFinite(pressure)
            || pressure < saturation.PressureMegapascals * (1d - 1e-10d)
            || pressure > 100d)
        {
            state = default;
            return false;
        }

        state = new Rp1bShadowState(
            "REGION-1-NEAR-BOUNDARY-C2",
            "SubcooledLiquid",
            saturation.TemperatureKelvins - 273.15d,
            pressure,
            null);
        return true;
    }

    private bool TryResolveNearBoundaryVapor(double specificVolume, double specificEnergy, out Rp1bShadowState state)
    {
        if (!_saturation.TryInterpolateByVaporEnergy(specificEnergy, out var saturation))
        {
            state = default;
            return false;
        }

        var volumeTolerance = Math.Max(1e-12d, saturation.VaporSpecificVolume * 1e-10d);
        if (specificVolume < saturation.VaporSpecificVolume - volumeTolerance)
        {
            state = default;
            return false;
        }

        var expansionRatio = (specificVolume / saturation.VaporSpecificVolume) - 1d;
        if (!double.IsFinite(expansionRatio) || expansionRatio < -1e-10d || expansionRatio > 0.05d)
        {
            state = default;
            return false;
        }

        var pressure = saturation.PressureMegapascals
            + (saturation.NearBoundaryVaporPressurePerSpecificVolumeSlope
                * (specificVolume - saturation.VaporSpecificVolume));
        var maximumPressure = Rp1bIf97Domain.Region2MaximumPressureMegapascals(saturation.TemperatureKelvins);
        pressure = Math.Clamp(pressure, 1e-6d, maximumPressure);
        if (!double.IsFinite(pressure) || pressure <= 0d)
        {
            state = default;
            return false;
        }

        state = new Rp1bShadowState(
            "REGION-2-NEAR-BOUNDARY-C2",
            "SuperheatedVapor",
            saturation.TemperatureKelvins - 273.15d,
            pressure,
            null);
        return true;
    }

    private static ReadOnlyCollection<SurrogateTemperatureRow> BuildLiquidRows()
    {
        var rows = new List<SurrogateTemperatureRow>();
        foreach (var temperature in TemperatureGrid(273.15d, 623.15d, 1d))
        {
            var saturationPressure = IapwsIf97Reference.SaturationPressureMegapascals(temperature);
            var states = new List<IapwsReferenceState>();
            AddUniqueState(states, IapwsIf97Reference.Region1(temperature, saturationPressure));

            foreach (var offset in LiquidBoundaryOffsetsMegapascals)
            {
                var pressure = saturationPressure + offset;
                if (pressure <= 100d)
                {
                    AddUniqueState(states, IapwsIf97Reference.Region1(temperature, pressure));
                }
            }

            foreach (var pressure in LiquidPressureNodesMegapascals)
            {
                if (pressure <= saturationPressure * (1d + 1e-12d)) continue;
                AddUniqueState(states, IapwsIf97Reference.Region1(temperature, pressure));
            }

            if (states.Count >= 2)
            {
                var points = states
                    .Select(ToTablePoint)
                    .OrderByDescending(static point => point.SpecificVolume)
                    .ToList()
                    .AsReadOnly();
                rows.Add(new SurrogateTemperatureRow(temperature, points));
            }
        }

        return rows.AsReadOnly();
    }

    private static ReadOnlyCollection<SurrogateTemperatureRow> BuildVaporRows()
    {
        var rows = new List<SurrogateTemperatureRow>();
        foreach (var temperature in TemperatureGrid(273.15d, 1_073.15d, 2d))
        {
            var maximumPressure = Rp1bIf97Domain.Region2MaximumPressureMegapascals(temperature);
            var states = new List<IapwsReferenceState>();
            AddUniqueState(states, IapwsIf97Reference.Region2(temperature, maximumPressure));

            foreach (var fraction in VaporBoundaryFractions)
            {
                var pressure = maximumPressure * fraction;
                if (pressure > 1e-6d)
                {
                    AddUniqueState(states, IapwsIf97Reference.Region2(temperature, pressure));
                }
            }

            foreach (var pressure in VaporPressureNodesMegapascals)
            {
                if (pressure > maximumPressure * (1d + 1e-12d)) continue;
                AddUniqueState(states, IapwsIf97Reference.Region2(temperature, pressure));
            }

            if (states.Count >= 2)
            {
                var points = states
                    .Select(ToTablePoint)
                    .OrderByDescending(static point => point.SpecificVolume)
                    .ToList()
                    .AsReadOnly();
                rows.Add(new SurrogateTemperatureRow(temperature, points));
            }
        }

        return rows.AsReadOnly();
    }

    private static void AddUniqueState(List<IapwsReferenceState> states, IapwsReferenceState candidate)
    {
        if (states.Any(state => Math.Abs(state.PressureMegapascals - candidate.PressureMegapascals) <= 1e-12d))
        {
            return;
        }

        states.Add(candidate);
    }

    private static bool TryResolveFromTable(
        IReadOnlyList<SurrogateTemperatureRow> rows,
        double specificVolume,
        double specificEnergy,
        string region,
        string phase,
        double maximumTemperatureGapKelvins,
        out Rp1bShadowState state)
    {
        var previousFound = false;
        var previous = default(RowInterpolation);

        foreach (var row in rows)
        {
            if (!TryInterpolateAtSpecificVolume(row, specificVolume, out var current))
            {
                continue;
            }

            var currentResidual = current.SpecificEnergy - specificEnergy;
            if (IsEnergyRoot(currentResidual, specificEnergy))
            {
                state = new Rp1bShadowState(region, phase, row.TemperatureKelvins - 273.15d, current.PressureMegapascals, null);
                return true;
            }

            if (previousFound)
            {
                var temperatureGap = row.TemperatureKelvins - previous.TemperatureKelvins;
                var previousResidual = previous.SpecificEnergy - specificEnergy;
                if (temperatureGap <= maximumTemperatureGapKelvins
                    && HasSignChange(previousResidual, currentResidual))
                {
                    var denominator = currentResidual - previousResidual;
                    var fraction = Math.Abs(denominator) <= 1e-30d
                        ? 0.5d
                        : Math.Clamp(-previousResidual / denominator, 0d, 1d);
                    var temperature = previous.TemperatureKelvins
                        + (fraction * (row.TemperatureKelvins - previous.TemperatureKelvins));
                    var pressure = previous.PressureMegapascals
                        + (fraction * (current.PressureMegapascals - previous.PressureMegapascals));
                    state = new Rp1bShadowState(region, phase, temperature - 273.15d, pressure, null);
                    return double.IsFinite(temperature) && double.IsFinite(pressure) && pressure > 0d;
                }
            }

            previousFound = true;
            previous = current with { TemperatureKelvins = row.TemperatureKelvins };
        }

        state = default;
        return false;
    }

    private static bool TryInterpolateAtSpecificVolume(
        SurrogateTemperatureRow row,
        double specificVolume,
        out RowInterpolation interpolation)
    {
        var points = row.Points;
        if (specificVolume > points[0].SpecificVolume * (1d + 1e-10d)
            || specificVolume < points[^1].SpecificVolume * (1d - 1e-10d))
        {
            interpolation = default;
            return false;
        }

        for (var index = 0; index < points.Count - 1; index++)
        {
            var left = points[index];
            var right = points[index + 1];
            if (specificVolume > left.SpecificVolume || specificVolume < right.SpecificVolume) continue;

            var denominator = right.SpecificVolume - left.SpecificVolume;
            var fraction = Math.Abs(denominator) <= 1e-30d
                ? 0d
                : Math.Clamp((specificVolume - left.SpecificVolume) / denominator, 0d, 1d);
            interpolation = new RowInterpolation(
                row.TemperatureKelvins,
                left.PressureMegapascals + (fraction * (right.PressureMegapascals - left.PressureMegapascals)),
                left.SpecificEnergy + (fraction * (right.SpecificEnergy - left.SpecificEnergy)));
            return true;
        }

        interpolation = default;
        return false;
    }

    private static bool IsEnergyRoot(double residual, double targetEnergy)
        => Math.Abs(residual) <= Math.Max(1e-5d, Math.Abs(targetEnergy) * 1e-8d);

    private static bool HasSignChange(double left, double right)
        => Math.Sign(left) != Math.Sign(right);

    private static SurrogateTablePoint ToTablePoint(IapwsReferenceState state)
        => new(
            state.PressureMegapascals,
            state.SpecificVolumeCubicMetresPerKilogram,
            state.SpecificInternalEnergyJoulesPerKilogram);

    private static IEnumerable<double> TemperatureGrid(double minimum, double maximum, double step)
    {
        for (var value = minimum; value < maximum - 1e-12d; value += step)
        {
            yield return value;
        }

        yield return maximum;
    }

    private readonly record struct SurrogateTablePoint(
        double PressureMegapascals,
        double SpecificVolume,
        double SpecificEnergy);

    private sealed record SurrogateTemperatureRow(
        double TemperatureKelvins,
        ReadOnlyCollection<SurrogateTablePoint> Points);

    private readonly record struct RowInterpolation(
        double TemperatureKelvins,
        double PressureMegapascals,
        double SpecificEnergy);
}

/// <summary>
/// RP1B Refinement 1 / D2. Test-only reference comparator. C2 supplies a bounded phase-aware seed;
/// direct IF97 equations are then used to refine the seed. D1 remains immutable evidence.
/// </summary>
internal sealed class Rp1bSeamCompleteIf97ComparatorCandidate : IRp1bShadowThermodynamicCandidate
{
    private readonly Rp1bExtendedTabulatedReferenceSurrogateCandidate _seed = new();

    public string CandidateId => "D2-SEAM-COMPLETE-IF97-COMPARATOR";
    public string FamilyId => "D-BOUNDED-PRODUCTION-IF97-SUBSET-COMPARATOR";
    public int InitializationReferencePointCount => _seed.InitializationReferencePointCount;
    public int MaximumIterativeSolveIterations => 1_500;
    public bool UsesDirectIf97AtResolveTime => true;

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

        if (_seed.TryResolve(specificVolumeCubicMetresPerKilogram, specificInternalEnergyJoulesPerKilogram, out var seed))
        {
            if (seed.Phase == "SaturatedMixture"
                && TryRefineMixture(
                    specificVolumeCubicMetresPerKilogram,
                    specificInternalEnergyJoulesPerKilogram,
                    seed.TemperatureCelsius + 273.15d,
                    out state))
            {
                return true;
            }

            if (seed.Phase == "SubcooledLiquid"
                && TryRefineSinglePhase(
                    specificVolumeCubicMetresPerKilogram,
                    specificInternalEnergyJoulesPerKilogram,
                    seed.TemperatureCelsius + 273.15d,
                    seed.PressureMegapascals,
                    isLiquid: true,
                    out state))
            {
                return true;
            }

            if (seed.Phase == "SuperheatedVapor"
                && TryRefineSinglePhase(
                    specificVolumeCubicMetresPerKilogram,
                    specificInternalEnergyJoulesPerKilogram,
                    seed.TemperatureCelsius + 273.15d,
                    seed.PressureMegapascals,
                    isLiquid: false,
                    out state))
            {
                return true;
            }
        }

        if (IapwsIf97Reference.TryResolveSaturatedMixtureFromSpecificVolumeAndInternalEnergy(
                specificVolumeCubicMetresPerKilogram,
                specificInternalEnergyJoulesPerKilogram,
                out var mixture))
        {
            state = ToState(mixture, "SaturatedMixture");
            return true;
        }

        if (IapwsIf97Reference.TryResolveRegion1FromSpecificVolumeAndInternalEnergy(
                specificVolumeCubicMetresPerKilogram,
                specificInternalEnergyJoulesPerKilogram,
                out var liquid))
        {
            state = ToState(liquid, "SubcooledLiquid");
            return true;
        }

        if (TryResolveRegion2Fallback(
                specificVolumeCubicMetresPerKilogram,
                specificInternalEnergyJoulesPerKilogram,
                out state))
        {
            return true;
        }

        state = default;
        return false;
    }

    private static bool TryResolveRegion2Fallback(double targetVolume, double targetEnergy, out Rp1bShadowState state)
    {
        const double waterVaporGasConstantJoulesPerKilogramKelvin = 461.526d;
        for (var temperature = 273.15d; temperature <= 1_073.15d + 1e-12d; temperature += 25d)
        {
            var idealPressure = (waterVaporGasConstantJoulesPerKilogramKelvin * temperature / targetVolume) / 1_000_000d;
            var boundedPressure = BoundPressureForRegion(temperature, idealPressure, isLiquid: false);
            if (TryRefineSinglePhase(
                    targetVolume,
                    targetEnergy,
                    temperature,
                    boundedPressure,
                    isLiquid: false,
                    out state))
            {
                return true;
            }
        }

        state = default;
        return false;
    }

    private static bool TryRefineMixture(
        double targetVolume,
        double targetEnergy,
        double initialTemperature,
        out Rp1bShadowState state)
    {
        var temperature = Math.Clamp(initialTemperature, 273.15d, 623.15d);
        for (var iteration = 0; iteration < 24; iteration++)
        {
            if (!TryEvaluateMixture(temperature, targetVolume, targetEnergy, out var residual, out var quality, out var pressure))
            {
                break;
            }

            var tolerance = Math.Max(1e-5d, Math.Abs(targetEnergy) * 1e-10d);
            if (Math.Abs(residual) <= tolerance)
            {
                state = new Rp1bShadowState(
                    "REGION-4-MIXTURE",
                    "SaturatedMixture",
                    temperature - 273.15d,
                    pressure,
                    quality);
                return true;
            }

            const double delta = 0.005d;
            var lowerTemperature = Math.Max(273.15d, temperature - delta);
            var upperTemperature = Math.Min(623.15d, temperature + delta);
            if (upperTemperature <= lowerTemperature
                || !TryEvaluateMixture(lowerTemperature, targetVolume, targetEnergy, out var lowerResidual, out _, out _)
                || !TryEvaluateMixture(upperTemperature, targetVolume, targetEnergy, out var upperResidual, out _, out _))
            {
                break;
            }

            var derivative = (upperResidual - lowerResidual) / (upperTemperature - lowerTemperature);
            if (!double.IsFinite(derivative) || Math.Abs(derivative) <= 1e-12d) break;
            var update = Math.Clamp(residual / derivative, -2d, 2d);
            temperature = Math.Clamp(temperature - update, 273.15d, 623.15d);
        }

        if (IapwsIf97Reference.TryResolveSaturatedMixtureFromSpecificVolumeAndInternalEnergy(
                targetVolume,
                targetEnergy,
                out var fallback))
        {
            state = ToState(fallback, "SaturatedMixture");
            return true;
        }

        state = default;
        return false;
    }

    private static bool TryRefineSinglePhase(
        double targetVolume,
        double targetEnergy,
        double initialTemperature,
        double initialPressure,
        bool isLiquid,
        out Rp1bShadowState state)
    {
        var minimumTemperature = 273.15d;
        var maximumTemperature = isLiquid ? 623.15d : 1_073.15d;
        var temperature = Math.Clamp(initialTemperature, minimumTemperature, maximumTemperature);
        var pressure = Math.Clamp(initialPressure, 1e-6d, isLiquid ? 100d : 20d);

        for (var iteration = 0; iteration < 32; iteration++)
        {
            pressure = BoundPressureForRegion(temperature, pressure, isLiquid);
            var current = isLiquid
                ? IapwsIf97Reference.Region1(temperature, pressure)
                : IapwsIf97Reference.Region2(temperature, pressure);
            var volumeResidual = current.SpecificVolumeCubicMetresPerKilogram - targetVolume;
            var energyResidual = current.SpecificInternalEnergyJoulesPerKilogram - targetEnergy;
            var volumeTolerance = Math.Max(1e-14d, targetVolume * 1e-10d);
            var energyTolerance = Math.Max(1e-5d, Math.Abs(targetEnergy) * 1e-10d);
            if (Math.Abs(volumeResidual) <= volumeTolerance && Math.Abs(energyResidual) <= energyTolerance)
            {
                state = new Rp1bShadowState(
                    isLiquid ? "REGION-1" : "REGION-2",
                    isLiquid ? "SubcooledLiquid" : "SuperheatedVapor",
                    temperature - 273.15d,
                    pressure,
                    null);
                return true;
            }

            const double deltaTemperature = 0.01d;
            var deltaPressure = Math.Max(1e-6d, pressure * 5e-5d);
            var temperaturePlus = Math.Min(maximumTemperature, temperature + deltaTemperature);
            var temperatureMinus = Math.Max(minimumTemperature, temperature - deltaTemperature);
            var pressurePlus = BoundPressureForRegion(temperature, pressure + deltaPressure, isLiquid);
            var pressureMinus = BoundPressureForRegion(temperature, Math.Max(1e-6d, pressure - deltaPressure), isLiquid);
            if (temperaturePlus <= temperatureMinus || pressurePlus <= pressureMinus) break;

            var stateTPlus = isLiquid
                ? IapwsIf97Reference.Region1(temperaturePlus, BoundPressureForRegion(temperaturePlus, pressure, isLiquid))
                : IapwsIf97Reference.Region2(temperaturePlus, BoundPressureForRegion(temperaturePlus, pressure, isLiquid));
            var stateTMinus = isLiquid
                ? IapwsIf97Reference.Region1(temperatureMinus, BoundPressureForRegion(temperatureMinus, pressure, isLiquid))
                : IapwsIf97Reference.Region2(temperatureMinus, BoundPressureForRegion(temperatureMinus, pressure, isLiquid));
            var statePPlus = isLiquid
                ? IapwsIf97Reference.Region1(temperature, pressurePlus)
                : IapwsIf97Reference.Region2(temperature, pressurePlus);
            var statePMinus = isLiquid
                ? IapwsIf97Reference.Region1(temperature, pressureMinus)
                : IapwsIf97Reference.Region2(temperature, pressureMinus);

            var dvDt = (stateTPlus.SpecificVolumeCubicMetresPerKilogram - stateTMinus.SpecificVolumeCubicMetresPerKilogram)
                / (temperaturePlus - temperatureMinus);
            var duDt = (stateTPlus.SpecificInternalEnergyJoulesPerKilogram - stateTMinus.SpecificInternalEnergyJoulesPerKilogram)
                / (temperaturePlus - temperatureMinus);
            var dvDp = (statePPlus.SpecificVolumeCubicMetresPerKilogram - statePMinus.SpecificVolumeCubicMetresPerKilogram)
                / (pressurePlus - pressureMinus);
            var duDp = (statePPlus.SpecificInternalEnergyJoulesPerKilogram - statePMinus.SpecificInternalEnergyJoulesPerKilogram)
                / (pressurePlus - pressureMinus);
            var determinant = (dvDt * duDp) - (dvDp * duDt);
            if (!double.IsFinite(determinant) || Math.Abs(determinant) <= 1e-18d) break;

            var deltaT = ((-volumeResidual * duDp) + (dvDp * energyResidual)) / determinant;
            var deltaP = ((duDt * volumeResidual) - (dvDt * energyResidual)) / determinant;
            if (!double.IsFinite(deltaT) || !double.IsFinite(deltaP)) break;

            temperature = Math.Clamp(
                temperature + Math.Clamp(deltaT, -5d, 5d),
                minimumTemperature,
                maximumTemperature);
            pressure = BoundPressureForRegion(
                temperature,
                pressure + Math.Clamp(deltaP, -5d, 5d),
                isLiquid);
        }

        if (isLiquid
            && IapwsIf97Reference.TryResolveRegion1FromSpecificVolumeAndInternalEnergy(
                targetVolume,
                targetEnergy,
                out var liquid))
        {
            state = ToState(liquid, "SubcooledLiquid");
            return true;
        }

        state = default;
        return false;
    }

    private static double BoundPressureForRegion(double temperatureKelvins, double pressureMegapascals, bool isLiquid)
    {
        if (isLiquid)
        {
            var minimum = IapwsIf97Reference.SaturationPressureMegapascals(
                Math.Clamp(temperatureKelvins, 273.15d, 623.15d));
            return Math.Clamp(pressureMegapascals, minimum, 100d);
        }

        var maximum = Rp1bIf97Domain.Region2MaximumPressureMegapascals(temperatureKelvins);
        return Math.Clamp(pressureMegapascals, 1e-6d, Math.Max(1e-6d, maximum));
    }

    private static bool TryEvaluateMixture(
        double temperatureKelvins,
        double targetVolume,
        double targetEnergy,
        out double energyResidual,
        out double quality,
        out double pressureMegapascals)
    {
        pressureMegapascals = IapwsIf97Reference.SaturationPressureMegapascals(temperatureKelvins);
        var liquid = IapwsIf97Reference.Region1(temperatureKelvins, pressureMegapascals);
        var vapor = IapwsIf97Reference.Region2(temperatureKelvins, pressureMegapascals);
        var denominator = vapor.SpecificVolumeCubicMetresPerKilogram - liquid.SpecificVolumeCubicMetresPerKilogram;
        if (!(denominator > 0d))
        {
            energyResidual = double.NaN;
            quality = double.NaN;
            return false;
        }

        quality = (targetVolume - liquid.SpecificVolumeCubicMetresPerKilogram) / denominator;
        if (quality < -1e-9d || quality > 1d + 1e-9d)
        {
            energyResidual = double.NaN;
            return false;
        }

        quality = Math.Clamp(quality, 0d, 1d);
        var modeledEnergy = liquid.SpecificInternalEnergyJoulesPerKilogram
            + (quality * (vapor.SpecificInternalEnergyJoulesPerKilogram - liquid.SpecificInternalEnergyJoulesPerKilogram));
        energyResidual = modeledEnergy - targetEnergy;
        return double.IsFinite(energyResidual);
    }

    private static Rp1bShadowState ToState(IapwsInverseReferenceState state, string phase)
        => new(
            state.Region,
            phase,
            state.TemperatureKelvins - 273.15d,
            state.PressureMegapascals,
            state.VaporQuality);
}

internal sealed class Rp1bRefinementSaturationTable
{
    private readonly ReadOnlyCollection<Rp1bRefinementSaturationNode> _nodes;

    private Rp1bRefinementSaturationTable(ReadOnlyCollection<Rp1bRefinementSaturationNode> nodes)
    {
        _nodes = nodes;
    }

    public ReadOnlyCollection<Rp1bRefinementSaturationNode> Nodes => _nodes;

    public static Rp1bRefinementSaturationTable Build(double stepKelvins)
    {
        var nodes = new List<Rp1bRefinementSaturationNode>();
        for (var temperature = 273.15d; temperature < 623.15d - 1e-12d; temperature += stepKelvins)
        {
            nodes.Add(CreateNode(temperature));
        }

        nodes.Add(CreateNode(623.15d));
        return new Rp1bRefinementSaturationTable(nodes.AsReadOnly());
    }

    public bool TryResolveMixture(double specificVolume, double targetEnergy, out Rp1bShadowState state)
    {
        var previousNode = _nodes[0];
        var previousReachable = TryMixtureResidual(previousNode, specificVolume, targetEnergy, out var previousResidual, out _);
        if (previousReachable && IsEnergyRoot(previousResidual, targetEnergy))
        {
            state = ToMixtureState(previousNode, specificVolume);
            return true;
        }

        for (var index = 1; index < _nodes.Count; index++)
        {
            var currentNode = _nodes[index];
            var currentReachable = TryMixtureResidual(currentNode, specificVolume, targetEnergy, out var currentResidual, out _);

            if (previousReachable && currentReachable)
            {
                if (IsEnergyRoot(currentResidual, targetEnergy))
                {
                    state = ToMixtureState(currentNode, specificVolume);
                    return true;
                }

                if (HasSignChange(previousResidual, currentResidual)
                    && TryBisectMixture(previousNode, currentNode, previousResidual, specificVolume, targetEnergy, out state))
                {
                    return true;
                }
            }
            else if (!previousReachable && currentReachable)
            {
                if (TryBuildReachabilityBoundary(previousNode, currentNode, specificVolume, out var boundary)
                    && TryMixtureResidual(boundary, specificVolume, targetEnergy, out var boundaryResidual, out _))
                {
                    if (IsEnergyRoot(boundaryResidual, targetEnergy))
                    {
                        state = ToMixtureState(boundary, specificVolume);
                        return true;
                    }

                    if (HasSignChange(boundaryResidual, currentResidual)
                        && TryBisectMixture(boundary, currentNode, boundaryResidual, specificVolume, targetEnergy, out state))
                    {
                        return true;
                    }
                }
            }
            else if (previousReachable && !currentReachable)
            {
                if (TryBuildReachabilityBoundary(previousNode, currentNode, specificVolume, out var boundary)
                    && TryMixtureResidual(boundary, specificVolume, targetEnergy, out var boundaryResidual, out _))
                {
                    if (IsEnergyRoot(boundaryResidual, targetEnergy))
                    {
                        state = ToMixtureState(boundary, specificVolume);
                        return true;
                    }

                    if (HasSignChange(previousResidual, boundaryResidual)
                        && TryBisectMixture(previousNode, boundary, previousResidual, specificVolume, targetEnergy, out state))
                    {
                        return true;
                    }
                }
            }

            previousNode = currentNode;
            previousReachable = currentReachable;
            previousResidual = currentResidual;
        }

        state = default;
        return false;
    }

    public bool TryInterpolateByLiquidEnergy(double liquidEnergy, out Rp1bRefinementSaturationNode node)
        => TryInterpolateByEnergy(liquidEnergy, useVaporEnergy: false, out node);

    public bool TryInterpolateByVaporEnergy(double vaporEnergy, out Rp1bRefinementSaturationNode node)
        => TryInterpolateByEnergy(vaporEnergy, useVaporEnergy: true, out node);

    private bool TryInterpolateByEnergy(double energy, bool useVaporEnergy, out Rp1bRefinementSaturationNode node)
    {
        var lower = 0;
        var upper = _nodes.Count - 1;
        var lowerEnergy = useVaporEnergy ? _nodes[lower].VaporSpecificEnergy : _nodes[lower].LiquidSpecificEnergy;
        var upperEnergy = useVaporEnergy ? _nodes[upper].VaporSpecificEnergy : _nodes[upper].LiquidSpecificEnergy;
        if (energy < Math.Min(lowerEnergy, upperEnergy) || energy > Math.Max(lowerEnergy, upperEnergy))
        {
            node = default;
            return false;
        }

        while (upper - lower > 1)
        {
            var middle = (lower + upper) / 2;
            var middleEnergy = useVaporEnergy ? _nodes[middle].VaporSpecificEnergy : _nodes[middle].LiquidSpecificEnergy;
            if (middleEnergy <= energy) lower = middle;
            else upper = middle;
        }

        var left = _nodes[lower];
        var right = _nodes[upper];
        var leftEnergy = useVaporEnergy ? left.VaporSpecificEnergy : left.LiquidSpecificEnergy;
        var rightEnergy = useVaporEnergy ? right.VaporSpecificEnergy : right.LiquidSpecificEnergy;
        var denominator = rightEnergy - leftEnergy;
        var fraction = Math.Abs(denominator) <= 1e-30d
            ? 0d
            : Math.Clamp((energy - leftEnergy) / denominator, 0d, 1d);
        node = Interpolate(left, right, fraction);
        return true;
    }

    private static bool TryBisectMixture(
        Rp1bRefinementSaturationNode lower,
        Rp1bRefinementSaturationNode upper,
        double lowerResidual,
        double specificVolume,
        double targetEnergy,
        out Rp1bShadowState state)
    {
        var resolved = lower;
        for (var iteration = 0; iteration < 48; iteration++)
        {
            var middle = Interpolate(lower, upper, 0.5d);
            if (!TryMixtureResidual(middle, specificVolume, targetEnergy, out var middleResidual, out _))
            {
                state = default;
                return false;
            }

            resolved = middle;
            if (IsEnergyRoot(middleResidual, targetEnergy))
            {
                state = ToMixtureState(resolved, specificVolume);
                return true;
            }

            if (HasSignChange(lowerResidual, middleResidual))
            {
                upper = middle;
            }
            else
            {
                lower = middle;
                lowerResidual = middleResidual;
            }
        }

        state = ToMixtureState(resolved, specificVolume);
        return true;
    }

    private static bool TryBuildReachabilityBoundary(
        Rp1bRefinementSaturationNode left,
        Rp1bRefinementSaturationNode right,
        double specificVolume,
        out Rp1bRefinementSaturationNode boundary)
    {
        var fractions = new List<double>(capacity: 2);
        AddCrossingFraction(left.LiquidSpecificVolume, right.LiquidSpecificVolume, specificVolume, fractions);
        AddCrossingFraction(left.VaporSpecificVolume, right.VaporSpecificVolume, specificVolume, fractions);

        foreach (var fraction in fractions.OrderBy(static value => value))
        {
            var candidate = Interpolate(left, right, fraction);
            var tolerance = Math.Max(1e-14d, Math.Abs(specificVolume) * 1e-10d);
            if (specificVolume >= candidate.LiquidSpecificVolume - tolerance
                && specificVolume <= candidate.VaporSpecificVolume + tolerance)
            {
                boundary = candidate;
                return true;
            }
        }

        boundary = default;
        return false;
    }

    private static void AddCrossingFraction(double left, double right, double target, List<double> fractions)
    {
        var leftDelta = target - left;
        var rightDelta = target - right;
        if (leftDelta == 0d)
        {
            fractions.Add(0d);
            return;
        }

        if (rightDelta == 0d)
        {
            fractions.Add(1d);
            return;
        }

        if (Math.Sign(leftDelta) == Math.Sign(rightDelta)) return;
        var denominator = right - left;
        if (Math.Abs(denominator) <= 1e-30d) return;
        fractions.Add(Math.Clamp((target - left) / denominator, 0d, 1d));
    }

    private static Rp1bRefinementSaturationNode CreateNode(double temperatureKelvins)
    {
        var pressure = IapwsIf97Reference.SaturationPressureMegapascals(temperatureKelvins);
        var liquid = IapwsIf97Reference.Region1(temperatureKelvins, pressure);
        var vapor = IapwsIf97Reference.Region2(temperatureKelvins, pressure);

        var liquidProbePressure = Math.Min(100d, pressure + 0.25d);
        var compressed = IapwsIf97Reference.Region1(temperatureKelvins, liquidProbePressure);
        var saturatedDensity = liquid.DensityKilogramsPerCubicMetre;
        var compressedDensity = compressed.DensityKilogramsPerCubicMetre;
        var compression = (compressedDensity / saturatedDensity) - 1d;
        var bulkModulus = compression > 0d
            ? ((liquidProbePressure - pressure) * 1_000_000d) / compression
            : double.NaN;

        var vaporProbePressure = Math.Max(1e-6d, pressure * 0.99d);
        var expandedVapor = IapwsIf97Reference.Region2(temperatureKelvins, vaporProbePressure);
        var volumeDelta = expandedVapor.SpecificVolumeCubicMetresPerKilogram - vapor.SpecificVolumeCubicMetresPerKilogram;
        var vaporSlope = Math.Abs(volumeDelta) > 1e-30d
            ? (vaporProbePressure - pressure) / volumeDelta
            : double.NaN;

        return new Rp1bRefinementSaturationNode(
            temperatureKelvins,
            pressure,
            liquid.SpecificVolumeCubicMetresPerKilogram,
            liquid.SpecificInternalEnergyJoulesPerKilogram,
            vapor.SpecificVolumeCubicMetresPerKilogram,
            vapor.SpecificInternalEnergyJoulesPerKilogram,
            bulkModulus,
            vaporSlope);
    }

    private static bool TryMixtureResidual(
        Rp1bRefinementSaturationNode node,
        double specificVolume,
        double targetEnergy,
        out double residual,
        out double quality)
    {
        var tolerance = Math.Max(1e-14d, Math.Abs(specificVolume) * 1e-10d);
        if (specificVolume < node.LiquidSpecificVolume - tolerance
            || specificVolume > node.VaporSpecificVolume + tolerance)
        {
            residual = double.NaN;
            quality = double.NaN;
            return false;
        }

        quality = Math.Clamp(
            (specificVolume - node.LiquidSpecificVolume)
                / (node.VaporSpecificVolume - node.LiquidSpecificVolume),
            0d,
            1d);
        var predictedEnergy = node.LiquidSpecificEnergy
            + (quality * (node.VaporSpecificEnergy - node.LiquidSpecificEnergy));
        residual = predictedEnergy - targetEnergy;
        return double.IsFinite(residual);
    }

    private static bool IsEnergyRoot(double residual, double targetEnergy)
        => Math.Abs(residual) <= Math.Max(1e-5d, Math.Abs(targetEnergy) * 1e-10d);

    private static bool HasSignChange(double left, double right)
        => Math.Sign(left) != Math.Sign(right);

    private static Rp1bShadowState ToMixtureState(
        Rp1bRefinementSaturationNode node,
        double specificVolume)
    {
        var quality = Math.Clamp(
            (specificVolume - node.LiquidSpecificVolume)
                / (node.VaporSpecificVolume - node.LiquidSpecificVolume),
            0d,
            1d);
        return new Rp1bShadowState(
            "REGION-4-MIXTURE-TABLE-C2",
            "SaturatedMixture",
            node.TemperatureKelvins - 273.15d,
            node.PressureMegapascals,
            quality);
    }

    private static Rp1bRefinementSaturationNode Interpolate(
        Rp1bRefinementSaturationNode left,
        Rp1bRefinementSaturationNode right,
        double fraction)
        => new(
            Lerp(left.TemperatureKelvins, right.TemperatureKelvins, fraction),
            Lerp(left.PressureMegapascals, right.PressureMegapascals, fraction),
            Lerp(left.LiquidSpecificVolume, right.LiquidSpecificVolume, fraction),
            Lerp(left.LiquidSpecificEnergy, right.LiquidSpecificEnergy, fraction),
            Lerp(left.VaporSpecificVolume, right.VaporSpecificVolume, fraction),
            Lerp(left.VaporSpecificEnergy, right.VaporSpecificEnergy, fraction),
            Lerp(left.NearBoundaryLiquidBulkModulusPascals, right.NearBoundaryLiquidBulkModulusPascals, fraction),
            Lerp(left.NearBoundaryVaporPressurePerSpecificVolumeSlope, right.NearBoundaryVaporPressurePerSpecificVolumeSlope, fraction));

    private static double Lerp(double left, double right, double fraction)
        => left + (fraction * (right - left));
}

internal readonly record struct Rp1bRefinementSaturationNode(
    double TemperatureKelvins,
    double PressureMegapascals,
    double LiquidSpecificVolume,
    double LiquidSpecificEnergy,
    double VaporSpecificVolume,
    double VaporSpecificEnergy,
    double NearBoundaryLiquidBulkModulusPascals,
    double NearBoundaryVaporPressurePerSpecificVolumeSlope);
