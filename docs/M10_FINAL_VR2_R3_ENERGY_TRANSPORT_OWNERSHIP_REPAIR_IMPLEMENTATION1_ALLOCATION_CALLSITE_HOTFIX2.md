# M10 Final VR2 R3 Energy-Transport Ownership Repair Implementation 1 - Allocation Callsite Hotfix 2

## Status

Test-only measurement hotfix. Production repair remains unchanged and R3 remains RED.

## Trigger

Allocation Test Hotfix 1 still reported exactly 24 allocated bytes in the authoritative measurement window after 20,000 warmup calls and one preceding 10,000-call stabilization window. The failing source line was the interface invocation inside the third syntactically distinct loop.

## Adjudication

The fixed 24-byte result does not scale with the 10,000 lookups and therefore is not evidence of a recurring per-lookup allocation. The previous test also warmed a different interface callsite from the callsite being measured. This hotfix makes the measurement stricter rather than looser: all warmup and measured windows call a single `MethodImplOptions.NoInlining` helper, so the same interface-dispatch callsite is warmed before allocation accounting.

The authoritative second measurement window still requires an exact delta of `0` bytes. No tolerance is added to the steady-state criterion.

## Frozen boundaries

- No production file change.
- No C4 payload change.
- No IF97 runtime dependency.
- No transport tolerance change.
- No fast-gate threshold change.
- No default closure or Exact-V9 change.
- R3 remains RED until Implementation 1 and subsequent Requalification 3 pass.

## Execution

Run:

```powershell
.\scripts\run-m10-final-vr2-r3-energy-transport-ownership-repair-implementation1.cmd
```
