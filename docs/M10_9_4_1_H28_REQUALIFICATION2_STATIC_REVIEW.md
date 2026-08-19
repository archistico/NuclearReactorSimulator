# M10.9.4.1-H.28 Requalification 2 — Static Review

## Baseline

User-validated H.28.1-G.

## Runtime delta

No numerical runtime implementation changes are introduced by Requalification 2.

Under `src/`, only `NuclearReactorSimulator.Application/ApplicationDescriptor.cs` changes and it is metadata only.

## Audit restoration

`FourNodePerformanceOperationalSoakAuditTests` is restored from H.28 Requalification 1. The explicit H.28 benchmark / soak method and all hard ceilings are unchanged. Only provenance is updated from frozen H.28.1-D evidence to frozen user-validated H.28.1-G evidence.

`run-four-node-performance-cost-operational-soak-audit.cmd` retains the same H.28 focused gate and artifact checks, with the prerequisite evidence check updated to H.28.1-G.

## Frozen H.28.1-G artifacts

- summary SHA-256: `260904E90A4D7B6E64F109BAF6FFE76A27DDCA7B82390348E01EBE4B380CC1E2`;
- steps SHA-256: `CBEC443C90CA49CB88A878352A5CAC392FBA9825E75C864AD1FD4FBD34AA05A0`;
- metrics SHA-256: `4E868C072485FC575A4D71CBAC0FB230809F119A26B44CD1F9B6FEDD6F92A2CC`.

## Expected interpretation

- Green H.28: performance block removed, default mode still ExplicitCommittedState, rerun H.24 once before H.29.
- Red H.28: retain H.28.1-G as validated optimization evidence and keep H.29 blocked. Do not raise H.28 ceilings.
