using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Domain.Physics.TurbineIsland.Turbine;

/// <summary>
/// Optional passive rotor-loss law used for bearing, windage and uncoupled generator drag.
/// Loss torque rises linearly with angular speed, so loss power rises with the square of speed and
/// naturally falls to zero at rest without introducing a constant-power singularity.
/// </summary>
public sealed class TurbineRotorMechanicalLossDefinition
{
    public TurbineRotorMechanicalLossDefinition(Power ratedSpeedLossPower)
    {
        if (ratedSpeedLossPower <= Power.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(ratedSpeedLossPower),
                ratedSpeedLossPower,
                "Rated-speed rotor mechanical-loss power must be greater than zero.");
        }

        RatedSpeedLossPower = ratedSpeedLossPower;
    }

    public Power RatedSpeedLossPower { get; }

    public Torque ResolveTorque(AngularSpeed angularSpeed, AngularSpeed ratedAngularSpeed)
    {
        if (ratedAngularSpeed <= AngularSpeed.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(ratedAngularSpeed),
                ratedAngularSpeed,
                "Rated angular speed must be greater than zero.");
        }

        if (angularSpeed <= AngularSpeed.Zero)
        {
            return Torque.Zero;
        }

        var speedFraction = angularSpeed.RadiansPerSecond / ratedAngularSpeed.RadiansPerSecond;
        var ratedTorqueNewtonMetres = RatedSpeedLossPower.Watts / ratedAngularSpeed.RadiansPerSecond;
        return Torque.FromNewtonMetres(ratedTorqueNewtonMetres * speedFraction);
    }
}
