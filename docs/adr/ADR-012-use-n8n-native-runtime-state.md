# ADR-012: Keep runtime technical state in n8n

## Status

Accepted on 2026-09-14 under the [runtime correction request](../../prompts/025-simplify-n8n-runtime-architecture.md) and the user's subsequent approval. Implementation and live acceptance are recorded separately in the runtime validation evidence.

Supersedes ADR-011's PostgreSQL source/delivery state and three-workflow boundaries, ADR-005's durable product-runtime state/recovery provisions, and D-002's eventual-delivery/circuit requirement. Also supersedes residual event/delivery/attempt persistence requirements in ADR-001/002/004/006 and ADR-010's delivery-intent snapshot clause. Retains n8n orchestration and matching, direct configuration reads, canonical typed events, Razor Pages, EF schema ownership, shared DEV topology, configured management owner and shared user destinations. Historical ADR text and commits remain evidence of the earlier approved design.

## Context

The initial AI-assisted design introduced separate ingestion, evaluation and delivery workflows, plus PostgreSQL event status and notification intents. This was approved and implemented at `b9cf171`; the subsequent SMTP extension was committed at `e1dc337`. Review then concluded that the design duplicated orchestration and technical state already available in n8n and solved reliability requirements absent from the original product brief. The user explicitly replaced those earlier approvals with a smaller best-effort runtime.

## Decision

PostgreSQL stores product configuration: `users` and `alerts`, including ownership, the inline typed condition and shared non-secret destinations. Remove the applied `notification_deliveries` and `source_events` tables through a reviewed forward EF migration. Preserve historical migrations. n8n performs no product DDL or runtime writes; its intended product credential needs only configuration reads.

Use one primary workflow, `Sonrisa - Process Alerts - DEV`: fetch USGS, normalize a canonical event, filter duplicates natively, read enabled configuration across owners, evaluate magnitude `gte` a finite numeric threshold, expand destinations and route channels. No current-owner filter, application processor, queue, runtime API, intent, delivery lifecycle, attempt history, circuit or recovery worker is needed.

Use Remove Duplicates v2 for both within-batch and previous-execution filtering, with `source + ':' + external_id`, node scope and 10,000 history entries. The installed n8n 2.38.7 node throws when stored count plus incoming batch exceeds its cap before filtering. This is bounded workflow-owned technical history, not a permanent ledger or globally atomic claim. Do not add another persistence layer or automatic history clearing. Recreated node/workflow identity, lost/reset history, future eviction behavior, concurrent execution or changed provider IDs may allow duplicates. Review the history boundary before unattended operation.

Slack uses native retry: five total attempts, five seconds between failures, one notification per invocation. Its exhausted error output records a safe diagnostic in execution data and returns to the item loop so subsequent notifications continue. Success also returns to that loop. Delivery is best-effort: failed notifications are discarded; ambiguous failures may duplicate or lose messages. Deduplication before dispatch is intentional, including loss after a downstream outage. Seen events do not need rematching for subsequently created or changed alerts. No exactly-once or guaranteed delivery is claimed.

Slack is implemented in this milestone. Email remains a required product channel and the next runtime milestone adds its transport branch to this same pipeline. Existing explicit user destination fields remain; theoretical channel extensibility does not justify a configuration refactor now. Reuse prior SMTP transport logic selectively without restoring delivery state.

Keep the workflow inactive and manually validate it. Five-minute polling of the bounded one-hour feed is future activation configuration. First-run validation must suppress transport until the authorized single synthetic send; an unrestricted first run can match several events from that hour. Archive old inactive workflows only after the replacement is validated. Retain their execution history and Git evidence.

## Alternatives and consequences

- Reject product database deduplication, queues and durable delivery recovery: these add migrations, lifecycle semantics and processors without a current product requirement.
- Retain the useful discovery that USGS preferred IDs may change; defer aliases and physical-earthquake reconciliation intentionally.
- Reject aborting the entire notification batch after one failed destination. Per-item native retry plus error-output feedback provides the required isolation without a custom retry loop.
- Reject a separate email runtime architecture. Extend channel routing next; email is required, not optional scope.
- n8n history and errors provide technical inspection. No product delivery history or global OTEL change is introduced. Domain IDs connect investigation without pretending to propagate application traces through PostgreSQL.

## Validation

Require real USGS normalization, controlled fixture matching across owners, malformed/disabled/nonmatching cases, actual native within/across-execution deduplication, bounded-history behavior, one real Slack send and no replay send. A controlled transport mock must prove five failed A attempts followed by B processing; pinned success data alone cannot prove retries. Review/apply the forward migration only against verified `sonrisa_dev`, check dependencies, and verify management behavior afterward. Export the actual final inactive graph without secrets or test payloads.
