# ADR-001: Use n8n for orchestration

## Status

**Accepted.** Recorded on 2026-09-14 from the supplied architectural direction. This records a decision already made, not an implementation outcome. No orchestration prototype, workflow, or comparative benchmark has been produced in this repository.

**Milestone 2 update:** n8n remains selected. [ADR-005](ADR-005-direct-database-integration-and-workflow-owned-delivery.md) supersedes the application/event-boundary ownership described below following the user's explicit direction to use direct database integration and workflow-owned delivery control. The original rationale is retained as historical context.

**DEV topology amendment:** [ADR-006](ADR-006-use-existing-shared-dev-infrastructure.md) selects the existing hosted DEV n8n runtime. This repository does not provision that runtime or manage its internal persistence. The original local operational assumptions below are historical.

## Context

The [product brief](../00-product-brief.md) calls for configurable alerts about important world events, email and Slack delivery, later channel extensibility, and an admin view. Event sources, importance rules, scale, UI needs, and detailed contracts are not yet specified. The work is time-boxed and should favor a useful end-to-end slice and maintainability over feature count.

The expected workload is primarily integration and orchestration: scheduled polling and/or webhook ingestion, HTTP/RSS-style sources, notification transport, credentials, retries, execution visibility, and workflow composition. These are generic integration concerns; the product-specific value lies in defining, validating, matching, and reliably tracking alerts and events.

The relevant n8n capabilities are documented by the platform:

| Need | Capability reference |
| --- | --- |
| Scheduled or webhook-driven execution | [Schedule Trigger](https://docs.n8n.io/integrations/builtin/core-nodes/n8n-nodes-base.scheduletrigger) and [Webhook](https://docs.n8n.io/integrations/builtin/core-nodes/n8n-nodes-base.webhook) |
| HTTP/RSS-style ingestion | [HTTP Request](https://docs.n8n.io/integrations/builtin/core-nodes/n8n-nodes-base.httprequest) and [RSS Read](https://docs.n8n.io/integrations/builtin/core-nodes/n8n-nodes-base.rssfeedread) |
| Email and Slack transport | [Send Email](https://docs.n8n.io/integrations/builtin/core-nodes/n8n-nodes-base.sendemail) and [Slack](https://docs.n8n.io/integrations/builtin/app-nodes/n8n-nodes-base.slack) |
| Integration credentials | [Create and edit credentials](https://docs.n8n.io/build/understand-workflows/create-and-edit-credentials) |
| Retry/error handling and execution visibility | [Node settings, including Retry On Fail](https://docs.n8n.io/build/understand-workflows/workflow-components/work-with-nodes), [error workflows](https://docs.n8n.io/build/flow-logic/handle-errors-gracefully), and [execution list](https://docs.n8n.io/build/understand-workflows/understand-executions/view-all-executions) |
| Workflow composition and portable definitions | [Sub-workflows](https://docs.n8n.io/build/flow-logic/break-workflows-into-smaller-parts) and [JSON export/import](https://docs.n8n.io/build/manage-workflows/export-and-import) |

References were consulted on 2026-09-14 to check capability-level claims. They do not select an n8n version, edition, hosting model, data source, or email provider. Verify specific node contracts and configuration against the chosen version when implementation begins.

## Decision

Use **n8n as the primary integration and workflow orchestration layer**. Let it handle scheduling, external integrations, workflow composition and execution, and notification transport.

Keep custom application code focused on product/domain rules, canonical event validation, alert matching, persistence boundaries, durable product state, and APIs where appropriate. Do not turn large visual workflows into an unstructured substitute for a domain layer. Durable state is expected outside transient workflow execution state; the storage design remains open.

The intended flow is external sources → n8n ingestion/orchestration → canonical application/event boundary → durable state and alert evaluation → notification delivery records → n8n channel delivery → email/Slack. See [the conceptual architecture](../04-architecture.md).

The exact custom application shape and management/admin UI technology remain undecided. This decision does not select Angular, .NET, a database, microservices, or any additional infrastructure.

## Alternatives considered

This is a qualitative design comparison against the known brief, not a report of implemented alternatives or measured results.

| Alternative | Benefits | Costs and current disposition |
| --- | --- | --- |
| Custom application worker/orchestrator | Direct control over execution and ordinary code-based testing; could fit naturally beside domain logic. | Requires building and maintaining scheduling, adapters, credential handling, retries, and execution visibility. Not selected because that generic infrastructure competes with proving product value in the time box. |
| Message-broker-oriented custom processing | Could support independent consumers and decoupled processing if requirements demonstrate that need. | Adds broker operations, producer/consumer contracts, retry coordination, and deployment complexity while integrations and domain logic still need implementation. Not selected because no scale or distribution requirement currently justifies it. |
| n8n | Reuses the capabilities above and allows custom code to concentrate on domain behavior. | Adds another platform to operate and review, with workflow testing and coupling costs. Selected as the current best complexity/value trade-off for an integration-heavy, deliberately small product. |

## Consequences

The expected benefit is less bespoke integration infrastructure and more time for a demonstrable product slice. This is an engineering expectation, not measured delivery savings. Integration behavior can be inspected through workflow executions, while domain behavior can be tested in ordinary application code.

n8n is an additional runtime to configure, maintain, and observe. Its credentials, access controls, upgrades, and execution-data retention will need deliberate handling when local infrastructure is designed. Domain and workflow contracts need coordinated changes and review. The product's admin requirement remains separate from n8n's workflow editor and execution views.

## Risks and trade-offs

- Large visual workflows can become difficult to understand, change, and review.
- Automated workflow testing can be less straightforward than tests of normal application code.
- Workflow definitions and node behavior create platform coupling and upgrade work.
- Source-controlled JSON can be noisy and needs disciplined review; exportability alone does not make changes understandable.
- Business rules hidden in workflow branches can erode the domain boundary and make behavior inconsistent.
- Transient execution state, retries, and transport success are insufficient on their own to guarantee durable or duplicate-free product behavior.
- Very high event throughput or new latency requirements could invalidate the processing architecture. No throughput threshold or performance claim has been established.

## Mitigations

- Give each workflow one clear responsibility and use reusable sub-workflows where they simplify repeated integration work.
- Store exported workflow definitions in `n8n/workflows/` when they exist; review JSON and actual behavior, record the tested version, and validate import/execution after relevant changes.
- Keep complex domain rules and canonical validation in custom code where practical, with explicit integration contracts and focused tests.
- Keep necessary product and delivery state durable outside transient executions. Specify idempotency and duplicate/update semantics at both ingestion and delivery boundaries before implementation.
- Coordinate workflow retries with durable application delivery policy. Exercise restart, replay, retry exhaustion, and accepted-but-unacknowledged sends. Do not promise exactly-once recipient delivery without a supported mechanism and evidence.
- Inspect exports before committing: n8n documents that they can contain credential names/IDs and embedded authentication headers. Also review pinned data and logs for secrets and sensitive content. [Export guidance](https://docs.n8n.io/build/manage-workflows/export-and-import)

## Revisit conditions

Revisit through a superseding ADR if measured throughput/latency, workflow maintenance or testing costs, operational constraints, or new product requirements invalidate this trade-off. Compare the demonstrated problem with simpler improvements and alternative processing designs. n8n is the current choice, not an irreversible commitment.
