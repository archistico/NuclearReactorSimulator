# M10.9.6.5 manual closure checklist

This is an artifact/semantic review, not an HMI gate. M10.9.7 owns challenge presentation.

After build, ordinary tests and `scripts\run-m1096-replay-checkpoint-closure-audit.cmd` pass, inspect `artifacts\m1096-closure` and confirm:

- `01-m1096-replay-checkpoint-determinism-closure.summary.txt` reports both replay determinism and checkpoint continuation as true;
- `02-m1096-closure-gate-matrix.csv` contains PASS for lifecycle, demand, scoring, packs, replay reconstruction, terminal lifecycle replay-step alignment and checkpoint continuation;
- `03-m1096-pack-identity-policy-matrix.csv` contains the six expected exact pack identities and exact scoring policies;
- no artifact describes external demand as a generator command or merges demand/requested load/actual output;
- generator-trip/load-rejection still treats the generator trip as required evidence, while normal demand-following retains its authored unexpected-trip failure condition;
- no artifact introduces a new demand-schedule action penalty or otherwise changes the exact M10.9.6.3 scoring policy semantics during closure;
- no new challenge UI is expected in this milestone; visual/objective/demand/score acceptance is deferred to M10.9.7.

If all checks are green, promote M10.9.6 to VALIDATED/CLOSED and begin M10.9.7.1.
