# M11 Release Evidence Matrix — Plan

## Status

**PLANNING — build the executable matrix during M11.1.**

The M10 27-row V&V matrix describes phenomenon qualification. M11 needs a complementary matrix for **release readiness**. The two matrices serve different purposes and must not be merged into one ambiguous “all green” table.

## Planned release-evidence rows

| ID | Area | Evidence class | Owning milestone | Release blocker if red? |
| --- | --- | --- | --- | --- |
| REL-01 | product/version identity | configuration assurance | M11.1 | yes |
| REL-02 | supported OS/architecture | support assurance | M11.1/M11.4 | yes |
| REL-03 | runtime/self-contained policy | support/package assurance | M11.1/M11.4 | yes |
| REL-04 | dependency/version inventory | COTS/dependency assurance | M11.1/M11.4 | yes |
| REL-05 | architecture invariants | Digital I&C verification | M11.1 | yes |
| REL-06 | human–automation allocation | Digital I&C verification | M11.1 | yes |
| REL-07 | release non-claims/limitations | documentation assurance | M11.1/M11.5 | yes |
| REL-08 | supported scenario/profile identities | compatibility | M11.2 | yes |
| REL-09 | session archive schema support | compatibility | M11.2 | yes |
| REL-10 | checkpoint support | compatibility | M11.2 | yes |
| REL-11 | snapshot fingerprint v1 support | deterministic compatibility | M11.2 | yes |
| REL-12 | action/history/authority semantic reconstruction | replay compatibility | M11.2 | yes |
| REL-13 | future/unsupported version fail-closed | compatibility/error contract | M11.2 | yes |
| REL-14 | runtime batch throughput | performance characterization | M11.3 | yes if above frozen ceiling |
| REL-15 | UI responsiveness | human-system performance | M11.3 | yes if above frozen ceiling |
| REL-16 | recorder long-session growth | memory/evidence assurance | M11.3 | yes if unbounded/unsafe |
| REL-17 | save/load cost and archive size | persistence performance | M11.3 | conditional/blocker by frozen budget |
| REL-18 | no silent logical-time loss | semantic timing verification | M11.3 | yes |
| REL-19 | recorder evidence-failure policy | evidence integrity | M11.3 | yes |
| REL-20 | clean publish | packaging verification | M11.4 | yes |
| REL-21 | packaged dependency/assets completeness | packaging verification | M11.4 | yes |
| REL-22 | packaged first startup | system acceptance | M11.4/M11.6 | yes |
| REL-23 | packaged save/reload/replay | system acceptance | M11.4/M11.6 | yes |
| REL-24 | Digital I&C hazard closure | hazard assurance | M11.5/M11.6 | yes |
| REL-25 | HMI classic failure-mode review | human-system acceptance | M11.5/M11.6 | yes |
| REL-26 | manual operator task set | user acceptance | M11.6 | yes |
| REL-27 | README/manual/package/version agreement | release documentation | M11.5/M11.6 | yes |
| REL-28 | zero-test/discovery fail-closed sentinel | validation-harness assurance | M11.6 | yes |
| REL-29 | M10 phenomenon V&V provenance intact | provenance | M11.6 | yes |
| REL-30 | final release closure | integrated release qualification | M11.6 | yes |

## Required fields in the machine-readable matrix

Each row should contain:

- `id`;
- `title`;
- `evidenceClass`;
- `ownerMilestone`;
- `sourceContract`;
- `executableRoute`;
- `acceptanceCriterion`;
- `supportedTargetOrScope`;
- `artifact`;
- `knownLimitation`;
- `status`;
- `blocking`.

## Status vocabulary

Use only explicit states such as:

- `PLANNED`;
- `FROZEN-PRE-EXECUTION`;
- `PASS`;
- `FAIL`;
- `NOT-APPLICABLE-WITH-JUSTIFICATION`;
- `DEFERRED-NON-BLOCKING`.

Do not use ambiguous `OK` where the evidence type or scope is unclear.

## Relationship to M10 V&V matrix

M11 does not reclassify M10 physical verification as experimental validation. `REL-29` verifies that release work has not invalidated the frozen M10 phenomenon provenance. If M11 unexpectedly changes production semantics, the affected M10 rows must be identified and requalified rather than merely marking `REL-29` green.
