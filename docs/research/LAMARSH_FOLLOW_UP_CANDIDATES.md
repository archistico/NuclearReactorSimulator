# Lamarsh Follow-Up Candidates

## Status

**RESEARCH BACKLOG — not authorized implementation work.**

The third pre-M11 source review identified several topics with potential educational and engineering value. Only the operating-point equilibrium work has been promoted into the planned roadmap as M12.0. The items below are retained so they can be reviewed deliberately later instead of being lost or silently leaking into current scope.

| Priority | Candidate | Potential value | Earliest sensible owner | Current decision |
| --- | --- | --- | --- | --- |
| 1 | Thermal Margin / Boiling Regime Monitor | Makes local heat-removal margin and boiling-regime state observable; useful prerequisite for later spatial/consequence work. | M12/M14 physical-fidelity review | **Study later.** No direct use of textbook PWR/BWR limit values. |
| 2 | Full-Plant Shutdown Cooling / Residual Heat Removal | Connects decay heat to explicit post-trip heat-removal availability and long-term cooling paths. | M12.5 or a separately scoped M12 slice | **Candidate refinement of already planned decay-heat work.** |
| 3 | Reactivity Feedback Stability Map | Offline perturbation audit of fuel-temperature, coolant-temperature, void, xenon and control sensitivities; can detect sign/configuration regressions. | M12/M14 validation tooling | **Study later.** No coefficient change without owner evidence. |
| 4 | Xenon Restart Margin / Reactor Deadtime Advisor | Turns existing I/Xe dynamics into an operator-facing restart-margin consequence rather than a hard-coded timer. | Post-M11 operations/training feature | **Study later.** Must derive from canonical physics, not scenario flags. |
| 5 | Measurement-State Coherence Monitor | Detects physically inconsistent combinations of individually “valid” measurements without silently using true state. | M13.9 Digital I&C / HMI | **Study later.** Complements stale/delayed/lost signal work. |
| 6 | Shutdown Margin / Protection Diversity Inventory | Makes available negative reactivity and shared protection dependencies explicit. | Documentation/validation first; possible later M5/M12/M14 work | **Inventory before implementation.** No independence claim from duplication alone. |
| 7 | Historical RBMK rod/displacer behavior | Could support a deliberately historical educational configuration. | Dedicated future historical-reactor scope | **Deferred.** Must never silently alter the default reduced RBMK-like reference reactor. |

## Selection rule

A candidate may move from this file into a milestone only when:

1. the educational or diagnostic value is concrete;
2. the physical/state owner is identified;
3. the required fidelity is compatible with the simulator’s reduced-order scope;
4. the acceptance/reference evidence is available or can be defined honestly;
5. exact-version and replay consequences are explicit;
6. the change-impact/revalidation class is assigned before implementation.
