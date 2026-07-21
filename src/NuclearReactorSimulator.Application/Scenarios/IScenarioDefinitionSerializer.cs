namespace NuclearReactorSimulator.Application.Scenarios;

/// <summary>Persistence-format boundary for versioned scenario definitions.</summary>
public interface IScenarioDefinitionSerializer
{
    string Serialize(ScenarioDefinition scenario);

    ScenarioDefinition Deserialize(string content);
}
