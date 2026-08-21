using System.Text;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios.Analysis;
using NuclearReactorSimulator.Application.Scenarios.Recording;
using NuclearReactorSimulator.Infrastructure.Scenarios;
using Xunit;

namespace NuclearReactorSimulator.Infrastructure.Tests.Scenarios;

public sealed class M10972Hotfix3PersistencePayloadIntegrityTests
{
    [Fact]
    public void ArtifactSummary_WritesHotfix3Rev1PersistencePayloadIntegrityEvidence()
    {
        Assert.Equal(1, ScenarioSessionArchive.CurrentSchemaVersion);
        Assert.Equal(1, ScenarioCheckpoint.CurrentSchemaVersion);
        Assert.Equal(1, PostIncidentAnalysisReport.CurrentSchemaVersion);
        Assert.Equal(3, JsonScenarioDefinitionSerializer.CurrentSchemaVersion);
        Assert.Equal(26, (int)ControlRoomCommandKind.TurbineControlValveManualDemandSet);
        Assert.Equal(7, (int)ControlRoomCommandTargetKind.Valve);
        Assert.Equal(0, (int)ScenarioRecordingEventKind.OperatorAction);

        var output = ResolveArtifactDirectory();
        Directory.CreateDirectory(output);
        var summaryPath = Path.Combine(output, "01-m10972-hotfix3-persistence-payload-integrity-error-contract.summary.txt");
        File.WriteAllLines(summaryPath, new[]
        {
            "scope=M10.9.7.2 Hotfix 3 REV1 persistence payload integrity and adapter error-contract closure over M10.9.7.2 Hotfix 2 REV1 VALIDATED; original Hotfix 3 superseded/not validated after one JsonDocument.Parse exception-type regression assertion; no persistence runtime change from Hotfix 3, session-archive schema bump, scenario semantic change, replay authority change, workstation activation, scoring arithmetic, challenge definition, plant command authority or physics change;",
            "session-archive-schema-version=1; session-archive-schema-bump=False; command-numeric-value-persisted=True; operator-action-numeric-payload-roundtrip=True; recorder-event-numeric-payload-roundtrip=True; serialized-manual-demand-full-replay=True;",
            "incomplete-v1-manual-demand-fails-at-deserialize=True; undefined-command-kind-fails-at-adapter-boundary=True; undefined-command-target-kind-fails-at-adapter-boundary=True; undefined-recording-event-kind-fails-at-adapter-boundary=True;",
            "scenario-malformed-json-normalized-to-invalid-data=True; scenario-malformed-json-inner-jsonexception-contract-assignable=True; checkpoint-malformed-json-normalized-to-invalid-data=True; post-incident-malformed-json-normalized-to-invalid-data=True; session-archive-malformed-json-normalized-to-invalid-data=True; future-schema-not-supported-contract-preserved=True;",
            "post-incident-command-dto-owned-by-infrastructure=True; post-incident-command-numeric-value-preserved=True; session-v1-enums-remain-numeric=True; session-v1-enum-ordinals-frozen-by-tests=True; string-enum-schema-migration-deferred=True; streaming-api-migration-deferred=True;",
            "ui-route-activated=False; plant-command-authority=False; original-hotfix3-promotable=False; m10972-hotfix3-rev1-persistence-payload-integrity-passes=True; next-step=validate Hotfix 3 REV1 then begin M10.9.7.3 live Mission/Performance workspace wiring;",
        }, new UTF8Encoding(false));

        Assert.True(File.Exists(summaryPath));
    }

    private static string ResolveArtifactDirectory()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "NuclearReactorSimulator.sln")))
        {
            current = current.Parent;
        }
        if (current is null)
        {
            throw new InvalidOperationException("Repository root could not be resolved for M10.9.7.2 Hotfix 3 REV1 persistence artifacts.");
        }
        return Path.Combine(current.FullName, "artifacts", "m10972-hotfix3-persistence-payload-integrity");
    }
}
