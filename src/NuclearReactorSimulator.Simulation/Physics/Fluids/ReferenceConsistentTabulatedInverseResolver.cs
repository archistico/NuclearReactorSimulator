using System.Security.Cryptography;
using System.Text;
using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Simulation.Physics.Fluids;

/// <summary>
/// Production-local, opt-in inverse-domain resolver for the selected RP1C C4 repair.
/// All IF97-derived data is loaded once from the versioned embedded payload; resolve-time
/// execution performs no resource I/O, payload decoding or payload-driven allocation.
/// </summary>
internal sealed class ReferenceConsistentTabulatedInverseResolver
{
    internal const string PayloadLogicalName = "NuclearReactorSimulator.Simulation.Physics.Fluids.ReferenceData.NRSVR2C4.v1.bin";
    internal const string ExpectedPayloadSha256 = "EF49B1D097FC63F1F1254E425F46C6ACA837B82C58EFBA9C7774727C51C82267";
    internal const int ExpectedSchemaVersion = 1;
    private const int EndianMarker = 0x01020304;
    private const int ExpectedDenseSaturationCount = 17502;
    private const int ExpectedPrefixSaturationCount = 701;
    private const int ExpectedLiquidRowCount = 351;
    private const int ExpectedVaporRowCount = 401;
    private const double MinimumBoundaryEnergyMarginJoulesPerKilogram = 0.05d;
    private const double MaximumBoundaryEnergyMarginJoulesPerKilogram = 100d;

    private static readonly Lazy<PayloadData> ProcessPayload = new(
        LoadPayload,
        LazyThreadSafetyMode.ExecutionAndPublication);

    private static int _payloadLoadCount;
    private readonly PayloadData _payload;

    internal ReferenceConsistentTabulatedInverseResolver()
    {
        _payload = ProcessPayload.Value;
    }

    internal static int PayloadLoadCount => Volatile.Read(ref _payloadLoadCount);
    internal static int ResolveTimeResourceIoCount => 0;
    internal static int ResolveTimePayloadDecodeCount => 0;

    internal bool TryResolve(
        double specificVolumeCubicMetresPerKilogram,
        double specificInternalEnergyJoulesPerKilogram,
        out ResolvedState state)
        => TryResolveWithPath(
            specificVolumeCubicMetresPerKilogram,
            specificInternalEnergyJoulesPerKilogram,
            out state,
            out _);

    internal bool TryResolveWithPath(
        double specificVolume,
        double specificEnergy,
        out ResolvedState state,
        out ResolutionPath path)
    {
        if (!double.IsFinite(specificVolume) || specificVolume <= 0d || !double.IsFinite(specificEnergy))
        {
            state = default;
            path = ResolutionPath.Unresolved;
            return false;
        }

        if (TryResolveNearVaporBoundary(specificVolume, specificEnergy, true, out state))
        {
            path = ResolutionPath.C3SuperheatedVaporSeam;
            return true;
        }

        if (TryResolveMixture(_payload.PrefixSaturation, specificVolume, specificEnergy, allocationNeutralBoundary: true, out state))
        {
            path = ResolutionPath.C2MixturePrefix;
            return true;
        }

        if (TryResolveFromTable(_payload.LiquidRows, specificVolume, specificEnergy, FluidPhase.SubcooledLiquid, 3.1d, out state))
        {
            path = ResolutionPath.C2LiquidTablePrefix;
            return true;
        }

        if (TryResolveNearBoundaryLiquid(specificVolume, specificEnergy, out state))
        {
            path = ResolutionPath.C2NearBoundaryLiquidPrefix;
            return true;
        }

        if (TryResolveC2Fallback(specificVolume, specificEnergy, out state))
        {
            path = ResolutionPath.ImmutableC2Fallback;
            return true;
        }

        if (TryResolveNearVaporBoundary(specificVolume, specificEnergy, false, out state))
        {
            path = ResolutionPath.C3SaturatedVaporSeamFallback;
            return true;
        }

        state = default;
        path = ResolutionPath.Unresolved;
        return false;
    }

    private bool TryResolveC2Fallback(double specificVolume, double specificEnergy, out ResolvedState state)
    {
        if (TryResolveMixture(_payload.PrefixSaturation, specificVolume, specificEnergy, allocationNeutralBoundary: false, out state))
        {
            return true;
        }

        if (TryResolveFromTable(_payload.LiquidRows, specificVolume, specificEnergy, FluidPhase.SubcooledLiquid, 3.1d, out state))
        {
            return true;
        }

        if (TryResolveNearBoundaryLiquid(specificVolume, specificEnergy, out state))
        {
            return true;
        }

        if (TryResolveFromTable(_payload.VaporRows, specificVolume, specificEnergy, FluidPhase.SuperheatedVapor, 6.1d, out state))
        {
            return true;
        }

        return TryResolveNearBoundaryVapor(specificVolume, specificEnergy, out state);
    }

    private bool TryResolveNearBoundaryLiquid(double specificVolume, double specificEnergy, out ResolvedState state)
    {
        if (!TryInterpolateByEnergy(_payload.PrefixSaturation, specificEnergy, useVaporEnergy: false, out var saturation))
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

        state = new ResolvedState(
            FluidPhase.SubcooledLiquid,
            saturation.TemperatureKelvins - 273.15d,
            pressure,
            null);
        return true;
    }

    private bool TryResolveNearBoundaryVapor(double specificVolume, double specificEnergy, out ResolvedState state)
    {
        if (!TryInterpolateByEnergy(_payload.PrefixSaturation, specificEnergy, useVaporEnergy: true, out var saturation))
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
        var maximumPressure = Region2MaximumPressureMegapascals(saturation.TemperatureKelvins);
        pressure = Math.Clamp(pressure, 1e-6d, maximumPressure);
        if (!double.IsFinite(pressure) || pressure <= 0d)
        {
            state = default;
            return false;
        }

        state = new ResolvedState(
            FluidPhase.SuperheatedVapor,
            saturation.TemperatureKelvins - 273.15d,
            pressure,
            null);
        return true;
    }

    private bool TryResolveNearVaporBoundary(
        double specificVolume,
        double specificEnergy,
        bool requireSuperheatedSide,
        out ResolvedState state)
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

            state = new ResolvedState(
                FluidPhase.SuperheatedVapor,
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

        state = new ResolvedState(
            FluidPhase.SaturatedMixture,
            boundary.TemperatureKelvins - 273.15d,
            boundary.PressureMegapascals,
            1d);
        return true;
    }

    private bool TryInterpolateSaturationByVaporVolume(double specificVolume, out SaturationNode node)
    {
        var nodes = _payload.DenseSaturation;
        if (nodes.Length < 2
            || specificVolume > nodes[0].VaporSpecificVolume
            || specificVolume < nodes[^1].VaporSpecificVolume)
        {
            node = default;
            return false;
        }

        var lower = 0;
        var upper = nodes.Length - 1;
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

        node = new SaturationNode(
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

    private static bool TryResolveFromTable(
        TemperatureRow[] rows,
        double specificVolume,
        double specificEnergy,
        FluidPhase phase,
        double maximumTemperatureGapKelvins,
        out ResolvedState state)
    {
        var previousFound = false;
        var previous = default(RowInterpolation);

        for (var rowIndex = 0; rowIndex < rows.Length; rowIndex++)
        {
            var row = rows[rowIndex];
            if (!TryInterpolateAtSpecificVolume(row, specificVolume, out var current))
            {
                continue;
            }

            var currentResidual = current.SpecificEnergy - specificEnergy;
            if (IsTableEnergyRoot(currentResidual, specificEnergy))
            {
                state = new ResolvedState(
                    phase,
                    row.TemperatureKelvins - 273.15d,
                    current.PressureMegapascals,
                    null);
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
                    state = new ResolvedState(phase, temperature - 273.15d, pressure, null);
                    return double.IsFinite(temperature) && double.IsFinite(pressure) && pressure > 0d;
                }
            }

            previousFound = true;
            previous = new RowInterpolation(
                row.TemperatureKelvins,
                current.PressureMegapascals,
                current.SpecificEnergy);
        }

        state = default;
        return false;
    }

    private static bool TryInterpolateAtSpecificVolume(
        TemperatureRow row,
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
            interpolation = new RowInterpolation(
                row.TemperatureKelvins,
                left.PressureMegapascals + (fraction * (right.PressureMegapascals - left.PressureMegapascals)),
                left.SpecificEnergy + (fraction * (right.SpecificEnergy - left.SpecificEnergy)));
            return true;
        }

        interpolation = default;
        return false;
    }

    private static bool TryResolveMixture(
        SaturationNode[] nodes,
        double specificVolume,
        double targetEnergy,
        bool allocationNeutralBoundary,
        out ResolvedState state)
    {
        var previousNode = nodes[0];
        var previousReachable = TryMixtureResidual(previousNode, specificVolume, targetEnergy, out var previousResidual);
        if (previousReachable && IsMixtureEnergyRoot(previousResidual, targetEnergy))
        {
            state = ToMixtureState(previousNode, specificVolume);
            return true;
        }

        for (var index = 1; index < nodes.Length; index++)
        {
            var currentNode = nodes[index];
            var currentReachable = TryMixtureResidual(currentNode, specificVolume, targetEnergy, out var currentResidual);

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
                var hasBoundary = allocationNeutralBoundary
                    ? TryBuildReachabilityBoundaryAllocationNeutral(previousNode, currentNode, specificVolume, out var boundary)
                    : TryBuildReachabilityBoundary(previousNode, currentNode, specificVolume, out boundary);
                if (hasBoundary
                    && TryMixtureResidual(boundary, specificVolume, targetEnergy, out var boundaryResidual))
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
                var hasBoundary = allocationNeutralBoundary
                    ? TryBuildReachabilityBoundaryAllocationNeutral(previousNode, currentNode, specificVolume, out var boundary)
                    : TryBuildReachabilityBoundary(previousNode, currentNode, specificVolume, out boundary);
                if (hasBoundary
                    && TryMixtureResidual(boundary, specificVolume, targetEnergy, out var boundaryResidual))
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

    private static bool TryInterpolateByEnergy(
        SaturationNode[] nodes,
        double energy,
        bool useVaporEnergy,
        out SaturationNode node)
    {
        var lower = 0;
        var upper = nodes.Length - 1;
        var lowerEnergy = useVaporEnergy ? nodes[lower].VaporSpecificEnergy : nodes[lower].LiquidSpecificEnergy;
        var upperEnergy = useVaporEnergy ? nodes[upper].VaporSpecificEnergy : nodes[upper].LiquidSpecificEnergy;
        if (energy < Math.Min(lowerEnergy, upperEnergy) || energy > Math.Max(lowerEnergy, upperEnergy))
        {
            node = default;
            return false;
        }

        while (upper - lower > 1)
        {
            var middle = (lower + upper) / 2;
            var middleEnergy = useVaporEnergy ? nodes[middle].VaporSpecificEnergy : nodes[middle].LiquidSpecificEnergy;
            if (middleEnergy <= energy)
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
        SaturationNode lower,
        SaturationNode upper,
        double lowerResidual,
        double specificVolume,
        double targetEnergy,
        out ResolvedState state)
    {
        var resolved = lower;
        for (var iteration = 0; iteration < 48; iteration++)
        {
            var middle = Interpolate(lower, upper, 0.5d);
            if (!TryMixtureResidual(middle, specificVolume, targetEnergy, out var middleResidual))
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

    private static bool TryBuildReachabilityBoundary(
        SaturationNode left,
        SaturationNode right,
        double specificVolume,
        out SaturationNode boundary)
    {
        Span<double> fractions = stackalloc double[2];
        var count = 0;
        if (TryGetCrossingFraction(left.LiquidSpecificVolume, right.LiquidSpecificVolume, specificVolume, out var first))
        {
            fractions[count++] = first;
        }
        if (TryGetCrossingFraction(left.VaporSpecificVolume, right.VaporSpecificVolume, specificVolume, out var second))
        {
            fractions[count++] = second;
        }
        if (count == 2 && fractions[1] < fractions[0])
        {
            (fractions[0], fractions[1]) = (fractions[1], fractions[0]);
        }

        for (var index = 0; index < count; index++)
        {
            if (IsReachableBoundaryFraction(left, right, specificVolume, fractions[index], out boundary))
            {
                return true;
            }
        }

        boundary = default;
        return false;
    }

    private static bool TryBuildReachabilityBoundaryAllocationNeutral(
        SaturationNode left,
        SaturationNode right,
        double specificVolume,
        out SaturationNode boundary)
    {
        var hasFirst = TryGetCrossingFraction(left.LiquidSpecificVolume, right.LiquidSpecificVolume, specificVolume, out var first);
        var hasSecond = TryGetCrossingFraction(left.VaporSpecificVolume, right.VaporSpecificVolume, specificVolume, out var second);
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
        SaturationNode left,
        SaturationNode right,
        double specificVolume,
        double fraction,
        out SaturationNode boundary)
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
        SaturationNode node,
        double specificVolume,
        double targetEnergy,
        out double residual)
    {
        var tolerance = Math.Max(1e-14d, Math.Abs(specificVolume) * 1e-10d);
        if (specificVolume < node.LiquidSpecificVolume - tolerance
            || specificVolume > node.VaporSpecificVolume + tolerance)
        {
            residual = double.NaN;
            return false;
        }

        var quality = Math.Clamp(
            (specificVolume - node.LiquidSpecificVolume)
                / (node.VaporSpecificVolume - node.LiquidSpecificVolume),
            0d,
            1d);
        var predictedEnergy = node.LiquidSpecificEnergy
            + (quality * (node.VaporSpecificEnergy - node.LiquidSpecificEnergy));
        residual = predictedEnergy - targetEnergy;
        return double.IsFinite(residual);
    }

    private static ResolvedState ToMixtureState(SaturationNode node, double specificVolume)
    {
        var quality = Math.Clamp(
            (specificVolume - node.LiquidSpecificVolume)
                / (node.VaporSpecificVolume - node.LiquidSpecificVolume),
            0d,
            1d);
        return new ResolvedState(
            FluidPhase.SaturatedMixture,
            node.TemperatureKelvins - 273.15d,
            node.PressureMegapascals,
            quality);
    }

    private static SaturationNode Interpolate(SaturationNode left, SaturationNode right, double fraction)
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

    private static bool IsMixtureEnergyRoot(double residual, double targetEnergy)
        => Math.Abs(residual) <= Math.Max(1e-5d, Math.Abs(targetEnergy) * 1e-10d);

    private static bool IsTableEnergyRoot(double residual, double targetEnergy)
        => Math.Abs(residual) <= Math.Max(1e-5d, Math.Abs(targetEnergy) * 1e-8d);

    private static bool HasSignChange(double left, double right)
        => Math.Sign(left) != Math.Sign(right);

    private static double Region2MaximumPressureMegapascals(double temperatureKelvins)
    {
        if (temperatureKelvins <= 623.15d)
        {
            return SaturationPressureMegapascals(temperatureKelvins);
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

    // Region-4 is retained only as a pure algebraic bound for the existing C2 near-vapor fallback.
    // No direct IF97 Region-1/2 state evaluation exists in production mode 2.
    private static double SaturationPressureMegapascals(double temperatureKelvins)
    {
        ReadOnlySpan<double> n =
        [
            0d,
            0.11670521452767e4d,
            -0.72421316703206e6d,
            -0.17073846940092e2d,
            0.12020824702470e5d,
            -0.32325550322333e7d,
            0.14915108613530e2d,
            -0.48232657361591e4d,
            0.40511340542057e6d,
            -0.23855557567849d,
            0.65017534844798e3d,
        ];

        var theta = temperatureKelvins + (n[9] / (temperatureKelvins - n[10]));
        var a = (theta * theta) + (n[1] * theta) + n[2];
        var b = (n[3] * theta * theta) + (n[4] * theta) + n[5];
        var c = (n[6] * theta * theta) + (n[7] * theta) + n[8];
        return Math.Pow((2d * c) / (-b + Math.Sqrt((b * b) - (4d * a * c))), 4d);
    }

    private static PayloadData LoadPayload()
    {
        Interlocked.Increment(ref _payloadLoadCount);
        using var stream = typeof(ReferenceConsistentTabulatedInverseResolver).Assembly
            .GetManifestResourceStream(PayloadLogicalName)
            ?? throw new InvalidDataException($"Embedded reference payload '{PayloadLogicalName}' was not found.");
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        var bytes = memory.ToArray();
        var actualSha = Convert.ToHexString(SHA256.HashData(bytes));
        if (!string.Equals(actualSha, ExpectedPayloadSha256, StringComparison.Ordinal))
        {
            throw new InvalidDataException($"Embedded reference payload SHA-256 mismatch. Expected {ExpectedPayloadSha256}, actual {actualSha}.");
        }

        using var reader = new BinaryReader(new MemoryStream(bytes, writable: false), Encoding.ASCII, leaveOpen: false);
        var magic = Encoding.ASCII.GetString(reader.ReadBytes(8));
        if (!string.Equals(magic, "NRSVR2C4", StringComparison.Ordinal))
        {
            throw new InvalidDataException("Embedded reference payload magic mismatch.");
        }

        var schema = reader.ReadInt32();
        var endian = reader.ReadInt32();
        var denseCount = reader.ReadInt32();
        var prefixCount = reader.ReadInt32();
        var liquidCount = reader.ReadInt32();
        var vaporCount = reader.ReadInt32();
        if (schema != ExpectedSchemaVersion
            || endian != EndianMarker
            || denseCount != ExpectedDenseSaturationCount
            || prefixCount != ExpectedPrefixSaturationCount
            || liquidCount != ExpectedLiquidRowCount
            || vaporCount != ExpectedVaporRowCount)
        {
            throw new InvalidDataException("Embedded reference payload header/count contract mismatch.");
        }

        var dense = ReadNodes(reader, denseCount);
        var prefix = ReadNodes(reader, prefixCount);
        var liquidRows = ReadRows(reader, liquidCount);
        var vaporRows = ReadRows(reader, vaporCount);
        if (reader.BaseStream.Position != reader.BaseStream.Length)
        {
            throw new InvalidDataException("Embedded reference payload contains trailing bytes.");
        }

        return new PayloadData(dense, prefix, liquidRows, vaporRows);
    }

    private static SaturationNode[] ReadNodes(BinaryReader reader, int count)
    {
        var nodes = new SaturationNode[count];
        for (var index = 0; index < count; index++)
        {
            nodes[index] = new SaturationNode(
                reader.ReadDouble(),
                reader.ReadDouble(),
                reader.ReadDouble(),
                reader.ReadDouble(),
                reader.ReadDouble(),
                reader.ReadDouble(),
                reader.ReadDouble(),
                reader.ReadDouble());
        }
        return nodes;
    }

    private static TemperatureRow[] ReadRows(BinaryReader reader, int count)
    {
        var rows = new TemperatureRow[count];
        for (var rowIndex = 0; rowIndex < count; rowIndex++)
        {
            var temperature = reader.ReadDouble();
            var pointCount = reader.ReadInt32();
            if (pointCount < 2 || pointCount > 64)
            {
                throw new InvalidDataException("Embedded reference payload row point count is invalid.");
            }

            var points = new TablePoint[pointCount];
            for (var pointIndex = 0; pointIndex < pointCount; pointIndex++)
            {
                points[pointIndex] = new TablePoint(
                    reader.ReadDouble(),
                    reader.ReadDouble(),
                    reader.ReadDouble());
            }
            rows[rowIndex] = new TemperatureRow(temperature, points);
        }
        return rows;
    }

    internal readonly record struct ResolvedState(
        FluidPhase Phase,
        double TemperatureCelsius,
        double PressureMegapascals,
        double? VaporQuality)
    {
        internal FluidThermodynamicState ToFluidThermodynamicState()
            => new(
                Pressure.FromMegapascals(PressureMegapascals),
                Temperature.FromDegreesCelsius(TemperatureCelsius),
                Phase,
                VaporQuality.HasValue ? global::NuclearReactorSimulator.Domain.Physics.Fluids.VaporQuality.FromFraction(VaporQuality.Value) : null);
    }

    internal enum ResolutionPath : byte
    {
        Unresolved = 0,
        C3SuperheatedVaporSeam = 1,
        C2MixturePrefix = 2,
        C2LiquidTablePrefix = 3,
        C2NearBoundaryLiquidPrefix = 4,
        ImmutableC2Fallback = 5,
        C3SaturatedVaporSeamFallback = 6,
    }

    private sealed class PayloadData(
        SaturationNode[] denseSaturation,
        SaturationNode[] prefixSaturation,
        TemperatureRow[] liquidRows,
        TemperatureRow[] vaporRows)
    {
        internal SaturationNode[] DenseSaturation { get; } = denseSaturation;
        internal SaturationNode[] PrefixSaturation { get; } = prefixSaturation;
        internal TemperatureRow[] LiquidRows { get; } = liquidRows;
        internal TemperatureRow[] VaporRows { get; } = vaporRows;
    }

    private readonly record struct SaturationNode(
        double TemperatureKelvins,
        double PressureMegapascals,
        double LiquidSpecificVolume,
        double LiquidSpecificEnergy,
        double VaporSpecificVolume,
        double VaporSpecificEnergy,
        double NearBoundaryLiquidBulkModulusPascals,
        double NearBoundaryVaporPressurePerSpecificVolumeSlope);

    private readonly record struct TablePoint(
        double PressureMegapascals,
        double SpecificVolume,
        double SpecificEnergy);

    private sealed record TemperatureRow(
        double TemperatureKelvins,
        TablePoint[] Points);

    private readonly record struct RowInterpolation(
        double TemperatureKelvins,
        double PressureMegapascals,
        double SpecificEnergy);
}
