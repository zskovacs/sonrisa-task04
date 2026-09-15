# n8n runtime contract

[ADR-012](adr/ADR-012-use-n8n-native-runtime-state.md) and the [approved specification](superpowers/specs/2026-09-14-runtime-simplification-design.md) replace the earlier database-backed runtime. PostgreSQL stores product configuration; canonical events and technical state belong to n8n. Historical first-runtime and SMTP evidence remain linked from the decision/review logs.

## Source and canonical mapping

Use the public [USGS all-hour GeoJSON feed](https://earthquake.usgs.gov/earthquakes/feed/v1.0/summary/all_hour.geojson), documented as [updated every minute](https://earthquake.usgs.gov/earthquakes/feed/v1.0/geojson.php). Proposed future polling is five minutes; the milestone workflow remains inactive and manually operated. First execution accepts the current bounded hour, without a cursor or archive import. It may match multiple events, so live validation suppresses transport before one authorized synthetic send.

| Field | Mapping |
| --- | --- |
| `contract_version` | `1`, retaining the accepted versioned envelope. |
| `source` | Trusted live path fixes `usgs`; explicit fixture path fixes `demo.usgs`. Provider content cannot choose the path. |
| `external_id` | Feature `id`, nonblank printable ASCII without spaces, at most 128 characters. |
| `event_type` | `earthquake`; unsupported feature types skipped. |
| `occurred_at` | `properties.time` as UTC epoch milliseconds, validated within supported bounds. |
| `title` | Bounded sanitized title, with a magnitude/place fallback. |
| `source_url` | Optional bounded HTTP(S) reference; invalid values omitted, never used as a fetch instruction. |
| `data` | Object containing only finite numeric `magnitude` from `properties.mag`; no string/null/boolean coercion. |

Normalizer limits are 1,000 records and 2 MiB input; invalid top-level shape fails visibly, malformed individual records produce safe diagnostic codes while other records continue. Canonical data is not stored in Sonrisa PostgreSQL. No product event ID, received timestamp, processing status or provider payload columns exist.

## Native duplicate filtering

Use Remove Duplicates v2 twice: within this input compare `source` and `external_id`, then remove keys seen in previous executions using `source + ':' + external_id`, node scope, history size 10,000. Scope follows n8n workflow/node identity; preserve both on updates. Native technical state lives in n8n-owned persistence, which this repository does not manage.

In n8n 2.38.7, as recorded during runtime validation, the previous-execution node checks stored count plus incoming item count **before filtering**, throwing when it exceeds the history cap. This is not automatic rolling retention or a permanent event ledger. Very old keys are not guaranteed after history reset/loss, identity recreation or changed retention behavior. Concurrent executions have no claimed globally atomic event-claim guarantee. Do not clear history automatically or add PostgreSQL state to solve these accepted limits; review capacity before unattended activation.

USGS [preferred identifiers may change](https://earthquake.usgs.gov/data/comcat/#id). A different external ID may therefore cause the same physical earthquake to be processed again. Alias tables, reconciliation and event merging are deliberately rejected for this MVP. Seen events are not rematched for newly created or edited alerts, or provider magnitude revisions under the same retained key.

## Configuration read and evaluation

The native PostgreSQL node executes only the fixed query in `n8n/sql/select-enabled-alerts.sql`, with one canonical event bound as a JSON parameter. It joins enabled earthquake alerts to users via owner ID and returns condition descriptors, the numeric threshold and shared destinations. No `MvpOwner:Id` filter, runtime write, DDL, stored rule engine or dynamic SQL concatenation exists. Each joined read sees a committed configuration snapshot; subsequent configuration changes affect later processing.

n8n evaluates only earthquake/magnitude/gte/number using finite numeric operands and inclusive `>=`. Disabled or unsupported/malformed configuration never matches. The configuration query additionally projects alert name and the evaluator carries it as `alert_name` solely for Email text; this does not change selection or evaluation. Expand the matching owner's non-secret destinations into channel items; destination validation is independent so one malformed channel does not suppress a different valid one. Unknown channels generate diagnostic skips. They never reach Slack or Email or report transport acceptance.

## Transport dispatch and failure isolation

Process one notification per Loop Over Items iteration. Route Slack to the native message/post node using the configured destination, with credentials held by n8n. Prepare bounded text, escape untrusted content and disable mentions/markdown/unfurls where applicable. Route Email to the native SMTP node using the matching owner's configured mailbox and n8n-owned SMTP credential. Prepare bounded plain text with alert name, magnitude, UTC occurrence time, readable source and a validated optional HTTP(S) source link; synthetic Email subject/body are unmistakably marked. Do not expose internal alert/execution IDs in user-facing Email text.

Both native transport nodes use five **total** attempts, five seconds between failures, and an explicit separate error output. Exhaustion records a safe discard diagnostic in execution data and returns to the loop. Success, invalid Email preparation and unconfirmed Email outcomes also return to the loop. A failed notification must not stop subsequent notification items. No product delivery record is created for either result.

Delivery is best-effort: bounded retries can end in a lost notification; ambiguous transport failures can duplicate or lose messages. Because events are marked seen before configuration/transport, later polling may never retry their notifications. This is intentional. There is no queue, attempt counter, circuit, dead-letter state, recovery processor, next-day retry or exactly-once guarantee.

Email is the second supported transport in this same route. SMTP4DEV validation proves one successful SMTP submission/capture; it does not prove external-provider or external-inbox delivery. A provider replacement requires credential/sender configuration changes, not workflow redesign.

## Operations and source control

Use n8n execution visibility and IDs, safe source/external/alert identifiers, node errors and terminal per-item diagnostics. Avoid credential/destination values in extra logs and avoid full payload dumps. Application OpenTelemetry remains unchanged; global n8n OTEL is unverified and not a prerequisite. PostgreSQL does not propagate application trace context.

The actual tested primary workflow is exported under `n8n/workflows/process.json`. Credential references are deliberately removed by the validated exporter; rebind the PostgreSQL, Slack and SMTP nodes after import. Never export secrets, pins, temporary mock nodes or execution histories. Read [the runbook](../n8n/README.md) for concrete commands, names and validation boundaries.

Forward migration `20260914184146_RemoveObsoleteRuntimeState` removed the two obsolete product runtime tables in dependency order, preserving historical migrations. Recreating an old schema does not restore deleted rows. Old inactive workflows were archived after replacement acceptance; history/evidence remain intact. Intended n8n database access is SELECT on users/alerts only. Existing broader DEV credential permissions remain a discrepancy, not a production recommendation.
