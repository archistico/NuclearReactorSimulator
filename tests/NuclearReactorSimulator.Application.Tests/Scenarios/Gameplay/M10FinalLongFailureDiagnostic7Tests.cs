using System.Globalization;
using System.Text;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios.Training;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay;

/// <summary>
/// M10 final LR-H1 Diagnostic 7. Exact-v6 closes the authored whole-cycle equations at t=0 but the
/// returned 600 s evidence still drifts. This diagnostic freezes the governor/droop and steam-path
/// ownership chain before any exact-v7 seed is authored.
/// </summary>
public sealed class M10FinalLongFailureDiagnostic7Tests
{
    private const string OptInEnvironmentVariable = "NRS_M10_FINAL_LONG_DIAGNOSTIC7";
    private const int StepsPerSecond = 100;
    private const int SampleStrideSteps = 10;
    private const int TotalSteps = 18_000;
    private static readonly UTF8Encoding Utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);

    [Fact(Explicit = true)]
    [Trait("Category", "M10FinalLongDiagnostic7")]
    public void LR_H1_ExactV6_GovernorDroopSteamPathOwnerCensus()
    {
        RequireOptIn();
        ResetDirectory(ReportDirectory());

        var engine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            new DesktopSustainedGenerationWholeCycleEquilibriumCandidateInitialConditionFactory().CreateRuntimeEngine());
        var rows = new List<Sample>();
        var trips = 0;
        var rollbacks = 0;

        Capture(engine, 0, rows);
        for (var step = 1; step <= TotalSteps; step++)
        {
            var presentation = engine.Step(ControlRoomRunState.Running);
            if (presentation.AnyTripActive)
            {
                trips++;
            }

            var telemetry = engine.LatestCanonicalSnapshot.Control.ProtectedControl.FullPlant
                .IntegratedCycle.PrimaryCircuit.HydraulicNumerics.FourNodeBranchContinuity;
            if (telemetry?.RollbackRequired == true)
            {
                rollbacks++;
            }

            if (step % SampleStrideSteps == 0)
            {
                Capture(engine, step, rows);
            }

            if (step % 3000 == 0)
            {
                AppendProgress($"LR-H1 diagnostic7 exact-v6 governor/steam-path census simulated-seconds={step / StepsPerSecond}; logical-step={step}");
            }
        }

        WriteTrajectory(rows);
        WriteSummary(rows, trips, rollbacks);

        Assert.Equal(TotalSteps, engine.LogicalStep);
        Assert.Equal(0, trips);
        Assert.Equal(0, rollbacks);
        Assert.All(rows, static item => Assert.True(item.AllFinite));
    }

    private static void Capture(IntegratedAutomaticOperationRuntimeEngine engine, int logicalStep, List<Sample> rows)
    {
        var protectedControl = engine.LatestCanonicalSnapshot.Control.ProtectedControl;
        var fullPlant = protectedControl.FullPlant;
        var cycle = fullPlant.IntegratedCycle;
        var turbine = cycle.TurbineExpansion;
        var network = turbine.MainSteamNetwork;
        var line = Assert.Single(network.SteamLines);
        var train = Assert.Single(network.AdmissionTrains);
        var stage = Assert.Single(turbine.StageGroups);
        var rotor = Assert.Single(turbine.Rotors);
        var generator = Assert.Single(cycle.Generators);
        var drum = Assert.Single(cycle.PrimaryCircuit.SteamDrums.Drums);
        var speed = protectedControl.TurbineSecondary.ControlAndActuator.Controllers.GetDiagnostic("speed-control");
        var plant = fullPlant.CandidatePlant;

        rows.Add(new Sample(
            logicalStep,
            logicalStep / (double)StepsPerSecond,
            speed.Setpoint,
            speed.Measurement ?? double.NaN,
            speed.Error,
            speed.ProportionalTerm,
            speed.IntegralTerm,
            speed.DerivativeTerm,
            speed.Output,
            100d * train.ControlValve.EffectivePosition.Fraction,
            rotor.FinalAngularSpeed.RevolutionsPerMinute,
            generator.FinalElectricalFrequency.Hertz,
            generator.FinalPhaseDifference.Radians,
            generator.RequestedElectricalPower.Megawatts,
            generator.MechanicalInputPower.Megawatts,
            generator.ElectricalOutputPower.Megawatts,
            line.MassFlowRate.KilogramsPerSecond,
            train.StopValve.MassFlowRate.KilogramsPerSecond,
            train.ControlValve.MassFlowRate.KilogramsPerSecond,
            train.AdmissionValve.MassFlowRate.KilogramsPerSecond,
            stage.CommandedMassFlowRate.KilogramsPerSecond,
            stage.EffectiveMassFlowRate.KilogramsPerSecond,
            stage.ShaftPower.Megawatts,
            drum.SeparatedSteamMassFlowRate.KilogramsPerSecond,
            plant.GetFluidNode("steam").Mass.Kilograms,
            plant.GetFluidNode("header").Mass.Kilograms,
            plant.GetFluidNode("stop-out").Mass.Kilograms,
            plant.GetFluidNode("control-out").Mass.Kilograms,
            plant.GetFluidNode("turbine-inlet").Mass.Kilograms,
            plant.GetFluidNode("steam").Pressure.Pascals,
            plant.GetFluidNode("header").Pressure.Pascals,
            plant.GetFluidNode("stop-out").Pressure.Pascals,
            plant.GetFluidNode("control-out").Pressure.Pascals,
            plant.GetFluidNode("turbine-inlet").Pressure.Pascals));
    }

    private static void WriteTrajectory(IReadOnlyList<Sample> rows)
    {
        var lines = new List<string>
        {
            "logical_step,simulated_seconds,governor_setpoint_rpm,governor_measurement_rpm,governor_error_rpm,governor_p,governor_i,governor_d,governor_output_percent,control_valve_position_percent,rotor_rpm,generator_frequency_hz,generator_phase_difference_rad,requested_electrical_mw,generator_mechanical_input_mw,electrical_output_mw,main_steam_line_kg_s,stop_valve_kg_s,control_valve_kg_s,admission_valve_kg_s,stage_commanded_kg_s,stage_effective_kg_s,stage_shaft_mw,separated_steam_kg_s,steam_mass_kg,header_mass_kg,stop_out_mass_kg,control_out_mass_kg,turbine_inlet_mass_kg,steam_pressure_pa,header_pressure_pa,stop_out_pressure_pa,control_out_pressure_pa,turbine_inlet_pressure_pa"
        };

        lines.AddRange(rows.Select(static item => string.Join(",",
            item.LogicalStep,
            F(item.SimulatedSeconds),
            F(item.GovernorSetpointRpm), F(item.GovernorMeasurementRpm), F(item.GovernorErrorRpm),
            F(item.GovernorProportional), F(item.GovernorIntegral), F(item.GovernorDerivative), F(item.GovernorOutputPercent),
            F(item.ControlValvePositionPercent), F(item.RotorRpm), F(item.GeneratorFrequencyHertz), F(item.GeneratorPhaseDifferenceRadians),
            F(item.RequestedElectricalMegawatts), F(item.GeneratorMechanicalInputMegawatts), F(item.ElectricalOutputMegawatts),
            F(item.MainSteamLineKilogramsPerSecond), F(item.StopValveKilogramsPerSecond), F(item.ControlValveKilogramsPerSecond),
            F(item.AdmissionValveKilogramsPerSecond), F(item.StageCommandedKilogramsPerSecond), F(item.StageEffectiveKilogramsPerSecond),
            F(item.StageShaftMegawatts), F(item.SeparatedSteamKilogramsPerSecond),
            F(item.SteamMassKilograms), F(item.HeaderMassKilograms), F(item.StopOutMassKilograms), F(item.ControlOutMassKilograms), F(item.TurbineInletMassKilograms),
            F(item.SteamPressurePascals), F(item.HeaderPressurePascals), F(item.StopOutPressurePascals), F(item.ControlOutPressurePascals), F(item.TurbineInletPressurePascals))));

        File.WriteAllLines(Path.Combine(ReportDirectory(), "80-v6-governor-steam-path-trajectory.csv"), lines, Utf8WithoutBom);
    }

    private static void WriteSummary(IReadOnlyList<Sample> rows, int trips, int rollbacks)
    {
        var initial = rows[0];
        var ten = Closest(rows, 10d);
        var sixty = Closest(rows, 60d);
        var final = rows[^1];
        var late = rows.Where(static item => item.SimulatedSeconds >= 120d).ToArray();

        File.WriteAllLines(Path.Combine(ReportDirectory(), "81-v6-governor-steam-path-summary.txt"), new[]
        {
            "scope=Diagnostic 7 exact-v6 governor/droop + steam-path owner census; Diagnostic 6 execution PASS but engineering NOT QUALIFIED; exact-v4 production selector unchanged; no exact-v7 exists; replacement long unauthorized;",
            "code-owner-observation=requested 5 MWe with 10 MWe generator and 1.5 rpm full-load droop implies effective automatic governor setpoint 3000.75 rpm; exact-v6 authored rotor seed is 3000 rpm; returned Diagnostic 6 electrical export moved 4.9986->5.2015 MWe while steam-path inventories redistributed;",
            FormatPoint("initial", initial),
            FormatPoint("10s", ten),
            FormatPoint("60s", sixty),
            FormatPoint("final", final),
            FormattableString.Invariant($"late-120-180-slopes=governor-output-percent-s:{Slope(late, static x => x.GovernorOutputPercent):G17}|control-valve-percent-s:{Slope(late, static x => x.ControlValvePositionPercent):G17}|rotor-rpm-s:{Slope(late, static x => x.RotorRpm):G17}|electrical-mw-s:{Slope(late, static x => x.ElectricalOutputMegawatts):G17}|steam-mass-kg-s:{Slope(late, static x => x.SteamMassKilograms):G17}|header-mass-kg-s:{Slope(late, static x => x.HeaderMassKilograms):G17}|stop-out-mass-kg-s:{Slope(late, static x => x.StopOutMassKilograms):G17}|control-out-mass-kg-s:{Slope(late, static x => x.ControlOutMassKilograms):G17}|turbine-inlet-mass-kg-s:{Slope(late, static x => x.TurbineInletMassKilograms):G17};"),
            FormattableString.Invariant($"late-120-180-flow-means=steam-line:{late.Average(static x => x.MainSteamLineKilogramsPerSecond):G17}|stop:{late.Average(static x => x.StopValveKilogramsPerSecond):G17}|control:{late.Average(static x => x.ControlValveKilogramsPerSecond):G17}|admission:{late.Average(static x => x.AdmissionValveKilogramsPerSecond):G17}|stage-effective:{late.Average(static x => x.StageEffectiveKilogramsPerSecond):G17}|separated-steam:{late.Average(static x => x.SeparatedSteamKilogramsPerSecond):G17} kg/s;"),
            FormattableString.Invariant($"trip-steps={trips}; rollbacks={rollbacks};"),
            "decision-rule=if governor setpoint/error/integral drives the control valve away from the exact-v6 authored 27.3123% and the measured valve-flow divergence tracks steam-path inventory transfer, classify the residual exact-v6 drift as governor-operating-point seed mismatch and author a separate exact-v7 that includes the coupled governor/generator state; otherwise continue owner diagnosis without retuning controller gains;",
        }, Utf8WithoutBom);
    }

    private static string FormatPoint(string label, Sample item)
        => FormattableString.Invariant(
            $"{label}=t:{item.SimulatedSeconds:G17}s|gov-sp:{item.GovernorSetpointRpm:G17}|gov-meas:{item.GovernorMeasurementRpm:G17}|gov-error:{item.GovernorErrorRpm:G17}|gov-I:{item.GovernorIntegral:G17}|gov-output:{item.GovernorOutputPercent:G17}%|control-pos:{item.ControlValvePositionPercent:G17}%|rotor:{item.RotorRpm:G17}rpm|electrical:{item.ElectricalOutputMegawatts:G17}MW|line/stop/control/admission/stage:{item.MainSteamLineKilogramsPerSecond:G17}/{item.StopValveKilogramsPerSecond:G17}/{item.ControlValveKilogramsPerSecond:G17}/{item.AdmissionValveKilogramsPerSecond:G17}/{item.StageEffectiveKilogramsPerSecond:G17}kg/s;");

    private static Sample Closest(IReadOnlyList<Sample> rows, double seconds)
        => rows.OrderBy(item => Math.Abs(item.SimulatedSeconds - seconds)).First();

    private static double Slope(IReadOnlyList<Sample> rows, Func<Sample, double> selector)
    {
        var meanX = rows.Average(static item => item.SimulatedSeconds);
        var meanY = rows.Average(selector);
        var numerator = 0d;
        var denominator = 0d;
        foreach (var row in rows)
        {
            var dx = row.SimulatedSeconds - meanX;
            numerator += dx * (selector(row) - meanY);
            denominator += dx * dx;
        }
        return denominator == 0d ? 0d : numerator / denominator;
    }

    private static string F(double value) => value.ToString("G17", CultureInfo.InvariantCulture);

    private static void RequireOptIn()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable(OptInEnvironmentVariable), "1", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Set {OptInEnvironmentVariable}=1 to run M10 final long Diagnostic 7.");
        }
    }

    private static string ReportDirectory()
        => Path.Combine(FindRepositoryRoot(), "artifacts", "m10-final-long-diagnostic7");

    private static void AppendProgress(string message)
    {
        Directory.CreateDirectory(ReportDirectory());
        File.AppendAllText(Path.Combine(ReportDirectory(), "00-progress.txt"), $"{DateTimeOffset.UtcNow:O} {message}{Environment.NewLine}", Utf8WithoutBom);
    }

    private static void ResetDirectory(string directory)
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "00-progress.txt"), $"M10 FINAL LONG FAILURE DIAGNOSTIC 7 STARTED{Environment.NewLine}", Utf8WithoutBom);
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "NuclearReactorSimulator.sln")))
            {
                return current.FullName;
            }
            current = current.Parent;
        }
        throw new InvalidOperationException("Could not locate repository root from test output directory.");
    }

    private sealed record Sample(
        int LogicalStep,
        double SimulatedSeconds,
        double GovernorSetpointRpm,
        double GovernorMeasurementRpm,
        double GovernorErrorRpm,
        double GovernorProportional,
        double GovernorIntegral,
        double GovernorDerivative,
        double GovernorOutputPercent,
        double ControlValvePositionPercent,
        double RotorRpm,
        double GeneratorFrequencyHertz,
        double GeneratorPhaseDifferenceRadians,
        double RequestedElectricalMegawatts,
        double GeneratorMechanicalInputMegawatts,
        double ElectricalOutputMegawatts,
        double MainSteamLineKilogramsPerSecond,
        double StopValveKilogramsPerSecond,
        double ControlValveKilogramsPerSecond,
        double AdmissionValveKilogramsPerSecond,
        double StageCommandedKilogramsPerSecond,
        double StageEffectiveKilogramsPerSecond,
        double StageShaftMegawatts,
        double SeparatedSteamKilogramsPerSecond,
        double SteamMassKilograms,
        double HeaderMassKilograms,
        double StopOutMassKilograms,
        double ControlOutMassKilograms,
        double TurbineInletMassKilograms,
        double SteamPressurePascals,
        double HeaderPressurePascals,
        double StopOutPressurePascals,
        double ControlOutPressurePascals,
        double TurbineInletPressurePascals)
    {
        public bool AllFinite =>
            double.IsFinite(SimulatedSeconds)
            && double.IsFinite(GovernorSetpointRpm)
            && double.IsFinite(GovernorMeasurementRpm)
            && double.IsFinite(GovernorErrorRpm)
            && double.IsFinite(GovernorProportional)
            && double.IsFinite(GovernorIntegral)
            && double.IsFinite(GovernorDerivative)
            && double.IsFinite(GovernorOutputPercent)
            && double.IsFinite(ControlValvePositionPercent)
            && double.IsFinite(RotorRpm)
            && double.IsFinite(GeneratorFrequencyHertz)
            && double.IsFinite(GeneratorPhaseDifferenceRadians)
            && double.IsFinite(RequestedElectricalMegawatts)
            && double.IsFinite(GeneratorMechanicalInputMegawatts)
            && double.IsFinite(ElectricalOutputMegawatts)
            && double.IsFinite(MainSteamLineKilogramsPerSecond)
            && double.IsFinite(StopValveKilogramsPerSecond)
            && double.IsFinite(ControlValveKilogramsPerSecond)
            && double.IsFinite(AdmissionValveKilogramsPerSecond)
            && double.IsFinite(StageCommandedKilogramsPerSecond)
            && double.IsFinite(StageEffectiveKilogramsPerSecond)
            && double.IsFinite(StageShaftMegawatts)
            && double.IsFinite(SeparatedSteamKilogramsPerSecond)
            && double.IsFinite(SteamMassKilograms)
            && double.IsFinite(HeaderMassKilograms)
            && double.IsFinite(StopOutMassKilograms)
            && double.IsFinite(ControlOutMassKilograms)
            && double.IsFinite(TurbineInletMassKilograms)
            && double.IsFinite(SteamPressurePascals)
            && double.IsFinite(HeaderPressurePascals)
            && double.IsFinite(StopOutPressurePascals)
            && double.IsFinite(ControlOutPressurePascals)
            && double.IsFinite(TurbineInletPressurePascals);
    }
}
