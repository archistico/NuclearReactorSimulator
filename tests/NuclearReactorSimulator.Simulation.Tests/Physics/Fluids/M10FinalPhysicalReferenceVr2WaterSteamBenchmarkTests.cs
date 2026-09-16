using System.Globalization;
using System.Text;
using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using NuclearReactorSimulator.Simulation.Tests.Physics.Fluids.Reference;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Physics.Fluids;

/// <summary>
/// M10 Final VR2 external water/steam model assessment against the minimum official IAPWS-IF97
/// Regions 1, 2 and 4 needed by the frozen VR0 matrix. Production source is observational only.
/// </summary>
public sealed class M10FinalPhysicalReferenceVr2WaterSteamBenchmarkTests
{
    private const string OptInEnvironmentVariable = "NRS_M10_FINAL_PHYSICAL_REFERENCE_VR2";
    private const string ArtifactDirectoryRelative = "artifacts/m10-final-physical-reference-vr2";
    private const double ReferenceSelfCheckMaximumRelativeError = 1e-8d;
    private const double QuantitativeEducationalMaximumRelativeError = 0.02d;
    private const double BoundedEducationalMaximumRelativeError = 0.10d;
    private const double QualitativeOnlyMaximumRelativeError = 0.25d;
    private const double M10CoreMinimumTemperatureCelsius = 200d;
    private const double M10CoreMaximumTemperatureCelsius = 300d;

    private static readonly double[] SaturationFullPropertyTemperaturesCelsius =
        [100d, 125d, 150d, 175d, 200d, 225d, 250d, 280d, 300d, 340d];

    private static readonly double[] SaturationPressureOnlyTemperaturesCelsius = [360d];

    private static readonly StatePoint[] CompressedLiquidPoints =
    [
        new("VR2-L100-P0.5", 100d, 0.5d, "REGION-1", FluidPhase.SubcooledLiquid),
        new("VR2-L150-P1", 150d, 1d, "REGION-1", FluidPhase.SubcooledLiquid),
        new("VR2-L200-P2", 200d, 2d, "REGION-1", FluidPhase.SubcooledLiquid),
        new("VR2-L250-P7", 250d, 7d, "REGION-1", FluidPhase.SubcooledLiquid),
        new("VR2-L280-P10", 280d, 10d, "REGION-1", FluidPhase.SubcooledLiquid),
    ];

    private static readonly StatePoint[] SuperheatedVaporPoints =
    [
        new("VR2-V200-P0.2", 200d, 0.2d, "REGION-2", FluidPhase.SuperheatedVapor),
        new("VR2-V250-P0.5", 250d, 0.5d, "REGION-2", FluidPhase.SuperheatedVapor),
        new("VR2-V300-P1", 300d, 1d, "REGION-2", FluidPhase.SuperheatedVapor),
        new("VR2-V400-P5", 400d, 5d, "REGION-2", FluidPhase.SuperheatedVapor),
    ];

    [Fact(Explicit = true)]
    [Trait("Category", "M10FinalPhysicalReferenceVR2")]
    public void SimplifiedWaterSteamModel_IsMappedAgainstIndependentIapwsIf97Reference()
    {
        RequireOptIn();
        var artifactDirectory = ResetArtifactDirectory();

        WriteSourceProvenanceManifest(artifactDirectory);
        WriteFrozenPointMatrix(artifactDirectory);

        var selfCheckRows = RunReferenceSelfCheck();
        WriteReferenceSelfCheck(artifactDirectory, selfCheckRows);
        var selfCheckMaximum = selfCheckRows.Max(static row => row.RelativeError);

        if (selfCheckMaximum > ReferenceSelfCheckMaximumRelativeError)
        {
            WriteAssessmentSummary(
                artifactDirectory,
                classification: "REFERENCE-HARNESS-FAIL",
                gatePasses: false,
                selfCheckMaximum,
                numericalComparisonCount: 0,
                m10CoreComparisonCount: 0,
                m10CoreMaximumRelativeError: double.NaN,
                unresolvedInsideEnvelopeCount: 0,
                m10CorePhaseMismatchCount: 0,
                deterministicRepeat: false,
                nextAuthorizedGate: "NONE-RETURN-VR2-ARTIFACTS");

            Assert.Fail(
                $"VR2 IAPWS reference self-check exceeded the frozen {ReferenceSelfCheckMaximumRelativeError:R} relative-error ceiling: {selfCheckMaximum:R}.");
        }

        var model = new SimplifiedWaterSteamThermodynamicModel(
            WaterSteamThermodynamicClosureMode.CorrelationConsistentInverseDomain);
        var numericalRows = RunSaturationComparison(model);
        var inverseRows = RunInverseStateComparison(model);
        numericalRows.AddRange(BuildInverseNumericalRows(inverseRows));

        var repeatModel = new SimplifiedWaterSteamThermodynamicModel(
            WaterSteamThermodynamicClosureMode.CorrelationConsistentInverseDomain);
        var repeatNumericalRows = RunSaturationComparison(repeatModel);
        var repeatInverseRows = RunInverseStateComparison(repeatModel);
        repeatNumericalRows.AddRange(BuildInverseNumericalRows(repeatInverseRows));
        var deterministicRepeat = numericalRows.SequenceEqual(repeatNumericalRows)
            && inverseRows.SequenceEqual(repeatInverseRows);

        WriteNumericalErrorMap(artifactDirectory, numericalRows);
        WriteInverseStateComparison(artifactDirectory, inverseRows);
        WriteDeterministicRepeat(artifactDirectory, deterministicRepeat, numericalRows.Count, inverseRows.Count);

        var summaries = BuildPropertyDomainSummaries(numericalRows);
        WritePropertyDomainSummary(artifactDirectory, summaries);

        var unresolvedInsideEnvelopeCount = inverseRows.Count(static row => row.InsideProductionDocumentedEnvelope && !row.Resolved);
        var m10CorePhaseMismatchCount = inverseRows.Count(static row => row.M10CoreEnvelope && row.Resolved && !row.PhaseMatches);
        var nonFiniteInsideEnvelope = numericalRows.Any(static row => row.InsideProductionDocumentedEnvelope && !row.IsFinite);
        var m10CoreMaterialError = numericalRows.Any(static row => row.M10CoreEnvelope && row.IsFinite && row.RelativeError > QualitativeOnlyMaximumRelativeError);
        var gatePasses = deterministicRepeat
            && unresolvedInsideEnvelopeCount == 0
            && m10CorePhaseMismatchCount == 0
            && !nonFiniteInsideEnvelope
            && !m10CoreMaterialError;
        var classification = gatePasses ? "VR2-PASS-NONBLOCKING" : "MODEL-DISCREPANCY-BLOCKING";
        var m10CoreRows = numericalRows.Where(static row => row.M10CoreEnvelope && row.IsFinite).ToArray();
        var m10CoreMaximumRelativeError = m10CoreRows.Length == 0
            ? double.NaN
            : m10CoreRows.Max(static row => row.RelativeError);
        var nextAuthorizedGate = gatePasses
            ? "VR3-I135-Xe135-Shutdown-Reference-Trajectory"
            : "NONE-RETURN-VR2-ARTIFACTS";

        WriteImpactAndKnownLimitations(
            artifactDirectory,
            classification,
            summaries,
            inverseRows,
            m10CoreMaximumRelativeError);
        WriteAssessmentSummary(
            artifactDirectory,
            classification,
            gatePasses,
            selfCheckMaximum,
            numericalRows.Count,
            m10CoreRows.Length,
            m10CoreMaximumRelativeError,
            unresolvedInsideEnvelopeCount,
            m10CorePhaseMismatchCount,
            deterministicRepeat,
            nextAuthorizedGate);

        Assert.True(
            gatePasses,
            $"VR2 classification was {classification}; return the complete VR2 artifact folder before changing thermodynamic physics/tolerances or proceeding to VR3.");
    }

    private static List<ReferenceSelfCheckRow> RunReferenceSelfCheck()
    {
        var rows = new List<ReferenceSelfCheckRow>();

        AddRegionSelfCheck(rows, "R1-T300-P3", "REGION-1", 300d, 3d, 0.100215168e-2d, 0.112324818e3d * 1_000d, IapwsIf97Reference.Region1);
        AddRegionSelfCheck(rows, "R1-T300-P80", "REGION-1", 300d, 80d, 0.971180894e-3d, 0.106448356e3d * 1_000d, IapwsIf97Reference.Region1);
        AddRegionSelfCheck(rows, "R1-T500-P3", "REGION-1", 500d, 3d, 0.120241800e-2d, 0.971934985e3d * 1_000d, IapwsIf97Reference.Region1);

        AddRegionSelfCheck(rows, "R2-T300-P0.0035", "REGION-2", 300d, 0.0035d, 0.394913866e2d, 0.241169160e4d * 1_000d, IapwsIf97Reference.Region2);
        AddRegionSelfCheck(rows, "R2-T700-P0.0035", "REGION-2", 700d, 0.0035d, 0.923015898e2d, 0.301262819e4d * 1_000d, IapwsIf97Reference.Region2);
        AddRegionSelfCheck(rows, "R2-T700-P30", "REGION-2", 700d, 30d, 0.542946619e-2d, 0.246861076e4d * 1_000d, IapwsIf97Reference.Region2);

        foreach (var (temperatureKelvins, expectedPressureMegapascals) in new[]
        {
            (300d, 0.353658941e-2d),
            (500d, 0.263889776e1d),
            (600d, 0.123443146e2d),
        })
        {
            var actual = IapwsIf97Reference.SaturationPressureMegapascals(temperatureKelvins);
            rows.Add(new ReferenceSelfCheckRow(
                $"R4-PSAT-T{temperatureKelvins:R}",
                "REGION-4",
                "saturation-pressure",
                expectedPressureMegapascals,
                actual,
                RelativeError(expectedPressureMegapascals, actual)));
        }

        foreach (var (pressureMegapascals, expectedTemperatureKelvins) in new[]
        {
            (0.1d, 0.372755919e3d),
            (1d, 0.453035632e3d),
            (10d, 0.584149488e3d),
        })
        {
            var actual = IapwsIf97Reference.SaturationTemperatureKelvins(pressureMegapascals);
            rows.Add(new ReferenceSelfCheckRow(
                $"R4-TSAT-P{pressureMegapascals:R}",
                "REGION-4",
                "saturation-temperature",
                expectedTemperatureKelvins,
                actual,
                RelativeError(expectedTemperatureKelvins, actual)));
        }

        return rows;
    }

    private static void AddRegionSelfCheck(
        ICollection<ReferenceSelfCheckRow> rows,
        string id,
        string region,
        double temperatureKelvins,
        double pressureMegapascals,
        double expectedSpecificVolume,
        double expectedInternalEnergyJoulesPerKilogram,
        Func<double, double, IapwsReferenceState> evaluator)
    {
        var state = evaluator(temperatureKelvins, pressureMegapascals);
        rows.Add(new ReferenceSelfCheckRow(
            id,
            region,
            "specific-volume",
            expectedSpecificVolume,
            state.SpecificVolumeCubicMetresPerKilogram,
            RelativeError(expectedSpecificVolume, state.SpecificVolumeCubicMetresPerKilogram)));
        rows.Add(new ReferenceSelfCheckRow(
            id,
            region,
            "specific-internal-energy",
            expectedInternalEnergyJoulesPerKilogram,
            state.SpecificInternalEnergyJoulesPerKilogram,
            RelativeError(expectedInternalEnergyJoulesPerKilogram, state.SpecificInternalEnergyJoulesPerKilogram)));
    }

    private static List<NumericComparisonRow> RunSaturationComparison(SimplifiedWaterSteamThermodynamicModel model)
    {
        var rows = new List<NumericComparisonRow>();

        foreach (var temperatureCelsius in SaturationFullPropertyTemperaturesCelsius)
        {
            var temperature = Temperature.FromDegreesCelsius(temperatureCelsius);
            var referencePressureMegapascals = IapwsIf97Reference.SaturationPressureMegapascals(temperature.Kelvins);
            var referenceLiquid = IapwsIf97Reference.Region1(temperature.Kelvins, referencePressureMegapascals);
            var referenceVapor = IapwsIf97Reference.Region2(temperature.Kelvins, referencePressureMegapascals);
            var production = model.GetSaturationProperties(temperature);
            var pointId = $"VR2-SAT-{temperatureCelsius:R}C";
            var m10Core = IsM10CoreTemperature(temperatureCelsius);

            rows.Add(CreateNumericRow(pointId, "SATURATION", "saturation-pressure", "Pa",
                referencePressureMegapascals * 1_000_000d, production.Pressure.Pascals, "REGION-4", "SATURATION-BOUNDARY", true, m10Core));
            rows.Add(CreateNumericRow(pointId, "SATURATION", "saturated-liquid-density", "kg/m3",
                referenceLiquid.DensityKilogramsPerCubicMetre, production.SaturatedLiquidDensity.KilogramsPerCubicMetre, "REGION-1-SATURATED-LIQUID", "SATURATION-BOUNDARY", true, m10Core));
            rows.Add(CreateNumericRow(pointId, "SATURATION", "saturated-vapor-density", "kg/m3",
                referenceVapor.DensityKilogramsPerCubicMetre, production.SaturatedVaporDensity.KilogramsPerCubicMetre, "REGION-2-SATURATED-VAPOR", "SATURATION-BOUNDARY", true, m10Core));
            rows.Add(CreateNumericRow(pointId, "SATURATION", "saturated-liquid-internal-energy", "J/kg",
                referenceLiquid.SpecificInternalEnergyJoulesPerKilogram, production.SaturatedLiquidInternalEnergy.JoulesPerKilogram, "REGION-1-SATURATED-LIQUID", "SATURATION-BOUNDARY", true, m10Core));
            rows.Add(CreateNumericRow(pointId, "SATURATION", "saturated-vapor-internal-energy", "J/kg",
                referenceVapor.SpecificInternalEnergyJoulesPerKilogram, production.SaturatedVaporInternalEnergy.JoulesPerKilogram, "REGION-2-SATURATED-VAPOR", "SATURATION-BOUNDARY", true, m10Core));
            rows.Add(CreateNumericRow(pointId, "SATURATION", "phase-internal-energy-difference", "J/kg",
                referenceVapor.SpecificInternalEnergyJoulesPerKilogram - referenceLiquid.SpecificInternalEnergyJoulesPerKilogram,
                production.LatentInternalEnergy.JoulesPerKilogram,
                "REGION-1/REGION-2-SATURATION",
                "SATURATION-BOUNDARY",
                true,
                m10Core));
        }

        foreach (var temperatureCelsius in SaturationPressureOnlyTemperaturesCelsius)
        {
            var temperature = Temperature.FromDegreesCelsius(temperatureCelsius);
            var referencePressureMegapascals = IapwsIf97Reference.SaturationPressureMegapascals(temperature.Kelvins);
            var production = model.GetSaturationProperties(temperature);
            rows.Add(CreateNumericRow(
                $"VR2-SAT-{temperatureCelsius:R}C-PONLY",
                "SATURATION",
                "saturation-pressure",
                "Pa",
                referencePressureMegapascals * 1_000_000d,
                production.Pressure.Pascals,
                "REGION-4",
                "SATURATION-BOUNDARY",
                true,
                m10CoreEnvelope: false));
        }

        return rows;
    }

    private static List<InverseStateRow> RunInverseStateComparison(SimplifiedWaterSteamThermodynamicModel model)
    {
        var rows = new List<InverseStateRow>();
        foreach (var point in CompressedLiquidPoints)
        {
            rows.Add(RunInverseStatePoint(model, point, IapwsIf97Reference.Region1));
        }

        foreach (var point in SuperheatedVaporPoints)
        {
            rows.Add(RunInverseStatePoint(model, point, IapwsIf97Reference.Region2));
        }

        return rows;
    }

    private static InverseStateRow RunInverseStatePoint(
        SimplifiedWaterSteamThermodynamicModel model,
        StatePoint point,
        Func<double, double, IapwsReferenceState> referenceEvaluator)
    {
        var temperature = Temperature.FromDegreesCelsius(point.TemperatureCelsius);
        var reference = referenceEvaluator(temperature.Kelvins, point.PressureMegapascals);
        var definition = new FluidNodeDefinition(
            point.Id,
            Volume.FromCubicMetres(reference.SpecificVolumeCubicMetresPerKilogram));
        var inventory = new FluidNodeInventory(
            Mass.FromKilograms(1d),
            Energy.FromJoules(reference.SpecificInternalEnergyJoulesPerKilogram));
        var insideEnvelope = IsInsideProductionDocumentedEnvelope(point);
        var m10Core = IsM10CoreTemperature(point.TemperatureCelsius);

        try
        {
            var production = model.Resolve(definition, inventory, PreviousState());
            return new InverseStateRow(
                point.Id,
                point.ExpectedPhase == FluidPhase.SubcooledLiquid ? "INVERSE-COMPRESSED-LIQUID" : "INVERSE-SUPERHEATED-VAPOR",
                point.ReferenceRegion,
                point.TemperatureCelsius,
                point.PressureMegapascals,
                reference.DensityKilogramsPerCubicMetre,
                reference.SpecificInternalEnergyJoulesPerKilogram,
                point.ExpectedPhase.ToString(),
                Resolved: true,
                production.Temperature.DegreesCelsius,
                production.Pressure.Megapascals,
                production.Phase.ToString(),
                PhaseMatches: production.Phase == point.ExpectedPhase,
                insideEnvelope,
                m10Core);
        }
        catch (WaterSteamStateOutOfRangeException)
        {
            return new InverseStateRow(
                point.Id,
                point.ExpectedPhase == FluidPhase.SubcooledLiquid ? "INVERSE-COMPRESSED-LIQUID" : "INVERSE-SUPERHEATED-VAPOR",
                point.ReferenceRegion,
                point.TemperatureCelsius,
                point.PressureMegapascals,
                reference.DensityKilogramsPerCubicMetre,
                reference.SpecificInternalEnergyJoulesPerKilogram,
                point.ExpectedPhase.ToString(),
                Resolved: false,
                double.NaN,
                double.NaN,
                "UNRESOLVED",
                PhaseMatches: false,
                insideEnvelope,
                m10Core);
        }
    }

    private static IEnumerable<NumericComparisonRow> BuildInverseNumericalRows(IEnumerable<InverseStateRow> inverseRows)
    {
        foreach (var row in inverseRows)
        {
            var referenceTemperatureKelvins = Temperature.FromDegreesCelsius(row.ReferenceTemperatureCelsius).Kelvins;
            var productionTemperatureKelvins = row.Resolved
                ? Temperature.FromDegreesCelsius(row.ProductionTemperatureCelsius).Kelvins
                : double.NaN;
            yield return CreateNumericRow(
                row.PointId,
                row.Domain,
                "resolved-temperature",
                "K",
                referenceTemperatureKelvins,
                productionTemperatureKelvins,
                row.ReferenceRegion,
                row.ProductionPhase,
                row.InsideProductionDocumentedEnvelope,
                row.M10CoreEnvelope);
            yield return CreateNumericRow(
                row.PointId,
                row.Domain,
                "resolved-pressure",
                "Pa",
                row.ReferencePressureMegapascals * 1_000_000d,
                row.Resolved ? row.ProductionPressureMegapascals * 1_000_000d : double.NaN,
                row.ReferenceRegion,
                row.ProductionPhase,
                row.InsideProductionDocumentedEnvelope,
                row.M10CoreEnvelope);
        }
    }

    private static NumericComparisonRow CreateNumericRow(
        string pointId,
        string domain,
        string property,
        string units,
        double referenceValue,
        double productionValue,
        string referenceRegionPhase,
        string productionPhase,
        bool insideProductionDocumentedEnvelope,
        bool m10CoreEnvelope)
    {
        var isFinite = double.IsFinite(referenceValue) && double.IsFinite(productionValue);
        var absoluteError = isFinite ? Math.Abs(productionValue - referenceValue) : double.NaN;
        var relativeError = isFinite ? RelativeError(referenceValue, productionValue) : double.NaN;
        return new NumericComparisonRow(
            pointId,
            domain,
            property,
            units,
            referenceValue,
            productionValue,
            absoluteError,
            relativeError,
            referenceRegionPhase,
            productionPhase,
            insideProductionDocumentedEnvelope,
            m10CoreEnvelope,
            isFinite);
    }

    private static List<PropertyDomainSummary> BuildPropertyDomainSummaries(IEnumerable<NumericComparisonRow> rows)
    {
        var summaries = new List<PropertyDomainSummary>();
        foreach (var group in rows.GroupBy(static row => (row.Domain, row.Property)))
        {
            var groupedRows = group.ToArray();
            var finiteRows = groupedRows.Where(static row => row.IsFinite).ToArray();
            var coreRows = finiteRows.Where(static row => row.M10CoreEnvelope).ToArray();
            var allErrors = finiteRows.Select(static row => row.RelativeError).OrderBy(static value => value).ToArray();
            var coreErrors = coreRows.Select(static row => row.RelativeError).OrderBy(static value => value).ToArray();
            var widerEnvelopeErrors = finiteRows
                .Where(static row => !row.M10CoreEnvelope)
                .Select(static row => row.RelativeError)
                .ToArray();
            var coreMaximum = coreErrors.Length == 0 ? double.NaN : coreErrors[^1];

            summaries.Add(new PropertyDomainSummary(
                group.Key.Domain,
                group.Key.Property,
                groupedRows.Length,
                finiteRows.Length,
                allErrors.Length == 0 ? double.NaN : allErrors[^1],
                Median(allErrors),
                Percentile95(allErrors),
                coreRows.Length,
                coreMaximum,
                Median(coreErrors),
                Percentile95(coreErrors),
                CoreClaimBand(coreMaximum),
                widerEnvelopeErrors.Any(static value => value > QualitativeOnlyMaximumRelativeError)));
        }

        return summaries
            .OrderBy(static summary => summary.Domain, StringComparer.Ordinal)
            .ThenBy(static summary => summary.Property, StringComparer.Ordinal)
            .ToList();
    }

    private static string CoreClaimBand(double maximumRelativeError)
    {
        if (!double.IsFinite(maximumRelativeError))
        {
            return "NOT-APPLICABLE";
        }

        if (maximumRelativeError <= QuantitativeEducationalMaximumRelativeError)
        {
            return "QUANTITATIVE-EDUCATIONAL";
        }

        if (maximumRelativeError <= BoundedEducationalMaximumRelativeError)
        {
            return "BOUNDED-EDUCATIONAL";
        }

        if (maximumRelativeError <= QualitativeOnlyMaximumRelativeError)
        {
            return "QUALITATIVE-ONLY";
        }

        return "MODEL-DISCREPANCY-BLOCKING";
    }

    private static double Median(IReadOnlyList<double> sortedValues)
    {
        if (sortedValues.Count == 0)
        {
            return double.NaN;
        }

        var middle = sortedValues.Count / 2;
        return sortedValues.Count % 2 == 0
            ? (sortedValues[middle - 1] + sortedValues[middle]) / 2d
            : sortedValues[middle];
    }

    private static double Percentile95(IReadOnlyList<double> sortedValues)
    {
        if (sortedValues.Count == 0)
        {
            return double.NaN;
        }

        var nearestRank = (int)Math.Ceiling(0.95d * sortedValues.Count);
        return sortedValues[Math.Clamp(nearestRank - 1, 0, sortedValues.Count - 1)];
    }

    private static bool IsM10CoreTemperature(double temperatureCelsius)
        => temperatureCelsius >= M10CoreMinimumTemperatureCelsius
            && temperatureCelsius <= M10CoreMaximumTemperatureCelsius;

    private static bool IsInsideProductionDocumentedEnvelope(StatePoint point)
        => point.TemperatureCelsius >= 0.01d
            && (point.ExpectedPhase == FluidPhase.SuperheatedVapor
                ? Temperature.FromDegreesCelsius(point.TemperatureCelsius) <= SimplifiedWaterSteamThermodynamicModel.MaximumSuperheatedTemperature
                : Temperature.FromDegreesCelsius(point.TemperatureCelsius) <= SimplifiedWaterSteamThermodynamicModel.MaximumSaturationTemperature);

    private static FluidThermodynamicState PreviousState()
        => new(Pressure.FromKilopascals(101.325d), Temperature.FromDegreesCelsius(20d));

    private static void WriteSourceProvenanceManifest(string artifactDirectory)
    {
        File.WriteAllLines(
            Path.Combine(artifactDirectory, "01-source-provenance-manifest.txt"),
            [
                "gate=VR2-IAPWS-IF97-Water-Steam-Error-Map",
                "authority=International Association for the Properties of Water and Steam (IAPWS)",
                "release=IAPWS R7-97(2012) Revised Release on the IAPWS Industrial Formulation 1997",
                "official-release-page=https://www.iapws.org/relguide/IF97-Rev.html",
                "official-pdf=https://www.iapws.org/relguide/IF97-Rev.pdf",
                "reference-regions=Region 1 basic equation; Region 2 basic equation; Region 4 saturation pressure/temperature",
                "reference-selfcheck=official Tables 5, 15, 35 and 36",
                "reference-helper=tests/NuclearReactorSimulator.Simulation.Tests/Physics/Fluids/Reference/IapwsIf97Reference.cs",
                "reference-calls-production-model=False",
                "third-party-if97-library=False",
                "runtime-package-added=False",
                "production-source-changed=False",
                "production-closure-mode=CorrelationConsistentInverseDomain",
            ]);
    }

    private static void WriteFrozenPointMatrix(string artifactDirectory)
    {
        var builder = new StringBuilder();
        builder.AppendLine("point_id,domain,temperature_celsius,pressure_megapascals,reference_region,expected_phase,inside_production_documented_envelope,m10_core_envelope");
        foreach (var temperatureCelsius in SaturationFullPropertyTemperaturesCelsius)
        {
            builder.AppendLine(FormattableString.Invariant($"VR2-SAT-{temperatureCelsius:R}C,SATURATION,{temperatureCelsius:R},AUTO-REGION4,REGION-4+REGION-1+REGION-2,SATURATION-BOUNDARY,true,{IsM10CoreTemperature(temperatureCelsius).ToString().ToLowerInvariant()}"));
        }

        foreach (var temperatureCelsius in SaturationPressureOnlyTemperaturesCelsius)
        {
            builder.AppendLine(FormattableString.Invariant($"VR2-SAT-{temperatureCelsius:R}C-PONLY,SATURATION-PRESSURE-ONLY,{temperatureCelsius:R},AUTO-REGION4,REGION-4,SATURATION-BOUNDARY,true,false"));
        }

        foreach (var point in CompressedLiquidPoints.Concat(SuperheatedVaporPoints))
        {
            builder.AppendLine(FormattableString.Invariant(
                $"{point.Id},{(point.ExpectedPhase == FluidPhase.SubcooledLiquid ? "INVERSE-COMPRESSED-LIQUID" : "INVERSE-SUPERHEATED-VAPOR")},{point.TemperatureCelsius:R},{point.PressureMegapascals:R},{point.ReferenceRegion},{point.ExpectedPhase},{IsInsideProductionDocumentedEnvelope(point).ToString().ToLowerInvariant()},{IsM10CoreTemperature(point.TemperatureCelsius).ToString().ToLowerInvariant()}"));
        }

        File.WriteAllText(Path.Combine(artifactDirectory, "02-frozen-point-matrix.csv"), builder.ToString(), Encoding.UTF8);
    }

    private static void WriteReferenceSelfCheck(string artifactDirectory, IEnumerable<ReferenceSelfCheckRow> rows)
    {
        var builder = new StringBuilder();
        builder.AppendLine("id,region,property,official_verification_value,helper_value,relative_error,passes_1e-8");
        foreach (var row in rows)
        {
            builder.AppendLine(FormattableString.Invariant(
                $"{row.Id},{row.Region},{row.Property},{row.OfficialValue:R},{row.HelperValue:R},{row.RelativeError:R},{(row.RelativeError <= ReferenceSelfCheckMaximumRelativeError).ToString().ToLowerInvariant()}"));
        }

        File.WriteAllText(Path.Combine(artifactDirectory, "03-reference-selfcheck.csv"), builder.ToString(), Encoding.UTF8);
    }

    private static void WriteNumericalErrorMap(string artifactDirectory, IEnumerable<NumericComparisonRow> rows)
    {
        var builder = new StringBuilder();
        builder.AppendLine("point_id,domain,property,units,reference_value,production_value,absolute_error,relative_error,reference_region_phase,production_phase,inside_production_documented_envelope,m10_core_envelope,is_finite");
        foreach (var row in rows)
        {
            builder.AppendLine(FormattableString.Invariant(
                $"{row.PointId},{row.Domain},{row.Property},{row.Units},{row.ReferenceValue:R},{row.ProductionValue:R},{row.AbsoluteError:R},{row.RelativeError:R},{row.ReferenceRegionPhase},{row.ProductionPhase},{row.InsideProductionDocumentedEnvelope.ToString().ToLowerInvariant()},{row.M10CoreEnvelope.ToString().ToLowerInvariant()},{row.IsFinite.ToString().ToLowerInvariant()}"));
        }

        File.WriteAllText(Path.Combine(artifactDirectory, "04-numerical-error-map.csv"), builder.ToString(), Encoding.UTF8);
    }

    private static void WriteInverseStateComparison(string artifactDirectory, IEnumerable<InverseStateRow> rows)
    {
        var builder = new StringBuilder();
        builder.AppendLine("point_id,domain,reference_region,reference_temperature_celsius,reference_pressure_megapascals,reference_density_kg_m3,reference_internal_energy_j_kg,expected_phase,resolved,production_temperature_celsius,production_pressure_megapascals,production_phase,phase_matches,inside_production_documented_envelope,m10_core_envelope");
        foreach (var row in rows)
        {
            builder.AppendLine(FormattableString.Invariant(
                $"{row.PointId},{row.Domain},{row.ReferenceRegion},{row.ReferenceTemperatureCelsius:R},{row.ReferencePressureMegapascals:R},{row.ReferenceDensityKilogramsPerCubicMetre:R},{row.ReferenceSpecificInternalEnergyJoulesPerKilogram:R},{row.ExpectedPhase},{row.Resolved.ToString().ToLowerInvariant()},{row.ProductionTemperatureCelsius:R},{row.ProductionPressureMegapascals:R},{row.ProductionPhase},{row.PhaseMatches.ToString().ToLowerInvariant()},{row.InsideProductionDocumentedEnvelope.ToString().ToLowerInvariant()},{row.M10CoreEnvelope.ToString().ToLowerInvariant()}"));
        }

        File.WriteAllText(Path.Combine(artifactDirectory, "05-inverse-state-comparison.csv"), builder.ToString(), Encoding.UTF8);
    }

    private static void WriteDeterministicRepeat(
        string artifactDirectory,
        bool deterministicRepeat,
        int numericalComparisonCount,
        int inverseStateCount)
    {
        File.WriteAllLines(
            Path.Combine(artifactDirectory, "09-deterministic-repeat.txt"),
            [
                $"deterministic-repeat={deterministicRepeat}",
                $"numerical-comparison-count={numericalComparisonCount}",
                $"inverse-state-count={inverseStateCount}",
                "comparison=exact-record-sequence-equality-across-independent-model-instances",
            ]);
    }

    private static void WritePropertyDomainSummary(string artifactDirectory, IEnumerable<PropertyDomainSummary> summaries)
    {
        var builder = new StringBuilder();
        builder.AppendLine("domain,property,row_count,finite_row_count,max_relative_error_all,median_relative_error_all,p95_relative_error_all,m10_core_row_count,max_relative_error_core,median_relative_error_core,p95_relative_error_core,m10_core_claim_band,wider_envelope_any_error_gt_25pct");
        foreach (var row in summaries)
        {
            builder.AppendLine(FormattableString.Invariant(
                $"{row.Domain},{row.Property},{row.RowCount},{row.FiniteRowCount},{row.MaximumRelativeErrorAll:R},{row.MedianRelativeErrorAll:R},{row.P95RelativeErrorAll:R},{row.M10CoreRowCount},{row.MaximumRelativeErrorCore:R},{row.MedianRelativeErrorCore:R},{row.P95RelativeErrorCore:R},{row.M10CoreClaimBand},{row.WiderEnvelopeAnyErrorGreaterThan25Percent.ToString().ToLowerInvariant()}"));
        }

        File.WriteAllText(Path.Combine(artifactDirectory, "06-property-domain-summary.csv"), builder.ToString(), Encoding.UTF8);
    }

    private static void WriteAssessmentSummary(
        string artifactDirectory,
        string classification,
        bool gatePasses,
        double selfCheckMaximum,
        int numericalComparisonCount,
        int m10CoreComparisonCount,
        double m10CoreMaximumRelativeError,
        int unresolvedInsideEnvelopeCount,
        int m10CorePhaseMismatchCount,
        bool deterministicRepeat,
        string nextAuthorizedGate)
    {
        File.WriteAllLines(
            Path.Combine(artifactDirectory, "07-vr2-assessment-summary.txt"),
            [
                $"classification={classification}",
                $"gate-passes={gatePasses}",
                $"reference-selfcheck-max-relative-error={selfCheckMaximum.ToString("R", CultureInfo.InvariantCulture)}",
                $"reference-selfcheck-ceiling={ReferenceSelfCheckMaximumRelativeError.ToString("R", CultureInfo.InvariantCulture)}",
                $"numerical-comparison-count={numericalComparisonCount}",
                $"m10-core-comparison-count={m10CoreComparisonCount}",
                $"m10-core-max-relative-error={m10CoreMaximumRelativeError.ToString("R", CultureInfo.InvariantCulture)}",
                $"unresolved-inside-production-envelope-count={unresolvedInsideEnvelopeCount}",
                $"m10-core-phase-mismatch-count={m10CorePhaseMismatchCount}",
                $"deterministic-repeat={deterministicRepeat}",
                $"blocking-threshold-material-m10-core-relative-error={QualitativeOnlyMaximumRelativeError.ToString("R", CultureInfo.InvariantCulture)}",
                $"next-authorized-gate={nextAuthorizedGate}",
                "production-repair-authorized=False",
                "exact-v9-change-authorized=False",
                "p3-r1-execution-authorized=False",
                "second-replacement-long-authorized=False",
            ]);
    }

    private static void WriteImpactAndKnownLimitations(
        string artifactDirectory,
        string classification,
        IEnumerable<PropertyDomainSummary> summaries,
        IEnumerable<InverseStateRow> inverseRows,
        double m10CoreMaximumRelativeError)
    {
        var summaryArray = summaries.ToArray();
        var inverseArray = inverseRows.ToArray();
        var coreBands = string.Join(
            ";",
            summaryArray
                .Where(static row => row.M10CoreRowCount > 0)
                .Select(static row => $"{row.Domain}/{row.Property}={row.M10CoreClaimBand}"));
        var outsideCoreLargeErrors = summaryArray.Count(static row => row.WiderEnvelopeAnyErrorGreaterThan25Percent);
        var outsideCorePhaseMismatches = inverseArray.Count(static row => !row.M10CoreEnvelope && row.Resolved && !row.PhaseMatches);

        File.WriteAllLines(
            Path.Combine(artifactDirectory, "08-impact-known-limitations.txt"),
            [
                $"classification={classification}",
                $"m10-core-max-relative-error={m10CoreMaximumRelativeError.ToString("R", CultureInfo.InvariantCulture)}",
                $"m10-core-claim-bands={coreBands}",
                $"wider-envelope-property-domains-with-error-gt-25pct={outsideCoreLargeErrors}",
                $"outside-core-phase-mismatch-count={outsideCorePhaseMismatches}",
                "claim-boundary=VR2 quantitatively maps the existing simplified closure against IAPWS-IF97; it does not relabel production as a full IF97 implementation.",
                "saturation-boundary-note=Production already uses the official Region-4 pressure relation; density and energy closures remain deliberately simplified and are assessed rather than repaired here.",
                "inverse-state-note=Reference IF97 rho/u are converted to a one-kilogram control-volume input before production Resolve, avoiding circular production-generated expected states.",
                "wider-envelope-policy=Errors outside the M10-core envelope are reported as support-limit evidence and do not independently trigger the >25% M10-core blocking condition; unresolved in-envelope states remain blocking.",
                "production-change=NONE",
                "next-action=Return the complete VR2 artifact folder. Proceed to VR3 only if 07-vr2-assessment-summary.txt reports gate-passes=True.",
            ]);
    }

    private static string ResetArtifactDirectory()
    {
        var root = FindRepositoryRoot();
        var path = Path.Combine(root, ArtifactDirectoryRelative.Replace('/', Path.DirectorySeparatorChar));
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }

        Directory.CreateDirectory(path);
        return path;
    }

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

    private static void RequireOptIn()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable(OptInEnvironmentVariable), "1", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"VR2 is explicit and fail-closed. Set {OptInEnvironmentVariable}=1 through the authorized runner.");
        }
    }

    private static double RelativeError(double reference, double candidate)
        => Math.Abs(candidate - reference) / Math.Max(Math.Abs(reference), 1e-300d);

    private readonly record struct StatePoint(
        string Id,
        double TemperatureCelsius,
        double PressureMegapascals,
        string ReferenceRegion,
        FluidPhase ExpectedPhase);

    private readonly record struct ReferenceSelfCheckRow(
        string Id,
        string Region,
        string Property,
        double OfficialValue,
        double HelperValue,
        double RelativeError);

    private readonly record struct NumericComparisonRow(
        string PointId,
        string Domain,
        string Property,
        string Units,
        double ReferenceValue,
        double ProductionValue,
        double AbsoluteError,
        double RelativeError,
        string ReferenceRegionPhase,
        string ProductionPhase,
        bool InsideProductionDocumentedEnvelope,
        bool M10CoreEnvelope,
        bool IsFinite);

    private readonly record struct InverseStateRow(
        string PointId,
        string Domain,
        string ReferenceRegion,
        double ReferenceTemperatureCelsius,
        double ReferencePressureMegapascals,
        double ReferenceDensityKilogramsPerCubicMetre,
        double ReferenceSpecificInternalEnergyJoulesPerKilogram,
        string ExpectedPhase,
        bool Resolved,
        double ProductionTemperatureCelsius,
        double ProductionPressureMegapascals,
        string ProductionPhase,
        bool PhaseMatches,
        bool InsideProductionDocumentedEnvelope,
        bool M10CoreEnvelope);

    private readonly record struct PropertyDomainSummary(
        string Domain,
        string Property,
        int RowCount,
        int FiniteRowCount,
        double MaximumRelativeErrorAll,
        double MedianRelativeErrorAll,
        double P95RelativeErrorAll,
        int M10CoreRowCount,
        double MaximumRelativeErrorCore,
        double MedianRelativeErrorCore,
        double P95RelativeErrorCore,
        string M10CoreClaimBand,
        bool WiderEnvelopeAnyErrorGreaterThan25Percent);
}
