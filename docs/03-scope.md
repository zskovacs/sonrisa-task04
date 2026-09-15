# MVP scope

The [product brief](00-product-brief.md) requires configurable alerts, Slack and Email delivery, channel extensibility and an admin view. Implementation and integrated validation completed at `e667447`; this documentation milestone records the result without adding behavior.

## Implemented

| Capability | Delivered behavior |
| --- | --- |
| Alert management | Razor Pages create/list/edit/enable/disable; one finite magnitude `>=` threshold per earthquake alert; new alerts disabled. No delete action. |
| Ownership-aware persistence | Stable owner UUID, scoped management queries, revision conflicts and atomic saves; shared per-owner Slack/Email settings. |
| Source and matching | USGS current one-hour GeoJSON, canonical normalization and deterministic earthquake/magnitude/gte/number evaluation in n8n. |
| Technical deduplication | Native within-batch and node-scoped previous-execution filtering by source/external ID; history bound 10,000. |
| Slack and Email | Same matcher and destination expansion, separate native transports, five total attempts per item with five-second waits; discard and continue after exhaustion. |
| Product admin | Read-only cross-owner `/admin`, `/admin/users`, `/admin/alerts`: counts, state, typed conditions and channel presence; full destinations omitted. |
| Diagnostics | Application OpenTelemetry logs/request traces and health endpoints; runtime execution/failure/retry inspection in n8n. |
| Development operation | Local application or application-only Docker, existing shared DEV PostgreSQL/n8n, one inactive manual workflow, reviewed export and deterministic fixtures. |

PostgreSQL stores `users`, `alerts` and EF migration history. Conditions are inline alert columns; non-secret destinations are shared user fields. The application manages configuration; n8n reads enabled alerts across owners. The configured owner is not a security boundary.

Email extended the channel route without schema or ASP.NET changes. Its small upstream addition projects the alert name and passes it through for message text; source ingestion, normalization, deduplication, selection and condition-matching behavior stayed unchanged. See [Email evidence](../evidence/reviews/2026-09-14-email-channel-validation.md).

## Explicitly out of scope / not implemented

- Authentication/authorization, production access isolation, role switching and full user administration. Admin pages are unprotected MVP surfaces.
- Additional sources/event types, multiple conditions, AND/OR groups, arbitrary scripts, a rules DSL and geospatial rules.
- Guaranteed or exactly-once delivery, queues, delivery ledgers, dead letters, recovery workers, circuits and infinite retries.
- Physical-earthquake alias reconciliation, cross-provider merging, correction/retraction handling and retrospective matching of previously seen events.
- Automatic scheduling/production activation, archive ingestion, completeness/latency guarantees and high-scale processing.
- Angular or a separate browser application, application runtime HTTP APIs, local PostgreSQL/n8n provisioning and new observability backends.

## Operating limitations

The current-hour feed has no cursor or history import. Deduplication history is bounded technical state, not a permanent product ledger: the validated n8n release can fail at capacity before filtering, rather than evict old entries. Lost/reset/recreated history or changed provider IDs can permit repeats; globally atomic concurrency is not established.

An event is marked seen before configuration reads and delivery. A downstream database failure or exhausted transport can therefore lose its notifications permanently. New/edited alerts do not rematch seen keys. Ambiguous provider outcomes can cause duplicates or loss.

SMTP4DEV proves SMTP submission and capture only; internet mailbox delivery remains unvalidated. Shared DEV administrative access and the observed lack of PostgreSQL TLS remain the accepted [DEV exception](adr/ADR-007-accept-current-dev-database-access.md), not production acceptance. See the [validation boundaries](05-validation-strategy.md).

## Future work

1. Select a real Email provider and validate an authorized external recipient.
2. Define access requirements before adding authenticated ownership and protected admin access.
3. Decide whether unattended operation is needed, then review source frequency and native-history capacity against an actual usage target.

No future item is authorized by this scope document.

## Preserved history

The first runtime (`b9cf171`) and SMTP extension (`e1dc337`) genuinely implemented database-backed runtime state and separate workflows. [ADR-012](adr/ADR-012-use-n8n-native-runtime-state.md) deliberately superseded them, and `c38f2a3` removed the runtime tables with a forward migration and archived the old workflows after replacement validation. Historical migrations, prompts and evidence remain intact. [Milestone history](01-plan.md) and [retrospective](final-reflection.md) explain the trade-off.
