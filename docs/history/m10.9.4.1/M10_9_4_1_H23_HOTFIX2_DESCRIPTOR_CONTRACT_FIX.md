# M10.9.4.1-H.23 Hotfix 2 — ApplicationDescriptor Case-Sensitive Contract Fix

## Status

**VALIDATED.** The complete H.23 Hotfix 2 build, ordinary suite and focused gate passed locally on 2026-08-18; H.23 Hotfix 2 is the authoritative validated baseline for H.24.

## Failure observed

After H.23 Hotfix 1 repaired the missing `NuclearReactorSimulator.Domain.Plant` import, compilation passed. The next ordinary test run and the focused H.23 gate stopped on the same `ApplicationDescriptorTests.Current_DescribesM10941H23ReplayCheckpointProtectionQualificationCandidate` assertion.

The descriptor contains:

```text
standard factories remain ExplicitCommittedState at 10 ms.
```

The test searched case-sensitively for:

```text
Standard factories remain ExplicitCommittedState
```

`Assert.Contains(string, string)` is case-sensitive, so the capital `S` made the contract fail even though the semantic text was present.

## Fix

Only the test expectation is changed:

```csharp
Assert.Contains("standard factories remain ExplicitCommittedState", descriptor.Status);
```

The descriptor text is not changed. No source/runtime algorithm, H.20 authority, H.22 commit seam, replay/checkpoint behavior, protection logic, frozen evidence, tolerance, trigger, target set or physical coefficient changes.

## Validation

```bat
APPLY_UPDATE.cmd
dotnet build
dotnet test
scripts\run-four-node-committed-replay-protection-qualification-audit.cmd
```

All three gates passed on 2026-08-18. The hotfix changed only the case-sensitive test expectation and did not change H.23 runtime behavior.
