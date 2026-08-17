using System.Text.Json.Serialization;
using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Condenser;

public sealed record CondenserSnapshot(
    string CondenserId,
    string TurbineStageGroupId,
    string SteamSpaceNodeId,
    string HotwellNodeId,
    string CoolingBoundaryId,
    Pressure InitialSteamSpacePressure,
    Pressure FinalSteamSpacePressure,
    PressureDifference InitialVacuumBelowAtmosphere,
    PressureDifference FinalVacuumBelowAtmosphere,
    Temperature InitialSteamSpaceTemperature,
    Temperature FinalSteamSpaceTemperature,
    FluidPhase InitialSteamSpacePhase,
    FluidPhase FinalSteamSpacePhase,
    VaporQuality? InitialSteamSpaceVaporQuality,
    VaporQuality? FinalSteamSpaceVaporQuality,
    double CondensableVaporMassFraction,
    Mass AvailableCondensableMass,
    MassFlowRate MaximumCondensationMassFlowRate,
    Temperature CoolingBoundaryTemperature,
    TemperatureDifference SteamToCoolingTemperatureDifference,
    ThermalConductance? OverallHeatTransferConductance,
    Power SurfaceHeatTransferLimitedPower,
    Power EffectiveHeatRejectionCapacity,
    MassFlowRate InventoryLimitedCondensationMassFlowRate,
    MassFlowRate ThermalLimitedCondensationMassFlowRate,
    MassFlowRate ActualCondensationMassFlowRate,
    FluidEnergyTransportMode EnergyTransportMode,
    SpecificEnergy SteamSpecificInternalEnergy,
    SpecificEnergy SteamSpecificFlowWork,
    SpecificEnergy SteamSpecificEnthalpy,
    SpecificEnergy SteamAdvectedSpecificEnergy,
    SpecificEnergy CondensateSpecificInternalEnergy,
    SpecificEnergy CondensateSpecificFlowWork,
    SpecificEnergy CondensateSpecificEnthalpy,
    SpecificEnergy CondensateAdvectedSpecificEnergy,
    Power SteamInternalEnergyRemovalRate,
    Power SteamFlowWorkRemovalRate,
    Power SteamEnergyRemovalRate,
    Power HotwellInternalEnergyAdditionRate,
    Power HotwellFlowWorkAdditionRate,
    Power HotwellEnergyAdditionRate,
    Power HeatRejectionPower,
    Mass InitialHotwellMass,
    Mass FinalHotwellMass,
    Temperature InitialHotwellTemperature,
    Temperature FinalHotwellTemperature,
    FluidPhase InitialHotwellPhase,
    FluidPhase FinalHotwellPhase)
{
    private const double FlowToleranceKilogramsPerSecond = 1e-9d;

    [JsonIgnore]
    public SpecificEnergy SpecificCondensationEnergyDrop => SpecificEnergy.FromJoulesPerKilogram(Math.Max(
        0d,
        SteamAdvectedSpecificEnergy.JoulesPerKilogram - CondensateAdvectedSpecificEnergy.JoulesPerKilogram));

    [JsonIgnore]
    public bool MaximumFlowLimitActive => IsActiveLimit(MaximumCondensationMassFlowRate);

    [JsonIgnore]
    public bool InventoryLimitActive => IsActiveLimit(InventoryLimitedCondensationMassFlowRate);

    [JsonIgnore]
    public bool ThermalLimitActive => IsActiveLimit(ThermalLimitedCondensationMassFlowRate);

    [JsonIgnore]
    public MassFlowRate MaximumFlowMargin => Margin(MaximumCondensationMassFlowRate);

    [JsonIgnore]
    public MassFlowRate InventoryFlowMargin => Margin(InventoryLimitedCondensationMassFlowRate);

    [JsonIgnore]
    public MassFlowRate ThermalFlowMargin => Margin(ThermalLimitedCondensationMassFlowRate);

    [JsonIgnore]
    public string ActiveCondensationLimits
    {
        get
        {
            var limits = new List<string>(3);
            if (MaximumFlowLimitActive) limits.Add("MAXIMUM FLOW");
            if (InventoryLimitActive) limits.Add("INVENTORY");
            if (ThermalLimitActive) limits.Add("THERMAL");
            return limits.Count == 0 ? "UNCONSTRAINED / ZERO DEMAND" : string.Join(" + ", limits);
        }
    }

    private bool IsActiveLimit(MassFlowRate limit)
        => Math.Abs(limit.KilogramsPerSecond - ActualCondensationMassFlowRate.KilogramsPerSecond)
            <= FlowToleranceKilogramsPerSecond;

    private MassFlowRate Margin(MassFlowRate limit)
        => MassFlowRate.FromKilogramsPerSecond(Math.Max(
            0d,
            limit.KilogramsPerSecond - ActualCondensationMassFlowRate.KilogramsPerSecond));
}
