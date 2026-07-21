using NuclearReactorSimulator.Domain.Physics.TurbineIsland.Integration;
using NuclearReactorSimulator.Simulation.Physics.Electrical;

namespace NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Integration;

/// <summary>
/// Complete M4.6 manual inputs. M4.6 adds no hidden control law and therefore delegates commands to the validated M4.5 input boundary.
/// </summary>
public sealed class IntegratedSecondaryCycleInputs
{
    public IntegratedSecondaryCycleInputs(
        IntegratedSecondaryCycleDefinition definition,
        GeneratorGridInputs generatorGridInputs)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        GeneratorGridInputs = generatorGridInputs ?? throw new ArgumentNullException(nameof(generatorGridInputs));

        if (!ReferenceEquals(generatorGridInputs.Definition, definition.GeneratorGridSystem))
        {
            throw new ArgumentException(
                "Generator/grid inputs do not use the integrated secondary cycle's canonical M4.5 definition.",
                nameof(generatorGridInputs));
        }
    }

    public IntegratedSecondaryCycleDefinition Definition { get; }

    public GeneratorGridInputs GeneratorGridInputs { get; }
}
