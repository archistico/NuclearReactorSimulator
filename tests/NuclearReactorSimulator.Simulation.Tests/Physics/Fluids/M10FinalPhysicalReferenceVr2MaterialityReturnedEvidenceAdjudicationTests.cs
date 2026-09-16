using System.Globalization;
using System.Text;
using NuclearReactorSimulator.Domain.Plant;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Physics.Fluids;

/// <summary>
/// Adjudicates the complete returned Attempt-5 VR2 materiality artifact set without replaying the 3600 s path.
/// The original long diagnostic and its 1e-9 post-evidence assertion remain frozen as historical evidence.
/// This gate asks whether that assertion was compatible with the authoritative exact-v9 H.22 fixed-point
/// flow residual contract and whether the already-returned materiality classification survives the canonical
/// numerical residual bound conservatively.
/// </summary>
public sealed class M10FinalPhysicalReferenceVr2MaterialityReturnedEvidenceAdjudicationTests
{
    private const string OptInEnvironmentVariable = "NRS_M10_FINAL_PHYSICAL_REFERENCE_VR2_MATERIALITY_ADJUDICATION";
    private const string FrozenArtifactDirectoryRelative = "eng/frozen-evidence/ordinary/M10FinalPhysicalReferenceVR2MaterialityDiagnostic1_Attempt5_Artifacts";
    private const string OutputDirectoryRelative = "artifacts/m10-final-physical-reference-vr2-materiality-diagnostic1-adjudication";
    private const double FrozenOriginalReproductionToleranceKilogramsPerSecond = 1e-9d;
    private const double ConfirmedImpactRatio = 1d;
    private const double NotExcludedImpactRatio = 0.1d;
    private const int RequiredPersistentWindows = 3;
    private static readonly UTF8Encoding Utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);

    [Fact(Explicit = true)]
    [Trait("Category", "M10FinalPhysicalReferenceVR2MaterialityDiagnostic1ReturnedEvidenceAdjudication")]
    public void ReturnedAttempt5_Evidence_IsAdjudicatedAgainstAuthoritativeExactV9FixedPointResidualContract()
    {
        RequireOptIn();
        var repositoryRoot = FindRepositoryRoot();
        var frozenDirectory = Path.Combine(repositoryRoot, FrozenArtifactDirectoryRelative.Replace('/', Path.DirectorySeparatorChar));
        var outputDirectory = ResetOutputDirectory(repositoryRoot);

        foreach (var fileName in new[]
        {
            "01-contract-and-provenance.txt",
            "02-p1b-checkpoint-reproduction.csv",
            "03-node-if97-inverse-map.csv",
            "04-hydraulic-path-counterfactual.csv",
            "05-late-window-materiality.csv",
            "06-materiality-summary.txt",
            "07-sentinels.txt",
        })
        {
            Assert.True(File.Exists(Path.Combine(frozenDirectory, fileName)), $"Returned Attempt-5 evidence file is missing: {fileName}.");
        }

        var exactV9FixedPointTolerance = HydraulicNumericalCouplingDefinition
            .H22FourNodeBranchContinuityCorrectedCommitOptIn
            .CorrectorAbsoluteFlowToleranceKilogramsPerSecond;
        Assert.Equal(1e-2d, exactV9FixedPointTolerance);

        var contract = ReadKeyValueFile(Path.Combine(frozenDirectory, "01-contract-and-provenance.txt"));
        Assert.Equal("NONE", contract["runtime-substitution"]);
        Assert.Equal("NONE", contract["production-src-change"]);
        Assert.Equal("NONE", contract["thermodynamic-tolerance-change"]);
        Assert.Equal("NONE", contract["exact-v9-change"]);
        Assert.True(ParseDouble(contract["inverse-reference-selfcheck-max-relative-error"]) <= ParseDouble(contract["inverse-reference-selfcheck-ceiling"]));

        var checkpoints = ReadCsv(Path.Combine(frozenDirectory, "02-p1b-checkpoint-reproduction.csv"));
        Assert.Equal(3, checkpoints.Count);
        Assert.All(checkpoints, row => Assert.Equal("true", row["matches"]));
        Assert.Equal(new[] { "900", "1800", "3600" }, checkpoints.Select(row => row["hold_seconds"]).ToArray());

        var nodeRows = ReadCsv(Path.Combine(frozenDirectory, "03-node-if97-inverse-map.csv"));
        Assert.Equal(360, nodeRows.Count);
        Assert.All(nodeRows, row => Assert.Equal("true", row["reference_resolved"]));

        var pressureProductionSubcooledReferenceMixture = nodeRows.Count(row
            => row["node_id"] == "pressure"
            && row["production_phase"] == "SubcooledLiquid"
            && row["reference_region"] == "REGION-4-MIXTURE"
            && row["reference_phase"] == "SaturatedMixture");
        var suctionProductionSubcooledReferenceMixture = nodeRows.Count(row
            => row["node_id"] == "suction"
            && row["production_phase"] == "SubcooledLiquid"
            && row["reference_region"] == "REGION-4-MIXTURE"
            && row["reference_phase"] == "SaturatedMixture");
        Assert.Equal(72, pressureProductionSubcooledReferenceMixture);
        Assert.Equal(55, suctionProductionSubcooledReferenceMixture);

        var pathRows = ReadCsv(Path.Combine(frozenDirectory, "04-hydraulic-path-counterfactual.csv"));
        Assert.Equal(288, pathRows.Count);
        Assert.All(pathRows, row => Assert.Equal("true", row["reference_resolved"]));
        var maximumFlowReproductionError = pathRows.Max(row => Math.Abs(
            ParseDouble(row["canonical_flow_kg_s"])
            - ParseDouble(row["production_formula_flow_kg_s"])));
        Assert.True(
            maximumFlowReproductionError > FrozenOriginalReproductionToleranceKilogramsPerSecond,
            "Returned Attempt-5 evidence must preserve the historical 1e-9 post-evidence assertion failure.");
        Assert.True(
            maximumFlowReproductionError <= exactV9FixedPointTolerance,
            $"Returned Attempt-5 fixed-point reproduction error {maximumFlowReproductionError:R} kg/s exceeds the authoritative exact-v9 H.22 flow residual tolerance {exactV9FixedPointTolerance:R} kg/s.");

        var windows = ReadCsv(Path.Combine(frozenDirectory, "05-late-window-materiality.csv"));
        Assert.Equal(16, windows.Count);
        Assert.Equal(4, windows.Count(row => row["path_id"] == "CHANNEL" && row["band"] == "CONFIRMED"));
        Assert.Equal(4, windows.Count(row => row["path_id"] == "FEEDWATER-PUMP" && row["band"] == "CONFIRMED"));
        Assert.Equal(4, windows.Count(row => row["path_id"] == "MCP" && row["band"] == "NOT-EXCLUDED"));
        Assert.Equal(4, windows.Count(row => row["path_id"] == "RETURN" && row["band"] == "NOT-EXCLUDED"));
        Assert.Equal(4, windows.Count(row => row["path_id"] == "CHANNEL" && row["driving_pressure_sign_change"] == "true"));

        var robustConfirmedWindows = 0;
        var robustNotExcludedWindows = 0;
        foreach (var row in windows)
        {
            var shift = ParseDouble(row["mean_abs_counterfactual_shift_kg_s"]);
            var scale = ParseDouble(row["phenomenon_scale_kg_s"]);
            var conservativeShift = Math.Max(0d, shift - exactV9FixedPointTolerance);
            var conservativeImpactRatio = conservativeShift / scale;
            var signChanged = row["driving_pressure_sign_change"] == "true";
            if (signChanged || conservativeImpactRatio >= ConfirmedImpactRatio)
            {
                robustConfirmedWindows++;
            }
            else if (conservativeImpactRatio >= NotExcludedImpactRatio)
            {
                robustNotExcludedWindows++;
            }
        }

        Assert.True(robustConfirmedWindows >= RequiredPersistentWindows);
        Assert.True(robustNotExcludedWindows >= RequiredPersistentWindows);
        Assert.True(
            windows.Where(row => row["path_id"] == "CHANNEL").Count(row
                => row["driving_pressure_sign_change"] == "true"
                    || ConservativeImpactRatio(row, exactV9FixedPointTolerance) >= ConfirmedImpactRatio)
            >= RequiredPersistentWindows);
        Assert.True(
            windows.Where(row => row["path_id"] == "FEEDWATER-PUMP").Count(row
                => ConservativeImpactRatio(row, exactV9FixedPointTolerance) >= ConfirmedImpactRatio)
            >= RequiredPersistentWindows);
        Assert.True(
            windows.Where(row => row["path_id"] == "RETURN").All(row
                => ConservativeImpactRatio(row, exactV9FixedPointTolerance) >= NotExcludedImpactRatio));

        var summary = ReadKeyValueFile(Path.Combine(frozenDirectory, "06-materiality-summary.txt"));
        Assert.Equal("True", summary["execution-pass"]);
        Assert.Equal("HYDRAULIC-MATERIALITY-CONFIRMED", summary["classification"]);
        Assert.Equal("3/3", summary["p1b-checkpoints-matched"]);
        Assert.Equal("360", summary["node-row-count"]);
        Assert.Equal("0", summary["unresolved-node-row-count"]);
        Assert.Equal("288", summary["path-row-count"]);
        Assert.Equal("0", summary["unresolved-path-row-count"]);
        Assert.Equal("8", summary["confirmed-window-count"]);
        Assert.Equal("8", summary["not-excluded-window-count"]);
        Assert.Equal("True", summary["deterministic-analysis-repeat"]);
        Assert.InRange(
            Math.Abs(ParseDouble(summary["max-hydraulic-flow-reproduction-error-kg-s"]) - maximumFlowReproductionError),
            0d,
            1e-12d);

        var sentinels = ReadKeyValueFile(Path.Combine(frozenDirectory, "07-sentinels.txt"));
        foreach (var key in new[]
        {
            "background-trip-steps",
            "background-nonconverged-steps",
            "background-nonfinite-numerical-steps",
            "background-rollback-steps",
            "load-trip-steps",
            "load-nonconverged-steps",
            "load-nonfinite-numerical-steps",
            "load-rollback-steps",
        })
        {
            Assert.Equal("0", sentinels[key]);
        }

        File.WriteAllLines(
            Path.Combine(outputDirectory, "01-returned-evidence-adjudication.txt"),
            new[]
            {
                "gate=VR2-MATERIALITY-DIAGNOSTIC1-RETURNED-EVIDENCE-ADJUDICATION",
                "source-attempt=5",
                "source-attempt-status=EXECUTED-RED-POST-EVIDENCE",
                "returned-artifact-set=COMPLETE-7-OF-7",
                "original-long-diagnostic-rerun=False",
                $"frozen-original-reproduction-tolerance-kg-s={F(FrozenOriginalReproductionToleranceKilogramsPerSecond)}",
                $"authoritative-exact-v9-h22-fixed-point-flow-tolerance-kg-s={F(exactV9FixedPointTolerance)}",
                $"returned-max-flow-reproduction-error-kg-s={F(maximumFlowReproductionError)}",
                "returned-max-flow-reproduction-error-within-authoritative-fixed-point-tolerance=True",
                "p1b-checkpoints-matched=3/3",
                "node-row-count=360",
                "unresolved-node-row-count=0",
                "path-row-count=288",
                "unresolved-path-row-count=0",
                $"pressure-production-subcooled-reference-region4-mixture-count={pressureProductionSubcooledReferenceMixture}",
                $"suction-production-subcooled-reference-region4-mixture-count={suctionProductionSubcooledReferenceMixture}",
                $"robust-confirmed-window-count-after-fixed-point-bound={robustConfirmedWindows}",
                $"robust-not-excluded-window-count-after-fixed-point-bound={robustNotExcludedWindows}",
                "engineering-classification=HYDRAULIC-MATERIALITY-CONFIRMED",
                "interpretation=The Attempt-5 1e-9 failure was a diagnostic self-consistency contract defect: committed H.22 flows are fixed-point iterates whose authoritative absolute flow residual ceiling is 1e-2 kg/s.",
                "interpretation-materiality=The IF97 pressure-only counterfactual remains materially large after conservatively subtracting the full authoritative fixed-point residual bound from every late-window shift.",
                "production-repair-authorized=False",
                "thermodynamic-tolerance-change-authorized=False",
                "exact-v9-change-authorized=False",
                "vr3-authorized=False",
                "p3-r1-authorized=False",
                "second-replacement-long-authorized=False",
                "next-action=Return this adjudication artifact for a separate engineering repair-planning decision before any production change or VR3.",
            },
            Utf8WithoutBom);
    }

    private static double ConservativeImpactRatio(IReadOnlyDictionary<string, string> row, double flowResidualBound)
    {
        var shift = ParseDouble(row["mean_abs_counterfactual_shift_kg_s"]);
        var scale = ParseDouble(row["phenomenon_scale_kg_s"]);
        return Math.Max(0d, shift - flowResidualBound) / scale;
    }

    private static List<Dictionary<string, string>> ReadCsv(string path)
    {
        var lines = File.ReadAllLines(path, Encoding.UTF8);
        Assert.True(lines.Length >= 2, $"CSV evidence is empty: {path}.");
        var headers = lines[0].Split(',');
        var rows = new List<Dictionary<string, string>>(lines.Length - 1);
        foreach (var line in lines.Skip(1).Where(static line => !string.IsNullOrWhiteSpace(line)))
        {
            var values = line.Split(',');
            Assert.Equal(headers.Length, values.Length);
            var row = new Dictionary<string, string>(StringComparer.Ordinal);
            for (var index = 0; index < headers.Length; index++)
            {
                row.Add(headers[index], values[index]);
            }
            rows.Add(row);
        }
        return rows;
    }

    private static Dictionary<string, string> ReadKeyValueFile(string path)
    {
        var output = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var line in File.ReadAllLines(path, Encoding.UTF8))
        {
            var separator = line.IndexOf('=');
            if (separator <= 0)
            {
                continue;
            }
            output.Add(line[..separator], line[(separator + 1)..]);
        }
        return output;
    }

    private static double ParseDouble(string value)
        => double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);

    private static string F(double value)
        => value.ToString("R", CultureInfo.InvariantCulture);

    private static void RequireOptIn()
    {
        Assert.Equal("1", Environment.GetEnvironmentVariable(OptInEnvironmentVariable));
    }

    private static string ResetOutputDirectory(string repositoryRoot)
    {
        var path = Path.Combine(repositoryRoot, OutputDirectoryRelative.Replace('/', Path.DirectorySeparatorChar));
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
            if (Directory.Exists(Path.Combine(current.FullName, "src"))
                && Directory.Exists(Path.Combine(current.FullName, "tests"))
                && Directory.Exists(Path.Combine(current.FullName, "eng")))
            {
                return current.FullName;
            }
            current = current.Parent;
        }
        throw new InvalidOperationException("Could not locate repository root for VR2 materiality returned-evidence adjudication.");
    }
}
