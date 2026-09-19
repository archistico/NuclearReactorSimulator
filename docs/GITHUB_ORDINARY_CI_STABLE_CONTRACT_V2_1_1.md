# GitHub Ordinary CI Stable Contract V2.1.1 - structural validator hotfix

Status: `IMPLEMENTED-NOT-HOSTED-VALIDATED`.

V2.1.1 preserves the V2.1 hosted behavior unchanged: one invocation of `eng\ci-ordinary.cmd`, complete hosted log capture, exact exit-code propagation, failure-only summarization, and always-published diagnostic artifacts.

The V2.1 local static audit false-RED because its validator searched for a workflow command using doubled backslashes inside a PowerShell single-quoted literal. In PowerShell single-quoted strings, backslash is not an escape character; the validator therefore searched for two literal backslashes while the YAML correctly contained one.

V2.1.1 removes full-line `.Contains()` checks for workflow behavior. The workflow is validated using three stable ASCII markers, slash normalization, multiline regular expressions, and an exact cardinality check requiring the hosted `eng/ci-ordinary.cmd` entry point to appear once. The workflow behavior itself is unchanged apart from marker comments.

No production source, test semantics, thresholds, R3 state, repair ownership, or repair authority changes are authorized. Hosted `ordinary-ci` GREEN remains the required hold before R3 Energy-Transport Ownership Repair Planning 1.
