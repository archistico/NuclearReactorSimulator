using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay;

public sealed class M10FinalVr2R3HConservedInventoryFeasibility1Tests
{
    [Fact]
    public void Preexecution_BaselineReproducesFactoryAndFrozenResiduals()
    {
        var harness = new R3HFeasibilityHarness();
        var hash = harness.BaselineIdentity();
        var baseline = harness.Evaluate(new double[24]);
        var second = new R3HFeasibilityHarness().Evaluate(new double[24]);
        Assert.Equal(JsonSerializer.Serialize(baseline.Observations), JsonSerializer.Serialize(second.Observations));
        // Recompute expected storage residuals from the immutable diagnostic, not from this harness.
        var lines = File.ReadAllLines(Path.Combine(R3HFeasibilityHarness.Root, "eng", "frozen-evidence", "r3-dyn-eq-d1", "01-node-residuals.csv"));
        var header = lines[0].Split(',');
        foreach (var observation in baseline.Observations)
        {
            var rows = lines.Skip(1).Select(static line => line.Split(',')).Where(row => row[0] == "candidate-mode2" && row[1] == observation.Checkpoint).ToArray();
            Assert.Equal(12, rows.Length);
            foreach (var node in observation.Nodes)
            {
                var row = Assert.Single(rows, row => row[2] == node.Id);
                Assert.Equal(node.Mass, Parse(row[Array.IndexOf(header, "mass_kg")]));
                Assert.Equal(node.Energy, Parse(row[Array.IndexOf(header, "energy_j")]));
                Assert.Equal(node.Pressure, Parse(row[Array.IndexOf(header, "pressure_pa")]));
                Assert.Equal(node.Temperature, Parse(row[Array.IndexOf(header, "temperature_k")]));
                Assert.Equal(node.Phase, row[Array.IndexOf(header, "phase")]);
            }
        }
        R3HFeasibilityHarness.Write("02-baseline.json", new { status = "PASS-BASELINE-IDENTITY", hash, baseline.Raw, baseline.Observations });
    }

    [Fact]
    public void Search_ConvergesOnBoundedCoupledLinearResidualAndReproducesTrace()
    {
        static R3HFeasibilitySearch.Sample Objective(double[] x) => new([10000 * (x[0] + x[1] - 0.008), 10000 * (x[0] - x[1] - 0.002)], "VALID");
        var a = new R3HFeasibilitySearch(2);
        var b = new R3HFeasibilitySearch(2);
        var result = a.Run(Objective);
        Assert.True(result.Root);
        Assert.All(result.Coordinates, static x => Assert.InRange(x, -0.02, 0.02));
        Assert.All(result.Residuals!, static r => Assert.InRange(Math.Abs(r), 0, 1));
        var repeated = b.Run(Objective);
        Assert.Equal(JsonSerializer.Serialize(result), JsonSerializer.Serialize(repeated));
        Assert.Equal(JsonSerializer.Serialize(a.Trace), JsonSerializer.Serialize(b.Trace));
        var accepted = a.Trace.Where(static e => e.Accepted).ToArray();
        for (var i = 1; i < accepted.Length; i++) Assert.True(accepted[i].Score < accepted[i - 1].Score);
    }

    [Fact]
    public void Search_CountsInvalidProbesAndStopsWithoutInventingDerivative()
    {
        var search = new R3HFeasibilitySearch(1);
        var result = search.Run(x => x[0] > 0 ? R3HFeasibilitySearch.Sample.Invalid("OUT-OF-DOMAIN") : new([100d], "VALID"));
        Assert.False(result.Root);
        Assert.Equal("INVALID-DIFFERENCE-PROBE", result.Stop);
        Assert.Equal(3, result.Evaluations);
        Assert.Equal("OUT-OF-DOMAIN", search.Trace[1].Reason);
    }

    [Fact]
    public void Search_RecipeBudgetIncludesFailedAttempts()
    {
        var search = new R3HFeasibilitySearch(2, evaluationLimit: 3);
        var result = search.Run(x => new([10 + x.Sum()], "VALID"));
        Assert.False(result.Root);
        Assert.Equal("RECIPE-BUDGET", result.Stop);
        Assert.Equal(3, result.Evaluations);
    }

    [Fact]
    public void Search_RejectsOutOfBoundsTrialsWithoutClippingOrInvokingPhysics()
    {
        var search = new R3HFeasibilitySearch(1);
        var physicsCalls = 0;
        var result = search.Run(x => { physicsCalls++; Assert.InRange(x[0], -0.02, 0.02); return new([10000 * (x[0] - 1)], "VALID"); });
        Assert.False(result.Root);
        Assert.Contains(search.Trace, static e => e.Reason == "OUTSIDE-FROZEN-BOUNDS");
        Assert.True(physicsCalls < result.Evaluations);
    }

    [Fact]
    public void Search_NormalMatrixPivotGuardRejectsUnresolvedConditioning()
    {
        Assert.Null(R3HFeasibilitySearch.Solve(new double[,] { { 1e12, 0 }, { 0, 1e-12 } }, [10, 10], 1e-3));
    }

    [Fact(Explicit = true)]
    public void ExecuteFrozenHFeasibilitySearch_AfterSuccessfulPreexecutionReview()
    {
        Assert.Equal("1", Environment.GetEnvironmentVariable("NRS_R3_H_FEASIBILITY1"));
        var contractPath = Path.Combine(R3HFeasibilityHarness.Root, "eng", "r3-h-feasibility1-contract.json");
        using var review = JsonDocument.Parse(File.ReadAllText(Path.Combine(R3HFeasibilityHarness.Output, "03-preexecution-review.json")));
        Assert.Equal("PASS-PREEXECUTION-REVIEW", review.RootElement.GetProperty("status").GetString());
        Assert.Equal(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(contractPath))), review.RootElement.GetProperty("contract_sha256").GetString());
        Assert.Equal(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(typeof(M10FinalVr2R3HConservedInventoryFeasibility1Tests).Assembly.Location))),
            review.RootElement.GetProperty("test_assembly_sha256").GetString());
        using var contract = JsonDocument.Parse(File.ReadAllText(contractPath));
        foreach (var file in contract.RootElement.GetProperty("implementation_files").EnumerateArray())
            Assert.Equal(file.GetProperty("sha256").GetString(), Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(R3HFeasibilityHarness.Root, file.GetProperty("path").GetString()!)))));

        var harness = new R3HFeasibilityHarness();
        var search = new R3HFeasibilitySearch(24);
        var result = search.Run(harness.Sample);
        Directory.CreateDirectory(R3HFeasibilityHarness.Output);
        File.WriteAllLines(Path.Combine(R3HFeasibilityHarness.Output, "04-search-trace.jsonl"), search.Trace.Select(static e => JsonSerializer.Serialize(e)), new UTF8Encoding(false));
        R3HFeasibilityHarness.Write("05-search-result.json", new { result, inventory = harness.InventoryVector(result.Coordinates) });
        var confirmations = new List<object>();
        var feasible = result.Root;
        string? firstSignature = null;
        if (result.Root)
        {
            for (var run = 1; run <= 3; run++)
            {
                var independent = new R3HFeasibilityHarness();
                var evaluation = independent.Evaluate(result.Coordinates);
                var observations = evaluation.Observations.ToList();
                var health = new List<R3HFeasibilityHarness.Observation> { evaluation.Observations[2] };
                var previous = evaluation.Observations[2].Nodes;
                for (var step = 2; step <= 100; step++)
                {
                    evaluation.Engine.Step(Application.ControlRoom.ControlRoomRunState.Running);
                    var observation = independent.Observe(step.ToString(System.Globalization.CultureInfo.InvariantCulture), evaluation.Engine, previous);
                    health.Add(observation);
                    if (R3HFeasibilityHarness.Checkpoints.Contains(step)) observations.Add(observation);
                    previous = observation.Nodes;
                }
                var signature = R3HFeasibilityHarness.Hash(JsonSerializer.Serialize(new { evaluation.Raw, observations, health }));
                firstSignature ??= signature;
                Assert.Equal(firstSignature, signature);
                feasible &= observations.All(static o => o.Healthy && o.ScaledResiduals.All(static r => Math.Abs(r) <= 1))
                    && health.All(static o => o.Healthy && o.InEnvelope);
                confirmations.Add(new { run, signature, evaluation.Raw, observations, health });
            }
        }
        var classification = !result.Root ? "H-INCONCLUSIVE-NO-CANDIDATE-IN-BOUNDS" : feasible ? "H-FEASIBLE-FLUID-SEED" : "H-CANDIDATE-NOT-QUALIFIED";
        R3HFeasibilityHarness.Write("06-confirmation.json", confirmations);
        R3HFeasibilityHarness.Write("07-execution-summary.json", new
        {
            status = "PASS-EVIDENCE-COMPLETE", classification, result.Stop, result.Evaluations,
            root = result.Root, max_scaled_residual = result.Residuals?.Max(static r => Math.Abs(r)),
            confirmation_runs = confirmations.Count, production_change = false, r3_passed = false,
            next_activity = "RETURNED-H-EVIDENCE-REVIEW-AND-PLANNING-ONLY"
        });
        Assert.InRange(result.Evaluations, 1, 4096);
    }

    private static double Parse(string value) => double.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
}
