# MVP scope

The [original brief](00-product-brief.md) requires configurable alerts, Slack and email, channel extensibility and an admin view. [ADR-012](adr/ADR-012-use-n8n-native-runtime-state.md) records the approved correction: those requirements do not imply guaranteed delivery, durable queues, delivery history or recovery processors.

## Current milestone 5

Implement one inactive, manual `Sonrisa - Process Alerts - DEV` workflow: real USGS all-hour data or explicit synthetic fixture, canonical validation, n8n-native within/across-execution duplicate filtering, enabled configuration reads across owners, deterministic one-condition magnitude matching, destination expansion, channel routing and Slack transport. Use five total native attempts per notification; discard an exhausted failure visibly and continue other notifications. Validate the actual source, deterministic cases, native duplicate history and retry isolation, and one controlled real Slack send.

Remove the rejected product runtime entities and applied tables via a reviewed forward EF migration. Preserve old migrations, commits and evidence; archive the three old workflows only after replacement validation. Keep the actual tested export in Git without credentials or pinned execution data. This is a correction within milestone 5, not a new feature milestone.

The configuration model, UI, owner resolution and application telemetry remain unchanged. PostgreSQL stores `users` and `alerts` plus EF history. The condition remains earthquake/magnitude/gte/number, with one finite numeric threshold per alert. Destinations are shared per-user fields; runtime processing across owners does not broaden management UI access.

## Required later work

Email is required by the product. The next runtime milestone adds email as another branch of this same primary workflow, optionally reusing only useful SMTP transport-specific code from history. It must not restore a separate delivery workflow, queue or product runtime state. The currently unsupported email branch records a skip, never success.

The admin view is also a later product milestone. Its precise product purpose must be specified without assuming an event/delivery ledger or retry/circuit dashboard. n8n already provides technical execution investigation. This task does not authorize admin UI implementation.

## Explicit limitations

- The workflow remains inactive; five-minute polling is only future configuration. First live processing considers the bounded current hour, without a cursor or historical import.
- Native deduplication is bounded technical history. Installed n8n 2.38.7 can stop at the history cap rather than automatically roll entries out. Lost/reset/recreated history or changed provider IDs can permit repeats; no globally atomic concurrency claim is made.
- Seen events are not rematched for subsequently created/edited alerts or automatically recovered after downstream failure.
- Slack is best-effort, with bounded retries and possible duplicate/lost messages after ambiguous failures. No permanent or exactly-once notification guarantee exists.
- Malformed events/configuration/destinations fail closed with diagnostics; unrelated valid alerts/channels continue where practical. One exhausted Slack notification must not stop later notifications.
- Authentication, multiple conditions, AND/OR groups, a rules DSL, additional sources, geospatial rules, physical-earthquake alias resolution, runtime APIs, workers and product queues are excluded.

## Preserved topology and history

The application runs locally with Razor Pages/EF, using existing shared DEV PostgreSQL and hosted n8n. Do not provision either service or change shared n8n/global telemetry/credentials without need. Secrets stay in external configuration and n8n credentials; connection-string values never enter Git.

The first runtime at `b9cf171` and SMTP extension at `e1dc337` were genuinely implemented and validated under earlier approvals. Their database state and workflow boundaries are now superseded deliberately, while their historical evidence remains intact. See the [approved correction specification](superpowers/specs/2026-09-14-runtime-simplification-design.md).
