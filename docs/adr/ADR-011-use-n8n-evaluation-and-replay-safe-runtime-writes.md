# ADR-011: Evaluate in n8n and use replay-safe runtime writes

## Status

Accepted on 2026-09-14 through the user's explicit [milestone-5 simplification](../../prompts/022-simplify-first-runtime-workflow-design.md). This supersedes ADR-005's one-operation SQL matching/intent/completion requirement and the associated event-wide configuration snapshot assumption. ADR-005's direct database integration and n8n runtime ownership remain. Implementation and validation are recorded separately.

## Decision

Use two minimal runtime tables, source_events and notification_deliveries. n8n normalizes USGS data, reads enabled alerts across all owners and their shared user destinations, and evaluates the supported earthquake/magnitude/gte/number condition. PostgreSQL persists configuration and durable state with parameterized queries; it is not the business-rule evaluator.

Accept source events idempotently through UNIQUE(source, external_id), retaining the first accepted canonical snapshot. Create notification intent idempotently through UNIQUE(source_event_id, alert_id, channel), retaining its destination snapshot. Mark an event evaluated only after all intended delivery writes succeed. A failure leaves pending work recoverable through replay; earlier committed intents remain. No distributed lock or event-wide transaction is required.

Each evaluation run uses its joined configuration read. Replays/overlapping runs may read different configuration after edits and accumulate intents from those snapshots. Existing committed intent is not retracted by a later disable and its destination is not redirected by a later profile edit. Completed events are not automatically rematched. This intentionally gives up the earlier atomic event-wide rule snapshot guarantee in exchange for simpler inspectable n8n steps.

Use the public USGS v1.0 all-hour GeoJSON feed, with a proposed future five-minute interval. First polling accepts the bounded recent feed without archive/cursor infrastructure. USGS documents that preferred IDs can change; this MVP deduplicates repeated observations of the same source/ID only. Alias-aware identity was considered and deliberately rejected for this milestone. Physical-earthquake exactly-once identity is not claimed; association/revision handling remains possible future work.

The Slack workflow requires one explicit delivery ID and conditionally transitions pending to processing before one send. All workflows remain inactive. Processing state prevents overlapping normal entries from sending the same intent; no token, attempt history, dispatch framework or automatic recovery is added. Acknowledged acceptance is recorded as sent, known failure remains visible, and ambiguity stays unresolved without automatic retry. Email intent is unsupported, never selected for Slack and never marked delivered. Full workflow-owned automatic retries/circuits/recovery and email remain milestone 6, retaining the user's eventual duplicate-tolerant retry requirement.

## Consequences and rejected alternatives

- Reject a complex atomic SQL matcher: understandable orchestration and condition evaluation belong in n8n; database constraints make replay safe. Tests must exercise partial commits and configuration changes honestly.
- Reject the alias table: its provider-identity benefit does not justify first-slice complexity. Preserve the discovery and explicit limitation in review evidence.
- Reject generic manual-only dispatch metadata: explicit-ID delivery with no drain entry point is sufficient now. Before future automatic delivery, review retained backlog and authorized destinations; do not silently drain earlier test work.
- Reject direct unrecorded sends: durable unique intent and a conditional processing transition are the minimum current send boundary. Do not claim exactly-once external delivery or safe replay from saved transport-node input.
- Preserve EF schema ownership, shared DEV infrastructure, separate intended least-privilege runtime credentials, secret handling, owner-scoped UI, and deferred authentication. No application runtime feature expansion.

## Verification

Validate exact n8n normalizer/evaluator code, PostgreSQL source/intent uniqueness, partial-run replay, destination snapshots, cross-owner selection, unsupported configurations, concurrent conditional claims and one authorized Slack send. Real USGS retrieval complements controlled source-shaped fixtures through the same downstream path. Workflows and exports must agree and remain inactive. [Specification](../superpowers/specs/2026-09-14-first-runtime-design.md).
