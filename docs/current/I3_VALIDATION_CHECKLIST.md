# I.3 validation checklist

Run:

```bat
dotnet build
dotnet test
scripts\run-phase-i-reference-trajectory-conservation-inventory-baseline-audit.cmd
```

Required focused-gate flags:

```text
phase-i-reference-trajectory-baseline-passes=True
phase-i-generation-continuity-baseline-passes=True
phase-i-conservation-inventory-baseline-passes=True
phase-i-production-telemetry-baseline-passes=True
phase-i-reference-determinism-passes=True
i3-audit-passes=True
phase-i-reference-tolerance-baseline-established=True
```


Packaging/evidence precondition:

- ordinary `dotnet test` must pass with no `tests/.../Gameplay/Evidence` directory present;
- frozen ordinary prerequisites resolve from `eng/frozen-evidence/ordinary`;
- omitted large trace identities resolve from `eng/frozen-evidence/large-payload-manifest.csv`;
- candidate ZIPs must contain zero `tests/.../Gameplay/Evidence`, `bin`, `obj` or runtime `artifacts` entries.

The gate must produce 19 tolerance-budget entries and seven final-window slope observations. Do not promote I.3 if the floor is weakened, v2 is substituted for the production selector, or any runtime tuning is introduced to fit the candidate budgets.
