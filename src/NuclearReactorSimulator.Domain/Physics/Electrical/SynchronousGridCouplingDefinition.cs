using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Domain.Physics.Electrical;

/// <summary>
/// Optional reduced-order infinite-bus synchronizing correction applied around the dispatched generator load.
/// The phase term provides electrical-angle stiffness while the frequency term damps rotor/grid slip.
/// A null coupling on <see cref="SynchronousGeneratorDefinition"/> preserves the historical dispatch-torque-only model.
/// </summary>
public sealed class SynchronousGridCouplingDefinition
{
    public SynchronousGridCouplingDefinition(
        Power maximumSynchronizingCorrectionPower,
        Power frequencyDampingPowerAtOneHertzSlip)
    {
        if (maximumSynchronizingCorrectionPower <= Power.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumSynchronizingCorrectionPower),
                maximumSynchronizingCorrectionPower,
                "Maximum synchronizing correction power must be greater than zero.");
        }

        if (frequencyDampingPowerAtOneHertzSlip <= Power.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(frequencyDampingPowerAtOneHertzSlip),
                frequencyDampingPowerAtOneHertzSlip,
                "Frequency-damping power at one hertz slip must be greater than zero.");
        }

        MaximumSynchronizingCorrectionPower = maximumSynchronizingCorrectionPower;
        FrequencyDampingPowerAtOneHertzSlip = frequencyDampingPowerAtOneHertzSlip;
    }

    /// <summary>
    /// Peak signed phase-angle correction magnitude. The applied term is Pmax*sin(delta), where positive delta means generator lead.
    /// </summary>
    public Power MaximumSynchronizingCorrectionPower { get; }

    /// <summary>
    /// Signed damping calibration: this much correction power is added per +1 Hz generator/grid frequency slip.
    /// Negative slip therefore unloads the shaft and positive slip increases electromagnetic loading.
    /// </summary>
    public Power FrequencyDampingPowerAtOneHertzSlip { get; }
}
