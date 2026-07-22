namespace NuclearReactorSimulator.Application.Scenarios.Recording;

public interface IScenarioCheckpointSerializer
{
    string Serialize(ScenarioCheckpoint checkpoint);
    ScenarioCheckpoint Deserialize(string content);
}
