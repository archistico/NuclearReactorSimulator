namespace NuclearReactorSimulator.Application.Scenarios.Analysis;

public interface IPostIncidentAnalysisSerializer
{
    string Serialize(PostIncidentAnalysisReport report);
    PostIncidentAnalysisReport Deserialize(string content);
}
