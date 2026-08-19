# H.28.1-G static review

Static review performed before local user validation.

Delta versus H.28.1-F working source: 20 paths total, 6 C# files, 3 files under `src/`.

Checks:

- reduced diagnostic is `internal`; no public API expansion;
- standard simplified provider uses the reduced path; non-standard providers retain the complete public diagnostic path;
- reduced diagnostic uses the same branch functions and production priority as the full diagnostic;
- exact-equivalence tests cover representative coarse saturated, subcooled liquid, coarse superheated, boundary-aware saturated, boundary-aware superheated and no-root states;
- H.9 deterministic result records, H.21 sidecar result record and H.22 telemetry record are byte-identical to H.28.1-F;
- frozen H.28.1-F failed evidence canonical SHA-256 values are checked by the focused gate;
- braces/parentheses/brackets are balanced in every modified C# file;
- private constants/readonly fields introduced in the focused test have no obvious unused declaration;
- no wall-clock/timer API token is present under `NuclearReactorSimulator.Simulation`;
- `.cmd` files use CRLF;
- no `bin`, `obj` or `artifacts` directories are packaged.

The assistant environment does not provide the project .NET SDK, so this review is static and does not replace local compilation/test validation.
