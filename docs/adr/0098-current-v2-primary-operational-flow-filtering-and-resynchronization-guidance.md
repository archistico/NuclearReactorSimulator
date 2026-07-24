# ADR 0098 — Current-v2 primary operational-flow filtering and re-synchronization guidance

## Status
Candidate with M10.9.4.1-C.2 Hotfix 1.

## Context
The current-v2 sustained seed uses deliberately low primary hydraulic resistances and a 10 ms explicit network step. The user observed large step-to-step changes in raw MCP, channel, return, drum-inlet and liquid-recirculation flows. The same user also observed that, after opening the generator breaker, waiting several minutes did not necessarily restore synchronization permissive.

## Decision
1. Do not retune the validated current-v2 hydraulic resistances merely to make the UI visually quieter.
2. Add deterministic 0.5 s instrumentation lag to dedicated operator-facing current-v2 primary flow channels. Controller-owned canonical channels remain unchanged.
3. Keep raw solver diagnostics available as MODEL data and document the underlying chatter as unresolved numerical-hardening debt.
4. When breaker is open, phase is outside tolerance and frequency slip is essentially zero, explicitly tell the operator that waiting alone cannot change relative phase. Use SPEED RAISE/LOWER to create phase slip, then return near synchronous speed and close only when Δf, Δphase and ΔV are all within limits.
5. Do not add hidden auto-synchronization or silently change breaker-close permissives.

## Consequences
The HMI becomes readable and educational without claiming a physical solver fix. Legacy/v1 profiles remain unchanged. The later numerical-stiffness gate must still decide whether raw primary hydraulic chatter requires substepping, semi-implicit treatment, hydraulic inertia, or another solver-level correction.
