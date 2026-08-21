# ADR-0177 — Project mission/performance as read-only aggregation of validated owners

- Status: Accepted
- Date: 2026-08-21

## Decision

M10.9.7 mission/performance presentation is a pure Application read model over validated M10.9.6 challenge, demand and scoring owners plus existing assistance/control-authority presentation state. It may copy deterministic recorder protection events for presentation but owns no challenge transition, score formula, plant command, controller, protection or wall-clock authority.

External grid demand, requested generator load and actual electrical output remain separate fields. Unavailable external demand does not erase independently available request/output evidence.

M10.9.7.1 does not modify the fixed Operator Computer F1–F8 navigation contract. Workstation placement is a separate explicit M10.9.7.2 decision.

## Consequences

The future UI can render one coherent immutable snapshot without reimplementing scoring or plant semantics. Replay/checkpoint and live presentation can share the same projection shape, while ownership remains auditable and deterministic.
