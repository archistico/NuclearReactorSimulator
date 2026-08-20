using System.Globalization;
using System.Text;
using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay;

/// <summary>
/// I.5 REV1 Hotfix 8 evidence-only audit of the simplified water/steam inverse-map topology.
/// It maps the saturated-vapor/superheated seam, probes the saturated-liquid/subcooled seam,
/// and classifies the two operational-envelope exhaust failures without changing production code.
/// </summary>
public sealed class PhaseIWaterSteamCorrelationTopologyAuditTests
{
    private const double TriplePointKelvins = 273.16d;
    private const double MaximumSaturationKelvins = 640d;
    private const double WaterVaporGasConstantJoulesPerKilogramKelvin = 461.526d;
    private const double LiquidSeamProbeEnergyJoulesPerKilogram = 10d;
    private const int BisectionIterations = 80;
    private static readonly UTF8Encoding Utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);

    private readonly SimplifiedWaterSteamThermodynamicModel _model = new();

    [Fact(Explicit = true)]
    [Trait("Category", "PhaseIWaterSteamCorrelationTopologyAudit")]
    public void WaterSteamPhaseBoundaryTopology_MapsGapOverlapAndLiquidContinuityWithoutRuntimeChanges()
    {
        ResetReportDirectory();

        var vaporRows = BuildVaporSeamRows();
        var liquidRows = BuildLiquidSeamRows();
        var observedFailures = BuildObservedFailureRows();
        var representativeGapProbes = BuildRepresentativeGapProbes();

        var crossover = FindGapOverlapCrossover(vaporRows);
        var maximumGap = FindMaximumPositiveGap(crossover.SaturatedBoundaryTemperatureKelvins);
        WriteArtifacts(vaporRows, liquidRows, observedFailures, representativeGapProbes, crossover, maximumGap);

        Assert.All(vaporRows, row => Assert.True(
            row.IdealGasBoundarySpecificVolume > row.CorrelatedSaturatedVaporSpecificVolume,
            $"Expected ideal-gas superheated boundary volume to remain above correlated saturated-vapor volume at T={F(row.SaturatedBoundaryTemperatureKelvins)} K."));

        Assert.Contains(vaporRows, static row => row.Topology == VaporSeamTopology.NoRootGap);
        Assert.Contains(vaporRows, static row => row.Topology == VaporSeamTopology.Overlap);
        Assert.Contains(vaporRows, static row => row.Topology == VaporSeamTopology.NoSuperheatedOnsetBelowSaturationCeiling);

        var lowTemperatureLiquidBlindSpotRows = liquidRows
            .Where(static row => row.BelowBoundaryPhase == "OUT-OF-RANGE")
            .ToArray();
        var regularLiquidRows = liquidRows
            .Where(static row => row.TemperatureKelvins >= TriplePointKelvins + 15d)
            .ToArray();

        Assert.Single(lowTemperatureLiquidBlindSpotRows);
        Assert.Equal(TriplePointKelvins + 5d, lowTemperatureLiquidBlindSpotRows[0].TemperatureKelvins, 9);
        Assert.DoesNotContain(regularLiquidRows, static row => row.BelowBoundaryPhase == "OUT-OF-RANGE");
        Assert.DoesNotContain(liquidRows, static row => row.AtBoundaryPhase == "OUT-OF-RANGE");
        Assert.DoesNotContain(liquidRows, static row => row.AboveBoundaryPhase == "OUT-OF-RANGE");

        Assert.All(observedFailures, static row => Assert.True(row.InsideNoRootGap));
        Assert.All(representativeGapProbes, static row => Assert.True(row.MidpointRejected));
    }

    private List<VaporSeamRow> BuildVaporSeamRows()
    {
        var temperatures = new SortedSet<double>();
        for (var kelvins = TriplePointKelvins; kelvins < MaximumSaturationKelvins; kelvins += 1d)
        {
            temperatures.Add(kelvins);
        }
        temperatures.Add(MaximumSaturationKelvins);

        var rows = new List<VaporSeamRow>(temperatures.Count);
        foreach (var saturatedBoundaryTemperatureKelvins in temperatures)
        {
            var saturation = SaturationAt(saturatedBoundaryTemperatureKelvins);
            var correlatedVaporSpecificVolume = saturation.SaturatedVaporSpecificVolumeCubicMetresPerKilogram;
            var idealGasBoundarySpecificVolume = IdealGasBoundarySpecificVolume(saturatedBoundaryTemperatureKelvins, saturation.Pressure.Pascals);
            var onsetTemperature = FindSuperheatedOnsetTemperature(correlatedVaporSpecificVolume);

            if (!onsetTemperature.HasValue)
            {
                rows.Add(new VaporSeamRow(
                    saturatedBoundaryTemperatureKelvins,
                    saturation.Pressure.Pascals,
                    correlatedVaporSpecificVolume,
                    idealGasBoundarySpecificVolume,
                    idealGasBoundarySpecificVolume / correlatedVaporSpecificVolume,
                    saturation.SaturatedVaporInternalEnergy.JoulesPerKilogram,
                    null,
                    null,
                    null,
                    VaporSeamTopology.NoSuperheatedOnsetBelowSaturationCeiling));
                continue;
            }

            var onsetSaturation = SaturationAt(onsetTemperature.Value);
            var saturatedEndpointEnergy = saturation.SaturatedVaporInternalEnergy.JoulesPerKilogram;
            var superheatedOnsetEnergy = onsetSaturation.SaturatedVaporInternalEnergy.JoulesPerKilogram;
            var energyDelta = superheatedOnsetEnergy - saturatedEndpointEnergy;
            var topology = energyDelta > 1e-6d
                ? VaporSeamTopology.NoRootGap
                : energyDelta < -1e-6d
                    ? VaporSeamTopology.Overlap
                    : VaporSeamTopology.Touching;

            rows.Add(new VaporSeamRow(
                saturatedBoundaryTemperatureKelvins,
                saturation.Pressure.Pascals,
                correlatedVaporSpecificVolume,
                idealGasBoundarySpecificVolume,
                idealGasBoundarySpecificVolume / correlatedVaporSpecificVolume,
                saturatedEndpointEnergy,
                onsetTemperature,
                superheatedOnsetEnergy,
                energyDelta,
                topology));
        }

        return rows;
    }

    private List<LiquidSeamRow> BuildLiquidSeamRows()
    {
        var rows = new List<LiquidSeamRow>();
        for (var kelvins = TriplePointKelvins + 5d; kelvins <= MaximumSaturationKelvins - 5d; kelvins += 10d)
        {
            var saturation = SaturationAt(kelvins);
            var specificVolume = saturation.SaturatedLiquidSpecificVolumeCubicMetresPerKilogram;
            var boundaryEnergy = saturation.SaturatedLiquidInternalEnergy.JoulesPerKilogram;
            var below = ResolvePhase(specificVolume, boundaryEnergy - LiquidSeamProbeEnergyJoulesPerKilogram);
            var at = ResolvePhase(specificVolume, boundaryEnergy);
            var above = ResolvePhase(specificVolume, boundaryEnergy + LiquidSeamProbeEnergyJoulesPerKilogram);

            rows.Add(new LiquidSeamRow(
                kelvins,
                saturation.Pressure.Pascals,
                specificVolume,
                boundaryEnergy,
                below,
                at,
                above));
        }

        return rows;
    }

    private List<ObservedFailureRow> BuildObservedFailureRows()
    {
        return new[]
        {
            ClassifyObservedFailure(
                "desktop-v3-corrected-authoritative",
                39.711673348724119d,
                2_443_634.7334251222d),
            ClassifyObservedFailure(
                "desktop-v2-explicit-control",
                32.501022453035212d,
                2_447_440.7115321835d),
            ClassifyObservedFailure(
                "m9.7-negative-regression",
                65.477888248812704d,
                2_434_355d),
        }.ToList();
    }

    private List<RepresentativeGapProbeRow> BuildRepresentativeGapProbes()
    {
        var temperaturesCelsius = new[] { 20d, 40d, 80d, 120d, 150d, 180d };
        var rows = new List<RepresentativeGapProbeRow>(temperaturesCelsius.Length);

        foreach (var temperatureCelsius in temperaturesCelsius)
        {
            var saturatedTemperatureKelvins = temperatureCelsius + 273.15d;
            var saturation = SaturationAt(saturatedTemperatureKelvins);
            var specificVolume = saturation.SaturatedVaporSpecificVolumeCubicMetresPerKilogram;
            var onsetTemperature = FindSuperheatedOnsetTemperature(specificVolume)
                ?? throw new InvalidOperationException("Representative low-pressure seam probe unexpectedly has no superheated onset below 640 K.");
            var onsetSaturation = SaturationAt(onsetTemperature);
            var lowerEnergy = saturation.SaturatedVaporInternalEnergy.JoulesPerKilogram;
            var upperEnergy = onsetSaturation.SaturatedVaporInternalEnergy.JoulesPerKilogram;
            if (upperEnergy <= lowerEnergy)
            {
                throw new InvalidOperationException("Representative low-pressure seam probe is not in the expected no-root gap regime.");
            }

            var midpointEnergy = (lowerEnergy + upperEnergy) / 2d;
            var endpointPhase = ResolvePhase(specificVolume, lowerEnergy);
            var onsetPhase = ResolvePhase(specificVolume, upperEnergy);
            var midpointPhase = ResolvePhase(specificVolume, midpointEnergy);

            rows.Add(new RepresentativeGapProbeRow(
                saturatedTemperatureKelvins,
                saturation.Pressure.Pascals,
                specificVolume,
                lowerEnergy,
                upperEnergy,
                upperEnergy - lowerEnergy,
                endpointPhase,
                onsetPhase,
                midpointPhase,
                midpointPhase == "OUT-OF-RANGE"));
        }

        return rows;
    }

    private ObservedFailureRow ClassifyObservedFailure(string label, double specificVolume, double specificInternalEnergy)
    {
        var saturatedEndpointTemperature = FindSaturatedVaporEndpointTemperature(specificVolume)
            ?? throw new InvalidOperationException($"Observed failure '{label}' does not intersect the supported saturated-vapor boundary.");
        var superheatedOnsetTemperature = FindSuperheatedOnsetTemperature(specificVolume)
            ?? throw new InvalidOperationException($"Observed failure '{label}' does not intersect the supported superheated onset boundary.");
        var saturatedEndpointEnergy = SaturationAt(saturatedEndpointTemperature).SaturatedVaporInternalEnergy.JoulesPerKilogram;
        var superheatedOnsetEnergy = SaturationAt(superheatedOnsetTemperature).SaturatedVaporInternalEnergy.JoulesPerKilogram;
        var gapWidth = superheatedOnsetEnergy - saturatedEndpointEnergy;
        var inside = gapWidth > 0d
            && specificInternalEnergy > saturatedEndpointEnergy
            && specificInternalEnergy < superheatedOnsetEnergy;

        return new ObservedFailureRow(
            label,
            specificVolume,
            specificInternalEnergy,
            saturatedEndpointTemperature,
            saturatedEndpointEnergy,
            superheatedOnsetTemperature,
            superheatedOnsetEnergy,
            gapWidth,
            specificInternalEnergy - saturatedEndpointEnergy,
            superheatedOnsetEnergy - specificInternalEnergy,
            inside);
    }

    private CrossoverResult FindGapOverlapCrossover(IReadOnlyList<VaporSeamRow> vaporRows)
    {
        VaporSeamRow? previous = null;
        foreach (var row in vaporRows.Where(static row => row.EnergyDeltaJoulesPerKilogram.HasValue))
        {
            if (previous is not null
                && previous.EnergyDeltaJoulesPerKilogram!.Value > 0d
                && row.EnergyDeltaJoulesPerKilogram!.Value <= 0d)
            {
                var lower = previous.SaturatedBoundaryTemperatureKelvins;
                var upper = row.SaturatedBoundaryTemperatureKelvins;
                for (var iteration = 0; iteration < BisectionIterations; iteration++)
                {
                    var middle = (lower + upper) / 2d;
                    var delta = BoundaryEnergyDelta(middle)
                        ?? throw new InvalidOperationException("Gap/overlap crossover bisection lost the superheated onset boundary.");
                    if (delta > 0d)
                    {
                        lower = middle;
                    }
                    else
                    {
                        upper = middle;
                    }
                }

                var crossoverTemperature = (lower + upper) / 2d;
                var saturation = SaturationAt(crossoverTemperature);
                var onsetTemperature = FindSuperheatedOnsetTemperature(saturation.SaturatedVaporSpecificVolumeCubicMetresPerKilogram)
                    ?? throw new InvalidOperationException("Gap/overlap crossover has no superheated onset.");
                return new CrossoverResult(
                    crossoverTemperature,
                    saturation.Pressure.Pascals,
                    saturation.SaturatedVaporSpecificVolumeCubicMetresPerKilogram,
                    onsetTemperature,
                    BoundaryEnergyDelta(crossoverTemperature)!.Value);
            }

            previous = row;
        }

        throw new InvalidOperationException("Could not locate the saturation-to-superheat gap/overlap crossover.");
    }

    private MaximumGapResult FindMaximumPositiveGap(double crossoverTemperatureKelvins)
    {
        const double coarseStepKelvins = 0.1d;
        var bestTemperature = TriplePointKelvins;
        var bestGap = double.NegativeInfinity;

        for (var kelvins = TriplePointKelvins; kelvins <= crossoverTemperatureKelvins; kelvins += coarseStepKelvins)
        {
            var delta = BoundaryEnergyDelta(kelvins);
            if (delta.HasValue && delta.Value > bestGap)
            {
                bestGap = delta.Value;
                bestTemperature = kelvins;
            }
        }

        var refineLower = Math.Max(TriplePointKelvins, bestTemperature - coarseStepKelvins);
        var refineUpper = Math.Min(crossoverTemperatureKelvins, bestTemperature + coarseStepKelvins);
        const int refineSamples = 400;
        for (var index = 0; index <= refineSamples; index++)
        {
            var kelvins = refineLower + ((refineUpper - refineLower) * index / refineSamples);
            var delta = BoundaryEnergyDelta(kelvins);
            if (delta.HasValue && delta.Value > bestGap)
            {
                bestGap = delta.Value;
                bestTemperature = kelvins;
            }
        }

        var saturation = SaturationAt(bestTemperature);
        var onsetTemperature = FindSuperheatedOnsetTemperature(saturation.SaturatedVaporSpecificVolumeCubicMetresPerKilogram)
            ?? throw new InvalidOperationException("Maximum positive gap sample unexpectedly has no superheated onset.");
        return new MaximumGapResult(
            bestTemperature,
            saturation.Pressure.Pascals,
            saturation.SaturatedVaporSpecificVolumeCubicMetresPerKilogram,
            onsetTemperature,
            bestGap);
    }

    private double? BoundaryEnergyDelta(double saturatedBoundaryTemperatureKelvins)
    {
        var saturation = SaturationAt(saturatedBoundaryTemperatureKelvins);
        var onsetTemperature = FindSuperheatedOnsetTemperature(saturation.SaturatedVaporSpecificVolumeCubicMetresPerKilogram);
        if (!onsetTemperature.HasValue)
        {
            return null;
        }

        return SaturationAt(onsetTemperature.Value).SaturatedVaporInternalEnergy.JoulesPerKilogram
            - saturation.SaturatedVaporInternalEnergy.JoulesPerKilogram;
    }

    private double? FindSaturatedVaporEndpointTemperature(double specificVolume)
    {
        var lowerValue = SaturationAt(TriplePointKelvins).SaturatedVaporSpecificVolumeCubicMetresPerKilogram;
        var upperValue = SaturationAt(MaximumSaturationKelvins).SaturatedVaporSpecificVolumeCubicMetresPerKilogram;
        if (specificVolume > lowerValue || specificVolume < upperValue)
        {
            return null;
        }

        var lower = TriplePointKelvins;
        var upper = MaximumSaturationKelvins;
        for (var iteration = 0; iteration < BisectionIterations; iteration++)
        {
            var middle = (lower + upper) / 2d;
            var middleVolume = SaturationAt(middle).SaturatedVaporSpecificVolumeCubicMetresPerKilogram;
            if (middleVolume > specificVolume)
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

    private double? FindSuperheatedOnsetTemperature(double specificVolume)
    {
        var minimumBoundaryVolume = IdealGasBoundarySpecificVolume(
            MaximumSaturationKelvins,
            SaturationAt(MaximumSaturationKelvins).Pressure.Pascals);
        var maximumBoundaryVolume = IdealGasBoundarySpecificVolume(
            TriplePointKelvins,
            SaturationAt(TriplePointKelvins).Pressure.Pascals);

        if (specificVolume < minimumBoundaryVolume)
        {
            return null;
        }

        if (specificVolume >= maximumBoundaryVolume)
        {
            return TriplePointKelvins;
        }

        var lower = TriplePointKelvins;
        var upper = MaximumSaturationKelvins;
        for (var iteration = 0; iteration < BisectionIterations; iteration++)
        {
            var middle = (lower + upper) / 2d;
            var saturation = SaturationAt(middle);
            var middleBoundaryVolume = IdealGasBoundarySpecificVolume(middle, saturation.Pressure.Pascals);
            if (middleBoundaryVolume > specificVolume)
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

    private string ResolvePhase(double specificVolume, double specificInternalEnergy)
    {
        var definition = new FluidNodeDefinition("topology-probe", Volume.FromCubicMetres(specificVolume));
        var inventory = new FluidNodeInventory(
            Mass.FromKilograms(1d),
            Energy.FromJoules(specificInternalEnergy));
        var previous = new FluidThermodynamicState(
            Pressure.StandardAtmosphere,
            Temperature.FromDegreesCelsius(20d));

        try
        {
            return _model.Resolve(definition, inventory, previous).Phase.ToString();
        }
        catch (WaterSteamStateOutOfRangeException)
        {
            return "OUT-OF-RANGE";
        }
    }

    private WaterSteamSaturationProperties SaturationAt(double temperatureKelvins)
        => _model.GetSaturationProperties(Temperature.FromKelvins(temperatureKelvins));

    private static double IdealGasBoundarySpecificVolume(double temperatureKelvins, double saturationPressurePascals)
        => WaterVaporGasConstantJoulesPerKilogramKelvin * temperatureKelvins / saturationPressurePascals;

    private static void WriteArtifacts(
        IReadOnlyList<VaporSeamRow> vaporRows,
        IReadOnlyList<LiquidSeamRow> liquidRows,
        IReadOnlyList<ObservedFailureRow> observedFailures,
        IReadOnlyList<RepresentativeGapProbeRow> representativeGapProbes,
        CrossoverResult crossover,
        MaximumGapResult maximumGap)
    {
        var directory = ReportDirectory();

        var vaporCsv = new List<string>
        {
            "sat_t_k,sat_t_c,sat_p_kpa,vg_correlation_m3_kg,vg_ideal_boundary_m3_kg,ideal_to_correlation_ratio,saturated_endpoint_u_j_kg,superheated_onset_t_k,superheated_onset_t_c,superheated_onset_u_j_kg,energy_delta_j_kg,topology",
        };
        vaporCsv.AddRange(vaporRows.Select(static row => string.Join(",",
            F(row.SaturatedBoundaryTemperatureKelvins),
            F(row.SaturatedBoundaryTemperatureKelvins - 273.15d),
            F(row.SaturatedBoundaryPressurePascals / 1_000d),
            F(row.CorrelatedSaturatedVaporSpecificVolume),
            F(row.IdealGasBoundarySpecificVolume),
            F(row.IdealToCorrelatedSpecificVolumeRatio),
            F(row.SaturatedEndpointEnergyJoulesPerKilogram),
            F(row.SuperheatedOnsetTemperatureKelvins),
            row.SuperheatedOnsetTemperatureKelvins.HasValue ? F(row.SuperheatedOnsetTemperatureKelvins.Value - 273.15d) : string.Empty,
            F(row.SuperheatedOnsetEnergyJoulesPerKilogram),
            F(row.EnergyDeltaJoulesPerKilogram),
            row.Topology)));
        File.WriteAllLines(Path.Combine(directory, "02-saturation-superheat-topology.csv"), vaporCsv, Utf8WithoutBom);

        var liquidCsv = new List<string>
        {
            "t_k,t_c,p_kpa,vf_m3_kg,uf_j_kg,minus_10j_phase,boundary_phase,plus_10j_phase",
        };
        liquidCsv.AddRange(liquidRows.Select(static row => string.Join(",",
            F(row.TemperatureKelvins),
            F(row.TemperatureKelvins - 273.15d),
            F(row.PressurePascals / 1_000d),
            F(row.SpecificVolume),
            F(row.BoundaryEnergyJoulesPerKilogram),
            row.BelowBoundaryPhase,
            row.AtBoundaryPhase,
            row.AboveBoundaryPhase)));
        File.WriteAllLines(Path.Combine(directory, "03-saturation-liquid-continuity-probes.csv"), liquidCsv, Utf8WithoutBom);

        var failuresCsv = new List<string>
        {
            "label,v_m3_kg,u_j_kg,saturated_endpoint_t_c,saturated_endpoint_u_j_kg,superheated_onset_t_c,superheated_onset_u_j_kg,gap_width_j_kg,distance_above_saturated_j_kg,distance_below_superheated_j_kg,inside_no_root_gap",
        };
        failuresCsv.AddRange(observedFailures.Select(static row => string.Join(",",
            row.Label,
            F(row.SpecificVolume),
            F(row.SpecificInternalEnergy),
            F(row.SaturatedEndpointTemperatureKelvins - 273.15d),
            F(row.SaturatedEndpointEnergyJoulesPerKilogram),
            F(row.SuperheatedOnsetTemperatureKelvins - 273.15d),
            F(row.SuperheatedOnsetEnergyJoulesPerKilogram),
            F(row.GapWidthJoulesPerKilogram),
            F(row.DistanceAboveSaturatedEndpointJoulesPerKilogram),
            F(row.DistanceBelowSuperheatedOnsetJoulesPerKilogram),
            row.InsideNoRootGap)));
        File.WriteAllLines(Path.Combine(directory, "04-observed-failure-classification.csv"), failuresCsv, Utf8WithoutBom);

        var probesCsv = new List<string>
        {
            "sat_t_c,sat_p_kpa,v_m3_kg,saturated_endpoint_u_j_kg,superheated_onset_u_j_kg,gap_width_j_kg,endpoint_phase,onset_phase,midpoint_phase,midpoint_rejected",
        };
        probesCsv.AddRange(representativeGapProbes.Select(static row => string.Join(",",
            F(row.SaturatedBoundaryTemperatureKelvins - 273.15d),
            F(row.SaturatedBoundaryPressurePascals / 1_000d),
            F(row.SpecificVolume),
            F(row.SaturatedEndpointEnergyJoulesPerKilogram),
            F(row.SuperheatedOnsetEnergyJoulesPerKilogram),
            F(row.GapWidthJoulesPerKilogram),
            row.EndpointPhase,
            row.OnsetPhase,
            row.MidpointPhase,
            row.MidpointRejected)));
        File.WriteAllLines(Path.Combine(directory, "05-representative-no-root-probes.csv"), probesCsv, Utf8WithoutBom);

        var gapRows = vaporRows.Where(static row => row.Topology == VaporSeamTopology.NoRootGap).ToArray();
        var overlapRows = vaporRows.Where(static row => row.Topology == VaporSeamTopology.Overlap).ToArray();
        var noOnsetRows = vaporRows.Where(static row => row.Topology == VaporSeamTopology.NoSuperheatedOnsetBelowSaturationCeiling).ToArray();
        var minimumRatio = vaporRows.Min(static row => row.IdealToCorrelatedSpecificVolumeRatio);
        var maximumRatio = vaporRows.Max(static row => row.IdealToCorrelatedSpecificVolumeRatio);

        var summary = new List<string>
        {
            "=== 01-i5-water-steam-correlation-topology-audit ===",
            "scope=production SimplifiedWaterSteamThermodynamicModel topology only; diagnostics/tests/docs; no runtime/model/equation/coefficient/tolerance/target-set changes;",
            $"vapor-seam-samples={vaporRows.Count}; same-temperature-ideal-boundary-volume-greater-than-correlated-vg={vaporRows.All(static row => row.IdealGasBoundarySpecificVolume > row.CorrelatedSaturatedVaporSpecificVolume)}; ratio-min={F(minimumRatio)}; ratio-max={F(maximumRatio)};",
            $"no-root-gap-samples={gapRows.Length}; overlap-samples={overlapRows.Length}; no-superheated-onset-below-640k-samples={noOnsetRows.Length};",
            $"gap-overlap-crossover=saturated-side-t-c:{F(crossover.SaturatedBoundaryTemperatureKelvins - 273.15d)}; saturated-side-p-kpa:{F(crossover.SaturatedBoundaryPressurePascals / 1_000d)}; v-m3-kg:{F(crossover.SpecificVolume)}; superheated-onset-t-c:{F(crossover.SuperheatedOnsetTemperatureKelvins - 273.15d)}; delta-u-j-kg:{F(crossover.EnergyDeltaJoulesPerKilogram)};",
            $"maximum-positive-gap=saturated-side-t-c:{F(maximumGap.SaturatedBoundaryTemperatureKelvins - 273.15d)}; saturated-side-p-kpa:{F(maximumGap.SaturatedBoundaryPressurePascals / 1_000d)}; v-m3-kg:{F(maximumGap.SpecificVolume)}; superheated-onset-t-c:{F(maximumGap.SuperheatedOnsetTemperatureKelvins - 273.15d)}; gap-width-j-kg:{F(maximumGap.GapWidthJoulesPerKilogram)};",
            $"liquid-seam-probes={liquidRows.Count}; liquid-seam-out-of-range-count={liquidRows.Count(static row => row.BelowBoundaryPhase == "OUT-OF-RANGE" || row.AtBoundaryPhase == "OUT-OF-RANGE" || row.AboveBoundaryPhase == "OUT-OF-RANGE")};",
            $"observed-failures-inside-same-no-root-family={observedFailures.All(static row => row.InsideNoRootGap)}; observed-count={observedFailures.Count}; representative-gap-midpoints-rejected={representativeGapProbes.Count(static row => row.MidpointRejected)}/{representativeGapProbes.Count};",
            "interpretation=the saturation-to-superheat boundary is structurally mismatched because saturated vapor uses the correlated vapor density while superheated onset uses ideal-gas p=R*T/v against Psat(T); low-pressure/dry-vapor states form a real no-root band and higher-pressure states form an overlap/multiple-root band. The 5.01 C liquid-side probe exposes a separate low-temperature inverse-search blind spot near the water density maximum; regular liquid-side probes from 15.01 C upward remain resolvable;",
            "phase-i-status=BLOCKED; next-step=complete the low-temperature liquid blind-spot census, then repair the vapor phase-boundary closure and saturated inverse-search topology coherently before requalifying operational/load/replay/long gates; do not patch exhaust only and do not weaken fail-closed behavior;",
        };
        File.WriteAllLines(Path.Combine(directory, "01-i5-water-steam-correlation-topology-audit.summary.txt"), summary, Utf8WithoutBom);
    }

    private static string F(double? value)
        => value.HasValue ? value.Value.ToString("G17", CultureInfo.InvariantCulture) : string.Empty;

    private static string F(double value)
        => value.ToString("G17", CultureInfo.InvariantCulture);

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "NuclearReactorSimulator.sln")))
            {
                return current.FullName;
            }
            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate NuclearReactorSimulator.sln from the test output directory.");
    }

    private static string ReportDirectory()
        => Path.Combine(FindRepositoryRoot(), "artifacts", "i5-water-steam-correlation-topology-audit");

    private static void ResetReportDirectory()
    {
        var directory = ReportDirectory();
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
        Directory.CreateDirectory(directory);
    }

    private enum VaporSeamTopology
    {
        Touching,
        NoRootGap,
        Overlap,
        NoSuperheatedOnsetBelowSaturationCeiling,
    }

    private sealed record VaporSeamRow(
        double SaturatedBoundaryTemperatureKelvins,
        double SaturatedBoundaryPressurePascals,
        double CorrelatedSaturatedVaporSpecificVolume,
        double IdealGasBoundarySpecificVolume,
        double IdealToCorrelatedSpecificVolumeRatio,
        double SaturatedEndpointEnergyJoulesPerKilogram,
        double? SuperheatedOnsetTemperatureKelvins,
        double? SuperheatedOnsetEnergyJoulesPerKilogram,
        double? EnergyDeltaJoulesPerKilogram,
        VaporSeamTopology Topology);

    private sealed record LiquidSeamRow(
        double TemperatureKelvins,
        double PressurePascals,
        double SpecificVolume,
        double BoundaryEnergyJoulesPerKilogram,
        string BelowBoundaryPhase,
        string AtBoundaryPhase,
        string AboveBoundaryPhase);

    private sealed record ObservedFailureRow(
        string Label,
        double SpecificVolume,
        double SpecificInternalEnergy,
        double SaturatedEndpointTemperatureKelvins,
        double SaturatedEndpointEnergyJoulesPerKilogram,
        double SuperheatedOnsetTemperatureKelvins,
        double SuperheatedOnsetEnergyJoulesPerKilogram,
        double GapWidthJoulesPerKilogram,
        double DistanceAboveSaturatedEndpointJoulesPerKilogram,
        double DistanceBelowSuperheatedOnsetJoulesPerKilogram,
        bool InsideNoRootGap);

    private sealed record RepresentativeGapProbeRow(
        double SaturatedBoundaryTemperatureKelvins,
        double SaturatedBoundaryPressurePascals,
        double SpecificVolume,
        double SaturatedEndpointEnergyJoulesPerKilogram,
        double SuperheatedOnsetEnergyJoulesPerKilogram,
        double GapWidthJoulesPerKilogram,
        string EndpointPhase,
        string OnsetPhase,
        string MidpointPhase,
        bool MidpointRejected);

    private sealed record CrossoverResult(
        double SaturatedBoundaryTemperatureKelvins,
        double SaturatedBoundaryPressurePascals,
        double SpecificVolume,
        double SuperheatedOnsetTemperatureKelvins,
        double EnergyDeltaJoulesPerKilogram);

    private sealed record MaximumGapResult(
        double SaturatedBoundaryTemperatureKelvins,
        double SaturatedBoundaryPressurePascals,
        double SpecificVolume,
        double SuperheatedOnsetTemperatureKelvins,
        double GapWidthJoulesPerKilogram);
}
