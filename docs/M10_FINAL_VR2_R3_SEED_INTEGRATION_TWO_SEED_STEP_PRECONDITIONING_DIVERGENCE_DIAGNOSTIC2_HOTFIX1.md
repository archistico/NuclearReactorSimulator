# M10 Final VR2 — R3 Diagnostic 2 Adjudicator Hotfix 1

## Status

**RETURNED-EVIDENCE ADJUDICATOR HOTFIX — NO DYNAMIC RE-RUN**

The Diagnostic 2 focused execution successfully wrote returned evidence files `01` through `06`. Execution then failed only at stage `[4/4] Evidence adjudication` with `PropertyNotFoundStrict` on `$rawPhase.Count`.

## Root cause

`PhaseNodes` emits pipeline objects. The raw checkpoint correctly contains zero phase mismatches. In Windows PowerShell, assigning a function invocation that emits zero objects produces `$null`, not a stable empty array. With `Set-StrictMode -Version Latest`, reading `$rawPhase.Count` on `$null` raises the observed property-not-found exception.

This is an adjudicator collection-shape defect. It is not evidence of a simulation, thermodynamic, seed, controller, or production-code failure.

## Hotfix

Hotfix 1 leaves the original Diagnostic 2 candidate and its contract untouched and adds a separate adjudicator. The only semantic correction is explicit array capture at the call boundary:

```powershell
$rawPhase=@(PhaseNodes $raw)
$s1Phase=@(PhaseNodes $s1)
$s2Phase=@(PhaseNodes $s2)
```

This guarantees collection cardinality `0/1/N` under Windows PowerShell and `Set-StrictMode`.

## Returned evidence reuse

The six returned CSV files are embedded unchanged and SHA-256 locked by the Hotfix 1 contract. No restore, build, focused test, seed preconditioning, or dynamic simulation is repeated.

The already-returned evidence directly shows:

- raw checkpoint: zero phase mismatches;
- seed step 1: first phase mismatch at `suction` (`SubcooledLiquid` versus `SaturatedMixture`);
- seed step 2: the same `suction` phase mismatch remains;
- the controller/governor path is essentially coincident at step 1 and only microscopically separated at step 2;
- hydraulic and primary-flow displacement grows between step 1 and step 2.

These observations remain diagnostic evidence only. Engineering classification and any next causal-seam planning must occur after the Hotfix 1 outputs are returned and reviewed.

## Authority

Hotfix 1 does not authorize seed retuning, threshold change, C4 change, canonical exact-v9 change, production repair, a new exact-version identity, R3 Short Requalification 3, R4 Planning 1, VR3, P3-R1, or a second replacement-long baseline.

R3 remains **RED** and R4 remains **BLOCKED**.

## Execution

From repository root in PowerShell:

```powershell
.\scripts\run-m10-final-vr2-r3-seed-integration-two-seed-step-preconditioning-divergence-diagnostic2-hotfix1-adjudication.cmd
```

This command audits the original Diagnostic 2 candidate plus the exact returned files `01`–`06`, then runs adjudication only. Return the complete Diagnostic 2 artifact directory, now including `07`, `08`, and `09`.
