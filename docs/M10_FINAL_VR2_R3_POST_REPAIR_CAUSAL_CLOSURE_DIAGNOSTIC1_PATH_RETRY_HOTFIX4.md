# M10 Final VR2 R3 Post-Repair Causal-Closure Diagnostic 1 - Path/Retry Hotfix 4

## Purpose

The engineering adjudication already had valid runtime evidence, but successive retries were allowed to fail while copying or persisting convenience artifacts through Windows PowerShell path providers. The observed failures were I/O plumbing failures, not causal-closure failures.

Hotfix 4 removes artifact persistence from the authority path.

## Contract

The adjudicator:

1. reads the existing Diagnostic 3 REV1 CSV in place;
2. computes the four frozen causal-acceptance quantities in memory;
3. prints the full adjudication summary and classification to stdout;
4. decides PASS/FAIL solely from those quantities;
5. only then attempts a best-effort write to the intentionally short `artifacts/r3d1.txt` using `System.IO.File.WriteAllLines`.

A failure to persist `artifacts/r3d1.txt` emits a warning and cannot turn a causal PASS into a gate failure. This does not relax any engineering threshold and does not change production, seed, physics, test source, C4 payload or R3 authority.

The retry runner still regenerates only the single existing Diagnostic 3 REV1 test with `--no-build` if runtime evidence is absent.

## Pre-execution self-check

The existing validator now also verifies the Hotfix 4 adjudicator contract before any adjudication runs: exactly one ASCII marker, no PowerShell path-provider writers (`Set-Content`, `Copy-Item`, `Out-File`, `Export-Csv`), exactly one optional `System.IO.File.WriteAllLines`, and the fixed short destination `artifacts/r3d1.txt`.
