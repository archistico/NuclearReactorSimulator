# RP1C Engineering Repair Selection 1 — Returned Evidence Adjudication

Adjudicate only the four returned decision artifacts. Do not reinterpret FDPC1/FDPC2, rerun performance evidence or modify production.

A returned selection PASS requires:

```text
selection-result=SELECT-C4
selection-mode=AUTHORED-ENGINEERING-DECISION
automatic-selection=False
select-none-preserved=True
selected-candidate=C4-ALLOCATION-NEUTRAL-VAPOR-SEAM-COMPLETE-SURROGATE
selected-runtime-profile=AMBIENT-UNSET
d3-selection-ready=False
new-measurement-performed=False
candidate-mutation-performed=False
production-repair-authorized=False
```

If those conditions and the frozen prerequisite hashes are intact, the returned selection may authorize planning only for `R1-IMPLEMENTATION-PLANNING-ONLY`. It must not authorize implementation or activation of the repair itself.
