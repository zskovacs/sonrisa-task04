# Sonrisa n8n runtime

The current architecture is [ADR-012](../docs/adr/ADR-012-use-n8n-native-runtime-state.md): one primary `Sonrisa - Process Alerts - DEV` workflow in the existing shared instance at `https://n8n.nasgard.io`. Keep it inactive. The completed MVP is manually operated; do not activate unattended polling.

## What the workflow does

Manual source selection → USGS all-hour fetch or explicit fixture → shared canonical normalizer → split events → native within-batch duplicate filter → native previous-execution filter → parameterized configuration SELECT → typed magnitude evaluation/channel expansion → one notification per iteration → Slack, Email, or visible unsupported-channel branch. Each terminal transport path returns to the loop, so later notifications continue.

PostgreSQL contains only product configuration and EF history. `n8n/sql/select-enabled-alerts.sql` reads enabled alerts joined to user profiles across owners. It never uses `MvpOwner:Id` or writes runtime state. Code bodies in `runtime/` implement focused normalization, matching and text preparation. There is no queue, delivery ID, claim, processing status or application runtime service.

Email is the second supported transport in this routing boundary. It uses bounded plain-text preparation and the n8n-owned SMTP credential; its recipient comes from the matched user's persisted Email destination. The added alert-name query projection/pass-through exists only for message text and does not alter selection/matching. Historical SMTP delivery-state machinery remains rejected.

For application setup and the concrete workflow import/rebinding steps, use the [runbook](../docs/08-runbook.md#n8n-operations). The existing authoritative workflow is [aVijfnQr0kdLAJHP](https://n8n.nasgard.io/workflow/aVijfnQr0kdLAJHP).

## Credentials and permissions

Bind `Load owner alert configuration` to the intended `postgres` credential targeting `sonrisa_dev`, `Send Slack notification` to the intended `slackApi` credential, and `Send Email notification` to the intended `smtp` credential. Actual runtime destinations come from the matched user's persisted profile, not a hard-coded transport parameter. SMTP4DEV validation confirms SMTP submission/capture, not external inbox delivery.

Credential values stay inside n8n. The exporter deliberately removes credential references as well as secrets, so all three nodes must be explicitly rebound after import. Intended product permissions are SELECT on `public.users` and `public.alerts`, without DDL or schema ownership. Current broad DEV access and transport limitations are documented exceptions, not production settings. Do not edit unrelated credentials or n8n internal persistence.

## Manual operation and first run

`Operator feed input` defaults to `mode=live`, `fixture_json=''`. A full manual run may notify for several matching events in the current one-hour feed; inactive means no recurring trigger, not that manual execution cannot send. The feed is bounded, but there is no historical cursor/bootstrap suppression.

For deterministic validation, select `mode=fixture` and supply a reviewed earthquake-shaped fixture JSON. `Select trusted feed path` sets the namespace to `demo.usgs`; provider data cannot impersonate this operator choice. The same downstream normalizer and matching code run. Synthetic Slack and Email content are visibly marked. Before any future schedule is added, connect it only to the live source path; never to mutable fixture selection.

Validate upstream with transport suppressed/mocked first. Before the single real Email test, inspect all matched channels and prove exactly one test-owned Email notification is eligible, with the intended persisted destination and SMTP credential. Do not debug by repeatedly sending. SMTP4DEV is an approved capture target; it does not establish external inbox delivery. Repeat the **full entry** to test deduplication; replaying a downstream send node bypasses duplicate filtering and can resend.

Restore live/empty-fixture defaults after validation. Proposed future polling is every five minutes against the one-hour feed, subject to a later history-capacity and activation review. No schedule is part of the current artifact.

## Native history and retry limits

Two Remove Duplicates v2 operations are necessary: source/external-ID comparison within input, then the `source + ':' + external_id` key against previous executions. Previous-execution scope is node, history size 10,000. Keep workflow/node IDs stable across updates.

Runtime validation recorded n8n 2.38.7 and confirmed that the node throws when existing history plus incoming item count exceeds its configured cap before filtering. Do not describe this as an unlimited ledger or automatic rolling eviction. Lost/reset/recreated history, future retention changes, concurrency or changed provider IDs can permit repeats. There is no globally atomic event claim. Do not add product persistence or clear history automatically as a workaround.

Slack and Email settings: `retryOnFail=true`, `maxTries=5`, `waitBetweenTries=5000`, `onError=continueErrorOutput`. The Loop Over Items batch size is one; output 1 processes an item and output 0 completes. Wire success/error/validation diagnostics back to the loop. An exhausted notification is discarded; subsequent items proceed. These are five total attempts, not five retries after an initial attempt.

Events are already seen before querying/sending. A failed database read or discarded Slack/Email notification may therefore never be retried by later polling. Seen events also do not rematch newly created/edited alerts. Ambiguous transport failures can cause lost or duplicate notifications. No queue/recovery worker or exactly-once guarantee exists.

## Source control and checks

From the repository root:

```bash
node --test n8n/tests/*.test.mjs
node n8n/build-workflow.mjs process
node n8n/build-workflow.mjs process --fixture earthquakes.json
```

The builder emits SDK source for the n8n MCP validator/importer. Validate the SDK before creating/updating a workflow and retrieve the saved graph afterward. The current source is `sdk/process.sdk.js`; the authoritative tested export is `workflows/process.json`.

Export an actual remote snapshot kept outside Git through the checked sanitizer:

```bash
node n8n/export-workflow.mjs process /path/outside/repository/remote-snapshot.json
```

Review the generated JSON before storing it as `n8n/workflows/process.json`. The sanitizer verifies the graph and important settings/source/query bindings and removes environment credential references. It rejects active state, unsafe transport changes, unexpected graph/code/SQL, temporary fixture input and pinned execution data. Do not hand-edit the final artifact away from the tested graph. Rebinding credentials after import is required; importing as a new workflow creates a distinct deduplication history.

Local tests cover exact Code source and export guards. Real execution evidence establishes native duplicate behavior, real configuration reads, real USGS, controlled retry exhaustion in both transport directions, preserved Slack routing, and one SMTP4DEV Email result. Temporary transport mocks are test instrumentation only: remove them and restore/inspect the real graph before export. Pinning success alone does not validate retries.

Use n8n execution IDs, source/external IDs and alert IDs for diagnosis. Terminal per-item diagnostics must distinguish acceptance, retry exhaustion and unsupported channels. Avoid raw destination/credential logging or execution dumps. Global n8n OTEL remains unverified and unchanged.

### Credential troubleshooting

The decryption and missing-scope incidents below are recorded in [historical first-runtime evidence](../evidence/reviews/2026-09-14-first-runtime-validation.md#controlled-slack-destination-and-initial-blockers).

Rebinding is expected after importing the sanitized export. If a `slackApi` node reports credential decryption failure, it failed before contacting Slack; verify the intended credential through the authorized n8n operator process rather than changing global encryption settings or exposing a token. A prior historical workflow also returned a missing required OAuth scope after decryption succeeded. For a comparable current error, verify the intended Slack posting scope and reauthorize the credential through the authorized process before a controlled retry.

For `postgres` and `smtp` failures, confirm only the intended node binding and target through the operator process. Do not repeatedly replay a downstream transport node: that bypasses duplicate filtering and can send again. Keep raw credentials, destination values, full execution payloads, and SMTP settings out of source control and evidence.

## Replacement and historical artifacts

The original ingestion, Pending evaluation and selected-ID Slack/SMTP workflows were archived after replacement validation in `c38f2a3`. Preserve their execution history and the original Git commits/evidence. Forward EF migration `20260914184146_RemoveObsoleteRuntimeState` removed the two obsolete runtime tables after target/dependency/SQL review; any backup stays outside Git. The application configuration model and ownership behavior remain unchanged.
