# First runtime database and workflow contract

[ADR-011](adr/ADR-011-use-n8n-evaluation-and-replay-safe-runtime-writes.md) and the [approved specification](superpowers/specs/2026-09-14-first-runtime-design.md) govern milestone 5. The [approved SMTP extension](superpowers/specs/2026-09-14-smtp-delivery-design.md) subsequently adds email through the same selected-ID workflow. EF migration `20260914155714_FirstRuntimeSlice` owns the initial schema; `20260914173558_EnableEmailDeliveryStates` enables active email delivery states. n8n owns normalization, evaluation and transport; the ASP.NET application has no event-processing service or HTTP integration. See [actual validation](../evidence/reviews/2026-09-14-first-runtime-validation.md) for the initial execution status and limitations, and [SMTP validation](../evidence/reviews/2026-09-14-smtp-delivery-validation.md) for the subsequent extension.

## Canonical mapping

The public USGS v1.0 [all-hour GeoJSON feed](https://earthquake.usgs.gov/earthquakes/feed/v1.0/summary/all_hour.geojson) is bounded to recent events and [updated every minute](https://earthquake.usgs.gov/earthquakes/feed/v1.0/geojson.php). Five-minute polling is a proposed future schedule, balancing demo latency and shared DEV load. This milestone uses manual execution and leaves every workflow inactive.

| Canonical field | Mapping and validation |
| --- | --- |
| `id` | Generated UUID; stable product investigation key. |
| `contract_version` | Supported version `1`. |
| `source` | Trusted live entry fixes `usgs`; explicit fixture entry fixes `demo.usgs`. Never selected by provider text. |
| `external_id` | USGS feature `id`; nonblank printable ASCII without spaces, at most 128 characters. |
| `event_type` | `earthquake`; unsupported record types are skipped. |
| `occurred_at` | `properties.time`, interpreted as UTC epoch milliseconds. Invalid/missing timestamps are rejected. |
| `title` | Bounded, trimmed plain-text `properties.title`; no control characters, at most 300 characters. |
| `source_url` | Optional valid HTTP(S) `properties.url`, at most 2048 characters; a display reference, never a fetch instruction. |
| `data` | JSONB object containing only numeric finite `magnitude` from `properties.mag`. String/null/boolean values are not coerced. |
| `received_at` | PostgreSQL ingestion time, independent of provider occurrence time. |
| `processing_status` | `pending` initially, `evaluated` after successful intent persistence, including a non-match. |

The workflow bounds input size/record count as specified and skips malformed individual records with safe diagnostic codes. Invalid top-level input fails the execution. A full provider payload is not retained in product data.

## Source identity and replay

`public.source_events` has `UNIQUE(source, external_id)` and a partial `(received_at, id)` index for Pending reads. Atomic insertion with `ON CONFLICT DO NOTHING` preserves the first accepted canonical snapshot. Repeated polling, restart, concurrent insert and manual replay under the same source/ID do not create another event. Provider content updates under that ID do not change the stored magnitude or rematch an evaluated event.

[USGS documents that preferred identifiers may change](https://earthquake.usgs.gov/data/comcat/#id). This MVP therefore does not guarantee one record per physical earthquake after a provider rename. Associated IDs and alias merging were investigated and deliberately deferred. There is no alias table, source cursor, generic source schema, update/retraction processing or cross-provider identity resolution.

The first ingestion accepts the current one-hour feed and evaluates it normally; no historical/archive import occurs. It may create multiple durable intents. It cannot cause a burst of external sends because delivery requires one explicitly selected intent and has no queue-drain entry point.

## Configuration selection and evaluation

Each evaluation invocation obtains one Pending event and loads relevant enabled alerts joined with `users` through `owner_id`. It processes all owners and never reads `MvpOwner:Id`. The management UI remains owner-scoped under ADR-008; runtime backend access is a separate concern.

The joined query yields one event/alerts envelope even if no alerts are enabled. n8n evaluates only `earthquake` / `magnitude` / `gte` / `number`, with finite numeric operands and the inclusive `>=` boundary. It neither relies on C# integer enums nor evaluates scripts/DSL/SQL stored in user configuration. Unknown/malformed conditions fail closed with a diagnostic; valid alerts continue.

Each attempt reads one consistent joined configuration snapshot. n8n prepares intents, persists them with parameterized values, and separately completes the event. There is no event-wide SQL matcher/transaction. Interrupted evaluation can replay Pending work; earlier committed intents remain. Replays or overlapping attempts may read changed settings and accumulate unique intents from those reads. No event-wide rule revision/snapshot guarantee is implied. Completed events are not automatically rematched.

## Minimal notification intent

`public.notification_deliveries` stores `id`, `source_event_id`, `alert_id`, textual `channel`, `destination`, `status`, `created_at`, optional `sent_at` and optional safe-code `last_error`. Source/alert foreign keys restrict deletion. `UNIQUE(source_event_id, alert_id, channel)` is the final idempotency boundary. Shared profile destinations create at most one Slack and one email intent per event/alert; no per-alert channel table is needed.

The destination is copied when the intent is first created. A later settings edit or duplicate insert cannot redirect that intent; a later disable does not retract it. Message content uses the immutable canonical source event and stable IDs. No rule/profile revisions, attempts, provider-delivery metadata, tokens, circuits, retry counters or dispatch-mode framework are introduced.

| Status | Current behavior |
| --- | --- |
| `pending` | New Slack/email intent available only to explicit-ID manual delivery. |
| `processing` | The conditional pending-to-processing claim succeeded; outcome may be unknown. No automatic reset/retry. |
| `sent` | Transport acceptance has been durably recorded with `sent_at`. SMTP acceptance is not inbox placement or human receipt. |
| `failed` | A known rejection is recorded with a bounded safe code. No automatic retry. |
| `unsupported` | Retained pre-extension email intent, `last_error=transport_not_implemented`, no sent timestamp. Never claimed, converted or automatically backfilled. |

`last_error` contains bounded lowercase diagnostic tokens, never raw provider exceptions or destinations. Database constraints reject inconsistent channel/status/timestamp combinations.

## Selected-ID delivery safety

The delivery workflow defaults to an empty delivery ID. An operator must select one intended UUID. A conditional database update claims only that pending Slack/email intent; zero updated rows stop before transport. Concurrent normal entries cannot both claim the same intent. Native send retries are disabled. No database transaction remains open during transport access.

Before live validation, verify the Sonrisa test alert, exact snapshot destination, intended n8n transport credential and selected intent. Credentials remain in n8n; PostgreSQL stores only non-secret destination configuration. Slack content must escape provider text and suppress mentions/unfurls where applicable. Synthetic messages are visibly marked.

Transport acceptance followed by a failed result write is ambiguous. Keep the intent `processing`, investigate, and do not automatically resend. A crash before the send may look the same; this milestone deliberately accepts that unresolved recovery gap. Do not replay the transport node directly with saved inputs; start at the selected-ID claim gate. No exactly-once delivery is claimed. Automatic retry/recovery and full attempts/circuits remain milestone 6. The SMTP transport extension does not complete those guarantees.

## Email transport

The same delivery workflow branches by the claimed record's textual channel. SMTP uses the native Send Email node with a separately bound SMTP credential, one explicit non-secret sender profile and the immutable recipient snapshot. New matching email configuration creates Pending intent. Existing Unsupported rows remain historical evidence.

Only one bare mailbox is accepted at the SMTP boundary; ambiguous recipient lists/groups, control characters and header syntax fail closed before the send. Invalid preparation is recorded as Failed/`invalid_email_message`. Messages are plain text, with domain identifiers and a synthetic marker, without CC/BCC, attachments or arbitrary headers. This transport boundary deliberately supports a conservative subset of mailbox syntax; it does not broaden profile validation.

A Sent outcome requires an acknowledgement for exactly the intended sole recipient with no rejected recipients and a consistent envelope. Native SMTP errors or incomplete/contradictory results retain Processing/`delivery_outcome_unknown`; no automatic resend or fragile error-message classifier is introduced. Use SMTP4DEV capture to verify the isolated DEV path. Production SMTP delivery and inbox placement are not established by that capture.

## Inspection and future activation

Use n8n execution visibility plus source, external ID, canonical event ID, alert ID and delivery ID. Product tables answer whether an event was seen, which intents exist, and whether acceptance was recorded. There is no new admin dashboard, vanity history table or synchronous ASP.NET trace propagation through PostgreSQL. Global n8n OTEL configuration is unverified and unchanged.

Product SQL uses explicit columns, fixed identifiers and parameter values. Runtime credentials should read configuration and read/write only these operational records, without DDL or schema ownership. Any current DEV privilege discrepancy must be reported separately from the intended least-privilege model. EF migrations alone change schema.

Before a later automatic-delivery milestone, review retained pending/test intents and authorized destinations instead of blindly draining this milestone's backlog. Retention/deletion can remove deduplication keys and permit replay, so no automatic cleanup is introduced. Test fixtures are identified explicitly and removed only by exact owned IDs where cleanup is part of the test.
