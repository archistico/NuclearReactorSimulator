# M10.9.4.1-I.2 Static Review

## Scope

I.2 is validation-infrastructure hardening built on validated I.1 Hotfix 1. It introduces no physics or numerical algorithm change.

## Production delta

Expected `src/` delta relative to I.1 Hotfix 1:

```text
src/NuclearReactorSimulator.Application/ApplicationDescriptor.cs
```

Metadata only.

Runtime owners that must remain byte-identical include the production selector, v2/v3 initial-condition factories, `PlantNetworkOrchestrator`, H.9 corrector, H.20 activation supervisor and H.22 commit seam.

## Consolidation findings

Current baseline verification can be represented without rerunning the complete H-series research lineage:

- ordinary suite + H.30/I.1/I.2 current contracts are push/PR work;
- gameplay-long, operational-envelope and reference-scale remain current scheduled/manual long work;
- H.24 post-H.28 and H.28 are frozen Phase-H evidence;
- H.5 and H.21 are historical-frozen for current CI purposes.

However, the H.5/H.21 numerical modes are still referenced by historical executable tests and source construction seams. Therefore I.2 does not classify either mode as safe-to-delete.

## CI findings

`eng/ci-ordinary.cmd`, `eng/ci-current-evidence.cmd` and `eng/ci-long.cmd` are the authoritative provider-neutral entry points. GitHub workflows are thin wrappers around those scripts and use the repository `global.json` rather than embedding a competing SDK contract.

## Risk assessment

Primary risk is accidentally treating frozen historical evidence as permission to remove still-compiled compatibility code. I.2 explicitly keeps current-CI dependency and source dependency as separate columns and requires `legacy-mode-retirement-authorized=False`.

No long-horizon requalification is justified because production runtime is unchanged.
