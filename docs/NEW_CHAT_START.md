# New Chat Start — Nuclear Reactor Simulator

Continue from this exact checkpoint:

- **Validated continuation baseline:** M10.9.4.1-F.2.
- **Working source:** M10.9.4.1-F.3 Hotfix 1 Conservative Turbine Bypass to Condenser CANDIDATE.
- F.2 atmospheric header relief is validated and unchanged.
- F.3 Hotfix 1 adds one current-v2 internal `header` → condenser `exhaust` bypass at 6.4/6.5 MPa, using actual committed condenser backpressure and the validated F.1 capacity law.
- Internal mass/internal-energy transfer is equal and opposite; external exchange is zero.
- Legacy/current-v1 bypass collections remain empty.
- Phase G remains the sole owner of flow-work/enthalpy migration.

Validate with:

```bat
dotnet build
scripts\run-turbine-bypass-tests.cmd
dotnet test
```

Then execute the cumulative gates in `M10_9_4_1_F3_VALIDATION_CHECKLIST.md` and review the four generated F.3 artifacts. Promote F.3 only after all gates pass.
