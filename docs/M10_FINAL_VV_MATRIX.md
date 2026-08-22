# M10 Final V&V Matrix

Status: **FROZEN-PRE-LONG**.

The machine-readable authority is `eng/m10-final-vv-matrix.json`. It contains 27 phenomenon/model rows and separates verification, model assessment, integral qualification and user/HMI acceptance. M10.9.8.5 manual acceptance is recorded as accepted; `LONG-SOAK-01` intentionally remains pending.

The final cumulative gate may not widen frozen I.3 budgets or reinterpret historical exact-version identities. Historical superseded long audits remain provenance unless explicitly selected by the current gate. Passing the cumulative gate does not close M10.

The curated cumulative gate has passed on the validated Hotfix 1 baseline (`m10-final-cumulative-validation-passes=True`). The only remaining blocking execution gate is now:

```bat
scripts\run-m10-final-long-validation.cmd
```

Passing the long gate makes M10 closure **eligible**; M10 is not declared CLOSED until the long evidence is promoted into the final closure record and this matrix is updated from `FROZEN-PRE-LONG`.
