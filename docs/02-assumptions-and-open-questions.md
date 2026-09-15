# Assumptions and open questions

This register describes the final MVP. Historical proposals and reversals remain in the [decision log](decision-log.md) and [ADR status index](adr/README.md). A deferred capability is not an unresolved requirement or an implicit promise.

## Resolved decisions

| Original ambiguity | Final decision and record |
| --- | --- |
| What counts as important? | The owner chooses one finite earthquake magnitude `>=` threshold. Multiple conditions and a DSL are excluded: ADR-002 and D-005. |
| Which source? | Public USGS all-hour GeoJSON; live and explicit synthetic paths share canonical normalization. [Runtime contract](07-runtime-contract.md). |
| Who processes events? | n8n owns fetch, normalization, native deduplication, typed matching, channel dispatch, bounded retries and execution history. [ADR-012](adr/ADR-012-use-n8n-native-runtime-state.md). |
| What is stored? | PostgreSQL owns alerts with inline typed conditions and users with shared non-secret Email/Slack destinations; EF owns migrations. [ADR-010](adr/ADR-010-store-shared-user-notification-destinations.md). |
| Who is the current user? | One configured/default owner and scoped management queries. No authentication or authorization. [ADR-008](adr/ADR-008-use-single-configured-mvp-owner.md). |
| What does admin mean? | Read-only cross-owner configuration counts and lists; runtime operations remain in n8n. D-010 and [admin evidence](../evidence/reviews/2026-09-14-operational-admin-validation.md). |
| Which UI? | Razor Pages + Tailwind; Angular was unnecessary for the agreed forms/lists. [ADR-003](adr/ADR-003-server-rendered-application-and-isolated-persistence.md). |
| Where does DEV run? | Local application, existing shared PostgreSQL and hosted n8n. No local service provisioning or ownership of n8n storage. [ADR-006](adr/ADR-006-use-existing-shared-dev-infrastructure.md). |
| What delivery guarantee? | Best effort, five total native attempts per notification, discard and continue. Stronger durability/recovery was considered and superseded by ADR-012. |
| How does Email extend the flow? | Same matcher/router, native SMTP branch and bounded plain text. Only alert-name projection/pass-through was added upstream; no schema/app change. D-009. |
| What observability? | Application logs/request traces with optional OTLP; no Npgsql spans, metrics or shared n8n telemetry changes. [ADR-009](adr/ADR-009-use-opentelemetry-logs-and-request-traces.md). |

## Accepted engineering limits

| Area | Limit established by implementation/evidence |
| --- | --- |
| Event identity | Source/external-ID keys; provider preferred IDs may change. No physical-earthquake exactly-once claim or alias reconciliation. |
| Deduplication | Node-scoped history of 10,000. Version 2.38.7 was recorded in runtime evidence; controlled cap testing confirmed failure before filtering, not rolling eviction. This documentation session did not re-establish the server release. |
| Rule sampling | Current enabled configuration is read for each retained event across owners. Previously seen events are not rematched after alert edits or provider magnitude changes under the same key. |
| Loss/retry | Dedup precedes query/send; downstream failure can permanently lose notifications. Ambiguous acknowledgements can lose or duplicate them. |
| Bad inputs | Malformed records/rules/destinations yield diagnostics; destination checks are independent. Unknown channels are visible skips. Invalid top-level source/query failures halt the execution. |
| Security | Configured owner is not a security boundary. Administrative DEV database access/no PostgreSQL TLS was explicitly accepted under ADR-007; production requirements remain separate. |
| Current operation | One inactive manual workflow, bounded current-hour feed, no schedule or historical cursor. Manual execution can send to all matching configured owners. |

## Intentionally deferred

- **Authentication and authorization:** require a later access requirement; protect admin and replace the owner resolver only then.
- **Unattended operation:** proposed five-minute polling is a future setting, subject to capacity, destination and shared-load review. No automatic dedup reset is approved.
- **New sources/channels/conditions:** only after concrete product demand; explicit Email/Slack profile fields do not constitute a generic channel registry.
- **Alias reconciliation and durable recovery:** deliberately excluded from the requested MVP; revisit only if a requirement changes.
- **Global n8n telemetry and retention administration:** owned by the shared service operator, unchanged by this project.

## Genuinely unknown / unvalidated

| Question | Current boundary |
| --- | --- |
| Which external Email provider and inbox? | No external provider/recipient delivery acceptance exists. SMTP4DEV capture is verified; a provider change would require credential/sender configuration and new validation. |
| What production scale, latency or reliability target? | No numeric requirement was supplied. Current tests do not establish load capacity, permanent deduplication, concurrent ingestion safety or schedule reliability. |
| What production deployment and access model? | No production-readiness date, numeric delivery time budget or public/multi-user deployment was supplied. DEV access acceptance is not production validation. |
| What long-term execution retention policy? | Shared n8n retention/endurance and restart behavior were not validated; native-history configuration and tested capacity behavior are documented separately. |

Record a future decision when made; do not resolve an unknown by inventing a requirement.
