using System.Globalization;
using System.Text;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor.Neutronics;
using NuclearReactorSimulator.Simulation.Physics.Reactor.Neutronics;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Physics.Reactor.Neutronics;

/// <summary>
/// M10 Final VR1 independent point-kinetics model assessment.
/// The reference path is a test-only adaptive Dormand-Prince 5(4) implementation and never calls PointKineticsSolver.
/// Production is evaluated separately at frozen caller timesteps against the reference trajectories.
/// </summary>
public sealed class M10FinalPhysicalReferenceVr1PointKineticsBenchmarkTests
{
    private const string OptInEnvironmentVariable = "NRS_M10_FINAL_PHYSICAL_REFERENCE_VR1";
    private const string ArtifactDirectoryRelative = "artifacts/m10-final-physical-reference-vr1";

    private const double ReferenceRelativeTolerance = 1e-10d;
    private const double ReferenceAbsoluteTolerance = 1e-12d;
    private const double ReferenceMaximumStepSeconds = 1e-3d;
    private const double ReferenceMinimumStepSeconds = 1e-10d;
    private const double OneGroupSelfCheckMaximumRelativeError = 1e-8d;

    private const double ConcordantNeutronMaximumRelativeError = 0.0025d;
    private const double ConcordantPrecursorMaximumRelativeError = 0.005d;
    private const double BoundedNeutronMaximumRelativeError = 0.01d;
    private const double BoundedPrecursorMaximumRelativeError = 0.02d;
    private const double ZeroReactivityMaximumRelativeDrift = 1e-10d;

    private static readonly double[] ProductionCallerStepsSeconds = [0.01d, 0.005d, 0.0025d];
    private static readonly double[] BenchmarkSampleSeconds = [0d, 0.01d, 0.1d, 0.5d, 1d, 5d, 20d, 60d];
    private static readonly double[] OneGroupSelfCheckSampleSeconds = [0.01d, 0.1d, 0.5d, 1d, 10d];

    private static readonly ReferenceKineticsDefinition SixGroupDefinition = new(
        2e-5d,
        [
            new ReferenceGroup("1", 0.000266d, 0.0127d),
            new ReferenceGroup("2", 0.001491d, 0.0317d),
            new ReferenceGroup("3", 0.001316d, 0.115d),
            new ReferenceGroup("4", 0.002849d, 0.311d),
            new ReferenceGroup("5", 0.000896d, 1.4d),
            new ReferenceGroup("6", 0.000182d, 3.87d),
        ]);

    private static readonly ReactivityCase[] ReactivityCases =
    [
        new("VR1-K0", 0d, 0d),
        new("VR1-KP25", 0.25d, 0.00175d),
        new("VR1-KP50", 0.50d, 0.0035d),
        new("VR1-KN50", -0.50d, -0.0035d),
        new("VR1-KN100", -1.00d, -0.007d),
    ];

    [Fact(Explicit = true)]
    [Trait("Category", "M10FinalPhysicalReferenceVR1")]
    public void GenericPointKineticsSolver_IsAssessedAgainstIndependentHebertSixGroupReference()
    {
        RequireOptIn();
        var artifactDirectory = ResetArtifactDirectory();

        WriteSourceProvenanceManifest(artifactDirectory);
        WriteFrozenInputs(artifactDirectory);

        var selfCheckRows = RunOneGroupReferenceSelfCheck();
        WriteOneGroupReferenceSelfCheck(artifactDirectory, selfCheckRows);
        var selfCheckMaximum = selfCheckRows.Max(static row => row.RelativeError);

        if (selfCheckMaximum > OneGroupSelfCheckMaximumRelativeError)
        {
            WriteAssessmentSummary(
                artifactDirectory,
                "REFERENCE-HARNESS-FAIL",
                gatePasses: false,
                selfCheckMaximum,
                zeroReactivityDrift: double.NaN,
                primaryNeutronMaximum: double.NaN,
                primaryPrecursorMaximum: double.NaN,
                deterministicRepeat: false,
                refinementDisposition: "NOT-EVALUATED",
                nextAuthorizedGate: "NONE-RETURN-VR1-ARTIFACTS");

            Assert.Fail(
                $"VR1 independent reference harness self-check exceeded the frozen {OneGroupSelfCheckMaximumRelativeError:R} relative-error ceiling: {selfCheckMaximum:R}.");
        }

        var referenceRows = RunSixGroupReference();
        WriteReferenceTrajectory(artifactDirectory, referenceRows);

        var productionRuns = new List<ProductionRun>();
        var deterministicRepeat = true;
        foreach (var stepSeconds in ProductionCallerStepsSeconds)
        {
            var first = RunProduction(stepSeconds);
            var repeat = RunProduction(stepSeconds);
            deterministicRepeat &= ProductionRunsAreExactlyEqual(first, repeat);
            productionRuns.Add(first);
        }

        WriteProductionTrajectory(artifactDirectory, productionRuns);

        var errorRows = BuildErrorRows(referenceRows, productionRuns);
        WriteErrorMap(artifactDirectory, errorRows);

        var summaries = BuildStepSummaries(errorRows, productionRuns);
        WriteRefinementSummary(artifactDirectory, summaries);
        WriteDeterministicRepeat(artifactDirectory, deterministicRepeat);

        var primary = summaries.Single(static row => NearlyEqual(row.CallerStepSeconds, 0.01d));
        var zeroDrift = CalculateZeroReactivityDrift(productionRuns.Single(static run => NearlyEqual(run.CallerStepSeconds, 0.01d)));
        var refinement = EvaluateRefinement(summaries);
        var classification = Classify(primary, zeroDrift, deterministicRepeat, refinement);
        var gatePasses = classification is "REFERENCE-CONCORDANT" or "BOUNDED-NUMERICAL-DISCREPANCY";
        var nextAuthorizedGate = gatePasses
            ? "VR2-IAPWS-IF97-Water-Steam-Error-Map"
            : "NONE-RETURN-VR1-ARTIFACTS";

        WriteImpactAndKnownLimitations(artifactDirectory, classification);
        WriteAssessmentSummary(
            artifactDirectory,
            classification,
            gatePasses,
            selfCheckMaximum,
            zeroDrift,
            primary.MaximumNeutronRelativeError,
            primary.MaximumPrecursorRelativeError,
            deterministicRepeat,
            refinement,
            nextAuthorizedGate);

        Assert.True(deterministicRepeat, "VR1 production repeat must be bitwise deterministic for every frozen caller step.");
        Assert.True(
            gatePasses,
            $"VR1 classification was {classification}; return the complete artifact folder before changing point-kinetics physics or proceeding to VR2.");
    }

    private static IReadOnlyList<SelfCheckRow> RunOneGroupReferenceSelfCheck()
    {
        var definition = new ReferenceKineticsDefinition(
            1e-4d,
            [new ReferenceGroup("1", 0.0065d, 0.0766d)]);
        const double rho = 0.0025d;
        var reference = IndependentReferenceIntegrator.Integrate(definition, rho, OneGroupSelfCheckSampleSeconds);
        var rows = new List<SelfCheckRow>(OneGroupSelfCheckSampleSeconds.Length);

        foreach (var timeSeconds in OneGroupSelfCheckSampleSeconds)
        {
            var numerical = reference.Single(row => NearlyEqual(row.TimeSeconds, timeSeconds)).Values[0];
            var analytical = OneGroupAnalyticalNeutronPopulation(definition, rho, timeSeconds);
            rows.Add(new SelfCheckRow(
                timeSeconds,
                analytical,
                numerical,
                Math.Abs(numerical - analytical),
                RelativeError(analytical, numerical)));
        }

        return rows;
    }

    private static double OneGroupAnalyticalNeutronPopulation(
        ReferenceKineticsDefinition definition,
        double reactivityDeltaKOverK,
        double timeSeconds)
    {
        var group = Assert.Single(definition.Groups);
        var lambda = group.DecayConstantPerSecond;
        var beta = group.Beta;
        var promptLifetime = definition.PromptNeutronGenerationTimeSeconds;
        var a = (reactivityDeltaKOverK - beta) / promptLifetime;
        var b = beta / promptLifetime;

        var trace = a - lambda;
        var determinant = -lambda * (a + b);
        var discriminant = (trace * trace) - (4d * determinant);
        var root = Math.Sqrt(discriminant);
        var r1 = 0.5d * (trace + root);
        var r2 = 0.5d * (trace - root);

        const double n0 = 1d;
        var initialDerivative = reactivityDeltaKOverK / promptLifetime * n0;
        var firstCoefficient = (initialDerivative - (r2 * n0)) / (r1 - r2);
        var secondCoefficient = ((r1 * n0) - initialDerivative) / (r1 - r2);

        return (firstCoefficient * Math.Exp(r1 * timeSeconds))
            + (secondCoefficient * Math.Exp(r2 * timeSeconds));
    }

    private static IReadOnlyList<ReferenceTrajectoryRow> RunSixGroupReference()
    {
        var rows = new List<ReferenceTrajectoryRow>();
        foreach (var reactivityCase in ReactivityCases)
        {
            var trajectory = IndependentReferenceIntegrator.Integrate(
                SixGroupDefinition,
                reactivityCase.ReactivityDeltaKOverK,
                BenchmarkSampleSeconds);

            foreach (var sample in trajectory)
            {
                rows.Add(new ReferenceTrajectoryRow(
                    reactivityCase.Id,
                    reactivityCase.Dollars,
                    reactivityCase.ReactivityDeltaKOverK,
                    sample.TimeSeconds,
                    sample.Values[0],
                    sample.Values[1..]));
            }
        }

        return rows;
    }

    private static ProductionRun RunProduction(double callerStepSeconds)
    {
        var parameters = CreateProductionParameters();
        var solver = new PointKineticsSolver(parameters);
        var caseRuns = new List<ProductionCaseRun>(ReactivityCases.Length);

        foreach (var reactivityCase in ReactivityCases)
        {
            var state = PointKineticsState.CreateCriticalEquilibrium(parameters, NeutronPopulation.Reference);
            var reactivity = Reactivity.FromDeltaKOverK(reactivityCase.ReactivityDeltaKOverK);
            var samples = new List<ProductionTrajectoryRow>(BenchmarkSampleSeconds.Length);
            var completedSteps = 0;

            foreach (var targetSeconds in BenchmarkSampleSeconds)
            {
                var targetSteps = checked((int)Math.Round(targetSeconds / callerStepSeconds, MidpointRounding.AwayFromZero));
                var reconstructedTime = targetSteps * callerStepSeconds;
                if (Math.Abs(reconstructedTime - targetSeconds) > 1e-12d)
                {
                    throw new InvalidOperationException(
                        $"Frozen VR1 sample time {targetSeconds:R} s is not an integer multiple of caller step {callerStepSeconds:R} s.");
                }

                while (completedSteps < targetSteps)
                {
                    state = solver.Step(state, reactivity, TimeSpan.FromSeconds(callerStepSeconds));
                    completedSteps++;
                }

                samples.Add(new ProductionTrajectoryRow(
                    reactivityCase.Id,
                    targetSeconds,
                    state.NeutronPopulation.Relative,
                    state.DelayedNeutronGroups.Select(static group => group.PrecursorPopulation.Relative).ToArray()));
            }

            caseRuns.Add(new ProductionCaseRun(reactivityCase, samples));
        }

        return new ProductionRun(callerStepSeconds, caseRuns);
    }

    private static PointKineticsParameters CreateProductionParameters()
        => new(
            TimeSpan.FromSeconds(SixGroupDefinition.PromptNeutronGenerationTimeSeconds),
            SixGroupDefinition.Groups.Select(static group => new DelayedNeutronGroupDefinition(
                group.Id,
                DelayedNeutronFraction.FromFraction(group.Beta),
                DecayConstant.FromPerSecond(group.DecayConstantPerSecond))));

    private static bool ProductionRunsAreExactlyEqual(ProductionRun left, ProductionRun right)
    {
        if (left.CallerStepSeconds != right.CallerStepSeconds || left.Cases.Count != right.Cases.Count)
        {
            return false;
        }

        for (var caseIndex = 0; caseIndex < left.Cases.Count; caseIndex++)
        {
            var leftCase = left.Cases[caseIndex];
            var rightCase = right.Cases[caseIndex];
            if (leftCase.Definition != rightCase.Definition || leftCase.Samples.Count != rightCase.Samples.Count)
            {
                return false;
            }

            for (var sampleIndex = 0; sampleIndex < leftCase.Samples.Count; sampleIndex++)
            {
                var leftSample = leftCase.Samples[sampleIndex];
                var rightSample = rightCase.Samples[sampleIndex];
                if (leftSample.CaseId != rightSample.CaseId
                    || leftSample.TimeSeconds != rightSample.TimeSeconds
                    || leftSample.NeutronPopulation != rightSample.NeutronPopulation
                    || !leftSample.Precursors.SequenceEqual(rightSample.Precursors))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static IReadOnlyList<ErrorRow> BuildErrorRows(
        IReadOnlyList<ReferenceTrajectoryRow> referenceRows,
        IReadOnlyList<ProductionRun> productionRuns)
    {
        var rows = new List<ErrorRow>();

        foreach (var productionRun in productionRuns)
        {
            foreach (var caseRun in productionRun.Cases)
            {
                foreach (var production in caseRun.Samples)
                {
                    var reference = referenceRows.Single(row =>
                        row.CaseId == production.CaseId && NearlyEqual(row.TimeSeconds, production.TimeSeconds));

                    rows.Add(new ErrorRow(
                        productionRun.CallerStepSeconds,
                        production.CaseId,
                        production.TimeSeconds,
                        "neutron",
                        reference.NeutronPopulation,
                        production.NeutronPopulation,
                        Math.Abs(production.NeutronPopulation - reference.NeutronPopulation),
                        RelativeError(reference.NeutronPopulation, production.NeutronPopulation)));

                    for (var index = 0; index < production.Precursors.Length; index++)
                    {
                        rows.Add(new ErrorRow(
                            productionRun.CallerStepSeconds,
                            production.CaseId,
                            production.TimeSeconds,
                            $"precursor-{index + 1}",
                            reference.Precursors[index],
                            production.Precursors[index],
                            Math.Abs(production.Precursors[index] - reference.Precursors[index]),
                            RelativeError(reference.Precursors[index], production.Precursors[index])));
                    }
                }
            }
        }

        return rows;
    }

    private static IReadOnlyList<StepSummary> BuildStepSummaries(
        IReadOnlyList<ErrorRow> errorRows,
        IReadOnlyList<ProductionRun> productionRuns)
    {
        var rows = new List<StepSummary>();
        foreach (var run in productionRuns)
        {
            var stepErrors = errorRows.Where(row => NearlyEqual(row.CallerStepSeconds, run.CallerStepSeconds)).ToArray();
            var neutronMaximum = stepErrors
                .Where(static row => row.Component == "neutron" && row.TimeSeconds > 0d)
                .Max(static row => row.RelativeError);
            var precursorMaximum = stepErrors
                .Where(static row => row.Component.StartsWith("precursor-", StringComparison.Ordinal) && row.TimeSeconds > 0d)
                .Max(static row => row.RelativeError);
            var zeroDrift = CalculateZeroReactivityDrift(run);

            rows.Add(new StepSummary(run.CallerStepSeconds, neutronMaximum, precursorMaximum, zeroDrift));
        }

        return rows;
    }

    private static double CalculateZeroReactivityDrift(ProductionRun run)
    {
        var zero = run.Cases.Single(static item => item.Definition.Id == "VR1-K0");
        return zero.Samples.Max(static sample => Math.Abs(sample.NeutronPopulation - 1d));
    }

    private static string EvaluateRefinement(IReadOnlyList<StepSummary> summaries)
    {
        var ten = summaries.Single(static row => NearlyEqual(row.CallerStepSeconds, 0.01d));
        var five = summaries.Single(static row => NearlyEqual(row.CallerStepSeconds, 0.005d));
        var twoPointFive = summaries.Single(static row => NearlyEqual(row.CallerStepSeconds, 0.0025d));

        var tenScore = Math.Max(
            ten.MaximumNeutronRelativeError / ConcordantNeutronMaximumRelativeError,
            ten.MaximumPrecursorRelativeError / ConcordantPrecursorMaximumRelativeError);
        var fiveScore = Math.Max(
            five.MaximumNeutronRelativeError / ConcordantNeutronMaximumRelativeError,
            five.MaximumPrecursorRelativeError / ConcordantPrecursorMaximumRelativeError);
        var twoPointFiveScore = Math.Max(
            twoPointFive.MaximumNeutronRelativeError / ConcordantNeutronMaximumRelativeError,
            twoPointFive.MaximumPrecursorRelativeError / ConcordantPrecursorMaximumRelativeError);

        if (tenScore <= 1d)
        {
            return "ALREADY-REFERENCE-CONCORDANT-AT-10MS";
        }

        var nonDivergent = fiveScore <= tenScore * 1.05d && twoPointFiveScore <= fiveScore * 1.05d;
        var materiallyReduced = twoPointFiveScore <= tenScore * 0.80d;
        return nonDivergent && materiallyReduced
            ? "NON-DIVERGENT-MATERIALLY-REDUCED"
            : "REFINEMENT-NOT-SUFFICIENT";
    }

    private static string Classify(
        StepSummary primary,
        double zeroReactivityDrift,
        bool deterministicRepeat,
        string refinementDisposition)
    {
        if (!deterministicRepeat)
        {
            return "MODEL-DISCREPANCY";
        }

        if (zeroReactivityDrift <= ZeroReactivityMaximumRelativeDrift
            && primary.MaximumNeutronRelativeError <= ConcordantNeutronMaximumRelativeError
            && primary.MaximumPrecursorRelativeError <= ConcordantPrecursorMaximumRelativeError)
        {
            return "REFERENCE-CONCORDANT";
        }

        if (primary.MaximumNeutronRelativeError <= BoundedNeutronMaximumRelativeError
            && primary.MaximumPrecursorRelativeError <= BoundedPrecursorMaximumRelativeError
            && refinementDisposition == "NON-DIVERGENT-MATERIALLY-REDUCED")
        {
            return "BOUNDED-NUMERICAL-DISCREPANCY";
        }

        return "MODEL-DISCREPANCY";
    }

    private static void WriteSourceProvenanceManifest(string artifactDirectory)
    {
        File.WriteAllLines(
            Path.Combine(artifactDirectory, "01-source-provenance-manifest.txt"),
            [
                "vr-gate=VR1-Point-Kinetics-Independent-Benchmark",
                "primary-source=Alain-Hebert-Applied-Reactor-Physics-3e-2020",
                "source-section=5.4.1",
                "source-equations=5.240|5.251|5.252|5.256",
                "source-exercises=5.10|5.11",
                "reference-method=test-only-adaptive-Dormand-Prince-5-4-CSharp",
                "reference-calls-production-solver=False",
                "production-generates-reference=False",
                "new-runtime-dependency=False",
                "python-project-tooling=False",
                "claim-boundary=generic-point-kinetics-equation-implementation-only",
                "exact-v9-plant-parameter-calibration-claim=False",
            ]);
    }

    private static void WriteFrozenInputs(string artifactDirectory)
    {
        var path = Path.Combine(artifactDirectory, "02-frozen-parameter-inputs.csv");
        using var writer = new StreamWriter(path, false, new UTF8Encoding(false));
        writer.WriteLine("kind,id,beta,lambda_per_second,prompt_generation_time_seconds,dollars,rho_delta_k_over_k,value");
        foreach (var group in SixGroupDefinition.Groups)
        {
            writer.WriteLine(FormattableString.Invariant(
                $"group,{group.Id},{group.Beta:R},{group.DecayConstantPerSecond:R},{SixGroupDefinition.PromptNeutronGenerationTimeSeconds:R},,,"));
        }

        foreach (var reactivityCase in ReactivityCases)
        {
            writer.WriteLine(FormattableString.Invariant(
                $"reactivity,{reactivityCase.Id},,,,{reactivityCase.Dollars:R},{reactivityCase.ReactivityDeltaKOverK:R},"));
        }

        foreach (var sample in BenchmarkSampleSeconds)
        {
            writer.WriteLine(FormattableString.Invariant($"sample-time,,,,,,,{sample:R}"));
        }

        foreach (var step in ProductionCallerStepsSeconds)
        {
            writer.WriteLine(FormattableString.Invariant($"production-caller-step,,,,,,,{step:R}"));
        }
    }

    private static void WriteOneGroupReferenceSelfCheck(string artifactDirectory, IReadOnlyList<SelfCheckRow> rows)
    {
        var path = Path.Combine(artifactDirectory, "03-reference-selfcheck.csv");
        using var writer = new StreamWriter(path, false, new UTF8Encoding(false));
        writer.WriteLine("time_seconds,analytical_neutron,dopri54_neutron,absolute_error,relative_error");
        foreach (var row in rows)
        {
            writer.WriteLine(FormattableString.Invariant(
                $"{row.TimeSeconds:R},{row.AnalyticalNeutron:R},{row.NumericalNeutron:R},{row.AbsoluteError:R},{row.RelativeError:R}"));
        }
    }

    private static void WriteReferenceTrajectory(string artifactDirectory, IReadOnlyList<ReferenceTrajectoryRow> rows)
    {
        var path = Path.Combine(artifactDirectory, "04-reference-trajectory.csv");
        using var writer = new StreamWriter(path, false, new UTF8Encoding(false));
        writer.WriteLine("case_id,dollars,rho_delta_k_over_k,time_seconds,neutron,c1,c2,c3,c4,c5,c6");
        foreach (var row in rows)
        {
            writer.WriteLine(FormattableString.Invariant(
                $"{row.CaseId},{row.Dollars:R},{row.ReactivityDeltaKOverK:R},{row.TimeSeconds:R},{row.NeutronPopulation:R},{string.Join(",", row.Precursors.Select(static value => value.ToString("R", CultureInfo.InvariantCulture)))}"));
        }
    }

    private static void WriteProductionTrajectory(string artifactDirectory, IReadOnlyList<ProductionRun> runs)
    {
        var path = Path.Combine(artifactDirectory, "05-production-trajectory.csv");
        using var writer = new StreamWriter(path, false, new UTF8Encoding(false));
        writer.WriteLine("caller_step_seconds,case_id,dollars,rho_delta_k_over_k,time_seconds,neutron,c1,c2,c3,c4,c5,c6");
        foreach (var run in runs)
        {
            foreach (var caseRun in run.Cases)
            {
                foreach (var sample in caseRun.Samples)
                {
                    writer.WriteLine(FormattableString.Invariant(
                        $"{run.CallerStepSeconds:R},{caseRun.Definition.Id},{caseRun.Definition.Dollars:R},{caseRun.Definition.ReactivityDeltaKOverK:R},{sample.TimeSeconds:R},{sample.NeutronPopulation:R},{string.Join(",", sample.Precursors.Select(static value => value.ToString("R", CultureInfo.InvariantCulture)))}"));
                }
            }
        }
    }

    private static void WriteErrorMap(string artifactDirectory, IReadOnlyList<ErrorRow> rows)
    {
        var path = Path.Combine(artifactDirectory, "06-error-map.csv");
        using var writer = new StreamWriter(path, false, new UTF8Encoding(false));
        writer.WriteLine("caller_step_seconds,case_id,time_seconds,component,reference,production,absolute_error,relative_error");
        foreach (var row in rows)
        {
            writer.WriteLine(FormattableString.Invariant(
                $"{row.CallerStepSeconds:R},{row.CaseId},{row.TimeSeconds:R},{row.Component},{row.Reference:R},{row.Production:R},{row.AbsoluteError:R},{row.RelativeError:R}"));
        }
    }

    private static void WriteRefinementSummary(string artifactDirectory, IReadOnlyList<StepSummary> rows)
    {
        var path = Path.Combine(artifactDirectory, "07-refinement-summary.csv");
        using var writer = new StreamWriter(path, false, new UTF8Encoding(false));
        writer.WriteLine("caller_step_seconds,max_neutron_relative_error,max_precursor_relative_error,zero_reactivity_neutron_drift");
        foreach (var row in rows.OrderByDescending(static item => item.CallerStepSeconds))
        {
            writer.WriteLine(FormattableString.Invariant(
                $"{row.CallerStepSeconds:R},{row.MaximumNeutronRelativeError:R},{row.MaximumPrecursorRelativeError:R},{row.ZeroReactivityDrift:R}"));
        }
    }

    private static void WriteDeterministicRepeat(string artifactDirectory, bool deterministicRepeat)
    {
        File.WriteAllLines(
            Path.Combine(artifactDirectory, "08-deterministic-repeat.txt"),
            [
                $"deterministic-repeat={deterministicRepeat}",
                "comparison=bitwise-double-equality-across-all-frozen-cases-samples-and-caller-steps",
            ]);
    }

    private static void WriteAssessmentSummary(
        string artifactDirectory,
        string classification,
        bool gatePasses,
        double selfCheckMaximum,
        double zeroReactivityDrift,
        double primaryNeutronMaximum,
        double primaryPrecursorMaximum,
        bool deterministicRepeat,
        string refinementDisposition,
        string nextAuthorizedGate)
    {
        File.WriteAllLines(
            Path.Combine(artifactDirectory, "09-vr1-assessment-summary.txt"),
            [
                $"vr1-execution-completed=True",
                $"vr1-gate-passes={gatePasses}",
                $"vr1-classification={classification}",
                $"reference-selfcheck-max-relative-error={Format(selfCheckMaximum)}",
                $"reference-selfcheck-ceiling={OneGroupSelfCheckMaximumRelativeError:R}",
                $"production-10ms-zero-reactivity-max-relative-drift={Format(zeroReactivityDrift)}",
                $"production-10ms-max-neutron-relative-error={Format(primaryNeutronMaximum)}",
                $"production-10ms-max-precursor-relative-error={Format(primaryPrecursorMaximum)}",
                $"deterministic-repeat={deterministicRepeat}",
                $"refinement-disposition={refinementDisposition}",
                "production-src-changed=False",
                "pre-existing-tests-changed=False",
                "exact-v9-changed=False",
                "production-repair-authorized=False",
                "p3r1-authorized=False",
                "second-replacement-long-authorized=False",
                "generic-point-kinetics-model-assessed=True",
                "exact-v9-one-group-plant-parameterization-physically-validated=False",
                $"next-authorized-gate={nextAuthorizedGate}",
            ]);
    }

    private static void WriteImpactAndKnownLimitations(string artifactDirectory, string classification)
    {
        File.WriteAllLines(
            Path.Combine(artifactDirectory, "10-impact-known-limitations.txt"),
            [
                $"classification={classification}",
                "assessment-scope=generic-point-kinetics-equation-implementation",
                "exact-v9-solver-owner-active=True",
                "exact-v9-parameterization=REDUCED-ONE-GROUP",
                "exact-v9-parameter-calibration-assessed=False",
                "rbmk-specific-kinetics-validation-created=False",
                "space-time-kinetics-validation-created=False",
                "external-reference=Hebert-six-group-point-kinetics",
                "interpretation=VR1 can support implementation-level model assessment only; plant-specific calibration remains a separate fidelity question.",
            ]);
    }

    private static string ResetArtifactDirectory()
    {
        var root = FindRepositoryRoot();
        var path = Path.Combine(root, ArtifactDirectoryRelative.Replace('/', Path.DirectorySeparatorChar));
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }

        Directory.CreateDirectory(path);
        return path;
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

        throw new DirectoryNotFoundException("Could not locate NuclearReactorSimulator.sln from the test output directory.");
    }

    private static void RequireOptIn()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable(OptInEnvironmentVariable), "1", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"VR1 is explicit and fail-closed. Set {OptInEnvironmentVariable}=1 through the authorized runner.");
        }
    }

    private static double RelativeError(double reference, double candidate)
        => Math.Abs(candidate - reference) / Math.Max(Math.Abs(reference), 1e-300d);

    private static bool NearlyEqual(double left, double right)
        => Math.Abs(left - right) <= 1e-12d;

    private static string Format(double value)
        => double.IsFinite(value) ? value.ToString("R", CultureInfo.InvariantCulture) : "not-evaluated";

    private sealed record ReferenceGroup(string Id, double Beta, double DecayConstantPerSecond);

    private sealed record ReferenceKineticsDefinition(
        double PromptNeutronGenerationTimeSeconds,
        IReadOnlyList<ReferenceGroup> Groups)
    {
        public double BetaTotal => Groups.Sum(static group => group.Beta);
    }

    private sealed record ReactivityCase(string Id, double Dollars, double ReactivityDeltaKOverK);

    private sealed record ReferenceSample(double TimeSeconds, double[] Values);

    private sealed record SelfCheckRow(
        double TimeSeconds,
        double AnalyticalNeutron,
        double NumericalNeutron,
        double AbsoluteError,
        double RelativeError);

    private sealed record ReferenceTrajectoryRow(
        string CaseId,
        double Dollars,
        double ReactivityDeltaKOverK,
        double TimeSeconds,
        double NeutronPopulation,
        double[] Precursors);

    private sealed record ProductionTrajectoryRow(
        string CaseId,
        double TimeSeconds,
        double NeutronPopulation,
        double[] Precursors);

    private sealed record ProductionCaseRun(
        ReactivityCase Definition,
        IReadOnlyList<ProductionTrajectoryRow> Samples);

    private sealed record ProductionRun(
        double CallerStepSeconds,
        IReadOnlyList<ProductionCaseRun> Cases);

    private sealed record ErrorRow(
        double CallerStepSeconds,
        string CaseId,
        double TimeSeconds,
        string Component,
        double Reference,
        double Production,
        double AbsoluteError,
        double RelativeError);

    private sealed record StepSummary(
        double CallerStepSeconds,
        double MaximumNeutronRelativeError,
        double MaximumPrecursorRelativeError,
        double ZeroReactivityDrift);

    private static class IndependentReferenceIntegrator
    {
        private const double SafetyFactor = 0.9d;
        private const double MinimumScaleFactor = 0.2d;
        private const double MaximumScaleFactor = 5d;
        private const int MaximumAcceptedAndRejectedSteps = 20_000_000;

        public static IReadOnlyList<ReferenceSample> Integrate(
            ReferenceKineticsDefinition definition,
            double reactivityDeltaKOverK,
            IReadOnlyList<double> sampleTimesSeconds)
        {
            if (sampleTimesSeconds.Count == 0)
            {
                return [];
            }

            var values = CreateCriticalEquilibrium(definition);
            var samples = new List<ReferenceSample>(sampleTimesSeconds.Count);
            var time = 0d;
            var step = Math.Min(ReferenceMaximumStepSeconds, Math.Max(ReferenceMinimumStepSeconds, definition.PromptNeutronGenerationTimeSeconds));
            var attempts = 0;

            foreach (var target in sampleTimesSeconds)
            {
                if (target < time - 1e-14d)
                {
                    throw new ArgumentException("VR1 reference sample times must be monotonically increasing.", nameof(sampleTimesSeconds));
                }

                while (time < target - 1e-14d)
                {
                    attempts++;
                    if (attempts > MaximumAcceptedAndRejectedSteps)
                    {
                        throw new InvalidOperationException("VR1 independent reference integrator exceeded its deterministic step-attempt limit.");
                    }

                    var h = Math.Min(step, target - time);
                    var trial = DormandPrinceTrial(definition, reactivityDeltaKOverK, values, h);
                    var errorNorm = ErrorNorm(values, trial.FifthOrder, trial.ErrorEstimate);

                    if (errorNorm <= 1d)
                    {
                        values = trial.FifthOrder;
                        time += h;
                        step = NextStep(h, errorNorm);
                    }
                    else
                    {
                        if (h <= ReferenceMinimumStepSeconds * (1d + 1e-12d))
                        {
                            throw new InvalidOperationException(
                                $"VR1 independent reference integrator could not satisfy tolerance at the frozen minimum step {ReferenceMinimumStepSeconds:R} s.");
                        }

                        step = NextStep(h, errorNorm);
                    }
                }

                samples.Add(new ReferenceSample(target, (double[])values.Clone()));
            }

            return samples;
        }

        private static double[] CreateCriticalEquilibrium(ReferenceKineticsDefinition definition)
        {
            var values = new double[definition.Groups.Count + 1];
            values[0] = 1d;
            for (var index = 0; index < definition.Groups.Count; index++)
            {
                var group = definition.Groups[index];
                values[index + 1] = group.Beta
                    / (definition.PromptNeutronGenerationTimeSeconds * group.DecayConstantPerSecond);
            }

            return values;
        }

        private static TrialResult DormandPrinceTrial(
            ReferenceKineticsDefinition definition,
            double reactivityDeltaKOverK,
            double[] y,
            double h)
        {
            var k1 = Derivative(definition, reactivityDeltaKOverK, y);
            var k2 = Derivative(definition, reactivityDeltaKOverK, Combine(y, h, (1d / 5d, k1)));
            var k3 = Derivative(definition, reactivityDeltaKOverK, Combine(y, h,
                (3d / 40d, k1), (9d / 40d, k2)));
            var k4 = Derivative(definition, reactivityDeltaKOverK, Combine(y, h,
                (44d / 45d, k1), (-56d / 15d, k2), (32d / 9d, k3)));
            var k5 = Derivative(definition, reactivityDeltaKOverK, Combine(y, h,
                (19372d / 6561d, k1), (-25360d / 2187d, k2), (64448d / 6561d, k3), (-212d / 729d, k4)));
            var k6 = Derivative(definition, reactivityDeltaKOverK, Combine(y, h,
                (9017d / 3168d, k1), (-355d / 33d, k2), (46732d / 5247d, k3), (49d / 176d, k4), (-5103d / 18656d, k5)));

            var fifthOrder = Combine(y, h,
                (35d / 384d, k1),
                (500d / 1113d, k3),
                (125d / 192d, k4),
                (-2187d / 6784d, k5),
                (11d / 84d, k6));
            var k7 = Derivative(definition, reactivityDeltaKOverK, fifthOrder);

            var fourthOrder = Combine(y, h,
                (5179d / 57600d, k1),
                (7571d / 16695d, k3),
                (393d / 640d, k4),
                (-92097d / 339200d, k5),
                (187d / 2100d, k6),
                (1d / 40d, k7));

            var error = new double[y.Length];
            for (var index = 0; index < y.Length; index++)
            {
                error[index] = fifthOrder[index] - fourthOrder[index];
                if (!double.IsFinite(fifthOrder[index]) || fifthOrder[index] < 0d)
                {
                    throw new InvalidOperationException(
                        $"VR1 independent reference component {index} became invalid ({fifthOrder[index]:R}).");
                }
            }

            return new TrialResult(fifthOrder, error);
        }

        private static double[] Derivative(
            ReferenceKineticsDefinition definition,
            double reactivityDeltaKOverK,
            IReadOnlyList<double> values)
        {
            var derivative = new double[values.Count];
            var neutron = values[0];
            var neutronDerivative = (reactivityDeltaKOverK - definition.BetaTotal)
                / definition.PromptNeutronGenerationTimeSeconds * neutron;

            for (var index = 0; index < definition.Groups.Count; index++)
            {
                var group = definition.Groups[index];
                var precursor = values[index + 1];
                neutronDerivative += group.DecayConstantPerSecond * precursor;
                derivative[index + 1] = group.Beta / definition.PromptNeutronGenerationTimeSeconds * neutron
                    - group.DecayConstantPerSecond * precursor;
            }

            derivative[0] = neutronDerivative;
            return derivative;
        }

        private static double[] Combine(
            IReadOnlyList<double> baseline,
            double h,
            params (double Coefficient, double[] Derivative)[] terms)
        {
            var result = new double[baseline.Count];
            for (var index = 0; index < result.Length; index++)
            {
                var value = baseline[index];
                foreach (var term in terms)
                {
                    value += h * term.Coefficient * term.Derivative[index];
                }

                result[index] = value;
            }

            return result;
        }

        private static double ErrorNorm(
            IReadOnlyList<double> baseline,
            IReadOnlyList<double> candidate,
            IReadOnlyList<double> error)
        {
            var maximum = 0d;
            for (var index = 0; index < baseline.Count; index++)
            {
                var scale = ReferenceAbsoluteTolerance
                    + ReferenceRelativeTolerance * Math.Max(Math.Abs(baseline[index]), Math.Abs(candidate[index]));
                maximum = Math.Max(maximum, Math.Abs(error[index]) / scale);
            }

            return maximum;
        }

        private static double NextStep(double currentStep, double errorNorm)
        {
            var scale = errorNorm <= double.Epsilon
                ? MaximumScaleFactor
                : SafetyFactor * Math.Pow(errorNorm, -0.2d);
            scale = Math.Clamp(scale, MinimumScaleFactor, MaximumScaleFactor);
            return Math.Clamp(currentStep * scale, ReferenceMinimumStepSeconds, ReferenceMaximumStepSeconds);
        }

        private sealed record TrialResult(double[] FifthOrder, double[] ErrorEstimate);
    }
}
