# ADR-0205 - Keep ordinary frozen evidence compact and archive large immutable payloads

## Status
Accepted.

## Context
`eng/frozen-evidence/ordinary` was introduced as a compact immutable prerequisite store for ordinary/current validation. Recent VR2 performance gates added large per-call CSV trees and expanded the directory to roughly 171 MiB, contrary to the earlier compact-store boundary used for I.3 large payloads.

The raw evidence remains valuable for audit and historical reruns, but keeping every large payload expanded is unnecessary for normal development and validation.

## Decision
Direct payloads in `eng/frozen-evidence/ordinary` must remain at or below 1 MiB unless a separately adjudicated exception exists. Larger immutable payloads are retained byte-for-byte in compressed gate-scoped archive packs under `eng/frozen-evidence/archive` and authenticated by canonical SHA-256, uncompressed byte count and logical path in `eng/frozen-evidence/large-payload-manifest.csv`.

Historical validators are not reinterpreted. If a historical validator requires a raw payload that has been compacted, the payload must first be restored from the authenticated archive. Restore and re-compaction tooling must verify hashes and fail closed.

## Consequences
- The ordinary store remains small and fast to copy, inspect and package.
- Full raw evidence remains recoverable without changing canonical bytes.
- Existing compact summaries, aggregates and runtime contexts stay at their original logical paths.
- Historical gates that require large raw files need an explicit restore step.
- No physics, numerical method, runtime configuration, threshold, exact-v9 identity or authority boundary changes.
