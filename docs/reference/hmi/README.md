# HMI Schematic Design References

These user-provided SVGs are design references for the approved M10.9 operator-experience refactor. They are **not authoritative runtime topology or protection logic**. M10.9.3 now realizes the whole-plant visual grammar as native Application-owned presentation contracts plus Avalonia rendering; the SVGs remain reference material rather than runtime data sources.

- `plant-mimic-schematic.svg` — whole-plant process/energy-path visual grammar realized by M10.9.3; retained as a design reference.
- `reactor-core-detail-schematic.svg` — reactor/core dependency visual grammar for M10.9.4.
- `turbine-island-detail-schematic.svg` — turbine/secondary equipment/flow visual grammar for M10.9.4.
- `instrumentation-protection-detail-schematic.svg` — signal/control/protection visual grammar for M10.9.4; signal paths remain distinct from process piping.

Implementation must bind canonical Application presentation contracts and preserve the ownership rules in `OPERATOR_EXPERIENCE_HMI_ARCHITECTURE.md` and ADR 0075.
