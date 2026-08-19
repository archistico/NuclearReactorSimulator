# M10.9.4.1-H.22 Validation Checklist

## Baseline and scope

- [x] candidate is built directly on user-validated **H.21 Hotfix 1**;
- [x] H.21 documentation records the real 2026-08-18 build/test/focused-gate pass;
- [x] frozen H.21 summary/telemetry/metrics fingerprints match the user-validated artifacts;
- [x] standard current-v2 remains `ExplicitCommittedState` at 10 ms;
- [x] H.22 mode is separately opt-in;
- [x] P060/F040, H.9, 2% / 5 K hysteresis and the four-node target set are unchanged;
- [x] H.20 supervisor eligibility/rollback contract is unchanged.

## Ordinary gates

```bat
APPLY_UPDATE.cmd
dotnet build
dotnet test
```

- [x] build passes with warnings-as-errors;
- [x] complete ordinary test suite passes;
- [x] H.21 frozen-evidence regression passes;
- [x] H.22 coupling definition retains exact frozen trigger/corrector headline controls;
- [x] H.22 commit-seam unit tests pass;
- [x] standard desktop/current-v2 factory remains explicit;
- [x] mutually exclusive numerical-mode construction remains enforced.

## Cumulative focused prerequisites

```bat
scripts\run-four-node-corrected-commit-seam-audit.cmd
```

This command must first execute the complete H.21 focused gate. Because H.21 itself chains H.19 and H.20:

- [x] H.19 long-horizon/cross-profile qualification remains green;
- [x] H.20 fail-closed authority/rollback contract remains green;
- [x] H.21 orchestrator sidecar integration remains green.

## H.22 corrected-commit gate

The H.22 run consists of two deterministic opt-in engines for 2,000 public intervals.

- [x] both engines remain trip-free throughout the control window;
- [x] H.22 mode is reported on every interval;
- [x] commit arm is reported enabled on every H.22 interval;
- [x] presentation equality H.22 vs H.22 repeat = 2,000/2,000;
- [x] actual P060/F040 trigger count > 0;
- [x] corrected commits > 0;
- [x] H.20 eligible count = H.22 commit-authorized count = corrected-commit count;
- [x] every untriggered interval remains explicit with H.20/H.22 `NotTriggered` semantics;
- [x] fallback commit violations = 0;
- [x] unsafe commits = 0;
- [x] every committed correction converges;
- [x] no committed correction exhausts line search;
- [x] every committed pressure residual <= 1e-5;
- [x] every committed flow residual <= 1e-2 kg/s;
- [x] no corrected commit occurs on an untargeted branch-disagreement interval;
- [x] telemetry fingerprint repeat = true;
- [x] seed/preconditioning commit status is explicitly reported rather than hidden.

Do **not** require the H.22 trigger count to equal H.21's 15. Corrected commits can legitimately alter subsequent trajectory and trigger timing.

## Per-step network accounting

- [x] maximum balance mass-rate residual <= 1e-8 kg/s;
- [x] maximum mass closure residual <= 1e-6 kg;
- [x] maximum balance power residual <= 1e-3 W;
- [x] maximum energy closure residual <= 1e-2 J;
- [x] audit follows corrected applied balances/pump work on committed intervals and explicit balances/pump work on fallback intervals.

## Expected artifacts

```text
artifacts\h22-four-node-corrected-commit-seam\
  00-progress.txt
  01-four-node-corrected-commit-seam.summary.txt
  02-step-commit-telemetry.csv
  03-four-node-corrected-commit-seam-metrics.csv
```

- [x] `four-node-corrected-commit-seam-passes=True`;
- [x] `h22-audit-passes=True`.

## Validation record — 2026-08-18

**VALIDATED.** User-reported build, complete ordinary suite and H.22 focused gate all passed.

```text
intervals=2000
P060-F040-triggered=443
H20-candidate-eligible=443
H22-commit-authorized=443
corrected-candidates-committed=443
H20-rollbacks=0
fallback-commit-violations=0
unsafe-corrected-commits=0
untargeted-branch-disagreements=0
repeat-presentation-equivalent=2000/2000
deterministic-repeat=True
telemetry-fingerprint=3366BCFFF62EBCC8C097EDC36DAF543D80BFBF05936AF6DAFE08EA34A7DBB178
four-node-corrected-candidate-commit-seam-passes=True
h22-audit-passes=True
```

Maximum closure/accounting residuals remained below every checklist bound. Standard current-v2 remained `ExplicitCommittedState`.

## Promotion rule

H.22 is now the authoritative validated baseline. Replay/protection, committed long-horizon/cross-profile and off-design activation gates remain future work; H.23 addresses replay/checkpoint/protection without changing H.22 numerical runtime code.
