using System.Globalization;
using System.Text;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios.Synchronization;
using NuclearReactorSimulator.Application.Scenarios.Training;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Simulation.Physics.Electrical;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay;

/// <summary>
/// M10.9.4.1-E.3.1 evidence-only audit. These explicit tests record signed current-v2 electrical trajectories before
/// reverse-power, supervised-underfrequency or loss-of-synchronism thresholds are selected. They intentionally add no
/// protection function and assert only deterministic physical/architectural invariants.
/// </summary>
public sealed class ElectricalProtectionTrajectoryAuditTests
{
    private static readonly TimeSpan RuntimeStep = TimeSpan.FromMilliseconds(10d);
    private const int SampleStride = 10;

    [Fact(Explicit = true)]
    [Trait("Category", "ElectricalProtectionTrajectoryAudit")]
    public void NormalGenerationAndLoadStep_RecordsBreakerClosedOperatingEnvelope()
    {
        var engine = CreateDesktopEngine();
        var samples = new List<TrajectorySample>();
        CaptureRuntime(samples, "steady-export", engine);

        AdvanceRuntime(engine, samples, "steady-export", 1_000);
        QueueGeneratorCommand(engine, ControlRoomCommandKind.GeneratorLoadLower);
        AdvanceRuntime(engine, samples, "load-lower-to-zero", 2_000);
        QueueGeneratorCommand(engine, ControlRoomCommandKind.GeneratorLoadRaise);
        AdvanceRuntime(engine, samples, "load-raise-to-five", 2_000);

        WriteRuntimeReport(
            "01-normal-generation-load-step",
            "Normal breaker-closed generation and a 5→0→5 MWe operator request trajectory.",
            samples);

        Assert.NotEmpty(samples);
        Assert.Contains(samples, static sample => Math.Abs(sample.RequestedPowerMegawatts) <= 1e-9d);
        Assert.Contains(samples, static sample => Math.Abs(sample.RequestedPowerMegawatts - 5d) <= 1e-9d);
        Assert.All(samples, static sample =>
        {
            Assert.True(sample.BreakerClosed, Diagnostic(sample));
            Assert.True(double.IsFinite(sample.GridExchangeMegawatts), Diagnostic(sample));
            Assert.True(double.IsFinite(sample.FrequencyHertz), Diagnostic(sample));
            Assert.InRange(sample.AbsolutePhaseDifferenceDegrees, 0d, 180d);
            Assert.False(sample.GeneratorTripActive, Diagnostic(sample));
        });
    }

    [Fact(Explicit = true)]
    [Trait("Category", "ElectricalProtectionTrajectoryAudit")]
    public void TurbineTripWithBreakerClosed_RecordsReversePowerMotoringTrajectory()
    {
        var engine = CreateDesktopEngine();
        var samples = new List<TrajectorySample>();
        AdvanceRuntime(engine, samples, "pre-trip-steady-export", 500);

        engine.QueueOperatorCommand(new ControlRoomCommand(ControlRoomCommandKind.TurbineTrip));
        QueueGeneratorCommand(engine, ControlRoomCommandKind.GeneratorLoadLower);
        AdvanceRuntime(engine, samples, "turbine-trip-zero-request-breaker-closed", 3_000);

        WriteRuntimeReport(
            "02-turbine-trip-reverse-power",
            "Prime-mover trip with the electrical request lowered to zero while the generator breaker remains closed, exposing motoring/reverse-power evidence without a breaker-open shortcut.",
            samples);

        Assert.Contains(samples, static sample => Math.Abs(sample.RequestedPowerMegawatts) <= 1e-9d);
        Assert.Contains(samples, static sample => sample.GridExchangeMegawatts < -0.001d);
        Assert.All(samples, static sample =>
        {
            Assert.True(sample.BreakerClosed, Diagnostic(sample));
            Assert.True(double.IsFinite(sample.GridExchangeMegawatts), Diagnostic(sample));
            Assert.True(double.IsFinite(sample.FrequencyHertz), Diagnostic(sample));
            Assert.InRange(sample.AbsolutePhaseDifferenceDegrees, 0d, 180d);
            Assert.False(sample.GeneratorTripActive, Diagnostic(sample));
        });
    }

    [Fact(Explicit = true)]
    [Trait("Category", "ElectricalProtectionTrajectoryAudit")]
    public void DisconnectedCoastdown_RecordsUnderfrequencyWithoutGeneratorTripEligibility()
    {
        var engine = CreateSynchronizationEngine();
        var samples = new List<TrajectorySample>();
        CaptureRuntime(samples, "breaker-open-pre-trip", engine);
        var initialFrequency = samples[0].FrequencyHertz;

        engine.QueueOperatorCommand(new ControlRoomCommand(ControlRoomCommandKind.TurbineTrip));
        AdvanceRuntime(engine, samples, "breaker-open-coastdown", 3_000);

        WriteRuntimeReport(
            "03-disconnected-underfrequency-coastdown",
            "Breaker-open turbine coastdown demonstrating why generator underfrequency must be supervised by connection/load state.",
            samples);

        Assert.All(samples, static sample =>
        {
            Assert.False(sample.BreakerClosed, Diagnostic(sample));
            Assert.True(double.IsFinite(sample.FrequencyHertz), Diagnostic(sample));
            Assert.False(sample.GeneratorTripActive, Diagnostic(sample));
        });
        Assert.True(samples.Min(static sample => sample.FrequencyHertz) < initialFrequency - 0.001d, BuildSummary(samples));
    }

    [Fact(Explicit = true)]
    [Trait("Category", "ElectricalProtectionTrajectoryAudit")]
    public void BreakerClosedPhaseOffsetSweep_RecordsReducedOrderCouplingEnvelope()
    {
        var source = CreateDesktopEngine();
        var definition = source.CurrentState.PlantDefinition.GeneratorGridSystem;
        var generator = Assert.Single(definition.Generators);
        var baseState = source.CurrentState.PlantState;
        var inputs = source.PersistentInputs.PlantInputs.GeneratorGridInputs;
        var solver = new GeneratorGridSolver(definition, new SimplifiedWaterSteamThermodynamicModel());
        var offsets = new[] { -135d, -90d, -45d, -15d, 15d, 45d, 90d, 135d };
        var summaries = new List<PhaseOffsetSummary>();

        foreach (var offsetDegrees in offsets)
        {
            var plantState = baseState.PlantState;
            var turbineState = baseState.TurbineState;
            var gridPhase = baseState.ElectricalState.GridPhaseAngle;
            var electricalState = new GeneratorGridState(
                definition,
                gridPhase,
                new[]
                {
                    new SynchronousGeneratorState(
                        generator.Id,
                        gridPhase.Advance(offsetDegrees * Math.PI / 180d),
                        breakerClosed: true),
                });
            var rows = new List<PhaseSweepSample>();
            var previousLead = offsetDegrees;
            var wrapCount = 0;

            for (var step = 1; step <= 500; step++)
            {
                var result = solver.Step(plantState, turbineState, electricalState, inputs, RuntimeStep);
                plantState = result.CandidatePlantState;
                turbineState = result.CandidateTurbineState;
                electricalState = result.CandidateElectricalState;

                var snapshot = Assert.Single(result.Snapshot.Generators);
                var signedLead = SignedShortestPhaseLeadDegrees(
                    snapshot.FinalElectricalPhaseAngle,
                    result.Snapshot.Grid.FinalPhaseAngle);
                if (Math.Abs(signedLead - previousLead) > 180d)
                {
                    wrapCount++;
                }

                previousLead = signedLead;
                if (step % SampleStride == 0 || step == 500)
                {
                    rows.Add(new PhaseSweepSample(
                        step,
                        step * RuntimeStep.TotalSeconds,
                        offsetDegrees,
                        signedLead,
                        snapshot.FinalPhaseDifference.Degrees,
                        snapshot.FinalElectricalFrequency.Hertz,
                        snapshot.FinalElectricalFrequency.Hertz - result.Snapshot.Grid.Frequency.Hertz,
                        snapshot.ElectricalOutputPower.Megawatts,
                        snapshot.ConversionLossPower.Megawatts,
                        snapshot.BreakerFinallyClosed));
                }
            }

            WritePhaseSweepCsv(offsetDegrees, rows);

            Assert.All(rows, static row =>
            {
                Assert.True(row.BreakerClosed, PhaseDiagnostic(row));
                Assert.True(double.IsFinite(row.SignedPhaseLeadDegrees), PhaseDiagnostic(row));
                Assert.True(double.IsFinite(row.FrequencySlipHertz), PhaseDiagnostic(row));
                Assert.True(double.IsFinite(row.GridExchangeMegawatts), PhaseDiagnostic(row));
                Assert.True(row.ConversionLossMegawatts >= 0d, PhaseDiagnostic(row));
            });

            var final = rows[^1];
            var maximumPhase = rows.MaxBy(static row => Math.Abs(row.SignedPhaseLeadDegrees))!;
            var maximumSlip = rows.MaxBy(static row => Math.Abs(row.FrequencySlipHertz))!;
            summaries.Add(new PhaseOffsetSummary(
                offsetDegrees,
                final.SignedPhaseLeadDegrees,
                Math.Abs(maximumPhase.SignedPhaseLeadDegrees),
                maximumPhase.Seconds,
                Math.Abs(maximumSlip.FrequencySlipHertz),
                maximumSlip.Seconds,
                rows.Min(static row => row.GridExchangeMegawatts),
                rows.Max(static row => row.GridExchangeMegawatts),
                wrapCount));
        }

        WritePhaseSweepSummary(summaries);
    }

    private static IntegratedAutomaticOperationRuntimeEngine CreateDesktopEngine()
        => Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            new DesktopSustainedGenerationInitialConditionFactory().CreateRuntimeEngine());

    private static IntegratedAutomaticOperationRuntimeEngine CreateSynchronizationEngine()
        => Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            new GridSynchronizationSustainedInitialConditionFactory().CreateRuntimeEngine());

    private static void QueueGeneratorCommand(
        IntegratedAutomaticOperationRuntimeEngine engine,
        ControlRoomCommandKind commandKind)
    {
        var generatorId = Assert.Single(engine.CurrentState.PlantDefinition.GeneratorGridSystem.Generators).Id;
        engine.QueueOperatorCommand(new ControlRoomCommand(
            commandKind,
            generatorId,
            ControlRoomCommandTargetKind.Generator));
    }

    private static void AdvanceRuntime(
        IntegratedAutomaticOperationRuntimeEngine engine,
        ICollection<TrajectorySample> samples,
        string segment,
        int stepCount)
    {
        for (var step = 1; step <= stepCount; step++)
        {
            engine.Step(ControlRoomRunState.Running);
            if (step % SampleStride == 0 || step == stepCount)
            {
                CaptureRuntime(samples, segment, engine);
            }
        }
    }

    private static void CaptureRuntime(
        ICollection<TrajectorySample> samples,
        string segment,
        IntegratedAutomaticOperationRuntimeEngine engine)
    {
        var canonical = engine.LatestCanonicalSnapshot.Control.ProtectedControl;
        var generatorGrid = canonical.FullPlant.IntegratedCycle.GeneratorGrid;
        var generator = Assert.Single(generatorGrid.Generators);
        samples.Add(new TrajectorySample(
            engine.LogicalStep,
            engine.LogicalStep * RuntimeStep.TotalSeconds,
            segment,
            generator.BreakerFinallyClosed,
            generator.RequestedElectricalPower.Megawatts,
            generator.ElectricalOutputPower.Megawatts,
            generator.MechanicalInputPower.Megawatts,
            generator.ConversionLossPower.Megawatts,
            generator.FinalElectricalFrequency.Hertz,
            generator.FinalElectricalFrequency.Hertz - generatorGrid.Grid.Frequency.Hertz,
            generator.FinalPhaseDifference.Degrees,
            SignedShortestPhaseLeadDegrees(generator.FinalElectricalPhaseAngle, generatorGrid.Grid.FinalPhaseAngle),
            canonical.Protection.TurbineTripActive,
            canonical.Protection.GeneratorTripActive));
    }

    private static double SignedShortestPhaseLeadDegrees(PhaseAngle generatorPhase, PhaseAngle gridPhase)
    {
        var difference = generatorPhase.Radians - gridPhase.Radians;
        var fullTurn = 2d * Math.PI;
        difference = (difference + Math.PI) % fullTurn;
        if (difference < 0d)
        {
            difference += fullTurn;
        }

        return (difference - Math.PI) * 180d / Math.PI;
    }

    private static void WriteRuntimeReport(string fileStem, string purpose, IReadOnlyList<TrajectorySample> samples)
    {
        var directory = EnsureReportDirectory();
        var csv = new StringBuilder();
        csv.AppendLine("logical_step,seconds,segment,breaker_closed,requested_power_mw,grid_exchange_mw,mechanical_exchange_mw,conversion_loss_mw,frequency_hz,frequency_slip_hz,absolute_phase_difference_deg,signed_phase_lead_deg,turbine_trip_active,generator_trip_active");
        foreach (var sample in samples)
        {
            csv.AppendLine(string.Join(",",
                sample.LogicalStep.ToString(CultureInfo.InvariantCulture),
                sample.Seconds.ToString("0.000", CultureInfo.InvariantCulture),
                EscapeCsv(sample.Segment),
                sample.BreakerClosed,
                sample.RequestedPowerMegawatts.ToString("0.000000", CultureInfo.InvariantCulture),
                sample.GridExchangeMegawatts.ToString("0.000000", CultureInfo.InvariantCulture),
                sample.MechanicalExchangeMegawatts.ToString("0.000000", CultureInfo.InvariantCulture),
                sample.ConversionLossMegawatts.ToString("0.000000", CultureInfo.InvariantCulture),
                sample.FrequencyHertz.ToString("0.000000", CultureInfo.InvariantCulture),
                sample.FrequencySlipHertz.ToString("0.000000", CultureInfo.InvariantCulture),
                sample.AbsolutePhaseDifferenceDegrees.ToString("0.000000", CultureInfo.InvariantCulture),
                sample.SignedPhaseLeadDegrees.ToString("0.000000", CultureInfo.InvariantCulture),
                sample.TurbineTripActive,
                sample.GeneratorTripActive));
        }

        File.WriteAllText(Path.Combine(directory, fileStem + ".csv"), csv.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        File.WriteAllText(
            Path.Combine(directory, fileStem + ".summary.txt"),
            string.Join(
                Environment.NewLine,
                $"=== {fileStem} ===",
                purpose,
                BuildSummary(samples),
                string.Empty),
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static string BuildSummary(IReadOnlyList<TrajectorySample> samples)
    {
        var minimumFrequency = samples.MinBy(static item => item.FrequencyHertz)!;
        var maximumPhase = samples.MaxBy(static item => item.AbsolutePhaseDifferenceDegrees)!;
        var negative = samples.Where(static item => item.GridExchangeMegawatts < 0d).ToArray();
        var negativeWindow = negative.Length == 0
            ? "negative-exchange=none"
            : string.Concat(
                FormattableString.Invariant(
                    $"negative-exchange-samples={negative.Length}; first-negative={negative[0].Seconds:0.000}s; "),
                FormattableString.Invariant(
                    $"last-negative={negative[^1].Seconds:0.000}s; sampled-span={negative[^1].Seconds - negative[0].Seconds:0.000}s"));

        return string.Concat(
            FormattableString.Invariant(
                $"samples={samples.Count}; seconds={samples.Min(static item => item.Seconds):0.000}..{samples.Max(static item => item.Seconds):0.000}; "),
            FormattableString.Invariant(
                $"request={samples.Min(static item => item.RequestedPowerMegawatts):0.000000}..{samples.Max(static item => item.RequestedPowerMegawatts):0.000000} MW; "),
            FormattableString.Invariant(
                $"grid-exchange={samples.Min(static item => item.GridExchangeMegawatts):0.000000}..{samples.Max(static item => item.GridExchangeMegawatts):0.000000} MW; "),
            FormattableString.Invariant(
                $"frequency={samples.Min(static item => item.FrequencyHertz):0.000000}..{samples.Max(static item => item.FrequencyHertz):0.000000} Hz; "),
            FormattableString.Invariant($"minimum-frequency-at={minimumFrequency.Seconds:0.000}s; "),
            FormattableString.Invariant(
                $"slip={samples.Min(static item => item.FrequencySlipHertz):0.000000}..{samples.Max(static item => item.FrequencySlipHertz):0.000000} Hz; "),
            FormattableString.Invariant(
                $"abs-phase-max={maximumPhase.AbsolutePhaseDifferenceDegrees:0.000000} deg at {maximumPhase.Seconds:0.000}s; "),
            FormattableString.Invariant(
                $"signed-phase={samples.Min(static item => item.SignedPhaseLeadDegrees):0.000000}..{samples.Max(static item => item.SignedPhaseLeadDegrees):0.000000} deg; "),
            FormattableString.Invariant(
                $"breaker-closed-samples={samples.Count(static item => item.BreakerClosed)}; generator-trip-samples={samples.Count(static item => item.GeneratorTripActive)}; "),
            negativeWindow);
    }

    private static void WritePhaseSweepCsv(double offsetDegrees, IReadOnlyList<PhaseSweepSample> rows)
    {
        var directory = EnsureReportDirectory();
        var csv = new StringBuilder();
        csv.AppendLine("step,seconds,initial_offset_deg,signed_phase_lead_deg,absolute_phase_difference_deg,frequency_hz,frequency_slip_hz,grid_exchange_mw,conversion_loss_mw,breaker_closed");
        foreach (var row in rows)
        {
            csv.AppendLine(string.Join(",",
                row.Step.ToString(CultureInfo.InvariantCulture),
                row.Seconds.ToString("0.000", CultureInfo.InvariantCulture),
                row.InitialOffsetDegrees.ToString("0.000", CultureInfo.InvariantCulture),
                row.SignedPhaseLeadDegrees.ToString("0.000000", CultureInfo.InvariantCulture),
                row.AbsolutePhaseDifferenceDegrees.ToString("0.000000", CultureInfo.InvariantCulture),
                row.FrequencyHertz.ToString("0.000000", CultureInfo.InvariantCulture),
                row.FrequencySlipHertz.ToString("0.000000", CultureInfo.InvariantCulture),
                row.GridExchangeMegawatts.ToString("0.000000", CultureInfo.InvariantCulture),
                row.ConversionLossMegawatts.ToString("0.000000", CultureInfo.InvariantCulture),
                row.BreakerClosed));
        }

        var suffix = offsetDegrees < 0d
            ? "minus" + Math.Abs(offsetDegrees).ToString("0", CultureInfo.InvariantCulture)
            : "plus" + offsetDegrees.ToString("0", CultureInfo.InvariantCulture);
        File.WriteAllText(
            Path.Combine(directory, $"04-phase-offset-{suffix}.csv"),
            csv.ToString(),
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static void WritePhaseSweepSummary(IReadOnlyList<PhaseOffsetSummary> summaries)
    {
        var directory = EnsureReportDirectory();
        var text = new StringBuilder();
        text.AppendLine("=== 04-breaker-closed-phase-offset-sweep ===");
        text.AppendLine("Synthetic breaker-closed phase offsets over the validated current-v2 physical state; evidence only, not a protection threshold.");
        text.AppendLine("initial_deg,final_deg,max_abs_deg,max_abs_at_s,max_abs_slip_hz,max_abs_slip_at_s,min_grid_exchange_mw,max_grid_exchange_mw,phase_wrap_count");
        foreach (var summary in summaries.OrderBy(static item => item.InitialOffsetDegrees))
        {
            text.AppendLine(string.Concat(
                FormattableString.Invariant(
                    $"{summary.InitialOffsetDegrees:0.000},{summary.FinalSignedLeadDegrees:0.000000},{summary.MaximumAbsoluteLeadDegrees:0.000000},"),
                FormattableString.Invariant(
                    $"{summary.MaximumAbsoluteLeadAtSeconds:0.000},{summary.MaximumAbsoluteFrequencySlipHertz:0.000000},"),
                FormattableString.Invariant(
                    $"{summary.MaximumAbsoluteFrequencySlipAtSeconds:0.000},{summary.MinimumGridExchangeMegawatts:0.000000},"),
                FormattableString.Invariant(
                    $"{summary.MaximumGridExchangeMegawatts:0.000000},{summary.PhaseWrapCount}")));
        }
        text.AppendLine();

        File.WriteAllText(
            Path.Combine(directory, "04-breaker-closed-phase-offset-sweep.summary.txt"),
            text.ToString(),
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static string EnsureReportDirectory()
    {
        var directory = Path.Combine(FindRepositoryRoot(), "artifacts", "e3-protection-trajectories");
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "NuclearReactorSimulator.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the NuclearReactorSimulator repository root from the test output directory.");
    }

    private static string EscapeCsv(string value)
        => '"' + value.Replace("\"", "\"\"") + '"';

    private static string Diagnostic(TrajectorySample sample)
        => string.Concat(
            FormattableString.Invariant(
                $"step={sample.LogicalStep}; t={sample.Seconds:0.000}s; segment={sample.Segment}; breaker={sample.BreakerClosed}; "),
            FormattableString.Invariant(
                $"request={sample.RequestedPowerMegawatts:0.000000} MW; exchange={sample.GridExchangeMegawatts:0.000000} MW; "),
            FormattableString.Invariant($"f={sample.FrequencyHertz:0.000000} Hz; "),
            FormattableString.Invariant(
                $"slip={sample.FrequencySlipHertz:0.000000} Hz; phase={sample.SignedPhaseLeadDegrees:0.000000} deg; "),
            FormattableString.Invariant($"generatorTrip={sample.GeneratorTripActive}"));

    private static string PhaseDiagnostic(PhaseSweepSample row)
        => string.Concat(
            FormattableString.Invariant(
                $"offset={row.InitialOffsetDegrees:0.000} deg; step={row.Step}; t={row.Seconds:0.000}s; "),
            FormattableString.Invariant(
                $"lead={row.SignedPhaseLeadDegrees:0.000000} deg; slip={row.FrequencySlipHertz:0.000000} Hz; "),
            FormattableString.Invariant(
                $"exchange={row.GridExchangeMegawatts:0.000000} MW; loss={row.ConversionLossMegawatts:0.000000} MW"));

    private sealed record TrajectorySample(
        long LogicalStep,
        double Seconds,
        string Segment,
        bool BreakerClosed,
        double RequestedPowerMegawatts,
        double GridExchangeMegawatts,
        double MechanicalExchangeMegawatts,
        double ConversionLossMegawatts,
        double FrequencyHertz,
        double FrequencySlipHertz,
        double AbsolutePhaseDifferenceDegrees,
        double SignedPhaseLeadDegrees,
        bool TurbineTripActive,
        bool GeneratorTripActive);

    private sealed record PhaseSweepSample(
        int Step,
        double Seconds,
        double InitialOffsetDegrees,
        double SignedPhaseLeadDegrees,
        double AbsolutePhaseDifferenceDegrees,
        double FrequencyHertz,
        double FrequencySlipHertz,
        double GridExchangeMegawatts,
        double ConversionLossMegawatts,
        bool BreakerClosed);

    private sealed record PhaseOffsetSummary(
        double InitialOffsetDegrees,
        double FinalSignedLeadDegrees,
        double MaximumAbsoluteLeadDegrees,
        double MaximumAbsoluteLeadAtSeconds,
        double MaximumAbsoluteFrequencySlipHertz,
        double MaximumAbsoluteFrequencySlipAtSeconds,
        double MinimumGridExchangeMegawatts,
        double MaximumGridExchangeMegawatts,
        int PhaseWrapCount);
}
