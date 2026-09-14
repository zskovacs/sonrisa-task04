Date: 2026-09-14

Purpose: Define and critically review the MVP product scope, architecture, reliability boundaries, and delivery plan.

$superpowers:brainstorming $brainstorming

We are moving from repository initialization into the architecture and product-design milestone.

Treat this as a real product engineering task.

Do NOT implement application code in this task.

Your goal is to reduce the current ambiguity into a small, coherent MVP architecture that can be implemented incrementally and validated end to end.

Before doing any design work:

1. inspect the repository
2. read the root `AGENTS.md`
3. read all existing planning, scope, assumption, architecture, ADR, and decision-log documents
4. inspect the current Git state and recent history
5. preserve all unrelated existing work
6. follow all repository documentation, prompt-history, validation, and commit rules

This prompt is material repository work and MUST be recorded in prompt history according to `AGENTS.md`.

Store the exact final English prompt verbatim.

Do not paraphrase or retroactively improve the stored prompt after execution.

# Product context

The product requirement is intentionally vague:

"We want users to be able to set up alerts so they get notified when something important happens in the world — like breaking news, market movements, natural disasters, that kind of thing. Should work for both email and Slack. Make it flexible enough that we can add more channels later. We need an admin view too."

There is no additional agreed specification.

There are currently no agreed:

- wireframes
- event schemas
- alert schemas
- external event sources
- scale assumptions
- authentication model
- exact admin requirements
- definition of "important"
- notification reliability guarantees

Do not invent product requirements merely to simplify implementation.

Where information is unavailable:

- identify the ambiguity
- make the smallest reasonable assumption if progress requires one
- clearly label it as an assumption
- record important unresolved questions

# Existing architectural decision

The following decision is already accepted:

n8n will be the primary workflow orchestration and external-integration layer.

The intended responsibility boundary is approximately:

n8n:
- scheduling
- polling and webhook orchestration
- external HTTP/RSS integrations
- source-specific workflow coordination
- email and Slack transport
- retries/orchestration where appropriate
- credential handling for external notification systems
- execution visibility

Custom application:
- durable product state
- canonical event validation
- event deduplication
- alert definitions
- deterministic alert matching
- notification intent / delivery records
- product-facing APIs where required
- management/admin functionality

Persistent database:
- authoritative durable product state

Do not redesign this decision unless you discover a serious architectural contradiction.

You should, however, challenge the exact boundaries and improve them where necessary.

ADR-001 already documents the n8n decision. Do not duplicate it.

# Core design principle

Optimize aggressively for the smallest credible product.

The implementation is time-constrained.

Prefer:

- one complete end-to-end vertical slice
- explicit boundaries
- deterministic behavior
- good failure semantics
- testability
- inspectability
- simple deployment
- low operational complexity

over:

- broad feature coverage
- speculative scalability
- infrastructure sophistication
- framework demonstrations
- unnecessary abstraction

Do not introduce technology merely because it is common in larger systems.

In particular, there is currently no demonstrated requirement for:

- microservices
- Kubernetes
- RabbitMQ
- Kafka or another streaming platform
- distributed caches
- a custom workflow engine
- a sophisticated rules engine
- a full production identity platform
- CQRS infrastructure
- event sourcing

If you believe any such component is needed, the burden of proof is on the design.

# Use brainstorming critically

Use the brainstorming process to challenge the problem before documenting a solution.

Do not treat existing ideas as automatically correct.

Explicitly reason about:

- what the user actually needs to accomplish
- what the administrator actually needs to see
- what "important" should mean in an MVP
- which responsibilities belong in n8n versus custom code
- which parts genuinely require an API
- what must be durable
- what must be idempotent
- what can safely be deferred
- what the smallest useful UI could be
- what the first vertical slice should demonstrate
- what failure cases materially affect the architecture

Prefer removing unnecessary components over making them more sophisticated.

# Product hypothesis to validate

The current leading hypothesis is:

For the MVP, "important" should not be determined by an LLM or a global importance score.

Instead:

An event is important to a user when it matches an enabled alert rule explicitly configured by that user.

This gives us deterministic and testable semantics.

Examples might include:

- earthquake magnitude greater than or equal to a configured threshold
- a news event containing a configured keyword
- a market event exceeding a configured movement threshold

Treat this as a strong proposed direction, not an immutable requirement.

Challenge it during brainstorming.

If you reject or materially change it, explain why.

Do not introduce runtime AI/LLM classification without a strong product reason.

# UI architecture decision

The UI technology is intentionally unresolved.

Evaluate at least these three options:

## Option A — ASP.NET Core Razor Pages / server-rendered UI

Potential advantages:

- one application/runtime
- minimal client-side application infrastructure
- straightforward forms and validation
- direct access to application services
- likely sufficient for simple alert CRUD and operational admin pages

Potential disadvantages:

- less suitable if interaction complexity becomes client-heavy
- some dynamic alert-rule editing may require JavaScript

## Option B — Lightweight browser UI using native JavaScript or TypeScript

Potential advantages:

- small frontend surface
- explicit HTTP API boundary
- little framework coupling

Potential disadvantages:

- risk of manually recreating form, state, routing, and validation infrastructure
- TypeScript introduces build tooling if used properly
- may become less maintainable than expected

## Option C — Angular

Potential advantages:

- strong structure and typing
- mature forms and HTTP tooling
- appropriate if client-side interaction becomes substantial
- clear independent API boundary

Potential disadvantages:

- separate frontend application and toolchain
- more scaffolding and deployment complexity
- likely unnecessary for a small number of CRUD/admin screens
- larger implementation surface for the current requirements

Do not select a UI based on developer familiarity alone.

Base the decision on actual MVP interaction requirements.

There is currently a bias toward the smallest maintainable solution.

If Razor Pages are sufficient, prefer them over creating a SPA without a demonstrated reason.

If you choose Razor Pages, explicitly document that this does NOT eliminate HTTP APIs needed by n8n or other external integrations.

The internal management UI may call application services directly instead of making HTTP calls back into the same application.

Do not implement any UI in this task.

# First vertical slice

Evaluate whether the first implementation slice should use earthquake events as the initial source.

The current hypothesis is that an earthquake feed is a useful first slice because it demonstrates:

- external ingestion
- structured source data
- canonical event normalization
- external-event deduplication
- deterministic numeric alert matching
- durable notification creation
- Slack delivery
- observable end-to-end behavior

A candidate flow is:

External earthquake source
→ n8n ingestion workflow
→ canonical event boundary
→ application validation
→ event deduplication
→ alert matching
→ durable pending notification
→ n8n delivery workflow
→ Slack

Challenge this choice.

Compare it briefly against starting with:

- RSS/news
- market data

Select the source that gives the best first end-to-end validation with the least accidental complexity.

Do NOT choose a specific external provider/API in this task unless that decision is necessary for architecture.

Provider selection can remain a later integration task.

# Canonical event boundary

Design the minimum useful canonical event concept.

The goal is to prevent source-specific schemas from leaking through the entire application.

Consider concepts such as:

- source
- external event identifier
- category
- event type
- occurrence time
- title/summary
- source-specific attributes
- ingestion time

Do not over-design the schema.

Specifically evaluate the trade-off between:

- strongly typed event subtypes
- a common envelope plus flexible attributes
- another simpler representation

The design must support source extensibility without turning the core application into an untyped JSON rule engine.

Record the selected direction and its trade-offs.

Do not finalize a database schema in unnecessary detail.

# Alert-rule design

Define only the minimum semantics necessary for the first MVP.

The current idea is something approximately equivalent to:

Alert
- owner
- name
- enabled
- target event category/type
- one or more deterministic conditions
- one or more notification channels

Possible condition semantics may include a deliberately small operator set such as:

- equals
- not equals
- greater than
- greater than or equal
- less than
- less than or equal
- contains

However, do not blindly accept this structure.

Challenge whether the first MVP needs:

- multiple conditions
- AND/OR grouping
- arbitrary nested expressions
- a rules DSL
- JSONPath
- source-specific rule types

Prefer the smallest rule model that supports the selected first vertical slice and plausible extension to a second source.

Complex nested boolean rule engines should be considered out of scope unless clearly justified.

# Notification extensibility

The product explicitly requires email and Slack and should make future channels possible.

Design a channel boundary that avoids coupling alert evaluation directly to Slack or email implementation details.

The application should preferably create durable notification intent before external delivery.

Reason about a lifecycle such as:

Pending
→ Processing
→ Sent

with failure/retry states where necessary.

The exact state machine should remain as small as possible.

Consider:

- idempotency
- duplicate prevention
- temporary failures
- permanent failures
- retries
- observability
- future notification channels

Do not build a generic messaging framework.

# Reliability and idempotency

The architecture must explicitly address at least:

## Source-event deduplication

Polling the same external source repeatedly must not repeatedly process the same source event as new.

## Notification deduplication

The same event/alert/channel combination should not accidentally generate duplicate user notifications because of workflow retries.

## Durable notification intent

A notification should not disappear merely because Slack or email is temporarily unavailable after matching occurs.

## Retry behavior

Transient delivery failures should have a recoverable path.

Do not assume exactly-once delivery is realistically achievable.

Document the practical delivery semantics being targeted.

Keep the solution proportional to the MVP.

# Admin view

"Admin view" is underspecified.

Do not assume it means a full user-management back office.

Evaluate a minimal operational admin view focused on system visibility.

Potentially useful information includes:

- recently ingested events
- alert matches
- notification deliveries
- failed deliveries
- retry state
- basic source/workflow health where available

Decide what the MVP admin page should contain.

Clearly distinguish the chosen scope from future administration capabilities.

# Authentication and authorization

Do not implement identity in this milestone.

Decide whether production-grade authentication should be part of the initial MVP implementation or intentionally deferred.

Consider the cost/value of:

- ASP.NET Core Identity
- external identity provider
- seeded/demo user and admin identities
- no authentication in a strictly local demo environment

The architecture should not make future ownership/authorization impossible.

However, do not spend significant implementation scope on identity unless the product requirement demonstrates the need.

Document the decision or, if necessary, leave it explicitly unresolved.

# Persistence

Assume durable relational persistence is likely appropriate.

The existing direction favors PostgreSQL, but validate this against actual requirements.

Do not create migrations or schemas yet.

Identify only the core durable concepts the system will likely need.

Potential concepts include:

- users or ownership identities
- alerts
- alert conditions
- notification channel configuration
- canonical events
- notification deliveries

Avoid speculative tables.

# API boundary

Determine which HTTP APIs are genuinely required.

At minimum, n8n will probably need a supported boundary for submitting normalized events and/or interacting with pending notification deliveries.

Do not assume the UI must communicate through HTTP if a server-rendered UI shares the same application runtime.

Evaluate clean boundaries such as:

n8n
→ HTTP API
→ application services
→ persistence/domain

and, if Razor Pages are selected:

Razor Pages
→ application services
→ persistence/domain

Do not make the application call its own HTTP API merely for architectural symmetry.

At the same time, keep external integration boundaries explicit and testable.

# Deployment/runtime shape

Design only the minimum expected local runtime topology.

The eventual implementation may reasonably contain:

- n8n
- ASP.NET Core application
- PostgreSQL

Possibly nothing else initially.

If email/Slack or external services require credentials, they should remain external integrations rather than additional infrastructure unless justified.

Do not create Docker configuration in this task.

Do not introduce production-cloud architecture yet.

# Demo and deterministic testing

The application must eventually be demonstrable without depending on a rare real-world event occurring at exactly the right time.

Design a safe deterministic demo/test path.

For example, a test/synthetic event may enter through the same canonical application boundary as production events.

Avoid creating a separate fake implementation path that bypasses real matching and delivery logic.

Clarify how synthetic events will be distinguishable from actual source events if necessary.

Do not implement the demo path yet.

# Required outputs

Update the existing repository documentation rather than creating duplicate documents unnecessarily.

At minimum, complete or materially improve:

- `docs/01-plan.md`
- `docs/02-assumptions-and-open-questions.md`
- `docs/03-scope.md`
- `docs/04-architecture.md`
- `docs/05-validation-strategy.md`
- `docs/decision-log.md`

Also create or update appropriate ADRs for decisions that are genuinely architectural.

Potential ADR candidates include:

- UI architecture / server-rendered vs SPA
- definition of user-relevant importance
- canonical event boundary
- durable notification intent

Do NOT create an ADR for every minor choice.

Group tightly related decisions where that creates a clearer architectural record.

Do not modify ADR-001 except to correct an actual inconsistency.

# Architecture documentation

`docs/04-architecture.md` should become a useful high-level system design.

Include at least:

- responsibilities of each major component
- component boundaries
- data/control flow
- n8n versus application responsibilities
- persistence responsibilities
- external integration boundaries
- reliability/idempotency approach
- admin/management surface
- unresolved architecture questions
- a Mermaid component/data-flow diagram

Prefer a diagram roughly at this level of abstraction:

External Sources
      |
      v
     n8n
 Ingestion / Normalization
      |
      | HTTP
      v
ASP.NET Core Application
      |
      +--> Event validation / deduplication
      |
      +--> Alert evaluation
      |
      +--> Durable delivery intent
      |
      v
 PostgreSQL
      ^
      |
     n8n
Delivery Orchestration
   |         |
   v         v
 Email     Slack

Management/Admin UI
      |
Application Services

Do not copy this blindly if brainstorming produces a clearer design.

# Validation strategy

Update `docs/05-validation-strategy.md` so the planned architecture can later be tested rather than merely demonstrated.

Include future validation for:

- rule-matching correctness
- source-event deduplication
- notification deduplication
- API contract validation
- malformed input
- temporary external-delivery failure
- retries
- idempotent retry behavior
- unavailable source
- database persistence boundaries
- workflow export validity
- secrets/configuration safety
- deterministic end-to-end demo
- manual admin visibility check

Distinguish:

- unit tests
- integration tests
- workflow-level tests/checks
- manual end-to-end validation

Do not claim any of these tests exist yet.

# Scope output

By the end of this task, `docs/03-scope.md` should define an implementation target small enough that another engineer could begin the first application milestone without reopening fundamental product questions.

The proposed MVP should likely contain only:

- one initial real event-source type
- deterministic user-defined alerts
- durable event storage/deduplication
- durable notification intent
- Slack
- email
- an extensible channel boundary
- minimal alert management
- minimal operational admin visibility
- deterministic demo/test event path

A second source type may be either:

- part of MVP if it provides high architectural validation at low cost
- a stretch goal if the first source already proves the architecture

Make that decision explicitly.

Potentially defer:

- production-grade authentication
- complex multi-tenant Slack OAuth
- sophisticated rule expressions
- geospatial rules
- runtime LLM classification
- SMS/push
- microservices
- message brokers
- high-availability deployment
- high-volume streaming architecture
- extensive user administration

Do not defer something that is actually necessary to satisfy the core product behavior.

# AI-assisted design review

The repository requires evidence of critical evaluation of AI-assisted work.

While brainstorming and designing:

- challenge your own proposals
- identify suggestions that are unnecessarily complex
- identify assumptions that are unsupported by the brief
- reject weak approaches explicitly
- record only meaningful findings in `docs/ai-review-log.md`

Good entries include examples such as:

- rejecting Angular because the interaction model does not justify a SPA
- rejecting direct Slack delivery from the matching transaction because it creates a reliability gap
- rejecting an arbitrary JSON rules engine because it weakens type safety and adds unnecessary complexity
- rejecting a message broker because current throughput/reliability requirements do not justify it

These are examples only.

Do not fabricate these findings if your actual reasoning leads elsewhere.

The log must reflect the work that genuinely happened.

# Milestone

This task corresponds to the second planned milestone:

`docs: record architecture decisions and system design`

Do NOT implement future milestones.

Do NOT:

- scaffold application projects
- generate Razor Pages
- generate Angular
- create JavaScript/TypeScript frontend code
- create EF Core models
- create database migrations
- create Docker Compose
- create n8n workflows
- install dependencies
- implement external APIs
- configure Slack/email
- implement authentication
- create fake screenshots
- create fake test results

At the end of the task, the repository should contain design artifacts only.

# Architecture review before committing

Before committing, review the resulting design critically.

Ask:

1. Can the MVP be explained in a few sentences?
2. Is every major component solving a demonstrated problem?
3. Is n8n being used for orchestration rather than becoming the domain model?
4. Is business logic deterministic and testable?
5. Is the UI choice proportional to actual interaction complexity?
6. Are API boundaries present where external integration needs them, without unnecessary internal HTTP hops?
7. Can duplicate external events be processed safely?
8. Can notification retries happen without accidental duplicate notifications?
9. Can a failed external notification be inspected later?
10. Is the first vertical slice truly end to end?
11. Can the product be demonstrated deterministically?
12. Are facts, assumptions, decisions, and open questions clearly distinguished?
13. Did the design avoid speculative scale/infrastructure requirements?
14. Are deferred items genuinely nonessential to the MVP?
15. Did any AI-generated suggestion survive merely because it sounded plausible rather than because it was validated?

If the answer exposes a design problem, correct the documentation before committing.

# Commit

Once the design is internally consistent and reviewed:

create the milestone commit:

`docs: record architecture decisions and system design`

Only include files relevant to this architecture/design milestone.

Do not include unrelated existing changes.

If repository state or Git configuration prevents a safe commit, do not modify global Git configuration and do not force the commit.

Report the blocker instead.

# Final report

At completion, provide a concise report containing:

1. the final proposed MVP in plain English
2. the selected UI approach and why
3. the selected first vertical slice and why
4. the resulting high-level architecture
5. ADRs created or changed
6. major assumptions made
7. important questions intentionally left open
8. alternatives explicitly rejected and why
9. files changed
10. whether the milestone commit was created
11. commit hash if created
12. any blockers or risks that should be reviewed before implementation

Do not start implementation after the design milestone is complete.