# M10 Final — VR2 — R1 Implementation Planning 1 — Pre-Implementation Review

## Status

**STATIC PRE-IMPLEMENTATION REVIEW CONTRACT.**

The planning audit must verify all of the following before implementation is authorized:

1. returned RP1C selection is complete and records `SELECT-C4` as authored, not automatic;
2. `SELECT-NONE` was preserved and considered;
3. C4 + `AMBIENT-UNSET` is the selected pair;
4. the selected C4 test-only implementation remains unchanged;
5. closure-mode values 0 and 1 remain historical/current immutable values;
6. the future mode is explicit opt-in `ReferenceConsistentTabulatedInverseDomain = 2`;
7. the default constructor and exact-v9 call sites are not changed by planning;
8. production IF97 runtime dependency is forbidden;
9. reference data is generated offline in C# and checked in with SHA-256 provenance;
10. the raw payload format is frozen as `NRSVR2C4` schema 1, little-endian IEEE-754 binary64 bits with full-payload SHA-256 validation and load-once semantics;
11. `NuclearReactorSimulator.Simulation.csproj` is explicitly in future R1 scope because the binary payload requires an explicit `EmbeddedResource` declaration with frozen logical name `NuclearReactorSimulator.Simulation.Physics.Fluids.ReferenceData.NRSVR2C4.v1.bin`;
12. mode 2 dispatches before the existing mode-0/mode-1 pipeline and may not fall through into either historical branch;
13. R1 acceptance uses the frozen 1,679-state + 288-hydraulic corpus (1,967 total), with source hashes and unique identities preserved, not a count-only substitute;
14. mode-2 resolve performs no payload I/O/decoding and no resolve-time allocation;
15. payload load/schema/hash validation occurs during the first mode-2 resolver construction, not lazily on first resolve; a static process-wide immutable cache is then reused by all mode-2 resolvers;
16. generator SHA-256 manifest output is evidence/provenance only, is returned canonically as `03-reference-data-provenance.txt`, and is not trusted by production runtime;
17. the three existing production files named by the contract are an exact modification allowlist; no existing production file deletion is permitted;
18. the exact future production/test file allowlist is frozen: one new resolver source, one embedded payload, one C# generator and one R1 implementation test file;
19. offline generation uses invariant culture and canonical ordering and must reproduce the payload twice byte-for-byte in temporary locations;
20. ordinary Release and historical-mode regression are required, with regression explicitly covering modes 0 and 1;
21. R1 cannot activate a new default, runtime override or exact identity;
22. R2 remains planning-only after returned R1 adjudication;
23. VR3, P3-R1 and second replacement-long remain unauthorized;
24. this planning candidate contains no production source or test modifications.
