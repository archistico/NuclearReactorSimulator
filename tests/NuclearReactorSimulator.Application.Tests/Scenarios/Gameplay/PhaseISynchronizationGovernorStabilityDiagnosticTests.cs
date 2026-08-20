using System.Globalization;
using System.Text;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios.PreStartup;
using NuclearReactorSimulator.Application.Scenarios.Synchronization;
using NuclearReactorSimulator.Domain.Physics.Control;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor.ControlRods;
using NuclearReactorSimulator.Domain.Physics.Reactor.PrimaryCircuit.SteamDrums;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay;

/// <summary>
/// I.5 closure-blocker diagnostic. It reproduces the validated pre-synchronization-grid-loading@2 long-journey
/// oscillation and compares a small, bounded governor-only candidate set without changing any registered initial
/// condition or production runtime. All plant, hydraulic, electrical-grid and seed parameters remain identical to v2.
/// </summary>
public sealed class PhaseISynchronizationGovernorStabilityDiagnosticTests
{
    private const int TotalSteps = 6_000;
    private const int SampleStrideSteps = 100;
    private const int StableWindowStartStep = 2_000;
    private static readonly UTF8Encoding Utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);

    [Fact(Explicit = true)]
    [Trait("Category", "PhaseISynchronizationGovernorStabilityDiagnostic")]
    public void FrozenV2AndBoundedGovernorCandidates_ClassifySixtySecondLowLoadStability()
    {
        var candidates = new[]
        {
            new GovernorCandidate("legacy-v2", 0.5d, 0.02d, 0d, "frozen exact-v2 controller"),
            new GovernorCandidate("add-kd-0p1", 0.5d, 0.02d, 0.1d, "minimal derivative damping"),
            new GovernorCandidate("add-kd-0p2", 0.5d, 0.02d, 0.2d, "desktop-proven derivative damping with legacy Kp"),
            new GovernorCandidate("kp1-pi", 1d, 0.02d, 0d, "desktop proportional gain without derivative"),
            new GovernorCandidate("desktop-pid", 1d, 0.02d, 0.2d, "controller gains already used by validated desktop v3 reference"),
        };

        ResetReportDirectory();
        var results = candidates.Select(RunCandidate).ToArray();
        WriteArtifacts(results);

        var legacy = results.Single(static item => item.Candidate.Id == "legacy-v2");
        Assert.False(legacy.Qualifies, "The diagnostic must reproduce the observed exact-v2 late low-load stability failure before recommending any replacement controller.");

        var qualified = results.Where(static item => item.Qualifies).ToArray();
        Assert.True(
            qualified.Length > 0,
            "No bounded governor-only candidate satisfied the strict 20-60 s stability window. Do not weaken the long-journey acceptance contract; inspect the generated diagnostic artifacts instead.");
    }

    private static CandidateResult RunCandidate(GovernorCandidate candidate)
    {
        var engine = CreateRuntimeEngine(candidate);
        var initial = engine.CreatePresentationSnapshot(ControlRoomRunState.Paused);
        var initialGenerator = Assert.Single(initial.Electrical.Generators);

        Assert.False(initialGenerator.BreakerClosed);
        Assert.True(initialGenerator.SynchronizationConditionsSatisfied);

        engine.QueueOperatorCommand(new ControlRoomCommand(
            ControlRoomCommandKind.GeneratorBreakerClose,
            initialGenerator.BreakerId,
            ControlRoomCommandTargetKind.Breaker));
        var paralleled = engine.Step(ControlRoomRunState.Running);
        var paralleledGenerator = Assert.Single(paralleled.Electrical.Generators);
        Assert.True(paralleledGenerator.BreakerClosed);

        engine.QueueOperatorCommand(new ControlRoomCommand(
            ControlRoomCommandKind.GeneratorLoadRaise,
            paralleledGenerator.GeneratorId,
            ControlRoomCommandTargetKind.Generator));
        var loaded = engine.Step(ControlRoomRunState.Running);
        var loadedGenerator = Assert.Single(loaded.Electrical.Generators);
        var initialLoadedElectricalMegawatts = loadedGenerator.ElectricalOutput.NumericValue ?? double.NaN;

        var samples = new List<DiagnosticSample>(TotalSteps / SampleStrideSteps);
        var tripSteps = 0;
        var breakerOpenSteps = 0;
        var requestViolationSteps = 0;
        var shaftViolationSteps = 0;
        var reverseAdmissionSteps = 0;
        var minShaftMegawatts = double.PositiveInfinity;
        var minGrossMegawatts = double.PositiveInfinity;
        var minRotorRpm = double.PositiveInfinity;
        var maxRotorRpm = double.NegativeInfinity;

        for (var step = 1; step <= TotalSteps; step++)
        {
            var snapshot = engine.Step(ControlRoomRunState.Running);
            var generator = Assert.Single(snapshot.Electrical.Generators);
            var rotor = Assert.Single(snapshot.TurbineSecondary.Rotors);
            var train = Assert.Single(snapshot.TurbineSecondary.AdmissionTrains);

            var request = generator.RequestedElectricalPower.NumericValue ?? double.NaN;
            var gross = generator.ElectricalOutput.NumericValue ?? double.NaN;
            var shaft = rotor.ShaftPower.NumericValue ?? double.NaN;
            var rpm = rotor.Speed.NumericValue ?? double.NaN;
            var admissionFlow = train.AdmissionFlow.NumericValue ?? double.NaN;

            if (snapshot.AnyTripActive)
            {
                tripSteps++;
            }
            if (!generator.BreakerClosed)
            {
                breakerOpenSteps++;
            }
            if (!(request > 4.5d))
            {
                requestViolationSteps++;
            }
            if (!(shaft > 4.5d))
            {
                shaftViolationSteps++;
            }
            if (!(admissionFlow >= 0d))
            {
                reverseAdmissionSteps++;
            }

            minShaftMegawatts = Math.Min(minShaftMegawatts, shaft);
            minGrossMegawatts = Math.Min(minGrossMegawatts, gross);
            minRotorRpm = Math.Min(minRotorRpm, rpm);
            maxRotorRpm = Math.Max(maxRotorRpm, rpm);

            if (step % SampleStrideSteps == 0)
            {
                samples.Add(new DiagnosticSample(
                    step,
                    step / 100d,
                    snapshot.AnyTripActive,
                    generator.BreakerClosed,
                    request,
                    gross,
                    shaft,
                    rpm,
                    admissionFlow,
                    train.ControlValvePosition.NumericValue ?? double.NaN));
            }
        }

        var stableWindowSamples = samples.Where(static sample => sample.Step >= StableWindowStartStep).ToArray();
        var stableGrossViolations = stableWindowSamples.Count(static sample => !(sample.GrossMegawatts > 4.0d));
        var stableRotorViolations = stableWindowSamples.Count(static sample => !(sample.RotorRpm >= 2_990d && sample.RotorRpm <= 3_010d));
        var stableShaftViolations = stableWindowSamples.Count(static sample => !(sample.ShaftMegawatts > 4.5d));
        var stableAdmissionReverseViolations = stableWindowSamples.Count(static sample => !(sample.AdmissionFlowKilogramsPerSecond >= 0d));
        var final = stableWindowSamples[^1];

        var qualifies =
            initialLoadedElectricalMegawatts > 4.5d
            && tripSteps == 0
            && breakerOpenSteps == 0
            && requestViolationSteps == 0
            && shaftViolationSteps == 0
            && reverseAdmissionSteps == 0
            && stableGrossViolations == 0
            && stableRotorViolations == 0
            && stableShaftViolations == 0
            && stableAdmissionReverseViolations == 0
            && final.GrossMegawatts > 4.0d
            && final.RotorRpm >= 2_990d
            && final.RotorRpm <= 3_010d;

        return new CandidateResult(
            candidate,
            initialLoadedElectricalMegawatts,
            samples,
            tripSteps,
            breakerOpenSteps,
            requestViolationSteps,
            shaftViolationSteps,
            reverseAdmissionSteps,
            stableGrossViolations,
            stableRotorViolations,
            stableShaftViolations,
            stableAdmissionReverseViolations,
            minGrossMegawatts,
            minShaftMegawatts,
            minRotorRpm,
            maxRotorRpm,
            final,
            qualifies);
    }

    private static IControlRoomRuntimeEngine CreateRuntimeEngine(GovernorCandidate candidate)
    {
        if (string.Equals(candidate.Id, "legacy-v2", StringComparison.Ordinal))
        {
            return new GridSynchronizationSustainedInitialConditionFactory().CreateRuntimeEngine();
        }

        return ColdShutdownInitialConditionFactory.CreateRuntimeEngineForOperationalSeed(
            NeutronPopulation.FromRelative(0.30d),
            mainCirculationRunning: true,
            initialRodPosition: ControlRodPosition.FromPercentWithdrawn(50d),
            initialPrimaryTemperatureCelsius: 280d,
            turbineStartupLineup: true,
            initialRotorSpeedRpm: 3_000d,
            initialGeneratorBreakerClosed: false,
            initialRequestedElectricalPowerMegawatts: 0d,
            initialCondenserCoolingPowerMegawatts: 40d,
            initialTurbineSpeedSetpointRpm: 3_000d,
            initialControlValvePercentOpen: 28d,
            initialHeaderSteamTemperatureCelsius: 278.5d,
            initialStopOutletSteamTemperatureCelsius: 277d,
            initialControlOutletSteamTemperatureCelsius: 249.5d,
            initialTurbineInletSteamTemperatureCelsius: 246.5d,
            primaryCirculationPipeResistancePascalSecondsSquaredPerKilogramSquared: 25d,
            mainCirculationPumpResistancePascalSecondsSquaredPerKilogramSquared: 25d,
            mainSteamLineResistancePascalSecondsSquaredPerKilogramSquared: 1_000d,
            turbineAdmissionValveResistancePascalSecondsSquaredPerKilogramSquared: 1_000d,
            speedControllerProportionalGain: candidate.ProportionalGain,
            speedControllerIntegralGainPerSecond: candidate.IntegralGainPerSecond,
            speedControllerDerivativeGainSeconds: candidate.DerivativeGainSeconds,
            hotwellControllerProportionalGain: -0.01d,
            includeTurbineShaftPowerInstrumentation: true,
            maximumCondenserMassFlowRateKilogramsPerSecond: 20d,
            condenserInstalledHeatRejectionCapacityMegawatts: 40d,
            condenserOverallHeatTransferConductanceMegawattsPerKelvin: 1.225d,
            condenserCoolingWaterTemperatureCelsius: 20d,
            usePressureResolvedCondenserCondensateEnergy: true,
            secondaryPumpResistancePascalSecondsSquaredPerKilogramSquared: 500d,
            initialCondensatePumpPercent: 42d,
            initialFeedwaterPumpPercent: 97d,
            levelControllerIntegralGainPerSecond: 0.001d,
            hotwellControllerIntegralGainPerSecond: -0.000001d,
            exhaustSteamSpaceVolumeCubicMetres: 1_000d,
            pressurizedSteamPathNodeVolumeCubicMetres: 100d,
            turbineExpansionResistancePascalSecondsSquaredPerKilogramSquared: 21_400d,
            useThermodynamicTurbineWork: true,
            turbineStageEfficiencyPercent: 86d,
            generatorMaximumSynchronizingCorrectionPowerMegawatts: 0.5d,
            generatorFrequencyDampingPowerAtOneHertzSlipMegawatts: 2d,
            secondaryPumpsHaveDischargeCheckValves: true,
            includeEnhancedSecondaryProtections: true,
            secondaryValveTravelRate: ActuatorTravelRate.FromFractionPerSecond(0.5d),
            turbineStopValveTravelRate: ActuatorTravelRate.FromFractionPerSecond(0.5d),
            secondaryPumpTravelRate: ActuatorTravelRate.FromFractionPerSecond(0.25d),
            governorFullLoadSpeedReferenceRiseRpm: 1.5d,
            steamDrumLiquidRecirculationMode: SteamDrumLiquidRecirculationMode.CirculationDemandBalanced,
            steamDrumSteamSourceResistancePascalSecondsSquaredPerKilogramSquared: 100d,
            includeCoreThermalCoupling: true,
            primaryOperationalFlowDisplayLagSeconds: 0.5d,
            initialSteamDrumLiquidLevelFraction: 0.5d,
            useVaporFractionLimitedTurbineAdmission: true,
            turbineRotorRatedSpeedMechanicalLossMegawatts: 0.5d,
            deterministicSeedStepCount: 2,
            generatorMaximumElectricalPowerMegawatts: 10d,
            generatorGridPowerFlowMode: NuclearReactorSimulator.Domain.Physics.Electrical.SynchronousGridPowerFlowMode.Bidirectional,
            includeEvidenceDerivedElectricalProtections: true,
            includeMainSteamHeaderRelief: true,
            includeTurbineBypass: true,
            useEnthalpyTransportForPassivePipesAndValves: true,
            useEnthalpyTransportForRemainingNonTurbinePaths: true,
            useEnthalpyTransportForTurbineExpansion: true,
            useHybridSemiImplicitHydraulics: false);
    }

    private static void WriteArtifacts(IReadOnlyList<CandidateResult> results)
    {
        var directory = ReportDirectory();

        var metrics = new List<string>
        {
            "candidate,kp,ki_per_s,kd_s,initial_loaded_mwe,trip_steps,breaker_open_steps,request_violation_steps,shaft_violation_steps,reverse_admission_steps,stable_gross_violations,stable_rotor_violations,stable_shaft_violations,stable_reverse_admission_violations,min_gross_mwe,min_shaft_mw,min_rotor_rpm,max_rotor_rpm,final_gross_mwe,final_shaft_mw,final_rotor_rpm,qualifies,note",
        };
        metrics.AddRange(results.Select(item => string.Join(",",
            item.Candidate.Id,
            F(item.Candidate.ProportionalGain),
            F(item.Candidate.IntegralGainPerSecond),
            F(item.Candidate.DerivativeGainSeconds),
            F(item.InitialLoadedElectricalMegawatts),
            item.TripSteps,
            item.BreakerOpenSteps,
            item.RequestViolationSteps,
            item.ShaftViolationSteps,
            item.ReverseAdmissionSteps,
            item.StableGrossViolations,
            item.StableRotorViolations,
            item.StableShaftViolations,
            item.StableAdmissionReverseViolations,
            F(item.MinGrossMegawatts),
            F(item.MinShaftMegawatts),
            F(item.MinRotorRpm),
            F(item.MaxRotorRpm),
            F(item.Final.GrossMegawatts),
            F(item.Final.ShaftMegawatts),
            F(item.Final.RotorRpm),
            item.Qualifies,
            item.Candidate.Note.Replace(',', ';'))));
        File.WriteAllLines(Path.Combine(directory, "02-governor-candidate-metrics.csv"), metrics, Utf8WithoutBom);

        var trace = new List<string>
        {
            "candidate,step,seconds,trip,breaker_closed,request_mwe,gross_mwe,shaft_mw,rotor_rpm,admission_kg_s,control_valve_percent",
        };
        foreach (var result in results)
        {
            trace.AddRange(result.Samples.Select(sample => string.Join(",",
                result.Candidate.Id,
                sample.Step,
                F(sample.Seconds),
                sample.AnyTrip,
                sample.BreakerClosed,
                F(sample.RequestMegawatts),
                F(sample.GrossMegawatts),
                F(sample.ShaftMegawatts),
                F(sample.RotorRpm),
                F(sample.AdmissionFlowKilogramsPerSecond),
                F(sample.ControlValvePercent))));
        }
        File.WriteAllLines(Path.Combine(directory, "03-one-second-trace.csv"), trace, Utf8WithoutBom);

        var legacy = results.Single(static item => item.Candidate.Id == "legacy-v2");
        var qualifyingCandidateCount = results.Count(static item => item.Candidate.Id != "legacy-v2" && item.Qualifies);
        var recommendation = results.FirstOrDefault(static item => item.Candidate.Id != "legacy-v2" && item.Qualifies);
        var summary = new[]
        {
            "=== 01-i5-synchronization-governor-stability-diagnostic ===",
            "This I.5 blocker diagnostic keeps the pre-synchronization-grid-loading@2 plant, hydraulics, grid coupling, protections and seed constant while varying only the turbine speed-controller gains. Exact v2 is not modified or reinterpreted.",
            $"legacy-v2-qualifies={legacy.Qualifies}; legacy-final-gross-mwe={F(legacy.Final.GrossMegawatts)}; legacy-final-rotor-rpm={F(legacy.Final.RotorRpm)}; legacy-stable-gross-violations={legacy.StableGrossViolations}; legacy-stable-rotor-violations={legacy.StableRotorViolations};",
            $"candidate-count={results.Count - 1}; qualifying-candidates={qualifyingCandidateCount}; recommended-candidate={(recommendation?.Candidate.Id ?? "NONE")};",
            recommendation is null
                ? "recommendation=NO-GOVERNOR-ONLY-CANDIDATE-QUALIFIED; do not weaken the long-journey floor; investigate the synchronization profile as a broader plant/control balance issue before I.5 closure."
                : $"recommendation=QUALIFY-NEW-EXACT-SYNCHRONIZATION-VERSION-USING-{recommendation.Candidate.Id}; preserve v1/v2 identities and validate the new version before restoring I.5 cumulative closure.",
            "i5-closure-status=BLOCKED-PENDING-SYNCHRONIZATION-STABILITY-QUALIFICATION; runtime-production-changed=False; exact-v2-changed=False; long-journey-floor-weakened=False;",
        };
        File.WriteAllLines(Path.Combine(directory, "01-i5-synchronization-governor-stability-diagnostic.summary.txt"), summary, Utf8WithoutBom);
    }

    private static string F(double value)
        => value.ToString("G17", CultureInfo.InvariantCulture);

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
        throw new DirectoryNotFoundException("Could not locate NuclearReactorSimulator.sln from the test output directory.");
    }

    private static string ReportDirectory()
        => Path.Combine(FindRepositoryRoot(), "artifacts", "i5-synchronization-governor-stability-diagnostic");

    private static void ResetReportDirectory()
    {
        var directory = ReportDirectory();
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
        Directory.CreateDirectory(directory);
        File.WriteAllText(
            Path.Combine(directory, "00-progress.txt"),
            $"{DateTimeOffset.UtcNow:O} I.5 synchronization governor stability diagnostic started{Environment.NewLine}",
            Utf8WithoutBom);
    }

    private sealed record GovernorCandidate(
        string Id,
        double ProportionalGain,
        double IntegralGainPerSecond,
        double DerivativeGainSeconds,
        string Note);

    private sealed record DiagnosticSample(
        int Step,
        double Seconds,
        bool AnyTrip,
        bool BreakerClosed,
        double RequestMegawatts,
        double GrossMegawatts,
        double ShaftMegawatts,
        double RotorRpm,
        double AdmissionFlowKilogramsPerSecond,
        double ControlValvePercent);

    private sealed record CandidateResult(
        GovernorCandidate Candidate,
        double InitialLoadedElectricalMegawatts,
        IReadOnlyList<DiagnosticSample> Samples,
        int TripSteps,
        int BreakerOpenSteps,
        int RequestViolationSteps,
        int ShaftViolationSteps,
        int ReverseAdmissionSteps,
        int StableGrossViolations,
        int StableRotorViolations,
        int StableShaftViolations,
        int StableAdmissionReverseViolations,
        double MinGrossMegawatts,
        double MinShaftMegawatts,
        double MinRotorRpm,
        double MaxRotorRpm,
        DiagnosticSample Final,
        bool Qualifies);
}
