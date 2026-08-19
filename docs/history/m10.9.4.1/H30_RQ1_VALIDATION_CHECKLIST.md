# H.30 Requalification 1 — Validation checklist

Promotion requires all of the following:

1. `dotnet build` passes with repository warnings-as-errors policy.
2. `dotnet test` passes completely.
3. `scripts\run-h30-rq1-production-policy-rereview-audit.cmd` passes.
4. The focused summary reports:
   - `h30-rq1-production-policy-decision=ACTIVATE`
   - `h30-rq1-evidence-chain-passes=True`
   - `h30-rq1-audit-passes=True`
   - `production-corrected-default-activated=True`
   - `i3-reference-rerun-unblocked=True`
5. Exact v3 is the live authoritative default in the candidate.
6. Exact v2 remains the fail-closed kill/rollback/reference path.
7. No H.9/H.20/H.22/P060-F040/hysteresis/physical-coefficient/timestep retuning is present.
8. The documentation cleanup does not remove frozen evidence or exact-version compatibility records.

Until all gates are explicitly reported green, I.2 remains the authoritative validated baseline and H.30's prior `OPT-IN ONLY` decision remains authoritative.
