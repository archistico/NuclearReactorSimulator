using System.Security.Cryptography;
using System.Text.Json;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay;

public sealed class M10R3HLocalModelTrial1Tests
{
    private static readonly string Output = Path.Combine(R3HFeasibilityHarness.Root, "artifacts", "r3-h-local-trial1");
    internal sealed record Point(string Id, double[] Coordinates, double[] Predicted, string[] Phases);
    internal sealed record Inputs(Point[] Points);
    internal sealed record Trial(string Id, int Repeat, string Reason, double[] Coordinates,
        R3HFeasibilityHarness.Node[]? Raw, object[]? Observations, double[]? Residuals, string[]? Phases);

    private static void Validate(Inputs input)
    {
        Assert.Equal(new[] { "baseline", "lambda-0.001", "lambda-0.01", "lambda-0.1", "lambda-1", "lambda-10", "lambda-100" }, input.Points.Select(p => p.Id));
        foreach (var point in input.Points)
        {
            Assert.Equal(24, point.Coordinates.Length);
            Assert.All(point.Coordinates, x => Assert.True(double.IsFinite(x) && Math.Abs(x) <= .02));
            Assert.Equal(75, point.Predicted.Length);
            Assert.All(point.Predicted, x => Assert.True(double.IsFinite(x)));
            Assert.Equal(48, point.Phases.Length);
        }
    }

    private static readonly string FrozenInputs = Path.Combine(R3HFeasibilityHarness.Root, "eng", "contracts", "r3-h-local-trial1-inputs.json");
    private static Inputs ReadInputs() => JsonSerializer.Deserialize<Inputs>(File.ReadAllText(FrozenInputs))!;
    private static string Sha(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));

    [Fact]
    public void Preexecution_AcceptsOnlyFrozenOrderAndBoundedFiniteCoordinates() => Validate(ReadInputs());

    [Fact]
    public void Preexecution_RejectsExtraPointAndOutOfBoundsBeforePhysics()
    {
        var input = ReadInputs();
        Assert.ThrowsAny<Exception>(() => Validate(new(input.Points.Append(input.Points[0]).ToArray())));
        input.Points[1].Coordinates[0] = .02000001;
        Assert.ThrowsAny<Exception>(() => Validate(input));
    }

    [Fact]
    public void Preexecution_RejectsNonfiniteAndReorderedInputs()
    {
        var input = ReadInputs();
        (input.Points[1], input.Points[2]) = (input.Points[2], input.Points[1]);
        Assert.ThrowsAny<Exception>(() => Validate(input));
        input = ReadInputs();
        input.Points[2].Predicted[4] = double.NaN;
        Assert.ThrowsAny<Exception>(() => Validate(input));
    }

    [Fact(Explicit = true)]
    public void ExecuteOnlyReviewedFourteenRecipes()
    {
        Assert.Equal("1", Environment.GetEnvironmentVariable("NRS_R3_H_LOCAL_TRIAL1"));
        using var review = JsonDocument.Parse(File.ReadAllText(Path.Combine(Output, "03-preexecution-review.json")));
        var token = review.RootElement;
        Assert.Equal("PASS-PREEXECUTION", token.GetProperty("status").GetString());
        var contractPath = Path.Combine(R3HFeasibilityHarness.Root, "eng", "r3-h-local-trial1-contract.json");
        Assert.Equal(Sha(contractPath), token.GetProperty("contract_sha256").GetString());
        Assert.Equal(Sha(typeof(M10R3HLocalModelTrial1Tests).Assembly.Location), token.GetProperty("assembly_sha256").GetString());
        Assert.Equal(Sha(Path.Combine(Output, "01-inputs.json")), token.GetProperty("inputs_sha256").GetString());
        Assert.Equal(Sha(FrozenInputs), token.GetProperty("inputs_sha256").GetString());
        using var contract = JsonDocument.Parse(File.ReadAllText(contractPath));
        foreach (var file in contract.RootElement.GetProperty("implementation_files").EnumerateArray())
            Assert.Equal(file.GetProperty("sha256").GetString(), Sha(Path.Combine(R3HFeasibilityHarness.Root, file.GetProperty("path").GetString()!)));
        var input = ReadInputs();
        Validate(input);
        var harness = new R3HFeasibilityHarness(); // Two setup steps, separate from the recipe budget.
        var records = new List<Trial>();
        foreach (var point in input.Points)
        {
            string? signature = null;
            for (var repeat = 1; repeat <= 2; repeat++)
            {
                Trial trial;
                try
                {
                    var evaluation = harness.Evaluate(point.Coordinates);
                    var phases = evaluation.Raw.Select(n => n.Phase).Concat(evaluation.Observations.SelectMany(o => o.Nodes.Select(n => n.Phase))).ToArray();
                    if (point.Id == "baseline")
                    {
                        Assert.Equal(point.Predicted, evaluation.Residuals);
                        Assert.Equal(point.Phases, phases);
                        Assert.All(evaluation.Observations, o => Assert.True(o.Healthy));
                    }
                    trial = new(point.Id, repeat, evaluation.Observations.All(o => o.Healthy) ? "VALID" : "HEALTH-SENTINEL",
                        point.Coordinates, evaluation.Raw, evaluation.Observations.Select(o => (object)new
                        {
                            o.Checkpoint, o.Nodes, o.ScaledResiduals, o.Healthy, o.InEnvelope,
                            o.ElectricalMwe, o.PumpKgS, o.DrumLevel, o.Governor, o.RotorTorque,
                            o.MassClosure, o.EnergyClosure, StateSha256 = R3HFeasibilityHarness.Hash(o.StateJson)
                        }).ToArray(), evaluation.Residuals, phases);
                }
                catch (R3HFeasibilityHarness.CandidateRejectedException ex) { trial = Reject(ex.Message); }
                catch (WaterSteamStateOutOfRangeException ex) { trial = Reject("THERMODYNAMIC-DOMAIN/" + ex.Message); }
                catch (ArgumentOutOfRangeException ex) { trial = Reject("QUANTITY-DOMAIN/" + ex.ParamName); }
                catch (ArithmeticException ex) { trial = Reject("NUMERICAL-REJECTION/" + ex.Message); }
                Assert.True(point.Id != "baseline" || trial.Reason == "VALID");
                // The repeat number is the only allowed difference in the complete observation record.
                var current = JsonSerializer.Serialize(trial with { Repeat = 0 });
                signature ??= current;
                Assert.Equal(signature, current);
                records.Add(trial);
                File.WriteAllLines(Path.Combine(Output, "04-trials.jsonl"), records.Select(r => JsonSerializer.Serialize(r)));
                Trial Reject(string reason) => new(point.Id, repeat, reason, point.Coordinates, null, null, null, null);
            }
        }
        Assert.Equal(14, records.Count);
    }
}
