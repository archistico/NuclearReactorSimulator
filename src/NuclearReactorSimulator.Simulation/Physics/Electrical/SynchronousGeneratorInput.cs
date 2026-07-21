using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Simulation.Physics.Electrical;

/// <summary>
/// Manual-first M4.5 generator command surface. Automatic excitation/load control belongs to M5.
/// </summary>
public sealed record SynchronousGeneratorInput
{
    public SynchronousGeneratorInput(
        string generatorId,
        ElectricPotential terminalLineVoltage,
        Power requestedElectricalPower,
        bool closeBreakerCommand = false,
        bool openBreakerCommand = false)
    {
        if (string.IsNullOrWhiteSpace(generatorId))
        {
            throw new ArgumentException("Generator-input id cannot be empty or whitespace.", nameof(generatorId));
        }

        if (requestedElectricalPower < Power.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(requestedElectricalPower), requestedElectricalPower, "Requested generator electrical power cannot be negative.");
        }

        if (closeBreakerCommand && openBreakerCommand)
        {
            throw new ArgumentException("Generator breaker close and open commands cannot both be active in the same step.");
        }

        GeneratorId = generatorId.Trim();
        TerminalLineVoltage = terminalLineVoltage;
        RequestedElectricalPower = requestedElectricalPower;
        CloseBreakerCommand = closeBreakerCommand;
        OpenBreakerCommand = openBreakerCommand;
    }

    public string GeneratorId { get; }

    public ElectricPotential TerminalLineVoltage { get; }

    public Power RequestedElectricalPower { get; }

    public bool CloseBreakerCommand { get; }

    public bool OpenBreakerCommand { get; }
}
