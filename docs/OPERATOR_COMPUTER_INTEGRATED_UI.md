# Operator Computer Integrated UI

**Milestone:** M10.8 — Integrated Operator Computer UI — VALIDATED
**Status:** VALIDATED — retained as the operator-computer baseline beneath M10.9.1+

## Purpose

M10.8 turns the capabilities accumulated in M10.1–M10.7.1 into one coherent operator workstation. It deliberately adds no new plant capability.

The terminal remains a thin presentation layer over canonical owners:

- M7 owns guidance/checklists/training evaluation;
- M5/M6 own measured/control/protection/alarm presentation and bounded live history;
- M9.1 owns recorder/checkpoint/full replay;
- M9.2 owns immutable post-incident analysis;
- M10.5/M10.6 own assistance/control-authority and M5 supervisory coordination;
- M10.7 owns replay-backed session archive composition.

## Fixed workstation shell

The terminal is organized as four fixed regions:

1. system header + 4×2 fixed F1–F8 menu;
2. always-visible runtime/step/alarm/signal/protection summary;
3. one vertically scrollable active-page body;
4. fixed status/keyboard-help footer.

Only the page body scrolls. The operator never loses the active-page menu or the core plant-status context while reading a long log/checklist/session view.

## Navigation

- F1 GUIDANCE
- F2 INFO
- F3 ALARMS
- F4 COMMANDS
- F5 MODES
- F6 DIAGNOSTICS
- F7 LOG
- F8 SESSION

The selected page has a persistent visual indicator independent of keyboard focus. This prevents focus movement from being confused with page selection.

Keyboard-only operation uses global F1–F8, Tab/Shift+Tab, list arrow keys and Enter. Mouse operation remains fully supported.

## Responsive layout decisions

The validated MainWindow center viewport is intentionally retained. M10.8 adapts the terminal within it instead of reintroducing synthetic horizontal scrolling or minimum-width hacks:

- page menu is 4×2 rather than eight cramped columns;
- COMMANDS uses bounded min/max list height instead of a rigid 360 px row and narrower tabular columns;
- SESSION action controls use 2×2 grids and checkpoint rows avoid displaying the full fingerprint in the list itself;
- full checkpoint fingerprint remains available in the selected-item detail contract;
- no horizontal scroll is required for normal terminal operation at the validated minimum window size.

## Non-goals

M10.8 does not add:

- new physics or control logic;
- automatic operation beyond validated M10.6;
- new alarm/checklist/history/replay/session owners;
- free-form/NLP commands;
- direct state mutation from Avalonia;
- hidden wall-clock decisions.

## Validation boundary

M10.8 is VALIDATED after local compilation and the complete automated suite passed. Its terminal capability remains preserved beneath the M10.9.1–M10.9.8 operator-experience refactor; M10 closes only at M10.9.8.
