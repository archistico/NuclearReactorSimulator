# ADR-0208 - Execute FDPC2 under ambient runtime on the frozen A2 host

Status: Accepted for evidence execution only.

FDPC2 uses `AMBIENT-UNSET` on the same host and power scheme qualified by A2. This avoids introducing an explicit runtime production override while preserving the absolute wall-clock context of the frozen thresholds.

FDPC1 remains negative historical evidence. FDPC2 is a separately versioned confirmation and does not rewrite FDPC1.

No RP1C selection or production change is authorized by this ADR.
