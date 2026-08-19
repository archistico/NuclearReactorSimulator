# H.30 Requalification 1 — Static review

## Scope

This review covers the production-policy re-review and the documentation consolidation packaged with it. It is not a substitute for local build/test execution.

## Runtime delta

The numerical solver path is not retuned. H.9, H.20, H.22, P060/F040, bounded hysteresis, physical coefficients and the external 10 ms fixed step remain unchanged.

Production-facing changes are limited to policy/composition plumbing:

- the desktop production selector chooses exact v3 by default;
- explicit kill still resolves fail-closed to exact v2;
- fresh desktop startup uses `DesktopIntegratedOperationsProductionProgram`;
- H.30 RQ1 creates a new production scenario identity over exact v3;
- the historical v2 scenario and historical H.29 candidate scenario remain separate replay-compatible identities;
- the current gameplay and operational-envelope scheduled long regressions resolve the authoritative production selector instead of pinning historical v2.

## Evidence/provenance

The re-review freezes the validated I.3 Hotfix 4 and Hotfix 5 artifacts by canonical SHA-256 and derives `ACTIVATE` only when the frozen explicit discontinuity, corrected comparison and corrected 300 s evidence are all intact.

No H.24, H.28 or I.3 long trajectory is rerun by the focused H.30 RQ1 gate.

## Documentation consolidation

The public README and current-status documents were rewritten around the present project state. The detailed `M10_9_4_1_*` chronology was moved from the `docs/` root to `docs/history/m10.9.4.1/` rather than deleted. The previous long-form project status, roadmap, limitations and milestone summary are retained under `docs/history/project/`.

The active documentation surface is now:

- `README.md`;
- `docs/README.md`;
- `docs/PROJECT_STATUS.md`;
- `docs/PROJECT_HANDOFF.md`;
- `docs/ROADMAP.md`;
- `docs/KNOWN_MODEL_LIMITATIONS.md`;
- `docs/current/` for the active candidate;
- `docs/history/` for superseded chronology/provenance.

## Validation still required

Run locally:

```text
dotnet build
dotnet test
scripts\run-h30-rq1-production-policy-rereview-audit.cmd
```

Until those gates are explicitly green, I.2 and the original H.30 `OPT-IN ONLY` decision remain authoritative.
