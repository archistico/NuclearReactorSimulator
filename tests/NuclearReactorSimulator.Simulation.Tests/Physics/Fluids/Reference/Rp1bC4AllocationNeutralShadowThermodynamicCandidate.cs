namespace NuclearReactorSimulator.Simulation.Tests.Physics.Fluids.Reference;

/// <summary>
/// RP1B C4. Test-only allocation-neutral shadow candidate. It preserves C3 vapor-seam precedence,
/// reproduces only the C2 mixture/liquid prefix with indexed, allocation-neutral resolve-time code,
/// then delegates all remaining paths to immutable C2 before the historical C3 saturated-side fallback.
/// Direct IF97 is used only while constructing frozen reference tables, never from TryResolve.
/// </summary>
internal sealed class Rp1bAllocationNeutralVaporSeamCompleteTabulatedSurrogateCandidate : IRp1bShadowThermodynamicCandidate
{
    private const double DenseSaturationStepKelvins = 0.02d;
    private const double PrefixSaturationStepKelvins = 0.5d;
    private const double MinimumBoundaryEnergyMarginJoulesPerKilogram = 0.05d;
    private const double MaximumBoundaryEnergyMarginJoulesPerKilogram = 100d;

    private static readonly double[] LiquidPressureNodesMegapascals =
    [
        0.02d, 0.05d, 0.1d, 0.2d, 0.5d, 1d, 2d, 5d, 7d, 10d, 15d, 20d, 30d, 50d, 75d, 100d,
    ];

    private static readonly double[] LiquidBoundaryOffsetsMegapascals =
    [
        0.001d, 0.005d, 0.02d, 0.1d, 0.5d, 2d, 5d,
    ];

    private readonly Rp1bExtendedTabulatedReferenceSurrogateCandidate _fallback = new();
    private readonly Rp1bRefinementSaturationTable _denseSaturation =
        Rp1bRefinementSaturationTable.Build(DenseSaturationStepKelvins);
    private readonly Rp1bRefinementSaturationTable _prefixSaturation =
        Rp1bRefinementSaturationTable.Build(PrefixSaturationStepKelvins);
    private readonly C4TemperatureRow[] _prefixLiquidRows;
    private readonly int _prefixLiquidReferencePointCount;

    public Rp1bAllocationNeutralVaporSeamCompleteTabulatedSurrogateCandidate()
    {
        _prefixLiquidRows = BuildLiquidRows(out _prefixLiquidReferencePointCount);
    }

    public string CandidateId => "C4-ALLOCATION-NEUTRAL-VAPOR-SEAM-COMPLETE-SURROGATE";
    public string FamilyId => "C-BOUNDED-IF97-DERIVED-TABLE-SURROGATE";
    public int InitializationReferencePointCount => _fallback.InitializationReferencePointCount
        + _denseSaturation.Nodes.Count
        + _prefixSaturation.Nodes.Count
        + _prefixLiquidReferencePointCount;
    public int MaximumIterativeSolveIterations => 48;
    public bool UsesDirectIf97AtResolveTime => false;

    public bool TryResolve(
        double specificVolumeCubicMetresPerKilogram,
        double specificInternalEnergyJoulesPerKilogram,
        out Rp1bShadowState state)
        => TryResolveWithPath(
            specificVolumeCubicMetresPerKilogram,
            specificInternalEnergyJoulesPerKilogram,
            out state,
            out _);

    internal bool TryResolveWithPath(
        double specificVolumeCubicMetresPerKilogram,
        double specificInternalEnergyJoulesPerKilogram,
        out Rp1bShadowState state,
        out Rp1bC4ResolutionPath resolutionPath)
    {
        if (!double.IsFinite(specificVolumeCubicMetresPerKilogram)
            || specificVolumeCubicMetresPerKilogram <= 0d
            || !double.IsFinite(specificInternalEnergyJoulesPerKilogram))
        {
            state = default;
            resolutionPath = Rp1bC4ResolutionPath.Unresolved;
            return false;
        }

        if (TryResolveNearVaporBoundary(
                specificVolumeCubicMetresPerKilogram,
                specificInternalEnergyJoulesPerKilogram,
                requireSuperheatedSide: true,
                out state))
        {
            resolutionPath = Rp1bC4ResolutionPath.C3SuperheatedVaporSeam;
            return true;
        }

        if (TryResolveMixtureAllocationNeutral(
                specificVolumeCubicMetresPerKilogram,
                specificInternalEnergyJoulesPerKilogram,
                out state))
        {
            resolutionPath = Rp1bC4ResolutionPath.C2MixturePrefix;
            return true;
        }

        if (TryResolveLiquidTableAllocationNeutral(
                specificVolumeCubicMetresPerKilogram,
                specificInternalEnergyJoulesPerKilogram,
                out state))
        {
            resolutionPath = Rp1bC4ResolutionPath.C2LiquidTablePrefix;
            return true;
        }

        if (TryResolveNearBoundaryLiquid(
                specificVolumeCubicMetresPerKilogram,
                specificInternalEnergyJoulesPerKilogram,
                out state))
        {
            resolutionPath = Rp1bC4ResolutionPath.C2NearBoundaryLiquidPrefix;
            return true;
        }

        if (_fallback.TryResolve(
                specificVolumeCubicMetresPerKilogram,
                specificInternalEnergyJoulesPerKilogram,
                out state))
        {
            resolutionPath = Rp1bC4ResolutionPath.ImmutableC2Fallback;
            return true;
        }

        if (TryResolveNearVaporBoundary(
                specificVolumeCubicMetresPerKilogram,
                specificInternalEnergyJoulesPerKilogram,
                requireSuperheatedSide: false,
                out state))
        {
            resolutionPath = Rp1bC4ResolutionPath.C3SaturatedVaporSeamFallback;
            return true;
        }

        resolutionPath = Rp1bC4ResolutionPath.Unresolved;
        return false;
    }

    private bool TryResolveMixtureAllocationNeutral(
        double specificVolume,
        double targetEnergy,
        out Rp1bShadowState state)
    {
        var nodes = _prefixSaturation.Nodes;
        var previousNode = nodes[0];
        var previousReachable = TryMixtureResidual(previousNode, specificVolume, targetEnergy, out var previousResidual, out _);
        if (previousReachable && IsMixtureEnergyRoot(previousResidual, targetEnergy))
        {
            state = ToMixtureState(previousNode, specificVolume);
            return true;
        }

        for (var index = 1; index < nodes.Count; index++)
        {
            var currentNode = nodes[index];
            var currentReachable = TryMixtureResidual(currentNode, specificVolume, targetEnergy, out var currentResidual, out _);

            if (previousReachable && currentReachable)
            {
                if (IsMixtureEnergyRoot(currentResidual, targetEnergy))
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
                if (TryBuildReachabilityBoundaryAllocationNeutral(previousNode, currentNode, specificVolume, out var boundary)
                    && TryMixtureResidual(boundary, specificVolume, targetEnergy, out var boundaryResidual, out _))
                {
                    if (IsMixtureEnergyRoot(boundaryResidual, targetEnergy))
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
                if (TryBuildReachabilityBoundaryAllocationNeutral(previousNode, currentNode, specificVolume, out var boundary)
                    && TryMixtureResidual(boundary, specificVolume, targetEnergy, out var boundaryResidual, out _))
                {
                    if (IsMixtureEnergyRoot(boundaryResidual, targetEnergy))
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

    private bool TryResolveLiquidTableAllocationNeutral(
        double specificVolume,
        double specificEnergy,
        out Rp1bShadowState state)
    {
        var previousFound = false;
        var previous = default(C4RowInterpolation);

        for (var rowIndex = 0; rowIndex < _prefixLiquidRows.Length; rowIndex++)
        {
            var row = _prefixLiquidRows[rowIndex];
            if (!TryInterpolateAtSpecificVolume(row, specificVolume, out var current))
            {
                continue;
            }

            var currentResidual = current.SpecificEnergy - specificEnergy;
            if (IsTableEnergyRoot(currentResidual, specificEnergy))
            {
                state = new Rp1bShadowState(
                    "REGION-1-TABLE-C2",
                    "SubcooledLiquid",
                    row.TemperatureKelvins - 273.15d,
                    current.PressureMegapascals,
                    null);
                return true;
            }

            if (previousFound)
            {
                var temperatureGap = row.TemperatureKelvins - previous.TemperatureKelvins;
                var previousResidual = previous.SpecificEnergy - specificEnergy;
                if (temperatureGap <= 3.1d && HasSignChange(previousResidual, currentResidual))
                {
                    var denominator = currentResidual - previousResidual;
                    var fraction = Math.Abs(denominator) <= 1e-30d
                        ? 0.5d
                        : Math.Clamp(-previousResidual / denominator, 0d, 1d);
                    var temperature = previous.TemperatureKelvins
                        + (fraction * (row.TemperatureKelvins - previous.TemperatureKelvins));
                    var pressure = previous.PressureMegapascals
                        + (fraction * (current.PressureMegapascals - previous.PressureMegapascals));
                    state = new Rp1bShadowState(
                        "REGION-1-TABLE-C2",
                        "SubcooledLiquid",
                        temperature - 273.15d,
                        pressure,
                        null);
                    return double.IsFinite(temperature) && double.IsFinite(pressure) && pressure > 0d;
                }
            }

            previousFound = true;
            previous = new C4RowInterpolation(
                row.TemperatureKelvins,
                current.PressureMegapascals,
                current.SpecificEnergy);
        }

        state = default;
        return false;
    }

    private bool TryResolveNearBoundaryLiquid(
        double specificVolume,
        double specificEnergy,
        out Rp1bShadowState state)
    {
        if (!_prefixSaturation.TryInterpolateByLiquidEnergy(specificEnergy, out var saturation))
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
        var nodes = _denseSaturation.Nodes;
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

    private static bool TryInterpolateAtSpecificVolume(
        C4TemperatureRow row,
        double specificVolume,
        out C4RowInterpolation interpolation)
    {
        var points = row.Points;
        if (specificVolume > points[0].SpecificVolume * (1d + 1e-10d)
            || specificVolume < points[^1].SpecificVolume * (1d - 1e-10d))
        {
            interpolation = default;
            return false;
        }

        for (var index = 0; index < points.Length - 1; index++)
        {
            var left = points[index];
            var right = points[index + 1];
            if (specificVolume > left.SpecificVolume || specificVolume < right.SpecificVolume)
            {
                continue;
            }

            var denominator = right.SpecificVolume - left.SpecificVolume;
            var fraction = Math.Abs(denominator) <= 1e-30d
                ? 0d
                : Math.Clamp((specificVolume - left.SpecificVolume) / denominator, 0d, 1d);
            interpolation = new C4RowInterpolation(
                row.TemperatureKelvins,
                left.PressureMegapascals + (fraction * (right.PressureMegapascals - left.PressureMegapascals)),
                left.SpecificEnergy + (fraction * (right.SpecificEnergy - left.SpecificEnergy)));
            return true;
        }

        interpolation = default;
        return false;
    }

    private static C4TemperatureRow[] BuildLiquidRows(out int referencePointCount)
    {
        var rows = new List<C4TemperatureRow>();
        referencePointCount = 0;
        for (var temperature = 273.15d; temperature < 623.15d - 1e-12d; temperature += 1d)
        {
            AddLiquidRow(rows, temperature, ref referencePointCount);
        }

        AddLiquidRow(rows, 623.15d, ref referencePointCount);
        return rows.ToArray();
    }

    private static void AddLiquidRow(List<C4TemperatureRow> rows, double temperature, ref int referencePointCount)
    {
        var saturationPressure = IapwsIf97Reference.SaturationPressureMegapascals(temperature);
        var states = new List<IapwsReferenceState>();
        AddUniqueState(states, IapwsIf97Reference.Region1(temperature, saturationPressure));

        for (var index = 0; index < LiquidBoundaryOffsetsMegapascals.Length; index++)
        {
            var pressure = saturationPressure + LiquidBoundaryOffsetsMegapascals[index];
            if (pressure <= 100d)
            {
                AddUniqueState(states, IapwsIf97Reference.Region1(temperature, pressure));
            }
        }

        for (var index = 0; index < LiquidPressureNodesMegapascals.Length; index++)
        {
            var pressure = LiquidPressureNodesMegapascals[index];
            if (pressure <= saturationPressure * (1d + 1e-12d))
            {
                continue;
            }

            AddUniqueState(states, IapwsIf97Reference.Region1(temperature, pressure));
        }

        if (states.Count < 2)
        {
            return;
        }

        var points = states
            .Select(static source => new C4TablePoint(
                source.PressureMegapascals,
                source.SpecificVolumeCubicMetresPerKilogram,
                source.SpecificInternalEnergyJoulesPerKilogram))
            .OrderByDescending(static point => point.SpecificVolume)
            .ToArray();
        referencePointCount += points.Length;
        rows.Add(new C4TemperatureRow(temperature, points));
    }

    private static void AddUniqueState(List<IapwsReferenceState> states, IapwsReferenceState candidate)
    {
        for (var index = 0; index < states.Count; index++)
        {
            if (Math.Abs(states[index].PressureMegapascals - candidate.PressureMegapascals) <= 1e-12d)
            {
                return;
            }
        }

        states.Add(candidate);
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
            if (IsMixtureEnergyRoot(middleResidual, targetEnergy))
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

    private static bool TryBuildReachabilityBoundaryAllocationNeutral(
        Rp1bRefinementSaturationNode left,
        Rp1bRefinementSaturationNode right,
        double specificVolume,
        out Rp1bRefinementSaturationNode boundary)
    {
        var hasFirst = TryGetCrossingFraction(
            left.LiquidSpecificVolume,
            right.LiquidSpecificVolume,
            specificVolume,
            out var first);
        var hasSecond = TryGetCrossingFraction(
            left.VaporSpecificVolume,
            right.VaporSpecificVolume,
            specificVolume,
            out var second);

        if (hasFirst && hasSecond && second < first)
        {
            (first, second) = (second, first);
        }

        if (hasFirst && IsReachableBoundaryFraction(left, right, specificVolume, first, out boundary))
        {
            return true;
        }

        if (hasSecond && IsReachableBoundaryFraction(left, right, specificVolume, second, out boundary))
        {
            return true;
        }

        boundary = default;
        return false;
    }

    private static bool TryGetCrossingFraction(double left, double right, double target, out double fraction)
    {
        var leftDelta = target - left;
        var rightDelta = target - right;
        if (leftDelta == 0d)
        {
            fraction = 0d;
            return true;
        }

        if (rightDelta == 0d)
        {
            fraction = 1d;
            return true;
        }

        if (Math.Sign(leftDelta) == Math.Sign(rightDelta))
        {
            fraction = default;
            return false;
        }

        var denominator = right - left;
        if (Math.Abs(denominator) <= 1e-30d)
        {
            fraction = default;
            return false;
        }

        fraction = Math.Clamp((target - left) / denominator, 0d, 1d);
        return true;
    }

    private static bool IsReachableBoundaryFraction(
        Rp1bRefinementSaturationNode left,
        Rp1bRefinementSaturationNode right,
        double specificVolume,
        double fraction,
        out Rp1bRefinementSaturationNode boundary)
    {
        var candidate = Interpolate(left, right, fraction);
        var tolerance = Math.Max(1e-14d, Math.Abs(specificVolume) * 1e-10d);
        if (specificVolume >= candidate.LiquidSpecificVolume - tolerance
            && specificVolume <= candidate.VaporSpecificVolume + tolerance)
        {
            boundary = candidate;
            return true;
        }

        boundary = default;
        return false;
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

    private static bool IsMixtureEnergyRoot(double residual, double targetEnergy)
        => Math.Abs(residual) <= Math.Max(1e-5d, Math.Abs(targetEnergy) * 1e-10d);

    private static bool IsTableEnergyRoot(double residual, double targetEnergy)
        => Math.Abs(residual) <= Math.Max(1e-5d, Math.Abs(targetEnergy) * 1e-8d);

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

    private readonly record struct C4TablePoint(
        double PressureMegapascals,
        double SpecificVolume,
        double SpecificEnergy);

    private sealed record C4TemperatureRow(
        double TemperatureKelvins,
        C4TablePoint[] Points);

    private readonly record struct C4RowInterpolation(
        double TemperatureKelvins,
        double PressureMegapascals,
        double SpecificEnergy);
}

internal enum Rp1bC4ResolutionPath : byte
{
    Unresolved = 0,
    C3SuperheatedVaporSeam = 1,
    C2MixturePrefix = 2,
    C2LiquidTablePrefix = 3,
    C2NearBoundaryLiquidPrefix = 4,
    ImmutableC2Fallback = 5,
    C3SaturatedVaporSeamFallback = 6,
}
