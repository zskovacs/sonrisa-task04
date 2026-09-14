Date: 2026-09-14

Purpose: Design and implement the alert configuration model and management UI milestone.

$superpowers:brainstorming $brainstorming

We are starting the next product milestone:

`feat: add alert configuration model and management UI`

This is a completely new agent session.

Do not assume any conversational context from previous sessions.

The repository is the source of truth for:

- product scope
- architecture
- accepted ADRs
- current implementation
- development topology
- repository rules
- prompt-history rules
- milestone history
- validation expectations

IMPORTANT:

Do not begin brainstorming from this prompt alone.

First reconstruct the current project context from the repository.

Do not implement anything until the brainstorming/design approval gate described below has been completed.

# Phase 1 — Reconstruct the complete current context

Before proposing any design or making implementation changes, inspect the repository thoroughly.

Read at minimum:

1. root `AGENTS.md`
2. root `README.md`
3. all planning and scope documents under `docs/`
4. `docs/01-plan.md`
5. `docs/02-assumptions-and-open-questions.md`
6. `docs/03-scope.md`
7. `docs/04-architecture.md`
8. `docs/05-validation-strategy.md`
9. `docs/decision-log.md`
10. `docs/ai-review-log.md`
11. every ADR under `docs/adr/`
12. relevant review evidence under `evidence/reviews/`
13. existing prompt history under `prompts/`
14. the current solution/project structure
15. current application configuration
16. current health-check implementation
17. current EF Core / PostgreSQL configuration
18. current logging and observability configuration
19. current Git status
20. recent Git history, especially:
    - repository bootstrap milestone
    - architecture/design milestone
    - DEV topology amendment
    - application skeleton milestone

Inspect the actual application code rather than assuming documentation exactly matches implementation.

Use Rider/project tooling where useful to understand:

- the solution
- project structure
- package dependencies
- startup configuration
- Razor Pages setup
- EF Core setup
- build state

Use PostgreSQL MCP only where it safely helps inspect or validate the existing DEV environment.

Use n8n MCP only for contextual inspection if genuinely necessary.

Do not modify n8n during this milestone.

Do not modify remote infrastructure during context reconstruction.

If this prompt conflicts with an accepted repository ADR or architecture decision:

STOP and report the conflict before proceeding.

Do not silently override accepted architecture.

Do not modify unrelated existing work.

# Prompt-history requirement

This is material coding-agent work.

Record the exact final English version of this prompt under `prompts/` according to `AGENTS.md`.

Do not paraphrase, shorten, translate, or retrospectively improve the stored prompt.

Never include secrets in the prompt-history artifact.

# Current known architecture

The repository documentation remains authoritative, but the current implementation/topology is expected to be approximately:

Local developer environment:

- root-level `Sonrisa.sln`
- one thin ASP.NET Core application
- Razor Pages
- application source under `src/Sonrisa.Web/`
- EF Core
- Docker support for the application where already established
- liveness/readiness health checks

Shared DEV infrastructure:

- existing PostgreSQL product database
- existing n8n runtime at `https://n8n.nasgard.io`

n8n internal persistence and administration are outside this repository's scope.

The management application is intentionally thin.

n8n owns runtime workflow processing.

PostgreSQL is the explicit durable integration contract between the management application and n8n.

Do not introduce an HTTP API between n8n and the application merely for architectural symmetry unless an accepted ADR already requires one or a concrete implementation requirement emerges.

Do not redesign existing skeleton/topology decisions without a concrete contradiction.

# Milestone objective

Implement the smallest useful alert-configuration control plane.

At the end of this milestone, a user should be able to manage alert configuration through the Razor Pages application and persist that configuration in PostgreSQL.

n8n should later be able to read this configuration directly from the product database.

This milestone does NOT implement runtime alert execution.

The product runtime should still do nothing with configured alerts until the later n8n workflow milestone.

The application must remain intentionally domain-light.

# Important architecture refinement: user ownership

The persistence model must be prepared for multiple users in the future.

However, the current MVP is intentionally single-user.

This distinction must be explicit.

The original product requirement requires users to configure alerts, but does not specify an authentication mechanism.

Therefore:

- authentication is intentionally NOT part of this MVP
- authorization infrastructure is intentionally NOT part of this MVP
- do not implement ASP.NET Core Identity
- do not generate Identity tables
- do not create login/logout/register pages
- do not implement OAuth/OIDC
- do not create fake authentication merely to simulate production

The persisted alert model must nevertheless include stable ownership.

The current application should operate using one configured/default MVP owner.

Future authentication should be able to resolve an authenticated user to this same ownership concept without requiring the alert schema to be redesigned.

The current single-user mechanism must NOT be presented as a security boundary.

Documentation must explicitly state:

- the persistence model is multi-user-ready
- the current MVP is single-user
- authentication was not part of the requested feature scope
- authentication is deliberately deferred
- the current owner-resolution mechanism is not production authentication

Most importantly:

all alert reads and writes exposed through the application must already be owner-scoped.

Do not rely on:

"there is only one user, therefore filtering by owner is unnecessary."

Future multi-user safety must be designed into the query boundary now.

# Important architecture refinement: observability

The current application has little or no project-level observability beyond basic/default framework logging.

Add a minimal OpenTelemetry-based observability foundation during this milestone.

The reason is architectural consistency:

- future n8n workflows are expected to use OpenTelemetry
- the ASP.NET Core application should use the same vendor-neutral observability approach
- application/database/configuration failures should be diagnosable
- logs and traces should allow useful investigation without introducing a vendor-specific runtime dependency

Required observability scope:

- application logs
- application traces
- trace/log correlation where naturally supported
- incoming ASP.NET Core request tracing
- useful PostgreSQL/EF/Npgsql tracing if stable, supported instrumentation is available and justified
- OTLP-compatible export
- normal console logging for local development where useful

Metrics are NOT required by this milestone.

Do NOT build a full observability platform.

Do NOT introduce:

- Grafana
- Prometheus
- Loki
- Tempo
- local OpenTelemetry Collector
- dashboards
- alerting rules
- custom telemetry backend infrastructure

unless an accepted repository decision already requires one.

Use standard OpenTelemetry conventions and standard configuration/environment variables where practical.

Do not hard-code an observability vendor.

Do not hard-code production OTLP endpoints.

If no OTLP endpoint is configured:

- the application must still start normally
- telemetry export must not become an availability dependency
- liveness/readiness must not fail because telemetry export is unavailable

Never emit secrets such as:

- database passwords
- connection strings
- tokens
- Slack secrets
- SMTP credentials
- private keys

Be cautious about emitting:

- email addresses
- arbitrary condition values
- user identifiers

Do not use high-cardinality or sensitive values as span attributes without a real diagnostic reason.

Use a stable service name so Sonrisa application telemetry can be distinguished from future n8n telemetry.

Do not pretend that the shared PostgreSQL database automatically creates distributed traces between n8n and the ASP.NET application.

There is no synchronous trace-context propagation simply because two systems share durable state.

# Phase 2 — Brainstorm the feature before implementation

After reconstructing repository context, use the brainstorming process to design the smallest coherent alert-configuration model and UI.

Do not scaffold or implement feature code before I approve the resulting design.

Critically challenge the proposed design rather than simply accepting every suggestion in this prompt.

Prefer the smallest design that:

- satisfies the MVP
- provides a clean PostgreSQL contract for n8n
- remains understandable
- is deterministic
- can later support another event source
- avoids speculative architecture

# 1. User ownership model

Evaluate the smallest clean ownership model.

A likely direction is:

`Alert.OwnerId`

using a stable opaque identifier.

Evaluate whether we actually need a persisted `User` table now.

Do not create one merely because multi-user support may exist later.

A stable owner identifier may be sufficient.

If that approach is selected, clearly define:

- identifier type
- how the current MVP owner is configured/resolved
- how application queries are scoped to that owner
- how future authentication can replace the temporary owner provider
- how owner IDs are prevented from being supplied arbitrarily from form input

The current owner should come from application context/configuration, not from an editable form field.

Do not implement fake authorization.

# 2. Alert model

Design the smallest useful alert configuration.

A candidate model is:

Alert
- Id
- OwnerId
- Name
- EventType
- Enabled

AlertCondition
- Id
- AlertId
- Field
- Operator
- Value
- ValueType

AlertChannel
- Id
- AlertId
- ChannelType
- Destination

Treat this as a candidate, not a mandatory schema.

Challenge it.

The persisted model is important because n8n will later query it directly.

Prefer a schema that is:

- understandable outside C#
- stable
- explicit
- easy to query from PostgreSQL/n8n
- deterministic
- extensible enough for a second source type
- small enough for the MVP

Avoid application-only representations that become awkward integration contracts.

# 3. Condition semantics

Keep runtime rule semantics deliberately small.

Current proposed constraint:

all conditions belonging to one alert are combined using logical AND.

For example:

`magnitude >= 5.5`
AND
`region contains "Japan"`

Nested expressions such as:

`(A AND B) OR (C AND D)`

are out of scope.

Do not implement:

- nested groups
- arbitrary expression languages
- scripting
- JavaScript stored in PostgreSQL
- `eval`
- JSONPath rule engines
- a custom DSL
- a general-purpose rule engine

unless brainstorming uncovers an exceptionally strong requirement.

Evaluate a deliberately small operator set, likely:

- equals
- not equals
- greater than
- greater than or equal
- less than
- less than or equal
- contains

Also evaluate value types.

The current expected minimum is:

- string
- number
- boolean

The persisted representation must allow n8n to interpret values deterministically.

Do not depend on guessing whether `"5.5"` is a number or string.

Prefer explicit type information.

# 4. Cross-platform database contract

PostgreSQL is not merely EF Core persistence.

It is the integration contract between:

- Razor Pages / EF Core
- n8n workflows

Design persistence with that in mind.

Explicitly evaluate representations for values such as:

- event type
- condition operator
- value type
- channel type

Prefer stable, readable textual codes over C# enum integer values unless there is a strong reason otherwise.

n8n should not need to understand C# enum ordering.

Avoid clever serialization.

Prefer boring, readable database state.

Review table and column naming from the perspective of direct SQL access from n8n.

Do not add a naming-convention dependency merely because one exists.

# 5. Event type and condition-field UX

The application should remain thin.

Avoid building a source-schema metadata/catalog system.

Brainstorm how the MVP user selects or enters:

- event type
- condition field
- operator
- value type
- value

There is a trade-off:

A free-text field name is simple but typo-prone.

A complete metadata/catalog subsystem is over-engineered.

Find the smallest practical middle ground.

The first runtime vertical slice is expected to be simple, likely based on structured earthquake data.

Do not build configuration for every future source.

# 6. Notification channels

The product eventually requires:

- Slack
- email

This milestone only stores configuration.

Do not send anything yet.

Design channel configuration so future channels can be added without changing alert-evaluation architecture.

A likely model is:

- ChannelType
- Destination

Do not store:

- Slack credentials
- SMTP credentials
- OAuth tokens
- API keys

inside alert configuration.

External transport credentials remain owned by n8n/runtime configuration.

The application stores only destination/configuration needed for delivery.

Evaluate minimal validation for:

- email destination
- Slack destination

Do not overbuild provider-specific configuration.

# 7. CRUD scope

Keep behavior minimal.

Expected minimum:

- list alerts
- create alert
- edit alert
- enable/disable alert

Evaluate whether hard-delete is actually necessary.

Because future event/delivery history may reference alerts, disabling may be preferable to delete for the MVP.

Do not automatically implement delete because CRUD traditionally includes it.

Explicitly justify the chosen scope.

# 8. Razor Pages UI and visual scope

The UI should remain server-rendered using Razor Pages.

Expected surface is approximately:

- `/alerts`
- `/alerts/create`
- `/alerts/{id}/edit`

Visual design is NOT a major objective of this milestone.

Do not spend significant time on:

- product branding
- custom visual design
- design systems
- complex responsive layouts
- animations
- visual effects
- dark mode
- pixel-perfect polish
- custom component libraries

However, the application should still have a minimum professional and usable visual baseline.

The UI should be:

- clean
- readable
- visually consistent
- easy to understand
- usable on standard desktop screens
- reasonably responsive on smaller screens
- accessible through proper labels, semantic HTML, sensible focus states, and understandable validation messages

Tailwind CSS is explicitly approved for this purpose.

Use Tailwind CSS as a lightweight styling utility layer for:

- page layout
- spacing
- typography
- navigation
- forms
- validation messages
- buttons
- tables/lists
- status indicators
- simple cards/panels where useful

Do not introduce a separate JavaScript frontend framework.

Do not introduce:

- Angular
- React
- Vue
- Alpine.js
- Bootstrap
- a Tailwind component framework
- a third-party UI component library

unless a concrete requirement appears that cannot reasonably be handled by Razor Pages, Tailwind CSS, and minimal vanilla JavaScript.

Tailwind must not become an architecture project of its own.

Before selecting the exact Tailwind integration:

- inspect the current project/build structure
- use the current stable and officially supported Tailwind approach
- verify compatibility with the repository's current build environment
- avoid unnecessary frontend tooling

If a small Node/npm-based build step is the simplest officially supported Tailwind integration, that is acceptable.

Keep it minimal.

Do not create a full frontend application or npm-based application architecture merely to compile CSS.

The application must remain fundamentally a Razor Pages application.

Generated CSS/build artifacts should be handled deliberately.

Do not commit:

- `node_modules`
- caches
- temporary build outputs

Only commit generated CSS assets if that is the selected, documented, reproducible build strategy.

A clean checkout must be able to reproduce the required CSS.

A small amount of vanilla JavaScript is acceptable where it materially improves usability, especially for dynamically adding/removing condition rows or channel rows.

Keep JavaScript:

- small
- local
- dependency-free where practical
- readable
- focused only on interaction that server-rendered forms cannot conveniently provide

Do not build a client-side state-management layer.

# UI scope for this milestone

The goal is functional clarity, not visual design.

A reasonable level of polish is:

Alerts list:
- clear page title
- obvious create action
- alert name
- event type
- enabled/disabled status
- edit action

Create/Edit alert:
- clearly grouped general settings
- conditions section
- notification channels section
- validation errors close to relevant fields
- clear Save / Cancel actions

Navigation:
- minimal application navigation
- only links corresponding to actual implemented functionality

Do not create placeholder navigation items for future functionality.

Do not create fake dashboards or product areas merely to make the application look larger.

If the default Razor template contains irrelevant demo content or styling, remove it rather than building around it.

# 9. Validation

Management-side validation is part of the application's responsibility.

At minimum reason about:

- name
- owner
- event type
- condition field
- supported operator
- supported value type
- value parseability for selected type
- at least one condition, if the chosen model requires it
- at least one channel, if appropriate
- channel type
- channel destination

Ensure invalid typed values cannot be persisted in a representation that n8n would later interpret differently.

Do not duplicate runtime alert evaluation inside the web application.

The management application validates configuration structure.

n8n will later evaluate actual events.

# 10. Transaction boundaries

Saving an alert may involve:

- alert
- conditions
- channels

The configuration should become visible atomically.

n8n must not observe a partially updated configuration containing a mixture of old and new child rows.

Use the simplest transaction semantics naturally provided by EF Core/PostgreSQL.

Do not build distributed coordination.

Document the assumption that configuration changes become visible atomically at the relational database transaction boundary.

# 11. Database migration

This milestone introduces the first real product schema.

EF Core remains the schema/migration owner if that is what accepted ADRs specify.

Create the migration only after design approval.

Review generated migration output carefully.

Do not blindly apply schema changes to shared DEV infrastructure.

Before applying a migration to the existing DEV PostgreSQL database:

1. verify the target is the intended Sonrisa product database
2. inspect the generated migration
3. inspect generated SQL where useful
4. confirm only expected product objects are affected
5. ensure no unrelated database/schema objects are modified

If target-database safety is uncertain, stop and ask.

Do not:

- drop unrelated tables
- alter unrelated schemas
- touch n8n internal persistence
- reset the database
- recreate the database
- perform destructive cleanup for convenience

# 12. Database access ownership

Preserve the accepted ownership model.

Expected direction:

Management application:
- read/write alert configuration

n8n:
- later read alert configuration
- later own runtime processing state according to architecture

During this milestone, do not create runtime processing tables unless genuinely required.

In particular, do not yet implement:

- SourceEvent
- NotificationDelivery

unless accepted architecture explicitly puts them in this milestone.

Those should likely be introduced with runtime workflow implementation.

Least-privilege PostgreSQL access remains an architectural goal.

Do not unnecessarily alter shared database roles/permissions during this milestone.

# 13. OpenTelemetry design

During brainstorming, inspect the current application logging and determine the smallest correct OpenTelemetry setup.

Target:

Logs:
- existing `ILogger` remains the application logging API
- logs can be exported using OpenTelemetry/OTLP when configured
- useful console output remains available locally
- trace/span identifiers should naturally correlate logs to requests where supported

Traces:
- incoming ASP.NET Core requests
- useful framework/runtime activity
- PostgreSQL/EF/Npgsql activity if stable instrumentation is available and justified

Use official/stable packages.

Verify compatibility with the repository's current .NET version before selecting packages.

Do not install observability packages merely because their names look plausible.

Prefer standard OpenTelemetry configuration/environment variables where practical.

Follow current official .NET/OpenTelemetry conventions rather than assuming exact package/configuration names from this prompt.

The service identity must make Sonrisa application telemetry distinguishable from n8n telemetry.

Do not invent manual spans unless they provide actual diagnostic value.

Start with automatic/framework instrumentation.

# 14. Observability failure behavior

Observability must not become a product availability dependency.

Expected behavior:

- missing OTLP endpoint does not prevent application startup
- unavailable telemetry backend does not make the application unavailable
- health checks do not fail solely because telemetry export fails
- exporter failures do not create recursive logging storms

Do not put the observability backend into application readiness.

PostgreSQL remains the meaningful readiness dependency.

# 15. Secrets and privacy

Do not log or trace:

- connection strings
- database credentials
- passwords
- authentication tokens
- Slack secrets
- SMTP credentials

Be cautious with:

- email destinations
- owner identifiers
- arbitrary condition values

Do not attach high-cardinality or sensitive values to spans without a clear diagnostic purpose.

# 16. Dependencies

Every new dependency must have a current reason.

Likely justified dependencies may include:

- existing EF Core/PostgreSQL dependencies
- OpenTelemetry SDK/instrumentation/export packages
- minimal Tailwind build tooling

Do not introduce speculative backend libraries such as:

- MediatR
- AutoMapper
- generic repository frameworks
- CQRS libraries
- retry libraries
- messaging libraries
- validation frameworks unless they solve a demonstrated problem better than built-in ASP.NET validation

Do not introduce a frontend application stack.

Tailwind build tooling is explicitly allowed.

Do not use that approval to introduce broader frontend architecture.

# 17. Testing strategy

This milestone introduces real product configuration behavior.

During brainstorming, propose a proportionate validation strategy.

Prioritize behavior protecting the future n8n contract:

- supported operator/value-type persistence
- typed value validation
- owner scoping
- alert create/edit behavior
- enable/disable behavior
- condition/channel replacement/update behavior
- atomic persistence
- invalid configuration rejection
- migration correctness
- PostgreSQL mapping
- Tailwind asset build/reproducibility
- application startup with no OTLP endpoint
- OpenTelemetry configuration behavior

Do not build a huge testing architecture.

Do not use EF InMemory as a substitute for PostgreSQL semantics where relational behavior matters.

If integration testing against shared DEV PostgreSQL could create unsafe/shared state, propose a safer approach.

Do not automatically introduce Testcontainers/local PostgreSQL without first assessing whether the extra complexity is justified.

# 18. Scope exclusions

This milestone does NOT implement:

- RSS ingestion
- earthquake ingestion
- market ingestion
- event normalization
- event deduplication
- runtime condition evaluation
- SourceEvent persistence unless explicitly required
- NotificationDelivery persistence unless explicitly required
- Slack sending
- email sending
- n8n product workflows
- authentication
- authorization
- ASP.NET Core Identity
- admin operational dashboard
- runtime retry logic
- synthetic event ingestion
- LLM-based importance classification
- complex rule DSLs
- metrics infrastructure
- production observability backend infrastructure

Do not drift into the next runtime milestone.

# Required brainstorming output

After reading the repository and completing the analysis, present one concise recommended design for approval.

Include:

1. current repository/architecture context reconstructed
2. proposed persisted alert model
3. proposed ownership approach
4. explicit single-user-now / multi-user-ready strategy
5. explanation of why authentication is intentionally deferred
6. proposed condition semantics
7. proposed operator/value-type representation
8. proposed notification-channel representation
9. proposed PostgreSQL contract from n8n's perspective
10. proposed Razor Pages user flow
11. proposed CRUD scope
12. proposed Tailwind integration and why it is minimal
13. proposed validation rules
14. transaction/update semantics
15. proposed migration approach
16. proposed OpenTelemetry logs/traces setup
17. packages/tooling expected to be added and why
18. testing/validation strategy
19. documentation/ADR changes expected
20. alternatives explicitly rejected
21. any unresolved issue genuinely requiring user input

Prefer one recommended design.

Do not present many equally weighted alternatives unless a real unresolved trade-off exists.

# Approval gate

STOP after presenting the design proposal.

Do NOT yet:

- create feature entities
- create migrations
- create Razor Pages
- install feature/observability/Tailwind packages
- modify PostgreSQL
- modify n8n
- implement application functionality
- create the milestone commit

Wait for my explicit approval.

This approval gate is intentional.

# After approval — implementation phase

Once I approve the brainstorming result, continue in the SAME agent session.

Do not ask for another generic approval unless:

- a new architecture conflict is discovered
- a destructive/shared-infrastructure operation becomes necessary
- credentials are required and unavailable
- the approved design becomes unsafe or impossible
- migration review reveals unexpected database impact

Implement only the approved design.

# Expected implementation scope after approval

Subject to the approved design, implementation will likely include:

- alert persistence model
- stable owner identifier
- current-owner provider/configuration
- alert conditions
- alert channels
- EF Core mappings
- first real product migration
- owner-scoped queries
- alert list Razor Page
- create Razor Page
- edit Razor Page
- enable/disable behavior
- minimal dynamic condition/channel editing if justified
- management-side validation
- Tailwind CSS integration
- minimum professional styling
- minimal vanilla JavaScript where required
- OpenTelemetry logs/traces
- optional OTLP export configuration
- relevant automated tests/checks
- README/documentation updates
- real review evidence

Do not implement runtime event processing.

# User ownership implementation quality

All alert reads and writes must be scoped to the current owner.

Do not rely on a single-user assumption to omit ownership filtering.

Do not accept OwnerId from an untrusted form.

The current owner must be obtained from an application-side owner context/provider.

The current mechanism must be replaceable later by a real authenticated-user resolver.

Do not create fake security theater.

Document clearly:

"This MVP is single-user and does not implement authentication. The persistence/query model is ownership-aware so authentication can be added later without redesigning alert ownership."

# Database-contract quality

Because n8n will later access alert configuration directly, inspect the resulting PostgreSQL schema after migration.

Verify:

- relationships are understandable
- ownership is explicit
- discriminator/operator/type values are stable
- there are no accidental C# enum integer contracts
- foreign keys are sensible
- constraints are proportionate
- n8n can query enabled alerts without reverse-engineering application internals
- no credentials are stored in channel configuration

Do not create speculative indexes.

Add indexes only for known access patterns.

Likely known access patterns include:

- listing alerts for one owner
- later querying enabled alerts by event type

Evaluate rather than blindly adding indexes.

# UI quality

Keep the UI intentionally simple but professionally usable.

Tailwind CSS may be used to provide a consistent minimum visual baseline.

A user should immediately understand:

- alert name
- enabled status
- target event type
- conditions
- configured notification channels

Prioritize:

1. correct behavior
2. clear information hierarchy
3. form usability
4. validation feedback
5. basic responsive behavior
6. visual consistency

Do not optimize for visual novelty.

Do not build custom UI abstractions when standard Razor markup plus Tailwind utilities are sufficient.

Use accessible HTML semantics where practical.

The resulting UI should look intentional rather than like an untouched framework template, but polish beyond that is explicitly out of scope.

# OpenTelemetry implementation quality

When implementing observability:

- use standard `ILogger`
- do not invent a logging abstraction
- use OpenTelemetry SDK/instrumentation
- keep exporter configuration external
- use vendor-neutral OTLP
- preserve application startup without an exporter
- validate startup both with and without OTLP configuration where practical
- verify traces/logging do not expose secrets
- avoid obviously excessive/noisy telemetry
- use stable service naming
- do not create artificial distributed tracing through PostgreSQL

If a proposed instrumentation package is:

- deprecated
- preview-only without good reason
- incompatible
- unnecessary

reject it.

Document the correction if material.

# Tailwind implementation quality

When implementing Tailwind:

- choose the smallest currently supported setup
- keep Razor Pages as the primary UI architecture
- do not create a frontend SPA/project
- keep Node/npm usage minimal if required
- ensure CSS generation is reproducible
- document the actual build/watch command if one is needed
- make sure Docker/build behavior still works if relevant
- exclude `node_modules` and caches from Git
- avoid third-party component frameworks
- avoid large custom CSS layers when Tailwind utilities are sufficient

Validate that a clean build can produce the required CSS.

Do not depend on developer-machine-only generated assets without documenting how they are produced.

# Migration safety

Before applying the first real migration to shared DEV PostgreSQL:

1. confirm the database target
2. review migration source
3. inspect SQL if useful
4. verify only expected Sonrisa product objects are affected
5. confirm no destructive unrelated operations exist

If anything is ambiguous, stop before applying it.

Do not modify unrelated shared infrastructure.

# Validation after implementation

Actually validate the feature.

At minimum, where applicable:

- `dotnet build`
- automated tests
- Rider build/inspection
- application startup
- liveness health
- readiness health
- Tailwind build
- Razor Pages rendering
- migration generation
- migration review
- migration application to intended DEV database if safe
- resulting PostgreSQL schema
- create alert
- edit alert
- enable/disable alert
- condition persistence
- channel persistence
- owner scoping
- invalid typed value rejection
- invalid destination rejection where supported
- transaction behavior for child rows
- restart application and confirm persistence
- startup without OTLP configuration
- telemetry behavior with OTLP configuration if a safe endpoint exists
- useful request/application/database traces where configured
- logs/traces do not expose secrets

Use Rider, PostgreSQL MCP, browser/application checks, and normal .NET tooling where appropriate.

Do not claim checks that were not performed.

# AI review requirements

Critically inspect all generated code and configuration.

Look specifically for mistakes such as:

- ASP.NET Identity added despite being out of scope
- queries not filtered by OwnerId
- OwnerId supplied directly from form input
- C# integer enums leaking into PostgreSQL integration contracts
- arbitrary JavaScript/expression text stored as conditions
- values persisted ambiguously without type metadata
- child collections partially updated outside a transaction
- hard-delete introduced without considering future history
- credentials stored in AlertChannel
- Slack/email sending implemented prematurely
- n8n workflows created prematurely
- SourceEvent/NotificationDelivery introduced too early
- unnecessary repository/service abstractions
- unnecessary Clean Architecture projects
- generic repositories around EF Core without demonstrated value
- AutoMapper/MediatR added without need
- telemetry exporter becoming a startup dependency
- telemetry endpoint included in readiness
- secrets appearing in telemetry
- high-cardinality condition values attached to spans
- deprecated/incompatible OpenTelemetry packages
- over-complicated Tailwind/npm setup
- a frontend framework introduced alongside Tailwind
- large amounts of unnecessary JavaScript
- migration affecting unrelated PostgreSQL objects
- fake authentication/admin functionality

Correct meaningful issues before accepting implementation.

Record meaningful AI corrections in `docs/ai-review-log.md`.

Do not manufacture review findings.

# Documentation requirements

Update existing documentation where this milestone establishes real decisions.

At minimum ensure documentation clearly records:

- single-user MVP
- multi-user-ready ownership persistence
- authentication intentionally deferred because it was not required
- current owner-resolution mechanism
- warning that current owner resolution is not production authentication
- condition semantics
- supported operators
- supported value types
- notification-channel model
- direct n8n PostgreSQL-consumption expectation
- Razor Pages/Tailwind UI approach
- visual design intentionally kept minimal
- OpenTelemetry logs/traces
- OTLP configuration expectations
- telemetry failure behavior
- runtime processing still deferred to n8n
- explicit non-goals

Update architecture/decision logs where necessary.

Create a new ADR only when the decision is significant enough to deserve one.

OpenTelemetry may justify an ADR if it establishes a project-wide observability standard.

Do not create ADRs for trivial implementation choices.

# Review evidence

Produce real review/validation evidence according to repository conventions.

Record:

- checks actually performed
- actual results
- failures encountered
- meaningful corrections made
- items not validated

Do not fabricate output.

Do not claim PostgreSQL, UI, Tailwind, or telemetry behavior was validated unless it actually was.

# Milestone self-review

Before committing, review the complete diff as if reviewing another senior engineer's pull request.

Explicitly ask:

1. Is the application still thin?
2. Is runtime event processing still outside this milestone?
3. Is the PostgreSQL model understandable to n8n?
4. Are condition semantics deterministic?
5. Are typed values unambiguous?
6. Are multiple conditions deliberately AND-only?
7. Is ownership represented everywhere it matters?
8. Are all alert queries owner-scoped?
9. Is OwnerId protected from form tampering?
10. Did we avoid implementing authentication?
11. Is the lack of authentication documented honestly?
12. Can future authentication plug into ownership without schema redesign?
13. Are notification credentials absent from product data?
14. Are configuration updates atomic?
15. Is the migration minimal and safe?
16. Does the migration affect only expected product objects?
17. Did we avoid premature runtime tables/workflows?
18. Does OpenTelemetry use vendor-neutral APIs?
19. Can the app run without an OTLP backend?
20. Are logs/traces useful without leaking secrets?
21. Is Tailwind integration proportionate?
22. Did Tailwind accidentally create a second frontend architecture?
23. Is the UI usable without becoming a design project?
24. Did we avoid speculative abstractions/packages?
25. Did any AI-generated output survive merely because it looked plausible?
26. Is there anything in this commit that belongs to the next runtime milestone?

Correct issues before committing.

# Milestone commit

Once the approved implementation is complete and validated, create:

`feat: add alert configuration model and management UI`

Include only files relevant to this milestone.

Do not amend or rewrite previous milestone commits.

Do not include unrelated working-tree changes.

Do not include:

- local `.env`
- secrets
- `node_modules`
- build caches
- IDE-local files
- runtime artifacts

If Git state prevents a safe commit:

- do not force it
- do not rewrite history
- do not modify global Git identity
- report the blocker

# Final report after implementation

At completion, report concisely:

1. repository/context reviewed
2. final data model
3. ownership strategy
4. how the single-user MVP works
5. how future authentication can integrate
6. why authentication remains out of scope
7. condition semantics
8. supported operators/value types
9. notification-channel model
10. Razor Pages implemented
11. Tailwind integration used
12. migration created/applied
13. PostgreSQL schema validation performed
14. OpenTelemetry configuration added
15. logs/traces actually validated
16. packages/tooling added and why
17. tests/checks actually run
18. meaningful generated-code corrections
19. documentation/ADRs updated
20. files changed
21. whether the milestone commit was created
22. commit hash
23. blockers or risks before the first n8n runtime workflow milestone

Do not proceed into n8n event ingestion, runtime condition evaluation, or notification delivery after this milestone.
