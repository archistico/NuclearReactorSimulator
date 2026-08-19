# M10.9.4.1-I.1 Static Review

## Scope

I.1 is intentionally compatibility/audit-only. It is built on H.30 and introduces no runtime algorithm, physics, selector or persistence-schema change.

## Production delta

Expected `src/` delta relative to H.30:

```text
src/NuclearReactorSimulator.Application/ApplicationDescriptor.cs
```

Metadata only.

The following runtime owners must remain unchanged:

- `DesktopHydraulicProductionPolicySelector`;
- `DesktopSustainedGenerationInitialConditionFactory`;
- `DesktopSustainedGenerationH29ActivationCandidateInitialConditionFactory`;
- `ColdShutdownInitialConditionFactory`;
- `PlantNetworkOrchestrator`;
- H.9 corrector;
- H.20 activation supervisor;
- H.22 commit seam.

## Compatibility findings

The desktop composition registers 12 exact-version factories across 9 IDs. Version `1` alone is not a legacy marker. Only identities with a later same-ID successor are classified compatibility-retained:

- `integrated-operations-desktop-stable@1`;
- `pre-synchronization-grid-loading@1`.

Both remain necessary for exact-version semantics and are therefore retained.

## Retirement findings

No exact-version profile is a safe deletion in I.1.

Two numerical modes are legitimate later retirement candidates because they are historical audit-only and are not selected by the H.30 production policy:

- `DeterministicHybridSemiImplicit`;
- `FourNodeBranchContinuityShadowIntegrated`.

They remain source-compatible for now because historical focused audits still use them. Retirement must wait for audit consolidation.

## Risk assessment

Primary risk is accidental archive/replay incompatibility caused by treating old versioned identities as dead code. I.1 fails closed against that risk by requiring zero `DELETE-NOW` exact versions and by checking exact registry resolution.

No long-running numerical requalification is required because no numerical implementation changes.


## Hotfix 1 static review

The reported build failure was analyzer-only (`xUnit2031`) at the unique-profile assertion. Hotfix 1 uses the filtering overload of `Assert.Single` and preserves the assertion semantics. No production runtime behavior is changed by the repair.
