# M10.9.7.4 Exact-V9 Hosted Runtime Alignment Diagnostic 1

Status: `READY-FOR-HOSTED-EXECUTION`

<!-- NRS-MARKER:M10974-EXACT-V9-HOSTED-RUNTIME-ALIGNMENT-DIAGNOSTIC1 -->

## Purpose

This is the stop condition for the cross-host ULP investigation. Diagnostic 1 through 4 established that the local and GitHub trajectories are bit-identical for 127 of 128 Exact-V9 steps. The sole divergence is step 126, where one shared STOP-out / CONTROL-in pressure node differs by one ULP at approximately 6.07 MPa. The resulting commanded-flow and shaft-power differences are of relative order 1e-15 and disappear at the next step.

The remaining material environmental difference is runtime patch level: the local frozen anchor is reproduced on .NET 10.0.5, while the hosted RED evidence is produced on .NET 10.0.12. A raw SHA-256 over IEEE-754 payloads is only a meaningful bit-exact regression contract when the execution runtime is itself controlled.

## Experiment

The repository's permanent `global.json`, `ordinary-ci.yml`, `ci-ordinary.cmd`, production source, test source and frozen golden hashes remain unchanged.

A separate one-shot GitHub workflow installs SDK `10.0.105`, which is the SDK servicing release that includes runtime `10.0.5`. The diagnostic then creates an isolated temporary `global.json` with SDK roll-forward disabled, builds only the Application test project with `RuntimeFrameworkVersion=10.0.5` and `RollForward=Disable`, validates the generated runtimeconfig, and executes only the already-authoritative Exact-V9 method.

The existing Exact-V9 diagnostic instrumentation records the actual framework and trajectory aggregate. The workflow uploads all evidence whether the test passes or fails.

## Decision rule

There are only two outcomes:

- GREEN with trace framework `.NET 10.0.5` and aggregate `7880AD...B5418`: runtime patch mismatch is confirmed as the CI blocker. The next and final CI-harness change will pin the permanent ordinary CI bit-exact baseline to the qualified runtime and remove this one-shot workflow.
- RED while the trace proves `.NET 10.0.5`: runtime patch mismatch is insufficient. No Diagnostic 5 follows. The project moves to a separately versioned numerical-canonicalization contract for cross-host determinism rather than continuing to chase individual ULPs.

Neither outcome changes reactor physics, tolerances, Fingerprint V1, the Exact-V9 historical golden or VR2/R3 authority.
