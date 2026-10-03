# Feature boundaries

Phase 1 contains only the `system` feature used to verify browser-to-API communication.
Future phases will introduce isolated `market`, `news`, `economic-data`, `analysts`,
`signals`, `backtesting`, and `statistics` features without moving shared transport code
out of `lib/api`.
