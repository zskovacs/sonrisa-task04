Date: 2026-09-14

Purpose: Add Email to the existing n8n runtime and validate channel extensibility.

$superpowers:brainstorming $brainstorming

We are starting the next milestone:

`feat: add email notification channel to n8n workflow`

This milestone is intentionally also an ARCHITECTURE EXTENSIBILITY TEST.

The previous milestone established a simplified runtime architecture where:

- PostgreSQL owns product configuration
- n8n owns runtime processing
- n8n fetches and normalizes external events
- n8n performs technical deduplication
- n8n loads alert configuration from PostgreSQL
- n8n evaluates the condition
- n8n routes matching notifications to the configured transport
- runtime retry and execution state remain inside n8n
- Sonrisa does NOT maintain custom runtime queues, delivery tables, retry state, or recovery processors

The purpose of this milestone is to prove that adding a second notification channel is genuinely an extension of the existing architecture rather than a redesign.

The desired result is conceptually:

Canonical event
    ↓
Deduplicate
    ↓
Load matching alerts
    ↓
Evaluate condition
    ↓
Expand configured notification channels
    ↓
Route by channel type
    ├── Slack
    └── Email

Email must be added to the EXISTING runtime pipeline.

Do NOT create a separate email-processing architecture.

# Important: this is a fresh agent context

Do not assume any conversational context from previous agent sessions.

The repository and actual DEV environment are the source of truth.

Before proposing or changing anything, reconstruct the current state completely.

Read at minimum:

1. root `AGENTS.md`
2. root `README.md`
3. all relevant documents under `docs/`
4. all ADRs under `docs/adr/`
5. `docs/01-plan.md`
6. `docs/02-assumptions-and-open-questions.md`
7. `docs/03-scope.md`
8. `docs/04-architecture.md`
9. `docs/05-validation-strategy.md`
10. `docs/decision-log.md`
11. `docs/ai-review-log.md`
12. milestone specifications/plans
13. prompt history under `prompts/`
14. review evidence under `evidence/`
15. current application model and EF migrations
16. actual PostgreSQL DEV schema
17. current Git status
18. recent Git history
19. current workflow exports under `n8n/workflows/`
20. the actual current Sonrisa workflows deployed in DEV n8n
21. execution history/evidence from the previous Slack milestone where useful

Use:

- Rider tooling where useful
- PostgreSQL MCP for safe database inspection
- n8n MCP for actual workflow/environment inspection

Do not modify shared infrastructure during initial context reconstruction.

Store this exact final English prompt in prompt history according to `AGENTS.md`.

If this request conflicts with a currently accepted ADR or the actual completed milestone-5 implementation, STOP and report the conflict before implementation.

# Milestone objective

Extend the existing authoritative Sonrisa runtime workflow with Email as a second notification transport.

The milestone should prove this architectural claim:

> Adding another notification channel should primarily require another transport branch, not changes to event ingestion, deduplication, alert matching, runtime persistence, or overall workflow architecture.

This is an important validation criterion.

If email requires substantial redesign of:

- event ingestion
- canonical normalization
- deduplication
- matching
- PostgreSQL runtime state
- top-level workflow structure

then STOP and report why the current architecture is not as extensible as intended.

Do not silently add complexity to make it work.

# Expected current runtime architecture

Verify the actual implementation first, but the expected current runtime is approximately:

USGS
    ↓
Normalize canonical earthquake event
    ↓
n8n-native deduplication
    ↓
Load enabled alerts/configuration from PostgreSQL
    ↓
Evaluate magnitude condition
    ↓
Expand configured notification destinations
    ↓
Channel routing
    ↓
Slack

The desired extension is:

USGS
    ↓
Normalize canonical earthquake event
    ↓
n8n-native deduplication
    ↓
Load enabled alerts/configuration from PostgreSQL
    ↓
Evaluate magnitude condition
    ↓
Expand configured notification destinations
    ↓
Channel routing
    ├── Slack
    └── Email

Everything before channel routing should ideally remain unchanged.

# 1. Do NOT create a second top-level runtime workflow

Email belongs in the SAME authoritative runtime pipeline.

Do not create:

- `Sonrisa - Deliver Email - DEV`
- an Email queue
- an Email worker
- a separate Email event-processing workflow
- database-backed email delivery state

The preferred architecture is a channel router/switch within the existing runtime.

A small reusable helper/sub-workflow is acceptable only if:

- the transport implementation genuinely benefits from reuse
- it does not create a queue/state boundary
- it reduces complexity rather than adding abstraction

Do not extract a sub-workflow merely because abstraction is possible.

For this MVP, an additional branch in the existing workflow is likely sufficient.

# 2. PostgreSQL remains configuration-only

Do NOT add runtime database tables for Email.

Do NOT introduce:

- EmailDelivery
- NotificationDelivery
- DeliveryAttempt
- RetryCount
- DeliveryStatus
- DeadLetter
- EmailQueue
- processing state

The current simplified architecture deliberately rejected those concepts.

PostgreSQL should continue to store only the non-secret product configuration already required by the application.

Inspect the actual current notification configuration model.

The previous implementation may store Slack/email destinations directly on the user/profile rather than in a generic `AlertChannel` table.

Preserve the implemented configuration model unless Email literally cannot be represented by it.

Do not refactor the product schema merely to create a theoretically perfect channel abstraction.

# 3. This milestone tests channel extensibility

Explicitly assess how much of the system must change to add Email.

The ideal answer is approximately:

Unchanged:
- source ingestion
- canonical normalization
- deduplication
- alert query
- condition evaluation
- ownership behavior

Changed:
- notification expansion/routing
- Email transport branch
- SMTP credential binding
- message formatting
- transport-specific validation/tests

If significantly more changes are required, document that honestly.

Do not hide architecture friction.

The final review should explicitly answer:

> Did adding Email validate the claim that notification channels are easy to extend?

# 4. Existing SMTP implementation/history

Previous Git history may contain an earlier SMTP implementation from before the runtime simplification.

Inspect it.

You may reuse transport-specific knowledge or configuration from that implementation, but do NOT restore the rejected runtime architecture around it.

Potentially reusable:

- verified SMTP node configuration
- safe credential reference
- known-good sender configuration
- message formatting
- validation lessons

Do NOT restore:

- delivery workflow boundaries
- delivery intent tables
- queue state
- processing states
- runtime database persistence
- old retry architecture

Treat historical code/workflows as reference material, not as something to cherry-pick blindly.

# 5. Email configuration

Inspect the current product configuration.

Determine how the recipient email address is currently represented.

Use the existing product configuration if appropriate.

Do not introduce duplicate email configuration if the user/profile already contains the required destination.

Do not hard-code the recipient.

Do not store SMTP credentials in PostgreSQL.

The separation must remain:

PostgreSQL:
- non-secret recipient/destination configuration

n8n:
- SMTP credential
- transport authentication

Do not store:

- SMTP passwords
- provider API keys
- OAuth secrets
- access tokens

in product data, workflow exports, documentation, prompts, or evidence.

# 6. SMTP credential

Inspect the DEV n8n environment for an existing intended Sonrisa SMTP credential.

Do not expose its secret values.

If an appropriate credential already exists:

- reuse it
- verify connectivity safely before the controlled send

If no credential exists or it is unusable:

STOP at the point where real transport validation requires it and ask me to configure one.

Do not create arbitrary credentials using guessed settings.

Do not include credentials in prompt history.

# 7. Email message format

Keep email content deliberately simple.

This milestone is about transport extensibility, not email design.

Prefer a simple plain-text message unless the existing validated transport implementation already uses a safe minimal HTML template with a compelling reason.

A suitable message may contain:

- Sonrisa alert name
- earthquake title
- magnitude
- occurred-at timestamp
- source URL when present and valid

Keep subject concise and recognizable as a Sonrisa alert.

For example conceptually:

Subject:
`Sonrisa alert: <event title>`

Body:
- alert name
- magnitude
- event time
- source link

Do not create:

- branded HTML email design
- responsive email templates
- inline image infrastructure
- tracking pixels
- attachments
- localization framework

Avoid unnecessary source/user data.

External source text must be treated as untrusted content.

If HTML is used, ensure source-controlled text is encoded appropriately.

Plain text is preferred because it minimizes this concern.

# 8. Channel routing

The existing runtime should route each configured destination independently.

Conceptually:

Configured notification
    ↓
ChannelType
    ↓
Switch
    ├── slack
    └── email

The routing model should allow both transports to exist for the same matching event when the persisted product configuration requests both.

Do not make Slack and Email mutually exclusive unless the current product configuration explicitly defines them that way.

The exact expansion logic must follow the actual implemented configuration model.

Do not invent a new model merely to match this diagram.

# 9. Independent channel failure

One failing transport must not prevent other configured notifications from being attempted.

Example:

Matching event
    ↓
Slack notification
    ↓ success

Email notification
    ↓ fails after retries
    ↓ discarded

The successful Slack notification remains successful.

Likewise:

Email failure must not prevent another user's Slack or email notification from being processed.

Do not abort the entire notification batch because one transport item fails.

Use n8n-native item/error handling.

Do not add persistence to achieve this.

# 10. Retry semantics

Use the SAME architectural policy established for Slack:

- n8n owns retry
- bounded retries
- target approximately five total attempts where supported
- after exhaustion, discard that notification
- continue processing remaining notification items

Do not implement custom persistent retry infrastructure.

Verify the actual SMTP/email node behavior in the installed n8n version.

Do not assume retry settings merely because Slack supports them.

If node-level retry semantics differ:

use the smallest n8n-native pattern that achieves bounded attempts and continue-after-failure.

Do not create:

- database retry counters
- retry jobs
- delayed queues
- delivery tables
- dead-letter state

# 11. Delivery semantics remain best-effort

The accepted product semantics remain:

> Notification delivery is best-effort.

For Email:

- n8n attempts delivery
- bounded transport retries occur
- after exhaustion the notification is discarded
- Sonrisa does not persist it for later recovery

Rare lost or duplicate notifications during ambiguous provider failures are acceptable MVP limitations.

Do not claim exactly-once or guaranteed delivery.

Do not introduce stronger semantics just for Email.

Slack and Email should follow the same architectural philosophy.

# 12. Deduplication remains unchanged

Do not modify event deduplication merely because a second transport is added.

The existing n8n-native source/external-ID deduplication should remain the same.

A single event should pass deduplication once and may then fan out to multiple configured notification channels.

Conceptually:

Event
    ↓
Deduplicate ONCE
    ↓
Match
    ↓
fan out
    ├── Slack
    └── Email

Do NOT deduplicate independently per channel at the source-event level.

Do NOT create email-specific deduplication tables.

# 13. Condition evaluation remains unchanged

Do not change the alert matching model.

The milestone should not modify:

- earthquake normalization
- magnitude extraction
- `gte` comparison
- typed numeric handling
- one-condition MVP
- owner-independent runtime alert selection

If Email requires changes to condition evaluation, something has gone wrong architecturally.

Report that instead of silently redesigning it.

# 14. Multi-user-ready runtime behavior

Continue processing enabled configuration across owners.

Do not use the management application's `MvpOwner:Id` in runtime processing.

Recipient addresses must come from the matched alert owner's persisted product configuration.

Never mix destinations between owners.

Validation should include at least two distinct configurations where practical to prove destinations are associated with the correct owner.

Do not implement authentication.

# 15. Unsupported/future channel types

The runtime may encounter an unsupported channel type in the future.

Unknown channel types must:

- not be routed to Slack
- not be routed to Email
- not be reported as delivered
- not break unrelated valid notifications where practical
- remain visibly diagnosable in execution output

Do not implement a generic plugin framework.

A safe default/unsupported branch is sufficient.

# 16. Workflow activation

Keep the authoritative Sonrisa runtime workflow inactive at milestone completion unless an accepted decision has changed this.

Manual validation remains sufficient.

Do not turn on unattended polling merely because Email now works.

This milestone tests transport extension, not production scheduling.

# 17. Do not modify old archived workflows

The previous runtime simplification should have left old workflows disabled/archived or otherwise non-authoritative.

Do not restore or modify them as part of Email support.

Only the current authoritative simplified runtime workflow should be extended.

If remote n8n inspection shows multiple competing active Sonrisa runtime workflows, STOP and report the discrepancy.

# 18. OpenTelemetry / observability

Do not introduce new product-level logging infrastructure.

Use existing:

- n8n execution history
- node errors
- execution IDs
- existing telemetry
- application observability where relevant

For Email transport diagnosis, enough information should be available to distinguish:

- transport attempted
- transport succeeded
- transport exhausted retries
- unsupported configuration

Do not log:

- SMTP password
- credentials
- tokens
- unnecessary recipient addresses
- full external source payload

Do not create database tables just for delivery observability.

# 19. Deterministic validation

Test the Email branch without depending exclusively on a real earthquake occurring.

Use the existing deterministic fixture/test entry that reaches the SAME downstream matching and dispatch path.

Do not create a separate fake email workflow.

Validate at minimum:

## Email-only path

Matching event
→ Email configured
→ Email branch selected
→ one controlled email received

## Slack-only path

Existing Slack behavior still works.

## Both channels

Where supported by the actual product configuration:

Matching event
→ Slack
→ Email

Both transports are independently attempted.

## No email destination

Do not attempt Email.

## Invalid/missing Email configuration

Safe skip or visible validation failure according to the current configuration contract.

No accidental send.

## Unsupported channel

Safe visible skip.

## Email failure

Bounded retries.

After exhaustion:
- discard that email
- continue processing another valid notification

Use mocked/controlled failure where safer than breaking real SMTP infrastructure.

Clearly distinguish mocked retry evidence from actual successful SMTP delivery evidence.

# 20. Real end-to-end Email validation

Perform ONE controlled real Email notification after upstream validation passes.

Before sending, verify:

- intended Sonrisa workflow
- intended alert fixture/event
- intended recipient
- intended SMTP credential
- test message is clearly identifiable

Do not send repeated real messages during debugging.

Validate upstream routing without transport first where practical.

# 21. Regression validation

Because this milestone is explicitly testing extensibility, prove that adding Email did not break Slack.

Re-run relevant existing validation:

- real/deterministic source processing
- deduplication behavior
- alert matching
- Slack routing
- one safe Slack path if necessary
- Email routing

Do not repeat external sends unnecessarily.

Use execution inspection for portions already known to work.

# 22. Database/schema expectation

The expected result is:

NO database schema change.

Email configuration should already be representable in the product model.

If implementing Email requires a migration:

STOP during brainstorming and explain exactly why.

A small configuration-model change is not categorically forbidden, but requiring one would weaken the claim that the current channel model is already sufficient.

Do not create a migration without explicit approval if one is unexpectedly required.

Under no circumstances reintroduce runtime delivery tables.

# 23. ASP.NET application expectation

The expected result is:

little or NO ASP.NET code change.

The management application already supports the required notification configuration according to previous milestones.

If Email needs a small management-side validation/configuration correction, identify it during brainstorming.

Do not introduce:

- notification sending services
- runtime APIs
- email workers
- SMTP libraries in ASP.NET
- delivery persistence

Transport execution remains n8n-owned.

# 24. Workflow source control

After the Email branch is implemented and tested:

- export the actual authoritative remote workflow
- update the existing artifact under `n8n/workflows/`
- do not create a parallel workflow artifact merely for Email
- verify repository export matches the tested remote graph
- sanitize environment/credential references according to existing repository conventions
- verify no secrets or pinned sensitive execution data are committed

The Git diff should make the transport extension understandable.

# 25. Architecture extensibility evidence

This milestone is intentionally evidence for the original architectural claim.

Record the result honestly.

Good evidence would be:

> Email support was added by extending the notification-routing portion of the existing n8n workflow. Source ingestion, canonical normalization, event deduplication, alert loading, condition evaluation, and PostgreSQL schema remained unchanged.

Only make that statement if it is actually true.

If adding Email required broader changes, document those instead.

The quality of the architecture is being tested here, not assumed.

# 26. AI review focus

Critically inspect generated changes for signs of architecture regression.

Look specifically for:

- creating a separate Email workflow
- reintroducing NotificationDelivery
- adding runtime database state
- adding a custom retry loop backed by PostgreSQL
- adding SMTP code to ASP.NET
- modifying event normalization unnecessarily
- modifying deduplication unnecessarily
- duplicating alert matching for Email
- hard-coded recipient address
- SMTP credentials in workflow JSON
- one Email failure aborting all remaining notifications
- accidentally routing Email config to Slack
- Slack regression after channel-router refactor
- large HTML email/template infrastructure
- restoring code from the rejected runtime architecture
- adding unnecessary abstractions merely because there are now two channels

Correct these before accepting the implementation.

Record meaningful AI-generated mistakes/course corrections in `docs/ai-review-log.md`.

Do not manufacture review findings.

# 27. Documentation updates

Update only documentation genuinely affected by this milestone.

At minimum record:

- Email is now the second supported runtime transport
- Slack and Email share the same runtime pipeline
- transport credentials remain n8n-owned
- product DB contains only non-secret notification configuration
- transport retry remains bounded/best-effort
- failed delivery is discarded after retries
- no durable delivery state exists
- adding Email did or did not validate the channel-extensibility claim
- future channels should extend the same routing boundary

Update the milestone roadmap/status accordingly.

Do not rewrite unrelated architecture.

Create an ADR only if implementation reveals a genuinely new architectural decision.

Adding one transport branch by itself does not justify a new ADR.

# Required brainstorming output

After full context reconstruction, present ONE recommended implementation design.

Include:

1. actual current authoritative n8n workflow
2. current channel-routing structure
3. current product notification configuration model
4. whether Email requires any database/schema change
5. whether Email requires any ASP.NET change
6. available SMTP credential/configuration state
7. proposed Email routing branch
8. proposed message format
9. proposed SMTP node/configuration approach
10. proposed retry/error behavior
11. how processing continues after an Email failure
12. how Slack remains isolated from Email failures
13. how one event can fan out to both transports
14. deterministic Email validation plan
15. real Email validation plan
16. Slack regression validation
17. workflow export/source-control plan
18. documentation/evidence updates
19. exact scope of expected code/workflow changes
20. whether this validates the architecture's channel-extensibility claim
21. any credentials/configuration required from me
22. any architecture conflict discovered

Prefer the smallest implementation.

# Approval gate

STOP after presenting the design.

Do NOT yet:

- modify n8n workflows
- send Email
- send Slack
- modify PostgreSQL
- install packages
- modify ASP.NET
- create migrations
- commit implementation

Wait for my explicit approval.

# After approval

After explicit approval, continue in the same agent session.

Implement the approved Email extension.

Do not ask for another generic approval unless:

- a required credential is missing
- a schema migration unexpectedly becomes necessary
- a destructive shared-infrastructure action is required
- a new architecture conflict is discovered

# Acceptance criteria

The milestone is complete only when:

- Email is part of the existing authoritative runtime workflow
- no separate Email runtime architecture was created
- source ingestion remains unchanged unless a correction was genuinely required
- canonical normalization remains unchanged
- n8n-native deduplication remains unchanged
- alert matching remains unchanged
- PostgreSQL runtime state was not introduced
- Email recipient comes from persisted product configuration
- SMTP credentials remain in n8n
- one controlled real Email is successfully received
- Slack behavior still works
- both channel paths can coexist where configuration allows
- one failed Email does not stop processing unrelated notifications
- retry exhaustion discards only that failed notification
- workflow remains inactive at milestone completion
- authoritative workflow export is updated in Git
- no secrets are committed
- documentation accurately records the extensibility result

# Final architecture review

Before committing, explicitly answer:

1. How many top-level authoritative runtime workflows exist now?
2. Did adding Email require changes before the channel-routing boundary?
3. Did adding Email require a database migration?
4. Did adding Email require ASP.NET runtime code?
5. Did we add any runtime persistence?
6. Can Slack and Email both use the same event/matching pipeline?
7. Does one channel failure remain isolated from other notifications?
8. Are retry semantics still n8n-owned?
9. Could a future Teams channel reasonably be added as another routing branch?
10. Did this milestone actually validate the architecture's extensibility claim?

If the result is more complex than adding a transport branch should reasonably be, simplify before committing.

# Milestone commit

After implementation and validation, create:

`feat: add email notification channel to n8n workflow`

Include only files relevant to this milestone.

Do not amend previous milestones.

Do not include:

- secrets
- local environment files
- credential values
- sensitive execution payloads
- unrelated n8n exports
- IDE-local files

# Final report

At completion, report concisely:

1. context reviewed
2. authoritative workflow modified
3. Email branch implementation
4. SMTP configuration approach
5. real Email send result
6. Slack regression result
7. both-channel behavior
8. retry/failure behavior
9. database changes, ideally none
10. ASP.NET changes, ideally none/minimal
11. workflow export updated
12. validation performed
13. AI-generated issues corrected
14. documentation updated
15. whether the architecture extensibility claim was validated
16. files changed
17. milestone commit hash
18. remaining risks before the operational admin-view milestone

Do not proceed into operational admin UI, additional event sources, or stronger delivery reliability after this milestone.
