using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios.Training;
using NuclearReactorSimulator.Domain.Physics.Control.TurbineSecondary;
using NuclearReactorSimulator.Domain.Physics.Electrical;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay;

/// <summary>
/// M10.9.4.1-A.3 audit-only evidence for the unresolved reference-plant scale contract.
/// These tests intentionally freeze the current hybrid values and their derived consequences without declaring them correct.
/// Any future scale migration must update the contract and these evidence values in the same candidate.
/// </summary>
public sealed class ReferencePlantScaleAuditTests
{
    private const double CandidateReducedScaleRatingMegawatts = 10d;

    [Fact(Explicit = true)]
    [Trait("Category", "OperationalEnvelopeAudit")]
    [Trait("Category", "ReferencePlantScaleAudit")]
    public void CurrentV2_ReferencePlantScaleEvidence_IsExplicitAndReproducible()
    {
        var engine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            new DesktopSustainedGenerationInitialConditionFactory().CreateRuntimeEngine());
        var plant = engine.CurrentState.PlantDefinition;
        var rotor = Assert.Single(plant.TurbineExpansionSystem.Rotors);
        var generator = Assert.Single(plant.GeneratorGridSystem.Generators);
        var generatorInput = Assert.Single(engine.PersistentInputs.PlantInputs.GeneratorGridInputs.GeneratorInputs);
        var droop = Assert.IsType<TurbineGovernorDroopDefinition>(
            engine.CurrentState.TurbineSecondaryControlState.Definition.GovernorDroop);
        var coupling = Assert.IsType<SynchronousGridCouplingDefinition>(generator.GridCoupling);

        var evidence = ReferencePlantScaleEvidence.Create(
            rotor.MomentOfInertia.KilogramSquareMetres,
            rotor.RatedAngularSpeed.RevolutionsPerMinute,
            rotor.RatedAngularSpeed.RadiansPerSecond,
            rotor.MomentOfInertia.KineticEnergyAt(rotor.RatedAngularSpeed).Megajoules,
            generator.MaximumElectricalPower.Megawatts,
            generatorInput.RequestedElectricalPower.Megawatts,
            generator.Efficiency.Fraction,
            droop.FullLoadSpeedReferenceRise.RevolutionsPerMinute,
            coupling.MaximumSynchronizingCorrectionPower.Megawatts,
            coupling.FrequencyDampingPowerAtOneHertzSlip.Megawatts,
            generator.MaximumSynchronizationFrequencyDifference.Hertz,
            CandidateReducedScaleRatingMegawatts);

        Assert.Equal(1_000d, evidence.RotorMomentOfInertiaKilogramSquareMetres, 12);
        Assert.Equal(3_000d, evidence.RatedRotorSpeedRpm, 12);
        Assert.Equal(314.1592653589793d, evidence.RatedRotorSpeedRadiansPerSecond, 12);
        Assert.Equal(49.34802200544679d, evidence.StoredRotationalEnergyMegajoules, 12);
        Assert.Equal(1_000d, evidence.GeneratorNameplateMegawatts, 12);
        Assert.Equal(5d, evidence.RequestedElectricalPowerMegawatts, 12);
        Assert.Equal(0.005d, evidence.RequestedLoadFractionOfNameplate, 12);
        Assert.Equal(0.75d, evidence.DroopReferenceRiseAtRequestedLoadRpm, 12);
        Assert.Equal(1_020.408163265306d, evidence.MaximumMechanicalPowerMegawatts, 12);
        Assert.Equal(0.0493480220054468d, evidence.InertiaConstantAtConfiguredNameplateSeconds, 12);
        Assert.Equal(4.93480220054468d, evidence.InertiaConstantAtCandidateReducedScaleSeconds, 12);
        Assert.Equal(30.3963550927013d, evidence.RotorAccelerationRpmPerSecondPerMegawattImbalance, 12);
        Assert.Equal(9.86960440108936d, evidence.SecondsToOverspeedAtOneMegawattImbalance, 12);
        Assert.Equal(1.97392088021787d, evidence.SecondsToOverspeedAtFiveMegawattImbalance, 12);
        Assert.Equal(0.5d, evidence.MaximumSynchronizingCorrectionMegawatts, 12);
        Assert.Equal(0.0005d, evidence.MaximumSynchronizingCorrectionFractionOfNameplate, 12);
        Assert.Equal(0.1d, evidence.MaximumSynchronizingCorrectionMultipleOfRequestedLoad, 12);
        Assert.Equal(0.4d, evidence.FrequencyDampingAtSynchronizationToleranceMegawatts, 12);

        Assert.Equal(0d, evidence.DroopReferenceRiseAtLoadMegawatts(0d), 12);
        Assert.Equal(0.75d, evidence.DroopReferenceRiseAtLoadMegawatts(5d), 12);
        Assert.Equal(1.5d, evidence.DroopReferenceRiseAtLoadMegawatts(10d), 12);
        Assert.Equal(15d, evidence.DroopReferenceRiseAtLoadMegawatts(100d), 12);
        Assert.Equal(150d, evidence.DroopReferenceRiseAtLoadMegawatts(1_000d), 12);
    }

    private sealed record ReferencePlantScaleEvidence(
        double RotorMomentOfInertiaKilogramSquareMetres,
        double RatedRotorSpeedRpm,
        double RatedRotorSpeedRadiansPerSecond,
        double StoredRotationalEnergyMegajoules,
        double GeneratorNameplateMegawatts,
        double RequestedElectricalPowerMegawatts,
        double RequestedLoadFractionOfNameplate,
        double FullLoadDroopReferenceRiseRpm,
        double DroopReferenceRiseAtRequestedLoadRpm,
        double MaximumMechanicalPowerMegawatts,
        double InertiaConstantAtConfiguredNameplateSeconds,
        double CandidateReducedScaleRatingMegawatts,
        double InertiaConstantAtCandidateReducedScaleSeconds,
        double RotorAccelerationRpmPerSecondPerMegawattImbalance,
        double SecondsToOverspeedAtOneMegawattImbalance,
        double SecondsToOverspeedAtFiveMegawattImbalance,
        double MaximumSynchronizingCorrectionMegawatts,
        double MaximumSynchronizingCorrectionFractionOfNameplate,
        double MaximumSynchronizingCorrectionMultipleOfRequestedLoad,
        double FrequencyDampingAtSynchronizationToleranceMegawatts)
    {
        public static ReferencePlantScaleEvidence Create(
            double rotorMomentOfInertiaKilogramSquareMetres,
            double ratedRotorSpeedRpm,
            double ratedRotorSpeedRadiansPerSecond,
            double storedRotationalEnergyMegajoules,
            double generatorNameplateMegawatts,
            double requestedElectricalPowerMegawatts,
            double generatorEfficiencyFraction,
            double fullLoadDroopReferenceRiseRpm,
            double maximumSynchronizingCorrectionMegawatts,
            double frequencyDampingPowerAtOneHertzSlipMegawatts,
            double maximumSynchronizationFrequencyDifferenceHertz,
            double candidateReducedScaleRatingMegawatts)
        {
            var requestedLoadFractionOfNameplate = requestedElectricalPowerMegawatts / generatorNameplateMegawatts;
            var accelerationRadiansPerSecondSquaredPerMegawatt =
                1_000_000d / ratedRotorSpeedRadiansPerSecond / rotorMomentOfInertiaKilogramSquareMetres;
            var rotorAccelerationRpmPerSecondPerMegawattImbalance =
                accelerationRadiansPerSecondSquaredPerMegawatt * 60d / (2d * Math.PI);
            var overspeedMarginRpm = 300d;

            return new ReferencePlantScaleEvidence(
                rotorMomentOfInertiaKilogramSquareMetres,
                ratedRotorSpeedRpm,
                ratedRotorSpeedRadiansPerSecond,
                storedRotationalEnergyMegajoules,
                generatorNameplateMegawatts,
                requestedElectricalPowerMegawatts,
                requestedLoadFractionOfNameplate,
                fullLoadDroopReferenceRiseRpm,
                fullLoadDroopReferenceRiseRpm * requestedLoadFractionOfNameplate,
                generatorNameplateMegawatts / generatorEfficiencyFraction,
                storedRotationalEnergyMegajoules / generatorNameplateMegawatts,
                candidateReducedScaleRatingMegawatts,
                storedRotationalEnergyMegajoules / candidateReducedScaleRatingMegawatts,
                rotorAccelerationRpmPerSecondPerMegawattImbalance,
                overspeedMarginRpm / rotorAccelerationRpmPerSecondPerMegawattImbalance,
                overspeedMarginRpm / (5d * rotorAccelerationRpmPerSecondPerMegawattImbalance),
                maximumSynchronizingCorrectionMegawatts,
                maximumSynchronizingCorrectionMegawatts / generatorNameplateMegawatts,
                maximumSynchronizingCorrectionMegawatts / requestedElectricalPowerMegawatts,
                frequencyDampingPowerAtOneHertzSlipMegawatts * maximumSynchronizationFrequencyDifferenceHertz);
        }

        public double DroopReferenceRiseAtLoadMegawatts(double requestedLoadMegawatts)
            => FullLoadDroopReferenceRiseRpm * requestedLoadMegawatts / GeneratorNameplateMegawatts;
    }
}
