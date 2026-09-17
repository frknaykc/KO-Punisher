# Tooltip OCR — verified state (2026-09-15)

## Production behavior

`TooltipOcrWindows.ReadAsync` defaults to **TesseractSoftContrast**: separate text bands, 4x enlargement, English LSTM and SingleLine (PSM 7). It reads supplied bitmaps only; it never captures the screen or sends input. The caller owns the input and returned Processed bitmap. Cancellation is checked between native operations; native resources remain alive until a running operation settles.

Ship the complete dist directory: Tesseract 5.2.0, x64 native libraries and tessdata/eng.traineddata. The model is tessdata_fast 4.1.0, SHA256 7d4322bd2a7749724879683fc3912cb542f19906c83bcc1a52132556427170b2. Windows OCR modes remain comparison-only and require the English Windows OCR capability.

Icon exclusion now uses header geometry rather than description-driven panel width. Unknown/ambiguous types and uncertain levels remain outside upgrade selection; no character substitutions are used. Evidence contains the source panel and exact OCR inputs.

## Verification

- Tests/Tests.csproj: 273 test groups passed on macOS; these do not exercise Windows input.
- Tests.Ocr.Windows/Pure/Preprocessing.csproj: 40 checks passed.
- Actual Windows OCR: four recorded fixtures passed exact name/type/level assertions, including Spirit of Genie and Helmet(+1). Selected description assertions also passed; complete description accuracy is not guaranteed.
- Added live-inventory fixture covering all 28 slots: 9 empty, 19 occupied. Full-texture maximum RGB L1 error is 9; darkness alone never proves emptiness. The small dark-item regression still passes.
- Widened-tooltip regression checks that panel width does not imply UI scale. No-icon and ambiguous-panel rejection tests remain passing.

Live scanning requires the game foreground and the same privilege level. A higher-integrity target now produces an actionable error before any movement. Tests.Inventory.Live is a console-free, read-only harness; no clicking, keyboard input, scroll use or Anvil actions are implemented there.

Earlier live baseline: 28 slots scanned, 16 parsed readable (not all exact), 2 empty, 10 unknown. Spirit of Genie was clipped; the recorded regression now passes. Final live results are recorded separately; do not substitute the earlier baseline for final verification.