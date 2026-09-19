# GitHub Ordinary CI Stable Contract V2.1 - hosted failure capture

Status: `IMPLEMENTED-NOT-HOSTED-VALIDATED`.

V2.1 does not change production code, test semantics, thresholds, filters, retries, or the ordinary CI pass/fail result. It preserves the V2 deterministic ordinary command and adds diagnostics around the single hosted invocation.

The hosted workflow writes the complete output of `eng\ci-ordinary.cmd` to `artifacts/ci/ordinary-ci-hosted.log`, replays that same output to the job log, propagates the exact exit code, and never invokes a second `dotnet test`. On failure, an ASCII-safe summarizer extracts the first xUnit failure block to `ordinary-first-failure.txt` and the GitHub step summary. The complete diagnostics directory is uploaded with `actions/upload-artifact@v7`.

This change exists because local V2 is GREEN while hosted ordinary CI consistently reports one historical `NuclearReactorSimulator.Application.Tests` failure, and the supplied hosted excerpts omit the failing test line/assertion. The observed counts show the failure predates Diagnostic 3 REV1: before new explicit diagnostics, 1525 total / 1370 passed / 154 skipped / 1 failed; after them, 1529 total / 1370 passed / 158 skipped / 1 failed.

Authority remains unchanged: R3 RED; repair owner unselected; production repair planning blocked pending hosted ordinary CI GREEN.
