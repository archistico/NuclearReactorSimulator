# M10 Final VR2 R3 Energy-Transport Ownership Repair Implementation 1 - Allocation Test Hotfix 1

NRS-MARKER:R3-ENERGY-TRANSPORT-IMPLEMENTATION1-ALLOCATION-TEST-HOTFIX1

## Scope

Test-harness correction only. The mode-2 transport lookup production path is unchanged.

The first runtime execution reached the focused allocation guard and reported 24 bytes total across 10,000 lookups after a single-call warmup. Static review of the measured lookup path found only value-type construction and array/value arithmetic, with no resolve-time resource I/O, payload decode, LINQ, delegate creation, reference allocation or per-call boxing. A fixed 24-byte total is not the signature of a recurring lookup allocation.

The focused test now separates warmup/stabilization from the authoritative steady-state measurement:

- 20,000 unmeasured warmup lookups;
- one 10,000-call stabilization window, bounded to at most 256 total bytes so a recurring allocation cannot hide;
- one subsequent 10,000-call authoritative window that must allocate exactly 0 bytes.

The contract remains `steady_state_lookup_allocated_bytes_per_call_max = 0`. No production source, C4 payload, transport tolerance, fast-gate threshold, default closure, Exact-V9 contract or R3 authority is changed.
