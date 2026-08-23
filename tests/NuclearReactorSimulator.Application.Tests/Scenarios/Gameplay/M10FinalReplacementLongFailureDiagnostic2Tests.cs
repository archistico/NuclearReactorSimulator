using System.Globalization;
using System.Text;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.ControlRoom.Automation;
using NuclearReactorSimulator.Application.Scenarios.Training;
using NuclearReactorSimulator.Domain.Physics.Control;
using NuclearReactorSimulator.Domain.Physics.Control.Supervisory;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay;

/// <summary>
/// Evidence-only discrimination after Replacement-Long Failure Diagnostic 1 proved that generator loss-of-synchronism
/// owns the common RL-M1/RL-R1 trip. The frozen long used SupervisoryAutomatic/HoldCurrentOperatingPoint plus only a
/// generator-load raise/lower policy, while the already validated M7.6 procedure requires coordinated rod withdrawal/HOLD
/// and turbine governing. This audit determines whether the authority mode suppresses that operator coordination and
/// explores a bounded Assisted-authority rod/load matrix without changing production source, protection, exact-v9,
/// mission semantics or the frozen replacement workload.
/// </summary>
public sealed class M10FinalReplacementLongFailureDiagnostic2Tests
{
    private const string OptInEnvironmentVariable = "NRS_M10_FINAL_REPLACEMENT_LONG_DIAGNOSTIC2";
    private const int StepsPerSecond = 100;
    private const int LoadRaiseStep = 500;
    private const int TotalSteps = 1_200;
    private static readonly UTF8Encoding Utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);

    [Fact(Explicit = true)]
    [Trait("Category", "M10FinalReplacementLongFailureDiagnostic2")]
    public void ExactV9_LoadRaiseAuthorityAndM76CoordinationDiscriminationCensus()
    {
        RequireOptIn();
        ResetDirectory(ReportDirectory());

        var probes = new[]
        {
            new ProbeDefinition("frozen-supervisory-load-only", PlantControlAuthorityMode.SupervisoryAutomatic, null, null),
            new ProbeDefinition("supervisory-withdraw-500ms-spanning-load", PlantControlAuthorityMode.SupervisoryAutomatic, 450, 500 + 50),
            new ProbeDefinition("assisted-load-only", PlantControlAuthorityMode.Assisted, null, null),
            new ProbeDefinition("assisted-withdraw-250ms-at-load", PlantControlAuthorityMode.Assisted, 500, 525),
            new ProbeDefinition("assisted-withdraw-500ms-at-load", PlantControlAuthorityMode.Assisted, 500, 550),
            new ProbeDefinition("assisted-withdraw-1000ms-at-load", PlantControlAuthorityMode.Assisted, 500, 600),
            new ProbeDefinition("assisted-prewithdraw-500ms", PlantControlAuthorityMode.Assisted, 450, 500),
            new ProbeDefinition("assisted-withdraw-1000ms-spanning-load", PlantControlAuthorityMode.Assisted, 450, 550),
        };

        var results = new List<ProbeResult>();
        var trajectory = new List<ProbeSample>();
        foreach (var probe in probes)
        {
            AppendProgress($"probe-start={probe.Id}");
            var result = RunProbe(probe, trajectory);
            results.Add(result);
            AppendProgress($"probe-complete={probe.Id}|trip-step={result.FirstTripStep?.ToString(CultureInfo.InvariantCulture) ?? "none"}|first-latch={result.FirstLatchedFunctionId ?? "none"}|rod-delta={F(result.RodWithdrawalDelta)}");
        }

        WriteProbeSummary(results);
        WriteTrajectory(trajectory);
        WriteDecisionSummary(results);

        var frozen = results.Single(static result => result.Id == "frozen-supervisory-load-only");
        Assert.NotNull(frozen.FirstTripStep);
        Assert.Equal(636L, frozen.FirstTripStep);
        Assert.Equal("generator-loss-of-synchronism", frozen.FirstLatchedFunctionId);
        Assert.All(results, static result => Assert.True(result.AllFinite));
        Assert.All(results, static result => Assert.Equal((long)TotalSteps, result.ExecutedSteps));
    }

    private static ProbeResult RunProbe(ProbeDefinition probe, ICollection<ProbeSample> trajectory)
    {
        var engine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            new DesktopSustainedGenerationPostMoistureEquilibriumCandidateInitialConditionFactory().CreateRuntimeEngine());

        if (probe.Authority == PlantControlAuthorityMode.SupervisoryAutomatic)
        {
            engine.RequestSupervisoryObjective(SupervisoryObjectiveRequest.HoldCurrentOperatingPoint());
        }
        engine.RequestPlantControlAuthority(probe.Authority);

        var initial = engine.CreatePresentationSnapshot(ControlRoomRunState.Paused);
        var generatorId = Assert.Single(initial.Electrical.Generators).GeneratorId;
        var initialRod = initial.ReactorCore.AverageRodWithdrawal.NumericValue ?? double.NaN;
        long? firstTripStep = null;
        string? firstLatchedFunctionId = null;
        long? firstLatchedFunctionStep = null;
        double minimumFrequency = double.PositiveInfinity;
        double maximumFrequency = double.NegativeInfinity;
        double maximumThermalPower = double.NegativeInfinity;
        double maximumElectricalOutput = double.NegativeInfinity;
        double maximumAbsolutePhaseDifference = 0d;
        var allFinite = true;

        Capture(probe.Id, engine, trajectory);
        for (var nextStep = 1; nextStep <= TotalSteps; nextStep++)
        {
            if (probe.RodWithdrawStep == nextStep)
            {
                engine.QueueOperatorCommand(new ControlRoomCommand(
                    ControlRoomCommandKind.ControlRodWithdraw,
                    "regulating",
                    ControlRoomCommandTargetKind.ControlRodGroup));
            }
            if (nextStep == LoadRaiseStep)
            {
                engine.QueueOperatorCommand(new ControlRoomCommand(
                    ControlRoomCommandKind.GeneratorLoadRaise,
                    generatorId,
                    ControlRoomCommandTargetKind.Generator));
            }
            if (probe.RodHoldStep == nextStep)
            {
                engine.QueueOperatorCommand(new ControlRoomCommand(
                    ControlRoomCommandKind.ControlRodHold,
                    "regulating",
                    ControlRoomCommandTargetKind.ControlRodGroup));
            }

            engine.Step(ControlRoomRunState.Running);
            var sample = Capture(probe.Id, engine, trajectory);
            allFinite &= sample.AllFinite;
            minimumFrequency = Math.Min(minimumFrequency, sample.GeneratorFrequencyHertz);
            maximumFrequency = Math.Max(maximumFrequency, sample.GeneratorFrequencyHertz);
            maximumThermalPower = Math.Max(maximumThermalPower, sample.ReactorThermalMegawatts);
            maximumElectricalOutput = Math.Max(maximumElectricalOutput, sample.ElectricalOutputMegawatts);
            maximumAbsolutePhaseDifference = Math.Max(maximumAbsolutePhaseDifference, Math.Abs(sample.GeneratorPhaseDifferenceRadians));
            if (sample.AnyTripActive)
            {
                firstTripStep ??= sample.LogicalStep;
            }

            if (firstLatchedFunctionId is null)
            {
                var protection = engine.LatestCanonicalSnapshot.Control.ProtectedControl.Protection;
                var firstLatch = protection.Functions.FirstOrDefault(static function => function.IsLatched);
                if (firstLatch is not null)
                {
                    firstLatchedFunctionId = firstLatch.FunctionId;
                    firstLatchedFunctionStep = sample.LogicalStep;
                }
            }
        }

        var final = engine.CreatePresentationSnapshot(ControlRoomRunState.Paused);
        var finalGenerator = Assert.Single(final.Electrical.Generators);
        var finalRod = final.ReactorCore.AverageRodWithdrawal.NumericValue ?? double.NaN;
        var automation = engine.CreateAutomationSnapshot();
        var powerController = automation.ControllerModes.Single(static item => item.ControllerId == "power-control");
        var speedController = automation.ControllerModes.Single(static item => item.ControllerId == "speed-control");
        var lateRows = trajectory.Where(row => string.Equals(row.ProbeId, probe.Id, StringComparison.Ordinal) && row.LogicalStep >= TotalSteps - StepsPerSecond).ToArray();
        var stableTenMweLate = firstTripStep is null
            && finalGenerator.BreakerClosed
            && lateRows.Length == StepsPerSecond + 1
            && lateRows.All(static row => row.ElectricalOutputMegawatts is >= 9.5d and <= 10.5d)
            && lateRows.All(static row => row.GeneratorFrequencyHertz is >= 49d and <= 51d);
        var survivesTwoSecondsAfterRaise = firstTripStep is null || firstTripStep > LoadRaiseStep + (2 * StepsPerSecond);

        return new ProbeResult(
            probe.Id,
            probe.Authority,
            probe.RodWithdrawStep,
            probe.RodHoldStep,
            engine.LogicalStep,
            firstTripStep,
            firstLatchedFunctionId,
            firstLatchedFunctionStep,
            minimumFrequency,
            maximumFrequency,
            maximumThermalPower,
            maximumElectricalOutput,
            maximumAbsolutePhaseDifference,
            finalGenerator.RequestedElectricalPower.NumericValue ?? double.NaN,
            finalGenerator.ElectricalOutput.NumericValue ?? double.NaN,
            final.ReactorCore.ReactorThermalPower.NumericValue ?? double.NaN,
            finalGenerator.BreakerClosed,
            initialRod,
            finalRod,
            finalRod - initialRod,
            powerController.Mode,
            speedController.Mode,
            automation.EffectiveAuthority,
            survivesTwoSecondsAfterRaise,
            stableTenMweLate,
            allFinite);
    }

    private static ProbeSample Capture(string probeId, IntegratedAutomaticOperationRuntimeEngine engine, ICollection<ProbeSample> trajectory)
    {
        var protectedControl = engine.LatestCanonicalSnapshot.Control.ProtectedControl;
        var cycle = protectedControl.FullPlant.IntegratedCycle;
        var generator = Assert.Single(cycle.Generators);
        var rotor = Assert.Single(cycle.TurbineExpansion.Rotors);
        var presentation = engine.CreatePresentationSnapshot(ControlRoomRunState.Paused);
        var presentationGenerator = Assert.Single(presentation.Electrical.Generators);
        var sample = new ProbeSample(
            probeId,
            engine.LogicalStep,
            engine.LogicalStep / (double)StepsPerSecond,
            presentationGenerator.RequestedElectricalPower.NumericValue ?? double.NaN,
            presentationGenerator.ElectricalOutput.NumericValue ?? double.NaN,
            presentation.ReactorCore.ReactorThermalPower.NumericValue ?? double.NaN,
            generator.MechanicalInputPower.Megawatts,
            rotor.FinalAngularSpeed.RevolutionsPerMinute,
            generator.FinalElectricalFrequency.Hertz,
            generator.FinalPhaseDifference.Radians,
            presentation.ReactorCore.AverageRodWithdrawal.NumericValue ?? double.NaN,
            presentationGenerator.BreakerClosed,
            protectedControl.Protection.ReactorScramActive,
            protectedControl.Protection.TurbineTripActive,
            protectedControl.Protection.GeneratorTripActive);
        trajectory.Add(sample);
        return sample;
    }

    private static void WriteProbeSummary(IEnumerable<ProbeResult> results)
    {
        var lines = new List<string>
        {
            "probe_id,requested_authority,rod_withdraw_step,rod_hold_step,executed_steps,first_trip_step,first_latched_function,first_latched_step,min_frequency_hz,max_frequency_hz,max_thermal_mw,max_electrical_mwe,max_abs_phase_difference_rad,final_requested_mwe,final_output_mwe,final_thermal_mw,final_breaker_closed,initial_rod_withdrawal,final_rod_withdrawal,rod_withdrawal_delta,final_power_controller_mode,final_speed_controller_mode,final_effective_authority,survives_two_seconds_after_raise,stable_ten_mwe_late,all_finite"
        };
        lines.AddRange(results.Select(static result => string.Join(',', new[]
        {
            Csv(result.Id), result.RequestedAuthority.ToString(), I(result.RodWithdrawStep), I(result.RodHoldStep), result.ExecutedSteps.ToString(CultureInfo.InvariantCulture),
            I(result.FirstTripStep), Csv(result.FirstLatchedFunctionId ?? string.Empty), I(result.FirstLatchedFunctionStep), F(result.MinimumFrequencyHertz), F(result.MaximumFrequencyHertz),
            F(result.MaximumThermalPowerMegawatts), F(result.MaximumElectricalOutputMegawatts), F(result.MaximumAbsolutePhaseDifferenceRadians), F(result.FinalRequestedElectricalMegawatts),
            F(result.FinalElectricalOutputMegawatts), F(result.FinalReactorThermalMegawatts), result.FinalBreakerClosed.ToString(), F(result.InitialRodWithdrawal), F(result.FinalRodWithdrawal),
            F(result.RodWithdrawalDelta), result.FinalPowerControllerMode.ToString(), result.FinalSpeedControllerMode.ToString(), result.FinalEffectiveAuthority.ToString(),
            result.SurvivesTwoSecondsAfterRaise.ToString(), result.StableTenMegawattsLate.ToString(), result.AllFinite.ToString(),
        })));
        File.WriteAllLines(Path.Combine(ReportDirectory(), "140-authority-coordination-probe-summary.csv"), lines, Utf8WithoutBom);
    }

    private static void WriteTrajectory(IEnumerable<ProbeSample> rows)
    {
        var lines = new List<string>
        {
            "probe_id,logical_step,simulated_seconds,requested_electrical_mwe,electrical_output_mwe,reactor_thermal_mw,generator_mechanical_input_mw,rotor_rpm,generator_frequency_hz,generator_phase_difference_rad,average_rod_withdrawal,breaker_closed,reactor_scram,turbine_trip,generator_trip"
        };
        lines.AddRange(rows.Select(static row => string.Join(',', new[]
        {
            Csv(row.ProbeId), row.LogicalStep.ToString(CultureInfo.InvariantCulture), F(row.SimulatedSeconds), F(row.RequestedElectricalMegawatts),
            F(row.ElectricalOutputMegawatts), F(row.ReactorThermalMegawatts), F(row.GeneratorMechanicalInputMegawatts), F(row.RotorRpm),
            F(row.GeneratorFrequencyHertz), F(row.GeneratorPhaseDifferenceRadians), F(row.AverageRodWithdrawal), row.BreakerClosed.ToString(),
            row.ReactorScram.ToString(), row.TurbineTrip.ToString(), row.GeneratorTrip.ToString(),
        })));
        File.WriteAllLines(Path.Combine(ReportDirectory(), "141-authority-coordination-trajectories.csv"), lines, Utf8WithoutBom);
    }

    private static void WriteDecisionSummary(IReadOnlyCollection<ProbeResult> results)
    {
        var frozen = results.Single(static result => result.Id == "frozen-supervisory-load-only");
        var suppressed = results.Single(static result => result.Id == "supervisory-withdraw-500ms-spanning-load");
        var assisted = results.Where(static result => result.RequestedAuthority == PlantControlAuthorityMode.Assisted && result.RodWithdrawStep.HasValue).ToArray();
        var survivors = assisted.Where(static result => result.SurvivesTwoSecondsAfterRaise).ToArray();
        var lateStable = assisted.Where(static result => result.StableTenMegawattsLate).ToArray();
        var lines = new List<string>
        {
            "scope=M10 Final replacement-long failure Diagnostic 2; Diagnostic 1 returned PASS and proved generator-loss-of-synchronism first latches at step 636 / 6.36 s on the frozen exact-v9 5->10 MWe path; this candidate changes no production src, protection semantics, exact-v9, mission @3 or frozen replacement workload; failed Execution 1 remains RED;",
            $"frozen-reference=trip-step:{I(frozen.FirstTripStep)}|first-latch:{frozen.FirstLatchedFunctionId}|min-frequency-hz:{F(frozen.MinimumFrequencyHertz)}|rod-delta:{F(frozen.RodWithdrawalDelta)};",
            $"supervisory-rod-discrimination=trip-step:{I(suppressed.FirstTripStep)}|first-latch:{suppressed.FirstLatchedFunctionId}|rod-delta:{F(suppressed.RodWithdrawalDelta)}|rod-delta-vs-frozen:{F(suppressed.RodWithdrawalDelta - frozen.RodWithdrawalDelta)}|same-trip-step-as-frozen:{(suppressed.FirstTripStep == frozen.FirstTripStep)}|same-latch-as-frozen:{string.Equals(suppressed.FirstLatchedFunctionId, frozen.FirstLatchedFunctionId, StringComparison.Ordinal)};",
            $"assisted-coordinated-probes={assisted.Length}; survive-two-seconds-after-raise={survivors.Length}; stable-ten-mwe-late={lateStable.Length};",
            $"survivor-ids={(survivors.Length == 0 ? "none" : string.Join('|', survivors.Select(static result => result.Id)))};",
            $"stable-ten-mwe-ids={(lateStable.Length == 0 ? "none" : string.Join('|', lateStable.Select(static result => result.Id)))};",
            "decision-rule=compare the SupervisoryAutomatic rod probe against the frozen load-only reference, including trip owner/timing and rod trajectory, instead of assuming suppression. If the rod command has no material physical effect and the frozen loss-of-synchronism path is reproduced while one or more Assisted coordinated probes materially delay/avoid the trip, classify the frozen replacement operator/authority policy as under-specified relative to M7.6. Do not retune protection. If a bounded Assisted probe also reaches a late stable 10 MWe window, use that evidence to author a separate revised workload/operator-policy candidate followed by a new replacement-long freeze. If no Assisted coordinated probe improves the protection margin, keep the workload unchanged and continue with a production transient/control-granularity diagnostic before any runtime repair;",
            "authorization=diagnostic-only; second replacement long remains unauthorized until a separate decision/repair and new baseline freeze pass;",
        };
        File.WriteAllLines(Path.Combine(ReportDirectory(), "142-authority-coordination-decision-summary.txt"), lines, Utf8WithoutBom);
    }

    private static string F(double value) => value.ToString("G17", CultureInfo.InvariantCulture);
    private static string I(long? value) => value?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
    private static string I(int? value) => value?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
    private static string Csv(string value) => '"' + value.Replace("\"", "\"\"", StringComparison.Ordinal) + '"';

    private static void RequireOptIn()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable(OptInEnvironmentVariable), "1", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Set {OptInEnvironmentVariable}=1 to run M10 Final replacement-long failure Diagnostic 2.");
        }
    }

    private static string ReportDirectory()
        => Path.Combine(FindRepositoryRoot(), "artifacts", "m10-final-replacement-long-failure-diagnostic2");

    private static void AppendProgress(string message)
    {
        Directory.CreateDirectory(ReportDirectory());
        File.AppendAllText(Path.Combine(ReportDirectory(), "00-progress.txt"), message + Environment.NewLine, Utf8WithoutBom);
    }

    private static void ResetDirectory(string directory)
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "00-progress.txt"), "M10 FINAL REPLACEMENT-LONG FAILURE DIAGNOSTIC 2 STARTED" + Environment.NewLine, Utf8WithoutBom);
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

    private sealed record ProbeDefinition(
        string Id,
        PlantControlAuthorityMode Authority,
        int? RodWithdrawStep,
        int? RodHoldStep);

    private sealed record ProbeResult(
        string Id,
        PlantControlAuthorityMode RequestedAuthority,
        int? RodWithdrawStep,
        int? RodHoldStep,
        long ExecutedSteps,
        long? FirstTripStep,
        string? FirstLatchedFunctionId,
        long? FirstLatchedFunctionStep,
        double MinimumFrequencyHertz,
        double MaximumFrequencyHertz,
        double MaximumThermalPowerMegawatts,
        double MaximumElectricalOutputMegawatts,
        double MaximumAbsolutePhaseDifferenceRadians,
        double FinalRequestedElectricalMegawatts,
        double FinalElectricalOutputMegawatts,
        double FinalReactorThermalMegawatts,
        bool FinalBreakerClosed,
        double InitialRodWithdrawal,
        double FinalRodWithdrawal,
        double RodWithdrawalDelta,
        ControllerMode FinalPowerControllerMode,
        ControllerMode FinalSpeedControllerMode,
        PlantControlAuthorityMode FinalEffectiveAuthority,
        bool SurvivesTwoSecondsAfterRaise,
        bool StableTenMegawattsLate,
        bool AllFinite);

    private sealed record ProbeSample(
        string ProbeId,
        long LogicalStep,
        double SimulatedSeconds,
        double RequestedElectricalMegawatts,
        double ElectricalOutputMegawatts,
        double ReactorThermalMegawatts,
        double GeneratorMechanicalInputMegawatts,
        double RotorRpm,
        double GeneratorFrequencyHertz,
        double GeneratorPhaseDifferenceRadians,
        double AverageRodWithdrawal,
        bool BreakerClosed,
        bool ReactorScram,
        bool TurbineTrip,
        bool GeneratorTrip)
    {
        public bool AnyTripActive => ReactorScram || TurbineTrip || GeneratorTrip;
        public bool AllFinite =>
            double.IsFinite(SimulatedSeconds)
            && double.IsFinite(RequestedElectricalMegawatts)
            && double.IsFinite(ElectricalOutputMegawatts)
            && double.IsFinite(ReactorThermalMegawatts)
            && double.IsFinite(GeneratorMechanicalInputMegawatts)
            && double.IsFinite(RotorRpm)
            && double.IsFinite(GeneratorFrequencyHertz)
            && double.IsFinite(GeneratorPhaseDifferenceRadians)
            && double.IsFinite(AverageRodWithdrawal);
    }
}
