using System.Diagnostics;
using System.Text;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios.Training;
using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using NuclearReactorSimulator.Simulation.Plant;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay;

/// <summary>
/// M10.9.4.1-H.6 shadow-only corrector-envelope refinement. The H.4 P060/F040 trigger remains frozen.
/// The validated H.5 Hotfix 2 explicit 10 ms production trajectory is replayed as committed evidence,
/// and alternative Picard relaxation/iteration envelopes are evaluated only against the seven intervals
/// already selected by the H.4 trigger. No shadow candidate is ever committed.
/// </summary>
public sealed class HybridCorrectorRescueEnvelopeAuditTests
{
    private static readonly TimeSpan Step = TimeSpan.FromMilliseconds(10d);
    private static readonly Encoding Utf8WithoutBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    private const int IntervalCount = 500;
    private const double PressureTrigger = 0.060d;
    private const double FlowTriggerKilogramsPerSecond = 40d;

    private static readonly SemiImplicitHydraulicPrototypeOptions H4Primary = new(
        maximumIterations: 72,
        relaxationFactor: 0.15d,
        relativePressureTolerance: 1e-5d,
        absoluteFlowToleranceKilogramsPerSecond: 1e-2d);

    [Fact(Explicit = true)]
    [Trait("Category", "HybridCorrectorRescueEnvelopeAudit")]
    public void CurrentV2TriggeredIntervals_CharacterizeBoundedRescueEnvelopeWithoutProductionActivation()
    {
        var thermodynamics = new SimplifiedWaterSteamThermodynamicModel();
        var prototype = new SemiImplicitHydraulicPrototypeSolver(thermodynamics);
        var gate = new HybridSemiImplicitHydraulicGateSolver(thermodynamics);
        var reference = BuildReferenceTrajectory(prototype);
        var baseline = EvaluatePrimaryGate(reference, gate);

        // Freeze the H.5 Hotfix 2 user-validated evidence before searching a numerical rescue envelope.
        Assert.True(reference.Count == IntervalCount, $"Expected {IntervalCount} committed reference intervals but found {reference.Count}.");
        Assert.True(baseline.Count == 7, $"Expected the validated H.5 trigger count 7 but found {baseline.Count}.");
        Assert.True(baseline.Count(static item => item.PrimaryResult.Converged) == 5, "Expected the validated H.5 primary convergence count 5/7.");
        Assert.True(baseline.Count(static item => !item.PrimaryResult.Converged) == 2, "Expected the validated H.5 primary non-convergence count 2/7.");

        var profileRuns = BuildRescueProfiles()
            .Select(profile => EvaluateProfile(reference, baseline, prototype, profile))
            .ToArray();

        Assert.All(profileRuns, run =>
        {
            Assert.True(run.Events.Count == baseline.Count, $"Profile {run.Profile.Id} did not evaluate every triggered interval.");
            Assert.True(double.IsFinite(run.DeterministicAlwaysUseWorkRatio));
            Assert.InRange(run.DeterministicAlwaysUseWorkRatio, 1d, 4d);
            Assert.True(run.MaximumInventoryMassResidualKilograms <= 1e-6d, run.ToString());
            Assert.True(run.MaximumInventoryEnergyResidualJoules <= 1e-2d, run.ToString());
            Assert.True(run.MaximumHydraulicMassClosureKilogramsPerSecond <= 1e-8d, run.ToString());
            Assert.True(run.MaximumHydraulicEnergyOwnershipResidualWatts <= 1e-3d, run.ToString());
        });

        var selectedRescue = SelectRescueProfile(profileRuns);
        var ladder = EvaluateTwoTierLadder(reference, baseline, prototype, selectedRescue);
        var repeat = EvaluateTwoTierLadder(reference, baseline, prototype, selectedRescue);

        var deterministicRepeat = ladder.Events.SequenceEqual(repeat.Events);
        Assert.True(deterministicRepeat);
        Assert.False(ladder.ProductionHybridActive);
        Assert.False(ladder.ShadowCandidatesCommitted);

        WriteAuditReports(baseline, profileRuns, selectedRescue, ladder with { DeterministicRepeat = deterministicRepeat });
    }

    private static IReadOnlyList<CorrectorProfile> BuildRescueProfiles()
        => new[]
        {
            new CorrectorProfile("R015-I096", new SemiImplicitHydraulicPrototypeOptions(96, 0.15d, 1e-5d, 1e-2d)),
            new CorrectorProfile("R0125-I096", new SemiImplicitHydraulicPrototypeOptions(96, 0.125d, 1e-5d, 1e-2d)),
            new CorrectorProfile("R010-I096", new SemiImplicitHydraulicPrototypeOptions(96, 0.10d, 1e-5d, 1e-2d)),
            new CorrectorProfile("R010-I128", new SemiImplicitHydraulicPrototypeOptions(128, 0.10d, 1e-5d, 1e-2d)),
            new CorrectorProfile("R0075-I128", new SemiImplicitHydraulicPrototypeOptions(128, 0.075d, 1e-5d, 1e-2d)),
            new CorrectorProfile("R0075-I160", new SemiImplicitHydraulicPrototypeOptions(160, 0.075d, 1e-5d, 1e-2d)),
        };

    private static IReadOnlyList<ReferenceInterval> BuildReferenceTrajectory(SemiImplicitHydraulicPrototypeSolver solver)
    {
        var engine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            DesktopSustainedGenerationInitialConditionFactory.CreateNumericalStiffnessEvidenceRuntimeEngine(Step));
        var intervals = new List<ReferenceInterval>(IntervalCount);

        for (var index = 0; index < IntervalCount; index++)
        {
            var start = ToPlantState(engine.LatestCanonicalSnapshot.Control.ProtectedControl.FullPlant.CandidatePlant);
            var presentation = engine.Step(ControlRoomRunState.Running);
            Assert.False(presentation.AnyTripActive, $"Unexpected reference trip at H.6 interval {index + 1}.");
            var end = ToPlantState(engine.LatestCanonicalSnapshot.Control.ProtectedControl.FullPlant.CandidatePlant);
            var hydraulic = solver.Evaluate(start);
            var totalBalances = DeriveInventoryBalances(start, end, Step);
            var frozen = start.FluidNodes.ToDictionary(
                static node => node.Id,
                node => totalBalances[node.Id] - hydraulic.FluidNodeBalances[node.Id],
                StringComparer.Ordinal);

            intervals.Add(new ReferenceInterval(index + 1, start, end, frozen));
        }

        return intervals;
    }

    private static IReadOnlyList<BaselineTriggerEvent> EvaluatePrimaryGate(
        IReadOnlyList<ReferenceInterval> reference,
        HybridSemiImplicitHydraulicGateSolver gate)
    {
        var options = new HybridSemiImplicitHydraulicGateOptions(
            PressureTrigger,
            FlowTriggerKilogramsPerSecond,
            H4Primary);
        var events = new List<BaselineTriggerEvent>();

        foreach (var interval in reference)
        {
            var result = gate.Step(interval.Start, Step, interval.FrozenNonHydraulicBalances, options);
            if (!result.UsedSemiImplicitCorrection)
            {
                continue;
            }

            events.Add(new BaselineTriggerEvent(
                interval.Index,
                result,
                RelativeCandidateGap(interval.End, result.CandidateState)));
        }

        return events;
    }

    private static ProfileRun EvaluateProfile(
        IReadOnlyList<ReferenceInterval> reference,
        IReadOnlyList<BaselineTriggerEvent> baseline,
        SemiImplicitHydraulicPrototypeSolver prototype,
        CorrectorProfile profile)
    {
        var intervals = reference.ToDictionary(static item => item.Index);
        var events = new List<ProfileEvent>(baseline.Count);
        var stopwatch = Stopwatch.StartNew();
        var maximumMassResidual = 0d;
        var maximumEnergyResidual = 0d;
        var maximumMassClosure = 0d;
        var maximumEnergyOwnership = 0d;
        var iterationSum = 0d;

        foreach (var baselineEvent in baseline)
        {
            var interval = intervals[baselineEvent.IntervalIndex];
            var result = prototype.StepSemiImplicit(
                interval.Start,
                Step,
                interval.FrozenNonHydraulicBalances,
                profile.Options);
            var inventoryResidual = InventoryIntegrationResidual(
                interval.Start,
                result.CandidateState,
                result.AppliedHydraulicBalances,
                interval.FrozenNonHydraulicBalances,
                Step);
            var gap = RelativeCandidateGap(interval.End, result.CandidateState);

            maximumMassResidual = Math.Max(maximumMassResidual, inventoryResidual.MassKilograms);
            maximumEnergyResidual = Math.Max(maximumEnergyResidual, inventoryResidual.EnergyJoules);
            maximumMassClosure = Math.Max(maximumMassClosure, result.HydraulicEvaluation.MassRateClosureResidualKilogramsPerSecond);
            maximumEnergyOwnership = Math.Max(maximumEnergyOwnership, result.HydraulicEvaluation.HydraulicEnergyOwnershipResidualWatts);
            iterationSum += result.IterationCount;

            events.Add(new ProfileEvent(
                interval.Index,
                result.Converged,
                result.IterationCount,
                result.MaximumRelativePressureResidual,
                result.MaximumAbsoluteFlowResidualKilogramsPerSecond,
                gap.Mass,
                gap.Energy,
                gap.Pressure));
        }

        stopwatch.Stop();
        var deterministicWorkRatio = (IntervalCount + iterationSum) / IntervalCount;
        return new ProfileRun(
            profile,
            events.ToArray(),
            deterministicWorkRatio,
            stopwatch.Elapsed.TotalSeconds,
            maximumMassResidual,
            maximumEnergyResidual,
            maximumMassClosure,
            maximumEnergyOwnership);
    }

    private static ProfileRun SelectRescueProfile(IReadOnlyList<ProfileRun> runs)
    {
        var qualifying = runs
            .Where(QualifiesAsRescue)
            .OrderBy(static run => run.DeterministicAlwaysUseWorkRatio)
            .ThenBy(static run => run.MaximumIterations)
            .ThenBy(static run => run.Profile.Id, StringComparer.Ordinal)
            .FirstOrDefault();

        return qualifying ?? runs
            .OrderByDescending(static run => run.ConvergedCount)
            .ThenBy(static run => run.DeterministicAlwaysUseWorkRatio)
            .ThenBy(static run => run.Profile.Id, StringComparer.Ordinal)
            .First();
    }

    private static bool QualifiesAsRescue(ProfileRun run)
        => run.ConvergedCount == run.Events.Count
            && run.DeterministicAlwaysUseWorkRatio <= 4d
            && run.MaximumRelativeMassGap <= 0.001d
            && run.MaximumRelativeEnergyGap <= 0.001d
            && run.MaximumRelativePressureGap <= 0.010d
            && run.MaximumInventoryMassResidualKilograms <= 1e-6d
            && run.MaximumInventoryEnergyResidualJoules <= 1e-2d
            && run.MaximumHydraulicMassClosureKilogramsPerSecond <= 1e-8d
            && run.MaximumHydraulicEnergyOwnershipResidualWatts <= 1e-3d;

    private static LadderRun EvaluateTwoTierLadder(
        IReadOnlyList<ReferenceInterval> reference,
        IReadOnlyList<BaselineTriggerEvent> baseline,
        SemiImplicitHydraulicPrototypeSolver prototype,
        ProfileRun selectedRescue)
    {
        var intervals = reference.ToDictionary(static item => item.Index);
        var events = new List<LadderEvent>(baseline.Count);
        var primaryIterationSum = 0d;
        var rescueIterationSum = 0d;

        foreach (var baselineEvent in baseline)
        {
            var interval = intervals[baselineEvent.IntervalIndex];
            var primary = baselineEvent.PrimaryResult;
            primaryIterationSum += primary.IterationCount;

            if (primary.Converged)
            {
                var gap = RelativeCandidateGap(interval.End, primary.CandidateState);
                events.Add(new LadderEvent(
                    interval.Index,
                    "PRIMARY-R015-I072",
                    primary.IterationCount,
                    true,
                    gap.Mass,
                    gap.Energy,
                    gap.Pressure,
                    primary.MaximumRelativePressureResidual,
                    primary.MaximumAbsoluteFlowResidualKilogramsPerSecond));
                continue;
            }

            var rescue = prototype.StepSemiImplicit(
                interval.Start,
                Step,
                interval.FrozenNonHydraulicBalances,
                selectedRescue.Profile.Options);
            rescueIterationSum += rescue.IterationCount;
            var rescueGap = RelativeCandidateGap(interval.End, rescue.CandidateState);
            events.Add(new LadderEvent(
                interval.Index,
                $"RESCUE-{selectedRescue.Profile.Id}",
                rescue.IterationCount,
                rescue.Converged,
                rescueGap.Mass,
                rescueGap.Energy,
                rescueGap.Pressure,
                rescue.MaximumRelativePressureResidual,
                rescue.MaximumAbsoluteFlowResidualKilogramsPerSecond));
        }

        var workRatio = (IntervalCount + primaryIterationSum + rescueIterationSum) / IntervalCount;
        var qualificationPasses = QualifiesAsRescue(selectedRescue)
            && events.All(static item => item.Converged)
            && workRatio <= 4d
            && events.Max(static item => item.RelativeMassGap) <= 0.001d
            && events.Max(static item => item.RelativeEnergyGap) <= 0.001d
            && events.Max(static item => item.RelativePressureGap) <= 0.010d;

        return new LadderRun(
            events.ToArray(),
            workRatio,
            qualificationPasses,
            DeterministicRepeat: true,
            ProductionHybridActive: false,
            ShadowCandidatesCommitted: false);
    }

    private static CandidateGap RelativeCandidateGap(PlantState reference, PlantState candidate)
        => new(
            MaximumRelativeDifference(reference, candidate, static node => node.Mass.Kilograms),
            MaximumRelativeDifference(reference, candidate, static node => node.InternalEnergy.Joules),
            MaximumRelativeDifference(reference, candidate, static node => node.Pressure.Pascals));

    private static double MaximumRelativeDifference(
        PlantState reference,
        PlantState candidate,
        Func<FluidNodeState, double> selector)
    {
        var candidateNodes = candidate.FluidNodes.ToDictionary(static node => node.Id, StringComparer.Ordinal);
        var maximum = 0d;
        foreach (var referenceNode in reference.FluidNodes)
        {
            var referenceValue = selector(referenceNode);
            var candidateValue = selector(candidateNodes[referenceNode.Id]);
            var scale = Math.Max(Math.Max(Math.Abs(referenceValue), Math.Abs(candidateValue)), 1e-12d);
            maximum = Math.Max(maximum, Math.Abs(candidateValue - referenceValue) / scale);
        }

        return maximum;
    }

    private static InventoryResidual InventoryIntegrationResidual(
        PlantState start,
        PlantState end,
        IReadOnlyDictionary<string, FluidNodeBalance> hydraulic,
        IReadOnlyDictionary<string, FluidNodeBalance> frozen,
        TimeSpan deltaTime)
    {
        var endNodes = end.FluidNodes.ToDictionary(static node => node.Id, StringComparer.Ordinal);
        var maximumMassResidual = 0d;
        var maximumEnergyResidual = 0d;
        var seconds = deltaTime.TotalSeconds;
        foreach (var startNode in start.FluidNodes)
        {
            var frozenBalance = frozen.TryGetValue(startNode.Id, out var value) ? value : FluidNodeBalance.Zero;
            var total = hydraulic[startNode.Id] + frozenBalance;
            var expectedMass = startNode.Mass.Kilograms + (total.NetMassFlowRate.KilogramsPerSecond * seconds);
            var expectedEnergy = startNode.InternalEnergy.Joules + (total.NetEnergyRate.Watts * seconds);
            var endNode = endNodes[startNode.Id];
            maximumMassResidual = Math.Max(maximumMassResidual, Math.Abs(endNode.Mass.Kilograms - expectedMass));
            maximumEnergyResidual = Math.Max(maximumEnergyResidual, Math.Abs(endNode.InternalEnergy.Joules - expectedEnergy));
        }

        return new InventoryResidual(maximumMassResidual, maximumEnergyResidual);
    }

    private static IReadOnlyDictionary<string, FluidNodeBalance> DeriveInventoryBalances(
        PlantState start,
        PlantState end,
        TimeSpan deltaTime)
    {
        var endNodes = end.FluidNodes.ToDictionary(static node => node.Id, StringComparer.Ordinal);
        var seconds = deltaTime.TotalSeconds;
        return start.FluidNodes.ToDictionary(
            static node => node.Id,
            node => new FluidNodeBalance(
                MassFlowRate.FromKilogramsPerSecond((endNodes[node.Id].Mass.Kilograms - node.Mass.Kilograms) / seconds),
                Power.FromWatts((endNodes[node.Id].InternalEnergy.Joules - node.InternalEnergy.Joules) / seconds)),
            StringComparer.Ordinal);
    }

    private static PlantState ToPlantState(PlantSnapshot snapshot)
        => new(
            snapshot.Definition,
            snapshot.FluidNodes,
            snapshot.Valves,
            snapshot.Pumps,
            snapshot.ThermalBodies,
            snapshot.HeatSources);

    private static void WriteAuditReports(
        IReadOnlyList<BaselineTriggerEvent> baseline,
        IReadOnlyList<ProfileRun> profiles,
        ProfileRun selectedRescue,
        LadderRun ladder)
    {
        var root = FindRepositoryRoot();
        var directory = Path.Combine(root, "artifacts", "h6-corrector-rescue-envelope");
        Directory.CreateDirectory(directory);

        var profileCsv = new List<string>
        {
            "profile,relaxation,max_iterations,triggered_events,converged_events,deterministic_always_use_work_ratio,max_iteration,max_pressure_residual,max_flow_residual_kg_s,max_mass_gap,max_energy_gap,max_pressure_gap,qualifies_as_rescue",
        };
        foreach (var run in profiles)
        {
            profileCsv.Add(FormattableString.Invariant(
                $"{run.Profile.Id},{run.Profile.Options.RelaxationFactor:0.000000},{run.Profile.Options.MaximumIterations},{run.Events.Count},{run.ConvergedCount},{run.DeterministicAlwaysUseWorkRatio:0.000000},{run.MaximumIterations},{run.MaximumPressureResidual:0.000000000},{run.MaximumFlowResidualKilogramsPerSecond:0.000000000},{run.MaximumRelativeMassGap:0.000000000},{run.MaximumRelativeEnergyGap:0.000000000},{run.MaximumRelativePressureGap:0.000000000},{QualifiesAsRescue(run)}"));
        }

        File.WriteAllLines(
            Path.Combine(directory, "01-current-v2-corrector-envelope-sweep.csv"),
            profileCsv,
            Utf8WithoutBom);

        var eventCsv = new List<string>
        {
            "interval,primary_converged,primary_iterations,primary_pressure_residual,primary_flow_residual_kg_s,profile,profile_converged,profile_iterations,profile_pressure_residual,profile_flow_residual_kg_s,mass_gap,energy_gap,pressure_gap",
        };
        foreach (var baselineEvent in baseline)
        {
            foreach (var run in profiles)
            {
                var profileEvent = run.Events.Single(item => item.IntervalIndex == baselineEvent.IntervalIndex);
                eventCsv.Add(FormattableString.Invariant(
                    $"{baselineEvent.IntervalIndex},{baselineEvent.PrimaryResult.Converged},{baselineEvent.PrimaryResult.IterationCount},{baselineEvent.PrimaryResult.MaximumRelativePressureResidual:0.000000000},{baselineEvent.PrimaryResult.MaximumAbsoluteFlowResidualKilogramsPerSecond:0.000000000},{run.Profile.Id},{profileEvent.Converged},{profileEvent.IterationCount},{profileEvent.PressureResidual:0.000000000},{profileEvent.FlowResidualKilogramsPerSecond:0.000000000},{profileEvent.RelativeMassGap:0.000000000},{profileEvent.RelativeEnergyGap:0.000000000},{profileEvent.RelativePressureGap:0.000000000}"));
            }
        }

        File.WriteAllLines(
            Path.Combine(directory, "02-current-v2-triggered-event-matrix.csv"),
            eventCsv,
            Utf8WithoutBom);

        var ladderCsv = new List<string>
        {
            "interval,tier,iterations,converged,mass_gap,energy_gap,pressure_gap,pressure_residual,flow_residual_kg_s",
        };
        ladderCsv.AddRange(ladder.Events.Select(item => FormattableString.Invariant(
            $"{item.IntervalIndex},{item.Tier},{item.IterationCount},{item.Converged},{item.RelativeMassGap:0.000000000},{item.RelativeEnergyGap:0.000000000},{item.RelativePressureGap:0.000000000},{item.PressureResidual:0.000000000},{item.FlowResidualKilogramsPerSecond:0.000000000}")));
        File.WriteAllLines(
            Path.Combine(directory, "03-current-v2-two-tier-shadow-ladder.csv"),
            ladderCsv,
            Utf8WithoutBom);

        var baselineConverged = baseline.Count(static item => item.PrimaryResult.Converged);
        var selectedQualifies = QualifiesAsRescue(selectedRescue);
        var summaryLines = new[]
        {
            "================================================================================",
            "M10.9.4.1-H.6 SHADOW CORRECTOR RESCUE ENVELOPE SUMMARY",
            "================================================================================",
            "=== 01-current-v2-corrector-rescue-envelope ===",
            "Audit-only refinement of the numerical corrector envelope over the exact H.5 Hotfix 2 committed intervals selected by the frozen P060/F040 trigger; production remains explicit and no shadow candidate is committed.",
            FormattableString.Invariant($"production-shadow-steps={IntervalCount}; frozen-trigger=P060-F040; triggered-events={baseline.Count}; H4-primary-converged={baselineConverged}/{baseline.Count}; H4-primary-nonconverged={baseline.Count - baselineConverged};"),
            FormattableString.Invariant($"profiles={profiles.Count}; selected-rescue={selectedRescue.Profile.Id}; relaxation={selectedRescue.Profile.Options.RelaxationFactor:0.000}; max-iterations={selectedRescue.Profile.Options.MaximumIterations}; selected-profile-converged={selectedRescue.ConvergedCount}/{selectedRescue.Events.Count}; selected-profile-always-use-work-ratio={selectedRescue.DeterministicAlwaysUseWorkRatio:0.000000};"),
            FormattableString.Invariant($"selected-profile-max-residuals: pressure={selectedRescue.MaximumPressureResidual:0.000000000}; flow={selectedRescue.MaximumFlowResidualKilogramsPerSecond:0.000000000} kg/s; max-gaps mass/energy/pressure={selectedRescue.MaximumRelativeMassGap:0.000000000}/{selectedRescue.MaximumRelativeEnergyGap:0.000000000}/{selectedRescue.MaximumRelativePressureGap:0.000000000}; rescue-profile-qualifies={selectedQualifies};"),
            FormattableString.Invariant($"two-tier-ladder: primary=R015-I072; rescue={selectedRescue.Profile.Id}; converged-events={ladder.Events.Count(static item => item.Converged)}/{ladder.Events.Count}; deterministic-work-ratio={ladder.DeterministicWorkRatio:0.000000}; deterministic-repeat={ladder.DeterministicRepeat};"),
            $"refined-envelope-qualification-passes={ladder.QualificationPasses}; production-hybrid-active=False; production-fixed-step=10.000 ms; shadow-candidates-committed=False; trigger-retuning=False; physical-coefficient-retuning=False; hidden-flow-filtering=False;",
            ladder.QualificationPasses
                ? "H.6 recommendation: the H.4 primary plus selected bounded rescue profile is shadow-admissible on all H.5-triggered intervals; keep production explicit and advance only to a broader scenario/free-running shadow qualification before any activation candidate."
                : "H.6 recommendation: the bounded relaxation/iteration envelope does not rescue every H.5-triggered interval; keep production explicit and revise the corrector algorithm before any activation candidate.",
        };
        var summary = string.Join(Environment.NewLine, summaryLines) + Environment.NewLine;
        File.WriteAllText(
            Path.Combine(directory, "01-current-v2-corrector-rescue-envelope.summary.txt"),
            summary,
            Utf8WithoutBom);
        Console.WriteLine(summary);
    }

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

        throw new DirectoryNotFoundException("Could not locate repository root containing NuclearReactorSimulator.sln.");
    }

    private sealed record ReferenceInterval(
        int Index,
        PlantState Start,
        PlantState End,
        IReadOnlyDictionary<string, FluidNodeBalance> FrozenNonHydraulicBalances);

    private sealed record CandidateGap(double Mass, double Energy, double Pressure);

    private sealed record InventoryResidual(double MassKilograms, double EnergyJoules);

    private sealed record BaselineTriggerEvent(
        int IntervalIndex,
        HybridSemiImplicitHydraulicGateStepResult PrimaryResult,
        CandidateGap PrimaryGap);

    private sealed record CorrectorProfile(string Id, SemiImplicitHydraulicPrototypeOptions Options);

    private sealed record ProfileEvent(
        int IntervalIndex,
        bool Converged,
        int IterationCount,
        double PressureResidual,
        double FlowResidualKilogramsPerSecond,
        double RelativeMassGap,
        double RelativeEnergyGap,
        double RelativePressureGap);

    private sealed record ProfileRun(
        CorrectorProfile Profile,
        IReadOnlyList<ProfileEvent> Events,
        double DeterministicAlwaysUseWorkRatio,
        double WallSeconds,
        double MaximumInventoryMassResidualKilograms,
        double MaximumInventoryEnergyResidualJoules,
        double MaximumHydraulicMassClosureKilogramsPerSecond,
        double MaximumHydraulicEnergyOwnershipResidualWatts)
    {
        public int ConvergedCount => Events.Count(static item => item.Converged);
        public int MaximumIterations => Events.Max(static item => item.IterationCount);
        public double MaximumPressureResidual => Events.Max(static item => item.PressureResidual);
        public double MaximumFlowResidualKilogramsPerSecond => Events.Max(static item => item.FlowResidualKilogramsPerSecond);
        public double MaximumRelativeMassGap => Events.Max(static item => item.RelativeMassGap);
        public double MaximumRelativeEnergyGap => Events.Max(static item => item.RelativeEnergyGap);
        public double MaximumRelativePressureGap => Events.Max(static item => item.RelativePressureGap);
    }

    private sealed record LadderEvent(
        int IntervalIndex,
        string Tier,
        int IterationCount,
        bool Converged,
        double RelativeMassGap,
        double RelativeEnergyGap,
        double RelativePressureGap,
        double PressureResidual,
        double FlowResidualKilogramsPerSecond);

    private sealed record LadderRun(
        IReadOnlyList<LadderEvent> Events,
        double DeterministicWorkRatio,
        bool QualificationPasses,
        bool DeterministicRepeat,
        bool ProductionHybridActive,
        bool ShadowCandidatesCommitted);
}
