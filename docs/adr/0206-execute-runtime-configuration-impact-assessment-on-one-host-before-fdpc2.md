# ADR 0206 - Execute runtime configuration impact assessment on one host before FDPC2

## Status
Accepted for the A2 evidence candidate.

## Context
Runtime Factor Isolation 1 established a material TieredCompilation effect on exact-v9 gross timing and Dynamic PGO Comparator 1 established a material Dynamic PGO effect on the central timing distribution. Neither result justifies a production runtime prescription. The project is also validated on multiple physical PCs with materially different performance, so wall-clock comparisons can be confounded by host changes.

## Decision
Before Full-Domain Performance Confirmation 2 or RP1C selection, execute `RP1C-C4-RUNTIME-CONFIGURATION-IMPACT-ASSESSMENT1` on one physical host. Compare `AMBIENT-UNSET` with `EXPLICIT-REFERENCE-ALL-ON` across exact-v9, the ordinary Release suite, replay/determinism and the validated M10.9.7.2 non-VR2 performance owner.

The host is fingerprinted at gate start/end, the power scheme must remain stable, and evidence from different hosts may not be merged. Structured xUnit XML is the authoritative test-count source so console localization cannot affect the gate.

## Consequences
The explicit all-ON profile remains a qualification reference rather than a deployment recommendation. Engineering-negative results are retained as evidence when the harness remains intact. FDPC2, RP1C selection and production runtime authority remain blocked until the complete A2 evidence tree is returned and adjudicated.

## Execution-harness clarification (second-pass review)

The A2 implementation uses native .NET 10 Microsoft Testing Platform argument boundaries: SDK/MTP options such as `--results-directory` and `--minimum-expected-tests 1` are passed before `--`, while xUnit report/filter options are forwarded after `--`. Structured xUnit XML is interpreted defensively: `executed = passed + failed + skipped`; reporter `total` is accepted only when it equals either `executed` or `executed + not-run`, because the returned xUnit v3 3.2.2 schema-3 evidence demonstrated the latter behavior despite the published format wording. Execution-cardinality decisions use `executed`, not reporter `total`. This prevents localized console output, zero-test discovery and explicit-test accounting from creating false evidence classifications.


### Hotfix 4 clarification — canonical source identity vs generated output

A2 source/test tree identity excludes generated `bin/` and `obj/` subtrees. Local working copies are allowed to contain those outputs from prior builds; they are not part of canonical source identity. Candidate packaging still excludes generated output. This distinction prevents normal build activity from creating a false source-drift RED without relaxing any source-file hash or engineering contract.


A previous failed A2 attempt may leave partial files under the owned A2 artifact root. This is permitted in the working copy because the A2 orchestrator deletes and recreates that entire owned root before new evidence collection, preventing stale/new evidence mixing.
