using System.Collections.ObjectModel;

namespace NuclearReactorSimulator.Simulation.Tests.Physics.Fluids.Reference;

internal interface IRp1bShadowThermodynamicCandidate
{
    string CandidateId { get; }
    string FamilyId { get; }
    int InitializationReferencePointCount { get; }
    int MaximumIterativeSolveIterations { get; }
    bool UsesDirectIf97AtResolveTime { get; }

    bool TryResolve(
        double specificVolumeCubicMetresPerKilogram,
        double specificInternalEnergyJoulesPerKilogram,
        out Rp1bShadowState state);
}

internal readonly record struct Rp1bShadowState(
    string Region,
    string Phase,
    double TemperatureCelsius,
    double PressureMegapascals,
    double? VaporQuality);

internal sealed class Rp1bPiecewiseReducedCandidate : IRp1bShadowThermodynamicCandidate
{
    private const double MinimumTemperatureKelvins = 273.15d;
    private const double MaximumVaporTemperatureKelvins = 1_073.15d;
    private const double WaterVaporGasConstantJoulesPerKilogramKelvin = 461.526d;
    private const double VaporSpecificHeatJoulesPerKilogramKelvin = 1_700d;
    private readonly SaturationTable _saturation;

    public Rp1bPiecewiseReducedCandidate()
    {
        _saturation = SaturationTable.Build(stepKelvins: 2d, includeBulkModulus: true);
    }

    public string CandidateId => "B1-PIECEWISE-REDUCED";
    public string FamilyId => "B-PIECEWISE-REFERENCE-CONSISTENT-REDUCED-CLOSURE";
    public int InitializationReferencePointCount => _saturation.Nodes.Count;
    public int MaximumIterativeSolveIterations => 48;
    public bool UsesDirectIf97AtResolveTime => false;

    public bool TryResolve(double specificVolumeCubicMetresPerKilogram, double specificInternalEnergyJoulesPerKilogram, out Rp1bShadowState state)
    {
        if (!IsValidInput(specificVolumeCubicMetresPerKilogram, specificInternalEnergyJoulesPerKilogram))
        {
            state = default;
            return false;
        }

        if (_saturation.TryResolveMixture(specificVolumeCubicMetresPerKilogram, specificInternalEnergyJoulesPerKilogram, out state))
        {
            return true;
        }

        if (TryResolveReducedLiquid(specificVolumeCubicMetresPerKilogram, specificInternalEnergyJoulesPerKilogram, out state))
        {
            return true;
        }

        return TryResolveReducedVapor(specificVolumeCubicMetresPerKilogram, specificInternalEnergyJoulesPerKilogram, out state);
    }

    private bool TryResolveReducedLiquid(double specificVolume, double specificEnergy, out Rp1bShadowState state)
    {
        if (!_saturation.TryInterpolateByLiquidEnergy(specificEnergy, out var saturation))
        {
            state = default;
            return false;
        }

        var actualDensity = 1d / specificVolume;
        var saturatedDensity = 1d / saturation.LiquidSpecificVolume;
        if (actualDensity + (saturatedDensity * 1e-10d) < saturatedDensity)
        {
            state = default;
            return false;
        }

        var compressionRatio = Math.Max(0d, (actualDensity / saturatedDensity) - 1d);
        var bulkModulus = saturation.EffectiveLiquidBulkModulusPascals;
        if (!double.IsFinite(bulkModulus) || bulkModulus <= 0d)
        {
            state = default;
            return false;
        }

        var pressureMegapascals = saturation.PressureMegapascals + ((bulkModulus * compressionRatio) / 1_000_000d);
        if (!double.IsFinite(pressureMegapascals)
            || pressureMegapascals < saturation.PressureMegapascals * (1d - 1e-10d)
            || pressureMegapascals > 100d)
        {
            state = default;
            return false;
        }

        state = new Rp1bShadowState(
            "REGION-1-REDUCED",
            "SubcooledLiquid",
            saturation.TemperatureKelvins - 273.15d,
            pressureMegapascals,
            null);
        return true;
    }

    private bool TryResolveReducedVapor(double specificVolume, double specificEnergy, out Rp1bShadowState state)
    {
        const int scanSegments = 48;
        var previousFound = false;
        var previousTemperature = 0d;
        var previousResidual = 0d;

        for (var index = 0; index <= scanSegments; index++)
        {
            var temperature = MinimumTemperatureKelvins
                + ((MaximumVaporTemperatureKelvins - MinimumTemperatureKelvins) * index / scanSegments);
            if (!TryEvaluateReducedVapor(temperature, specificVolume, specificEnergy, out var pressure, out var residual))
            {
                previousFound = false;
                continue;
            }

            if (IsEnergyRoot(residual, specificEnergy))
            {
                state = new Rp1bShadowState("REGION-2-REDUCED", "SuperheatedVapor", temperature - 273.15d, pressure, null);
                return true;
            }

            if (previousFound && HasSignChange(previousResidual, residual))
            {
                var lowerTemperature = previousTemperature;
                var upperTemperature = temperature;
                var lowerResidual = previousResidual;
                var resolvedPressure = pressure;
                var resolvedTemperature = temperature;

                for (var iteration = 0; iteration < 48; iteration++)
                {
                    var middleTemperature = 0.5d * (lowerTemperature + upperTemperature);
                    if (!TryEvaluateReducedVapor(middleTemperature, specificVolume, specificEnergy, out var middlePressure, out var middleResidual))
                    {
                        state = default;
                        return false;
                    }

                    resolvedTemperature = middleTemperature;
                    resolvedPressure = middlePressure;
                    if (IsEnergyRoot(middleResidual, specificEnergy))
                    {
                        break;
                    }

                    if (HasSignChange(lowerResidual, middleResidual))
                    {
                        upperTemperature = middleTemperature;
                    }
                    else
                    {
                        lowerTemperature = middleTemperature;
                        lowerResidual = middleResidual;
                    }
                }

                state = new Rp1bShadowState(
                    "REGION-2-REDUCED",
                    "SuperheatedVapor",
                    resolvedTemperature - 273.15d,
                    resolvedPressure,
                    null);
                return true;
            }

            previousFound = true;
            previousTemperature = temperature;
            previousResidual = residual;
        }

        state = default;
        return false;
    }

    private bool TryEvaluateReducedVapor(double temperatureKelvins, double specificVolume, double targetEnergy, out double pressureMegapascals, out double residual)
    {
        pressureMegapascals = (WaterVaporGasConstantJoulesPerKilogramKelvin * temperatureKelvins / specificVolume) / 1_000_000d;
        if (!double.IsFinite(pressureMegapascals) || pressureMegapascals <= 0d || pressureMegapascals > 20d)
        {
            residual = double.NaN;
            return false;
        }

        if (!_saturation.TryInterpolateByPressure(pressureMegapascals, out var saturation))
        {
            residual = double.NaN;
            return false;
        }

        if (temperatureKelvins + 1e-10d < saturation.TemperatureKelvins)
        {
            residual = double.NaN;
            return false;
        }

        var modeledEnergy = saturation.VaporSpecificEnergy
            + (VaporSpecificHeatJoulesPerKilogramKelvin * (temperatureKelvins - saturation.TemperatureKelvins));
        residual = modeledEnergy - targetEnergy;
        return double.IsFinite(residual);
    }

    private static bool IsValidInput(double specificVolume, double specificEnergy)
        => double.IsFinite(specificVolume) && specificVolume > 0d && double.IsFinite(specificEnergy);

    private static bool IsEnergyRoot(double residual, double targetEnergy)
        => Math.Abs(residual) <= Math.Max(1e-5d, Math.Abs(targetEnergy) * 1e-10d);

    private static bool HasSignChange(double left, double right)
        => Math.Sign(left) != Math.Sign(right);
}

internal sealed class Rp1bTabulatedReferenceSurrogateCandidate : IRp1bShadowThermodynamicCandidate
{
    private static readonly double[] LiquidPressureNodesMegapascals = [0.1d, 0.2d, 0.5d, 1d, 2d, 5d, 7d, 10d, 15d, 20d, 30d, 50d, 75d, 100d];
    private static readonly double[] VaporPressureNodesMegapascals = [0.01d, 0.02d, 0.05d, 0.1d, 0.2d, 0.5d, 1d, 2d, 5d, 10d, 15d, 20d];

    private readonly SaturationTable _saturation;
    private readonly ReadOnlyCollection<TableTemperatureRow> _liquidRows;
    private readonly ReadOnlyCollection<TableTemperatureRow> _vaporRows;
    private readonly int _referencePointCount;

    public Rp1bTabulatedReferenceSurrogateCandidate()
    {
        _saturation = SaturationTable.Build(stepKelvins: 1d, includeBulkModulus: false);
        _liquidRows = BuildLiquidRows();
        _vaporRows = BuildVaporRows();
        _referencePointCount = _saturation.Nodes.Count
            + _liquidRows.Sum(static row => row.Points.Count)
            + _vaporRows.Sum(static row => row.Points.Count);
    }

    public string CandidateId => "C1-TABULATED-SURROGATE";
    public string FamilyId => "C-BOUNDED-IF97-DERIVED-TABLE-SURROGATE";
    public int InitializationReferencePointCount => _referencePointCount;
    // Includes the Region-4 saturation-table bisection ceiling used before single-phase table interpolation.
    public int MaximumIterativeSolveIterations => 32;
    public bool UsesDirectIf97AtResolveTime => false;

    public bool TryResolve(double specificVolumeCubicMetresPerKilogram, double specificInternalEnergyJoulesPerKilogram, out Rp1bShadowState state)
    {
        if (!double.IsFinite(specificVolumeCubicMetresPerKilogram)
            || specificVolumeCubicMetresPerKilogram <= 0d
            || !double.IsFinite(specificInternalEnergyJoulesPerKilogram))
        {
            state = default;
            return false;
        }

        if (_saturation.TryResolveMixture(specificVolumeCubicMetresPerKilogram, specificInternalEnergyJoulesPerKilogram, out state))
        {
            return true;
        }

        if (TryResolveFromTable(_liquidRows, specificVolumeCubicMetresPerKilogram, specificInternalEnergyJoulesPerKilogram, "REGION-1-TABLE", "SubcooledLiquid", out state))
        {
            return true;
        }

        return TryResolveFromTable(_vaporRows, specificVolumeCubicMetresPerKilogram, specificInternalEnergyJoulesPerKilogram, "REGION-2-TABLE", "SuperheatedVapor", out state);
    }

    private static ReadOnlyCollection<TableTemperatureRow> BuildLiquidRows()
    {
        var rows = new List<TableTemperatureRow>();
        foreach (var temperature in TemperatureGrid(273.15d, 623.15d, 5d))
        {
            var saturationPressure = IapwsIf97Reference.SaturationPressureMegapascals(temperature);
            var points = new List<TablePoint>
            {
                ToTablePoint(IapwsIf97Reference.Region1(temperature, saturationPressure))
            };

            foreach (var pressure in LiquidPressureNodesMegapascals)
            {
                if (pressure <= saturationPressure * (1d + 1e-12d)) continue;
                points.Add(ToTablePoint(IapwsIf97Reference.Region1(temperature, pressure)));
            }

            if (points.Count >= 2)
            {
                rows.Add(new TableTemperatureRow(temperature, points.OrderByDescending(static point => point.SpecificVolume).ToList().AsReadOnly()));
            }
        }

        return rows.AsReadOnly();
    }

    private static ReadOnlyCollection<TableTemperatureRow> BuildVaporRows()
    {
        var rows = new List<TableTemperatureRow>();
        foreach (var temperature in TemperatureGrid(273.15d, 1_073.15d, 10d))
        {
            var maximumPressure = Rp1bIf97Domain.Region2MaximumPressureMegapascals(temperature);
            var points = new List<TablePoint>();

            foreach (var pressure in VaporPressureNodesMegapascals)
            {
                if (pressure > maximumPressure * (1d + 1e-12d)) continue;
                points.Add(ToTablePoint(IapwsIf97Reference.Region2(temperature, pressure)));
            }

            if (maximumPressure <= 20d
                && !points.Any(point => Math.Abs(point.PressureMegapascals - maximumPressure) <= 1e-12d))
            {
                points.Add(ToTablePoint(IapwsIf97Reference.Region2(temperature, maximumPressure)));
            }

            if (points.Count >= 2)
            {
                rows.Add(new TableTemperatureRow(temperature, points.OrderByDescending(static point => point.SpecificVolume).ToList().AsReadOnly()));
            }
        }

        return rows.AsReadOnly();
    }

    private static bool TryResolveFromTable(
        IReadOnlyList<TableTemperatureRow> rows,
        double specificVolume,
        double specificEnergy,
        string region,
        string phase,
        out Rp1bShadowState state)
    {
        var previousFound = false;
        var previous = default(RowInterpolation);

        foreach (var row in rows)
        {
            if (!TryInterpolateAtSpecificVolume(row, specificVolume, out var current))
            {
                previousFound = false;
                continue;
            }

            var currentResidual = current.SpecificEnergy - specificEnergy;
            if (Math.Abs(currentResidual) <= Math.Max(1e-5d, Math.Abs(specificEnergy) * 1e-8d))
            {
                state = new Rp1bShadowState(region, phase, row.TemperatureKelvins - 273.15d, current.PressureMegapascals, null);
                return true;
            }

            if (previousFound)
            {
                var previousResidual = previous.SpecificEnergy - specificEnergy;
                if (Math.Sign(previousResidual) != Math.Sign(currentResidual))
                {
                    var denominator = currentResidual - previousResidual;
                    var fraction = Math.Abs(denominator) <= 1e-30d ? 0.5d : Math.Clamp(-previousResidual / denominator, 0d, 1d);
                    var temperature = previous.TemperatureKelvins + (fraction * (row.TemperatureKelvins - previous.TemperatureKelvins));
                    var pressure = previous.PressureMegapascals + (fraction * (current.PressureMegapascals - previous.PressureMegapascals));
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

    private static bool TryInterpolateAtSpecificVolume(TableTemperatureRow row, double specificVolume, out RowInterpolation interpolation)
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

    private static TablePoint ToTablePoint(IapwsReferenceState state)
        => new(state.PressureMegapascals, state.SpecificVolumeCubicMetresPerKilogram, state.SpecificInternalEnergyJoulesPerKilogram);

    private static IEnumerable<double> TemperatureGrid(double minimum, double maximum, double step)
    {
        for (var value = minimum; value < maximum - 1e-12d; value += step)
        {
            yield return value;
        }
        yield return maximum;
    }

    private readonly record struct TablePoint(double PressureMegapascals, double SpecificVolume, double SpecificEnergy);
    private sealed record TableTemperatureRow(double TemperatureKelvins, ReadOnlyCollection<TablePoint> Points);
    private readonly record struct RowInterpolation(double TemperatureKelvins, double PressureMegapascals, double SpecificEnergy);
}

internal sealed class Rp1bBoundedIf97SubsetCandidate : IRp1bShadowThermodynamicCandidate
{
    private readonly Rp1bTabulatedReferenceSurrogateCandidate _seed = new();

    public string CandidateId => "D1-BOUNDED-IF97-SUBSET";
    public string FamilyId => "D-BOUNDED-PRODUCTION-IF97-SUBSET-COMPARATOR";
    public int InitializationReferencePointCount => _seed.InitializationReferencePointCount;
    // Conservative ceiling including refinement, bounded inverse-reference scans, reachability search and bisection fallbacks.
    public int MaximumIterativeSolveIterations => 1_200;
    public bool UsesDirectIf97AtResolveTime => true;

    public bool TryResolve(double specificVolumeCubicMetresPerKilogram, double specificInternalEnergyJoulesPerKilogram, out Rp1bShadowState state)
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
                && TryRefineMixture(specificVolumeCubicMetresPerKilogram, specificInternalEnergyJoulesPerKilogram, seed.TemperatureCelsius + 273.15d, out state))
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

        state = default;
        return false;
    }

    private static bool TryRefineMixture(double targetVolume, double targetEnergy, double initialTemperature, out Rp1bShadowState state)
    {
        var temperature = Math.Clamp(initialTemperature, 273.15d, 623.15d);
        for (var iteration = 0; iteration < 20; iteration++)
        {
            if (!TryEvaluateMixture(temperature, targetVolume, targetEnergy, out var residual, out var quality, out var pressure))
            {
                break;
            }

            var tolerance = Math.Max(1e-5d, Math.Abs(targetEnergy) * 1e-10d);
            if (Math.Abs(residual) <= tolerance)
            {
                state = new Rp1bShadowState("REGION-4-MIXTURE", "SaturatedMixture", temperature - 273.15d, pressure, quality);
                return true;
            }

            const double delta = 0.01d;
            var lowerTemperature = Math.Max(273.15d, temperature - delta);
            var upperTemperature = Math.Min(623.15d, temperature + delta);
            if (!TryEvaluateMixture(lowerTemperature, targetVolume, targetEnergy, out var lowerResidual, out _, out _)
                || !TryEvaluateMixture(upperTemperature, targetVolume, targetEnergy, out var upperResidual, out _, out _))
            {
                break;
            }

            var derivative = (upperResidual - lowerResidual) / (upperTemperature - lowerTemperature);
            if (!double.IsFinite(derivative) || Math.Abs(derivative) <= 1e-12d) break;
            var update = Math.Clamp(residual / derivative, -5d, 5d);
            temperature = Math.Clamp(temperature - update, 273.15d, 623.15d);
        }

        if (IapwsIf97Reference.TryResolveSaturatedMixtureFromSpecificVolumeAndInternalEnergy(targetVolume, targetEnergy, out var fallback))
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
        var pressure = Math.Clamp(initialPressure, 0.000611213d, isLiquid ? 100d : 20d);

        for (var iteration = 0; iteration < 20; iteration++)
        {
            pressure = BoundPressureForRegion(temperature, pressure, isLiquid);
            var current = isLiquid
                ? IapwsIf97Reference.Region1(temperature, pressure)
                : IapwsIf97Reference.Region2(temperature, pressure);
            var volumeResidual = current.SpecificVolumeCubicMetresPerKilogram - targetVolume;
            var energyResidual = current.SpecificInternalEnergyJoulesPerKilogram - targetEnergy;
            var volumeTolerance = Math.Max(1e-14d, targetVolume * 1e-9d);
            var energyTolerance = Math.Max(1e-4d, Math.Abs(targetEnergy) * 1e-9d);
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

            var deltaTemperature = 0.02d;
            var deltaPressure = Math.Max(1e-5d, pressure * 1e-4d);
            var temperaturePlus = Math.Min(maximumTemperature, temperature + deltaTemperature);
            var temperatureMinus = Math.Max(minimumTemperature, temperature - deltaTemperature);
            var pressurePlus = BoundPressureForRegion(temperature, pressure + deltaPressure, isLiquid);
            var pressureMinus = BoundPressureForRegion(temperature, Math.Max(0.000611213d, pressure - deltaPressure), isLiquid);
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

            var dvDt = (stateTPlus.SpecificVolumeCubicMetresPerKilogram - stateTMinus.SpecificVolumeCubicMetresPerKilogram) / (temperaturePlus - temperatureMinus);
            var duDt = (stateTPlus.SpecificInternalEnergyJoulesPerKilogram - stateTMinus.SpecificInternalEnergyJoulesPerKilogram) / (temperaturePlus - temperatureMinus);
            var dvDp = (statePPlus.SpecificVolumeCubicMetresPerKilogram - statePMinus.SpecificVolumeCubicMetresPerKilogram) / (pressurePlus - pressureMinus);
            var duDp = (statePPlus.SpecificInternalEnergyJoulesPerKilogram - statePMinus.SpecificInternalEnergyJoulesPerKilogram) / (pressurePlus - pressureMinus);
            var determinant = (dvDt * duDp) - (dvDp * duDt);
            if (!double.IsFinite(determinant) || Math.Abs(determinant) <= 1e-18d) break;

            var deltaT = ((-volumeResidual * duDp) + (dvDp * energyResidual)) / determinant;
            var deltaP = ((duDt * volumeResidual) - (dvDt * energyResidual)) / determinant;
            if (!double.IsFinite(deltaT) || !double.IsFinite(deltaP)) break;

            temperature = Math.Clamp(temperature + Math.Clamp(deltaT, -10d, 10d), minimumTemperature, maximumTemperature);
            pressure = BoundPressureForRegion(temperature, pressure + Math.Clamp(deltaP, -10d, 10d), isLiquid);
        }

        if (isLiquid
            && IapwsIf97Reference.TryResolveRegion1FromSpecificVolumeAndInternalEnergy(targetVolume, targetEnergy, out var liquid))
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
            var minimum = IapwsIf97Reference.SaturationPressureMegapascals(Math.Clamp(temperatureKelvins, 273.15d, 623.15d));
            return Math.Clamp(pressureMegapascals, minimum, 100d);
        }

        var maximum = Rp1bIf97Domain.Region2MaximumPressureMegapascals(temperatureKelvins);
        return Math.Clamp(pressureMegapascals, 0.000611213d, Math.Max(0.000611213d, maximum));
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
        if (quality < -1e-10d || quality > 1d + 1e-10d)
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
        => new(state.Region, phase, state.TemperatureKelvins - 273.15d, state.PressureMegapascals, state.VaporQuality);
}

internal static class Rp1bIf97Domain
{
    public static double Region2MaximumPressureMegapascals(double temperatureKelvins)
    {
        if (temperatureKelvins <= 623.15d)
        {
            return IapwsIf97Reference.SaturationPressureMegapascals(temperatureKelvins);
        }

        if (temperatureKelvins <= 863.15d)
        {
            var boundary23 = 348.05185628969d
                - (1.1671859879975d * temperatureKelvins)
                + (0.0010192970039326d * temperatureKelvins * temperatureKelvins);
            return Math.Min(20d, boundary23);
        }

        return 20d;
    }
}

internal sealed class SaturationTable
{
    private readonly ReadOnlyCollection<SaturationNode> _nodes;

    private SaturationTable(ReadOnlyCollection<SaturationNode> nodes)
    {
        _nodes = nodes;
    }

    public ReadOnlyCollection<SaturationNode> Nodes => _nodes;

    public static SaturationTable Build(double stepKelvins, bool includeBulkModulus)
    {
        var nodes = new List<SaturationNode>();
        for (var temperature = 273.15d; temperature < 623.15d - 1e-12d; temperature += stepKelvins)
        {
            nodes.Add(CreateNode(temperature, includeBulkModulus));
        }
        nodes.Add(CreateNode(623.15d, includeBulkModulus));
        return new SaturationTable(nodes.AsReadOnly());
    }

    public bool TryResolveMixture(double specificVolume, double specificEnergy, out Rp1bShadowState state)
    {
        var previousFound = false;
        var previousIndex = -1;
        var previousResidual = 0d;

        for (var index = 0; index < _nodes.Count; index++)
        {
            if (!TryMixtureResidual(_nodes[index], specificVolume, specificEnergy, out var residual, out _))
            {
                previousFound = false;
                continue;
            }

            if (Math.Abs(residual) <= Math.Max(1e-5d, Math.Abs(specificEnergy) * 1e-9d))
            {
                var node = _nodes[index];
                var quality = QualityAt(node, specificVolume);
                state = new Rp1bShadowState("REGION-4-MIXTURE-TABLE", "SaturatedMixture", node.TemperatureKelvins - 273.15d, node.PressureMegapascals, quality);
                return true;
            }

            if (previousFound && Math.Sign(previousResidual) != Math.Sign(residual))
            {
                var lower = _nodes[previousIndex];
                var upper = _nodes[index];
                var lowerResidual = previousResidual;
                var resolved = lower;

                for (var iteration = 0; iteration < 32; iteration++)
                {
                    var fraction = 0.5d;
                    var middle = Interpolate(lower, upper, fraction);
                    if (!TryMixtureResidual(middle, specificVolume, specificEnergy, out var middleResidual, out _))
                    {
                        state = default;
                        return false;
                    }

                    resolved = middle;
                    if (Math.Abs(middleResidual) <= Math.Max(1e-5d, Math.Abs(specificEnergy) * 1e-9d))
                    {
                        break;
                    }

                    if (Math.Sign(lowerResidual) != Math.Sign(middleResidual))
                    {
                        upper = middle;
                    }
                    else
                    {
                        lower = middle;
                        lowerResidual = middleResidual;
                    }
                }

                var quality = QualityAt(resolved, specificVolume);
                state = new Rp1bShadowState(
                    "REGION-4-MIXTURE-TABLE",
                    "SaturatedMixture",
                    resolved.TemperatureKelvins - 273.15d,
                    resolved.PressureMegapascals,
                    quality);
                return true;
            }

            previousFound = true;
            previousIndex = index;
            previousResidual = residual;
        }

        state = default;
        return false;
    }

    public bool TryInterpolateByLiquidEnergy(double liquidEnergy, out SaturationNode node)
    {
        for (var index = 0; index < _nodes.Count - 1; index++)
        {
            var left = _nodes[index];
            var right = _nodes[index + 1];
            if (liquidEnergy < left.LiquidSpecificEnergy || liquidEnergy > right.LiquidSpecificEnergy) continue;
            var denominator = right.LiquidSpecificEnergy - left.LiquidSpecificEnergy;
            var fraction = Math.Abs(denominator) <= 1e-30d ? 0d : Math.Clamp((liquidEnergy - left.LiquidSpecificEnergy) / denominator, 0d, 1d);
            node = Interpolate(left, right, fraction);
            return true;
        }

        node = default;
        return false;
    }

    public bool TryInterpolateByPressure(double pressureMegapascals, out SaturationNode node)
    {
        if (pressureMegapascals < _nodes[0].PressureMegapascals || pressureMegapascals > _nodes[^1].PressureMegapascals)
        {
            node = default;
            return false;
        }

        var lower = 0;
        var upper = _nodes.Count - 1;
        while (upper - lower > 1)
        {
            var middle = (lower + upper) / 2;
            if (_nodes[middle].PressureMegapascals <= pressureMegapascals) lower = middle;
            else upper = middle;
        }

        var left = _nodes[lower];
        var right = _nodes[upper];
        var denominator = right.PressureMegapascals - left.PressureMegapascals;
        var fraction = Math.Abs(denominator) <= 1e-30d ? 0d : Math.Clamp((pressureMegapascals - left.PressureMegapascals) / denominator, 0d, 1d);
        node = Interpolate(left, right, fraction);
        return true;
    }

    private static SaturationNode CreateNode(double temperatureKelvins, bool includeBulkModulus)
    {
        var pressure = IapwsIf97Reference.SaturationPressureMegapascals(temperatureKelvins);
        var liquid = IapwsIf97Reference.Region1(temperatureKelvins, pressure);
        var vapor = IapwsIf97Reference.Region2(temperatureKelvins, pressure);
        var bulkModulus = double.NaN;

        if (includeBulkModulus)
        {
            var probePressure = Math.Min(100d, pressure + 10d);
            if (probePressure > pressure * (1d + 1e-12d))
            {
                var compressed = IapwsIf97Reference.Region1(temperatureKelvins, probePressure);
                var saturatedDensity = liquid.DensityKilogramsPerCubicMetre;
                var compressedDensity = compressed.DensityKilogramsPerCubicMetre;
                var compression = (compressedDensity / saturatedDensity) - 1d;
                if (compression > 0d)
                {
                    bulkModulus = ((probePressure - pressure) * 1_000_000d) / compression;
                }
            }
        }

        return new SaturationNode(
            temperatureKelvins,
            pressure,
            liquid.SpecificVolumeCubicMetresPerKilogram,
            liquid.SpecificInternalEnergyJoulesPerKilogram,
            vapor.SpecificVolumeCubicMetresPerKilogram,
            vapor.SpecificInternalEnergyJoulesPerKilogram,
            bulkModulus);
    }

    private static bool TryMixtureResidual(SaturationNode node, double specificVolume, double targetEnergy, out double residual, out double quality)
    {
        if (specificVolume < node.LiquidSpecificVolume || specificVolume > node.VaporSpecificVolume)
        {
            residual = double.NaN;
            quality = double.NaN;
            return false;
        }

        quality = QualityAt(node, specificVolume);
        var predictedEnergy = node.LiquidSpecificEnergy
            + (quality * (node.VaporSpecificEnergy - node.LiquidSpecificEnergy));
        residual = predictedEnergy - targetEnergy;
        return double.IsFinite(residual);
    }

    private static double QualityAt(SaturationNode node, double specificVolume)
        => Math.Clamp(
            (specificVolume - node.LiquidSpecificVolume) / (node.VaporSpecificVolume - node.LiquidSpecificVolume),
            0d,
            1d);

    private static SaturationNode Interpolate(SaturationNode left, SaturationNode right, double fraction)
        => new(
            Lerp(left.TemperatureKelvins, right.TemperatureKelvins, fraction),
            Lerp(left.PressureMegapascals, right.PressureMegapascals, fraction),
            Lerp(left.LiquidSpecificVolume, right.LiquidSpecificVolume, fraction),
            Lerp(left.LiquidSpecificEnergy, right.LiquidSpecificEnergy, fraction),
            Lerp(left.VaporSpecificVolume, right.VaporSpecificVolume, fraction),
            Lerp(left.VaporSpecificEnergy, right.VaporSpecificEnergy, fraction),
            InterpolateOptionalPositive(left.EffectiveLiquidBulkModulusPascals, right.EffectiveLiquidBulkModulusPascals, fraction));

    private static double InterpolateOptionalPositive(double left, double right, double fraction)
    {
        if (double.IsFinite(left) && left > 0d && double.IsFinite(right) && right > 0d) return Lerp(left, right, fraction);
        if (double.IsFinite(left) && left > 0d) return left;
        if (double.IsFinite(right) && right > 0d) return right;
        return double.NaN;
    }

    private static double Lerp(double left, double right, double fraction) => left + (fraction * (right - left));
}

internal readonly record struct SaturationNode(
    double TemperatureKelvins,
    double PressureMegapascals,
    double LiquidSpecificVolume,
    double LiquidSpecificEnergy,
    double VaporSpecificVolume,
    double VaporSpecificEnergy,
    double EffectiveLiquidBulkModulusPascals);
