using System.Security.Cryptography;
using System.Text.Json;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay;

public sealed class M10R3HProtocolBatchTests
{
    internal sealed record Request(string Id, double[] Coordinates);
    private static string Sha(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
    private static string Protocol(string? value)
    {
        Assert.NotNull(value);
        Assert.Matches("^r3-h-[a-z0-9]+(?:-[a-z0-9]+)*$", value);
        return value;
    }

    private static void Validate(Request[] requests)
    {
        Assert.InRange(requests.Length, 1, 48);
        Assert.Equal(requests.Length, requests.Select(r => r.Id).Distinct(StringComparer.Ordinal).Count());
        foreach (var request in requests)
        {
            Assert.False(string.IsNullOrWhiteSpace(request.Id));
            Assert.Equal(24, request.Coordinates.Length);
            Assert.All(request.Coordinates, x => Assert.True(double.IsFinite(x) && Math.Abs(x) <= .02));
        }
    }

    [Fact]
    public void Preexecution_RejectsProtocolPathTraversal()
    {
        Assert.Equal("r3-h-minimax-trial2", Protocol("r3-h-minimax-trial2"));
        Assert.ThrowsAny<Exception>(() => Protocol("../outside"));
        Assert.ThrowsAny<Exception>(() => Protocol("r3-h-x/../other"));
        Assert.ThrowsAny<Exception>(() => Protocol(null));
    }

    [Fact]
    public void Preexecution_RejectsUnbudgetedOrAmbiguousBatch()
    {
        Assert.ThrowsAny<Exception>(() => Validate([]));
        Assert.ThrowsAny<Exception>(() => Validate(Enumerable.Range(0, 49).Select(i => new Request(i.ToString(), new double[24])).ToArray()));
        Assert.ThrowsAny<Exception>(() => Validate([new("same", new double[24]), new("same", new double[24])]));
    }

    [Fact]
    public void Preexecution_RejectsInvalidCoordinatesBeforePhysics()
    {
        var coordinates = new double[24];
        Validate([new("fixture", coordinates)]);
        coordinates[0] = .020001;
        Assert.ThrowsAny<Exception>(() => Validate([new("fixture", coordinates)]));
        coordinates[0] = double.NaN;
        Assert.ThrowsAny<Exception>(() => Validate([new("fixture", coordinates)]));
        Assert.ThrowsAny<Exception>(() => Validate([new("fixture", new double[23])]));
    }

    [Fact(Explicit = true)]
    public void ExecuteReviewedProtocolBatch()
    {
        var protocol = Protocol(Environment.GetEnvironmentVariable("NRS_R3_H_PROTOCOL"));
        var output = Path.Combine(R3HFeasibilityHarness.Root, "artifacts", protocol);
        var contractPath = Path.Combine(R3HFeasibilityHarness.Root, "eng", protocol + "-contract.json");
        using var tokenDocument = JsonDocument.Parse(File.ReadAllText(Path.Combine(output, "batch-token.json")));
        var token = tokenDocument.RootElement;
        Assert.Equal("PASS-BATCH-REVIEW", token.GetProperty("status").GetString());
        Assert.Equal(Sha(contractPath), token.GetProperty("contract_sha256").GetString());
        Assert.Equal(Sha(typeof(M10R3HProtocolBatchTests).Assembly.Location), token.GetProperty("assembly_sha256").GetString());
        var requestPath = Path.Combine(output, "batch-request.json");
        Assert.Equal(Sha(requestPath), token.GetProperty("request_sha256").GetString());
        using var contractDocument = JsonDocument.Parse(File.ReadAllText(contractPath));
        var contract = contractDocument.RootElement;
        Assert.Equal(protocol + "-v1", contract.GetProperty("schema").GetString());
        Assert.Equal("artifacts/" + protocol, contract.GetProperty("output").GetString());
        foreach (var file in contract.GetProperty("implementation_files").EnumerateArray())
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
                var cycle = evaluation.Engine.LatestCanonicalSnapshot.Control.ProtectedControl.FullPlant.IntegratedCycle;
                var steam = cycle.TurbineExpansion.MainSteamNetwork;
                record = new
                {
                    request.Id, request.Coordinates, Reason = evaluation.Observations.All(o => o.Healthy) ? "VALID" : "HEALTH-SENTINEL",
                    evaluation.Raw, Residuals = evaluation.Residuals,
                    Phases = evaluation.Raw.Select(n => n.Phase).Concat(evaluation.Observations.SelectMany(o => o.Nodes.Select(n => n.Phase))).ToArray(),
                    Observations = evaluation.Observations.Select(o => new
                    {
                        o.Checkpoint, o.Nodes, o.ScaledResiduals, o.Healthy, o.InEnvelope,
                        o.ElectricalMwe, o.PumpKgS, o.DrumLevel, o.Governor, o.RotorTorque,
                        o.MassClosure, o.EnergyClosure, StateSha256 = R3HFeasibilityHarness.Hash(o.StateJson)
                    }).ToArray(),
                    SecondaryEnergy = new
                    {
                        Checkpoint = "1",
                        Trains = steam.AdmissionTrains.Select(t => new
                        {
                            t.TrainId, t.HeaderNodeId, t.TurbineInletNodeId,
                            StopMassKgS = t.StopValve.MassFlowRate.KilogramsPerSecond, StopEnergyW = t.StopValve.AdvectedEnergyFlowRate.Watts,
                            ControlMassKgS = t.ControlValve.MassFlowRate.KilogramsPerSecond, ControlEnergyW = t.ControlValve.AdvectedEnergyFlowRate.Watts,
                            AdmissionMassKgS = t.AdmissionValve.MassFlowRate.KilogramsPerSecond, AdmissionEnergyW = t.AdmissionValve.AdvectedEnergyFlowRate.Watts
                        }).ToArray(),
                        Boundaries = steam.TurbineAdmissionBoundaries.Select(b => new { b.BoundaryId, b.SourceNodeId, MassKgS = b.MassFlowRate.KilogramsPerSecond, EnergyW = b.EnergyExportRate.Watts }).ToArray(),
                        Stages = cycle.TurbineExpansion.StageGroups.Select(s => new
                        {
                            s.StageGroupId, s.InletNodeId, s.ExhaustNodeId, s.MoistureDrainNodeId,
                            MassKgS = s.TotalTransferredMassFlowRate.KilogramsPerSecond, VaporMassKgS = s.EffectiveMassFlowRate.KilogramsPerSecond,
                            InletEnergyW = s.InletEnergyFlowRate.Watts, ExhaustEnergyW = s.ExhaustEnergyFlowRate.Watts,
                            ShaftPowerW = s.ShaftPower.Watts, DrainMassKgS = s.MoistureDrainMassFlowRate.KilogramsPerSecond,
                            DrainEnergyW = s.MoistureDrainEnergyFlowRate.Watts, OwnershipResidualW = s.TurbineEnergyOwnershipResidual.Watts
                        }).ToArray()
                    }
                };
            }
            catch (R3HFeasibilityHarness.CandidateRejectedException ex) { record = Reject(ex.Message); }
            catch (WaterSteamStateOutOfRangeException ex) { record = Reject("THERMODYNAMIC-DOMAIN/" + ex.Message); }
            catch (ArgumentOutOfRangeException ex) { record = Reject("QUANTITY-DOMAIN/" + ex.ParamName); }
            catch (ArithmeticException ex) { record = Reject("NUMERICAL-REJECTION/" + ex.Message); }
            records.Add(record);
            object Reject(string reason) => new { request.Id, request.Coordinates, Reason = reason, Residuals = (double[]?)null };
        }
        File.WriteAllLines(Path.Combine(output, "batch-results.jsonl"), records.Select(r => JsonSerializer.Serialize(r)));
    }
}
