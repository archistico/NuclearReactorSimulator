# M10 Final VR2 — R3 Short Exact-v9-Equivalent Shadow / Composition Requalification 2 — Returned Evidence Adjudication

## Status
**RED — R3-SHADOW-COMPOSITION-BLOCKING**

Requalification 2 no longer throws the earlier `WaterSteamStateOutOfRangeException`: the repaired mode-2 shadow completes all `12,000` steps and remains finite, trip-free, breaker-closed, conservative and deterministic.

However the exact-v9 operating-point health envelope is violated in `12,000/12,000` steps.

Returned evidence:
- canonical exact-v9 vs mode-1 shadow: `128/128` fingerprint equality;
- non-finite steps: `0`;
- health-envelope violation steps: `12,000`;
- rollbacks: `3,005`;
- electrical range: approximately `4.035..4.989 MWe`;
- primary-pump range: approximately `-35.77..170.02 kg/s`;
- drum-level range: approximately `0.492..0.548`;
- governor output range: approximately `30.60..55.80 %`;
- deterministic repeat: PASS.

The first 1 s sample is already materially displaced while rollback count is still zero, so rollback accumulation cannot explain the initial divergence.

R3 remains RED. No threshold relaxation, R4 planning, default mode-2 activation or exact-v9 mutation is authorized.
