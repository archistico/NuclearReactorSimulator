# M10 validator inputs

This directory contains 174 exact-text specifications, evidence reviews, milestone
records and reference inputs. They were moved from `docs/` without changing their
bytes. They are validator fixtures; the readable project account is the
[validation summary](../../../docs/validation/README.md) and
[engineering decisions](../../../docs/validation/MODEL_DECISIONS.md).

- [Manifest](manifest.json): original and current paths, SHA-256 digests, navigation
  snapshots and the operational path/digest migration.
- `navigation/`: snapshots of the former documentation interfaces required by
  historical validators. Old status phrases and markers describe their original
  context, not current work.
- `milestones/` and `research/`: original milestone sheets and independent-reference
  records, summarized in the readable validation guide.
- [Original migration inputs](../../../docs/history/archives/m10-contract-migration-2026-11-11.zip):
  exact pre-migration documents, affected tooling and navigation.

Operational validators and contracts reference the relocated paths. Source/test
snapshots and returned evidence remain unchanged. A digest is updated only when
its referenced operational file changes; a frozen copy with the same former digest
keeps its own original identity.

The specifications retain original relative links and repository coordinates as
part of their exact bytes. These are archival references, not the navigation scheme
of this directory. Use the manifest to translate an original `docs/M10…` path.

The [current project checkpoint](../../../docs/PROJECT.md) remains authoritative for
execution status and continuation. Historical validators can retain earlier
candidate-specific failures; relocation does not promote or requalify those gates.
