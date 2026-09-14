Date: 2026-09-14

Purpose: Correct and simplify runtime architecture to n8n-owned processing and configuration-only PostgreSQL.

$brainstorming

We need to correct and simplify the current runtime architecture before continuing milestone 5.

This is a deliberate architecture course correction.

The previous runtime design became significantly more complex than the actual product requirements justify. We introduced concepts such as pending events, separate evaluation workflows, delivery intents, runtime database state, and delivery/recovery concerns that duplicate responsibilities already provided by n8n.

That direction is now rejected.

The new design principle is:

**PostgreSQL = product configuration.**
**n8n = runtime processing, technical workflow state, deduplication, and bounded retries.**
**Sonrisa does NOT build its own workflow engine, processing queue, or delivery infrastructure.**

The goal is to make the runtime as small and understandable as possible.

# Important: reconstruct context first

This may be a fresh agent session.

Do not assume previous conversational context.

Before proposing or changing anything, inspect the actual current state.

Read at minimum:

1. root `AGENTS.md`
2. root `README.md`
3. all relevant files under `docs/`
4. all ADRs under `docs/adr/`
5. `docs/01-plan.md`
6. `docs/02-assumptions-and-open-questions.md`
7. `docs/03-scope.md`
8. `docs/04-architecture.md`
9. `docs/05-validation-strategy.md`
10. `docs/decision-log.md`
11. `docs/ai-review-log.md`
12. relevant specifications/plans
13. prompt history under `prompts/`
14. review evidence under `evidence/`
15. current EF Core model and migrations
16. actual PostgreSQL DEV schema
17. current Git status
18. recent Git history
19. workflow exports under `n8n/workflows/`
20. actual Sonrisa workflows currently deployed in the DEV n8n instance

Use:

- Rider tooling where useful
- PostgreSQL MCP for safe inspection
- n8n MCP for inspection of the actual DEV workflows and capabilities

Do not modify shared infrastructure during the initial inspection.

Store this exact final English prompt in prompt history according to `AGENTS.md`.

If this requested correction conflicts with an existing accepted ADR, do not silently ignore the conflict. Identify which previous decision needs to be superseded as part of this course correction.

# Current problem

The runtime has drifted toward a design approximately like:

External source
    ↓
Ingest workflow
    ↓
persist Pending event
    ↓
Evaluate Pending Events workflow
    ↓
persist delivery intent
    ↓
Deliver Slack Notification workflow
    ↓
delivery status / retry state

This is too complicated for the actual requirement.

We selected n8n specifically because it already provides:

- workflow orchestration
- execution state
- external integrations
- branching
- retries
- scheduling
- execution history
- failure visibility

Building our own queue/state/recovery framework in PostgreSQL defeats much of the reason for choosing n8n.

The product brief does NOT require:

- guaranteed notification delivery
- durable retry queues
- exactly-once delivery
- dead-letter queues
- notification recovery after application restart
- notification delivery history
- processing lifecycle state machines

Those requirements were introduced by us, not by the product.

They are now explicitly out of scope.

# Target runtime architecture

The desired runtime is one clear end-to-end n8n processing pipeline.

Conceptually:

Sources
    ↓
Fetch
    ↓
Normalize to canonical event
    ↓
n8n-native deduplication
    ↓
Load enabled alert configuration from PostgreSQL
    ↓
Evaluate condition
    ↓
Iterate configured notification channels
    ↓
Route by channel type
    ├── Slack
    ├── Email
    └── future channels

This should be easy to understand by looking at the workflow canvas.

A reviewer should be able to explain the runtime in one sentence:

> n8n fetches external events, normalizes and deduplicates them, reads configured alerts from PostgreSQL, evaluates the condition, and sends matching notifications through the configured channel.

If the final runtime requires explaining queues, pending state, delivery intents, workers, recovery processors, or database-backed retry state, it is still too complex.

# 1. PostgreSQL responsibility

PostgreSQL should contain PRODUCT CONFIGURATION.

Expected configuration concepts remain approximately:

- user/owner identity
- Alert
- AlertCondition
- AlertChannel

PostgreSQL should NOT become the runtime workflow engine.

The default direction is to remove/reject runtime tables and state such as:

- SourceEvent
- ProcessedEvent
- PendingEvent
- NotificationDelivery
- DeliveryAttempt
- ProcessingStatus
- RetryCount
- NextRetryAt
- DeadLetter
- delivery queues
- processing queues

unless inspection shows that an existing field/table serves a product requirement independent of the rejected architecture.

Do not keep runtime state merely because it has already been generated.

At the same time:

- do not destructively modify shared DEV PostgreSQL without approval
- do not rewrite committed migration history
- do not casually drop applied tables

First inspect what has actually been committed/applied.

If obsolete runtime schema has already been applied, propose the safest forward-removal strategy.

# 2. Runtime technical state belongs to n8n

Runtime technical state should remain in n8n wherever possible.

This includes:

- execution history
- workflow retry state
- deduplication history
- node failures
- workflow execution details

Do not mirror n8n execution state into Sonrisa product tables without a demonstrated product requirement.

# 3. Event deduplication

We still need to avoid sending the same external event again every time a polling source returns it.

Do NOT build a Sonrisa/PostgreSQL deduplication subsystem for this.

Prefer n8n's native deduplication capabilities.

Specifically investigate the current installed n8n version and verify whether the built-in `Remove Duplicates` node supports comparison against items processed in previous executions.

If supported and appropriate, use it.

The conceptual deduplication key should be based on the canonical source identity, for example:

`source + ":" + externalId`

For USGS:

`usgs:<event-id>`

The desired behavior:

First occurrence:
    continue

Same source/external ID in a later execution:
    discard

Do not add PostgreSQL state solely for this purpose.

If the installed n8n version does NOT safely support persistent cross-execution duplicate filtering:

- investigate the smallest n8n-native alternative
- do not immediately fall back to building product-level PostgreSQL queues/state
- report the limitation before changing architecture

The deduplication state is technical workflow state and should preferably remain owned by n8n.

# 4. Deduplication semantics are intentionally limited

For the MVP, the guarantee is only:

> the same `(source, externalId)` should not normally be processed repeatedly by the workflow.

We already discovered that USGS provider identifiers may change.

Do NOT solve physical-earthquake identity resolution.

Do NOT create:

- alias tables
- event merging
- provider identity reconciliation

Document the known limitation:

> If the provider later represents the same physical event using a different external identifier, the MVP may process it again.

That is acceptable.

This should remain useful AI/design-review evidence because the edge case was correctly identified but deliberately deferred.

# 5. No delivery persistence

We do NOT need to know in Sonrisa/PostgreSQL whether a Slack or email notification was successfully delivered.

Do not create or retain a database-backed notification delivery queue merely for reliability.

Do not persist:

- Pending delivery
- Sent
- Failed
- AttemptCount
- NextRetry
- delivery intent
- dead-letter state

unless inspection reveals an independent product requirement that explicitly needs such information.

The original requirement only says the user should get notified.

It does not require guaranteed delivery or audit-grade delivery history.

# 6. Notification failure semantics

Use n8n's retry/error behavior.

Target behavior:

Notification node
    ↓ failure
retry
    ↓ failure
retry
    ↓ ...
maximum approximately 5 attempts
    ↓
give up / discard

The exact implementation must follow the capabilities of the installed n8n version.

Before configuring it:

- inspect current official n8n behavior/documentation where necessary
- verify the actual node's retry capabilities
- do not assume configuration names or retry semantics

Five attempts is the desired upper bound if supported cleanly.

After retries are exhausted:

- the notification may be lost
- no Sonrisa database recovery job is required
- no next-day retry is required
- no dead-letter processor is required

This is an intentional MVP trade-off.

# 7. Delivery semantics

Do not claim exactly-once notification delivery.

A suitable documented statement is approximately:

> Notification delivery is best-effort. n8n performs bounded retries for external transport failures. If all attempts fail, the notification is discarded. In rare ambiguous transport failures, duplicate or lost notifications may occur.

That is acceptable for this MVP.

Do not introduce additional infrastructure to improve this guarantee unless the product requirement changes.

# 8. Deduplication before notification

It is acceptable that the event is considered seen by n8n before every external notification has necessarily succeeded.

For example:

Fetch event
    ↓
Normalize
    ↓
Remove Duplicates marks/recognizes event
    ↓
Match alert
    ↓
Slack
    ↓
5 failures
    ↓
notification lost

On the next source poll, the event may already be considered a duplicate and therefore not retried.

This is ACCEPTABLE.

Do not build delivery persistence to solve it.

This behavior follows directly from the best-effort notification requirement.

Document it honestly.

# 9. One primary runtime workflow

The default architecture should be ONE primary end-to-end workflow.

A suitable conceptual name is:

`Sonrisa - Process Alerts - DEV`

The exact name should follow repository conventions.

Conceptually:

Trigger
    ↓
Fetch source data
    ↓
Normalize
    ↓
Remove Duplicates
    ↓
Load candidate alerts
    ↓
Evaluate condition
    ↓
Expand configured channels
    ↓
Switch ChannelType
    ├── Slack
    ├── Email
    └── future

Do not create separate top-level workflows for:

- ingestion state
- pending evaluation
- delivery queues

Those boundaries are explicitly rejected.

A helper sub-workflow is allowed only if it genuinely improves reuse/readability and does NOT create a state/queue boundary.

The architectural goal is one understandable runtime pipeline, not literally one file at any cost.

# 10. Multiple event sources

The architecture must be easy to extend with new sources.

Future example:

USGS
RSS
Market API
    ↓
source-specific fetch / transformation
    ↓
canonical event
    ↓
same common processing pipeline

Adding a new source should mainly mean:

- fetch new provider
- normalize into canonical event

It should NOT require:

- new runtime tables
- new evaluator architecture
- new delivery architecture
- new processing queues

For the current milestone, implement only the already selected first source.

Do not implement RSS or market data yet.

# 11. Canonical event

Preserve the existing canonical-event concept if accepted by repository architecture.

For earthquake, the runtime needs only a small normalized representation approximately containing:

- source
- externalId
- eventType
- occurredAt
- title
- relevant typed data such as magnitude

Keep it minimal.

Do not persist it into PostgreSQL solely because it exists during workflow execution.

The canonical event may exist only as workflow data.

Do not make every external-source field part of the common contract.

# 12. Current USGS vertical slice

Keep USGS earthquake ingestion as the first real source unless repository inspection reveals a strong accepted contradiction.

The first end-to-end path should be approximately:

USGS
    ↓
normalize earthquake
    ↓
Remove Duplicates using source/external ID
    ↓
query enabled earthquake alerts from PostgreSQL
    ↓
evaluate configured magnitude threshold
    ↓
load configured notification channel(s)
    ↓
Slack branch
    ↓
send

That alone is sufficient for milestone 5.

# 13. Condition evaluation stays in n8n

Do not implement matching in ASP.NET.

Do not implement matching as a large database/stored-procedure engine.

The workflow should load configuration and evaluate it.

Current MVP remains intentionally small:

- one condition per alert
- earthquake
- field = magnitude
- operator = gte
- numeric value

Use the persisted textual/type-safe contract created by the management milestone.

Do not create a generic rule engine.

Malformed or unsupported configuration must produce no false match.

Fail closed.

# 14. Runtime owner behavior

The management application currently operates as a single-user MVP but is ownership-aware.

n8n runtime processing must NOT depend on the management application's configured current owner.

The runtime should process enabled alert configurations independent of the MVP UI owner mechanism.

This keeps the runtime compatible with future multi-user behavior.

Do not implement authentication.

# 15. Notification-channel extensibility

After matching, route based on persisted `AlertChannel` configuration.

Conceptually:

AlertChannel
    ↓
ChannelType
    ↓
Switch
    ├── slack
    ├── email
    └── future

Adding a future channel such as Teams should mainly require:

- adding/allowing the channel type
- adding a new dispatch branch/node

It should NOT require:

- a new top-level workflow
- a new queue
- new runtime tables
- a new worker

Use this as an architecture quality test.

# 16. Slack

Slack remains the first real notification transport.

Credentials belong to n8n.

PostgreSQL contains only non-secret destination information.

Do not store:

- bot tokens
- client secrets
- OAuth secrets

in product tables.

Validate one controlled Sonrisa DEV notification.

Do not spam the channel during testing.

Validate upstream processing independently before repeatedly invoking Slack.

# 17. Email

Email does not need to be implemented in this milestone unless the current approved milestone already requires it.

The important requirement for milestone 5 is that the architecture makes the second channel easy to add.

If an alert contains an unsupported channel during this milestone:

- skip it safely
- make the behavior visible in execution/debugging
- never report it as successfully delivered

Email implementation remains a later milestone.

# 18. n8n retry configuration

Use n8n-native retry/error features for Slack.

Inspect the installed n8n/node behavior first.

Prefer a simple bounded retry configuration.

Do not implement:

- custom retry loops with database counters
- polling retry workers
- custom backoff tables
- dead-letter queues

If n8n offers node-level retry-on-fail, prefer that.

If after the configured attempts the node still fails, the notification can be dropped.

Do not build recovery infrastructure.

# 19. Current workflows to inspect

Inspect the actual current remote state of workflows such as:

- `Sonrisa - Ingest Earthquakes - DEV`
- `Sonrisa - Evaluate Pending Events - DEV`
- `Sonrisa - Deliver Slack Notification - DEV`

Do not assume names/state from this prompt.

Determine:

- which exist
- which are active
- which contain useful implementation pieces
- which reflect the rejected architecture

Do not delete them during brainstorming.

After the simplified replacement is approved and validated:

- disable obsolete workflows
- safely remove them if appropriate
- make the simplified workflow the single authoritative DEV runtime

Do not leave two competing runtime architectures active.

# 20. Existing runtime database artifacts

Inspect whether runtime entities/migrations/tables such as:

- SourceEvent
- NotificationDelivery
- processing status

have been:

- only designed
- generated
- committed
- applied to `sonrisa_dev`

Classify each.

The target architecture no longer needs product database runtime queues/state.

If unwanted artifacts are:

## uncommitted and unapplied

Remove/simplify them before milestone completion.

## committed but unapplied

Preserve Git history appropriately and use a forward correction if repository policy requires it.

## applied to shared DEV

Do not drop them casually.

Propose a safe forward migration/removal strategy and request approval before destructive changes.

The final database should ideally return to product configuration concerns only.

# 21. Do not hide the course correction

This is valuable engineering evidence.

Do not rewrite history to make it appear that the over-engineered architecture never existed.

Record the actual review conclusion.

The substance should reflect:

> The initial AI-assisted design introduced separate ingestion, evaluation, and delivery workflows plus database-backed processing and delivery state. Review determined that this duplicated responsibilities already provided by n8n and solved reliability requirements that were not present in the product brief. The runtime was simplified to an n8n-owned end-to-end workflow using n8n-native deduplication and bounded retry behavior, while PostgreSQL remains focused on product configuration.

Use wording consistent with actual repository state.

Also record:

- USGS ID instability was identified
- alias resolution was consciously deferred
- guaranteed delivery was consciously deferred
- delivery persistence/recovery was consciously rejected as unnecessary for the MVP

These are meaningful AI review/course-correction artifacts.

# 22. Documentation correction

Update relevant architecture/plan/scope/ADR documents to make the boundary explicit:

## PostgreSQL

Product configuration:
- ownership
- alerts
- condition
- notification-channel configuration

## n8n

Runtime:
- source polling
- normalization
- technical deduplication
- condition evaluation
- channel dispatch
- bounded retries
- workflow execution state

Do not describe PostgreSQL as:

- processing queue
- event queue
- notification queue
- retry storage

unless a future requirement reintroduces those concepts.

# 23. Existing OpenTelemetry direction

Keep application OpenTelemetry as already designed.

Do not invent database state for observability.

For n8n, use:

- execution history
- execution IDs
- node errors
- any existing n8n telemetry already configured

Do not make configuring global n8n OpenTelemetry a prerequisite for this architecture correction.

Useful identifiers such as:

- source
- external ID
- alert ID

may be used for diagnosis where safe.

Do not log credentials or unnecessarily sensitive destination data.

# 24. Deterministic validation

Retain deterministic testing.

Use a controlled earthquake-shaped input that enters the SAME downstream processing path as real normalized events.

Test at least:

- magnitude below threshold -> no Slack send
- magnitude equal threshold -> match
- magnitude above threshold -> match
- disabled alert -> no send
- malformed/non-numeric magnitude -> no false match
- duplicate source/external ID -> removed by n8n deduplication
- supported Slack channel -> Slack dispatch branch
- unsupported channel -> safe skip

Also validate the real USGS source.

Do not create a second fake runtime implementation.

# 25. Deduplication-state validation

Because deduplication is now intentionally owned by n8n, explicitly validate the chosen mechanism.

Verify:

- first event passes
- same deduplication key in a later execution is removed
- workflow restart/execution behavior matches expectations
- the behavior is persistent enough for the intended DEV/runtime use
- any limits or retention behavior of the n8n dedup mechanism are understood

Do not claim permanent deduplication if n8n's actual implementation has bounded history/retention.

Document the real behavior.

If the built-in mechanism is unsuitable, STOP and report that before inventing another persistence layer.

# 26. Retry validation

Verify actual n8n behavior rather than assuming it.

If practical, simulate a controlled Slack/transport failure and confirm:

- retry occurs
- retry count is bounded
- after final failure the execution fails/stops as configured
- no Sonrisa database retry state is created

Do not disrupt shared services merely to simulate a failure.

Mock/test failure behavior is acceptable if clearly labeled.

# 27. Source control for workflows

The final tested n8n workflow must be exported to:

`n8n/workflows/`

according to repository conventions.

The repository artifact should represent the authoritative final workflow.

Do not commit:

- credential secret values
- tokens
- connection strings
- sensitive execution payloads
- unrelated n8n workflows

If exported workflow JSON contains credential references without secrets, handle them according to existing repository conventions and document required rebinding if necessary.

# 28. Keep the ASP.NET application unchanged unless necessary

This correction should not move runtime behavior into .NET.

Do not add:

- runtime APIs
- background services
- queue workers
- alert matcher services
- notification services

The management app remains thin.

Only change application code/schema if needed to REMOVE already-introduced runtime infrastructure safely.

# 29. Explicitly rejected architecture

The following concepts are now explicitly rejected for the MVP unless repository inspection reveals an independent product requirement:

- Pending Event workflow
- Evaluate Pending Events workflow
- separate Delivery workflow as a state boundary
- runtime event queue in PostgreSQL
- NotificationDelivery queue
- retry counters in PostgreSQL
- DeadLetter state
- durable delivery recovery
- exactly-once notification design
- provider-ID alias-resolution subsystem
- application runtime processor
- database-centric rule evaluation engine

Do not refine these designs.

Remove/supersede them.

# Required brainstorming output

After full inspection, present ONE recommended correction plan.

Include:

1. actual current Git/runtime state
2. currently deployed Sonrisa n8n workflows
3. current active/inactive state
4. current runtime EF models/migrations
5. current DEV PostgreSQL runtime tables
6. which over-engineered components are already committed/applied
7. what should be retained
8. what should be removed
9. safe schema cleanup strategy if required
10. proposed final primary n8n workflow
11. exact source-normalization path
12. exact n8n-native deduplication approach
13. verified capabilities/limitations of the selected n8n deduplication mechanism
14. alert-loading approach
15. condition-evaluation approach
16. channel-routing approach
17. Slack retry approach
18. actual best-effort delivery semantics
19. future source extension model
20. future notification-channel extension model
21. deterministic test strategy
22. real USGS validation strategy
23. obsolete remote-workflow removal/deactivation strategy
24. workflow source-control/export strategy
25. documentation/ADR amendments
26. AI-review/course-correction evidence to record
27. any destructive action requiring approval
28. credentials/configuration still needed
29. any remaining architecture conflict

The resulting design should be significantly smaller than the current design.

# Approval gate

STOP after presenting the correction plan.

Do NOT yet:

- modify/delete remote workflows
- apply/drop database migrations
- drop database tables
- implement the replacement workflow
- send Slack messages
- activate schedules
- rewrite Git history
- commit the correction

Wait for my explicit approval.

# After approval

After explicit approval, continue in the same session.

Implement the approved simplified architecture.

The target runtime must remain easy to explain:

> n8n fetches events, normalizes and deduplicates them, reads alert configuration from PostgreSQL, evaluates the condition, and routes matching alerts to the configured notification channel. n8n owns retry behavior and runtime technical state.

If implementation begins creating custom queues, delivery lifecycle tables, or recovery processors again, STOP and simplify.

# Final validation after implementation

Validate at minimum:

- real USGS fetch
- canonical normalization
- n8n cross-execution deduplication
- no-match behavior
- exact-threshold match
- above-threshold match
- disabled alert
- malformed magnitude
- runtime selection independent of MVP UI owner
- configured Slack destination
- one controlled real Slack send
- Slack retry behavior where safely testable
- unsupported-channel behavior
- workflow re-execution
- workflow export
- no secrets in Git
- obsolete workflows inactive/removed
- PostgreSQL contains only justified product/runtime artifacts
- ASP.NET management application still works after any schema cleanup

Do not claim validation that was not actually performed.

# Final architecture review

Before committing, explicitly verify:

1. Is there one understandable primary runtime workflow?
2. Does PostgreSQL primarily contain product configuration?
3. Is workflow technical state owned by n8n?
4. Is event deduplication n8n-native?
5. Did we avoid a product-level processing queue?
6. Did we remove Pending/Evaluated processing state?
7. Did we avoid a NotificationDelivery queue?
8. Does n8n own condition evaluation?
9. Can a future source join through normalization?
10. Can a future channel be added through routing?
11. Does Slack use bounded n8n retry behavior?
12. Are exhausted retries allowed to result in a lost notification?
13. Is that limitation documented honestly?
14. Did we avoid solving exactly-once delivery?
15. Did we preserve the architecture course correction as evidence?
16. Is the final implementation clearly simpler than what it replaced?

If any answer reveals unnecessary complexity, simplify again before committing.

# Milestone

This remains milestone 5:

`feat: implement first end-to-end n8n alert workflow`

The course correction is part of completing this milestone correctly.

Do not create a separate feature milestone merely for the correction.

After the simplified implementation is complete and validated, use the existing planned commit:

`feat: implement first end-to-end n8n alert workflow`

Do not proceed into email delivery, operational admin UI, or additional sources after this milestone.
