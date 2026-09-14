# Runtime simplification: inspection before approval

Date: 2026-09-14

Status: read-only investigation and proposed correction; implementation is not approved by this record. The exact request is [prompt 025](../../prompts/025-simplify-n8n-runtime-architecture.md).

## Repository and live state

- Branch: `feat/first-n8n-alert-workflow`; HEAD `e1dc3377cb8303951ed07c18e5923309bd7c3db3` (`feat: add SMTP notification delivery`). The runtime milestone is already committed at `b9cf17159e20cd9d0382a9fd4d18f3e05253abc8`. The working tree was clean before this request's prompt archive and inspection evidence.
- Read-only PostgreSQL MCP inspection verified `sonrisa_dev`, PostgreSQL 18.6, all five repository migrations applied, and product tables `users`, `alerts`, `source_events`, `notification_deliveries`, plus EF migration history.
- The two runtime tables contain 12 source events and 13 delivery records. These are real retained DEV records, not unapplied scaffolding. Removal requires an approved forward migration and explicit acknowledgement of data loss; counts must be checked again immediately before removal.
- `FirstRuntimeSlice` and `EnableEmailDeliveryStates` are committed and applied. Preserve these migrations and their designers. A forward correction would remove current runtime entities/configuration and update the model snapshot, without rewriting migration history.
- The actual configuration contract stores condition columns on `alerts`, and shared email/Slack destinations on `users`. There is no separate current AlertChannel table. Preserve this configuration model and owner-scoped UI behavior.
- No independent requirement for runtime queues or delivery history was found in the product brief. Earlier user clarifications did approve the durable architecture and SMTP extension; the correction must preserve that chronology rather than describe those implementations as unauthorized.

## Remote Sonrisa workflows inspected through n8n MCP

| ID | Name | Nodes | Active |
| --- | --- | --- | --- |
| `K3A9cowlgglFqriG` | Sonrisa - Ingest Earthquakes - DEV | 8 | No |
| `zZYWvZSk1LDGhgJa` | Sonrisa - Evaluate Pending Events - DEV | 6 | No |
| `5fGfgXCBLxhNO479` | Sonrisa - Deliver Notification - DEV | 22 | No |

The delivery workflow already contains Slack and SMTP branches. Referenced credential names include `Postgres account`, `Sonrisa DEV Slack`, and `SMTP account`. No credential secret was retrieved. Existing authorized Slack test destination: `C0C1QRQDTSN`. These references establish configuration availability, not a fresh successful authentication test.

No workflow was changed, executed, archived, deleted, or activated during this inspection. No notifications or database writes were performed. Browser tooling was unavailable; no canvas screenshot or browser inspection is claimed.

## Installed n8n capability verification

The public editor's release metadata reports `n8n@2.38.7`. Only the release/environment metadata was projected, without telemetry connection values. MCP exposes Remove Duplicates version 2 and its previous-execution operation. Official documentation and the version-pinned upstream source were inspected; this is source/capability verification, not a deployed replacement workflow test.

### Native deduplication

Proposed settings: first remove duplicates within the input using `source` and `external_id`; then use `removeItemsSeenInPreviousExecutions`, `removeItemsWithAlreadySeenKeyValues`, expression `source + ':' + external_id`, node scope, history size 10,000.

The previous-execution operation alone does not filter duplicate occurrences within the current input. Two native nodes address the two responsibilities. [Official examples](https://docs.n8n.io/integrations/builtin/core-nodes/n8n-nodes-base.removeduplicates/templates-and-examples/).

The installed-version implementation checks stored count plus input count against the maximum before filtering. It throws on overflow; do not promise automatic rolling eviction or indefinite polling. No automatic history clear is proposed. [Version-pinned node source](https://github.com/n8n-io/n8n/blob/n8n%402.38.7/packages/nodes-base/nodes/Transform/RemoveDuplicates/v2/RemoveDuplicatesV2.node.ts).

The helper persists hashed keys in n8n-owned processed data, scoped by workflow and node ID, before returning downstream. It is not Sonrisa product persistence or execution-only memory. Its read/update sequence does not establish an atomic concurrent claim guarantee. Retaining the same workflow/node identity matters; recreated workflows or cleared history can process earlier events again. A shared-instance restart was not performed. [Version-pinned helper](https://github.com/n8n-io/n8n/blob/n8n%402.38.7/packages/cli/src/deduplication/deduplication-helper.ts).

The bounded mechanism is proposed for manual milestone validation. Live cross-execution, downstream-failure and small-history boundary checks remain required after approval. Before unattended activation, the history limit needs explicit operational review. No alternative Sonrisa persistence layer is proposed.

### Native retry

The installed execution engine supports `retryOnFail: true`, `maxTries: 5`, `waitBetweenTries: 5000`: five total node attempts, with four waits on repeated failure. Node retry re-executes the invocation, so dispatch should present one notification item at a time. Proposed final error behavior is stop-workflow: visible failure, no recovery state, and possible loss of remaining work already deduplicated. Live controlled retry validation is still outstanding. [Version-pinned engine](https://github.com/n8n-io/n8n/blob/n8n%402.38.7/packages/core/src/execution-engine/workflow-execute.ts).

## Decisions requiring explicit supersession

ADR-011's three workflow/state boundaries and database idempotency conflict directly with the correction. ADR-005's durable operational-state/recovery provisions and D-002's uncertain-send retry/circuit requirement also conflict. Preserve n8n orchestration, direct configuration reads, canonical typed events, EF ownership, shared DEV topology, user destinations, and management ownership. Update affected residual claims in ADR-001/002/004/006/010 and current architecture/scope/plan/runbooks after approval, with a new architectural supersession record.

Retain the USGS preferred-ID investigation and explicit rejection of alias resolution. Record that durable queues/recovery were previously approved but are now rejected as unnecessary for the revised MVP. Existing Slack/SMTP validation remains historical evidence; it does not prove the proposed native deduplication/retry path.
