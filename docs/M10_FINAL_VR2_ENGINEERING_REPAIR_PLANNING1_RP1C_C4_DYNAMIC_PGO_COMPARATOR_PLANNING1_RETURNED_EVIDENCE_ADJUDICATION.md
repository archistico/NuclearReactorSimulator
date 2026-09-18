# M10 Final - VR2 Engineering Repair Planning 1 - RP1C C4 Dynamic PGO Comparator Planning 1 Returned-Evidence Adjudication

## Status

**PASS - RETURNED PLANNING EVIDENCE ACCEPTED AS AUTHORED.**

The returned four-file planning artifact set is complete and internally consistent with the frozen Planning 1 candidate. It freezes only the separately versioned evidence gate `RP1C-C4-EXACT-V9-DYNAMIC-PGO-COMPARATOR1`.

## Returned contract accepted

The returned planning evidence records:

- `status=PASS-AS-AUTHORED`;
- Runtime Factor Isolation 1 returned adjudication `PASS`;
- roadmap branch `A-RUNTIME-SENSITIVE`;
- TieredCompilation material single-factor effect retained;
- QuickJit rare-tail causality not promoted;
- QuickJitForLoops material tail benefit false;
- Dynamic PGO comparator material true;
- 2 runtime modes, 5 fresh processes per mode, 10 total processes;
- 230,400 total measured calls;
- exactly 46 required evidence files;
- strict maximum unchanged at `409.30666666666673 us`;
- `100 us` diagnostic floor remains non-qualifying;
- ambient/unset mode excluded;
- RP1C selection and production runtime change remain unauthorized.

The returned matrix changes exactly one factor:

- `PGO-OFF-QJFL-ON`: TieredCompilation=1, TieredPGO=0, QuickJit=1, QuickJitForLoops=1, ReadyToRun=1;
- `PGO-ON-QJFL-ON`: TieredCompilation=1, TieredPGO=1, QuickJit=1, QuickJitForLoops=1, ReadyToRun=1.

The only changed factor is `DOTNET_TieredPGO`.

## Adjudication

Planning 1 is accepted. This adjudication authorizes implementation and local execution of **Dynamic PGO Comparator 1 as evidence-only** under the returned contract.

It does not authorize a Runtime Configuration Impact Assessment, effective-default equivalence testing, RP1C selection, any production/runtime change, production repair, C4 mutation, threshold change, exact-v9 change, VR3, P3-R1 or a second replacement-long baseline.

A negative, neutral or inconclusive PGO comparison is engineering evidence and must not be converted into infrastructure RED. Infrastructure RED remains limited to build, caller-environment, corpus/candidate integrity, measurement-matrix integrity, allocation/harness integrity and required-evidence-shape failures.

## Next action

Implement and execute only:

`RP1C-C4-EXACT-V9-DYNAMIC-PGO-COMPARATOR1`

After local completion, return the complete 46-file evidence tree before any causal adjudication or Branch A2 planning.
