# Advanced Instrument & Gauge System

## Purpose

M10.9.2 provides a reusable HMI instrument language for values that must be interpreted relative to limits, targets or setpoints.

The system is deliberately presentation-only. Domain/Simulation owners still define plant configuration, controllers, alarms, protection and measured-signal validity.

## Reading an instrument

An advanced gauge may contain several independent layers:

```text
scale                 where a value can be displayed
operating band        canonical interpretation, only when explicitly available
target band           desired window / synchronization window / scenario target
setpoint               controller/reference request
protection marker      authoritative protection threshold
current marker         currently published value
trend                  change per published logical step
```

Absence of a colored operating band means “no canonical band was published”, not “everything is normal”. Every advanced gauge also prints a compact numeric semantics line so colors are never the only carrier of range/threshold meaning.

## Provenance and quality

Every advanced instrument keeps source meaning visible:

- `MEASURED` — instrumentation path;
- `MODEL` — explicitly diagnostic/model value;
- `ANNUNCIATOR` — annunciator-derived information when used;
- `SOURCE —` — unspecified presentation provenance.

Quality is independent from numeric range. A suspect or unavailable measurement does not silently fall back to true/model state.

## Off-scale behavior

A numeric value outside the display scale remains numerically available when the source itself is valid. The gauge:

- clamps only the graphical marker to the nearest edge;
- adds an explicit off-scale arrow/endpoint indication;
- prints `< minimum` or `> maximum` status.

This is intentionally different from silently clipping a value and presenting it as in-range.

## Trend semantics

Trend is a presentation diagnostic, not a physics derivative:

```text
rate = (current published value - previous published value)
       / (current logical step - previous logical step)
```

It never uses UI frame rate or wall-clock elapsed time. Same-step publications and backwards logical-step discontinuities (reset/load/replay boundaries) produce `TREND —`.

## Linear vs circular selection

Use a linear gauge for most bounded engineering quantities and multi-marker comparisons.

Use a circular gauge only where dial position is operationally useful, such as rotor speed relative to rated/overspeed state. Avoid turning every number into a dial.

## Ownership rule

Avalonia may map already-published semantics to geometry and color. It must not:

- decide alarm/trip thresholds;
- infer a normal band from gaps between thresholds;
- calculate protection permissives;
- replace missing measurement with model/true state;
- create scenario targets before the scenario/training owner publishes them.
