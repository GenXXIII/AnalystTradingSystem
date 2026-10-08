# 0008: Full Analyst is independent and WAIT-first

Status: Accepted

The Full Analyst owns separate jobs, snapshots, workspace configurations, provider credentials, specialist outputs, master results, and lifecycle history. Local Analyst signals and Target Analyst targets are not synthesis inputs. Telegram Scanner data is outside this repository boundary.

Full Master weighs evidence quality, freshness, independence, conflict, uncertainty, and risk; it does not count specialist votes. Any missing, stale, invalid, malformed, incomplete, or insufficient evidence path may resolve to `WAIT`. Confidence describes evidence quality and is not a profit probability.

Phase 14 records whether a future scenario is eligible but does not create one. Future path generation remains a Phase 15 responsibility.
