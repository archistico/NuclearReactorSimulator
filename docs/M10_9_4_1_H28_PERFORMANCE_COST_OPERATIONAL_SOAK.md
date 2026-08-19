# M10.9.4.1-H.28 — Performance, Cost & Long-Running Operational Soak

## Status

**REQUALIFICATION CANDIDATE**, built directly on user-validated **M10.9.4.1-H.28.1-D Preflight Hotfix 1**. The original H.28 ceilings and workloads are unchanged.

H.27 remains the validated bounded off-design prerequisite. H.28.1-D is the validated performance-optimization prerequisite: it preserved 35 hydraulic evaluations, 32 probes, Jacobian dimension 32 and the exact H.28 deterministic fingerprint while materially reducing CPU cost.

## Question

Is the already-qualified corrected-commit path operationally affordable and stable enough to remain a credible H.29 activation candidate, without changing the 10 ms fixed timestep or weakening any numerical/ownership contract?

H.28 is not another numerical-method milestone. It changes no H.9/H.20/H.22 algorithm and performs no retuning.

## Why wall-clock must be relative

The production fixed step is **10 ms of simulated deterministic time**. It is not a promise that an xUnit run must finish one engine step in less than 10 ms on every machine.

Therefore H.28 records absolute wall-clock evidence but qualifies cost primarily through a paired explicit-vs-corrected comparison in the same process and on the same machine.

The hard regression ceilings are deliberately broad:

- corrected median step cost / explicit median step cost: **<= 8.0**;
- corrected p95 step cost / explicit p95 step cost: **<= 12.0**;
- corrected median allocated bytes / explicit median allocated bytes: **<= 16.0**.

A stronger advisory classification is also produced:

- `activation-favorable` when median wall ratio <= 4.0 and median allocation ratio <= 8.0;
- otherwise `bounded-but-costly` when the hard H.28 ceilings still pass.

A green `bounded-but-costly` result does not imply default activation. H.29/H.30 must carry that classification forward and may legitimately end at `OPT-IN ONLY`.

## Focused evidence

### 1. Paired benchmark

After 64 warmup steps per mode, H.28 measures 256 steps of the same bounded 5→0→5 MWe manoeuvring workload in:

- standard `ExplicitCommittedState`;
- opt-in `FourNodeBranchContinuityCorrectedCommitOptIn`.

Per-step evidence records:

- wall microseconds around the engine `Step` call;
- current-thread managed allocation delta;
- trigger, commit and rollback flags;
- H.9 shadow iteration count;
- unsafe-commit status.

The report also separates average/max wall cost and average allocation for the corrected steps that actually trigger P060/F040/H.9.

The corrected benchmark must actually observe at least one P060/F040 trigger and one corrected commit.

### 2. Bounded operational soak

A fresh corrected-commit engine runs **1,536 steps** (15.36 simulated seconds) with two safe 5→0→5 MWe request manoeuvres. H.24 already owns rare 30,000-step duration evidence, so H.28 does not duplicate that 4h31m55s gate.

Every soak step keeps the H.22/H.27 safety contract:

- no fallback commit;
- no unsafe corrected commit;
- no untargeted branch disagreement;
- no unexpected trip;
- network closure and ownership within H.22 limits.

H.28 also records:

- average/max step wall time;
- average/max current-thread allocated bytes;
- managed-heap start/end snapshot;
- Gen0/Gen1/Gen2 collection counts;
- average/max triggered H.9 iteration count;
- trigger/commit/rollback counts.

Managed-heap delta and GC counts are diagnostic only because they are runner/process dependent.

### 3. Publication/telemetry pressure

The soak keeps full runtime publication active but writes only one artifact sample every 32 steps. Artifact bytes and sample stride are reported so the qualification does not accidentally turn into another heavy per-step CSV benchmark.

### 4. Determinism control

Two fresh 128-step corrected runs, including a bounded 5→0→5 request change, must produce the same presentation/authority/commit fingerprint. Timing and allocation values are deliberately excluded from the deterministic fingerprint.

## Frozen prerequisites

H.28 freezes both the user-validated H.27 evidence and the user-validated H.28.1-D summary/steps/cost-centers/metrics with canonical SHA-256 fingerprints. H.24 is **not** rerun by this performance gate.

## Non-goals

H.28 does not:

- activate corrected ownership by default;
- change the production fixed timestep;
- change P060/F040;
- change H.9 tolerances or algorithm;
- change the 2% / 5 K bounded hysteresis;
- change the four-node target set;
- change H.20 authority or H.22 commit ownership;
- change physical coefficients;
- broaden the H.27 envelope;
- optimize code merely to make the gate pass.

## Decision after H.28

If the hard cost ceilings and soak safety/determinism gates pass, H.28 qualifies the current implementation for the **separately reviewed H.29 production activation candidate**.

The reported cost class remains evidence for H.30:

- `activation-favorable` supports considering `ACTIVATE`;
- `bounded-but-costly` is a material argument for `OPT-IN ONLY` unless later optimization is separately implemented and requalified.
