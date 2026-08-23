using System.Globalization;
using System.Reflection;
using System.Text;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.ControlRoom.Automation;
using NuclearReactorSimulator.Application.Scenarios.Training;
using NuclearReactorSimulator.Domain.Physics.Control.Supervisory;
using NuclearReactorSimulator.Simulation.Physics.Control.Integration;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay;

/// <summary>
/// Evidence-only follow-up after Replacement-Long Failure Diagnostic 3 showed that breaker-closed SPEED commands are
/// superseded by droop, valve preloading creates no material shaft margin, and exact-v4 reproduces the same protected
/// 5 -> 10 MWe failure family. This audit separates generator-load command granularity from missing reactor/steam energy
/// support before any production generator-grid, protection, exact-v9, mission-pack, or replacement-workload change.
/// </summary>
public sealed class M10FinalReplacementLongFailureDiagnostic4Tests
{
    private const string OptInEnvironmentVariable = "NRS_M10_FINAL_REPLACEMENT_LONG_DIAGNOSTIC4";
    private const int StepsPerSecond = 100;
    private const int TotalSteps = 3_000;
    private static readonly UTF8Encoding Utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);

    [Fact(Explicit = true)]
    [Trait("Category", "M10FinalReplacementLongFailureDiagnostic4")]
    public void ExactV9_LoadRampTorqueCouplingAndEnergySupportCensus()
    {
        RequireOptIn();
        ResetDirectory(ReportDirectory());

        ProbeDefinition[] probes =
        [
            new(
                "exact-v9-reference-5mwe-step-hold-current",
                ExactVersion: 9,
                LoadIncrementMegawatts: 5d,
                FirstLoadCommandStep: 500,
                LoadCommandSpacingSteps: 1,
                LoadCommandCount: 1,
                CoordinatedReactorSupport: false,
                ReactorSupportLeadSteps: 0,
                PrePowerTargetMegawatts: null,
                PrePowerStartStep: null),
            new(
                "exact-v9-ramp-1mwe-1s-hold-current",
                ExactVersion: 9,
                LoadIncrementMegawatts: 1d,
                FirstLoadCommandStep: 500,
                LoadCommandSpacingSteps: 100,
                LoadCommandCount: 5,
                CoordinatedReactorSupport: false,
                ReactorSupportLeadSteps: 0,
                PrePowerTargetMegawatts: null,
                PrePowerStartStep: null),
            new(
                "exact-v9-ramp-0p5mwe-0p5s-hold-current",
                ExactVersion: 9,
                LoadIncrementMegawatts: 0.5d,
                FirstLoadCommandStep: 500,
                LoadCommandSpacingSteps: 50,
                LoadCommandCount: 10,
                CoordinatedReactorSupport: false,
                ReactorSupportLeadSteps: 0,
                PrePowerTargetMegawatts: null,
                PrePowerStartStep: null),
            new(
                "exact-v9-ramp-1mwe-2s-hold-current",
                ExactVersion: 9,
                LoadIncrementMegawatts: 1d,
                FirstLoadCommandStep: 500,
                LoadCommandSpacingSteps: 200,
                LoadCommandCount: 5,
                CoordinatedReactorSupport: false,
                ReactorSupportLeadSteps: 0,
                PrePowerTargetMegawatts: null,
                PrePowerStartStep: null),
            new(
                "exact-v9-ramp-1mwe-2s-reactor-supported",
                ExactVersion: 9,
                LoadIncrementMegawatts: 1d,
                FirstLoadCommandStep: 500,
                LoadCommandSpacingSteps: 200,
                LoadCommandCount: 5,
                CoordinatedReactorSupport: true,
                ReactorSupportLeadSteps: 100,
                PrePowerTargetMegawatts: null,
                PrePowerStartStep: null),
            new(
                "exact-v9-prepower-66mw-then-5mwe-step",
                ExactVersion: 9,
                LoadIncrementMegawatts: 5d,
                FirstLoadCommandStep: 2_000,
                LoadCommandSpacingSteps: 1,
                LoadCommandCount: 1,
                CoordinatedReactorSupport: false,
                ReactorSupportLeadSteps: 0,
                PrePowerTargetMegawatts: 66d,
                PrePowerStartStep: 100),
            new(
                "exact-v4-ramp-1mwe-2s-reactor-supported",
                ExactVersion: 4,
                LoadIncrementMegawatts: 1d,
                FirstLoadCommandStep: 500,
                LoadCommandSpacingSteps: 200,
                LoadCommandCount: 5,
                CoordinatedReactorSupport: true,
                ReactorSupportLeadSteps: 100,
                PrePowerTargetMegawatts: null,
                PrePowerStartStep: null),
        ];

        var results = new List<ProbeResult>();
        var trajectory = new List<ProbeSample>();
        foreach (var probe in probes)
        {
            AppendProgress($"probe-start={probe.Id}");
            var result = RunProbe(probe, trajectory);
            results.Add(result);
            AppendProgress(
                $"probe-complete={probe.Id}|executed={result.ExecutedSteps}|first-trip={I(result.FirstTripStep)}|first-latch={result.FirstLatchedFunctionId ?? "none"}|first-10mwe={I(result.FirstTenMegawattRequestStep)}|survive-2s-after-10={result.SurvivesTwoSecondsAfterTenMegawatts}|stable-10-late={result.StableTenMegawattsLate}|max-thermal={F(result.MaximumThermalMegawatts)}|max-shaft={F(result.MaximumShaftMegawatts)}");
        }

        WriteProbeSummary(results);
        WriteTrajectory(trajectory);
        WriteDecisionSummary(results);

        var reference = results.Single(static result => result.Id == "exact-v9-reference-5mwe-step-hold-current");
        Assert.Null(reference.ExceptionType);
        Assert.Equal((long)TotalSteps, reference.ExecutedSteps);
        Assert.Equal(636L, reference.FirstTripStep);
        Assert.Equal("generator-loss-of-synchronism", reference.FirstLatchedFunctionId);
        Assert.All(results, static result => Assert.True(result.DiagnosticComplete));
    }

    private static ProbeResult RunProbe(ProbeDefinition probe, ICollection<ProbeSample> allTrajectory)
    {
        var engine = CreateEngine(probe.ExactVersion, probe.LoadIncrementMegawatts);
        engine.RequestPlantControlAuthority(PlantControlAuthorityMode.SupervisoryAutomatic);

        var initialPresentation = engine.CreatePresentationSnapshot(ControlRoomRunState.Paused);
        var generatorId = Assert.Single(initialPresentation.Electrical.Generators).GeneratorId;
        var initialRequestedMegawatts = Assert.Single(initialPresentation.Electrical.Generators).RequestedElectricalPower.NumericValue
            ?? throw new InvalidOperationException("Initial generator requested load is unavailable.");
        var initialThermalMegawatts = initialPresentation.ReactorCore.ReactorThermalPower.NumericValue
            ?? throw new InvalidOperationException("Initial reactor thermal power is unavailable.");

        // Start every probe from the same frozen operating-point objective. Exploratory reactor-support
        // objectives replace it only at their explicit support/pre-power step.
        engine.RequestSupervisoryObjective(SupervisoryObjectiveRequest.HoldCurrentOperatingPoint());

        var localRows = new List<ProbeSample>();
        long? firstTripStep = null;
        string? firstLatchedFunctionId = null;
        long? firstLatchedFunctionStep = null;
        long? firstTenMegawattRequestStep = null;
        string? exceptionType = null;
        string? exceptionMessage = null;
        var allFinite = true;

        Capture(probe.Id, engine, localRows, allTrajectory);
        for (var nextStep = 1; nextStep <= TotalSteps; nextStep++)
        {
            try
            {
                if (probe.PrePowerStartStep == nextStep && probe.PrePowerTargetMegawatts.HasValue)
                {
                    engine.RequestSupervisoryObjective(
                        SupervisoryObjectiveRequest.HoldReactorPower(probe.PrePowerTargetMegawatts.Value * 1_000_000d));
                }

                var commandIndex = LoadCommandIndexAtStep(probe, nextStep);
                if (probe.CoordinatedReactorSupport)
                {
                    var supportIndex = ReactorSupportIndexAtStep(probe, nextStep);
                    if (supportIndex.HasValue)
                    {
                        var nextRequestedMegawatts = Math.Min(
                            10d,
                            initialRequestedMegawatts + (probe.LoadIncrementMegawatts * (supportIndex.Value + 1)));
                        var targetThermalMegawatts = initialThermalMegawatts * nextRequestedMegawatts / initialRequestedMegawatts;
                        engine.RequestSupervisoryObjective(
                            SupervisoryObjectiveRequest.HoldReactorPower(targetThermalMegawatts * 1_000_000d));
                    }
                }

                if (commandIndex.HasValue)
                {
                    engine.QueueOperatorCommand(new ControlRoomCommand(
                        ControlRoomCommandKind.GeneratorLoadRaise,
                        generatorId,
                        ControlRoomCommandTargetKind.Generator));
                }

                engine.Step(ControlRoomRunState.Running);
                var sample = Capture(probe.Id, engine, localRows, allTrajectory);
                allFinite &= sample.AllFinite;

                if (sample.AnyTripActive)
                {
                    firstTripStep ??= sample.LogicalStep;
                }

                if (firstTenMegawattRequestStep is null && sample.RequestedElectricalMegawatts >= 9.999d)
                {
                    firstTenMegawattRequestStep = sample.LogicalStep;
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
            catch (Exception exception)
            {
                exceptionType = exception.GetType().FullName ?? exception.GetType().Name;
                exceptionMessage = exception.Message.Replace('\r', ' ').Replace('\n', ' ');
                break;
            }
        }

        var firstLoadStep = probe.FirstLoadCommandStep;
        var loadStart = localRows.FirstOrDefault(row => row.LogicalStep == firstLoadStep - 1);
        var firstLoad = localRows.FirstOrDefault(row => row.LogicalStep == firstLoadStep);
        var minimumFrequency = localRows.Count == 0 ? double.NaN : localRows.Min(static row => row.GeneratorFrequencyHertz);
        var maximumThermal = localRows.Count == 0 ? double.NaN : localRows.Max(static row => row.ReactorThermalMegawatts);
        var maximumShaft = localRows.Count == 0 ? double.NaN : localRows.Max(static row => row.TurbineShaftMegawatts);
        var maximumPhase = localRows.Count == 0 ? double.NaN : localRows.Max(static row => Math.Abs(row.GeneratorPhaseDifferenceRadians));
        var final = localRows.Count == 0 ? null : localRows[^1];

        var survivesTwoSecondsAfterTenMegawatts = firstTenMegawattRequestStep.HasValue
            && localRows.Any(row => row.LogicalStep >= firstTenMegawattRequestStep.Value + (2 * StepsPerSecond) && !row.AnyTripActive);
        var stableTenMegawattsLate = firstTenMegawattRequestStep.HasValue
            && localRows.Where(row => row.LogicalStep >= firstTenMegawattRequestStep.Value + (5 * StepsPerSecond)).TakeLast(StepsPerSecond).Any()
            && localRows.Where(row => row.LogicalStep >= firstTenMegawattRequestStep.Value + (5 * StepsPerSecond)).TakeLast(StepsPerSecond)
                .All(static row => !row.AnyTripActive
                    && row.RequestedElectricalMegawatts >= 9.999d
                    && row.GeneratorFrequencyHertz >= 49.5d
                    && Math.Abs(row.GeneratorPhaseDifferenceRadians) <= 0.5d);

        return new ProbeResult(
            probe.Id,
            probe.ExactVersion,
            probe.LoadIncrementMegawatts,
            probe.FirstLoadCommandStep,
            probe.LoadCommandSpacingSteps,
            probe.LoadCommandCount,
            probe.CoordinatedReactorSupport,
            probe.ReactorSupportLeadSteps,
            probe.PrePowerTargetMegawatts,
            probe.PrePowerStartStep,
            localRows.Count == 0 ? 0 : localRows[^1].LogicalStep,
            firstTripStep,
            firstLatchedFunctionId,
            firstLatchedFunctionStep,
            firstTenMegawattRequestStep,
            loadStart?.TurbineShaftMegawatts ?? double.NaN,
            firstLoad?.TurbineShaftMegawatts ?? double.NaN,
            firstLoad?.GeneratorMechanicalInputMegawatts ?? double.NaN,
            firstLoad?.CommandedElectromagneticTorqueNewtonMetres ?? double.NaN,
            firstLoad?.EffectiveElectromagneticTorqueNewtonMetres ?? double.NaN,
            minimumFrequency,
            maximumPhase,
            maximumThermal,
            maximumShaft,
            final?.RequestedElectricalMegawatts ?? double.NaN,
            final?.ElectricalOutputMegawatts ?? double.NaN,
            final?.ReactorThermalMegawatts ?? double.NaN,
            survivesTwoSecondsAfterTenMegawatts,
            stableTenMegawattsLate,
            allFinite,
            exceptionType,
            exceptionMessage);
    }

    private static int? LoadCommandIndexAtStep(ProbeDefinition probe, int nextStep)
    {
        if (nextStep < probe.FirstLoadCommandStep)
        {
            return null;
        }

        var delta = nextStep - probe.FirstLoadCommandStep;
        if (delta % probe.LoadCommandSpacingSteps != 0)
        {
            return null;
        }

        var index = delta / probe.LoadCommandSpacingSteps;
        return index >= 0 && index < probe.LoadCommandCount ? index : null;
    }

    private static int? ReactorSupportIndexAtStep(ProbeDefinition probe, int nextStep)
    {
        var firstSupportStep = probe.FirstLoadCommandStep - probe.ReactorSupportLeadSteps;
        if (nextStep < firstSupportStep)
        {
            return null;
        }

        var delta = nextStep - firstSupportStep;
        if (delta % probe.LoadCommandSpacingSteps != 0)
        {
            return null;
        }

        var index = delta / probe.LoadCommandSpacingSteps;
        return index >= 0 && index < probe.LoadCommandCount ? index : null;
    }

    private static IntegratedAutomaticOperationRuntimeEngine CreateEngine(int exactVersion, double loadIncrementMegawatts)
    {
        var baseline = exactVersion switch
        {
            9 => Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
                new DesktopSustainedGenerationPostMoistureEquilibriumCandidateInitialConditionFactory().CreateRuntimeEngine()),
            4 => Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
                new DesktopSustainedGenerationI5RepairedActivationCandidateInitialConditionFactory().CreateRuntimeEngine()),
            _ => throw new ArgumentOutOfRangeException(nameof(exactVersion), exactVersion, "Unsupported diagnostic exact version."),
        };

        if (Math.Abs(loadIncrementMegawatts - 5d) <= 1e-12d)
        {
            return baseline;
        }

        var solverField = typeof(IntegratedAutomaticOperationRuntimeEngine).GetField(
            "_solver",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Runtime diagnostic clone could not locate the private solver field.");
        var solver = solverField.GetValue(baseline) as IntegratedAutomaticOperationSolver
            ?? throw new InvalidOperationException("Runtime diagnostic clone could not read the integrated solver.");
        var commandPolicy = new ControlRoomRuntimeCommandPolicy(
            ControlRoomRuntimeCommandPolicy.Default.TurbineSpeedSetpointIncrementRpm,
            loadIncrementMegawatts * 1_000_000d);

        return new IntegratedAutomaticOperationRuntimeEngine(
            solver,
            baseline.CurrentState,
            baseline.PersistentInputs,
            baseline.LatestCanonicalSnapshot,
            baseline.FixedDeltaTime,
            baseline.LogicalStep,
            commandPolicy);
    }

    private static ProbeSample Capture(
        string probeId,
        IntegratedAutomaticOperationRuntimeEngine engine,
        ICollection<ProbeSample> localRows,
        ICollection<ProbeSample> allRows)
    {
        var protectedControl = engine.LatestCanonicalSnapshot.Control.ProtectedControl;
        var cycle = protectedControl.FullPlant.IntegratedCycle;
        var stage = Assert.Single(cycle.TurbineExpansion.StageGroups);
        var rotor = Assert.Single(cycle.TurbineExpansion.Rotors);
        var generator = Assert.Single(cycle.Generators);
        var presentation = engine.CreatePresentationSnapshot(ControlRoomRunState.Paused);
        var presentationGenerator = Assert.Single(presentation.Electrical.Generators);
        var step = engine.LogicalStep;

        var row = new ProbeSample(
            probeId,
            step,
            step / (double)StepsPerSecond,
            presentationGenerator.RequestedElectricalPower.NumericValue ?? double.NaN,
            presentationGenerator.ElectricalOutput.NumericValue ?? double.NaN,
            presentation.ReactorCore.ReactorThermalPower.NumericValue ?? double.NaN,
            generator.MechanicalInputPower.Megawatts,
            stage.ShaftPower.Megawatts,
            rotor.FinalAngularSpeed.RevolutionsPerMinute,
            generator.FinalElectricalFrequency.Hertz,
            generator.FinalPhaseDifference.Radians,
            generator.CommandedElectromagneticTorque.NewtonMetres,
            generator.EffectiveElectromagneticTorque.NewtonMetres,
            generator.BreakerFinallyClosed,
            protectedControl.Protection.ReactorScramActive,
            protectedControl.Protection.TurbineTripActive,
            protectedControl.Protection.GeneratorTripActive);
        localRows.Add(row);
        allRows.Add(row);
        return row;
    }

    private static void WriteProbeSummary(IEnumerable<ProbeResult> results)
    {
        var lines = new List<string>
        {
            "probe_id,exact_version,load_increment_mwe,first_load_command_step,load_command_spacing_steps,load_command_count,coordinated_reactor_support,reactor_support_lead_steps,prepower_target_mw,prepower_start_step,executed_steps,first_trip_step,first_latched_function,first_latched_step,first_10mwe_request_step,preload_shaft_mw,first_load_shaft_mw,first_load_generator_mechanical_mw,first_load_commanded_em_torque_nm,first_load_effective_em_torque_nm,min_frequency_hz,max_abs_phase_rad,max_thermal_mw,max_shaft_mw,final_requested_mwe,final_output_mwe,final_thermal_mw,survives_two_seconds_after_10mwe,stable_10mwe_late,all_finite,exception_type,exception_message"
        };
        lines.AddRange(results.Select(static result => string.Join(',', new[]
        {
            Csv(result.Id), result.ExactVersion.ToString(CultureInfo.InvariantCulture), F(result.LoadIncrementMegawatts),
            result.FirstLoadCommandStep.ToString(CultureInfo.InvariantCulture), result.LoadCommandSpacingSteps.ToString(CultureInfo.InvariantCulture), result.LoadCommandCount.ToString(CultureInfo.InvariantCulture),
            result.CoordinatedReactorSupport.ToString(), result.ReactorSupportLeadSteps.ToString(CultureInfo.InvariantCulture), F(result.PrePowerTargetMegawatts), I(result.PrePowerStartStep),
            result.ExecutedSteps.ToString(CultureInfo.InvariantCulture), I(result.FirstTripStep), Csv(result.FirstLatchedFunctionId ?? string.Empty), I(result.FirstLatchedFunctionStep), I(result.FirstTenMegawattRequestStep),
            F(result.PreLoadShaftMegawatts), F(result.FirstLoadShaftMegawatts), F(result.FirstLoadGeneratorMechanicalMegawatts), F(result.FirstLoadCommandedElectromagneticTorqueNewtonMetres), F(result.FirstLoadEffectiveElectromagneticTorqueNewtonMetres),
            F(result.MinimumFrequencyHertz), F(result.MaximumAbsolutePhaseDifferenceRadians), F(result.MaximumThermalMegawatts), F(result.MaximumShaftMegawatts), F(result.FinalRequestedMegawatts), F(result.FinalElectricalOutputMegawatts), F(result.FinalThermalMegawatts),
            result.SurvivesTwoSecondsAfterTenMegawatts.ToString(), result.StableTenMegawattsLate.ToString(), result.AllFinite.ToString(), Csv(result.ExceptionType ?? string.Empty), Csv(result.ExceptionMessage ?? string.Empty),
        })));
        File.WriteAllLines(Path.Combine(ReportDirectory(), "160-load-ramp-energy-support-probe-summary.csv"), lines, Utf8WithoutBom);
    }

    private static void WriteTrajectory(IEnumerable<ProbeSample> rows)
    {
        var lines = new List<string>
        {
            "probe_id,logical_step,simulated_seconds,requested_electrical_mwe,electrical_output_mwe,reactor_thermal_mw,generator_mechanical_input_mw,turbine_shaft_mw,rotor_rpm,generator_frequency_hz,generator_phase_difference_rad,commanded_em_torque_nm,effective_em_torque_nm,breaker_closed,reactor_scram,turbine_trip,generator_trip"
        };
        lines.AddRange(rows.Select(static row => string.Join(',', new[]
        {
            Csv(row.ProbeId), row.LogicalStep.ToString(CultureInfo.InvariantCulture), F(row.SimulatedSeconds), F(row.RequestedElectricalMegawatts), F(row.ElectricalOutputMegawatts), F(row.ReactorThermalMegawatts),
            F(row.GeneratorMechanicalInputMegawatts), F(row.TurbineShaftMegawatts), F(row.RotorRpm), F(row.GeneratorFrequencyHertz), F(row.GeneratorPhaseDifferenceRadians), F(row.CommandedElectromagneticTorqueNewtonMetres), F(row.EffectiveElectromagneticTorqueNewtonMetres),
            row.BreakerClosed.ToString(), row.ReactorScram.ToString(), row.TurbineTrip.ToString(), row.GeneratorTrip.ToString(),
        })));
        File.WriteAllLines(Path.Combine(ReportDirectory(), "161-load-ramp-energy-support-trajectories.csv"), lines, Utf8WithoutBom);
    }

    private static void WriteDecisionSummary(IReadOnlyCollection<ProbeResult> results)
    {
        var reference = results.Single(static result => result.Id == "exact-v9-reference-5mwe-step-hold-current");
        var v9LoadOnly = results.Where(static result => result.ExactVersion == 9 && !result.CoordinatedReactorSupport && !result.PrePowerTargetMegawatts.HasValue).ToArray();
        var v9Supported = results.Where(static result => result.ExactVersion == 9 && (result.CoordinatedReactorSupport || result.PrePowerTargetMegawatts.HasValue)).ToArray();
        var v4Supported = results.Single(static result => result.Id == "exact-v4-ramp-1mwe-2s-reactor-supported");

        var lines = new[]
        {
            "scope=M10 Final replacement-long failure Diagnostic 4; Diagnostic 3 execution PASS showed breaker-closed SPEED commands ineffective, 55/65% valve preload unable to create material shaft margin, and exact-v4 reproducing the same loss-of-synchronism family; this candidate remains evidence-only and changes no production src, default command increment, protection, exact-v9, mission @3 or frozen replacement workload; failed Execution 1 remains RED;",
            FormattableString.Invariant($"reference-v9=trip:{I(reference.FirstTripStep)}|latch:{reference.FirstLatchedFunctionId ?? "none"}|first-load-commanded-em-torque-nm:{F(reference.FirstLoadCommandedElectromagneticTorqueNewtonMetres)}|first-load-effective-em-torque-nm:{F(reference.FirstLoadEffectiveElectromagneticTorqueNewtonMetres)}|first-load-shaft-mw:{F(reference.FirstLoadShaftMegawatts)}|first-load-generator-mechanical-mw:{F(reference.FirstLoadGeneratorMechanicalMegawatts)};"),
            $"v9-load-only-probes={v9LoadOnly.Length}|reach-10mwe:{v9LoadOnly.Count(static result => result.FirstTenMegawattRequestStep.HasValue)}|survive-2s-after-10:{v9LoadOnly.Count(static result => result.SurvivesTwoSecondsAfterTenMegawatts)}|stable-10-late:{v9LoadOnly.Count(static result => result.StableTenMegawattsLate)};",
            $"v9-energy-supported-probes={v9Supported.Length}|reach-10mwe:{v9Supported.Count(static result => result.FirstTenMegawattRequestStep.HasValue)}|survive-2s-after-10:{v9Supported.Count(static result => result.SurvivesTwoSecondsAfterTenMegawatts)}|stable-10-late:{v9Supported.Count(static result => result.StableTenMegawattsLate)}|max-thermal-mw:{F(v9Supported.Max(static result => result.MaximumThermalMegawatts))}|max-shaft-mw:{F(v9Supported.Max(static result => result.MaximumShaftMegawatts))};",
            FormattableString.Invariant($"historical-v4-supported=trip:{I(v4Supported.FirstTripStep)}|latch:{v4Supported.FirstLatchedFunctionId ?? "none"}|first-10mwe:{I(v4Supported.FirstTenMegawattRequestStep)}|survive-2s-after-10:{v4Supported.SurvivesTwoSecondsAfterTenMegawatts}|stable-10-late:{v4Supported.StableTenMegawattsLate}|max-thermal-mw:{F(v4Supported.MaximumThermalMegawatts)}|max-shaft-mw:{F(v4Supported.MaximumShaftMegawatts)};"),
            "decision-rule=first compare smaller/slower load-only ramps against the frozen 5 MWe step. If one reaches a stable protected 10 MWe window without additional reactor support, classify the default 5 MWe command granularity / instantaneous request step as the missing margin and author a separate command/workload-policy candidate before any production runtime repair. If load-only ramps fail but a reactor-supported ramp or 66 MW pre-power control reaches stable 10 MWe, classify the frozen workload as missing slow energy-support coordination; do not retune protection or generator-grid physics. If even energy-supported ramps materially raise thermal/shaft power yet still fail before a stable 10 MWe window, escalate to a dedicated generator-grid torque-coupling / attainable-capacity repair investigation. Compare exact-v4 only after the same supported schedule; a material v4/v9 split localizes version-specific steam-path capacity, while matching failure remains shared semantics/capacity evidence. Diagnostic 4 itself authorizes no production change;",
            "authorization=diagnostic-only; second replacement long remains unauthorized; GitHub ordinary CI remains a separate gate and must be green or its specific failing non-explicit test identified before M10 closure;",
        };
        File.WriteAllLines(Path.Combine(ReportDirectory(), "162-load-ramp-energy-support-decision-summary.txt"), lines, Utf8WithoutBom);
    }

    private static void RequireOptIn()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable(OptInEnvironmentVariable), "1", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Set {OptInEnvironmentVariable}=1 to run M10 Final replacement-long failure Diagnostic 4.");
        }
    }

    private static string ReportDirectory()
        => Path.Combine(FindRepositoryRoot(), "artifacts", "m10-final-replacement-long-failure-diagnostic4");

    private static void ResetDirectory(string directory)
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "00-progress.txt"), "M10 FINAL REPLACEMENT-LONG FAILURE DIAGNOSTIC 4 STARTED" + Environment.NewLine, Utf8WithoutBom);
    }

    private static void AppendProgress(string message)
        => File.AppendAllText(Path.Combine(ReportDirectory(), "00-progress.txt"), message + Environment.NewLine, Utf8WithoutBom);

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "NuclearReactorSimulator.sln")))
            {
                return directory.FullName;
            }
            directory = directory.Parent;
        }
        throw new DirectoryNotFoundException("Could not locate NuclearReactorSimulator.sln from the test base directory.");
    }

    private static string F(double? value)
        => value.HasValue && double.IsFinite(value.Value)
            ? value.Value.ToString("G17", CultureInfo.InvariantCulture)
            : string.Empty;

    private static string F(double value)
        => double.IsFinite(value) ? value.ToString("G17", CultureInfo.InvariantCulture) : string.Empty;

    private static string I(long? value)
        => value?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;

    private static string I(int? value)
        => value?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;

    private static string Csv(string value)
        => '"' + value.Replace("\"", "\"\"", StringComparison.Ordinal) + '"';

    private sealed record ProbeDefinition(
        string Id,
        int ExactVersion,
        double LoadIncrementMegawatts,
        int FirstLoadCommandStep,
        int LoadCommandSpacingSteps,
        int LoadCommandCount,
        bool CoordinatedReactorSupport,
        int ReactorSupportLeadSteps,
        double? PrePowerTargetMegawatts,
        int? PrePowerStartStep);

    private sealed record ProbeResult(
        string Id,
        int ExactVersion,
        double LoadIncrementMegawatts,
        int FirstLoadCommandStep,
        int LoadCommandSpacingSteps,
        int LoadCommandCount,
        bool CoordinatedReactorSupport,
        int ReactorSupportLeadSteps,
        double? PrePowerTargetMegawatts,
        int? PrePowerStartStep,
        long ExecutedSteps,
        long? FirstTripStep,
        string? FirstLatchedFunctionId,
        long? FirstLatchedFunctionStep,
        long? FirstTenMegawattRequestStep,
        double PreLoadShaftMegawatts,
        double FirstLoadShaftMegawatts,
        double FirstLoadGeneratorMechanicalMegawatts,
        double FirstLoadCommandedElectromagneticTorqueNewtonMetres,
        double FirstLoadEffectiveElectromagneticTorqueNewtonMetres,
        double MinimumFrequencyHertz,
        double MaximumAbsolutePhaseDifferenceRadians,
        double MaximumThermalMegawatts,
        double MaximumShaftMegawatts,
        double FinalRequestedMegawatts,
        double FinalElectricalOutputMegawatts,
        double FinalThermalMegawatts,
        bool SurvivesTwoSecondsAfterTenMegawatts,
        bool StableTenMegawattsLate,
        bool AllFinite,
        string? ExceptionType,
        string? ExceptionMessage)
    {
        public bool DiagnosticComplete => AllFinite && ExecutedSteps > 0;
    }

    private sealed record ProbeSample(
        string ProbeId,
        long LogicalStep,
        double SimulatedSeconds,
        double RequestedElectricalMegawatts,
        double ElectricalOutputMegawatts,
        double ReactorThermalMegawatts,
        double GeneratorMechanicalInputMegawatts,
        double TurbineShaftMegawatts,
        double RotorRpm,
        double GeneratorFrequencyHertz,
        double GeneratorPhaseDifferenceRadians,
        double CommandedElectromagneticTorqueNewtonMetres,
        double EffectiveElectromagneticTorqueNewtonMetres,
        bool BreakerClosed,
        bool ReactorScram,
        bool TurbineTrip,
        bool GeneratorTrip)
    {
        public bool AnyTripActive => ReactorScram || TurbineTrip || GeneratorTrip;

        public bool AllFinite => double.IsFinite(SimulatedSeconds)
            && double.IsFinite(RequestedElectricalMegawatts)
            && double.IsFinite(ElectricalOutputMegawatts)
            && double.IsFinite(ReactorThermalMegawatts)
            && double.IsFinite(GeneratorMechanicalInputMegawatts)
            && double.IsFinite(TurbineShaftMegawatts)
            && double.IsFinite(RotorRpm)
            && double.IsFinite(GeneratorFrequencyHertz)
            && double.IsFinite(GeneratorPhaseDifferenceRadians)
            && double.IsFinite(CommandedElectromagneticTorqueNewtonMetres)
            && double.IsFinite(EffectiveElectromagneticTorqueNewtonMetres);
    }
}
