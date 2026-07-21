namespace NuclearReactorSimulator.Application.Scenarios.Training;

/// <summary>Presentation-only procedure-assistance mode. Changing it never changes simulation inputs or scoring semantics.</summary>
public enum TrainingGuidanceMode
{
    Hidden = 0,
    ChecklistOnly = 1,
    Guided = 2,
}
