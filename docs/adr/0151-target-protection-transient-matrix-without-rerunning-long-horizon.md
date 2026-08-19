# ADR 0151 — Target protection/transient qualification without rerunning the long-horizon gate

## Status

Accepted for M10.9.4.1-H.25 candidate.

## Context

H.24 validated 30,008 committed runtime steps but its focused gate required 4h31m55s. H.25 changes no numerical runtime and needs to qualify a different dimension: representative protection and operational-transient interaction.

Automatically chaining H.24 into every later Phase H milestone would materially slow development without adding proportional evidence when the committed numerical runtime is unchanged.

## Decision

H.25 will:

1. freeze compact validated H.24 evidence and the canonical fingerprint of its full telemetry;
2. preserve the current-v2 protection catalogue through an ordinary contract test;
3. run a short committed matrix of representative protection/action/supervision cases;
4. retain H.20/H.22 fail-closed and closure/ownership checks on every matrix step;
5. not rerun H.24 automatically.

H.24 is rerun only when committed numerical runtime behavior changes or a later closure gate explicitly requires it.

## Consequences

Development feedback remains practical while the expensive long-horizon evidence remains authoritative. H.25 can qualify protection/transient integration without pretending to replace off-design, rollback-stress or final activation evidence.
