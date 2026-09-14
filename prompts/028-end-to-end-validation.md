Date: 2026-09-14

Purpose: Validate end-to-end MVP behavior and failure scenarios.

We are starting the next milestone:

`test: validate end-to-end behavior and failure scenarios`

This is a fresh agent session.

Do not assume any conversational context from previous sessions.

The repository, Git history, current DEV infrastructure, and existing implementation are the source of truth.

This milestone may run unattended.

You are authorized to:

- reconstruct context
- design the validation approach
- execute tests and controlled validations
- fix genuine defects found during validation
- update tests and documentation
- produce evidence
- review the completed work
- commit the milestone

Do NOT stop for routine test-design or implementation questions.

Use engineering judgment and proceed with the smallest solution consistent with the accepted architecture.

Stop only if:

- the previous milestone is incomplete
- a genuine architecture conflict is discovered
- a destructive shared-infrastructure operation would be required
- a required credential is unavailable and blocks meaningful validation
- a fix would require a material product/architecture change
- a database migration with unexpected/destructive consequences becomes necessary

Do not invent new product requirements.

Do not expand scope merely because a more production-complete solution is possible.

# Sequential branch rule

This milestone must continue from the CURRENT completed milestone.

Do NOT branch from `main`.

Before changing anything:

1. inspect the current Git branch
2. inspect Git status
3. verify the working tree is clean except for explicitly known ignored/local files
4. verify recent history contains the completed milestone:

   `feat: add operational admin view`

5. verify the previous Email and n8n runtime milestones are also present in the current history
6. do NOT checkout `main`
7. create and checkout:

   `test/end-to-end-validation`

from the current HEAD

If the expected previous milestone is missing or incomplete, STOP rather than branching from an older state.

Do not merge branches.

At completion, leave `test/end-to-end-validation` checked out so the final documentation milestone can branch directly from it.

# Prompt-history requirement

This is a standalone material task.

Store this exact final English prompt verbatim under `prompts/` according to `AGENTS.md`.

Use the next normal sequential prompt-history number.

Do not combine it with earlier or future milestone prompts.

Do not retrospectively modify historical prompts.

Never store secrets in prompt history.

# Phase 1 — Reconstruct complete current context

Before designing the validation plan or modifying anything, inspect the repository thoroughly.

Read at minimum:

1. root `AGENTS.md`
2. root `README.md`
3. all relevant documents under `docs/`
4. `docs/01-plan.md`
5. `docs/02-assumptions-and-open-questions.md`
6. `docs/03-scope.md`
7. `docs/04-architecture.md`
8. `docs/05-validation-strategy.md`
9. `docs/decision-log.md`
10. `docs/ai-review-log.md`
11. all ADRs under `docs/adr/`
12. current milestone specifications/plans
13. prompt history and prompt index
14. relevant evidence under `evidence/`
15. current ASP.NET Core/Razor Pages implementation
16. current ownership implementation
17. current alert-management implementation
18. current admin implementation
19. current OpenTelemetry setup
20. current EF Core entities/migrations
21. actual DEV PostgreSQL schema
22. current n8n workflow export under `n8n/workflows/`
23. actual authoritative Sonrisa workflow in DEV n8n
24. archived/historical Sonrisa workflows where relevant
25. recent Git history from skeleton through current HEAD

Use:

- Rider tooling where useful
- PostgreSQL MCP for safe database inspection
- n8n MCP for workflow/execution inspection
- normal `dotnet`/Node/project tooling where appropriate

Inspect actual implementation rather than assuming documentation is perfectly current.

Do not mutate shared infrastructure during context reconstruction.

# Accepted architecture — do not redesign it

The following decisions are established and must be treated as constraints.

## ASP.NET Core / Razor Pages

Owns:

- alert configuration
- owner-scoped management UI
- product configuration validation
- read-only product/admin visibility
- application logs/traces through OpenTelemetry

The MVP is:

- single-user in the normal management UI
- ownership-aware in persistence
- intentionally without authentication

Do NOT introduce authentication during validation.

## PostgreSQL

Owns product configuration only.

Expected product state includes approximately:

- owners/users
- alerts
- condition configuration
- Slack destination configuration
- Email destination configuration

PostgreSQL does NOT own:

- event-processing queues
- notification-delivery queues
- retry state
- dead-letter state
- runtime workflow state
- runtime event history

Do not reintroduce any of these concepts.

## n8n

Owns runtime processing:

- external source retrieval
- canonical normalization
- technical duplicate filtering
- alert configuration loading
- condition evaluation
- channel routing
- Slack transport
- Email transport
- bounded retry behavior
- workflow execution/runtime technical state

There should be one authoritative primary Sonrisa runtime workflow.

Do not recreate the earlier:

- ingestion workflow
- pending evaluator workflow
- delivery workflow

architecture.

## Delivery semantics

Notification delivery is intentionally best-effort.

Accepted limitations include:

- bounded n8n retries
- exhausted retries may lose the notification
- ambiguous external failures may theoretically cause duplicate or lost notifications
- events already considered seen are not required to be retried because delivery failed
- no exactly-once delivery guarantee
- no guaranteed-delivery requirement
- no durable delivery recovery

Do NOT "fix" these limitations by adding persistence or queues.

## Deduplication

Event deduplication is n8n-owned technical state.

The intended identity is based on approximately:

`source + ":" + externalId`

The n8n duplicate history is bounded and is not a permanent event ledger.

This limitation is accepted.

USGS identifier changes may cause the same physical earthquake to be processed again.

Alias resolution is intentionally deferred.

Do not build provider-identity reconciliation.

# Milestone objective

This milestone does NOT add product features.

Its goal is to validate the completed MVP systematically and produce trustworthy evidence that it behaves according to the accepted design.

The central question is:

> Does the implemented system actually work according to the documented architecture, scope, and trade-offs?

Focus on:

- correctness
- integration boundaries
- owner isolation
- n8n runtime behavior
- deduplication
- condition matching
- Slack/Email routing
- failure isolation
- retry behavior
- observability
- secrets/security hygiene
- reproducibility
- regression

Do not optimize for the number of tests.

Prefer high-value validation.

# No feature expansion

Do NOT add:

- another external event source
- another notification channel
- authentication
- authorization
- user management
- complex rule expressions
- multiple conditions per alert
- durable delivery state
- runtime event persistence
- custom queues
- dead-letter processing
- stronger notification guarantees
- additional infrastructure
- production scheduling activation
- a new observability backend
- broad UI redesign

If validation reveals a limitation that is outside accepted scope:

document it.

Do not implement it.

# Validation approach

Use a mix of:

- automated tests
- integration tests
- deterministic workflow fixtures
- controlled real external integration checks
- database inspection
- n8n execution inspection
- manual browser/application checks where they add value

Clearly distinguish which type of validation produced each result.

Do not claim validation that was not actually performed.

# 1. Clean build and application startup

Validate at minimum:

- Rider recognizes the solution
- `dotnet build` succeeds
- no unexpected warnings/errors
- application starts
- application health/liveness endpoint works
- PostgreSQL readiness works with correct configuration
- missing/invalid DB readiness behaves as designed where safely testable
- application restarts cleanly

Do not alter infrastructure merely to create test failures.

# 2. OpenTelemetry / logging validation

Validate the existing observability foundation proportionately.

Check:

- normal application logging works
- ASP.NET request traces are produced when configured
- PostgreSQL/Npgsql tracing behaves according to existing implementation
- logs and traces can correlate requests where expected
- application starts when no OTLP exporter endpoint is configured
- unavailable OTLP backend does not break application startup
- unavailable telemetry does not fail liveness/readiness
- secrets/connection strings are not emitted

Do NOT:

- deploy a new collector
- deploy Grafana/Loki/Tempo
- modify global n8n OTEL configuration
- create custom telemetry infrastructure

For n8n, use available execution history and existing telemetry.

# 3. Alert management validation

Validate the actual management UI.

At minimum:

## Alert list

- current MVP owner sees their alerts
- alerts from another owner are not visible

## Create

- valid alert can be created
- persisted values match expected configuration contract

## Edit

- valid changes persist
- condition configuration remains deterministic

## Enable/disable

- state changes correctly
- runtime later respects disabled state

## Validation

Verify rejection of invalid configuration such as applicable:

- missing required name
- unsupported/malformed condition value
- invalid numeric threshold
- invalid/missing required destination configuration

Do not extend the validation model beyond the current MVP.

# 4. Ownership boundary validation

This is important.

The normal management UI is single-user MVP but ownership-aware.

Prove that:

- `/alerts` queries only the configured MVP owner
- direct route manipulation cannot edit another owner's alert
- form tampering cannot change ownership
- OwnerId is not trusted from posted user input
- another owner's alert remains inaccessible through normal management pages

The runtime must behave differently:

- n8n evaluates enabled configuration across owners
- n8n does NOT filter using `MvpOwner:Id`

Validate both boundaries.

Do not implement authentication.

# 5. Admin view validation

Validate the completed read-only admin pages.

At minimum:

## `/admin`

Verify:

- owner/user count
- total alert count
- enabled alert count
- disabled alert count
- Slack configured count
- Email configured count

## `/admin/users`

Verify:

- multiple owners appear
- alert count per owner is correct
- enabled-alert count is correct
- Slack configured indicator is correct
- Email configured indicator is correct
- unnecessary sensitive destination values are not exposed

## `/admin/alerts`

Verify:

- alerts across owners are visible
- owner association is correct
- event type is correct
- enabled/disabled state is correct
- condition rendering is correct
- notification-channel indication is correct

## Boundary regression

Verify:

- admin pages intentionally see cross-owner configuration
- normal alert pages remain owner-scoped

Do not add admin functionality during this milestone.

# 6. PostgreSQL schema validation

Inspect the actual DEV schema and EF migration history.

Verify:

- current EF model matches the actual schema
- only justified product configuration tables remain
- rejected runtime tables are absent
- obsolete `source_events` / `notification_deliveries` state is gone if the accepted simplification removed them
- ownership relationships are correct
- alert configuration relationships are correct
- no credentials are stored in product data
- migration history is coherent
- no accidental runtime queue/state schema remains

Do not create a migration unless a genuine schema defect is discovered.

If a migration unexpectedly becomes necessary:

use the smallest safe forward correction.

Do not rewrite existing migration history.

# 7. Authoritative n8n workflow validation

Inspect the actual DEV n8n environment.

Verify:

- exactly one authoritative current Sonrisa processing workflow exists
- it is the expected simplified workflow
- historical/rejected workflows are archived/inactive
- no competing active Sonrisa runtime architecture exists
- the authoritative workflow remains inactive unless an accepted later decision says otherwise
- repository export corresponds to the remote workflow
- no secret material exists in the exported artifact

Do not activate unattended scheduling during validation.

# 8. Real USGS source validation

Validate the actual source integration.

Check:

- USGS request succeeds
- expected feed structure is received
- event IDs are extracted correctly
- timestamp conversion is correct
- title handling is correct
- numeric magnitude handling is correct
- malformed or missing magnitude does not produce a false match
- canonical normalized structure matches accepted design

Do not implement USGS alias reconciliation.

Document current ID limitations accurately.

# 9. Deterministic fixture validation

Use the existing deterministic fixture/manual test entry.

The fixture must enter the SAME downstream pipeline as real normalized events.

Do not create a separate matching implementation.

Validate at least:

- magnitude below threshold
- magnitude equal to threshold
- magnitude above threshold
- malformed magnitude
- disabled alert
- unsupported configuration
- distinct owners
- Slack-only destination
- Email-only destination
- both destinations

Use unique synthetic external IDs where tests must bypass previous dedup history.

Ensure synthetic input cannot accidentally become part of future unattended live ingestion.

# 10. Deduplication validation

Validate the actual n8n-native deduplication behavior.

At minimum:

## Same input batch

Two identical canonical keys in one batch.

Expected:
- only one proceeds downstream

## Later execution

Run the same canonical key again.

Expected:
- it is filtered as previously seen

## New external ID

Expected:
- it proceeds

## Downstream failure

An event becomes seen and transport later fails.

Expected:
- a later replay may still be removed by deduplication
- this is ACCEPTED behavior

Verify the documented bounded-history behavior matches the actual n8n version/configuration.

Do not replace native deduplication with PostgreSQL persistence because it is bounded.

Do not claim permanent deduplication.

# 11. Condition matching validation

Validate only the accepted MVP matching semantics.

Current expected rule:

- event type: earthquake
- field: magnitude
- operator: gte
- value type: number
- one condition per alert

At minimum:

`magnitude < threshold`
→ no notification

`magnitude == threshold`
→ match

`magnitude > threshold`
→ match

Malformed numeric source value
→ no false match

Malformed/unsupported persisted condition
→ no false match / safe diagnostic

Disabled alert
→ no match

Do not generalize the rules engine.

# 12. Slack validation

Validate the existing Slack branch without unnecessary real sends.

Use existing successful external-send evidence if:

- implementation has not materially changed
- credential/destination contract remains unchanged
- evidence is trustworthy

Otherwise perform the minimum controlled send required.

Validate:

- correct branch selection
- correct persisted destination
- credential remains n8n-owned
- no token is exposed
- bounded retry configuration remains correct
- one failed Slack notification does not stop unrelated notifications

Do not spam the test channel.

# 13. Email validation

Validate the existing Email branch.

Use SMTP4DEV as the controlled SMTP transport environment where that is the established implementation.

Verify:

- Email-only configuration routes correctly
- recipient comes from persisted product configuration
- SMTP credential remains n8n-owned
- plain-text message is correct
- synthetic messages are visibly identified
- SMTP4DEV captures one controlled message

Document accurately:

SMTP4DEV proves SMTP submission/capture.

It does NOT prove delivery to an external internet mailbox.

# 14. Slack + Email fan-out validation

This is an important architecture test.

For one matching configured alert/profile with both destinations:

Event
    ↓
deduplicate once
    ↓
match once
    ↓
fan out
    ├── Slack
    └── Email

Verify:

- matching is not duplicated independently per transport
- one source event is deduplicated before fan-out
- both channel items are produced correctly
- Slack and Email remain transport-specific branches only

Do not create transport-specific source deduplication.

# 15. Failure isolation

Explicitly validate that failure of one notification item does not abort unrelated notifications.

Validate equivalent scenarios such as:

Email A
→ retries exhausted
→ discarded

Slack B
→ still processed

Email C
→ still processed

And where safely practical:

Slack A
→ retries exhausted
→ discarded

Email B
→ still processed

Use safe controlled/mock failures where possible.

Do not intentionally damage shared credentials/infrastructure.

# 16. Retry behavior

Validate actual n8n behavior rather than merely inspecting configuration.

Verify where safely possible:

- retry occurs
- approximately five total attempts occur according to accepted configuration
- delay behavior is reasonable
- exhausted item is visibly failed/discarded
- processing continues with later notification items
- no PostgreSQL retry state is created
- no delivery queue exists

If mock transport failure is used:

clearly label it as mocked retry validation.

Do not imply actual provider failure was tested.

# 17. Unsupported channel behavior

If an unsupported/future channel can be represented in fixture/runtime data:

verify:

- it does not route to Slack
- it does not route to Email
- it is not reported as delivered
- it produces visible safe diagnostic behavior
- other valid notifications continue

Do not add another real channel just to test this.

# 18. Best-effort delivery semantics

Verify implementation and documentation remain consistent with accepted semantics:

- notification may be lost after retry exhaustion
- event may already be deduplicated and therefore not reprocessed later
- ambiguous provider result may theoretically create lost/duplicate delivery
- no durable recovery follows
- no exactly-once guarantee exists

Do not treat these as defects.

They are intentional MVP trade-offs.

# 19. n8n workflow export parity

Verify the repository workflow artifact represents the tested remote workflow.

Check:

- graph structure
- meaningful node configuration
- channel branches
- deduplication nodes
- retry/error behavior
- workflow inactivity
- credential references sanitized according to repository convention
- no secret values
- no pinned sensitive execution data

Run existing workflow artifact tests where available.

Do not manually alter the export into something different from the tested workflow without a reproducible normalization process.

# 20. Security/secrets review

Perform a focused repository and workflow hygiene review.

Search for accidental:

- database passwords
- connection strings containing secrets
- Slack tokens
- SMTP passwords
- OAuth secrets
- API keys
- `.env` content
- private keys
- secrets in prompt history
- secrets in evidence
- secrets in workflow exports
- secrets in appsettings files
- secrets in logs/screenshots

Also check that:

- external source text remains treated as data
- SQL queries are parameterized where user/external values are involved
- HTML/output encoding is appropriate where relevant
- Email content does not expose unnecessary internal/debug IDs
- admin pages do not expose secrets

Do not turn this into a broad penetration test.

# 21. Clean checkout / reproducibility review

Validate the documented developer path proportionately.

Check:

- root solution structure
- Rider load/build
- .NET SDK instructions
- Tailwind build/reproduction
- application startup
- safe DB configuration approach
- distinction between local app and shared DEV services
- n8n workflow import/rebinding instructions where present

Do not provision duplicate local PostgreSQL/n8n services.

# 22. Regression suite

Run the complete applicable automated test suites.

Expected categories may include:

- .NET unit tests
- .NET integration tests where safely configured
- n8n/Node workflow tests
- documentation/link checks
- workflow sanitizer/export checks

Report skipped tests honestly.

Do not state "all tests passed" if some were skipped without explaining why.

# Defect handling policy

If validation exposes a real defect in ACCEPTED functionality:

fix it.

Examples:

- cross-owner data leak
- incorrect threshold comparison
- broken channel fan-out
- Email failure stopping Slack
- secret leak
- invalid destination routing
- workflow export mismatch
- admin count bug

Keep fixes minimal.

Add regression tests where useful.

Do not use defects as an excuse for architecture expansion.

If the proposed fix starts introducing:

- runtime DB state
- queues
- workers
- new top-level workflows
- new infrastructure
- new product scope

STOP and reassess.

Prefer the accepted simple architecture.

# Known limitation policy

If validation confirms a known limitation rather than a defect:

document it and leave it.

Examples:

- bounded n8n dedup history
- USGS external ID changes
- lost notification after exhausted retry
- ambiguous transport outcome
- no authentication
- workflow inactive/manual in DEV
- SMTP4DEV not proving internet inbox delivery

Do not "solve" these limitations in this milestone.

# Evidence requirements

Produce trustworthy evidence according to repository conventions.

Record:

- actual commands executed
- test results
- relevant n8n execution IDs
- deterministic fixture results
- real-source validation
- transport validation
- retry/failure evidence
- database/schema inspection
- security checks
- defects discovered
- corrections made
- tests skipped
- things not validated

Use screenshots only when they add real evidence.

Do not include secrets.

Do not manufacture evidence.

# AI review requirements

Critically assess generated/tested work.

Record meaningful findings such as:

- generated test assumption contradicted runtime behavior
- retry configuration behaved differently from expected
- channel failure initially stopped later items
- owner query leaked another owner's data
- workflow export contained unsafe environment detail
- documentation overstated reliability
- implementation incorrectly treated SMTP4DEV as external mailbox delivery

Do not invent findings to make the review log look active.

# Documentation updates

Update only documentation affected by actual validation.

Examples:

- confirmed behavior
- corrected inaccurate claims
- newly confirmed limitations
- actual validation coverage
- final milestone status

Do not perform the final documentation/retrospective rewrite here.

That is the next milestone.

# Self-approval rule

This milestone may run unattended.

Do not stop for routine choices such as:

- exact test class names
- fixture organization
- evidence filename
- mock mechanism
- assertion style

Prefer:

1. existing test patterns
2. smallest useful test
3. no new architecture
4. real evidence
5. deterministic validation before external sends

Stop only for:

- destructive shared-infrastructure action
- missing credential that blocks a required real integration check
- fundamental architecture conflict
- unexpectedly necessary destructive schema migration
- material failure of the previous milestone

# Final self-review

Before committing, explicitly answer:

1. Does the management UI behave correctly?
2. Is normal owner scoping intact?
3. Does admin intentionally see cross-owner configuration?
4. Does PostgreSQL contain only justified product configuration state?
5. Is there one authoritative n8n workflow?
6. Does real USGS ingestion work?
7. Does normalization work?
8. Does n8n deduplication behave as documented?
9. Does threshold matching behave correctly?
10. Does disabled/malformed configuration fail safely?
11. Does Slack work?
12. Does Email work?
13. Can one event fan out to both?
14. Does one transport failure leave unrelated notifications processing?
15. Are retries bounded and n8n-owned?
16. Are accepted delivery limitations documented honestly?
17. Are workflow exports free of secrets?
18. Are application telemetry and health still valid?
19. Are there any secret leaks?
20. Are all meaningful defects fixed without expanding architecture?
21. Is anything being claimed that was not actually tested?

Correct real defects before committing.

# Milestone commit

After validation, defect correction, evidence, and review are complete, create:

`test: validate end-to-end behavior and failure scenarios`

Only include files relevant to this milestone.

Do not amend previous milestone commits.

Do not rewrite history.

Leave branch:

`test/end-to-end-validation`

checked out after commit.

Do not merge it.

# Final report

At completion, report concisely:

1. branch created
2. repository/context reviewed
3. build result
4. automated test result, including skipped tests
5. management UI validation
6. ownership validation
7. admin validation
8. PostgreSQL/schema validation
9. USGS validation
10. deduplication validation
11. condition-matching validation
12. Slack validation
13. Email/SMTP4DEV validation
14. Slack+Email fan-out validation
15. retry/failure-isolation validation
16. OpenTelemetry/logging validation
17. workflow export parity
18. security/secrets review
19. reproducibility checks
20. defects found and fixed
21. known limitations intentionally left unresolved
22. evidence created/updated
23. documentation corrections
24. commit hash
25. anything requiring human review before the final documentation milestone

Do not continue into the final documentation/retrospective milestone.
