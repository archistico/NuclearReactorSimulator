using System.Text.Json.Serialization;
using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor.PrimaryCircuit.SteamDrums;

namespace NuclearReactorSimulator.Simulation.Physics.Reactor.PrimaryCircuit.SteamDrums;

public sealed record SteamDrumSnapshot(
    string DrumId,
    string MainCirculationLoopId,
    string InventoryNodeId,
    string SteamOutletNodeId,
    string LiquidRecirculationNodeId,
    Mass InventoryMass,
    Energy InventoryInternalEnergy,
    Pressure Pressure,
    Temperature Temperature,
    FluidPhase Phase,
    VaporQuality? VaporQuality,
    VoidFraction VoidFraction,
    SteamDrumLevelFraction LiquidLevelFraction,
    MassFlowRate IncomingReturnMassFlowRate,
    MassFlowRate SeparatedSteamMassFlowRate,
    MassFlowRate RecirculatedLiquidMassFlowRate,
    SpecificEnergy SteamSpecificInternalEnergy,
    SpecificEnergy LiquidSpecificInternalEnergy,
    Power SteamEnergyRate,
    Power LiquidEnergyRate,
    double SeparationMassResidualKilogramsPerSecond,
    double SeparationEnergyResidualWatts)
{

    [JsonIgnore]
    public FluidEnergyTransportMode EnergyTransportMode { get; init; } = FluidEnergyTransportMode.SpecificInternalEnergy;

    [JsonIgnore]
    public SpecificEnergy SteamSpecificFlowWork { get; init; } = SpecificEnergy.Zero;

    [JsonIgnore]
    public SpecificEnergy LiquidSpecificFlowWork { get; init; } = SpecificEnergy.Zero;

    [JsonIgnore]
    public SpecificEnergy SteamSpecificEnthalpy { get; init; } = SteamSpecificInternalEnergy;

    [JsonIgnore]
    public SpecificEnergy LiquidSpecificEnthalpy { get; init; } = LiquidSpecificInternalEnergy;

    [JsonIgnore]
    public SpecificEnergy SteamAdvectedSpecificEnergy { get; init; } = SteamSpecificInternalEnergy;

    [JsonIgnore]
    public SpecificEnergy LiquidAdvectedSpecificEnergy { get; init; } = LiquidSpecificInternalEnergy;

    [JsonIgnore]
    public Power SteamInternalEnergyRate { get; init; } = SteamEnergyRate;

    [JsonIgnore]
    public Power LiquidInternalEnergyRate { get; init; } = LiquidEnergyRate;

    [JsonIgnore]
    public Power SteamFlowWorkRate { get; init; } = Power.Zero;

    [JsonIgnore]
    public Power LiquidFlowWorkRate { get; init; } = Power.Zero;

    /// <summary>Current-v2 diagnostic: liquid mass that can physically participate in liquid recirculation.</summary>
    [JsonIgnore]
    public Mass SeparableLiquidInventoryMass { get; init; } = Mass.Zero;

    /// <summary>Current-v2 diagnostic: pump-demand liquid recirculation before inventory limiting.</summary>
    [JsonIgnore]
    public MassFlowRate RequestedLiquidRecirculationMassFlowRate { get; init; } = RecirculatedLiquidMassFlowRate;

    /// <summary>Current-v2 diagnostic: maximum liquid recirculation supported by same-step incoming liquid plus committed liquid inventory.</summary>
    [JsonIgnore]
    public MassFlowRate MaximumInventorySupportedLiquidRecirculationMassFlowRate { get; init; } = RecirculatedLiquidMassFlowRate;

    [JsonIgnore]
    public bool LiquidRecirculationInventoryLimited { get; init; }

    [JsonIgnore]
    public bool HasSeparableLiquidInventory => SeparableLiquidInventoryMass > Mass.Zero;

    /// <summary>Current-v2 diagnostic: committed separable-liquid mass divided by total committed drum inventory mass.</summary>
    [JsonIgnore]
    public double SeparableLiquidInventoryMassFraction => InventoryMass > Mass.Zero
        ? Math.Clamp(SeparableLiquidInventoryMass.Kilograms / InventoryMass.Kilograms, 0d, 1d)
        : 0d;

    /// <summary>Current-v2 diagnostic: true when no committed liquid inventory remains available for water/steam separation.</summary>
    [JsonIgnore]
    public bool CommittedLiquidInventoryDepleted => !HasSeparableLiquidInventory;

    /// <summary>Current-v2 diagnostic: true when the committed drum state is all vapor and therefore has no liquid phase to separate/recirculate.</summary>
    [JsonIgnore]
    public bool WaterSteamSeparationUnavailable => Phase == FluidPhase.SuperheatedVapor && CommittedLiquidInventoryDepleted;

    /// <summary>Current-v2 diagnostic: requested pump recirculation not delivered because liquid inventory is insufficient.</summary>
    [JsonIgnore]
    public MassFlowRate LiquidRecirculationInventoryDeficitMassFlowRate => MassFlowRate.FromKilogramsPerSecond(Math.Max(
        0d,
        RequestedLiquidRecirculationMassFlowRate.KilogramsPerSecond - RecirculatedLiquidMassFlowRate.KilogramsPerSecond));

    /// <summary>True when the current pressure/energy/inventory-driven steam-source closure is active.</summary>
    [JsonIgnore]
    public bool UsesPressureEnergyInventorySteamSource { get; init; }

    /// <summary>Forward steam-source capacity implied by committed drum-to-outlet pressure head.</summary>
    [JsonIgnore]
    public MassFlowRate SteamSourcePressureDrivenCapacityMassFlowRate { get; init; } = SeparatedSteamMassFlowRate;

    /// <summary>Total steam source available from same-step return-energy surplus plus committed separable vapor inventory.</summary>
    [JsonIgnore]
    public MassFlowRate SteamSourceAvailableMassFlowRate { get; init; } = SeparatedSteamMassFlowRate;

    /// <summary>Steam production supported by the energy carried by positive return flow above the liquid reference state.</summary>
    [JsonIgnore]
    public MassFlowRate SteamSourceIncomingEnergySupportedMassFlowRate { get; init; } = SeparatedSteamMassFlowRate;

    /// <summary>Committed vapor mass available to supplement same-step energy-driven steam production.</summary>
    [JsonIgnore]
    public Mass SteamSourceStoredVaporInventoryMass { get; init; } = Mass.Zero;

    [JsonIgnore]
    public bool SteamSourcePressureLimited { get; init; }

    [JsonIgnore]
    public bool SteamSourceAvailabilityLimited { get; init; }
}
