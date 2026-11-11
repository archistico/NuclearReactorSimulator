using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay;
using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Simulation.Physics.Control.Integration;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using NuclearReactorSimulator.Simulation.Physics.Instrumentation;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.MainSteam;

const BindingFlags hidden = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
if (args.Length is < 2 or > 3 || (args.Length == 3 && args[2] != "--validate-only"))
    throw new ArgumentException("Supply repository root, protocol and optional --validate-only.");
var root = Path.GetFullPath(args[0]);
var protocol = args[1];
if (!System.Text.RegularExpressions.Regex.IsMatch(protocol, "^r3-repaired-[a-z0-9]+(?:-[a-z0-9]+)*$"))
    throw new ArgumentException("Invalid protocol selector.");
var output = Path.Combine(root, "artifacts", protocol);
var contractPath = Path.Combine(root, "eng", protocol + "-contract.json");
using var contract = JsonDocument.Parse(File.ReadAllText(contractPath));
using var token = JsonDocument.Parse(File.ReadAllText(Path.Combine(output, "02-preexecution.json")));
if (token.RootElement.GetProperty("contract_sha256").GetString() != Hash(File.ReadAllBytes(contractPath)))
    throw new InvalidOperationException("Contract identity drift.");
foreach (var entry in contract.RootElement.GetProperty("implementation_files").EnumerateArray())
    if (Hash(File.ReadAllBytes(Path.Combine(root, entry.GetProperty("path").GetString()!))) != entry.GetProperty("sha256").GetString())
        throw new InvalidOperationException("Implementation identity drift.");
foreach (var entry in token.RootElement.GetProperty("assemblies").EnumerateArray())
    if (Hash(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, entry.GetProperty("file").GetString()!))) != entry.GetProperty("sha256").GetString())
        throw new InvalidOperationException("Assembly identity drift.");
var requestsPath = Path.Combine(output, "01-requests.json");
if (token.RootElement.GetProperty("requests_sha256").GetString() != Hash(File.ReadAllBytes(requestsPath)))
    throw new InvalidOperationException("Request identity drift.");
var requests = JsonSerializer.Deserialize<Request[]>(File.ReadAllText(requestsPath))!;
var policy = contract.RootElement.GetProperty("policy");
var count = policy.GetProperty("recipes").GetInt32();
var steps = policy.GetProperty("maximum_runtime_steps").GetInt32();
if (contract.RootElement.GetProperty("schema").GetString() != protocol + "-v1" || count is < 1 or > 512 || steps != 2 + 3 * count)
    throw new InvalidOperationException("Invalid bounded acquisition contract.");
if (requests.Length != count || requests.Select(r => r.Id).Distinct(StringComparer.Ordinal).Count() != requests.Length)
    throw new InvalidOperationException("Unbudgeted request count.");
foreach (var request in requests)
    if (request.Coordinates.Length != 24 || request.OperatingPointFractions.Length != 6 ||
        string.IsNullOrWhiteSpace(request.Id) ||
        !request.Coordinates.Concat(request.OperatingPointFractions).All(x => double.IsFinite(x) && Math.Abs(x) <= .02))
        throw new InvalidOperationException("Invalid request; checked before physics.");
if (args.Length == 3)
{
    Console.WriteLine("PASS-REQUEST-AND-BINARY-PREFLIGHT; zero runtime steps.");
    return;
}

// Read existing internal diagnostic seams without editing src/tests or their assembly identities.
var harnessType = typeof(M10R3StageTransportOwnershipRepairTests).Assembly.GetType(
    "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.R3HFeasibilityHarness", true)!;
var operatingType = harnessType.GetNestedType("OperatingPoint", hidden)!;
var nodeType = harnessType.GetNestedType("Node", hidden)!;
var observe = harnessType.GetMethod("Observe", hidden)!;
var create = harnessType.GetMethods(hidden).Single(m => m.Name == "CreateSeedEngine" && m.GetParameters().Length == 3);
using var stream = new FileStream(Path.Combine(output, "03-measurements.jsonl"), FileMode.CreateNew, FileAccess.Write, FileShare.Read);
using var writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true };
// The harness constructor performs exactly two setup runtime steps, once for the entire acquisition.
var harness = Activator.CreateInstance(harnessType, true)!;
var baseline = (Array)harnessType.GetField("_baseline", hidden)!.GetValue(harness)!;
var volumes = (double[])harnessType.GetField("_volumes", hidden)!.GetValue(harness)!;
var resolver = harnessType.GetField("_resolver", hidden)!.GetValue(harness)!;
var resolve = resolver.GetType().GetMethod("TryResolve", hidden)!;
foreach (var request in requests)
{
    try
    {
        var seeds = Array.CreateInstance(baseline.GetType().GetElementType()!, 12);
        var raw = Array.CreateInstance(nodeType, 12);
        var initialFluidTemperatures = new Dictionary<string, double>(StringComparer.Ordinal);
        for (var index = 0; index < 12; index++)
        {
            var seed = baseline.GetValue(index)!;
            var id = (string)Get(seed, "NodeId");
            var mass = (double)Get(seed, "MassKilograms") * (1 + request.Coordinates[2 * index]);
            var energy = (double)Get(seed, "InternalEnergyJoules") * (1 + request.Coordinates[2 * index + 1]);
            object?[] input = [volumes[index] / mass, energy / mass, null];
            if (!(bool)Invoke(resolve, resolver, input)!) throw new Rejected("C4-UNRESOLVED/" + id);
            var resolved = input[2]!;
            var state = (FluidThermodynamicState)Invoke(resolved.GetType().GetMethod("ToFluidThermodynamicState", hidden)!, resolved, [])!;
            seeds.SetValue(Activator.CreateInstance(seed.GetType(), hidden, null,
                [id, mass, energy, state.Pressure.Pascals, state.Temperature.Kelvins, state.Phase, state.VaporQuality?.Fraction], null), index);
            raw.SetValue(Activator.CreateInstance(nodeType, hidden, null,
                [id, mass, energy, state.Pressure.Pascals, state.Temperature.Kelvins, state.Phase.ToString(), state.VaporQuality?.Fraction], null), index);
            initialFluidTemperatures.Add(id, state.Temperature.Kelvins);
        }
        var operating = Invoke(operatingType.GetMethod("FromFractions", hidden)!, null, [request.OperatingPointFractions])!;
        var first = (IntegratedAutomaticOperationRuntimeEngine)Invoke(create, null, [seeds, 1, operating])!;
        var thermalInitial = first.CurrentState.PlantState.PlantState.ThermalBodies.ToDictionary(b => b.Id,
            b => new ThermalPoint(b.Id,
                b.Definition.HeatCapacity.JoulesPerKelvin * Temperature.FromDegreesCelsius((double)Get(operating,
                    b.Id == "fuel" ? "FuelTemperatureCelsius" : "StructureTemperatureCelsius")).Kelvins,
                Temperature.FromDegreesCelsius((double)Get(operating, b.Id == "fuel" ? "FuelTemperatureCelsius" : "StructureTemperatureCelsius")).Kelvins), StringComparer.Ordinal);
        var firstObservation = Invoke(observe, harness, ["seed-step1", first, raw])!;
        var firstExtra = Extra(first, thermalInitial, initialFluidTemperatures);
        var firstPlant = first.CurrentState.PlantState.PlantState;
        var state1 = first.CurrentState;
        var solver = new IntegratedAutomaticOperationSolver(state1.ReactorPrimaryControlState.Definition,
            state1.TurbineSecondaryControlState.Definition, state1.ProtectionState.Definition, state1.AlarmState.Definition,
            new SimplifiedWaterSteamThermodynamicModel(WaterSteamThermodynamicClosureMode.ReferenceConsistentTabulatedInverseDomain),
            InstrumentSignalSourceCatalog.CreateFullPlantCatalog(state1.PlantDefinition));
        var second = solver.Step(state1, first.PersistentInputs, TimeSpan.FromMilliseconds(10));
        var engine = new IntegratedAutomaticOperationRuntimeEngine(solver, second.CandidateState, first.PersistentInputs, second.Snapshot, TimeSpan.FromMilliseconds(10));
        var post = Invoke(observe, harness, ["post-seed", engine, Get(firstObservation, "Nodes")])!;
        var postExtra = Extra(engine, Thermal(first), firstPlant.FluidNodes.ToDictionary(n => n.Id, n => n.Temperature.Kelvins));
        var firstFlow = StageFlow(first);
        var postFlow = StageFlow(engine);
        var beforeRunning = engine.CurrentState.PlantState.PlantState;
        var beforeThermal = Thermal(engine);
        engine.Step(ControlRoomRunState.Running);
        var running = Invoke(observe, harness, ["1", engine, Get(post, "Nodes")])!;
        var runningExtra = Extra(engine, beforeThermal, beforeRunning.FluidNodes.ToDictionary(n => n.Id, n => n.Temperature.Kelvins));
        object[] observations = [Observation(firstObservation, firstExtra, firstFlow), Observation(post, postExtra, postFlow),
            Observation(running, runningExtra, StageFlow(engine))];
        var residuals = new[] { firstObservation, post, running }.SelectMany(o => (double[])Get(o, "ScaledResiduals")).ToArray();
        var phases = raw.Cast<object>().Select(n => (string)Get(n, "Phase")).Concat(new[] { firstObservation, post, running }
            .SelectMany(o => ((Array)Get(o, "Nodes")).Cast<object>().Select(n => (string)Get(n, "Phase")))).ToArray();
        writer.WriteLine(JsonSerializer.Serialize(new
        {
            request.Id, request.Coordinates, request.OperatingPointFractions,
            Reason = new[] { firstObservation, post, running }.All(o => (bool)Get(o, "Healthy")) ? "VALID" : "HEALTH-SENTINEL",
            Raw = raw, Residuals = residuals, Phases = phases, Observations = observations
        }));
    }
    catch (Exception ex) when (ex is Rejected or WaterSteamStateOutOfRangeException or ArgumentOutOfRangeException or ArithmeticException)
    {
        var reason = ex switch
        {
            Rejected => ex.Message,
            WaterSteamStateOutOfRangeException => "THERMODYNAMIC-DOMAIN/" + ex.Message,
            ArgumentOutOfRangeException => "QUANTITY-DOMAIN/" + ex.Message,
            _ => "NUMERICAL-REJECTION/" + ex.Message
        };
        writer.WriteLine(JsonSerializer.Serialize(new { request.Id, request.Coordinates, request.OperatingPointFractions,
            Reason = reason, Residuals = (double[]?)null }));
    }
}
Console.WriteLine($"{count} recipes; maximum {steps} runtime steps including setup; no search or activation.");

static object Get(object owner, string property) => owner.GetType().GetProperty(property,
    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(owner)!;
static object? Invoke(MethodInfo method, object? target, object?[] parameters)
{
    try { return method.Invoke(target, parameters); }
    catch (TargetInvocationException ex) when (ex.InnerException is not null)
    {
        ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
        throw;
    }
}
static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));
static IntegratedAutomaticOperationSnapshot Canonical(IntegratedAutomaticOperationRuntimeEngine engine)
    => (IntegratedAutomaticOperationSnapshot)Get(engine, "LatestCanonicalSnapshot");
static Dictionary<string, ThermalPoint> Thermal(IntegratedAutomaticOperationRuntimeEngine engine)
    => engine.CurrentState.PlantState.PlantState.ThermalBodies.ToDictionary(b => b.Id,
        b => new ThermalPoint(b.Id, b.StoredThermalEnergy.Joules, b.Temperature.Kelvins), StringComparer.Ordinal);
static object Observation(object observation, object extra, object stageFlow)
{
    var properties = observation.GetType().GetProperties().Where(p => p.Name != "StateJson")
        .ToDictionary(p => p.Name, p => p.GetValue(observation));
    properties.Add("StateSha256", Hash(Encoding.UTF8.GetBytes((string)Get(observation, "StateJson"))));
    properties.Add("Extra", extra);
    // Additive observation only: every other property is byte-identical to R3ProbeRunner records.
    properties.Add("StageFlow", stageFlow);
    return properties;
}
// Reads the step's own snapshot. The main-steam valve snapshots and the stage-flow resolver both evaluate the
// protected/faulted commanded plant state of the same step, so these are the effective (rate-limited) positions.
static object StageFlow(IntegratedAutomaticOperationRuntimeEngine engine)
{
    var control = Canonical(engine).Control.ProtectedControl;
    var turbine = control.FullPlant.IntegratedCycle.TurbineExpansion;
    var stage = turbine.StageGroups.Single();
    var definition = turbine.Definition.StageGroups.Single();
    var train = turbine.MainSteamNetwork.AdmissionTrains.Single();
    var committed = engine.CurrentState.PlantState.PlantState;
    object Valve(MainSteamValveSnapshot valve)
    {
        var state = committed.GetValve(valve.ValveId);
        return new
        {
            valve.ValveId,
            EffectivePosition = valve.EffectivePosition.Fraction,
            FlowCoefficient = valve.FlowCoefficient.Fraction,
            PressureDifferencePa = valve.PressureDifference.Pascals,
            MassFlowKgS = valve.MassFlowRate.KilogramsPerSecond,
            ResistancePaS2Kg2 = committed.Definition.GetValve(valve.ValveId).Pipe.Resistance.PascalSecondsSquaredPerKilogramSquared,
            CommittedPosition = state.Position.Fraction,
            state.IsFailSafeActive,
            RequestedPositions = control.TurbineSecondary.ControlAndActuator.ActuatorCommands.ValveCommands
                .Where(c => c.ValveId == valve.ValveId).Select(c => new { c.ActuatorId, Requested = c.RequestedPosition.Fraction })
        };
    }
    return new
    {
        train.TrainId,
        Stop = Valve(train.StopValve),
        Control = Valve(train.ControlValve),
        Admission = Valve(train.AdmissionValve),
        StopToControlContinuityKgS = train.StopToControlContinuityResidual.KilogramsPerSecond,
        ControlToAdmissionContinuityKgS = train.ControlToAdmissionContinuityResidual.KilogramsPerSecond,
        stage.InletNodeId,
        stage.ExhaustNodeId,
        CommandedMassFlowKgS = stage.CommandedMassFlowRate.KilogramsPerSecond,
        EffectiveMassFlowKgS = stage.EffectiveMassFlowRate.KilogramsPerSecond,
        InletPressurePa = stage.InletPressure.Pascals,
        ExhaustPressurePa = stage.ExhaustPressure.Pascals,
        InletPhase = stage.InletPhase.ToString(),
        InletQuality = stage.InletVaporQuality?.Fraction,
        ExpansionResistancePaS2Kg2 = definition.ExpansionResistance?.PascalSecondsSquaredPerKilogramSquared,
        AdmissionPhasePolicy = definition.AdmissionPhasePolicy.ToString(),
        stage.TripBlocked,
        control.Arbitration.TurbineAdmissionOpeningInhibited,
        control.Arbitration.StopValvesForcedClosed
    };
}
static object Extra(IntegratedAutomaticOperationRuntimeEngine engine, Dictionary<string, ThermalPoint> previous, Dictionary<string, double> fluidTemperatures)
{
    var cycle = Canonical(engine).Control.ProtectedControl.FullPlant.IntegratedCycle;
    var plant = engine.CurrentState.PlantState.PlantState;
    double TemperatureAt(string id) => fluidTemperatures.TryGetValue(id, out var t) ? t : previous[id].TemperatureK;
    var stage = cycle.TurbineExpansion.StageGroups.Single();
    var condenser = cycle.Condenser.Condensers.Single();
    var rotor = cycle.TurbineExpansion.Rotors.Single();
    var generator = cycle.Generators.Single();
    var governor = Canonical(engine).Control.ProtectedControl.TurbineSecondary.ControlAndActuator.Controllers.GetDiagnostic("speed-control");
    return new
    {
        ThermalBefore = previous.Values.OrderBy(b => b.Id, StringComparer.Ordinal),
        ThermalAfter = Thermal(engine).Values.OrderBy(b => b.Id, StringComparer.Ordinal),
        HeatTransfers = plant.Definition.HeatTransfers.Select(h => new
        {
            h.Id, h.FromDomainId, h.ToDomainId, ConductanceWK = h.Conductance.WattsPerKelvin,
            FromK = TemperatureAt(h.FromDomainId), ToK = TemperatureAt(h.ToDomainId)
        }),
        ChannelHeat = cycle.PrimaryCircuit.ChannelGroups.Groups.Select(g => new
        {
            g.GroupId, NuclearW = g.TotalNuclearHeatPower.Watts, FuelW = g.FuelHeatPower.Watts,
            StructureW = g.StructureHeatPower.Watts, CoolantW = g.CoolantHeatPower.Watts
        }),
        HydraulicNumerics = cycle.PrimaryCircuit.HydraulicNumerics,
        Mechanical = new
        {
            TurbineTorqueNm = rotor.TurbineTorque.NewtonMetres,
            ExternalLoadTorqueNm = rotor.EffectiveExternalLoadTorque.NewtonMetres,
            LossTorqueNm = rotor.PassiveMechanicalLossTorque.NewtonMetres,
            NetTorqueNm = rotor.NetTorque.NewtonMetres,
            InitialOmega = rotor.InitialAngularSpeed.RadiansPerSecond,
            FinalOmega = rotor.FinalAngularSpeed.RadiansPerSecond,
            RatedOmega = rotor.RatedAngularSpeed.RadiansPerSecond,
            InertiaKgM2 = rotor.MomentOfInertia.KilogramSquareMetres,
            GeneratorTorqueNm = generator.EffectiveElectromagneticTorque.NewtonMetres,
            StageIdealWorkJKg = stage.EffectiveIdealSpecificWork.JoulesPerKilogram,
            StageEfficiency = cycle.TurbineExpansion.Definition.StageGroups.Single().Efficiency.Fraction,
            stage.TripBlocked, stage.ThermodynamicWorkLimited,
            TorqueReferenceAboveRated = rotor.InitialAngularSpeed > rotor.RatedAngularSpeed,
            rotor.ExternalLoadTorqueLimitedAtZeroSpeed, rotor.PassiveMechanicalLossTorqueLimitedAtZeroSpeed,
            Governor = governor
        },
        Stage = new
        {
            VaporMassKgS = stage.EffectiveMassFlowRate.KilogramsPerSecond,
            ExhaustEnergyW = stage.ExhaustEnergyFlowRate.Watts,
            stage.ExhaustNodeId
        },
        Condenser = new
        {
            condenser.SteamSpaceNodeId, MassKgS = condenser.ActualCondensationMassFlowRate.KilogramsPerSecond,
            SteamEnergyW = condenser.SteamEnergyRemovalRate.Watts,
            SteamAdvectedJKg = condenser.SteamAdvectedSpecificEnergy.JoulesPerKilogram,
            CondensateAdvectedJKg = condenser.CondensateAdvectedSpecificEnergy.JoulesPerKilogram,
            HeatRejectionW = condenser.HeatRejectionPower.Watts,
            EffectiveCoolingW = condenser.EffectiveHeatRejectionCapacity.Watts,
            condenser.ActiveCondensationLimits,
            CoolingActiveLimits = cycle.Condenser.CoolingBoundaries.Single().ActiveHeatRejectionLimits
        },
        BypassMassKgS = cycle.Condenser.TotalTurbineBypassMassFlowRate.KilogramsPerSecond,
        BypassEnergyW = cycle.Condenser.TotalTurbineBypassAdvectedEnergyTransferRate.Watts
    };
}
internal sealed record Request(string Id, double[] Coordinates, double[] OperatingPointFractions);
internal sealed record ThermalPoint(string Id, double EnergyJ, double TemperatureK);
internal sealed class Rejected(string reason) : Exception(reason);
