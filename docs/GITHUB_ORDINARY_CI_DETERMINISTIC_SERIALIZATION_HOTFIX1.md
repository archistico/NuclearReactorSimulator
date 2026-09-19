# GitHub Ordinary CI Deterministic Serialization Hotfix 1

## Status

**Execution candidate / CI-harness-only hotfix.**

The hosted `ordinary-ci` run restores and builds Release successfully with zero warnings and zero errors, then returns one test failure from `NuclearReactorSimulator.Application.Tests` while the other four test assemblies pass. The supplied workflow excerpt reports 1,525 total tests: 1,370 succeeded, 154 skipped and 1 failed. The excerpt does not expose the exact failing test method or stack trace.

This hotfix does **not** classify that single test as flaky and does not assert that parallel scheduling is already proven as its root cause. It removes two uncontrolled scheduling differences from the ordinary gate and makes a remaining failure easier to identify.

## Change

`eng/ci-ordinary.cmd` remains the sole ordinary CI entry point and now:

1. sets `CI=true` inside its own `setlocal` scope, so invoking the same script locally and on GitHub selects the same `ContinuousIntegrationBuild` branch in `Directory.Build.props`;
2. invokes `dotnet test` with `--max-parallel-test-modules 1`, so Microsoft.Testing.Platform executes one test module at a time;
3. forwards `--parallel none` to xUnit v3, disabling test-collection parallel execution inside each module;
4. requests detailed Microsoft.Testing.Platform output plus xUnit information so a persistent failure exposes useful identity/context in the job log;
5. preserves immediate non-zero exit on any failed command.

The complete ordinary suite is still executed. There is no `--filter`, retry loop, `continue-on-error`, skipped failing assembly, threshold change or test-source edit.

## Why this is the smallest safe CI change

The hosted workflow already uses the same repository command, but previously the effective environment and scheduling were not fully pinned by that command itself. GitHub supplied `CI=true`, while a local direct invocation did not. In addition, neither Microsoft.Testing.Platform module-level concurrency nor xUnit collection-level concurrency was explicitly frozen in the ordinary entry point.

The objective of Hotfix 1 is therefore **deterministic reproduction**, not suppression. If the one `Application.Tests` failure was caused by shared-state or scheduling interference, the serialized gate should go green. If it remains, the run must remain RED and the more detailed log becomes the evidence for a targeted test/root-cause repair.

## Diagnostic 2 returned-evidence checkpoint

Before this CI maintenance activity, R3 Seed Integration Two-Seed-Step Preconditioning Divergence Diagnostic 2 Adjudicator Hotfix 1 returned PASS as an evidence/adjudicator gate. The returned 01–09 artifact set is frozen with this candidate.

The engineering result remains:

- raw phase mismatch count = 0;
- first phase divergence = `seed-step1`;
- first divergent node = `suction`;
- candidate phase at that checkpoint = `SaturatedMixture` versus canonical mode-1 `SubcooledLiquid`;
- seed-step1 governor delta = 0, so controller response is not the initiating signal in the returned evidence;
- R3 remains RED;
- no production repair, seed retuning, threshold change, C4 change, canonical exact-v9 change, R3 Requalification 3 or R4 planning is authorized by this CI hotfix.

## Validation sequence

From a PowerShell prompt at repository root, execute:

```powershell
.\scripts\run-github-ordinary-ci-deterministic-hotfix1.cmd
```

The runner first performs a fail-closed static contract/provenance audit and then calls the exact ordinary CI entry point used by GitHub. A PASS requires the complete ordinary suite and `ci-current-evidence.cmd` to pass; there is no fallback path.

After the local run is green, push the candidate and confirm the hosted `ordinary-ci` workflow is also green. If it remains RED, retain the complete GitHub failure output; the next repair must target the named failing test or its demonstrated dependency rather than weakening the gate.
