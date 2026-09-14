# Assumptions and open questions

Initial register: 2026-09-14. No answers have been agreed for the questions below. “Resolve before” identifies the dependency, not a promised date. Update a row when an answer is actually obtained and link the decision or supporting evidence.

## Known facts and accepted direction

The [brief](00-product-brief.md) requires user-configured alerts, email and Slack notifications, extensibility for later channels, and an admin view. The work is time-boxed. [ADR-001](adr/ADR-001-use-n8n-for-orchestration.md) accepts n8n for orchestration, with domain concerns and durable product state kept outside complex visual workflow logic where appropriate. These do not define alert semantics or select an application stack.

## Working proposals, not agreed requirements

| ID | Proposal | Status and validation needed |
| --- | --- | --- |
| A-01 | Start with a narrowly selected event/source scenario and small explicit rule behavior. | Proposed scope strategy, not an agreed source count or rule language. Confirm that the selected scenario is useful with the product owner before implementation. |
| A-02 | Use deterministic fixtures alongside a later real integration demo. | Proposed validation approach. Agree representative examples and expected outcomes before treating them as acceptance criteria. |

No numeric scale, latency, retention, retry, or availability assumptions have been adopted. No ownership or authentication model is assumed. Additional assumptions must state their rationale, impact, and how they will be checked.

## Open questions

| ID | Area | Unresolved question | Resolve before |
| --- | --- | --- | --- |
| Q-01 | Importance | What makes an event important, to whom, and which observable examples should or should not notify? | First-slice specification. |
| Q-02 | Alert definition | How do users express and manage alert rules? Which inputs and operations are necessary? | Rule model and user workflow design. |
| Q-03 | Event/source types | Which initial subject and event types are in scope? News, markets, and disasters are examples only. | First-slice specification. |
| Q-04 | External sources | Which sources are appropriate and accessible, with usable contracts, quotas, and terms? None selected. | Source adapter/workflow design. |
| Q-05 | Ingestion mode | Polling, webhooks, or both? What freshness and missed-ingestion behavior are needed? | Workflow design. |
| Q-06 | Canonical event | Which fields, provenance, identifiers, timestamps, validation, and versioning rules are necessary? No schema agreed. | Ingestion contract and persistence design. |
| Q-07 | Matching semantics | What comparisons, combinations, time interpretation, and handling of missing data are required? No rule language selected. | Matching implementation. |
| Q-08 | Duplicate events | What identifies a repeat versus an update? Do reports from different sources about the same occurrence count as duplicates? | Event persistence and matching. |
| Q-09 | Notification idempotency | What identifies one intended delivery? How should replay, concurrency, and unknown transport outcomes be handled? What residual duplicate risk is acceptable? | Delivery contract and durable state design. |
| Q-10 | Retry/failure behavior | Which failures are retryable, under what limits/delays, and what happens after exhaustion or restart? Who owns each retry and any manual recovery? | Workflow and delivery design. |
| Q-11 | User ownership | Who owns alerts and destinations? Are accounts, shared ownership, or multiple organizations needed in the first slice? | Persistence, API, and management design. |
| Q-12 | Authentication/authorization | Who may create/change alerts, access destinations, invoke internal boundaries, or view/administer operations? What is the intended deployment/access context? | Exposing APIs, user/admin surfaces, or webhooks. |
| Q-13 | Admin view | Which operational questions must it answer? Are event/delivery status, failure detail, or recovery actions needed, and for whom? | Admin design; do not assume the n8n editor is the product admin UI. |
| Q-14 | Scale/throughput | Expected users, alerts, sources, event rate, bursts, and notification fan-out? Any latency target? | Capacity-sensitive design and acceptance budgets. |
| Q-15 | Retention | How long should events, rules, delivery records, execution history, and personal data remain? What deletion expectations apply? | Persistence lifecycle and logging design. |
| Q-16 | UI technology | Which of a small server-rendered UI (including ASP.NET Core Razor Pages), native JavaScript/TypeScript plus HTTP API, or Angular best serves the agreed interactions? | Selecting application/UI skeleton; future ADR. |
| Q-17 | Slack workspace behavior | One configured workspace or multiple tenants/workspaces? Who supplies credentials and chooses destinations? Is self-service OAuth necessary? | Slack transport and ownership design. |
| Q-18 | Email delivery | Which delivery mechanism/provider and sender setup are available? Who configures recipients and credentials? | Email transport design. |
| Q-19 | Application and persistence | What is the smallest custom application shape and durable store? How are product records and n8n execution data separated? | Executable skeleton; future architecture decision. |
| Q-20 | Delivery constraints | What is the actual time budget and which demonstrable journey defines success within it? | Committing to implementation scope. |

## Updating this register

Distinguish an answer from a temporary assumption. Link accepted decisions to [the decision log](decision-log.md) and an ADR when architectural. Mark rejected proposals with the actual reason when rejection occurs. Keep unresolved questions visible and record new questions only when they arise; do not backfill imaginary discussions.
