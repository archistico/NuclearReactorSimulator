using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Domain.Physics.TurbineIsland.Turbine;

/// <summary>
/// Canonical M4.2 lumped turbine rotor with inertia and explicit rated/overspeed thresholds.
/// </summary>
public sealed class TurbineRotorDefinition
{
    public TurbineRotorDefinition(
        string id,
        MomentOfInertia momentOfInertia,
        AngularSpeed ratedAngularSpeed,
        AngularSpeed overspeedThreshold)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Turbine rotor id cannot be empty or whitespace.", nameof(id));
        }

        if (momentOfInertia.KilogramSquareMetres <= 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(momentOfInertia), momentOfInertia, "Turbine rotor moment of inertia must be greater than zero.");
        }

        if (ratedAngularSpeed <= AngularSpeed.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(ratedAngularSpeed), ratedAngularSpeed, "Rated turbine angular speed must be greater than zero.");
        }

        if (overspeedThreshold <= ratedAngularSpeed)
        {
            throw new ArgumentOutOfRangeException(nameof(overspeedThreshold), overspeedThreshold, "Overspeed threshold must be greater than rated angular speed.");
        }

        Id = id.Trim();
        MomentOfInertia = momentOfInertia;
        RatedAngularSpeed = ratedAngularSpeed;
        OverspeedThreshold = overspeedThreshold;
    }

    public string Id { get; }

    public MomentOfInertia MomentOfInertia { get; }

    public AngularSpeed RatedAngularSpeed { get; }

    public AngularSpeed OverspeedThreshold { get; }
}
