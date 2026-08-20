# I.4 validation checklist

- [ ] `APPLY_UPDATE.cmd`
- [ ] `dotnet build`
- [ ] `dotnet test`
- [ ] `scripts\run-phase-i-known-limitations-legacy-retirement-review-audit.cmd`
- [ ] `phase-i-known-limitations-review-passes=True`
- [ ] `phase-i-legacy-retirement-review-passes=True`
- [ ] `i4-audit-passes=True`
- [ ] `i5-closure-gate-unblocked=True`
- [ ] No production policy, exact-version identity, numerical mathematics, physical coefficient or 10 ms timestep change.
- [ ] Candidate ZIP contains no `tests/.../Gameplay/Evidence`, `artifacts`, `bin` or `obj`.
