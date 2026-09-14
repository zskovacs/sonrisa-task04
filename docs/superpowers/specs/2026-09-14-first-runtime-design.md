# First n8n runtime slice design

> Historical implementation plan/design: runtime boundaries are superseded by the approved [runtime simplification](../specs/2026-09-14-runtime-simplification-design.md). Preserve this record with its original implementation and validation context.


Approved on 2026-09-14 through [the user's simplifying correction](../../../prompts/022-simplify-first-runtime-workflow-design.md), which supersedes the more complex pre-approval proposal. No further generic design gate is required. Implementation acceptance remains separate.

## Scope and architecture correction

One real USGS earthquake source, deterministic n8n condition evaluation, durable source/event and notification identities, and one explicitly selected Slack send. Use existing shared DEV n8n and `sonrisa_dev`. The application remains configuration management and EF schema ownership only.

The user explicitly supersedes ADR-005's atomic SQL evaluation/intent/completion allocation: n8n now loads a configuration snapshot, evaluates the one supported condition, writes idempotent intents and then completes the event in separate replay-safe steps. Record this in ADR-011. No application evaluator, SQL business-rule engine, cross-node transaction assumption, aliases, attempts table, tokens, revisions, circuit state, dispatch framework, automatic retries, email transport or operational UI.

## Source and canonical event

Use `https://earthquake.usgs.gov/earthquakes/feed/v1.0/summary/all_hour.geojson`. The future polling cadence is five minutes, UTC; all three workflows remain inactive at handoff. Manual execution is sufficient. The first run accepts the current bounded hour, without a cursor or archive import. Longer outages and delayed reports outside the window can be missed.

Normalize GeoJSON FeatureCollection records into version 1 canonical events: trusted source `usgs`, provider `id` as external ID, event type `earthquake`, UTC occurrence from numeric epoch milliseconds, bounded plain title, optional valid HTTP(S) source URL, and validated JSON data with one numeric finite `magnitude`. Received time comes from PostgreSQL. The source namespace `demo.usgs` identifies synthetic events and is derived only from a distinct manual fixture entry, never source content. Live and synthetic records share normalization and downstream processing; no scheduled path reaches fixture generation.

Bounds: source 32 characters; external ID 128 nonblank printable ASCII characters excluding whitespace; title 300 characters; URL 2048 characters; canonical JSON data contains only magnitude; contract version 1. Use provider title where present, otherwise derive a plain title from magnitude and bounded place, falling back to `Earthquake`. Safely omit invalid optional URLs. Reject missing/non-numeric/nonfinite magnitude, invalid occurrence time and malformed required identifiers with fixed diagnostic codes and safe external identity where available. Do not coerce strings, null, booleans or missing values to numbers. Per-record failure must not discard otherwise valid records. Reject malformed/oversized top-level input visibly; ingestion input limits are 2 MiB and 1000 features.

USGS preferred identifiers can change. The accepted guarantee is deduplication of the same `(source, external_id)`, not physical-earthquake exactly-once identity. Alias-aware identity is deliberately deferred. First accepted snapshots are immutable, including magnitude/title. Corrections under the same ID do not rematch a completed event.

## Minimal product schema

`public.source_events`: `id uuid` primary key; `contract_version integer`; `source varchar(32)`; `external_id varchar(128)`; `event_type text`; `occurred_at timestamptz`; `title varchar(300)`; nullable `source_url varchar(2048)`; `data jsonb` (strict magnitude object); `received_at timestamptz` database default; `processing_status text` in `pending/evaluated`. Unique `(source, external_id)` and partial pending index ordered by received time and ID. The synthetic source implies its marker; no duplicate boolean or execution-history columns.

`public.notification_deliveries`: `id uuid` primary key; `source_event_id uuid` and `alert_id uuid` restricted foreign keys; `channel text` in `slack/email` (the stable channel identity); `destination varchar(254)` snapshot; `status text` in `pending/processing/sent/failed/unsupported`; `created_at timestamptz` database default; nullable `sent_at timestamptz`; nullable `last_error varchar(200)` containing safe codes only. Unique `(source_event_id, alert_id, channel)`. Slack starts pending; email starts unsupported with `transport_not_implemented` and never enters Slack selection. Processing gates one send without leases/tokens; attempt recovery remains out of scope. Sent requires a timestamp; unsupported is email-only. Use constraints for nonempty IDs, codes, finite event magnitude, nonblank bounded text and safe channel destination shape. No provider message metadata or condition/profile revision snapshots.

The immutable event supplies message content later; the alert ID identifies the matched rule. Delivery does not reread mutable destination or threshold to construct old messages. No automatic deletion or expiry. Future backlog automation requires a deliberate operator review of retained intents; this milestone has no drain entry point or dispatch framework.

## Replay and matching semantics

Ingestion uses parameterized `INSERT ... ON CONFLICT (source, external_id) DO NOTHING`; duplicate input never updates an accepted snapshot.

Each evaluation invocation processes one selected/oldest pending event (repeat manual runs to make progress). A single explicit SELECT joins enabled alerts across all owners to users and captures their shared destinations. It must always return exactly one envelope containing the selected event and an alerts JSON array, using COALESCE(jsonb_agg(...), '[]'::jsonb) for zero enabled alerts; an empty candidate set must not terminate n8n item flow before event completion. Pass the native float8 condition value and exact textual codes to one focused n8n evaluation node. Only `earthquake/magnitude/gte/number` and finite JS numeric values match. Unknown fields/operators/types, malformed destinations and invalid values produce safe diagnostics and no false match; other alerts continue. Never consult `MvpOwner:Id`.

The evaluator emits `{event_id, intents, diagnostics}` even for zero matches. Intent items contain only alert ID, channel and snapshot destination. A parameterized set-based insert expands these prepared intents; SQL performs persistence, not condition evaluation. `ON CONFLICT ... DO NOTHING` retains the first committed destination. A successful write returns one summary row even when there are zero intents. Only then update the event to evaluated. Never mark completion on a database-error branch.

If a run fails before completion, earlier committed intents remain and the event stays pending. Replay reloads current enabled configuration and reuses existing intent identities. Under edits or overlapping runs, the accumulated intents may reflect more than one configuration snapshot; no event-wide atomic snapshot or exactly-once evaluation is promised. Configuration edits/disable cannot retract a committed intent. Completed events are not normally selected again. Uniqueness handles overlapping executions without distributed locking.

## Three small workflows

1. `Sonrisa - Ingest Earthquakes - DEV`: manual live entry (future schedule documented, inactive) -> HTTP retrieval -> common normalization/validation -> idempotent source insert. A distinct manual/test fixture entry goes through the same normalization node with a trusted synthetic envelope. Use a built-in manual/form/subworkflow trigger only as supported safely by installed MCP; do not publish a webhook for fixtures. At most these three workflows; no extra sub-workflows.
2. `Sonrisa - Evaluate Pending Events - DEV`: manual entry -> read one pending event -> load joined configuration -> focused deterministic evaluator -> idempotent intent insert -> completion update. Empty queue completes without writes; zero matches still complete the selected event.
3. `Sonrisa - Deliver Slack Notification - DEV`: manual input requires a valid explicit delivery UUID -> atomic conditional pending-to-processing update for that Slack ID -> prepare immutable event message -> one Slack call -> record success/failure. No query scans pending deliveries for sending. The ID is initially empty so a casual run cannot send.

Use native HTTP/Postgres/Slack nodes. Focused Code nodes may implement validation and typed evaluation where clearer than large expressions; test their exact code, not a parallel fake implementation. All SQL identifiers are fixed and values parameterized. Do not bind arbitrary credential IDs from product data. Inspect SDK and node schemas before writing workflow code; validate and inspect actual remote connections after every create/update.

## Slack safety and delivery outcomes

Before live delivery verify the selected Sonrisa alert, destination snapshot, current intended DEV Slack credential, explicitly authorized test channel and one delivery ID. Native node retries must be disabled and effective transport behavior checked. Escape or disable Slack markup, mentions and link/media unfurls; show synthetic marker and event/alert/delivery identifiers. No full source payload, email address or credential is logged.

The conditional update allows only one concurrent run to transition pending to processing. No matching row means no send. Acknowledged Slack success transitions processing to sent with database time. Repeating outcome persistence is harmless and never resends. Known rejection records failed with a sanitized error classification; uncertain transport failure records processing/`delivery_outcome_unknown`. A crash or failure after sending but before recording leaves processing, visibly unresolved; no automatic retry or reset exists. Manual saved-node replay bypassing the claim is prohibited in the runbook. This is not exactly-once delivery or completed automatic recovery. No reset operation is added to the workflow.

## Validation and permissions

EF creates one forward migration with only the two runtime tables and their constraints/indexes. Controller reviews source/SQL, verifies external migration target `sonrisa_dev`, applies and inspects schema. Never n8n DDL or shared service reconfiguration. Existing application DEV privilege/TLS exception remains; inspect n8n role separately and disclose broad access without falsely calling it least privilege. Desired n8n rights: configuration SELECT and required source/delivery SELECT/INSERT/UPDATE only; no config writes, DDL or ownership. Do not create/replace shared credentials.

Meaningful tests use exact workflow normalization/evaluator code and real PostgreSQL constraints/queries. Cover below/equal/above threshold, disabled, unsupported code, nonnumeric magnitude, duplicate and changed-payload source, repeated/partial evaluation, owner-independent matching, destination snapshot preservation, email unsupported, no-match/empty queue, concurrent source/intent creation and conditional delivery claim. Database interruption is simulated without stopping shared services. Tests own exact fixture IDs and clean only their own records; no schema reset or disabling shared constraints.

Execute real USGS retrieval in n8n, deterministic fixtures through the same path, and one controlled Slack send once runtime configuration is available. An unconfigured credential is a blocker for the dependent execution, not for writing/validating the inactive workflow. Do not claim mocks prove DB or transport integration. No automatic schedules are published.

## Source control and evidence

Keep actual remote exports in `n8n/workflows/`; reproducibly remove credential bindings, pin data and environment noise, document rebinding, and compare the sanitized remote definition with Git. Store editable workflow code/SQL and small deterministic tests under `n8n/` only where they make the tested export reproducible. Do not add dependencies for an SDK installed locally; use live MCP validation and native Node test runner. Do not commit execution dumps or secret material.

Use domain identifiers and n8n executions for inspection. No global OTEL configuration or synthetic distributed traces through PostgreSQL. Record actual validation and meaningful corrections, update current docs and preserve superseded history. Controller creates only the requested milestone commit after required review and actual acceptance; no merge/push or next-milestone implementation.
