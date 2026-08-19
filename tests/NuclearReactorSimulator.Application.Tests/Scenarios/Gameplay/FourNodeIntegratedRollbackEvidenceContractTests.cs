using System.Security.Cryptography;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios.Training;
using NuclearReactorSimulator.Domain.Plant;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay;

public sealed class FourNodeIntegratedRollbackEvidenceContractTests
{
    private static readonly IReadOnlyDictionary<string, string> FrozenH25Fingerprints =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["H25_ValidatedProtectionTransientMatrixSummary.txt"] = "09112868F26AAD1F007820F27BBED6BF48462FC5E8A61C47E0D786D079090E85",
            ["H25_ValidatedProtectionTransientMatrixTelemetry.csv"] = "337A559800853724003DC23345A68A2C2744EEC132C257E7386690DBF3279CFD",
            ["H25_ValidatedProtectionTransientMatrixMetrics.csv"] = "037517B1258B2C238F5606F5E22AA05BFB206107872F7978F511351FFFD53317",
        };

    [Fact]
    public void FrozenH25Evidence_RetainsValidatedProtectionTransientMatrixAndDefaultExplicitIsolation()
    {
        var evidenceDirectory = EvidenceDirectory();
        foreach (var expected in FrozenH25Fingerprints)
        {
            var path = Path.Combine(evidenceDirectory, expected.Key);
            Assert.True(File.Exists(path), $"Frozen H.25 evidence file is missing: {expected.Key}");
            Assert.Equal(expected.Value, CanonicalSha256(path));
        }

        var summary = File.ReadAllText(Path.Combine(evidenceDirectory, "H25_ValidatedProtectionTransientMatrixSummary.txt"));
        Assert.Contains("matrix-scenarios=5", summary, StringComparison.Ordinal);
        Assert.Contains("runtime-steps=837", summary, StringComparison.Ordinal);
        Assert.Contains("corrected-candidates-committed=178", summary, StringComparison.Ordinal);
        Assert.Contains("H20-rollbacks=0", summary, StringComparison.Ordinal);
        Assert.Contains("fallback-commit-violations=0", summary, StringComparison.Ordinal);
        Assert.Contains("unsafe-corrected-commits=0", summary, StringComparison.Ordinal);
        Assert.Contains("four-node-committed-protection-operational-transient-matrix-passes=True", summary, StringComparison.Ordinal);
        Assert.Contains("h25-audit-passes=True", summary, StringComparison.Ordinal);

        var telemetryPath = Path.Combine(evidenceDirectory, "H25_ValidatedProtectionTransientMatrixTelemetry.csv");
        Assert.Equal(837, File.ReadLines(telemetryPath).Skip(1).Count());

        var defaultEngine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            new DesktopSustainedGenerationInitialConditionFactory().CreateRuntimeEngine());
        Assert.Equal(
            HydraulicNumericalCouplingMode.ExplicitCommittedState,
            defaultEngine.CurrentState.PlantDefinition.PlantDefinition.HydraulicNumericalCoupling.Mode);
    }

    private static string EvidenceDirectory()
        => Path.Combine(FindRepositoryRoot(), "tests", "NuclearReactorSimulator.Application.Tests", "Scenarios", "Gameplay", "Evidence");

    private static string CanonicalSha256(string path)
    {
        var bytes = File.ReadAllBytes(path);
        var canonical = System.Text.Encoding.UTF8.GetBytes(
            System.Text.Encoding.UTF8.GetString(bytes).Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n'));
        return Convert.ToHexString(SHA256.HashData(canonical));
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "NuclearReactorSimulator.sln")))
            {
                return current.FullName;
            }
            current = current.Parent;
        }
        throw new DirectoryNotFoundException("Could not locate NuclearReactorSimulator.sln from the H.26 evidence test output directory.");
    }
}
