# M10 Final — VR2 — R1 Selected C4 Opt-In Closure Implementation 1 — Returned-Evidence Adjudication

## Status

**RETURNED — PASS**

The returned seven-file evidence tree has been reviewed independently against the R1 implementation contract and the executed Hotfix 7 candidate. R1 is qualified as **implementation evidence only**.

## Returned evidence accepted

The returned evidence establishes all of the following:

- exactly seven required artifacts are present;
- payload `NRSVR2C4-v1` is 1,531,264 bytes with SHA-256 `EF49B1D097FC63F1F1254E425F46C6ACA837B82C58EFBA9C7774727C51C82267`;
- two independent C# regenerations are byte-identical to the checked-in payload and the compiled hash anchor;
- 1,679 state comparisons and 288 hydraulic comparisons complete with zero semantic mismatches;
- repeat, resolve, resolution-path and canonical Domain-unit integration mismatch counts are zero;
- resolve-time allocation, payload runtime I/O and payload decode counts are zero;
- mode 0/default regression passes;
- mode 1 preservation is proven by legacy source projection SHA-256 `93C5212C09D5D7362531398DE1CED105589D93DF9A892A3E6BE6BF401446D55E` plus the ordinary Release suite;
- ordinary Release passes;
- default construction and exact-v9 activation remain unchanged.

The seven returned artifact hashes are frozen under `eng/frozen-evidence/ordinary/M10FinalVR2_R1_SelectedC4_OptInClosure_Implementation1_Artifacts`:

```text
01-contract-and-provenance.txt=B8674F6BD34C10EC5CC652FB3EE9E1F7616FE9A3D5F44D23B5C951CABBF00677
02-production-change-manifest.csv=CCCB4B1E3FB59C2D33DEBA9E1538F481E86E0941E8377CF280B7A7FC51F653CB
03-reference-data-provenance.txt=A1F89F9328261B5C0F54D6DEC864D34B43159A0749130DC8B18918E2FF65EAD6
04-c4-production-equivalence-summary.txt=C8E345693AB30518695906B28EEF09E8CD5C042CCE0D689FD16DD1CEBFBCC33B
05-historical-mode-regression-summary.txt=83F3600A430AE209FADC8BC3A0DC71AE872B625D89F8C59E79985AED15312F83
06-r1-implementation-summary.txt=994487A59E28A3C5FC01B422FB5DBC03C95BB3A74EA227DA0092A7AD6AFCC942
07-prequalification-review.txt=ACCD3C7E0FB419EAAE7980A507659AC4A7A84D76369A3037838635677582AC08
```

The machine-readable returned review is frozen at `eng/frozen-evidence/ordinary/M10FinalVR2_R1_SelectedC4_OptInClosure_Implementation1_ReturnedAudit.txt`.

## Adjudication

```text
R1-SELECTED-C4-OPT-IN-CLOSURE-IMPLEMENTATION1=PASS
MODE2-IMPLEMENTATION=QUALIFIED-OPT-IN-ONLY
R2-PLANNING-AUTHORIZED=True
R2-EXECUTION-AUTHORIZED=False
DEFAULT-ACTIVATION-AUTHORIZED=False
EXACT-V9-ACTIVATION-AUTHORIZED=False
PRODUCTION-RUNTIME-CHANGE-AUTHORIZED=False
VR3-AUTHORIZED=False
P3-R1-AUTHORIZED=False
SECOND-REPLACEMENT-LONG-AUTHORIZED=False
```

The only authorized successor is planning-only `R2-FOCUSED-THERMODYNAMIC-REFERENCE-TOPOLOGY-QUALIFICATION-PLANNING1`. No R2 execution, exact-v9 composition, default switch or versioned activation follows automatically from this adjudication.
