using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Simulation.Physics.Fluids;

/// <summary>
/// Educational deterministic water/steam closure for lumped control volumes.
/// It uses the IAPWS-IF97 Region-4 saturation-pressure equation as a reference boundary,
/// combined with deliberately simplified correlations for density and internal energy.
/// It is not a complete IAPWS-IF97 implementation and must not be used for engineering design.
/// </summary>
public sealed class SimplifiedWaterSteamThermodynamicModel : IFluidThermodynamicModel, IWaterSteamSaturationPropertyProvider, IWaterSteamInverseBranchDiagnosticProvider
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

    // H.28.1-D CPU-only optimization: the coarse saturated-mixture scan always visits the exact same
    // 513 temperatures. Cache those immutable saturation properties once so every inverse-map Resolve()
    // preserves the identical scan grid and branch order without repeating the expensive IF97/density
    // correlations for every sample. Dynamic/boundary-aware/bisection temperatures still use the unchanged
    // EvaluateSaturationValue path.
    private static readonly SaturationPropertyValue[] CoarseSaturationScan = BuildCoarseSaturationScan();
    private static readonly double MinimumSaturationPressurePascals = SaturationPressurePascals(TriplePointTemperatureKelvins);
    private static readonly double MaximumSupportedSaturationPressurePascals = SaturationPressurePascals(MaximumSaturationTemperatureKelvins);
    private static readonly double SaturatedLiquidDensityMaximumTemperatureKelvins = FindSaturatedLiquidDensityMaximumTemperatureKelvins();

    private readonly WaterSteamThermodynamicClosureMode _closureMode;

    public SimplifiedWaterSteamThermodynamicModel()
        : this(WaterSteamThermodynamicClosureMode.HistoricalCorrelationTopology)
    {
    }

    public SimplifiedWaterSteamThermodynamicModel(WaterSteamThermodynamicClosureMode closureMode)
    {
        if (!Enum.IsDefined(closureMode))
        {
            throw new ArgumentOutOfRangeException(nameof(closureMode), closureMode, "Unknown simplified water/steam thermodynamic closure mode.");
        }

        _closureMode = closureMode;
    }

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

    internal WaterSteamBranchContinuityEvaluation EvaluateBranchContinuity(
        FluidNodeDefinition definition,
        FluidNodeInventory inventory,
        FluidThermodynamicState previousState)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(inventory);
        ArgumentNullException.ThrowIfNull(previousState);

        var specificVolume = definition.Volume.CubicMetres / inventory.Mass.Kilograms;
        var specificInternalEnergy = inventory.SpecificInternalEnergy.JoulesPerKilogram;
        if (!double.IsFinite(specificVolume) || specificVolume <= 0d || !double.IsFinite(specificInternalEnergy))
        {
            throw new WaterSteamStateOutOfRangeException(definition.Id, specificVolume, specificInternalEnergy);
        }

        var coarseSaturatedFound = TryResolveSaturatedMixture(specificVolume, specificInternalEnergy, out var coarseSaturated);
        FluidThermodynamicState liquid = null!;
        var coarseSuperheatedAttempted = false;
        var coarseSuperheatedFound = false;
        FluidThermodynamicState coarseSuperheated = null!;
        var boundarySaturatedAttempted = false;
        var boundarySaturatedFound = false;
        FluidThermodynamicState boundarySaturated = null!;
        var boundarySuperheatedAttempted = false;
        var boundarySuperheatedFound = false;
        FluidThermodynamicState boundarySuperheated = null!;

        FluidThermodynamicState production;
        if (coarseSaturatedFound)
        {
            production = coarseSaturated;
        }
        else
        {
            if (TryResolveSubcooledLiquid(specificVolume, specificInternalEnergy, out liquid))
            {
                production = liquid;
            }
            else
            {
                coarseSuperheatedAttempted = true;
                coarseSuperheatedFound = TryResolveSuperheatedVaporForContinuity(specificVolume, specificInternalEnergy, out coarseSuperheated);
                if (coarseSuperheatedFound)
                {
                    production = coarseSuperheated;
                }
                else
                {
                    boundarySaturatedAttempted = true;
                    boundarySaturatedFound = TryResolveBoundaryAwareSaturatedMixture(specificVolume, specificInternalEnergy, out boundarySaturated);
                    if (boundarySaturatedFound)
                    {
                        production = boundarySaturated;
                    }
                    else
                    {
                        boundarySuperheatedAttempted = true;
                        boundarySuperheatedFound = TryResolveBoundaryAwareSuperheatedVapor(specificVolume, specificInternalEnergy, out boundarySuperheated);
                        if (!boundarySuperheatedFound)
                        {
                            throw new WaterSteamStateOutOfRangeException(definition.Id, specificVolume, specificInternalEnergy);
                        }

                        production = boundarySuperheated;
                    }
                }
            }
        }

        // Preserve the diagnostic traversal order for branches still needed after production selection.
        if (!coarseSuperheatedAttempted)
        {
            coarseSuperheatedFound = TryResolveSuperheatedVaporForContinuity(specificVolume, specificInternalEnergy, out coarseSuperheated);
        }

        if (!boundarySaturatedAttempted && !coarseSaturatedFound)
        {
            boundarySaturatedFound = TryResolveBoundaryAwareSaturatedMixture(specificVolume, specificInternalEnergy, out boundarySaturated);
        }

        if (!boundarySuperheatedAttempted && !coarseSuperheatedFound)
        {
            boundarySuperheatedFound = TryResolveBoundaryAwareSuperheatedVapor(specificVolume, specificInternalEnergy, out boundarySuperheated);
        }

        var saturatedAvailable = coarseSaturatedFound || boundarySaturatedFound;
        var superheatedAvailable = coarseSuperheatedFound || boundarySuperheatedFound;
        WaterSteamInverseBranchCandidate? previousPhaseCandidate = previousState.Phase switch
        {
            FluidPhase.SaturatedMixture when coarseSaturatedFound => CreateBranchCandidate("coarse-saturated", 1, true, coarseSaturated),
            FluidPhase.SaturatedMixture when boundarySaturatedFound => CreateBranchCandidate("boundary-aware-saturated", 4, true, boundarySaturated),
            FluidPhase.SuperheatedVapor when coarseSuperheatedFound => CreateBranchCandidate("coarse-superheated", 3, true, coarseSuperheated),
            FluidPhase.SuperheatedVapor when boundarySuperheatedFound => CreateBranchCandidate("boundary-aware-superheated", 5, true, boundarySuperheated),
            _ => null,
        };

        return new WaterSteamBranchContinuityEvaluation(
            production,
            saturatedAvailable && superheatedAvailable,
            previousPhaseCandidate);
    }

    /// <summary>
    /// H.28.1-G internal fast path for the four-node untargeted disagreement scan. It returns only the
    /// production-selected phase and the late-boundary-saturated-shadow flag consumed by that scan.
    /// The branch equations and priority remain identical to <see cref="DiagnoseInverseBranchSelection"/>,
    /// but branches that cannot affect those two outputs are not evaluated.
    /// </summary>
    internal WaterSteamBranchDisagreementEvaluation EvaluateBranchDisagreement(
        FluidNodeDefinition definition,
        FluidNodeInventory inventory)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(inventory);

        var specificVolume = definition.Volume.CubicMetres / inventory.Mass.Kilograms;
        var specificInternalEnergy = inventory.SpecificInternalEnergy.JoulesPerKilogram;
        if (!double.IsFinite(specificVolume) || specificVolume <= 0d || !double.IsFinite(specificInternalEnergy))
        {
            throw new WaterSteamStateOutOfRangeException(definition.Id, specificVolume, specificInternalEnergy);
        }

        if (TryResolveSaturatedMixture(specificVolume, specificInternalEnergy, out var coarseSaturated))
        {
            return new WaterSteamBranchDisagreementEvaluation(
                coarseSaturated.Phase,
                LateBoundarySaturatedShadowedByEarlierSuperheated: false);
        }

        var liquidFound = TryResolveSubcooledLiquid(specificVolume, specificInternalEnergy, out var liquid);
        var coarseSuperheatedFound = TryResolveSuperheatedVapor(
            specificVolume,
            specificInternalEnergy,
            out var coarseSuperheated);
        var boundarySaturatedFound = TryResolveBoundaryAwareSaturatedMixture(
            specificVolume,
            specificInternalEnergy,
            out var boundarySaturated);
        var lateBoundarySaturatedShadow = coarseSuperheatedFound && boundarySaturatedFound;

        if (liquidFound)
        {
            return new WaterSteamBranchDisagreementEvaluation(liquid.Phase, lateBoundarySaturatedShadow);
        }

        if (coarseSuperheatedFound)
        {
            return new WaterSteamBranchDisagreementEvaluation(coarseSuperheated.Phase, lateBoundarySaturatedShadow);
        }

        if (boundarySaturatedFound)
        {
            return new WaterSteamBranchDisagreementEvaluation(boundarySaturated.Phase, lateBoundarySaturatedShadow);
        }

        var boundarySuperheatedFound = TryResolveBoundaryAwareSuperheatedVapor(
            specificVolume,
            specificInternalEnergy,
            out var boundarySuperheated);
        return new WaterSteamBranchDisagreementEvaluation(
            boundarySuperheatedFound ? boundarySuperheated.Phase : FluidPhase.Unspecified,
            lateBoundarySaturatedShadow);
    }

    public WaterSteamInverseBranchSelectionDiagnostic DiagnoseInverseBranchSelection(
        FluidNodeDefinition definition,
        FluidNodeInventory inventory,
        FluidThermodynamicState previousState)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(inventory);
        ArgumentNullException.ThrowIfNull(previousState);

        var specificVolume = definition.Volume.CubicMetres / inventory.Mass.Kilograms;
        var specificInternalEnergy = inventory.SpecificInternalEnergy.JoulesPerKilogram;
        if (!double.IsFinite(specificVolume) || specificVolume <= 0d || !double.IsFinite(specificInternalEnergy))
        {
            throw new WaterSteamStateOutOfRangeException(definition.Id, specificVolume, specificInternalEnergy);
        }

        var coarseSaturatedFound = TryResolveSaturatedMixture(specificVolume, specificInternalEnergy, out var coarseSaturated);
        var liquidFound = TryResolveSubcooledLiquid(specificVolume, specificInternalEnergy, out var liquid);
        var coarseSuperheatedFound = TryResolveSuperheatedVapor(specificVolume, specificInternalEnergy, out var coarseSuperheated);
        var boundarySaturatedFound = TryResolveBoundaryAwareSaturatedMixture(specificVolume, specificInternalEnergy, out var boundarySaturated);
        var boundarySuperheatedFound = TryResolveBoundaryAwareSuperheatedVapor(specificVolume, specificInternalEnergy, out var boundarySuperheated);

        var candidates = new[]
        {
            CreateBranchCandidate("coarse-saturated", 1, coarseSaturatedFound, coarseSaturated),
            CreateBranchCandidate("subcooled-liquid", 2, liquidFound, liquid),
            CreateBranchCandidate("coarse-superheated", 3, coarseSuperheatedFound, coarseSuperheated),
            CreateBranchCandidate("boundary-aware-saturated", 4, boundarySaturatedFound, boundarySaturated),
            CreateBranchCandidate("boundary-aware-superheated", 5, boundarySuperheatedFound, boundarySuperheated),
        };
        var selected = candidates.FirstOrDefault(static candidate => candidate.RootFound);
        var saturatedAvailable = coarseSaturatedFound || boundarySaturatedFound;
        var superheatedAvailable = coarseSuperheatedFound || boundarySuperheatedFound;

        return new WaterSteamInverseBranchSelectionDiagnostic(
            definition.Id,
            specificVolume,
            specificInternalEnergy,
            selected?.Branch ?? "none",
            selected?.Phase ?? FluidPhase.Unspecified.ToString(),
            saturatedAvailable,
            superheatedAvailable,
            saturatedAvailable && superheatedAvailable,
            coarseSaturatedFound,
            boundarySaturatedFound,
            coarseSuperheatedFound,
            boundarySuperheatedFound,
            !coarseSaturatedFound && coarseSuperheatedFound && boundarySaturatedFound,
            candidates);
    }

    private static WaterSteamInverseBranchCandidate CreateBranchCandidate(
        string branch,
        int attemptOrder,
        bool rootFound,
        FluidThermodynamicState state)
        => rootFound
            ? new WaterSteamInverseBranchCandidate(
                branch,
                attemptOrder,
                true,
                state.Phase.ToString(),
                state.Pressure.Pascals,
                state.Temperature.Kelvins,
                state.VaporQuality?.Fraction)
            : new WaterSteamInverseBranchCandidate(
                branch,
                attemptOrder,
                false,
                FluidPhase.Unspecified.ToString(),
                double.NaN,
                double.NaN,
                null);

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

    public WaterSteamSaturationProperties GetSaturationProperties(Pressure pressure)
    {
        if (!double.IsFinite(pressure.Pascals) || pressure <= Pressure.Vacuum)
        {
            throw new ArgumentOutOfRangeException(nameof(pressure), pressure, "Saturation pressure must be finite and greater than zero.");
        }

        var saturationTemperatureKelvins = SaturationTemperatureFromPressure(pressure.Pascals);
        if (!saturationTemperatureKelvins.HasValue)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pressure),
                pressure,
                $"Simplified saturation properties are not available above the supported saturation envelope ending at {MaximumSaturationTemperatureKelvins} K.");
        }

        return EvaluateSaturation(saturationTemperatureKelvins.Value);
    }

    private static bool TryResolveSaturatedMixture(
        double specificVolume,
        double specificInternalEnergy,
        out FluidThermodynamicState state)
    {
        SaturatedEvaluation? previous = null;

        for (var index = 0; index <= SearchSegments; index++)
        {
            var saturation = CoarseSaturationScan[index];
            var evaluation = EvaluateSaturatedCandidate(saturation, specificVolume, specificInternalEnergy);

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

    private bool TryResolveBoundaryAwareSaturatedMixture(
        double specificVolume,
        double specificInternalEnergy,
        out FluidThermodynamicState state)
        => _closureMode == WaterSteamThermodynamicClosureMode.CorrelationConsistentInverseDomain
            ? TryResolveIntervalAwareSaturatedMixture(specificVolume, specificInternalEnergy, out state)
            : TryResolveHistoricalBoundaryAwareSaturatedMixture(specificVolume, specificInternalEnergy, out state);

    private static bool TryResolveHistoricalBoundaryAwareSaturatedMixture(
        double specificVolume,
        double specificInternalEnergy,
        out FluidThermodynamicState state)
    {
        if (!TryGetHistoricalSaturatedTemperatureUpperBound(specificVolume, out var maximum))
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

    private static bool TryResolveIntervalAwareSaturatedMixture(
        double specificVolume,
        double specificInternalEnergy,
        out FluidThermodynamicState state)
    {
        if (!TryGetSaturatedTemperatureInterval(specificVolume, out var minimum, out var maximum))
        {
            state = null!;
            return false;
        }

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

            if (previous is not null
                && HasSignChange(previous.Value.ResidualJoulesPerKilogram, evaluation.Value.ResidualJoulesPerKilogram))
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

    private static bool TryGetSaturatedTemperatureInterval(
        double specificVolume,
        out double minimumKelvins,
        out double maximumKelvins)
    {
        var triple = EvaluateSaturationValue(TriplePointTemperatureKelvins);
        var densityMaximum = EvaluateSaturationValue(SaturatedLiquidDensityMaximumTemperatureKelvins);
        var ceiling = EvaluateSaturationValue(MaximumSaturationTemperatureKelvins);

        var minimumLiquidSpecificVolume = densityMaximum.SaturatedLiquidSpecificVolumeCubicMetresPerKilogram;
        var tripleLiquidSpecificVolume = triple.SaturatedLiquidSpecificVolumeCubicMetresPerKilogram;
        var ceilingLiquidSpecificVolume = ceiling.SaturatedLiquidSpecificVolumeCubicMetresPerKilogram;
        var tripleVaporSpecificVolume = triple.SaturatedVaporSpecificVolumeCubicMetresPerKilogram;
        var ceilingVaporSpecificVolume = ceiling.SaturatedVaporSpecificVolumeCubicMetresPerKilogram;

        if (specificVolume < minimumLiquidSpecificVolume || specificVolume > tripleVaporSpecificVolume)
        {
            minimumKelvins = 0d;
            maximumKelvins = 0d;
            return false;
        }

        if (specificVolume >= tripleLiquidSpecificVolume)
        {
            minimumKelvins = TriplePointTemperatureKelvins;
        }
        else
        {
            minimumKelvins = FindSpecificVolumeBoundary(
                TriplePointTemperatureKelvins,
                SaturatedLiquidDensityMaximumTemperatureKelvins,
                specificVolume,
                useLiquidBoundary: true,
                returnUpperSide: true);
        }

        var liquidUpper = specificVolume >= ceilingLiquidSpecificVolume
            ? MaximumSaturationTemperatureKelvins
            : FindSpecificVolumeBoundary(
                SaturatedLiquidDensityMaximumTemperatureKelvins,
                MaximumSaturationTemperatureKelvins,
                specificVolume,
                useLiquidBoundary: true,
                returnUpperSide: false);

        var vaporUpper = specificVolume <= ceilingVaporSpecificVolume
            ? MaximumSaturationTemperatureKelvins
            : FindSpecificVolumeBoundary(
                TriplePointTemperatureKelvins,
                MaximumSaturationTemperatureKelvins,
                specificVolume,
                useLiquidBoundary: false,
                returnUpperSide: false);

        maximumKelvins = Math.Min(liquidUpper, vaporUpper);
        if (maximumKelvins + 1e-12d < minimumKelvins)
        {
            minimumKelvins = 0d;
            maximumKelvins = 0d;
            return false;
        }

        return true;
    }

    private static double FindSpecificVolumeBoundary(
        double lowerTemperatureKelvins,
        double upperTemperatureKelvins,
        double targetSpecificVolume,
        bool useLiquidBoundary,
        bool returnUpperSide)
    {
        var lower = lowerTemperatureKelvins;
        var upper = upperTemperatureKelvins;
        var lowerValue = SaturationBoundarySpecificVolume(lower, useLiquidBoundary) - targetSpecificVolume;

        for (var iteration = 0; iteration < BisectionIterations; iteration++)
        {
            var middle = (lower + upper) / 2d;
            var middleValue = SaturationBoundarySpecificVolume(middle, useLiquidBoundary) - targetSpecificVolume;
            if (HasSignChange(lowerValue, middleValue))
            {
                upper = middle;
            }
            else
            {
                lower = middle;
                lowerValue = middleValue;
            }
        }

        return returnUpperSide ? upper : lower;
    }

    private static double SaturationBoundarySpecificVolume(double temperatureKelvins, bool useLiquidBoundary)
    {
        var saturation = EvaluateSaturationValue(temperatureKelvins);
        return useLiquidBoundary
            ? saturation.SaturatedLiquidSpecificVolumeCubicMetresPerKilogram
            : saturation.SaturatedVaporSpecificVolumeCubicMetresPerKilogram;
    }

    private static bool TryGetHistoricalSaturatedTemperatureUpperBound(double specificVolume, out double upperBoundKelvins)
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
        var saturation = EvaluateSaturationValue(temperatureKelvins);
        return specificVolume >= saturation.SaturatedLiquidSpecificVolumeCubicMetresPerKilogram
            && specificVolume <= saturation.SaturatedVaporSpecificVolumeCubicMetresPerKilogram;
    }

    private bool TryResolveBoundaryAwareSuperheatedVapor(
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

    private bool TryGetSuperheatedTemperatureBounds(
        double specificVolume,
        out double minimumKelvins,
        out double maximumKelvins)
        => _closureMode == WaterSteamThermodynamicClosureMode.CorrelationConsistentInverseDomain
            ? TryGetCorrelationConsistentSuperheatedTemperatureBounds(specificVolume, out minimumKelvins, out maximumKelvins)
            : TryGetHistoricalSuperheatedTemperatureBounds(specificVolume, out minimumKelvins, out maximumKelvins);

    private static bool TryGetHistoricalSuperheatedTemperatureBounds(
        double specificVolume,
        out double minimumKelvins,
        out double maximumKelvins)
    {
        var pressureLimitedMaximum = MaximumSupportedSaturationPressurePascals * specificVolume
            / WaterVaporGasConstantJoulesPerKilogramKelvin;
        maximumKelvins = Math.Min(MaximumSuperheatedTemperatureKelvins, pressureLimitedMaximum);

        if (maximumKelvins < TriplePointTemperatureKelvins
            || !IsHistoricalSuperheatedTemperatureAdmissible(maximumKelvins, specificVolume))
        {
            minimumKelvins = 0d;
            maximumKelvins = 0d;
            return false;
        }

        if (IsHistoricalSuperheatedTemperatureAdmissible(TriplePointTemperatureKelvins, specificVolume))
        {
            minimumKelvins = TriplePointTemperatureKelvins;
            return true;
        }

        var lower = TriplePointTemperatureKelvins;
        var upper = Math.Min(MaximumSaturationTemperatureKelvins, maximumKelvins);
        if (!IsHistoricalSuperheatedTemperatureAdmissible(upper, specificVolume))
        {
            minimumKelvins = 0d;
            maximumKelvins = 0d;
            return false;
        }

        for (var iteration = 0; iteration < BisectionIterations; iteration++)
        {
            var middle = (lower + upper) / 2d;
            if (IsHistoricalSuperheatedTemperatureAdmissible(middle, specificVolume))
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

    private bool IsSuperheatedTemperatureAdmissible(double temperatureKelvins, double specificVolume)
        => IsSuperheatedTemperatureAdmissible(temperatureKelvins, specificVolume, SuperheatedPressurePascals(temperatureKelvins, specificVolume));

    private static bool IsHistoricalSuperheatedTemperatureAdmissible(double temperatureKelvins, double specificVolume)
        => IsSuperheatedTemperatureAdmissible(
            temperatureKelvins,
            specificVolume,
            WaterVaporGasConstantJoulesPerKilogramKelvin * temperatureKelvins / specificVolume);

    private static bool IsSuperheatedTemperatureAdmissible(
        double temperatureKelvins,
        double specificVolume,
        double pressurePascals)
    {
        _ = specificVolume;
        if (!double.IsFinite(pressurePascals) || pressurePascals <= 0d || pressurePascals >= CriticalPressurePascals)
        {
            return false;
        }

        var saturationTemperature = SaturationTemperatureFromPressure(pressurePascals);
        return saturationTemperature is not null && temperatureKelvins >= saturationTemperature.Value;
    }

    private bool TryGetCorrelationConsistentSuperheatedTemperatureBounds(
        double specificVolume,
        out double minimumKelvins,
        out double maximumKelvins)
    {
        if (!IsSuperheatedTemperatureAdmissible(MaximumSaturationTemperatureKelvins, specificVolume))
        {
            minimumKelvins = 0d;
            maximumKelvins = 0d;
            return false;
        }

        if (IsSuperheatedTemperatureAdmissible(TriplePointTemperatureKelvins, specificVolume))
        {
            minimumKelvins = TriplePointTemperatureKelvins;
        }
        else
        {
            var lower = TriplePointTemperatureKelvins;
            var upper = MaximumSaturationTemperatureKelvins;
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
        }

        if (IsSuperheatedTemperatureAdmissible(MaximumSuperheatedTemperatureKelvins, specificVolume))
        {
            maximumKelvins = MaximumSuperheatedTemperatureKelvins;
            return true;
        }

        var validLower = MaximumSaturationTemperatureKelvins;
        var invalidUpper = MaximumSuperheatedTemperatureKelvins;
        for (var iteration = 0; iteration < BisectionIterations; iteration++)
        {
            var middle = (validLower + invalidUpper) / 2d;
            if (IsSuperheatedTemperatureAdmissible(middle, specificVolume))
            {
                validLower = middle;
            }
            else
            {
                invalidUpper = middle;
            }
        }

        maximumKelvins = validLower;
        return maximumKelvins + 1e-12d >= minimumKelvins;
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

        var saturation = EvaluateSaturationValue(temperatureKelvins);
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

    private bool TryResolveSuperheatedVapor(
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

    /// <summary>
    /// H.28.1-E corrected-path-only coarse superheated scan. It preserves the exact historical grid and
    /// root equations, but avoids the expensive inverse saturation-temperature bisection when p > psat(T)
    /// by a guarded margin proves the sampled temperature cannot be superheated. Standard production Resolve
    /// and the public diagnostic continue to use TryResolveSuperheatedVapor unchanged.
    /// </summary>
    private bool TryResolveSuperheatedVaporForContinuity(
        double specificVolume,
        double specificInternalEnergy,
        out FluidThermodynamicState state)
    {
        SuperheatedEvaluation? previous = null;

        for (var index = 0; index <= SearchSegments; index++)
        {
            var temperature = TriplePointTemperatureKelvins
                + ((MaximumSuperheatedTemperatureKelvins - TriplePointTemperatureKelvins) * index / SearchSegments);
            var evaluation = EvaluateSuperheatedCandidateForContinuity(temperature, specificVolume, specificInternalEnergy);

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

    private static SaturationPropertyValue[] BuildCoarseSaturationScan()
    {
        var values = new SaturationPropertyValue[SearchSegments + 1];
        for (var index = 0; index <= SearchSegments; index++)
        {
            var temperature = TriplePointTemperatureKelvins
                + ((MaximumSaturationTemperatureKelvins - TriplePointTemperatureKelvins) * index / SearchSegments);
            values[index] = EvaluateSaturationValue(temperature);
        }

        return values;
    }

    private static SaturatedEvaluation? EvaluateSaturatedCandidate(
        double temperatureKelvins,
        double specificVolume,
        double targetSpecificInternalEnergy)
        => EvaluateSaturatedCandidate(
            EvaluateSaturationValue(temperatureKelvins),
            specificVolume,
            targetSpecificInternalEnergy);

    private static SaturatedEvaluation? EvaluateSaturatedCandidate(
        SaturationPropertyValue saturation,
        double specificVolume,
        double targetSpecificInternalEnergy)
    {
        var temperatureKelvins = saturation.Temperature.Kelvins;
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

    private double SuperheatedPressurePascals(double temperatureKelvins, double specificVolume)
    {
        if (_closureMode == WaterSteamThermodynamicClosureMode.HistoricalCorrelationTopology)
        {
            return WaterVaporGasConstantJoulesPerKilogramKelvin * temperatureKelvins / specificVolume;
        }

        var boundaryTemperatureKelvins = Math.Min(temperatureKelvins, MaximumSaturationTemperatureKelvins);
        var boundary = EvaluateSaturationValue(boundaryTemperatureKelvins);
        var idealBoundarySpecificVolume = WaterVaporGasConstantJoulesPerKilogramKelvin
            * boundaryTemperatureKelvins
            / boundary.Pressure.Pascals;
        var volumeShift = idealBoundarySpecificVolume - boundary.SaturatedVaporSpecificVolumeCubicMetresPerKilogram;
        var effectiveSpecificVolume = specificVolume + volumeShift;
        if (!double.IsFinite(effectiveSpecificVolume) || effectiveSpecificVolume <= 0d)
        {
            return double.NaN;
        }

        return WaterVaporGasConstantJoulesPerKilogramKelvin * temperatureKelvins / effectiveSpecificVolume;
    }

    private SuperheatedEvaluation? EvaluateSuperheatedCandidateForContinuity(
        double temperatureKelvins,
        double specificVolume,
        double targetSpecificInternalEnergy)
    {
        var pressurePascals = SuperheatedPressurePascals(temperatureKelvins, specificVolume);
        if (!double.IsFinite(pressurePascals) || pressurePascals <= 0d || pressurePascals >= CriticalPressurePascals)
        {
            return null;
        }

        if (temperatureKelvins <= MaximumSaturationTemperatureKelvins
            && pressurePascals >= MinimumSaturationPressurePascals
            && pressurePascals <= MaximumSupportedSaturationPressurePascals)
        {
            var saturationPressureAtCandidateTemperature = SaturationPressurePascals(temperatureKelvins);
            if (pressurePascals > saturationPressureAtCandidateTemperature * (1d + 1e-12d))
            {
                return null;
            }
        }

        return EvaluateSuperheatedCandidate(temperatureKelvins, specificVolume, targetSpecificInternalEnergy);
    }

    private SuperheatedEvaluation? EvaluateSuperheatedCandidate(
        double temperatureKelvins,
        double specificVolume,
        double targetSpecificInternalEnergy)
    {
        var pressurePascals = SuperheatedPressurePascals(temperatureKelvins, specificVolume);

        if (!double.IsFinite(pressurePascals) || pressurePascals <= 0d || pressurePascals >= CriticalPressurePascals)
        {
            return null;
        }

        var saturationTemperature = SaturationTemperatureFromPressure(pressurePascals);

        if (saturationTemperature is null || temperatureKelvins < saturationTemperature.Value)
        {
            return null;
        }

        var saturation = EvaluateSaturationValue(saturationTemperature.Value);
        var modeledSpecificInternalEnergy = saturation.SaturatedVaporInternalEnergy.JoulesPerKilogram
            + (VaporSpecificHeatAtConstantVolumeJoulesPerKilogramKelvin * (temperatureKelvins - saturationTemperature.Value));

        return new SuperheatedEvaluation(
            temperatureKelvins,
            pressurePascals,
            modeledSpecificInternalEnergy - targetSpecificInternalEnergy);
    }

    private SuperheatedEvaluation BisectSuperheated(
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
        var value = EvaluateSaturationValue(temperatureKelvins);
        return new WaterSteamSaturationProperties(
            value.Temperature,
            value.Pressure,
            value.SaturatedLiquidDensity,
            value.SaturatedVaporDensity,
            value.SaturatedLiquidInternalEnergy,
            value.SaturatedVaporInternalEnergy);
    }

    private static SaturationPropertyValue EvaluateSaturationValue(double temperatureKelvins)
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

        return new SaturationPropertyValue(
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

    private static double FindSaturatedLiquidDensityMaximumTemperatureKelvins()
    {
        var lower = TriplePointTemperatureKelvins;
        var upper = MaximumSaturationTemperatureKelvins;

        // The saturated-liquid correlation is unimodal over the supported interval: it rises from the
        // triple point to the physical density maximum near 4 C and then decreases toward the critical region.
        // Ternary search locates that turning point without hard-coding a temperature into inverse-domain logic.
        for (var iteration = 0; iteration < BisectionIterations; iteration++)
        {
            var left = lower + ((upper - lower) / 3d);
            var right = upper - ((upper - lower) / 3d);
            var leftDensity = SaturatedLiquidDensityKilogramsPerCubicMetre(left);
            var rightDensity = SaturatedLiquidDensityKilogramsPerCubicMetre(right);
            if (leftDensity < rightDensity)
            {
                lower = left;
            }
            else
            {
                upper = right;
            }
        }

        return (lower + upper) / 2d;
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
        if (pressurePascals < MinimumSaturationPressurePascals)
        {
            return TriplePointTemperatureKelvins;
        }

        if (pressurePascals > MaximumSupportedSaturationPressurePascals)
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
        SaturationPropertyValue Saturation);

    private readonly record struct SaturationPropertyValue(
        Temperature Temperature,
        Pressure Pressure,
        Density SaturatedLiquidDensity,
        Density SaturatedVaporDensity,
        SpecificEnergy SaturatedLiquidInternalEnergy,
        SpecificEnergy SaturatedVaporInternalEnergy)
    {
        public double SaturatedLiquidSpecificVolumeCubicMetresPerKilogram => 1d / SaturatedLiquidDensity.KilogramsPerCubicMetre;

        public double SaturatedVaporSpecificVolumeCubicMetresPerKilogram => 1d / SaturatedVaporDensity.KilogramsPerCubicMetre;
    }

    private readonly record struct SuperheatedEvaluation(
        double TemperatureKelvins,
        double PressurePascals,
        double ResidualJoulesPerKilogram);
}

internal readonly record struct WaterSteamBranchDisagreementEvaluation(
    FluidPhase ProductionSelectedPhase,
    bool LateBoundarySaturatedShadowedByEarlierSuperheated);

internal sealed record WaterSteamBranchContinuityEvaluation(
    FluidThermodynamicState ProductionState,
    bool MultiplePhaseRootsAvailable,
    WaterSteamInverseBranchCandidate? PreviousPhaseCandidate);
