# RP1C Engineering Repair Selection 1 — Pre-execution Review

1. Selection Planning 1 returned `PASS-AS-AUTHORED` with four complete artifacts.
2. The decision space is exactly `SELECT-C4 | SELECT-NONE` and `SELECT-NONE` remains mandatory.
3. C4 + `AMBIENT-UNSET` is selection-ready after green FDPC2.
4. D3 is not selection-ready because its frozen seam maximum remains above the unchanged ceiling.
5. FDPC1 negative historical evidence remains preserved.
6. The authored selection is `SELECT-C4`; it is not an automatic consequence of selection-ready count.
7. This gate performs no new measurement and does not mutate candidates, corpora, runtime configuration or thresholds.
8. The gate writes exactly four decision artifacts.
9. A returned `SELECT-C4` result may authorize only `R1-IMPLEMENTATION-PLANNING-ONLY` after separate returned-selection adjudication.
10. Production repair and every downstream runtime/physics/VR3/P3-R1/replacement-long authority remain false.
11. Canonical `src` and `tests` identity must remain byte-identical to the Selection Planning 1 baseline, excluding generated `bin/obj` working-copy output.
