# M11 COTS / Dependency Assurance Plan — Reviewed Planning Baseline

## Source principle

Chapter 8 of the 1997 NRC/National Research Council report treats commercial off-the-shelf hardware/software assurance as a graded activity: the criteria and verification effort should be commensurate with application significance and complexity.

For this educational desktop simulator, the appropriate translation is **dependency assurance**, not nuclear-grade dedication.

## Current direct dependency inventory

Current tree evidence:

| Dependency | Current declared version / contract | Role | Can affect released runtime? | M11 assurance level |
|---|---|---|---|---|
| .NET target/runtime | `net10.0`; `global.json` SDK baseline `10.0.100` with `latestFeature` roll-forward | Runtime, GC, BCL, JIT, file I/O, crypto, threading | Yes | High for support/packaging; representative release tests required |
| Avalonia | 12.1.0 | Desktop UI framework | Yes | High for desktop packaging/HMI behavior |
| Avalonia.Desktop | 12.1.0 | Desktop host/backend integration | Yes | High for launch/input/windowing verification |
| Avalonia.Fonts.Inter | 12.1.0 | UI font asset package | Yes, presentation/assets | Medium; packaged asset/render check |
| Avalonia.Themes.Fluent | 12.1.0 | UI theme resources | Yes, presentation | Medium; resource/load check |
| xUnit v3 | 3.2.2 | Test framework only | No production runtime | High for validation-tool integrity, not runtime support claim |
| Microsoft.Testing.Platform | selected through `global.json` runner setting | Test runner | No production runtime | High for gate discovery/runner correctness |

## Assurance rules

### Runtime dependencies

For .NET/Avalonia dependencies, M11 should freeze:

- exact package versions in `Directory.Packages.props`;
- supported OS/publish target(s);
- framework-dependent vs self-contained decision;
- tested runtime requirement;
- clean-machine launch evidence;
- assets/fonts/resources presence;
- save/load/replay representative behavior on packaged build.

A dependency update after M11.1 freeze is a release change and must rerun the affected gate set.

### Test-only dependencies

For xUnit/Microsoft.Testing.Platform:

- focused scripts must assert expected test discovery rather than accepting a zero-test run;
- filter classes/methods should resolve before expensive gates where practical;
- runner syntax/version assumptions should be centralized/documented;
- a toolchain update that changes discovery semantics requires gate requalification.

This is especially important because a “no tests executed” exit can be a tooling/filter problem rather than a product failure—and must never be mistaken for a passing route.

### Do not over-qualify

M11 should **not** attempt to prove internal correctness of .NET, Avalonia or xUnit. Instead it should demonstrate that the exact supported combination works for the product's declared scope through configuration freeze, packaging verification, representative execution and regression evidence.

## Proposed M11 deliverable

Create `eng/m11-release-dependency-matrix.json` with fields:

```text
id
name
version_or_runtime_contract
role
runtime_or_test_only
release_impact
supported_target_requirements
verification_routes
known_limitations
change_requires_rerun_of
```

The matrix should be consumed by M11.4 packaging and M11.6 final closure checks.
