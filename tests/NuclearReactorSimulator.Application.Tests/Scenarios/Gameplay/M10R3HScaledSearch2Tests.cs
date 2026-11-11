using System.Security.Cryptography;
using System.Text.Json;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay;

public sealed class M10R3HScaledSearch2Tests
{
    private static readonly string Output = Path.Combine(R3HFeasibilityHarness.Root, "artifacts", "r3-h-scaled-search2");
    internal sealed record Request(string Id, double[] Coordinates);
    private static string Sha(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));

    private static void Validate(Request[] requests)
    {
        Assert.InRange(requests.Length, 1, 48);
        Assert.Equal(requests.Length, requests.Select(r => r.Id).Distinct(StringComparer.Ordinal).Count());
        foreach (var request in requests)
        {
            Assert.Equal(24, request.Coordinates.Length);
            Assert.All(request.Coordinates, x => Assert.True(double.IsFinite(x) && Math.Abs(x) <= .02));
        }
    }

    [Fact]
    public void Preexecution_RejectsEscapedAndNonfiniteBatches()
    {
        var point = new double[24];
        Validate([new("fixture", point)]);
        point[0] = .020001;
        Assert.ThrowsAny<Exception>(() => Validate([new("fixture", point)]));
        point[0] = double.NaN;
        Assert.ThrowsAny<Exception>(() => Validate([new("fixture", point)]));
    }

    [Fact]
    public void Preexecution_RejectsUnbudgetedAndAmbiguousBatches()
    {
        Assert.ThrowsAny<Exception>(() => Validate([]));
        Assert.ThrowsAny<Exception>(() => Validate(Enumerable.Range(0, 49).Select(i => new Request(i.ToString(), new double[24])).ToArray()));
        Assert.ThrowsAny<Exception>(() => Validate([new("same", new double[24]), new("same", new double[24])]));
    }

    [Fact(Explicit = true)]
    public void ExecuteReviewedBatch()
    {
        Assert.Equal("1", Environment.GetEnvironmentVariable("NRS_R3_H_SCALED_SEARCH2"));
        using var tokenDocument = JsonDocument.Parse(File.ReadAllText(Path.Combine(Output, "batch-token.json")));
        var token = tokenDocument.RootElement;
        Assert.Equal("PASS-BATCH-REVIEW", token.GetProperty("status").GetString());
        var contractPath = Path.Combine(R3HFeasibilityHarness.Root, "eng", "r3-h-scaled-search2-contract.json");
        Assert.Equal(Sha(contractPath), token.GetProperty("contract_sha256").GetString());
        Assert.Equal(Sha(typeof(M10R3HScaledSearch2Tests).Assembly.Location), token.GetProperty("assembly_sha256").GetString());
        var requestPath = Path.Combine(Output, "batch-request.json");
        Assert.Equal(Sha(requestPath), token.GetProperty("request_sha256").GetString());
        using var contract = JsonDocument.Parse(File.ReadAllText(contractPath));
        foreach (var file in contract.RootElement.GetProperty("implementation_files").EnumerateArray())
            Assert.Equal(file.GetProperty("sha256").GetString(), Sha(Path.Combine(R3HFeasibilityHarness.Root, file.GetProperty("path").GetString()!)));
        var requests = JsonSerializer.Deserialize<Request[]>(File.ReadAllText(requestPath))!;
        Validate(requests);
        var harness = new R3HFeasibilityHarness();
        var records = new List<object>();
        foreach (var request in requests)
        {
            object record;
            try
            {
                var evaluation = harness.Evaluate(request.Coordinates);
                var phases = evaluation.Raw.Select(n => n.Phase).Concat(evaluation.Observations.SelectMany(o => o.Nodes.Select(n => n.Phase))).ToArray();
                record = new
                {
                    request.Id, request.Coordinates, Reason = evaluation.Observations.All(o => o.Healthy) ? "VALID" : "HEALTH-SENTINEL",
                    evaluation.Raw, Residuals = evaluation.Residuals, Phases = phases,
                    Observations = evaluation.Observations.Select(o => new
                    {
                        o.Checkpoint, o.Nodes, o.ScaledResiduals, o.Healthy, o.InEnvelope,
                        o.ElectricalMwe, o.PumpKgS, o.DrumLevel, o.Governor, o.RotorTorque,
                        o.MassClosure, o.EnergyClosure, StateSha256 = R3HFeasibilityHarness.Hash(o.StateJson)
                    }).ToArray()
                };
            }
            catch (R3HFeasibilityHarness.CandidateRejectedException ex) { record = Reject(ex.Message); }
            catch (WaterSteamStateOutOfRangeException ex) { record = Reject("THERMODYNAMIC-DOMAIN/" + ex.Message); }
            catch (ArgumentOutOfRangeException ex) { record = Reject("QUANTITY-DOMAIN/" + ex.ParamName); }
            catch (ArithmeticException ex) { record = Reject("NUMERICAL-REJECTION/" + ex.Message); }
            records.Add(record);
            object Reject(string reason) => new { request.Id, request.Coordinates, Reason = reason, Residuals = (double[]?)null };
        }
        File.WriteAllLines(Path.Combine(Output, "batch-results.jsonl"), records.Select(r => JsonSerializer.Serialize(r)));
    }
}
