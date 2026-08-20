using System.Globalization;
using System.Text;
using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay;

/// <summary>
/// I.5 REV1 Hotfix 9 evidence-only census of the low-temperature liquid/saturation inverse-map blind spot
/// exposed by Hotfix 8. It does not change the production thermodynamic model.
/// </summary>
public sealed class PhaseIWaterSteamLowTemperatureLiquidSeamAuditTests
{
    private const double TriplePointKelvins = 273.16d;
    private const double LiquidSpecificHeatJoulesPerKilogramKelvin = 4_200d;
    private const double ProbeEnergyBelowBoundaryJoulesPerKilogram = 10d;
    private const double LocalBracketOffsetKelvins = 0.005d;
    private const int BisectionIterations = 80;
    private static readonly UTF8Encoding Utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);

    private readonly SimplifiedWaterSteamThermodynamicModel _model = new();

    [Fact(Explicit = true)]
    [Trait("Category", "PhaseIWaterSteamLowTemperatureLiquidSeamAudit")]
    public void LowTemperatureLiquidSeam_MapsDensityMaximumBlindSpotWithoutRuntimeChanges()
    {
        ResetReportDirectory();

        var densityMaximum = FindDensityMaximum();
        var warmTwin = FindWarmSpecificVolumeTwin(densityMaximum);
        var rows = BuildRows();
        var blindRows = rows.Where(static row => row.BlindSpot).ToArray();
        var colderControls = rows
            .Where(row => row.TemperatureKelvins <= densityMaximum.TemperatureKelvins - 0.1d)
            .ToArray();
        var warmerControls = rows
            .Where(row => row.TemperatureKelvins >= warmTwin.TemperatureKelvins + 0.1d)
            .ToArray();

        WriteArtifacts(rows, blindRows, densityMaximum, warmTwin);

        Assert.NotEmpty(blindRows);
        Assert.All(blindRows, row =>
        {
            Assert.True(row.ExpectedRootExists);
            Assert.True(row.LocalSaturatedRootBracketExists);
            Assert.Equal("OUT-OF-RANGE", row.ProductionPhase);
            Assert.True(row.TemperatureKelvins > densityMaximum.TemperatureKelvins);
            Assert.True(row.TemperatureKelvins < warmTwin.TemperatureKelvins + 0.1d);
        });
        Assert.DoesNotContain(colderControls, static row => row.ProductionPhase == "OUT-OF-RANGE");
        Assert.DoesNotContain(warmerControls, static row => row.ExpectedRootExists && row.ProductionPhase == "OUT-OF-RANGE");
    }

    private List<LowTemperatureLiquidSeamRow> BuildRows()
    {
        var rows = new List<LowTemperatureLiquidSeamRow>();
        for (var offsetKelvins = 0.5d; offsetKelvins <= 12d + 1e-12d; offsetKelvins += 0.05d)
        {
            var temperatureKelvins = TriplePointKelvins + offsetKelvins;
            var boundary = SaturationAt(temperatureKelvins);
            var specificVolume = boundary.SaturatedLiquidSpecificVolumeCubicMetresPerKilogram;
            var density = 1d / specificVolume;
            var boundaryEnergy = boundary.SaturatedLiquidInternalEnergy.JoulesPerKilogram;
            var targetEnergy = boundaryEnergy - ProbeEnergyBelowBoundaryJoulesPerKilogram;
            var targetLiquidTemperature = TriplePointKelvins + (targetEnergy / LiquidSpecificHeatJoulesPerKilogramKelvin);
            var targetLiquidSaturation = SaturationAt(targetLiquidTemperature);
            var targetSaturatedLiquidDensity = targetLiquidSaturation.SaturatedLiquidDensity.KilogramsPerCubicMetre;
            var actualDensity = 1d / specificVolume;
            var subcooledAdmissible = actualDensity + (targetSaturatedLiquidDensity * 1e-10d) >= targetSaturatedLiquidDensity;
            var localSaturatedRootBracketExists = HasLocalSaturatedRootBracket(
                temperatureKelvins,
                specificVolume,
                targetEnergy,
                boundaryEnergy);
            var expectedRootExists = subcooledAdmissible || localSaturatedRootBracketExists;
            var productionPhase = ResolvePhase(specificVolume, targetEnergy);
            var blindSpot = expectedRootExists && productionPhase == "OUT-OF-RANGE";

            rows.Add(new LowTemperatureLiquidSeamRow(
                temperatureKelvins,
                specificVolume,
                density,
                boundaryEnergy,
                targetEnergy,
                targetLiquidTemperature,
                subcooledAdmissible,
                localSaturatedRootBracketExists,
                expectedRootExists,
                productionPhase,
                blindSpot));
        }

        return rows;
    }

    private bool HasLocalSaturatedRootBracket(
        double boundaryTemperatureKelvins,
        double specificVolume,
        double targetEnergy,
        double boundaryEnergy)
    {
        var lowerTemperatureKelvins = boundaryTemperatureKelvins - LocalBracketOffsetKelvins;
        if (lowerTemperatureKelvins < TriplePointKelvins)
        {
            return false;
        }

        var lower = SaturationAt(lowerTemperatureKelvins);
        var lowerLiquidVolume = lower.SaturatedLiquidSpecificVolumeCubicMetresPerKilogram;
        var lowerVaporVolume = lower.SaturatedVaporSpecificVolumeCubicMetresPerKilogram;
        if (specificVolume < lowerLiquidVolume || specificVolume > lowerVaporVolume)
        {
            return false;
        }

        var quality = (specificVolume - lowerLiquidVolume) / (lowerVaporVolume - lowerLiquidVolume);
        quality = Math.Clamp(quality, 0d, 1d);
        var lowerLiquidEnergy = lower.SaturatedLiquidInternalEnergy.JoulesPerKilogram;
        var lowerVaporEnergy = lower.SaturatedVaporInternalEnergy.JoulesPerKilogram;
        var lowerMixtureEnergy = lowerLiquidEnergy + (quality * (lowerVaporEnergy - lowerLiquidEnergy));

        return lowerMixtureEnergy < targetEnergy && boundaryEnergy > targetEnergy;
    }

    private DensityMaximumResult FindDensityMaximum()
    {
        const double upperKelvins = TriplePointKelvins + 12d;
        const double coarseStepKelvins = 0.01d;
        var bestTemperature = TriplePointKelvins;
        var bestDensity = double.NegativeInfinity;

        for (var kelvins = TriplePointKelvins; kelvins <= upperKelvins; kelvins += coarseStepKelvins)
        {
            var density = SaturationAt(kelvins).SaturatedLiquidDensity.KilogramsPerCubicMetre;
            if (density > bestDensity)
            {
                bestDensity = density;
                bestTemperature = kelvins;
            }
        }

        var lower = Math.Max(TriplePointKelvins, bestTemperature - coarseStepKelvins);
        var upper = Math.Min(upperKelvins, bestTemperature + coarseStepKelvins);
        const int refinementSamples = 2_000;
        for (var index = 0; index <= refinementSamples; index++)
        {
            var kelvins = lower + ((upper - lower) * index / refinementSamples);
            var saturation = SaturationAt(kelvins);
            var density = saturation.SaturatedLiquidDensity.KilogramsPerCubicMetre;
            if (density > bestDensity)
            {
                bestDensity = density;
                bestTemperature = kelvins;
            }
        }

        return new DensityMaximumResult(bestTemperature, bestDensity, 1d / bestDensity);
    }

    private WarmTwinResult FindWarmSpecificVolumeTwin(DensityMaximumResult densityMaximum)
    {
        var tripleVolume = SaturationAt(TriplePointKelvins).SaturatedLiquidSpecificVolumeCubicMetresPerKilogram;
        var lower = densityMaximum.TemperatureKelvins;
        var upper = TriplePointKelvins + 12d;
        var upperVolume = SaturationAt(upper).SaturatedLiquidSpecificVolumeCubicMetresPerKilogram;
        if (upperVolume <= tripleVolume)
        {
            throw new InvalidOperationException("Could not bracket the warm specific-volume twin of the triple-point saturated liquid.");
        }

        for (var iteration = 0; iteration < BisectionIterations; iteration++)
        {
            var middle = (lower + upper) / 2d;
            var middleVolume = SaturationAt(middle).SaturatedLiquidSpecificVolumeCubicMetresPerKilogram;
            if (middleVolume < tripleVolume)
            {
                lower = middle;
            }
            else
            {
                upper = middle;
            }
        }

        var temperature = (lower + upper) / 2d;
        var saturation = SaturationAt(temperature);
        return new WarmTwinResult(
            temperature,
            saturation.SaturatedLiquidDensity.KilogramsPerCubicMetre,
            saturation.SaturatedLiquidSpecificVolumeCubicMetresPerKilogram,
            tripleVolume);
    }

    private string ResolvePhase(double specificVolume, double specificInternalEnergy)
    {
        var definition = new FluidNodeDefinition("low-temperature-liquid-seam-probe", Volume.FromCubicMetres(specificVolume));
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

    private static void WriteArtifacts(
        IReadOnlyList<LowTemperatureLiquidSeamRow> rows,
        IReadOnlyList<LowTemperatureLiquidSeamRow> blindRows,
        DensityMaximumResult densityMaximum,
        WarmTwinResult warmTwin)
    {
        var directory = ReportDirectory();
        var csv = new List<string>
        {
            "t_k,t_c,vf_m3_kg,rho_f_kg_m3,boundary_u_j_kg,target_u_j_kg,target_liquid_t_c,subcooled_admissible,local_saturated_root_bracket_exists,expected_root_exists,production_phase,blind_spot",
        };
        csv.AddRange(rows.Select(static row => string.Join(",",
            F(row.TemperatureKelvins),
            F(row.TemperatureKelvins - 273.15d),
            F(row.SpecificVolume),
            F(row.DensityKilogramsPerCubicMetre),
            F(row.BoundaryEnergyJoulesPerKilogram),
            F(row.TargetEnergyJoulesPerKilogram),
            F(row.TargetLiquidTemperatureKelvins - 273.15d),
            row.SubcooledAdmissible,
            row.LocalSaturatedRootBracketExists,
            row.ExpectedRootExists,
            row.ProductionPhase,
            row.BlindSpot)));
        File.WriteAllLines(Path.Combine(directory, "02-low-temperature-liquid-seam-census.csv"), csv, Utf8WithoutBom);

        var firstBlind = blindRows.FirstOrDefault();
        var lastBlind = blindRows.LastOrDefault();
        var productionOutOfRange = rows.Count(static row => row.ProductionPhase == "OUT-OF-RANGE");
        var summary = new List<string>
        {
            "=== 01-i5-water-steam-low-temperature-liquid-seam-audit ===",
            "scope=production SimplifiedWaterSteamThermodynamicModel low-temperature liquid/saturation inverse topology only; diagnostics/tests/docs; no runtime/model/equation/coefficient/tolerance changes;",
            $"density-maximum=t-c:{F(densityMaximum.TemperatureKelvins - 273.15d)}; rho-kg-m3:{F(densityMaximum.DensityKilogramsPerCubicMetre)}; vf-m3-kg:{F(densityMaximum.SpecificVolume)};",
            $"triple-volume-warm-twin=t-c:{F(warmTwin.TemperatureKelvins - 273.15d)}; rho-kg-m3:{F(warmTwin.DensityKilogramsPerCubicMetre)}; vf-m3-kg:{F(warmTwin.SpecificVolume)}; triple-vf-m3-kg:{F(warmTwin.TriplePointSpecificVolume)};",
            $"census-samples={rows.Count}; expected-root-samples={rows.Count(static row => row.ExpectedRootExists)}; production-out-of-range={productionOutOfRange}; proven-blind-spot-samples={blindRows.Count};",
            $"blind-spot-sampled-range-t-c={F(firstBlind?.TemperatureKelvins - 273.15d)}..{F(lastBlind?.TemperatureKelvins - 273.15d)}; all-blind-spots-have-local-saturated-root-bracket={blindRows.All(static row => row.LocalSaturatedRootBracketExists)};",
            "interpretation=the saturated-liquid specific-volume correlation is non-monotonic around the water density maximum. For warm-side states below the triple-point-volume twin, a valid saturated root can exist in a local temperature island even though the current boundary-aware saturated fallback rejects the fixed volume because it is not valid at the triple point. This is a second internal inverse-map defect family, distinct from the broad vapor correlation mismatch;",
            "phase-i-status=BLOCKED; next-step=design one coherent thermodynamic inverse-domain repair that removes the vapor seam gap/overlap and makes saturated root discovery interval-aware rather than assuming triple-point-connected monotonic validity; fail-closed behavior remains required for genuinely unsupported states;",
        };
        File.WriteAllLines(Path.Combine(directory, "01-i5-water-steam-low-temperature-liquid-seam-audit.summary.txt"), summary, Utf8WithoutBom);
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
        => Path.Combine(FindRepositoryRoot(), "artifacts", "i5-water-steam-low-temperature-liquid-seam-audit");

    private static void ResetReportDirectory()
    {
        var directory = ReportDirectory();
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
        Directory.CreateDirectory(directory);
    }

    private sealed record LowTemperatureLiquidSeamRow(
        double TemperatureKelvins,
        double SpecificVolume,
        double DensityKilogramsPerCubicMetre,
        double BoundaryEnergyJoulesPerKilogram,
        double TargetEnergyJoulesPerKilogram,
        double TargetLiquidTemperatureKelvins,
        bool SubcooledAdmissible,
        bool LocalSaturatedRootBracketExists,
        bool ExpectedRootExists,
        string ProductionPhase,
        bool BlindSpot);

    private sealed record DensityMaximumResult(
        double TemperatureKelvins,
        double DensityKilogramsPerCubicMetre,
        double SpecificVolume);

    private sealed record WarmTwinResult(
        double TemperatureKelvins,
        double DensityKilogramsPerCubicMetre,
        double SpecificVolume,
        double TriplePointSpecificVolume);
}
