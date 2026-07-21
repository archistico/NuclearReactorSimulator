using NuclearReactorSimulator.Simulation.Physics.Control.ReactorPrimary;
using NuclearReactorSimulator.Simulation.Physics.Control.TurbineSecondary;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Integration;

namespace NuclearReactorSimulator.Simulation.Physics.Control.Protection;

/// <summary>M5.5 top-level snapshot: physical truth plus normal-control and protection diagnostics remain separate.</summary>
public sealed record ProtectedAutomaticControlSnapshot(
    FullPlantSnapshot FullPlant,
    ReactorPrimaryControlSnapshot ReactorPrimary,
    TurbineSecondaryControlSnapshot TurbineSecondary,
    ProtectionSystemSnapshot Protection,
    ProtectionArbitrationSnapshot Arbitration);
