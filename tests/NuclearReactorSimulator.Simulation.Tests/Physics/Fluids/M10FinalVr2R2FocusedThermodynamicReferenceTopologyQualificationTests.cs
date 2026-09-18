using System.Globalization;
using System.Text;
using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using NuclearReactorSimulator.Simulation.Tests.Physics.Fluids.Reference;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Physics.Fluids;

/// <summary>
/// R2 independently requalifies the explicit production mode-2 thermodynamic path against the
/// frozen IF97/RP1A reference domains. It does not compose mode 2 into exact-v9 or change runtime defaults.
/// </summary>
public sealed class M10FinalVr2R2FocusedThermodynamicReferenceTopologyQualificationTests
{
    private const string OptInEnvironmentVariable = "NRS_M10_FINAL_VR2_R2_QUALIFICATION1";
    private const string ArtifactDirectoryRelative = "artifacts/m10-final-physical-reference-vr2-r2-focused-thermodynamic-reference-topology-qualification1";
    private const string FrozenRp1aRelative = "eng/frozen-evidence/ordinary/M10FinalVR2EngineeringRepairPlanning1_RP1A_Artifacts";
    private const double ReferenceSelfCheckMaximumRelativeError = 1e-8d;
    private const double ExistingVr2BlockingCeilingFraction = 0.25d;
    private const double PlanningTargetFraction = 0.10d;
    private const double MaximumLiquidSeamPressureJumpMegapascals = 0.0005137228525280754d;
    private const double MaximumLiquidSeamTemperatureJumpCelsius = 3.5596193356468575E-05d;
    private const double MaximumVaporSeamPressureJumpMegapascals = 0.00026625516826150886d;
    private const double MaximumVaporSeamTemperatureJumpCelsius = 0.001903154074568647d;
    private static readonly UTF8Encoding Utf8WithoutBom = new(false);

    [Fact(Explicit = true)]
    [Trait("Category", "M10FinalVr2R2FocusedThermodynamicReferenceTopologyQualification1")]
    public void R2_Mode2_IsIndependentlyQualifiedAgainstIf97AndFrozenTopology()
    {
        Assert.Equal("1", Environment.GetEnvironmentVariable(OptInEnvironmentVariable));
        var root = FindRepositoryRoot();
        var artifactDirectory = Path.Combine(root, ArtifactDirectoryRelative.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(artifactDirectory);
        var frozen = Path.Combine(root, FrozenRp1aRelative.Replace('/', Path.DirectorySeparatorChar));

        var selfCheck = RunReferenceSelfCheck();
        WriteReferenceSelfCheck(artifactDirectory, selfCheck);
        var selfCheckMaximum = selfCheck.Max(static row => row.RelativeError);
        Assert.True(selfCheckMaximum <= ReferenceSelfCheckMaximumRelativeError,
            $"R2 IF97 reference self-check exceeded {ReferenceSelfCheckMaximumRelativeError:R}: {selfCheckMaximum:R}.");

        var vr2Rows = LoadVr2Rows(Path.Combine(frozen, "02-vr2-reference-point-corpus.csv"));
        var exactRows = LoadExactRows(Path.Combine(frozen, "03-exact-v9-node-corpus.csv"));
        var seamRows = LoadSeamRows(Path.Combine(frozen, "05-seam-probe-map.csv"));
        Assert.Equal(40, vr2Rows.Count);
        Assert.Equal(39, vr2Rows.Count(static row => row.InverseApplicable));
        Assert.Equal(360, exactRows.Count);
        Assert.Equal(1_280, seamRows.Count);
        Assert.Equal(320, seamRows.Select(static row => row.BoundaryIndex).Distinct().Count());

        var model = new SimplifiedWaterSteamThermodynamicModel(
            WaterSteamThermodynamicClosureMode.ReferenceConsistentTabulatedInverseDomain);
        var vr2 = EvaluateVr2(model, vr2Rows);
        var exact = EvaluateExact(model, exactRows);
        var seam = EvaluateSeam(model, seamRows);
        var continuity = BuildSeamContinuity(seam);

        WriteVr2Qualification(artifactDirectory, vr2);
        WriteExactQualification(artifactDirectory, exact);
        WriteSeamQualification(artifactDirectory, seam, continuity);

        var repeatModel = new SimplifiedWaterSteamThermodynamicModel(
            WaterSteamThermodynamicClosureMode.ReferenceConsistentTabulatedInverseDomain);
        var repeatMismatches = CountRepeatMismatches(model, repeatModel, vr2Rows, exactRows, seamRows);
        WriteDeterministicRepeat(artifactDirectory, repeatMismatches);

        var vr2Unresolved = vr2.Count(static row => row.InverseApplicable && !row.Resolved);
        var vr2PhaseMismatch = vr2.Count(static row => row.InverseApplicable && row.Resolved && !row.PhaseMatches);
        var exactUnresolved = exact.Count(static row => !row.Resolved);
        var exactPhaseMismatch = exact.Count(static row => row.Resolved && !row.PhaseMatches);
        var seamUnresolved = seam.Count(static row => !row.Resolved);
        var seamPhaseMismatch = seam.Count(static row => row.Resolved && !row.PhaseMatches);
        var exactPhaseAgreementPercent = 100d * (exact.Count - exactUnresolved - exactPhaseMismatch) / exact.Count;
        var maximumCorePressureRelativeError = MaxFinite(
            vr2.Where(static row => row.InverseApplicable).Select(static row => row.PressureRelativeError)
                .Concat(exact.Select(static row => row.PressureRelativeError)));
        var maximumCoreTemperatureRelativeKelvinError = MaxFinite(
            vr2.Where(static row => row.InverseApplicable).Select(static row => row.TemperatureRelativeKelvinError)
                .Concat(exact.Select(static row => row.TemperatureRelativeKelvinError)));
        var maximumHotCorePressureRelativeError = MaxFinite(
            vr2.Where(static row => row.InverseApplicable && row.SourceFamily == "COMPRESSED-LIQUID")
                .Select(static row => row.PressureRelativeError)
                .Concat(exact.Select(static row => row.PressureRelativeError)));
        var referenceCorpusInputMaximumRelativeError = MaxFinite(vr2.Select(static row => row.ReferenceCorpusInputMaximumRelativeError));
        var boundaryOnly = vr2.Single(static row => !row.InverseApplicable);

        var vr2Pass = vr2Unresolved == 0 && vr2PhaseMismatch == 0
            && vr2.Where(static row => row.InverseApplicable).All(static row => row.IsFinite);
        var exactPass = exactUnresolved == 0 && exactPhaseMismatch == 0
            && exactPhaseAgreementPercent == 100d && exact.All(static row => row.IsFinite);
        var corePass = maximumCorePressureRelativeError <= ExistingVr2BlockingCeilingFraction
            && maximumCoreTemperatureRelativeKelvinError <= ExistingVr2BlockingCeilingFraction;
        var planningTargetPass = maximumHotCorePressureRelativeError <= PlanningTargetFraction;
        var seamPass = seamUnresolved == 0 && seamPhaseMismatch == 0
            && continuity.LiquidPressureJumpMegapascals <= MaximumLiquidSeamPressureJumpMegapascals
            && continuity.LiquidTemperatureJumpCelsius <= MaximumLiquidSeamTemperatureJumpCelsius
            && continuity.VaporPressureJumpMegapascals <= MaximumVaporSeamPressureJumpMegapascals
            && continuity.VaporTemperatureJumpCelsius <= MaximumVaporSeamTemperatureJumpCelsius;
        var referenceIntegrityPass = referenceCorpusInputMaximumRelativeError <= ReferenceSelfCheckMaximumRelativeError
            && boundaryOnly.BoundaryReferenceRelativeError <= ReferenceSelfCheckMaximumRelativeError;
        var deterministicPass = repeatMismatches == 0;
        var pass = vr2Pass && exactPass && corePass && planningTargetPass && seamPass && referenceIntegrityPass && deterministicPass;

        WriteTopologySummary(
            artifactDirectory,
            pass,
            selfCheckMaximum,
            referenceCorpusInputMaximumRelativeError,
            boundaryOnly.BoundaryReferenceRelativeError,
            vr2Unresolved,
            vr2PhaseMismatch,
            exactUnresolved,
            exactPhaseMismatch,
            exactPhaseAgreementPercent,
            seamUnresolved,
            seamPhaseMismatch,
            maximumCorePressureRelativeError,
            maximumCoreTemperatureRelativeKelvinError,
            maximumHotCorePressureRelativeError,
            continuity,
            repeatMismatches);

        Assert.True(referenceIntegrityPass, "R2 independent IF97/reference-corpus integrity failed.");
        Assert.True(vr2Pass, $"R2 VR2 matrix failed: unresolved={vr2Unresolved}, phaseMismatch={vr2PhaseMismatch}.");
        Assert.True(exactPass, $"R2 exact-v9 topology failed: unresolved={exactUnresolved}, phaseMismatch={exactPhaseMismatch}, agreement={exactPhaseAgreementPercent:R}%.");
        Assert.True(corePass, $"R2 25% blocking ceiling failed: pressure={maximumCorePressureRelativeError:R}, temperatureK={maximumCoreTemperatureRelativeKelvinError:R}.");
        Assert.True(planningTargetPass, $"R2 10% compressed-liquid/hot-primary pressure target failed: {maximumHotCorePressureRelativeError:R}.");
        Assert.True(seamPass,
            $"R2 seam topology/continuity failed: unresolved={seamUnresolved}, phaseMismatch={seamPhaseMismatch}, liquidP={continuity.LiquidPressureJumpMegapascals:R}, liquidT={continuity.LiquidTemperatureJumpCelsius:R}, vaporP={continuity.VaporPressureJumpMegapascals:R}, vaporT={continuity.VaporTemperatureJumpCelsius:R}.");
        Assert.Equal(0, repeatMismatches);
        Assert.True(pass);
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
        foreach (var point in new[] { (300d, 0.353658941e-2d), (500d, 0.263889776e1d), (600d, 0.123443146e2d) })
        {
            var actual = IapwsIf97Reference.SaturationPressureMegapascals(point.Item1);
            rows.Add(new ReferenceSelfCheckRow($"R4-PSAT-T{point.Item1.ToString("R", CultureInfo.InvariantCulture)}", "REGION-4", "saturation-pressure", point.Item2, actual, RelativeError(actual, point.Item2)));
        }
        foreach (var point in new[] { (0.1d, 0.372755919e3d), (1d, 0.453035632e3d), (10d, 0.584149488e3d) })
        {
            var actual = IapwsIf97Reference.SaturationTemperatureKelvins(point.Item1);
            rows.Add(new ReferenceSelfCheckRow($"R4-TSAT-P{point.Item1.ToString("R", CultureInfo.InvariantCulture)}", "REGION-4", "saturation-temperature", point.Item2, actual, RelativeError(actual, point.Item2)));
        }
        return rows;
    }

    private static void AddRegionSelfCheck(ICollection<ReferenceSelfCheckRow> rows, string id, string region, double temperatureKelvins,
        double pressureMegapascals, double expectedSpecificVolume, double expectedInternalEnergy, Func<double, double, IapwsReferenceState> evaluator)
    {
        var state = evaluator(temperatureKelvins, pressureMegapascals);
        rows.Add(new ReferenceSelfCheckRow(id, region, "specific-volume", expectedSpecificVolume, state.SpecificVolumeCubicMetresPerKilogram,
            RelativeError(state.SpecificVolumeCubicMetresPerKilogram, expectedSpecificVolume)));
        rows.Add(new ReferenceSelfCheckRow(id, region, "specific-internal-energy", expectedInternalEnergy, state.SpecificInternalEnergyJoulesPerKilogram,
            RelativeError(state.SpecificInternalEnergyJoulesPerKilogram, expectedInternalEnergy)));
    }

    private static IReadOnlyList<Vr2Evaluation> EvaluateVr2(SimplifiedWaterSteamThermodynamicModel model, IReadOnlyList<Vr2Row> rows)
    {
        var result = new List<Vr2Evaluation>(rows.Count);
        foreach (var row in rows)
        {
            var reconstructed = ReconstructVr2Reference(row);
            var inputError = row.InverseApplicable
                ? Math.Max(RelativeError(reconstructed.SpecificVolume, row.SpecificVolume), RelativeError(reconstructed.SpecificEnergy, row.SpecificEnergy))
                : double.NaN;
            var boundaryError = !row.InverseApplicable
                ? RelativeError(reconstructed.PressureMegapascals, row.ReferencePressureMegapascals)
                : double.NaN;
            if (!row.InverseApplicable)
            {
                result.Add(new Vr2Evaluation(row.PointId, row.SourceFamily, row.ReferenceRegion, row.ReferencePhase,
                    row.ReferenceTemperatureCelsius, row.ReferencePressureMegapascals, false, false, "BOUNDARY-ONLY", double.NaN, double.NaN,
                    true, double.NaN, double.NaN, inputError, boundaryError, true));
                continue;
            }
            var state = Resolve(model, row.PointId, row.SpecificVolume, row.SpecificEnergy);
            result.Add(new Vr2Evaluation(row.PointId, row.SourceFamily, row.ReferenceRegion, row.ReferencePhase,
                row.ReferenceTemperatureCelsius, row.ReferencePressureMegapascals, true, state.Resolved, state.Phase,
                state.TemperatureCelsius, state.PressureMegapascals, state.Resolved && PhaseMatches(row.ReferencePhase, state.Phase),
                state.Resolved ? RelativeError(state.PressureMegapascals, row.ReferencePressureMegapascals) : double.NaN,
                state.Resolved ? RelativeKelvinError(state.TemperatureCelsius, row.ReferenceTemperatureCelsius) : double.NaN,
                inputError, boundaryError, state.Resolved && state.IsFinite));
        }
        return result;
    }

    private static IReadOnlyList<ExactEvaluation> EvaluateExact(SimplifiedWaterSteamThermodynamicModel model, IReadOnlyList<ExactRow> rows)
        => rows.Select(row =>
        {
            var state = Resolve(model, $"{row.ProbeId}|{row.LogicalStep}|{row.NodeId}", row.SpecificVolume, row.SpecificEnergy);
            return new ExactEvaluation(row.ProbeId, row.LogicalStep, row.ElapsedSeconds, row.NodeId, row.ReferenceRegion, row.ReferencePhase,
                row.ReferenceTemperatureCelsius, row.ReferencePressureMegapascals, row.ReferenceQuality, state.Resolved, state.Phase,
                state.TemperatureCelsius, state.PressureMegapascals, state.Quality, state.Resolved && PhaseMatches(row.ReferencePhase, state.Phase),
                state.Resolved ? RelativeError(state.PressureMegapascals, row.ReferencePressureMegapascals) : double.NaN,
                state.Resolved ? RelativeKelvinError(state.TemperatureCelsius, row.ReferenceTemperatureCelsius) : double.NaN,
                state.Resolved && state.IsFinite);
        }).ToArray();

    private static IReadOnlyList<SeamEvaluation> EvaluateSeam(SimplifiedWaterSteamThermodynamicModel model, IReadOnlyList<SeamRow> rows)
        => rows.Select(row =>
        {
            var state = Resolve(model, $"seam-{row.BoundaryIndex}-{row.ProbeSide}", row.SpecificVolume, row.SpecificEnergy);
            return new SeamEvaluation(row.BoundaryIndex, row.BoundaryTemperatureCelsius, row.ProbeSide, row.ReferenceRegion, row.ReferencePhase,
                row.ReferenceTemperatureCelsius, row.ReferencePressureMegapascals, row.ReferenceQuality, state.Resolved, state.Phase,
                state.TemperatureCelsius, state.PressureMegapascals, state.Quality, state.Resolved && PhaseMatches(row.ReferencePhase, state.Phase),
                state.Resolved ? state.TemperatureCelsius - row.ReferenceTemperatureCelsius : double.NaN,
                state.Resolved ? state.PressureMegapascals - row.ReferencePressureMegapascals : double.NaN,
                state.Resolved && state.IsFinite);
        }).ToArray();

    private static ReferenceReconstruction ReconstructVr2Reference(Vr2Row row)
    {
        var temperatureKelvins = row.ReferenceTemperatureCelsius + 273.15d;
        if (!row.InverseApplicable)
            return new ReferenceReconstruction(double.NaN, double.NaN, IapwsIf97Reference.SaturationPressureMegapascals(temperatureKelvins));
        if (row.SourceFamily == "COMPRESSED-LIQUID")
        {
            var state = IapwsIf97Reference.Region1(temperatureKelvins, row.ReferencePressureMegapascals);
            return new ReferenceReconstruction(state.SpecificVolumeCubicMetresPerKilogram, state.SpecificInternalEnergyJoulesPerKilogram, state.PressureMegapascals);
        }
        if (row.SourceFamily == "SUPERHEATED-VAPOR")
        {
            var state = IapwsIf97Reference.Region2(temperatureKelvins, row.ReferencePressureMegapascals);
            return new ReferenceReconstruction(state.SpecificVolumeCubicMetresPerKilogram, state.SpecificInternalEnergyJoulesPerKilogram, state.PressureMegapascals);
        }
        var pressure = IapwsIf97Reference.SaturationPressureMegapascals(temperatureKelvins);
        var liquid = IapwsIf97Reference.Region1(temperatureKelvins, pressure);
        var vapor = IapwsIf97Reference.Region2(temperatureKelvins, pressure);
        var quality = row.ReferenceQuality;
        return new ReferenceReconstruction(
            liquid.SpecificVolumeCubicMetresPerKilogram + quality * (vapor.SpecificVolumeCubicMetresPerKilogram - liquid.SpecificVolumeCubicMetresPerKilogram),
            liquid.SpecificInternalEnergyJoulesPerKilogram + quality * (vapor.SpecificInternalEnergyJoulesPerKilogram - liquid.SpecificInternalEnergyJoulesPerKilogram),
            pressure);
    }

    private static ResolvedState Resolve(SimplifiedWaterSteamThermodynamicModel model, string id, double specificVolume, double specificEnergy)
    {
        try
        {
            var definition = new FluidNodeDefinition(id, Volume.FromCubicMetres(specificVolume));
            var inventory = new FluidNodeInventory(Mass.FromKilograms(1d), Energy.FromJoules(specificEnergy));
            var state = model.Resolve(definition, inventory,
                new FluidThermodynamicState(Pressure.StandardAtmosphere, Temperature.FromDegreesCelsius(20d)));
            return new ResolvedState(true, state.Phase.ToString(), state.Temperature.DegreesCelsius, state.Pressure.Megapascals,
                state.VaporQuality.HasValue ? state.VaporQuality.Value.Fraction : null, double.IsFinite(state.Temperature.Kelvins) && double.IsFinite(state.Pressure.Pascals));
        }
        catch (WaterSteamStateOutOfRangeException)
        {
            return new ResolvedState(false, FluidPhase.Unspecified.ToString(), double.NaN, double.NaN, null, false);
        }
    }

    private static SeamContinuity BuildSeamContinuity(IReadOnlyList<SeamEvaluation> rows)
    {
        var liquidPressure = new List<double>(); var liquidTemperature = new List<double>();
        var vaporPressure = new List<double>(); var vaporTemperature = new List<double>();
        foreach (var group in rows.GroupBy(static row => row.BoundaryIndex))
        {
            var r1 = group.Single(static row => row.ProbeSide == "R1-SIDE");
            var r4l = group.Single(static row => row.ProbeSide == "R4-LIQUID-SIDE");
            var r4v = group.Single(static row => row.ProbeSide == "R4-VAPOR-SIDE");
            var r2 = group.Single(static row => row.ProbeSide == "R2-SIDE");
            if (r1.Resolved && r4l.Resolved)
            {
                liquidPressure.Add(Math.Abs(r1.ProductionPressureMegapascals - r4l.ProductionPressureMegapascals));
                liquidTemperature.Add(Math.Abs(r1.ProductionTemperatureCelsius - r4l.ProductionTemperatureCelsius));
            }
            if (r4v.Resolved && r2.Resolved)
            {
                vaporPressure.Add(Math.Abs(r4v.ProductionPressureMegapascals - r2.ProductionPressureMegapascals));
                vaporTemperature.Add(Math.Abs(r4v.ProductionTemperatureCelsius - r2.ProductionTemperatureCelsius));
            }
        }
        return new SeamContinuity(MaxFinite(liquidPressure), MaxFinite(liquidTemperature), MaxFinite(vaporPressure), MaxFinite(vaporTemperature));
    }

    private static int CountRepeatMismatches(SimplifiedWaterSteamThermodynamicModel first, SimplifiedWaterSteamThermodynamicModel second,
        IReadOnlyList<Vr2Row> vr2, IReadOnlyList<ExactRow> exact, IReadOnlyList<SeamRow> seam)
    {
        var mismatches = 0;
        foreach (var row in vr2.Where(static row => row.InverseApplicable))
            if (!Signature(Resolve(first,row.PointId,row.SpecificVolume,row.SpecificEnergy)).Equals(Signature(Resolve(second,row.PointId,row.SpecificVolume,row.SpecificEnergy)))) mismatches++;
        foreach (var row in exact)
            if (!Signature(Resolve(first,row.NodeId,row.SpecificVolume,row.SpecificEnergy)).Equals(Signature(Resolve(second,row.NodeId,row.SpecificVolume,row.SpecificEnergy)))) mismatches++;
        foreach (var row in seam)
            if (!Signature(Resolve(first,row.ProbeSide,row.SpecificVolume,row.SpecificEnergy)).Equals(Signature(Resolve(second,row.ProbeSide,row.SpecificVolume,row.SpecificEnergy)))) mismatches++;
        return mismatches;
    }

    private static StateSignature Signature(ResolvedState state) => state.Resolved
        ? new StateSignature(true, state.Phase, BitConverter.DoubleToInt64Bits(Temperature.FromDegreesCelsius(state.TemperatureCelsius).Kelvins),
            BitConverter.DoubleToInt64Bits(Pressure.FromMegapascals(state.PressureMegapascals).Pascals), state.Quality.HasValue,
            state.Quality.HasValue ? BitConverter.DoubleToInt64Bits(state.Quality.Value) : 0L)
        : new StateSignature(false, FluidPhase.Unspecified.ToString(), 0L, 0L, false, 0L);

    private static List<Vr2Row> LoadVr2Rows(string path)
    {
        var rows = new List<Vr2Row>();
        foreach (var line in File.ReadAllLines(path, Encoding.UTF8).Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line)) continue; var p=line.Split(',');
            rows.Add(new Vr2Row(p[0],p[1],p[2],p[3],D(p[4]),D(p[5]),DN(p[6]),DN(p[7]),DN(p[8]),!string.IsNullOrWhiteSpace(p[6])));
        }
        return rows;
    }
    private static List<ExactRow> LoadExactRows(string path)
    {
        var rows = new List<ExactRow>();
        foreach (var line in File.ReadAllLines(path, Encoding.UTF8).Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line)) continue; var p=line.Split(',');
            rows.Add(new ExactRow(p[0],long.Parse(p[1],CultureInfo.InvariantCulture),D(p[2]),p[3],1d/D(p[6]),D(p[7]),p[11],p[12],D(p[13]),D(p[14]),DN(p[15])));
        }
        return rows;
    }
    private static List<SeamRow> LoadSeamRows(string path)
    {
        var rows = new List<SeamRow>();
        foreach (var line in File.ReadAllLines(path, Encoding.UTF8).Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line)) continue; var p=line.Split(',');
            rows.Add(new SeamRow(int.Parse(p[0],CultureInfo.InvariantCulture),D(p[1]),p[2],p[3],p[4],D(p[1]),D(p[5]),D(p[6]),D(p[7]),DN(p[8])));
        }
        return rows;
    }

    private static void WriteReferenceSelfCheck(string directory, IEnumerable<ReferenceSelfCheckRow> rows)
    {
        var lines=new List<string>{"id,region,property,official_value,helper_value,relative_error,passes_1e-8"};
        lines.AddRange(rows.Select(row => string.Join(",", new[] { row.Id,row.Region,row.Property,F(row.OfficialValue),F(row.HelperValue),F(row.RelativeError),B(row.RelativeError<=ReferenceSelfCheckMaximumRelativeError) })));
        File.WriteAllLines(Path.Combine(directory,"02-reference-selfcheck.csv"),lines,Utf8WithoutBom);
    }
    private static void WriteVr2Qualification(string directory, IEnumerable<Vr2Evaluation> rows)
    {
        var lines=new List<string>{"point_id,source_family,reference_region,reference_phase,reference_temperature_c,reference_pressure_mpa,inverse_applicable,production_resolved,production_phase,production_temperature_c,production_pressure_mpa,phase_matches,pressure_relative_error,temperature_relative_kelvin_error,reference_corpus_input_max_relative_error,boundary_reference_relative_error,is_finite"};
        lines.AddRange(rows.Select(r=>string.Join(",", new[] { r.PointId,r.SourceFamily,r.ReferenceRegion,r.ReferencePhase,F(r.ReferenceTemperatureCelsius),F(r.ReferencePressureMegapascals),B(r.InverseApplicable),B(r.Resolved),r.ProductionPhase,F(r.ProductionTemperatureCelsius),F(r.ProductionPressureMegapascals),B(r.PhaseMatches),F(r.PressureRelativeError),F(r.TemperatureRelativeKelvinError),F(r.ReferenceCorpusInputMaximumRelativeError),F(r.BoundaryReferenceRelativeError),B(r.IsFinite) })));
        File.WriteAllLines(Path.Combine(directory,"03-vr2-reference-point-qualification.csv"),lines,Utf8WithoutBom);
    }
    private static void WriteExactQualification(string directory, IEnumerable<ExactEvaluation> rows)
    {
        var lines=new List<string>{"probe_id,logical_step,elapsed_s,node_id,reference_region,reference_phase,reference_temperature_c,reference_pressure_mpa,reference_quality,production_resolved,production_phase,production_temperature_c,production_pressure_mpa,production_quality,phase_matches,pressure_relative_error,temperature_relative_kelvin_error,is_finite"};
        lines.AddRange(rows.Select(r=>string.Join(",", new[] { r.ProbeId,r.LogicalStep.ToString(CultureInfo.InvariantCulture),F(r.ElapsedSeconds),r.NodeId,r.ReferenceRegion,r.ReferencePhase,F(r.ReferenceTemperatureCelsius),F(r.ReferencePressureMegapascals),F(r.ReferenceQuality),B(r.Resolved),r.ProductionPhase,F(r.ProductionTemperatureCelsius),F(r.ProductionPressureMegapascals),F(r.ProductionQuality),B(r.PhaseMatches),F(r.PressureRelativeError),F(r.TemperatureRelativeKelvinError),B(r.IsFinite) })));
        File.WriteAllLines(Path.Combine(directory,"04-exact-v9-topology-qualification.csv"),lines,Utf8WithoutBom);
    }
    private static void WriteSeamQualification(string directory, IReadOnlyList<SeamEvaluation> rows, SeamContinuity continuity)
    {
        var lines=new List<string>{"boundary_index,boundary_temperature_c,probe_side,reference_region,reference_phase,reference_temperature_c,reference_pressure_mpa,reference_quality,production_resolved,production_phase,production_temperature_c,production_pressure_mpa,production_quality,phase_matches,production_minus_reference_temperature_c,production_minus_reference_pressure_mpa,is_finite,boundary_max_liquid_pressure_jump_mpa,boundary_max_liquid_temperature_jump_c,boundary_max_vapor_pressure_jump_mpa,boundary_max_vapor_temperature_jump_c"};
        lines.AddRange(rows.Select(r=>string.Join(",", new[] { r.BoundaryIndex.ToString(CultureInfo.InvariantCulture),F(r.BoundaryTemperatureCelsius),r.ProbeSide,r.ReferenceRegion,r.ReferencePhase,F(r.ReferenceTemperatureCelsius),F(r.ReferencePressureMegapascals),F(r.ReferenceQuality),B(r.Resolved),r.ProductionPhase,F(r.ProductionTemperatureCelsius),F(r.ProductionPressureMegapascals),F(r.ProductionQuality),B(r.PhaseMatches),F(r.ProductionMinusReferenceTemperatureCelsius),F(r.ProductionMinusReferencePressureMegapascals),B(r.IsFinite),F(continuity.LiquidPressureJumpMegapascals),F(continuity.LiquidTemperatureJumpCelsius),F(continuity.VaporPressureJumpMegapascals),F(continuity.VaporTemperatureJumpCelsius) })));
        File.WriteAllLines(Path.Combine(directory,"05-seam-topology-continuity.csv"),lines,Utf8WithoutBom);
    }
    private static void WriteTopologySummary(string directory,bool pass,double selfCheck,double inputError,double boundaryError,int vr2Unresolved,int vr2Phase,int exactUnresolved,int exactPhase,double exactAgreement,int seamUnresolved,int seamPhase,double coreP,double coreT,double hotP,SeamContinuity c,int repeat)
    {
        File.WriteAllLines(Path.Combine(directory,"06-topology-qualification-summary.txt"),new[]{
            "status="+(pass?"PASS-R2-TOPOLOGY-QUALIFICATION":"R2-REFERENCE-OR-TOPOLOGY-BLOCKING"),
            "reference-selfcheck-max-relative-error="+F(selfCheck),"reference-selfcheck-ceiling="+F(ReferenceSelfCheckMaximumRelativeError),
            "reference-corpus-input-max-relative-error="+F(inputError),"boundary-only-reference-relative-error="+F(boundaryError),
            "vr2-rows=40","vr2-inverse-rows=39","vr2-unresolved="+I(vr2Unresolved),"vr2-phase-mismatch="+I(vr2Phase),
            "exact-v9-rows=360","exact-v9-unresolved="+I(exactUnresolved),"exact-v9-phase-mismatch="+I(exactPhase),"exact-v9-phase-agreement-percent="+F(exactAgreement),
            "seam-rows=1280","seam-boundaries=320","seam-unresolved="+I(seamUnresolved),"seam-phase-mismatch="+I(seamPhase),
            "max-core-pressure-relative-error="+F(coreP),"max-core-temperature-relative-kelvin-error="+F(coreT),"max-hot-core-pressure-relative-error="+F(hotP),
            "vr2-blocking-ceiling="+F(ExistingVr2BlockingCeilingFraction),"planning1-pressure-target="+F(PlanningTargetFraction),
            "max-liquid-seam-pressure-jump-mpa="+F(c.LiquidPressureJumpMegapascals),"max-liquid-seam-temperature-jump-c="+F(c.LiquidTemperatureJumpCelsius),
            "max-vapor-seam-pressure-jump-mpa="+F(c.VaporPressureJumpMegapascals),"max-vapor-seam-temperature-jump-c="+F(c.VaporTemperatureJumpCelsius),
            "liquid-seam-pressure-ceiling-mpa="+F(MaximumLiquidSeamPressureJumpMegapascals),"liquid-seam-temperature-ceiling-c="+F(MaximumLiquidSeamTemperatureJumpCelsius),
            "vapor-seam-pressure-ceiling-mpa="+F(MaximumVaporSeamPressureJumpMegapascals),"vapor-seam-temperature-ceiling-c="+F(MaximumVaporSeamTemperatureJumpCelsius),
            "deterministic-repeat-mismatches="+I(repeat),"exact-v9-composition-executed=False","hydraulic-long-materiality-executed=False"
        },Utf8WithoutBom);
    }
    private static void WriteDeterministicRepeat(string directory,int mismatches)
    {
        File.WriteAllLines(Path.Combine(directory,"07-deterministic-repeat.txt"),new[]{"status="+(mismatches==0?"PASS-DETERMINISTIC-REPEAT":"FAIL-DETERMINISTIC-REPEAT"),"state-comparisons=1679","repeat-mismatches="+I(mismatches),"production-mode=ReferenceConsistentTabulatedInverseDomain","production-mode-value=2"},Utf8WithoutBom);
    }

    private static bool PhaseMatches(string reference,string production)=>string.Equals(reference,production,StringComparison.Ordinal);
    private static double RelativeError(double actual,double expected)=>Math.Abs(actual-expected)/Math.Max(Math.Abs(expected),1e-12d);
    private static double RelativeKelvinError(double actualC,double expectedC)=>Math.Abs(actualC-expectedC)/Math.Max(Math.Abs(expectedC+273.15d),1e-12d);
    private static double MaxFinite(IEnumerable<double> values){var a=values.Where(double.IsFinite).ToArray();return a.Length==0?double.NaN:a.Max();}
    private static double D(string s)=>double.Parse(s,CultureInfo.InvariantCulture);
    private static double DN(string s)=>string.IsNullOrWhiteSpace(s)?double.NaN:D(s);
    private static string F(double v)=>double.IsNaN(v)?string.Empty:v.ToString("R",CultureInfo.InvariantCulture);
    private static string F(double? v)=>v.HasValue?v.Value.ToString("R",CultureInfo.InvariantCulture):string.Empty;
    private static string B(bool v)=>v?"true":"false";
    private static string I(int v)=>v.ToString(CultureInfo.InvariantCulture);
    private static string FindRepositoryRoot(){var current=new DirectoryInfo(AppContext.BaseDirectory);while(current is not null){if(File.Exists(Path.Combine(current.FullName,"NuclearReactorSimulator.sln")))return current.FullName;current=current.Parent;}throw new InvalidOperationException("Could not locate NuclearReactorSimulator.sln.");}

    private sealed record ReferenceSelfCheckRow(string Id,string Region,string Property,double OfficialValue,double HelperValue,double RelativeError);
    private sealed record ReferenceReconstruction(double SpecificVolume,double SpecificEnergy,double PressureMegapascals);
    private sealed record Vr2Row(string PointId,string SourceFamily,string ReferenceRegion,string ReferencePhase,double ReferenceTemperatureCelsius,double ReferencePressureMegapascals,double SpecificVolume,double SpecificEnergy,double ReferenceQuality,bool InverseApplicable);
    private sealed record ExactRow(string ProbeId,long LogicalStep,double ElapsedSeconds,string NodeId,double SpecificVolume,double SpecificEnergy,string ReferenceRegion,string ReferencePhase,double ReferenceTemperatureCelsius,double ReferencePressureMegapascals,double ReferenceQuality);
    private sealed record SeamRow(int BoundaryIndex,double BoundaryTemperatureCelsius,string ProbeSide,string ReferenceRegion,string ReferencePhase,double ReferenceTemperatureCelsius,double ReferencePressureMegapascals,double SpecificVolume,double SpecificEnergy,double ReferenceQuality);
    private sealed record ResolvedState(bool Resolved,string Phase,double TemperatureCelsius,double PressureMegapascals,double? Quality,bool IsFinite);
    private sealed record Vr2Evaluation(string PointId,string SourceFamily,string ReferenceRegion,string ReferencePhase,double ReferenceTemperatureCelsius,double ReferencePressureMegapascals,bool InverseApplicable,bool Resolved,string ProductionPhase,double ProductionTemperatureCelsius,double ProductionPressureMegapascals,bool PhaseMatches,double PressureRelativeError,double TemperatureRelativeKelvinError,double ReferenceCorpusInputMaximumRelativeError,double BoundaryReferenceRelativeError,bool IsFinite);
    private sealed record ExactEvaluation(string ProbeId,long LogicalStep,double ElapsedSeconds,string NodeId,string ReferenceRegion,string ReferencePhase,double ReferenceTemperatureCelsius,double ReferencePressureMegapascals,double ReferenceQuality,bool Resolved,string ProductionPhase,double ProductionTemperatureCelsius,double ProductionPressureMegapascals,double? ProductionQuality,bool PhaseMatches,double PressureRelativeError,double TemperatureRelativeKelvinError,bool IsFinite);
    private sealed record SeamEvaluation(int BoundaryIndex,double BoundaryTemperatureCelsius,string ProbeSide,string ReferenceRegion,string ReferencePhase,double ReferenceTemperatureCelsius,double ReferencePressureMegapascals,double ReferenceQuality,bool Resolved,string ProductionPhase,double ProductionTemperatureCelsius,double ProductionPressureMegapascals,double? ProductionQuality,bool PhaseMatches,double ProductionMinusReferenceTemperatureCelsius,double ProductionMinusReferencePressureMegapascals,bool IsFinite);
    private sealed record SeamContinuity(double LiquidPressureJumpMegapascals,double LiquidTemperatureJumpCelsius,double VaporPressureJumpMegapascals,double VaporTemperatureJumpCelsius);
    private readonly record struct StateSignature(bool Resolved,string Phase,long TemperatureBits,long PressureBits,bool HasQuality,long QualityBits);
}
