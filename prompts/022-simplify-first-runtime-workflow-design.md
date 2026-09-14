Date: 2026-09-14

Purpose: Simplify and approve the first runtime workflow design and authorize implementation.

I approve the overall direction, but NOT the design exactly as proposed.

Before implementation, simplify it according to the following decisions.

The goal remains one credible end-to-end vertical slice, not production-complete earthquake processing.

## 1. Keep the selected source

Approved:

- USGS earthquake feed
- public/no-secret integration
- bounded recent feed
- five-minute polling as the proposed future schedule
- workflow remains inactive at milestone completion
- real-source validation plus deterministic test input

The schedule should not be activated automatically during this milestone.

Manual execution is sufficient to prove the workflow.

## 2. Do NOT implement alias-aware earthquake identity in this MVP

The investigation into USGS identifiers was valuable and should be retained as design/review evidence.

However, `source_event_aliases` and alias-merging behavior are too much complexity for this first vertical slice.

For the MVP use:

`UNIQUE(source, external_id)`

where `external_id` is the selected USGS event identifier.

Document explicitly that:

- USGS may change preferred identifiers
- therefore this MVP guarantees deduplication for repeated observations of the same `(source, external_id)`
- it does NOT guarantee physical-earthquake exactly-once identity if the provider changes the identifier
- alias-aware identity resolution is a possible future improvement

This is an intentional scope decision, not an overlooked issue.

Record the rejected alias-table proposal as meaningful AI/design review evidence if appropriate.

Do not create `source_event_aliases` in this milestone.

## 3. Keep runtime persistence minimal

Introduce only runtime state that is necessary for correctness and recoverability.

### SourceEvent

Use a minimal persisted source/canonical event representation consistent with the accepted canonical-event ADR.

It should contain only fields justified by the runtime flow, approximately:

- Id
- Source
- ExternalId
- EventType
- OccurredAt
- Title
- typed/canonical event data
- ReceivedAt
- ProcessingStatus if needed for recoverability

Database-enforced uniqueness on:

`(Source, ExternalId)`

is required.

A small processing state such as `Pending` / `Evaluated` is acceptable because inserting an event and then failing during evaluation must not permanently lose that event.

Do not add speculative execution/history metadata unless it has an immediate use.

### NotificationDelivery

A minimal durable notification intent IS approved in this milestone because it provides the idempotency boundary required for a safe end-to-end notification.

Keep it small.

It should represent approximately:

- Id
- SourceEventId
- AlertId
- AlertChannelId or equivalent stable channel identity
- destination snapshot if required to prevent later configuration changes from redirecting an already-created intent
- Status
- CreatedAt
- SentAt where applicable
- LastError where applicable

Use a database uniqueness constraint that prevents the same event/alert/channel from creating duplicate delivery intents.

Do not introduce in this milestone unless strictly required:

- attempt-history tables
- first-attempt tokens
- condition revisions
- profile revisions
- circuit state
- retry counters/backoff models
- provider-specific delivery metadata
- a generic `dispatch_mode` framework

Those belong to the durable-delivery/retry milestone if they become necessary.

## 4. Keep condition evaluation in n8n

Do NOT move the alert-matching business logic into one complex PostgreSQL statement.

The intended architecture is:

PostgreSQL stores configuration and durable state.

n8n performs runtime orchestration and deterministic condition evaluation.

The workflow should conceptually:

1. obtain a pending canonical event
2. load relevant enabled alerts and their configured condition/channel data
3. evaluate the persisted typed condition in n8n
4. create durable delivery intent(s) using safe parameterized SQL
5. mark the event evaluated once processing completes

For the current MVP:

- one condition per alert
- earthquake event type
- magnitude field
- `gte`
- numeric value

Follow the persisted textual database contract.

Do not build a generic rule engine.

Unknown or unsupported configurations must fail closed / produce no match and be diagnostically visible.

## 5. Prefer idempotent replay over one giant transaction

We do not need to force the entire event-evaluation process into one large PostgreSQL transaction.

Prefer simple replay-safe steps backed by database constraints.

For example:

- source event insertion is idempotent because of `(source, external_id)` uniqueness
- delivery creation is idempotent because of the delivery uniqueness constraint
- a Pending event can be evaluated again if a previous execution failed
- repeated delivery-intent creation resolves safely as already-existing work

This is easier to understand in n8n and keeps business logic out of complex SQL.

Use transactions where a genuinely atomic relational update requires them, but do not use a large SQL transaction merely to simulate an application processing engine inside PostgreSQL.

## 6. Keep the three workflows only if they remain small

The proposed workflows are acceptable:

- `Sonrisa - Ingest Earthquakes - DEV`
- `Sonrisa - Evaluate Pending Events - DEV`
- `Sonrisa - Deliver Slack Notification - DEV`

The separation is justified only because it creates useful recovery boundaries:

- ingestion can be rerun without another notification
- evaluation can resume Pending events
- delivery can be tested independently

Do not add further sub-workflows unless clearly necessary.

Do not hide the whole system inside giant Code nodes.

All three workflows should remain inactive at milestone completion.

## 7. First-run behavior

The bounded one-hour USGS feed is acceptable.

Do not build a cursor system.

On first ingestion:

- accept the bounded current feed
- persist new events
- deduplicate by source/external ID
- evaluate them normally

No historical/archive import is needed.

Because delivery remains manually controlled in this milestone, the first run must not automatically generate a burst of external Slack messages.

It is acceptable for multiple durable delivery intents to be created.

## 8. Slack delivery must require one explicit delivery intent

Implement Slack as the first real transport.

The delivery workflow in this milestone must NOT automatically scan and drain every Pending delivery.

For the controlled end-to-end test, it should require one explicit delivery ID / selected intent.

That gives us safe milestone-5 behavior without introducing a general `manual_only` dispatch framework.

Before the real send verify:

- Sonrisa test alert
- Slack channel configuration
- intended DEV credential
- intended destination
- one specific delivery intent

Perform only the minimum controlled real Slack sends required for validation.

If Slack accepts the message but persistence of the success result fails, treat the result as ambiguous.

Do NOT automatically retry an ambiguous delivery in this milestone.

Do not claim exactly-once notification delivery.

Automatic retry/recovery remains milestone 6.

## 9. Email remains unsupported at runtime in this milestone

Existing email channel configuration may produce an intent if that is consistent with the approved model, but email must not be sent.

It must never be reported as successfully delivered.

Use clear behavior such as an unsupported/deferred state or otherwise ensure it is visibly excluded from Slack delivery.

Do not implement the email transport yet.

## 10. Runtime must process alerts across owners

Approved.

The n8n runtime must not use `MvpOwner:Id`.

That mechanism is only for the single-user management UI.

Runtime processing should consider all enabled matching alert configurations in the database so the execution model remains multi-user-ready.

Do not implement authentication.

## 11. Canonical event mapping

Keep the canonical mapping minimal and consistent with the existing ADR.

For the earthquake slice, the runtime data required for matching is essentially:

- source = `usgs`
- external ID
- event type = `earthquake`
- occurred time
- title
- optional source URL where valid
- typed data containing numeric `magnitude`

Validate malformed/missing/non-numeric magnitude safely.

Do not copy unnecessary full provider payloads into first-class database columns.

Do not build generic source-schema infrastructure.

## 12. Deterministic testing

Approved.

Provide a controlled fixture/manual-input path that enters the SAME normalization/evaluation pipeline used by the real source.

Do not create a separate fake matching implementation.

Synthetic/test input must be clearly distinguishable and must never be accidentally picked up by a future recurring production trigger.

Use deterministic tests to prove at minimum:

- magnitude below threshold -> no intent
- magnitude equal to threshold -> match
- magnitude above threshold -> match
- disabled alert -> no intent
- malformed magnitude -> no false match
- duplicate source event -> no duplicate SourceEvent
- repeated evaluation -> no duplicate delivery intent

Also execute ingestion against the real USGS source.

## 13. PostgreSQL ownership and migrations

Approved:

- EF Core remains schema owner
- n8n must not perform DDL
- create forward migration for the minimum runtime tables
- review migration and generated SQL
- verify the target `sonrisa_dev` database
- apply only after confirming no unrelated objects are affected

Do not grant unnecessary PostgreSQL privileges.

If the available n8n credential is broader than least privilege, document the discrepancy rather than silently presenting it as the desired production permission model.

## 14. n8n source control

Approved.

The actual tested remote workflows must be exported into:

`n8n/workflows/`

Keep the Git artifacts representative of the tested workflows.

Remove/sanitize only environment-specific or sensitive material where necessary.

Never commit:

- credential secrets
- tokens
- connection strings
- execution payload dumps containing sensitive data

Document any required credential rebinding.

## 15. Observability

Do not make global n8n OpenTelemetry reconfiguration part of this milestone.

The fact that n8n OTEL configuration could not currently be verified is acceptable.

Use:

- n8n execution visibility
- SourceEvent ID
- external event ID
- Alert ID
- NotificationDelivery ID
- relevant execution identifiers

for runtime investigation.

Do not fake distributed trace propagation through PostgreSQL.

Do not log credentials or notification destinations unnecessarily.

## 16. Credentials/configuration

I approve the design direction before credentials are supplied.

Continue implementation until a real credential or test destination is actually required.

At that point, ask me separately for only the runtime configuration you need.

Never store secret values in:

- prompt history
- Git
- documentation
- evidence

A non-secret n8n credential name and authorized Sonrisa Slack test channel ID/name may be recorded if necessary.

## 17. Documentation/course-correction evidence

Please explicitly preserve the useful reasoning discovered during brainstorming:

- USGS preferred IDs may change
- alias-aware deduplication was considered
- it was deliberately deferred because the MVP only needs source/external-ID idempotency
- complex atomic SQL matching was rejected in favor of n8n-owned deterministic evaluation plus database idempotency constraints
- full automatic notification retries remain deferred

These are meaningful engineering trade-offs and should be represented honestly in the decision/review artifacts.

## Approval

With the simplifications above, the design is approved.

Proceed with:

1. the milestone specification
2. reviewed implementation plan
3. minimum EF runtime model/migration
4. n8n workflow implementation
5. deterministic validation
6. real USGS validation
7. one controlled Slack end-to-end validation when safe configuration is available
8. workflow export/source control
9. documentation/review evidence
10. milestone commit

Use the planned commit:

`feat: implement first end-to-end n8n alert workflow`

Do not proceed into automatic retries, general backlog draining, email delivery, or the next milestone.
