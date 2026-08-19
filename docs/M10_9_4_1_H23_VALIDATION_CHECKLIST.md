# M10.9.4.1-H.23 Hotfix 2 Validation Checklist — VALIDATED

> Validation history (2026-08-18): the first H.23 build failed only with CS0246 in the focused test; Hotfix 1 added `using NuclearReactorSimulator.Domain.Plant;` and compilation then passed. The next ordinary/focused run failed on one case-sensitive `ApplicationDescriptorTests` substring: expected `Standard factories remain ExplicitCommittedState`, actual descriptor text contains lowercase `standard factories remain ExplicitCommittedState at 10 ms.` Hotfix 2 changes only that expected substring; the H.23 experiment, descriptor and runtime are unchanged.

## Baseline and scope

- [x] candidate is built directly on user-validated **H.22**;
- [x] H.22 is documented as validated with 443/443 eligible/authorized/committed corrections and zero unsafe/fallback-commit violations;
- [x] H.22 numerical runtime code is unchanged by H.23;
- [x] standard current-v2 remains `ExplicitCommittedState` at 10 ms;
- [x] H.20 authority and H.22 commit seam are unchanged;
- [x] P060/F040, H.9, 2% / 5 K hysteresis and `steam|stop-out|header|turbine-inlet` are unchanged;
- [x] H.23 exact-version evidence factory exists only in the test assembly.

## Ordinary gate

```bat
APPLY_UPDATE.cmd
dotnet build
dotnet test
```

- [x] build passes with warnings-as-errors;
- [x] complete ordinary suite passes;
- [x] ApplicationDescriptor identifies H.23 as candidate over validated H.22;
- [x] all three frozen H.22 evidence fingerprints pass;
- [x] existing recorder/replay/checkpoint tests remain green;
- [x] existing protection tests remain green.

## Focused gate

```bat
scripts\run-four-node-committed-replay-protection-qualification-audit.cmd
```

H.23 intentionally does not rerun the expensive H.22 cumulative numerical gate because it changes no H.22 numerical runtime code. The focused script first verifies the frozen user-validated H.22 artifacts, then runs the H.23 exact-version replay/protection audit.

Required results:

- [x] pre-trip H.22 committed control segment remains trip-free;
- [x] corrected commits are observed on the recorded H.23 trajectory;
- [x] reverse-power pickup reaches a non-zero in-flight timer before generator trip;
- [x] checkpoint is created while reverse-power pickup is in-flight and generator trip is false;
- [x] generator trip subsequently becomes active;
- [x] reverse-power protection is latched at the final state;
- [x] generator breaker is open at the final state;
- [x] full replay final snapshot fingerprint equals the recording;
- [x] full replay internal H.20/H.22/protection trace equals the recording trace exactly;
- [x] checkpoint seek fingerprint equals the recorded checkpoint;
- [x] restored checkpoint retains in-flight reverse-power pickup and no generator trip;
- [x] continuation from checkpoint reaches exactly the original final fingerprint;
- [x] restored prefix + continuation internal trace equals the original trace exactly;
- [x] deterministic telemetry/protection trace fingerprint repeat = true;
- [x] corrected commits > 0;
- [x] fallback commit violations = 0;
- [x] unsafe corrected commits = 0;
- [x] every committed correction remains evaluated/converged, non-line-search-exhausted and inside H.20 pressure/flow/mass/energy guard limits;
- [x] every protection-transient step remains within H.22 network mass closure `1e-6 kg`, energy closure `1e-2 J`, balance mass-rate `1e-8 kg/s` and balance power `1e-3 W` limits;
- [x] every H.20 rollback, if any, remains explicit and uncommitted;
- [x] standard desktop factory remains `ExplicitCommittedState`.

## Expected artifacts

```text
artifacts\h23-four-node-committed-replay-protection-qualification\
  00-progress.txt
  01-four-node-committed-replay-checkpoint-protection.summary.txt
  02-replay-protection-trace.csv
  03-four-node-committed-replay-protection-metrics.csv
```

- [x] `four-node-committed-replay-checkpoint-protection-qualification-passes=True`;
- [x] `h23-audit-passes=True`.

## Promotion rule

H.23 Hotfix 2 is **VALIDATED**: local build, complete ordinary suite and focused H.23 gate passed on 2026-08-18. Standard current-v2 remains explicit. The next milestone is H.24 committed long-horizon/cross-profile qualification; the broader H.24–H.30 roadmap then requires protection/transient, rollback, off-design, performance/soak and explicit activation/closure gates.
