using System.Globalization;
using System.Text;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios.Training;
using NuclearReactorSimulator.Simulation.Plant;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay;

/// <summary>
/// Test-only diagnostic for the returned Seed Integration Implementation 1 fast-gate RED.
/// Runs canonical exact-v9 mode 1 and the new reference-consistent mode-2 candidate side by side
/// from their post-seed snapshots through the first 100 Running steps. No production mutation,
/// threshold change, or R3 qualification is performed by this diagnostic.
/// </summary>
public sealed class M10FinalVr2R3ReferenceConsistentSeedIntegrationFastGateDynamicEquilibriumDiagnostic1Tests
{
    private const string OptInEnvironmentVariable = "NRS_M10_FINAL_VR2_R3_SEED_FAST_DYNAMIC_DIAGNOSTIC1";
    private static readonly TimeSpan RuntimeStep = TimeSpan.FromMilliseconds(10d);
    private const int DynamicSteps = 100;
    private static readonly UTF8Encoding Utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);

    [Fact(Explicit = true)]
    [Trait("Category", "M10FinalVr2R3ReferenceConsistentSeedIntegrationFastGateDynamicEquilibriumDiagnostic1")]
    public void ReturnedFastGateRed_LocalizesPostSeedDynamicEquilibriumDivergence()
    {
        RequireOptIn();
        ResetArtifactDirectory();

        var mode1 = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            DesktopSustainedGenerationInitialConditionFactory.CreatePostMoistureEquilibriumCandidateRuntimeEngine(RuntimeStep));
        var candidate = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            DesktopSustainedGenerationInitialConditionFactory.CreateReferenceConsistentPostMoistureEquilibriumCandidateRuntimeEngine(RuntimeStep));

        var postSeedRows = ComparePostSeedNodes(mode1, candidate);
        WritePostSeedComparison(postSeedRows);

        var dynamicRows = RunDynamicComparison(mode1, candidate);
        WriteDynamicComparison(dynamicRows);
        WriteHeadComparison(dynamicRows);
        WriteControllerTurbineComparison(dynamicRows);

        Assert.Equal(12, postSeedRows.Count);
        Assert.Equal(DynamicSteps, dynamicRows.Count);
        Assert.All(dynamicRows, static row => Assert.True(row.CandidateFinite));
        Assert.All(dynamicRows, static row => Assert.True(row.Mode1Finite));
    }

    private static IReadOnlyList<PostSeedRow> ComparePostSeedNodes(
        IntegratedAutomaticOperationRuntimeEngine mode1,
        IntegratedAutomaticOperationRuntimeEngine candidate)
    {
        var left = mode1.LatestCanonicalSnapshot.Control.ProtectedControl.FullPlant.CandidatePlant.FluidNodes
            .ToDictionary(static node => node.Id, StringComparer.Ordinal);
        var right = candidate.LatestCanonicalSnapshot.Control.ProtectedControl.FullPlant.CandidatePlant.FluidNodes
            .ToDictionary(static node => node.Id, StringComparer.Ordinal);
        Assert.Equal(left.Keys.OrderBy(static x => x, StringComparer.Ordinal), right.Keys.OrderBy(static x => x, StringComparer.Ordinal));

        return left.Keys.OrderBy(static x => x, StringComparer.Ordinal).Select(id =>
        {
            var a = left[id];
            var b = right[id];
            return new PostSeedRow(
                id,
                a.Mass.Kilograms, b.Mass.Kilograms, b.Mass.Kilograms - a.Mass.Kilograms,
                a.InternalEnergy.Joules, b.InternalEnergy.Joules, b.InternalEnergy.Joules - a.InternalEnergy.Joules,
                a.Pressure.Pascals, b.Pressure.Pascals, b.Pressure.Pascals - a.Pressure.Pascals,
                a.Temperature.Kelvins, b.Temperature.Kelvins, b.Temperature.Kelvins - a.Temperature.Kelvins,
                a.Phase.ToString(), b.Phase.ToString(),
                a.VaporQuality?.Fraction, b.VaporQuality?.Fraction);
        }).ToArray();
    }

    private static IReadOnlyList<DynamicRow> RunDynamicComparison(
        IntegratedAutomaticOperationRuntimeEngine mode1,
        IntegratedAutomaticOperationRuntimeEngine candidate)
    {
        var rows = new List<DynamicRow>(DynamicSteps);
        for (var step = 1; step <= DynamicSteps; step++)
        {
            var view1 = mode1.Step(ControlRoomRunState.Running);
            var view2 = candidate.Step(ControlRoomRunState.Running);
            var full1 = mode1.LatestCanonicalSnapshot.Control.ProtectedControl.FullPlant;
            var full2 = candidate.LatestCanonicalSnapshot.Control.ProtectedControl.FullPlant;
            var plant1 = full1.CandidatePlant;
            var plant2 = full2.CandidatePlant;
            var gen1 = Assert.Single(view1.Electrical.Generators);
            var gen2 = Assert.Single(view2.Electrical.Generators);
            var drum1 = Assert.Single(full1.IntegratedCycle.PrimaryCircuit.SteamDrums.Drums);
            var drum2 = Assert.Single(full2.IntegratedCycle.PrimaryCircuit.SteamDrums.Drums);
            var stage1 = Assert.Single(full1.IntegratedCycle.TurbineExpansion.StageGroups);
            var stage2 = Assert.Single(full2.IntegratedCycle.TurbineExpansion.StageGroups);
            var speed1 = mode1.LatestCanonicalSnapshot.Control.ProtectedControl.TurbineSecondary.ControlAndActuator.Controllers.GetDiagnostic("speed-control");
            var speed2 = candidate.LatestCanonicalSnapshot.Control.ProtectedControl.TurbineSecondary.ControlAndActuator.Controllers.GetDiagnostic("speed-control");
            var circ1 = full1.IntegratedCycle.PrimaryCircuit.MainCirculation;
            var circ2 = full2.IntegratedCycle.PrimaryCircuit.MainCirculation;

            var e1 = gen1.ElectricalOutput.NumericValue ?? double.NaN;
            var e2 = gen2.ElectricalOutput.NumericValue ?? double.NaN;
            var p1 = circ1.TotalPumpMassFlowRate.KilogramsPerSecond;
            var p2 = circ2.TotalPumpMassFlowRate.KilogramsPerSecond;
            var d1 = drum1.LiquidLevelFraction.Fraction;
            var d2 = drum2.LiquidLevelFraction.Fraction;
            var g1 = speed1.Output;
            var g2 = speed2.Output;

            rows.Add(new DynamicRow(
                step, step * RuntimeStep.TotalSeconds,
                e1, e2, p1, p2,
                circ1.TotalChannelMassFlowRate.KilogramsPerSecond, circ2.TotalChannelMassFlowRate.KilogramsPerSecond,
                circ1.TotalReturnMassFlowRate.KilogramsPerSecond, circ2.TotalReturnMassFlowRate.KilogramsPerSecond,
                d1, d2, g1, g2,
                speed1.Setpoint, speed2.Setpoint,
                speed1.Measurement, speed2.Measurement,
                speed1.Error, speed2.Error,
                speed1.IntegralTerm, speed2.IntegralTerm,
                Pressure(plant1,"suction"), Pressure(plant2,"suction"),
                Pressure(plant1,"pressure"), Pressure(plant2,"pressure"),
                Pressure(plant1,"outlet"), Pressure(plant2,"outlet"),
                Pressure(plant1,"drum"), Pressure(plant2,"drum"),
                Pressure(plant1,"steam"), Pressure(plant2,"steam"),
                Pressure(plant1,"header"), Pressure(plant2,"header"),
                Pressure(plant1,"stop-out"), Pressure(plant2,"stop-out"),
                Pressure(plant1,"control-out"), Pressure(plant2,"control-out"),
                Pressure(plant1,"turbine-inlet"), Pressure(plant2,"turbine-inlet"),
                Pressure(plant1,"exhaust"), Pressure(plant2,"exhaust"),
                stage1.CommandedMassFlowRate.KilogramsPerSecond, stage2.CommandedMassFlowRate.KilogramsPerSecond,
                stage1.TotalTransferredMassFlowRate.KilogramsPerSecond, stage2.TotalTransferredMassFlowRate.KilogramsPerSecond,
                stage1.MoistureDrainMassFlowRate.KilogramsPerSecond, stage2.MoistureDrainMassFlowRate.KilogramsPerSecond,
                stage1.ShaftPower.Watts, stage2.ShaftPower.Watts,
                IsFinite(e1,p1,d1,g1,speed1.Error,speed1.IntegralTerm),
                IsFinite(e2,p2,d2,g2,speed2.Error,speed2.IntegralTerm),
                InEnvelope(e1,p1,d1,g1) && !view1.AnyTripActive && gen1.BreakerClosed,
                InEnvelope(e2,p2,d2,g2) && !view2.AnyTripActive && gen2.BreakerClosed));
        }
        return rows;
    }

    private static double Pressure(PlantSnapshot plant, string id) => plant.GetFluidNode(id).Pressure.Pascals;
    private static bool IsFinite(params double[] values) => values.All(double.IsFinite);
    private static bool InEnvelope(double electrical, double pump, double drum, double governor)
        => electrical >= 4.99d && electrical <= 5.01d
            && pump >= 99.9d && pump <= 100.1d
            && drum >= 0.49d && drum <= 0.51d
            && governor >= 29.27d && governor <= 29.30d;
    private static string F(double v) => v.ToString("R", CultureInfo.InvariantCulture);
    private static string N(double? v) => v?.ToString("R", CultureInfo.InvariantCulture) ?? "null";

    private static void WritePostSeedComparison(IReadOnlyList<PostSeedRow> rows)
    {
        var lines = new List<string>{"node,mode1_mass_kg,candidate_mass_kg,mass_delta_kg,mode1_energy_j,candidate_energy_j,energy_delta_j,mode1_pressure_pa,candidate_pressure_pa,pressure_delta_pa,mode1_temperature_k,candidate_temperature_k,temperature_delta_k,mode1_phase,candidate_phase,mode1_quality,candidate_quality"};
        lines.AddRange(rows.Select(r => string.Join(",", r.Node,F(r.Mode1Mass),F(r.CandidateMass),F(r.MassDelta),F(r.Mode1Energy),F(r.CandidateEnergy),F(r.EnergyDelta),F(r.Mode1Pressure),F(r.CandidatePressure),F(r.PressureDelta),F(r.Mode1Temperature),F(r.CandidateTemperature),F(r.TemperatureDelta),r.Mode1Phase,r.CandidatePhase,N(r.Mode1Quality),N(r.CandidateQuality))));
        File.WriteAllLines(Path.Combine(ArtifactDirectory(),"01-post-seed-node-comparison.csv"),lines,Utf8WithoutBom);
    }

    private static void WriteDynamicComparison(IReadOnlyList<DynamicRow> rows)
    {
        var lines = new List<string>{"step,seconds,mode1_electrical_mwe,candidate_electrical_mwe,mode1_primary_pump_kg_s,candidate_primary_pump_kg_s,mode1_channel_kg_s,candidate_channel_kg_s,mode1_return_kg_s,candidate_return_kg_s,mode1_drum_level,candidate_drum_level,mode1_governor,candidate_governor,mode1_finite,candidate_finite,mode1_envelope,candidate_envelope"};
        lines.AddRange(rows.Select(r=>string.Join(",",r.Step,F(r.Seconds),F(r.Mode1Electrical),F(r.CandidateElectrical),F(r.Mode1Pump),F(r.CandidatePump),F(r.Mode1Channel),F(r.CandidateChannel),F(r.Mode1Return),F(r.CandidateReturn),F(r.Mode1Drum),F(r.CandidateDrum),F(r.Mode1Governor),F(r.CandidateGovernor),r.Mode1Finite.ToString().ToLowerInvariant(),r.CandidateFinite.ToString().ToLowerInvariant(),r.Mode1Envelope.ToString().ToLowerInvariant(),r.CandidateEnvelope.ToString().ToLowerInvariant())));
        File.WriteAllLines(Path.Combine(ArtifactDirectory(),"02-first-100-step-key-dynamics.csv"),lines,Utf8WithoutBom);
    }

    private static void WriteHeadComparison(IReadOnlyList<DynamicRow> rows)
    {
        var lines = new List<string>{"step,seconds,path,mode1_head_pa,candidate_head_pa,head_delta_pa"};
        foreach(var r in rows)
        {
            Add("main-circulation-pump", r.Mode1Suction + 1_000_000d - r.Mode1Pressure, r.CandidateSuction + 1_000_000d - r.CandidatePressure);
            Add("channel", r.Mode1Pressure-r.Mode1Outlet, r.CandidatePressure-r.CandidateOutlet);
            Add("return", r.Mode1Outlet-r.Mode1DrumPressure, r.CandidateOutlet-r.CandidateDrumPressure);
            Add("main-steam-line", r.Mode1Steam-r.Mode1Header, r.CandidateSteam-r.CandidateHeader);
            Add("stop-valve", r.Mode1Header-r.Mode1StopOut, r.CandidateHeader-r.CandidateStopOut);
            Add("control-valve", r.Mode1StopOut-r.Mode1ControlOut, r.CandidateStopOut-r.CandidateControlOut);
            Add("admission-valve", r.Mode1ControlOut-r.Mode1TurbineInlet, r.CandidateControlOut-r.CandidateTurbineInlet);
            Add("turbine-expansion", r.Mode1TurbineInlet-r.Mode1Exhaust, r.CandidateTurbineInlet-r.CandidateExhaust);
            void Add(string path,double a,double b)=>lines.Add(string.Join(",",r.Step,F(r.Seconds),path,F(a),F(b),F(b-a)));
        }
        File.WriteAllLines(Path.Combine(ArtifactDirectory(),"03-first-100-step-hydraulic-heads.csv"),lines,Utf8WithoutBom);
    }

    private static void WriteControllerTurbineComparison(IReadOnlyList<DynamicRow> rows)
    {
        var lines = new List<string>{"step,seconds,mode1_speed_setpoint,candidate_speed_setpoint,mode1_speed_measurement,candidate_speed_measurement,mode1_speed_error,candidate_speed_error,mode1_speed_integral,candidate_speed_integral,mode1_governor,candidate_governor,mode1_stage_command_kg_s,candidate_stage_command_kg_s,mode1_stage_transfer_kg_s,candidate_stage_transfer_kg_s,mode1_moisture_drain_kg_s,candidate_moisture_drain_kg_s,mode1_shaft_power_w,candidate_shaft_power_w"};
        lines.AddRange(rows.Select(r=>string.Join(",",r.Step,F(r.Seconds),F(r.Mode1SpeedSetpoint),F(r.CandidateSpeedSetpoint),N(r.Mode1SpeedMeasurement),N(r.CandidateSpeedMeasurement),F(r.Mode1SpeedError),F(r.CandidateSpeedError),F(r.Mode1SpeedIntegral),F(r.CandidateSpeedIntegral),F(r.Mode1Governor),F(r.CandidateGovernor),F(r.Mode1StageCommand),F(r.CandidateStageCommand),F(r.Mode1StageTransfer),F(r.CandidateStageTransfer),F(r.Mode1Drain),F(r.CandidateDrain),F(r.Mode1ShaftPower),F(r.CandidateShaftPower))));
        File.WriteAllLines(Path.Combine(ArtifactDirectory(),"04-first-100-step-controller-turbine.csv"),lines,Utf8WithoutBom);
    }

    private static void RequireOptIn()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable(OptInEnvironmentVariable),"1",StringComparison.Ordinal))
            throw new InvalidOperationException($"Set {OptInEnvironmentVariable}=1 to run this diagnostic.");
    }
    private static string ArtifactDirectory()
    {
        var p=Path.Combine(FindRepositoryRoot(),"artifacts","m10-final-physical-reference-vr2-r3-reference-consistent-seed-integration-fast-gate-dynamic-equilibrium-diagnostic1");
        Directory.CreateDirectory(p); return p;
    }
    private static void ResetArtifactDirectory()
    {
        var p=ArtifactDirectory(); foreach(var f in Directory.EnumerateFiles(p)) File.Delete(f);
    }
    private static string FindRepositoryRoot()
    {
        var d=new DirectoryInfo(AppContext.BaseDirectory);
        while(d is not null){ if(File.Exists(Path.Combine(d.FullName,"NuclearReactorSimulator.sln"))) return d.FullName; d=d.Parent; }
        throw new DirectoryNotFoundException("Repository root not found.");
    }

    private sealed record PostSeedRow(string Node,double Mode1Mass,double CandidateMass,double MassDelta,double Mode1Energy,double CandidateEnergy,double EnergyDelta,double Mode1Pressure,double CandidatePressure,double PressureDelta,double Mode1Temperature,double CandidateTemperature,double TemperatureDelta,string Mode1Phase,string CandidatePhase,double? Mode1Quality,double? CandidateQuality);
    private sealed record DynamicRow(int Step,double Seconds,double Mode1Electrical,double CandidateElectrical,double Mode1Pump,double CandidatePump,double Mode1Channel,double CandidateChannel,double Mode1Return,double CandidateReturn,double Mode1Drum,double CandidateDrum,double Mode1Governor,double CandidateGovernor,double Mode1SpeedSetpoint,double CandidateSpeedSetpoint,double? Mode1SpeedMeasurement,double? CandidateSpeedMeasurement,double Mode1SpeedError,double CandidateSpeedError,double Mode1SpeedIntegral,double CandidateSpeedIntegral,double Mode1Suction,double CandidateSuction,double Mode1Pressure,double CandidatePressure,double Mode1Outlet,double CandidateOutlet,double Mode1DrumPressure,double CandidateDrumPressure,double Mode1Steam,double CandidateSteam,double Mode1Header,double CandidateHeader,double Mode1StopOut,double CandidateStopOut,double Mode1ControlOut,double CandidateControlOut,double Mode1TurbineInlet,double CandidateTurbineInlet,double Mode1Exhaust,double CandidateExhaust,double Mode1StageCommand,double CandidateStageCommand,double Mode1StageTransfer,double CandidateStageTransfer,double Mode1Drain,double CandidateDrain,double Mode1ShaftPower,double CandidateShaftPower,bool Mode1Finite,bool CandidateFinite,bool Mode1Envelope,bool CandidateEnvelope);
}
