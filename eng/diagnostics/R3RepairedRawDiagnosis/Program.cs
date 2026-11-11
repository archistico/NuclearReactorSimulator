using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using NuclearReactorSimulator.Simulation.Physics.Control.Integration;

const string protocol = "r3-repaired-raw-owner-diagnostic1";
if (args.Length != 1) throw new ArgumentException("Supply the repository root.");
var root = Path.GetFullPath(args[0]);
var output = Path.Combine(root, "artifacts", protocol);
var contractPath = Path.Combine(root, "eng", protocol + "-contract.json");
using var contract = JsonDocument.Parse(File.ReadAllText(contractPath));
using var token = JsonDocument.Parse(File.ReadAllText(Path.Combine(output, "02-preexecution.json")));
if (token.RootElement.GetProperty("contract_sha256").GetString() != FileHash(contractPath))
    throw new InvalidOperationException("Contract identity drift.");
foreach (var entry in contract.RootElement.GetProperty("implementation_files").EnumerateArray())
    if (FileHash(Path.Combine(root, entry.GetProperty("path").GetString()!)) != entry.GetProperty("sha256").GetString())
        throw new InvalidOperationException("Implementation identity drift.");
foreach (var entry in token.RootElement.GetProperty("assemblies").EnumerateArray())
    if (FileHash(Path.Combine(AppContext.BaseDirectory, entry.GetProperty("file").GetString()!)) != entry.GetProperty("sha256").GetString())
        throw new InvalidOperationException("Assembly identity drift.");
var target = Path.Combine(output, "03-measurements.jsonl");
// A failed or partial execution remains visible and cannot be silently retried.
using var stream = new FileStream(target, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
using var writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true };
var factory = typeof(IControlRoomRuntimeEngine).Assembly
    .GetType("NuclearReactorSimulator.Application.Scenarios.Training.DesktopSustainedGenerationInitialConditionFactory", true)!
    .GetMethod("CreateReferenceConsistentPostMoistureEquilibriumCandidateRuntimeEngine", BindingFlags.NonPublic | BindingFlags.Static)
    ?? throw new MissingMethodException("Frozen C4 factory not found.");
for (var repeat = 1; repeat <= 2; repeat++)
{
    // Exactly two preparation steps in the production factory and one Running step.
    var engine = (IntegratedAutomaticOperationRuntimeEngine)factory.Invoke(null, [TimeSpan.FromMilliseconds(10)])!;
    var before = engine.CurrentState.PlantState.PlantState;
    var beforeHash = TextHash(StateJson(engine));
    engine.Step(ControlRoomRunState.Running);
    var after = engine.CurrentState.PlantState.PlantState;
    var cycle = Canonical(engine).Control.ProtectedControl.FullPlant.IntegratedCycle;
    var groups = cycle.PrimaryCircuit.ChannelGroups.Groups;
    var definition = before.Definition;
    double Temperature(string id) => before.FluidNodes.FirstOrDefault(n => n.Id == id)?.Temperature.Kelvins
        ?? before.ThermalBodies.Single(n => n.Id == id).Temperature.Kelvins;
    writer.WriteLine(JsonSerializer.Serialize(new
    {
        Id = $"base/repeat/{repeat}", BeforeStateSha256 = beforeHash, AfterStateSha256 = TextHash(StateJson(engine)),
        Before = Nodes(before), After = Nodes(after),
        Thermal = before.ThermalBodies.Select(b => new
        {
            b.Id, BeforeK = b.Temperature.Kelvins, AfterK = after.GetThermalBody(b.Id).Temperature.Kelvins,
            BeforeJ = b.StoredThermalEnergy.Joules, AfterJ = after.GetThermalBody(b.Id).StoredThermalEnergy.Joules
        }),
        HeatTransfers = definition.HeatTransfers.Select(h => new
        {
            h.Id, h.FromDomainId, h.ToDomainId, ConductanceWK = h.Conductance.WattsPerKelvin,
            FromK = Temperature(h.FromDomainId), ToK = Temperature(h.ToDomainId)
        }),
        HeatSources = definition.HeatSources.Select(h => new
        {
            h.Id, h.TargetDomainId, AppliedW = before.HeatSources.Single(s => s.HeatSourceId == h.Id).IsEnabled ? h.RatedThermalPower.Watts : 0d
        }),
        ChannelOwners = cycle.PrimaryCircuit.Definition.ChannelGroups.Groups.Select(g => new
        {
            g.Id, g.FuelThermalBodyId, g.StructureThermalBodyId, g.OutletCoolantNodeId,
            FuelHeatW = groups.Single(s => s.GroupId == g.Id).FuelHeatPower.Watts,
            StructureHeatW = groups.Single(s => s.GroupId == g.Id).StructureHeatPower.Watts,
            CoolantHeatW = groups.Single(s => s.GroupId == g.Id).CoolantHeatPower.Watts
        }),
        Pipes = definition.Pipes.Select(p => new { p.Id, p.FromNodeId, p.ToNodeId }),
        Valves = definition.Valves.Select(v => new { v.Id, v.Pipe.FromNodeId, v.Pipe.ToNodeId }),
        Pumps = definition.Pumps.Select(p => new { p.Id, p.Pipe.FromNodeId, p.Pipe.ToNodeId }),
        HistoricalChannelFlowKgS = cycle.PrimaryCircuit.MainCirculation.TotalChannelMassFlowRate.KilogramsPerSecond,
        HistoricalReturnFlowKgS = cycle.PrimaryCircuit.MainCirculation.TotalReturnMassFlowRate.KilogramsPerSecond,
        HydraulicNumerics = cycle.PrimaryCircuit.HydraulicNumerics,
        Stage = cycle.TurbineExpansion.StageGroups.Single(),
        Condenser = cycle.Condenser.Condensers.Single(),
        Cooling = cycle.Condenser.CoolingBoundaries.Single(),
        CoolingInstalledW = cycle.Condenser.CoolingBoundaries.Single().InstalledHeatRejectionCapacity.Watts,
        CoolingActiveLimits = cycle.Condenser.CoolingBoundaries.Single().ActiveHeatRejectionLimits,
        Bypasses = cycle.Condenser.TurbineBypasses
    }));
}
Console.WriteLine("Captured two baseline recipes / six runtime steps; no perturbation or activation.");

static object[] Nodes(PlantState plant) => plant.FluidNodes.OrderBy(n => n.Id, StringComparer.Ordinal).Select(n => (object)new
{
    n.Id, Mass = n.Mass.Kilograms, Energy = n.InternalEnergy.Joules, PressurePa = n.Pressure.Pascals,
    TemperatureK = n.Temperature.Kelvins, DensityKgM3 = n.Density.KilogramsPerCubicMetre,
    VolumeM3 = plant.Definition.GetFluidNode(n.Id).Volume.CubicMetres,
    EnthalpyJKg = FluidEnergyTransport.ResolveSpecificEnthalpy(n.SpecificInternalEnergy, n.Pressure, n.Density).JoulesPerKilogram,
    Phase = n.Phase.ToString(), Quality = n.VaporQuality?.Fraction
}).ToArray();

// Same explicit state-owner projection as the existing R3HFeasibilityHarness.StateJson.
static string StateJson(IntegratedAutomaticOperationRuntimeEngine engine)
{
    var state = engine.CurrentState;
    var snapshot = Canonical(engine).Control.ProtectedControl;
    var plant = state.PlantState.PlantState;
    return JsonSerializer.Serialize(new
    {
        Fluids = plant.FluidNodes.Select(static n => new { n.Id, Mass = n.Mass.Kilograms, Energy = n.InternalEnergy.Joules, Pressure = n.Pressure.Pascals, Temperature = n.Temperature.Kelvins, n.Phase, Quality = n.VaporQuality?.Fraction }),
        Thermal = plant.ThermalBodies.Select(static n => new { n.Id, Energy = n.StoredThermalEnergy.Joules, Temperature = n.Temperature.Kelvins }),
        plant.Valves, plant.Pumps, plant.HeatSources,
        state.ReactorPrimaryControlState.PointKinetics, state.ReactorPrimaryControlState.IodineXenon, state.ReactorPrimaryControlState.ControlRods,
        PrimaryMemory = state.ReactorPrimaryControlState.ControlAndActuator.Controllers.Controllers,
        PrimaryActuators = state.ReactorPrimaryControlState.ControlAndActuator.Actuators.Actuators,
        SecondaryMemory = state.TurbineSecondaryControlState.ControlAndActuator.Controllers.Controllers,
        SecondaryActuators = state.TurbineSecondaryControlState.ControlAndActuator.Actuators.Actuators,
        PrimaryControllers = snapshot.ReactorPrimary.ControlAndActuator.Controllers.Diagnostics,
        SecondaryControllers = snapshot.TurbineSecondary.ControlAndActuator.Controllers.Diagnostics,
        Rotors = snapshot.FullPlant.IntegratedCycle.TurbineExpansion.Rotors,
        Generators = snapshot.FullPlant.IntegratedCycle.Generators,
        FissionWatts = snapshot.FullPlant.IntegratedCycle.PrimaryCircuit.TotalFissionThermalPower.Watts,
        DecayWatts = snapshot.FullPlant.IntegratedCycle.PrimaryCircuit.TotalDecayHeatPower.Watts
    });
}
static string TextHash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
static string FileHash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
static IntegratedAutomaticOperationSnapshot Canonical(IntegratedAutomaticOperationRuntimeEngine engine)
    => (IntegratedAutomaticOperationSnapshot)typeof(IntegratedAutomaticOperationRuntimeEngine)
        .GetProperty("LatestCanonicalSnapshot", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(engine)!;
