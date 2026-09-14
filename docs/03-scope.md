# Initial scope proposal

The product needs in [the brief](00-product-brief.md) are known. The MVP below is an engineering proposal to narrow those needs; detailed acceptance criteria are not agreed. Confirm the first scenario through [the open questions](02-assumptions-and-open-questions.md) before implementation.

## Current task: repository baseline only

Create working rules, the brief, plan, scope, question register, initial architecture, n8n ADR, validation strategy, log formats, and reserved directories. Reach only the first milestone commit.

Do not create production code, .NET projects/solutions, frontend applications, npm packages, Docker services, PostgreSQL configuration, executable n8n workflows, authentication, alert models, or notification delivery. Do not install packages or select external APIs. The bootstrap/context prompt is excluded from prompt history.

## Proposed MVP

- One narrow, useful end-to-end scenario before expanding event or source coverage. Select the scenario explicitly; the illustrative subject areas are not three required integrations.
- n8n for orchestration and external integrations, with small workflows reviewed and exported to Git when implemented.
- A canonical event concept with explicit validation and duplicate handling; actual fields and identity rules remain open.
- User-configurable alerts with only the rule behavior needed by the agreed scenario. No complex rules language is assumed.
- Email and Slack delivery through an extensible notification-channel boundary. Adding a future channel should not require redesigning matching.
- Durable state for the necessary product and delivery records, with agreed idempotency, failure, retry, and recovery behavior. No database or exactly-once transport guarantee is selected.
- A minimal alert-management surface and an operational admin surface. Exact workflows and access rules need agreement. Prefer the smallest suitable UI; Angular is not selected.
- Repeatable fixtures and a deterministic demo/test path, complemented by a later real integration demonstration. Make limitations and delivery outcomes observable.

The initial value target is a complete path from a configured alert through a matching event to a delivered notification and an inspectable outcome, including both required channels before MVP acceptance. This is a proposed acceptance direction, not an implemented capability.

## Stretch goals

Consider these only if the agreed slice is complete, validated, and there is remaining time and demonstrated value:

- Additional event/source types or simple rule operators.
- Additional operator convenience, such as a carefully bounded recovery action, if it is not already required by the agreed failure contract.
- Additional UI polish after essential user and admin journeys work.

## Explicit non-goals and deferred work

Sophisticated rule DSLs, geospatial rules, LLM-based importance classification, microservices, Kubernetes, RabbitMQ, event streaming platforms, a custom workflow engine, high-availability infrastructure, cloud infrastructure, large-scale streaming architecture, complex multi-tenant Slack OAuth, SMS/push, and production-grade identity platforms are deferred. Reconsider them only for a demonstrated requirement with documented alternatives and costs.

Deferring a full identity platform does not decide that access is anonymous or remove authorization needs. Deferring complex Slack OAuth does not decide workspace ownership. Both remain open and must be resolved before exposing the affected behavior.

## Scope change rule

Record the requirement or observation driving a change, its effect on time and validation, and the simpler alternatives considered. Review the changed scope and update the plan before implementing it. Keep useful ideas outside the current milestone as future work.
