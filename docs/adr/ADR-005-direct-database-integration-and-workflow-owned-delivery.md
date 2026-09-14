# ADR-005: Use direct database integration and workflow-owned delivery control

## Status

**Accepted boundary correction.** Recorded on 2026-09-14 from [the user's explicit clarification](../../prompts/005-revise-n8n-and-application-boundaries.md). No code or workflows exist. The user subsequently confirmed n8n ownership of the complete event-processing sequence in [prompt 006](../../prompts/006-confirm-workflow-processing-sequence.md). The final proposed processing/state contracts are documented in [the system design](../04-architecture.md).

This supersedes the application/HTTP ownership assumptions in ADR-001 and ADR-002, and the HTTP integration and exclusive product-database access portions of ADR-003. Their other choices remain applicable. It rejects the application-owned circuit/claim mechanism proposed in ADR-004; that mechanism was never implemented or accepted as a complete design.

**DEV topology amendment:** [ADR-006](ADR-006-use-existing-shared-dev-infrastructure.md) supersedes the requirement below that this project account for a separate n8n internal database and its credential. Hosted n8n owns its internal persistence outside this repository. The product database remains the shared integration contract, with separate least-privilege application and workflow credentials and EF Core schema ownership. All runtime-processing ownership decisions remain in force.

## Context

The prior design assigned canonical validation, matching, durable delivery coordination, and circuit gating to application services accessed through HTTP. The user clarified a smaller application responsibility: save user conditions and provide the associated management functionality. n8n should read product data directly and implement the circuit breaker within its workflows.

The clarification changes the responsibility boundary; it is not equivalent to the previous proposal. Record the correction explicitly rather than implying that direct database integration was always intended.

## Decision

- Keep the ASP.NET Core/Razor Pages application focused on configuration, condition management, user/role concerns, and the management/admin surface.
- Let n8n access the application PostgreSQL database directly. Do not create application HTTP endpoints for n8n or an HTTP retry/callback/claim protocol between them.
- Implement notification retry and circuit-breaker behavior in n8n workflows. The application does not authorize send attempts, execute the breaker state machine, or schedule retries.
- Preserve two separate PostgreSQL databases: the product/application database and n8n's internal database. Direct product access does not merge these databases. n8n uses an additional credential for the product database, distinct from its internal database credential and the application's credential.
- Retain EF Core migrations as the owner of the application database schema. n8n must not independently modify that schema. Use a dedicated runtime database role with only the reads/writes required by the agreed workflow contract; direct access does not require migration or superuser privileges.
- Keep all connection-string values outside Git, including workflow exports. Use the n8n credential store for workflow database credentials and the already accepted external configuration mechanisms for the application.

## Consequences and remaining design work

The integration contract becomes a documented, versioned database contract rather than HTTP endpoints. Coordinate EF migrations, workflow queries, and validation whenever shared fields change. Use explicit query projections and parameterized values; do not interpret a user condition as executable SQL or a free-form expression.

The application does not provide an event-processing API. The user confirmed that n8n owns event normalization/validation, matching, deduplication coordination, and durable notification writes. The system design splits ingestion, pending-event evaluation, and pending-delivery recovery into three scheduled responsibilities. Evaluation and required intent creation complete atomically in one workflow-owned PostgreSQL operation; failed executions leave discoverable pending work. Do not retain an application evaluator or delivery gate behind another name.

Workflow-owned reliability uses explicit event, delivery/attempt, and per-transport circuit records in the product PostgreSQL database. n8n workflows own their state transitions; EF migrations own schema and the admin may read them. This does not transfer behavioral ownership to application code. Workflow execution memory and undocumented n8n internal tables are not the product source of truth. Circuit scope, neutral outcomes, leases, replay, and proposed demo timing defaults are specified in the system design.

Removing HTTP also removes the proposed HTTP retry layer. It does not make database reads/writes infallible or automatically idempotent. A failed database operation must leave recoverable work; replay must not duplicate events or notification intents. These are workflow/database contract concerns under the revised boundary.

The admin reads event/processing, delivery/attempt, and circuit state without controlling delivery. Recovery is an n8n operational responsibility. The MVP deliberately assumes first-accepted event snapshots and evaluation against rules current at the atomic evaluation operation; the limitations and revisit conditions are explicit in the system design.

## Alternatives and trade-offs

| Alternative | Disposition |
| --- | --- |
| Application domain services and n8n HTTP integration | Previously proposed/partly approved, now superseded by the user's explicit narrower application scope. |
| Direct n8n database access and workflow-owned reliability | Selected. Removes application integration endpoints, but couples workflows to the product schema and shifts validation/testing into workflow and database contracts. |
| One giant workflow with arbitrary SQL or duplicated per-source logic | Not implied by the decision. Preserve small reusable boundaries and the accepted typed event/rule contract. |

The price of removing HTTP is not zero integration work: schema compatibility, query safety, durable state, and failure behavior still need deliberate design. No new infrastructure, custom workflow engine, or production identity platform is introduced.

## Validation implications

Replace planned application ingestion/delivery API tests with workflow input-contract tests, product-database permission checks, EF migration/query compatibility tests, and workflow/database replay/concurrency experiments. Test breaker state across executions, not only within one run. Record actual results only when implementation exists.
