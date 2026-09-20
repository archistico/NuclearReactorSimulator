# M10 Final VR2 R3 Energy-Transport Ownership Repair Implementation 1 - Allocation Slope Hotfix 3

NRS-MARKER:R3-ENERGY-TRANSPORT-IMPLEMENTATION1-ALLOCATION-SLOPE-HOTFIX3

## Scope

Test-only correction. Production lookup, C4 payload, thermodynamic values, thresholds, fast gate and R3 authority are unchanged.

## Returned evidence motivating the hotfix

Three independent local runs reported a fixed 24-byte thread-allocation delta for a 10,000-call measurement window, including after extended warmup and after forcing warmup and measurement through a single interface-dispatch callsite. The delta did not scale with lookup count and the production lookup path contains no recurring managed allocation.

## Qualification rule

The planning requirement is zero steady-state allocation *per lookup*. The focused test therefore measures the marginal allocation slope across 1,000, 10,000 and 20,000 lookups through the same warmed helper. Fixed measurement/runtime overhead up to 256 bytes is permitted for the 1,000-call sample, but increasing the call count must add exactly zero managed bytes:

- allocation(10k) - allocation(1k) = 0 bytes;
- allocation(20k) - allocation(10k) = 0 bytes.

Any recurring per-call allocation grows with call count and remains RED. This does not introduce a per-call tolerance.
