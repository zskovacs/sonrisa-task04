# Runtime simplification validation

Date: 2026-09-14. Baseline: `e1dc337` on `feat/first-n8n-alert-workflow`. Scope: [approved correction](../../docs/superpowers/specs/2026-09-14-runtime-simplification-design.md). This record distinguishes local checks, mocked n8n behavior and actual external transport evidence.

## Design and task review

The plan reviewer requested explicit independent destination validation and malformed-destination tests; the correction was added and re-review approved it. Task 1 EF review approved configuration-only mappings, the ordered forward drops, unchanged historical migrations and focused model tests. Existing architecture/SMTP approvals remain historical facts. The latest user approval specifically requires continuation after individual retry exhaustion and retains email as a required next same-workflow channel milestone.

## Baseline and local checks

- Baseline Node suite: 26 passed.
- Baseline .NET suite: 56 passed, 25 guarded PostgreSQL skips, zero failures.
- Task 1: focused model/migration tests 2/2; solution build zero warnings/errors; EF no pending model changes; local full suite 56 passed, 13 guarded PostgreSQL skips.
- Task 2: exact Code/export Node tests 8/8; read-only hosted SDK validation valid with 22 nodes. These are local/schema checks, not live dedup/retry proof.

## Migration preflight

Verified `sonrisa_dev`: five applied migrations ending at `20260914173558_EnableEmailDeliveryStates`; 12 source events and 13 deliveries. Configuration baseline: 4 users and 5 alerts. Configuration digests before removal: users `2e23b5cfc4a96bcae91710d30b324e8b`, alerts `6d31ee11ef3b6832589a67945e68c587` (values not recorded).

Reviewed migration `20260914184146_RemoveObsoleteRuntimeState` and its incremental idempotent SQL: two ordered table drops, then EF history insert, wrapped in a transaction; no CASCADE or configuration-table changes. Dependency inspection found only those tables' own constraints and the delivery-to-source foreign key; no unrelated normal dependencies. Historical migration files are unchanged.

A restricted backup of the 12 event/13 delivery rows was written outside Git with mode 0600. Its directory has mode 0700. No row data, credentials or connection values were printed or copied into this evidence. The shell did not expose the existing User Secrets file, so the controller resolved the already ignored `.env` connection only in memory/environment; every operational connection verifies `current_database()` before work. The first User Secrets lookup failed before connecting; the corrected lookup then completed the backup.

## Applied schema correction

Applied `20260914184146_RemoveObsoleteRuntimeState` through EF after the target, generated source/SQL and dependency checks above. Post-apply inspection found only `users`, `alerts` and `__EFMigrationsHistory` in the product schema. Both obsolete runtime tables are absent and the new migration is the history head. No historical migration was edited. The original configuration remained unchanged.

All temporary validation configurations were subsequently removed by their exact test-owned IDs. Final counts are again 4 users / 5 alerts. Final `md5(jsonb_agg(to_jsonb(row) ORDER BY id)::text)` digests match the pre-migration values above for both tables. An intermediate check used a different row-JSON/string-aggregation representation and therefore produced different digests; repeating the original JSONB algorithm confirmed equality. No configuration values are recorded here.

## Hosted replacement and live source

Created [Sonrisa - Process Alerts - DEV](https://n8n.nasgard.io/workflow/aVijfnQr0kdLAJHP), ID `aVijfnQr0kdLAJHP`, with 22 final nodes. It remained inactive throughout. There is no schedule or webhook trigger. PostgreSQL and Slack credential references were inspected by non-secret name/ID; their values were never retrieved. The live query observed the exact test configurations created in verified `sonrisa_dev`, across two owners.

During upstream tests a temporary Code transport mock replaced the disconnected, disabled Slack node. Its counters existed only in n8n test execution state. A mock acceptance diagnostic is **not evidence of a Slack send**. The mock was removed before the real send and final export.

| Execution | Actual result and boundary |
| --- | --- |
| 38 | Real public USGS all-hour request succeeded. Four provider features became four canonical numeric earthquake items; no normalization diagnostics. Four fresh keys passed native dedup; actual configuration read/evaluation produced no match. No transport invoked. |
| 39 | A new real-source execution returned the same four keys: previous-execution dedup output was 0 new / 4 discarded; no configuration query or transport. |
| 40 | Controlled provider-shaped fixture entered the normalizer and actual configuration query. Five records included one malformed string magnitude and one repeated ID: four canonical items, three within-batch distinct items. With two test owners at threshold 5, 4.9 generated no notification for those owners, 5 and 5.1 generated one Slack item per owner. The existing independent threshold-4 alert also matched, only against the mock. Disabled test alert generated no item. Email items reached `email_transport_not_implemented`. |
| 41, 42 | Repeated full fixture executions discarded all three previously seen keys; no downstream processing. Execution 42 followed an MCP update rejected atomically for an invalid parameter path; inspection confirmed the previous fixture was unchanged. The subsequent update used complete node parameters. |
| 43 | Initial mock demonstrated five attempts and later notifications continuing, but included additional JSON keys beside `error`, so n8n classified its final output as ordinary data. This was a test-double defect and did not validate error-output routing. No actual Slack call occurred. |
| 44 | Corrected mock reproduced Slack's error-only JSON shape. One owner's notification failed five times over 20,062 ms, reached `slack_retries_exhausted_discarded`, and did not reach acceptance. The next owner's notification was attempted once; all later notifications continued. Five other mock Slack items completed. A magnitude-3 event produced no notification for any enabled alert. Overall execution completed successfully. |
| 45 | Temporarily reducing native history size to 1 caused the documented capacity error before configuration/transport: stored history plus incoming count exceeded the limit. |
| 46 | Restoring history size to 10,000, without clearing history or changing node identity, allowed the fresh magnitude-3 event through. No match or transport. |
| 47 | A temporary read-only `SELECT` with deliberate division by zero failed visibly at the PostgreSQL node after dedup. This simulated a query failure, **not a database outage**; no shared service was disrupted. |
| 48 | Restored the exact reviewed query and repeated full entry. The event was already seen, so the query and transport were not retried. This demonstrates the accepted seen-before-downstream-success loss boundary. |
| 49 | MCP test with explicitly pinned configuration, actual normalizer/dedup/evaluator/loop and mocked transport: unknown operator, string threshold and malformed destinations emitted safe diagnostics; disabled config emitted nothing; unrelated valid Slack items continued; valid email was skipped visibly. Pins were test-only. |
| 50 | Created a new threshold-3 test alert after execution 46 had seen its event. Replaying that event was still discarded; the new alert did not cause historical rematching. |
| 51 | Full preflight with actual configuration and mock transport produced exactly one Slack item for the single test alert and authorized destination. No actual Slack execution. |
| 52 | **One real Slack send.** Fresh `demo.usgs:correction-single-send-authorized`, magnitude 3, test alert `d70bc002-a913-4d7e-8de7-860000000006`. Slack returned `ok=true`, channel `C0C1QRQDTSN`, app `A0C1LMW5S0M`, bot `B0C1K5D8ZLK`; the native node completed in 246 ms and the acceptance diagnostic carried the correct event/alert IDs. The message was clearly marked synthetic. This proves Slack API acceptance, not that a human read it. |
| 53 | Repeated the complete real-send entry: 0 new / 1 discarded, no Slack node execution. No second message was sent. |

The failure-isolation check is a controlled transport mock running in the installed engine, not five deliberate calls to a broken real Slack destination. The installed [Slack v2 implementation](https://github.com/n8n-io/n8n/blob/n8n%402.38.7/packages/nodes-base/nodes/Slack/V2/SlackV2.node.ts) returns an error-only JSON object on continued failure. The [engine error-output classifier](https://github.com/n8n-io/n8n/blob/n8n%402.38.7/packages/core/src/execution-engine/workflow-execute.ts) recognizes that shape; additional arbitrary fields in the first mock prevented classification. Correcting the mock retained native retry logic and proved the approved A-fails/B-continues condition without changing production orchestration.

No shared n8n restart or concurrent race experiment was performed. Separate completed executions with stable workflow/dedup node IDs demonstrated persisted cross-execution behavior. Source review and the capacity experiment establish bounded history; there is no permanent-ledger, rolling-eviction or globally atomic claim. USGS preferred-ID changes remain an explicitly accepted duplicate risk. Global n8n OpenTelemetry configuration was not changed or claimed verified.

## Authoritative export and obsolete workflows

Restored `mode=live`, empty fixture input, the real Slack node and five-attempt/error-output settings. The final graph is inactive, with no test mock, pins, fixture payload, SQL mutation, schedule or additional transport. Exported the actual remote definition through the repository sanitizer into `n8n/workflows/process.json`; credential bindings and the native Slack node's generated webhook metadata are deliberately stripped. Import requires credential rebinding and a new workflow has a different native dedup history.

The first sanitizer assumed node-array order and rejected the SDK's recursive ordering and Slack's automatically generated non-secret webhook ID. Review corrected it to verify the exact node set and edges independently of array order and strip that metadata only for the known Slack send node. Tests reject webhook metadata on unrelated nodes.

After replacement validation/export, n8n returned `archived=true` for the three previously inactive workflows: `K3A9cowlgglFqriG` (Ingest Earthquakes), `zZYWvZSk1LDGhgJa` (Evaluate Pending Events), and `5fGfgXCBLxhNO479` (Deliver Notification). No workflow or execution deletion was requested. Their historical exports, commits and evidence remain in Git history. The current MCP Sonrisa workflow listing contains only the new inactive primary workflow. Archived definitions are no longer returned by the normal MCP detail lookup.

## Final automated verification

- `node --test n8n/tests/*.test.mjs`: final run 12 passed, zero failures, including actual export checks and four retained source-validation regressions. Controller review restored those useful source-only tests after they had been removed with the obsolete runtime-state suite.
- Guarded exact PostgreSQL configuration-query tests: 2 passed, zero skips.
- Full `dotnet test Sonrisa.sln --no-restore` with the externally configured, name-guarded DEV test target: 71 passed, zero skips/failures. This includes management, ownership, validation and database checks after schema removal.
- `dotnet build Sonrisa.sln --no-restore --verbosity minimal`: zero warnings/errors.
- Hosted `validate_workflow` on the final SDK: valid, 22 nodes, no reported warnings.

- `dotnet ef migrations has-pending-model-changes --project src/Sonrisa.Web --no-build`: no changes since the last migration.
- Exact deep comparison of the sanitized final remote snapshot against `n8n/workflows/process.json`: equal, 22 inactive nodes, no credential bindings, pins, mock or SMTP node.
- `git diff --check`: passed.
- Local Markdown links in changed current documents/specs/plans/evidence: no missing targets.

The focused documentation review found that the runbook's final-export claim was premature while validation was in progress. It is now substantiated by the actual exported file and parity check above. No other material architecture contradiction was reported.

## Final whole-branch review

The final reviewer found one Important documentation inconsistency: decision-log rows still presented the superseded eventual-delivery/circuit and three-workflow choices as current despite the new header. Their status and rationale now explicitly record historical approval and ADR-012 supersession, while retaining original decision text. Re-review: **APPROVE**, no remaining Critical/Important findings. The reviewer independently reran all 12 Node tests, export parity and whitespace checks, and assessed the recorded 71/71 guarded .NET results.

The review confirmed one primary runtime, configuration-only product schema, native workflow-owned dedup/state/retry, no pending/delivery queue, n8n matching, future source normalization/channel routing extension, per-item retry exhaustion with permitted loss, and retained course-correction history. No exactly-once guarantee, email transport, recurring activation or runtime .NET processor was introduced.

The final changed/new-file scan found no private keys, Slack token patterns, bearer tokens or connection-string values. Structured export inspection found no credential bindings, pins, static data or webhook IDs. All ten pre-existing migration source/designer files are byte-for-byte unchanged. Export SHA-256: `13f57ad184c518036458ef61f67b86241839085f4957a3f2bb4abcfe64a091ff`. Exact archived correction-prompt body: 26,579 characters; SHA-256 `bf2c7e8ba46f52d673631eed8f35eae855a7838c4906c5f20494e375fe331239`.
