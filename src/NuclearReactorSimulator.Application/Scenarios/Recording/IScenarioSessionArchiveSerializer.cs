namespace NuclearReactorSimulator.Application.Scenarios.Recording;

public interface IScenarioSessionArchiveSerializer
{
    string Serialize(ScenarioSessionArchive archive);
    ScenarioSessionArchive Deserialize(string content);
}
