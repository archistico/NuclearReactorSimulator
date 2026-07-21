# ADR 0049 — Primary-circuit mnemonics preserve measurement and topology ownership

## Status

Accepted / validated with M6.4.

## Context

The Primary Circuit workspace must show process connectivity, flows, pressures, steam-drum levels and operator controls without allowing Avalonia to traverse true plant state or silently treat model diagnostics as measured instrumentation.

## Decision

1. Avalonia binds only to `PrimaryCircuitPanelSnapshot` and related Application presentation records.
2. Existing M5.1 semantic sources provide measured loop flow/header ΔP and steam-drum pressure/level with validity and quality semantics preserved.
3. Non-instrumented values may be projected only as explicitly labelled model diagnostics.
4. Mnemonic equipment identity and connectivity derive from canonical M3 definitions; presentation code does not invent pumps, branches, drums or valves.
5. Valve state is shown only for canonical valves hydraulically connected to primary-circuit nodes.
6. MCP operator actions leave Avalonia as typed pump command intents. M5.3 retains pump-command ownership and M3 retains sole hydraulic/inventory integration.
7. Flow-direction labels are presentation semantics based on the sign of the relevant presented flow and cannot become a hidden solver/protection input.

## Consequences

- The primary mnemonic can be topology-rich without introducing a second plant model in the UI.
- Operators can distinguish measured instruments from educational diagnostics.
- Missing equipment/data remains absent or unavailable rather than fabricated.
- Later runtime wiring can consume typed MCP intents without changing the UI architecture.
