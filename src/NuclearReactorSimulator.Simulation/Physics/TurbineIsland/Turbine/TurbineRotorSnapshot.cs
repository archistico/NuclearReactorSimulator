using System.Text.Json.Serialization;
using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Turbine;

public sealed record TurbineRotorSnapshot(
    string RotorId,
    MomentOfInertia MomentOfInertia,
    AngularSpeed InitialAngularSpeed,
    AngularSpeed FinalAngularSpeed,
    AngularSpeed AverageAngularSpeed,
    AngularSpeed RatedAngularSpeed,
    AngularSpeed OverspeedThreshold,
    Torque TurbineTorque,
    Torque CommandedExternalLoadTorque,
    Torque EffectiveExternalLoadTorque,
    Torque NetTorque,
    Power ShaftPower,
    Power ExternalLoadPower,
    Energy InitialKineticEnergy,
    Energy FinalKineticEnergy,
    bool TripCommandActive,
    bool OverspeedDetectedAtStart,
    bool OverspeedDetectedAtEnd,
    bool ExternalLoadTorqueLimitedAtZeroSpeed)
{
    [JsonIgnore]
    public Torque PassiveMechanicalLossTorque { get; init; }

    [JsonIgnore]
    public Power PassiveMechanicalLossPower { get; init; }

    [JsonIgnore]
    public bool PassiveMechanicalLossTorqueLimitedAtZeroSpeed { get; init; }
}
