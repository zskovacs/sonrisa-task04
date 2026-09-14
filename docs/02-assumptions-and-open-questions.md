# Assumptions and open questions

Milestone 2 register, recorded on 2026-09-14. Distinguish user-confirmed constraints, engineering decisions, and unvalidated planning assumptions. The original brief remains unchanged. Links below identify decisions made during the actual discussion, including superseded ownership proposals.

## Confirmed constraints and current decisions

- Email, Slack, configurable alerts, extensibility, and admin visibility come from the [product brief](00-product-brief.md).
- The user confirmed a strictly local demo, one trusted operator, and pre-created user/admin roles (D-001).
- Earthquakes are the first slice. The user explicitly clarified that RSS in the workflow example did not change that choice.
- The user accepted a common event envelope, validated type-specific data, and a supported typed field/operator condition after challenging source-specific hardcoding ([ADR-002](adr/ADR-002-canonical-events-and-typed-alert-conditions.md)).
- Razor Pages, PostgreSQL with separate application/n8n databases, application EF Core migrations, and connection strings outside Git are accepted ([ADR-003](adr/ADR-003-server-rendered-application-and-isolated-persistence.md)).
- The user requires automatic retries of uncertain external email/Slack sends and a circuit breaker, accepting possible duplicate external messages (D-002). The five-send example is a priority statement, not an attempt limit.
- The user's later clarification and workflow sequence assign all event processing and delivery control to n8n with direct product-database access. The application manages conditions and the UI. No n8n/application HTTP layer or application-owned breaker exists in the current design ([ADR-005](adr/ADR-005-direct-database-integration-and-workflow-owned-delivery.md)).

See [the decision log](decision-log.md) for chronology. Earlier ADRs remain historical where ADR-005 supersedes them.

## Explicit engineering assumptions

| ID | Assumption/design default | Rationale, effect, and validation |
| --- | --- | --- |
| A-01 | One real earthquake provider/type; a second type is a stretch goal. | Prove the full path first. Validate the provider's canonical mapping and demonstrate the extension boundary without building unused types. |
| A-02 | First valid accepted snapshot per source/external ID is immutable. | Keeps the new-event flow small. Provider corrections/retractions, including a later threshold crossing, can be missed. Review this limitation before expanding beyond the demo. |
| A-03 | Rules are sampled at the atomic evaluation operation; completed events are not rematched after edits. | Gives one explicit snapshot and recoverable processing. Test edits/creation while an event is Pending and verify snapshot consistency. |
| A-04 | Process the provider's bounded feed window on initial polling; no additional history import. | A first poll may include earlier occurrences. Confirm the chosen feed window and describe it in the demo; no completeness or latency SLA is assumed. |
| A-05 | One Slack workspace/profile, one email sender/profile, and allowlisted test destinations. | Fits one trusted operator and reduces onboarding. Confirm actual authorized accounts/destinations before live sends; do not assume credentials exist. |
| A-06 | At most one destination per channel per alert; one intent per event/alert/channel. | Provides a clear deduplication identity and avoids multi-recipient fan-out in this MVP. Test configuration validation and intent uniqueness. |
| A-07 | Development-only demo identity selection, with owner/role checks in management operations. | Demonstrates user/admin behavior without real identity onboarding. It is intentionally not production authentication; test local-only configuration and fail closed outside that mode. |
| A-08 | Three scheduled workflow responsibilities: ingest, evaluate Pending events, deliver Pending notifications. | Source failure or lack of new events must not strand durable work. Exercise crash/restart and independent recovery. |
| A-09 | Workflow-owned event, delivery/attempt, and circuit state lives in the product database; EF migrations own schema. | Supports admin reads and durable cross-execution coordination without application runtime ownership or custom n8n internal tables. Test permissions and migration/query compatibility. |
| A-10 | Configurable demo timing defaults from the architecture: small failure threshold/cooldown, one probe, bounded send/lease, capped retry delay. | These are engineering defaults, not measured capacity or user SLAs. Verify actual node timeouts/retries and controlled-time state transitions before acceptance. |
| A-11 | No automatic product-state deletion or pending-notification expiry during the demo. | Preserves deduplication keys and pending intent across replay/outages. An explicit isolated reset is allowed; long-running retention and stale-message expiry need a later product decision. |
| A-12 | Transient/uncertain work keeps retrying through backoff/circuits; permanent failures remain for operator correction. | Matches the user's preference to avoid loss. It cannot guarantee provider recovery, valid recipients, inbox placement, or human receipt. Test unknown outcomes and visible permanent failures. |

## Question register

| ID | Area | Current answer / remaining question | Resolve or revisit before |
| --- | --- | --- | --- |
| Q-01 | Importance | Enabled user-defined conditions determine relevance. No global score or runtime LLM. | New importance semantics require explicit product examples. |
| Q-02 | Alert definition | One supported field/operator/value condition; list/create/edit/enable/disable; one or both channels. | Detailed form/input bounds before management implementation. |
| Q-03 | Event/source type | Earthquakes confirmed. News/markets are extension examples, not additional MVP integrations. | Revisit after full-slice acceptance. |
| Q-04 | External source | No provider chosen. Require stable IDs, useful magnitude/time data, usable terms/quotas, and a bounded polling window. | Source workflow milestone. |
| Q-05 | Ingestion mode | Scheduled polling for the first slice; provider-specific cadence/window remains open. Webhook ingestion is deferred. | Source adapter implementation. |
| Q-06 | Canonical event | Common versioned envelope and validated earthquake data; required concepts are in the architecture. Exact size bounds/serialization mapping need the implementation contract. | Canonical ingestion milestone. |
| Q-07 | Matching | Magnitude greater than or equal to threshold; supported typed condition; missing/invalid magnitude rejected. Rule sampling follows A-03. | Matching query and tests. |
| Q-08 | Duplicate/update handling | Unique source/external ID and A-02 first-snapshot semantics. No cross-provider occurrence merging or correction/retraction processing. | Revisit before supporting revised reports. |
| Q-09 | Notification idempotency | Unique event/alert/channel intent; idempotent DB operations and attempt outcomes; external duplicates accepted for uncertain sends. | Claim/outcome query and workflow tests. |
| Q-10 | Failure/circuit behavior | n8n owns persistent circuit/retry logic; defaults and neutral/unknown outcomes are defined in the architecture. Provider-specific errors/timeouts must be checked. | Delivery integration milestone. |
| Q-11 | Ownership | Pre-created demo identities; owners manage their alerts; operator-provisioned destinations. No organization/sharing model. | Management implementation; revisit before pilot. |
| Q-12 | Access | Explicit local demo identity selection and separate database privileges. No n8n/application HTTP authentication requirement. Production authentication is deferred. | Any public or independent multi-user access. |
| Q-13 | Admin | Read-only events, processing, deliveries/attempts, errors, retry/circuit visibility. Recovery/debugging belongs in n8n. | Operational UI and workflow recovery runbook. |
| Q-14 | Scale/throughput | No numeric production user/event/latency target exists. Profile serialization is a local simplification, not a capacity claim. | Capacity-sensitive expansion; check demo backlog/fairness during validation. |
| Q-15 | Retention | Product data retained until explicit demo reset. n8n execution retention is separate; production lifecycle and notification expiry remain open. | Long-running or production operation. |
| Q-16 | UI | Razor Pages selected over native JS/TypeScript and Angular for current forms/lists. | Revisit if interaction complexity changes. |
| Q-17 | Slack | One configured workspace/profile, allowlisted destinations. Actual workspace, credential scope, and permissions remain open. | Authorized Slack setup and tests. |
| Q-18 | Email | One configured sender/profile. Provider, mechanism, sender setup, recipient allowlist, and error mapping remain open. | Authorized email setup and tests. |
| Q-19 | Persistence/runtime | ASP.NET Core, EF Core migrations, separate PostgreSQL databases, and n8n direct product access. Compatible supported version matrix and concrete privilege/query contracts remain integration work. | Executable skeleton and relevant schema changes. |
| Q-20 | Time budget | Time-constrained, but no numeric duration or production readiness target was supplied. MVP is one complete local slice with both channels and failure validation. | Committing to a dated schedule or expanding scope. |

## Updating this register

Record real answers and corrections when they occur. Update an assumption when evidence or user direction changes it; retain why it existed and link architectural changes through ADRs. Do not manufacture agreement, scale estimates, test outcomes, or business requirements.
