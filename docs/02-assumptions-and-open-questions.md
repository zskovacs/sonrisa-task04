# Assumptions and open questions

Current decisions follow [ADR-012](adr/ADR-012-use-n8n-native-runtime-state.md), the user's approved runtime correction. Earlier approvals are preserved in the [decision log](decision-log.md) and historical ADRs; the current register no longer treats durable recovery as a product requirement.

## Confirmed constraints

- The original brief requires configurable alerts, Slack, email, extensibility and an admin view. No reliability SLA, permanent event ledger, delivery audit, guaranteed delivery or recovery after outages is required.
- Earthquakes remain the first source type; public USGS all-hour GeoJSON is selected. RSS and market data are extension examples, not current integrations.
- One supported typed condition: earthquake/magnitude/gte/number with a finite numeric threshold. No multiple conditions, AND/OR, arbitrary scripts or rules DSL.
- Single-user management uses one configured/default owner, owner-scoped reads/writes and no authentication (ADR-008). Runtime reads enabled configuration across owners.
- The actual PostgreSQL configuration model has alerts with inline conditions and users with shared email/Slack destinations (ADR-010). Preserve revision/atomicity validation and FluentValidation.
- ASP.NET Core/Razor Pages is the local management plane; shared DEV PostgreSQL and hosted n8n remain externally operated. EF owns product migrations; n8n reads configuration directly. There is no application runtime API.
- n8n owns normalization, native technical deduplication, evaluation, channel dispatch, bounded retry and execution visibility. Product runtime tables/queues/recovery are rejected.
- Slack and Email use five total native attempts per notification; exhaustion discards that item and continues subsequent notifications. Ambiguous duplicates/loss and dedup-before-delivery loss are accepted.
- Email is supported through the same inactive runtime's channel branch. The only upstream exception is an alert-name query projection/pass-through used for Email text; selection, matching, ownership and deduplication remain unchanged.

## Engineering assumptions and limits

| Area | Current decision or assumption |
| --- | --- |
| Initial window | Process the bounded current one-hour feed; no cursor/archive import or completeness/latency guarantee. Manual tests suppress transport until the one authorized send. |
| Schedule | Inactive/manual after milestone 6. Five-minute polling is proposed only for later reviewed activation. |
| Canonical event | Versioned workflow-only envelope plus finite magnitude; reject malformed required data, omit invalid optional URL. No product persistence is needed. |
| Identity | Same retained source/external-ID key is normally filtered. Provider preferred IDs can change; aliases, cross-provider merging, corrections and retractions remain deliberately unimplemented. |
| Dedup history | n8n node-scoped history size 10,000. Installed 2.38.7 checks stored count plus incoming batch before filtering and may throw at the cap; no rolling/permanent/atomic guarantee. History loss/reset or identity recreation can permit repeats. |
| Rule sampling | Sample current enabled configuration when each new event reaches the query. No historical rematching after an alert is created or edited. |
| Delivery | Best-effort five-attempt native retry per notification; explicit error output continues the loop. A seen event's notification may be lost after DB/transport failure; no next-day recovery or backlog drain. |
| Unsupported channels | Unknown channels are visible skips, never delivered. Missing/invalid Email destinations are independently discarded/diagnosed so a bad channel cannot suppress another valid channel. |
| Visibility | Use n8n executions/errors and source/external/alert IDs. No new product history tables or global n8n OTEL requirement. |
| Data removal | Applied runtime state is removed by a reviewed forward migration; original migrations and historical evidence remain. An optional restricted backup stays outside Git. |

## Remaining questions

| Area | Remaining question / revisit point |
| --- | --- |
| External Email delivery | SMTP4DEV capture validates the implemented native SMTP branch, credential binding, message construction and capture. A real external provider and recipient inbox remain untested; a provider swap should need credential/sender configuration only. |
| Admin purpose | Define the smallest product admin view independently of the rejected event/delivery ledger. Technical debugging remains in n8n. |
| Unattended activation | Review history capacity behavior, authorized destinations, source frequency, timeouts and shared DEV load before activating the future five-minute schedule. No automatic history reset is currently approved. |
| Scale | No numeric production event/user/latency target exists. Do not infer one or add concurrency infrastructure speculatively. |
| Production security | Distinct least-privilege application, migration and n8n roles and secure transport remain required before production. Existing broad DEV access/no-TLS observations are documented under ADR-007 and runtime evidence, not desired production settings. |
| Authentication | Deferred intentionally; replace the owner resolver with authenticated identity only under a later requirement. |
| Telemetry | Application OpenTelemetry remains implemented; global hosted n8n OTEL is unverified and unchanged. |
| Retention | Configuration lifecycle and n8n execution/dedup retention are separate. No automatic product-runtime cleanup exists because there is no product runtime store. |
| Time budget | No numeric delivery date or production-readiness target is supplied. Do not turn MVP validation into a production guarantee. |

Record future decisions when made, distinguishing observations from assumptions and preserving superseded reasoning.
