# ADR 0100 — Turbine admission authority is measured before retuning

## Status

Proposed / M10.9.4.1-D.2 candidate.

## Context

An earlier external audit, based on an older control-valve bias near 46%, estimated that the 21,400 Pa·s²/kg² turbine-stage resistance dominated the 1,000 Pa·s²/kg² valve paths strongly enough to leave little governor authority. The consolidated continuation base moved the sustained current-v2 control-valve seed to 28%, so the old numerical conclusion could not be carried forward unchanged. D.3.2 Hotfix 1 briefly tested 30% on the loaded desktop, but local evidence rejected that bias-only hypothesis; Hotfix 2 restores both sustained profiles to 28% and rebalances the loaded stop-out pressure seed instead.

Changing stage resistance, valve resistance or the flow law before measuring the new operating point would risk another tuning-driven fix.

## Decision

D.2 is evidence-first and changes no production physics.

The current-v2 resistance budget and valve characteristic are frozen by explicit audit tests. A deterministic local governor-reference perturbation records control-valve position, turbine-inlet pressure, commanded/effective stage flow and shaft power.

The static equal-head resistance map is treated only as an analytical authority indicator. Dynamic plant evidence remains authoritative for the correction decision because intermediate node pressures, steam-source capacity, phase policy, condenser response, generator/grid coupling and controllers evolve together.

No resistance rescaling, effective-area admission model, Stodola/ellipse law or governor retuning is accepted until that evidence is reviewed.

## Consequences

The shared 28% sustained seed remains materially different from the older 46% audit point and retains about 20.9% idealized full-open flow-capacity headroom. The rejected 30% point remains only a comparison datum at about 18.2% headroom. Authority nevertheless compresses rapidly above roughly 60% opening because fixed stage/upstream resistance dominates.

D.2 may therefore close without a physics change if runtime evidence shows adequate authority around the validated operating point. If evidence shows inadequate authority or saturation over the required load envelope, a separate correction checkpoint must choose and validate the smallest physically coherent law change.
