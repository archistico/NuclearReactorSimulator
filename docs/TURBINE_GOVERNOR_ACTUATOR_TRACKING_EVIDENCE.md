> **D.3 outcome:** the breaker-open audit reached about 3301 rpm with controller output 0%, valve 0%, effective stage flow 0 kg/s and shaft power 0 MW, then showed exactly zero speed change. The missing element was passive rotor drag. D.3.1 supplied the passive-loss closure; D.3.2 Hotfix 2 is now the active cumulative candidate; this document remains the evidence record.

# M10.9.4.1-D.3 — Governor Effective-Setpoint & Actuator-Tracking Evidence

## Purpose

D.3 determines whether the current-v2 governor requires a new actuator-position tracking anti-windup law, or whether the existing conditional-integration anti-windup is already adequate for the validated operating envelope.

This checkpoint is evidence-only. It changes no controller, actuator, turbine, generator or hydraulic law.

## Why the D.2 runtime perturbation needed correction

The first D.2 runtime journey used `SPEED RAISE/LOWER` from the sustained 5 MWe seed, whose generator breaker is already closed.

That command changes the requested speed-controller setpoint, but while the generator is paralleled the current-v2 droop adapter intentionally replaces the effective speed-controller setpoint with:

```text
synchronous speed + full-load droop rise × requested load fraction
```

Therefore a direct speed-reference command while paralleled does not constitute a valid governor-setpoint perturbation. The earlier test could pass because the plant continued evolving naturally during the observation interval.

D.3 corrects the evidence method:

- breaker open: use `SPEED RAISE/LOWER`, where the requested speed setpoint remains authoritative;
- breaker closed: use `LOAD RAISE/LOWER`, where the droop-derived setpoint is authoritative.

The corrected D.2 journey is also moved to the breaker-open sustained synchronization seed.

## Existing current-v2 control contract

The two sustained current-v2 profiles deliberately retain distinct controller tuning:

- desktop sustained generation: P=`1.0`, I=`0.02 s⁻¹`, D=`0.2 s`;
- breaker-open synchronization: P=`0.5`, I=`0.02 s⁻¹`, D=`0`;
- both: output `0–100%`, control-valve travel `0.5 fraction/s`, full-load droop rise `150 rpm` and conditional-integration anti-windup.

D.3 does not retune either profile.

## Evidence journey 1 — breaker-open speed-reference tracking

Starting from `GridSynchronizationSustainedInitialConditionFactory`:

1. run until the breaker-open governor reaches a controllable baseline within ±5 rpm, with a nonzero physical valve and unsaturated output; fail if this does not occur within 90 simulated seconds;
2. capture that baseline;
3. issue `SPEED RAISE` (`+10 rpm`);
4. capture the first committed response after one `0.01 s` solver step, including the immediate controller and initial valve response;
5. continue sampling every `0.1 s` for the remainder of the 10-second interval;
6. issue `SPEED LOWER` (`-10 rpm`);
7. again capture the first committed `0.01 s` response;
8. continue sampling every `0.1 s` for the remainder of the recovery interval.

The audit records:

- effective governor setpoint and measured rotor speed;
- P/I/D terms;
- unsaturated and bounded controller output;
- controller saturation and conditional anti-windup state;
- physical control-valve position;
- command-to-valve tracking gap;
- commanded/effective turbine-stage flow;
- shaft power.

This separates controller-output behavior from finite-rate physical valve motion.

## Evidence journey 2 — breaker-closed load-droop tracking

Starting from `DesktopSustainedGenerationInitialConditionFactory` at 5 MWe:

1. run 5 simulated seconds;
2. issue `LOAD RAISE` (`+5 MWe`);
3. verify the droop-derived effective governor setpoint;
4. sample the response for 10 seconds;
5. issue `LOAD LOWER` (`-5 MWe`);
6. sample the recovery for 10 seconds.

With the current generator nameplate of 1,000 MWe and a full-load droop rise of 150 rpm:

```text
5 MWe / 1,000 MWe × 150 rpm = 0.75 rpm
```

Therefore one accepted `LOAD RAISE` command changes the effective governor setpoint by only `+0.75 rpm`.

This is not automatically a governor defect. It is direct evidence that low-load droop authority is coupled to the unresolved reference-plant scale contract. Any attempt to enlarge the droop response in isolation would pre-empt Phase E.

## Decision rule

After the dedicated audit, classify the result as follows.

### Existing anti-windup is adequate

No tracking-law change is required when:

- controller saturation is brief or absent;
- the integral term does not continue moving in the direction of an unavailable controller output;
- physical valve motion follows the bounded command at the configured travel rate;
- restoring the setpoint/load request produces a prompt controller and valve reversal;
- no material unmet response is caused by command/position divergence.

In this case Phase D closes without tracking anti-windup, and the synchronized low-load authority question moves to Phase E with the reference-scale migration.

### Tracking anti-windup is justified

A separate D.3.x physics checkpoint is justified only if evidence shows:

- sustained controller/physical-valve divergence;
- material integral accumulation while the actuator cannot follow;
- delayed reversal or recovery attributable to that accumulated integral term;
- the issue persists independently of the 1,000 MWe scale mismatch and hydraulic upper-range compression.

Any new tracking law must be versioned or explicitly scoped, preserve legacy behavior, and pass controller-unit, 60-second, 300-second, conservation, protection and replay gates.

## D.3 Hotfix 1 — first-step event capture

The original D.3 candidate began its sampled window `0.1 s` after each speed-reference command. Because the controller derivative term is computed from the error change over the `0.01 s` solver step, the largest directional controller response occurs in the first committed step and could be missed completely by the coarser audit sampling.

Hotfix 1 captures that first step explicitly before resuming the `0.1 s` evidence cadence. The directional assertion is applied to this event sample rather than to a later-window maximum. This preserves the intended control-law check without changing PID gains, actuator travel, production code or simulation physics. Evidence is written before assertions so any future gate failure retains the decisive diagnostics in the test output.

## D.3.1 consequence

The first corrected run proved that no amount of waiting or additional sampling could recover the breaker-open seed: once steam torque reached zero, net torque also reached zero because the model had no passive loss. D.3.1 adds that missing path before the anti-windup decision is revisited.
