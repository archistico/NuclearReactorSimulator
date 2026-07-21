using NuclearReactorSimulator.Domain.Physics.Fluids;

namespace NuclearReactorSimulator.Simulation.Physics.Fluids;

/// <summary>
/// Deterministically maps mechanical valve position to normalized hydraulic capacity.
/// </summary>
public sealed class ValveCharacteristicSolver
{
    public ValveFlowCoefficient Evaluate(
        ValveCharacteristic characteristic,
        ValvePosition position)
    {
        ArgumentNullException.ThrowIfNull(characteristic);

        if (position.IsClosed)
        {
            return ValveFlowCoefficient.Closed;
        }

        if (position.IsFullyOpen)
        {
            return ValveFlowCoefficient.FullyOpen;
        }

        var x = position.Fraction;
        var coefficient = characteristic.Kind switch
        {
            ValveCharacteristicKind.Linear => x,
            ValveCharacteristicKind.QuickOpening => Math.Sqrt(x),
            ValveCharacteristicKind.EqualPercentage =>
                (Math.Pow(characteristic.Rangeability, x) - 1d)
                / (characteristic.Rangeability - 1d),
            _ => throw new ArgumentOutOfRangeException(
                nameof(characteristic),
                characteristic.Kind,
                "Unknown valve characteristic kind."),
        };

        if (!double.IsFinite(coefficient))
        {
            throw new ArithmeticException("Valve characteristic calculation produced a non-finite flow coefficient.");
        }

        return ValveFlowCoefficient.FromFraction(Math.Clamp(coefficient, 0d, 1d));
    }
}
