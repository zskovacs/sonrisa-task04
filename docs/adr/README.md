# Architecture decision status

Read [current architecture](../04-architecture.md) first. ADRs preserve the decision and evidence available when recorded; “no implementation yet” in an old ADR describes that time. This index reconciles all twelve ADRs without rewriting their historical reasoning.

| ADR | Final status |
| --- | --- |
| [001 — n8n orchestration](ADR-001-use-n8n-for-orchestration.md) | Orchestration retained. Application-processing ownership superseded by 005; runtime persistence/recovery by 012. |
| [002 — Canonical events and typed conditions](ADR-002-canonical-events-and-typed-alert-conditions.md) | Typed extension direction and one-condition MVP retained. Application-processing ownership superseded by 005; persistence/recovery by 012. |
| [003 — Razor Pages and isolated persistence](ADR-003-server-rendered-application-and-isolated-persistence.md) | Razor Pages, EF product migrations and external secrets retained. HTTP runtime integration superseded by 005, local service/internal-storage scope by 006, demo identity selection by 008. Angular deferred for scope/complexity. |
| [004 — Application delivery coordination](ADR-004-durable-delivery-and-circuit-breaking.md) | Application allocation rejected before implementation by 005. Durable guarantees/circuits subsequently superseded by 012. |
| [005 — Direct database integration](ADR-005-direct-database-integration-and-workflow-owned-delivery.md) | n8n runtime ownership/direct configuration reads retained. SQL matching superseded by 011; runtime state/recovery by 012; topology by 006, demo ownership by 008 and destination allowlists by 010. |
| [006 — Shared DEV infrastructure](ADR-006-use-existing-shared-dev-infrastructure.md) | Accepted. Application-local/shared-service topology and exclusion of n8n internal storage retained; current DEV access exception is 007, runtime-state clauses superseded by 012. |
| [007 — DEV access exception](ADR-007-accept-current-dev-database-access.md) | Accepted for current DEV administrative application access and observed lack of PostgreSQL TLS. Production restrictions remain. |
| [008 — Configured MVP owner](ADR-008-use-single-configured-mvp-owner.md) | Accepted. No authentication; owner-scoped management. Shared user destinations refined by 010; read-only cross-owner admin separately authorized by D-010. |
| [009 — OpenTelemetry](ADR-009-use-opentelemetry-logs-and-request-traces.md) | Accepted and implemented: application logs/request traces, optional OTLP, no Npgsql spans/metrics/backend provisioning. |
| [010 — Shared destinations](ADR-010-store-shared-user-notification-destinations.md) | Accepted configuration model. Delivery-intent snapshot clause superseded by 012. |
| [011 — n8n evaluation and replay-safe writes](ADR-011-use-n8n-evaluation-and-replay-safe-runtime-writes.md) | Historically implemented. Typed n8n matching and alias deferral retained; separate workflows and PostgreSQL runtime writes superseded by 012. |
| [012 — n8n-native runtime state](ADR-012-use-n8n-native-runtime-state.md) | Current runtime authority: configuration-only PostgreSQL, one n8n pipeline, bounded dedup/retry and best-effort delivery. Email completion is D-009. |

[Decision log](../decision-log.md) records refinements including Email and the admin split. [Runtime simplification evidence](../../evidence/reviews/2026-09-14-runtime-simplification-validation.md) records the actual forward migration and replacement workflow validation. No new architectural decision was required for documentation finalization.
