# M10.9.7.4 Exact-V9 Hosted Runtime Alignment Diagnostic 1 — Returned-Evidence Adjudication 1

## Result

`RUNTIME-PATCH-MISMATCH-EXCLUDED`

The one-shot GitHub probe installed SDK `10.0.105`, requested `Microsoft.NETCore.App 10.0.5`, set
`DOTNET_ROLL_FORWARD=Disable`, and the Exact-V9 trace itself reported `.NET 10.0.5`.

The raw V1 Exact-V9 aggregate nevertheless remained:

`1E8AAF3799059D8C9FB2E930981E26B0B31ECF465673AB6C722B24A95D80FDD7`

rather than the historical local:

`7880AD580179B936C584EB0055BE663E0A1CFA65C5191B0DB8A7F3C514DB5418`.

The runtime-alignment artifact and the ordinary hosted artifact have byte-identical sequence TSVs, all 128 selector
payloads, all 128 direct payloads and all stage causal/resolver/shared-node TSVs. Only environment summary files differ.

## Engineering interpretation

The returned evidence does not support a persistent physical divergence. Prior diagnostics established one
host-sensitive low-order floating-point seam at step 126, followed by bit-identical reconvergence at steps 127 and 128.

The raw V1 aggregate is therefore retained as same-host exact evidence and historical provenance. It is not suitable as
a frozen cross-host assertion.

No further ULP-by-ULP physical diagnostics are authorized by this adjudication. The successor is the versioned
cross-host presentation-canonical Determinism Contract V2.
