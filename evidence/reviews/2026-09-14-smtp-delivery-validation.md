# SMTP delivery extension validation

Baseline: `b9cf171`. Scope: [approved design](../../docs/superpowers/specs/2026-09-14-smtp-delivery-design.md) and [reviewed plan](../../docs/superpowers/plans/2026-09-14-smtp-delivery.md). This record distinguishes actual integration outcomes from pinned tests.

## Inspection and plan review

- Existing three Sonrisa workflows are inactive. Existing delivery ID `5fGfgXCBLxhNO479` was retained and renamed; Slack credential binding is `Sonrisa DEV Slack`.
- n8n metadata exposes one `smtp` credential, `SMTP account`. No secret fields were retrieved. The user indicated it should target `192.168.0.2:25`; the later actual capture corroborated connectivity.
- The user-supplied SMTP4DEV URL returned HTTP 200. A projected read of its documented-by-client `api/Server` route reported SMTP port 25 and no relay server. Server settings were not changed.
- PostgreSQL MCP and the externally configured EF inspection helper independently confirmed `sonrisa_dev` and the four existing migrations. Baseline delivery counts: email Unsupported 5; Slack Failed 1, Pending 3, Processing 2, Sent 1. No prior intents were reset.
- Native n8n discovery confirmed Send Email 2.1, operation Send, SMTP credentials and text format. [Official node documentation](https://docs.n8n.io/integrations/builtin/core-nodes/n8n-nodes-base.sendemail/) permits recipient lists, so a sole-mailbox runtime guard is needed for the product's one-recipient contract. [Native implementation](https://github.com/n8n-io/n8n/blob/master/packages/nodes-base/nodes/EmailSend/v2/send.operation.ts) returns Nodemailer info; current source supports the test assumptions but the later live execution verified this result shape.
- Independent plan review found three Important omissions: queue/other-alert isolation for the fixture, a durable no-send outcome for invalid email preparation, and verifying actual SMTP host/port rather than credential name alone. The plan was corrected and scoped re-review approved it. No implementation check was claimed by that review.
- The proposed fourth workflow was rejected by the user. Common selected-ID claim/read and channel-specific transport branches avoid duplicating orchestration.

## Schema and first focused verification

- Reviewed generated migration and SQL `20260914173558_EnableEmailDeliveryStates`: one drop/add of `ck_notification_deliveries_status` inside the EF migration transaction, plus EF history. No new table, product data conversion, unrelated schema or n8n storage change. Downgrade refuses active email states rather than silently changing data. Applied to externally configured, independently verified `sonrisa_dev`; PostgreSQL inspection confirmed the resulting constraint.
- `node --test n8n/tests/runtime.test.mjs`: 12/12 passed after initial failing tests. Offline full .NET run: 56 passed, 25 PostgreSQL tests skipped; this is not relational evidence.
- After migration, guarded `dotnet test tests/Sonrisa.Web.Tests/Sonrisa.Web.Tests.csproj --no-restore --filter 'FullyQualifiedName~RuntimeModelTests|FullyQualifiedName~PostgresRuntimeTests|FullyQualifiedName~PostgresWorkflowSqlTests' --verbosity minimal`: 14 passed, 0 skipped/failed. Covers email state, exact SQL, replay, legacy exclusion and concurrent claim.
- Evaluation SDK validated (6 nodes); actual inactive evaluation workflow Code body was updated and checked against repository source, then exported with the existing sanitizer.
- Task review delegation could not start: the tool returned `agent thread limit reached` for a new custom reviewer and attempts to resume existing reviewers. The controller reviewed the scoped source, SQL and tests directly. Independent implementation review was subsequently completed using the custom-role CLI fallback described below.

## Controlled source and matching path

The dedicated provider-shaped `n8n/fixtures/smtp-email.json` uses a clearly synthetic magnitude -100, not a scientifically realistic earthquake. It exists to match only the owned test alert without touching other owners' matching configurations. The native normalization contract accepts finite magnitude without an invented physical range restriction.

- Preflight confirmed no Pending events, no other enabled supported threshold at/below -100 and no prior fixture source ID. Created one owned email-only profile/alert, using the reserved test recipient.
- Execution **30** ran the actual existing ingestion workflow with temporary explicit fixture input, through its shared normalizer and database insertion. Inserted one `demo.usgs` / `sonrisa-smtp-20260914-01` event, ID `9d705904-7240-46ab-a6e6-38f79f4a7b71`. Restored live/empty ingestion defaults and verified the graph was unchanged.
- A readonly preflight passed the exact `select-event-candidates.sql` output through the exact `evaluate-alerts.js` body: one intended email intent, zero unrelated matches. The Pending queue contained only the new fixture.
- Execution **31** ran actual evaluation: one Pending email intent for alert `734f38d8-18ec-4b48-8f68-4b1367e5387e`, no diagnostics, event Evaluated. Delivery ID `81abff1e-5269-4112-8c5d-ce21d1c4d7b0`; PostgreSQL confirmed the intended destination snapshot. No transport was involved in either execution.
- Disabled the exact owned SMTP test alert after evaluation. This prevents future fixture matching; it does not retract already committed delivery intent under the established snapshot contract. Earlier milestone records remain untouched.

## Delivery graph review and tool compatibility

The built-in agent orchestration tool reached its thread limit. Review continued through the local Codex CLI using the exact `sp_task_reviewer` role instructions/model from the configured role files, with a read-only sandbox and no MCP/runtime secrets. Task 1 and Task 2 independently received APPROVE with no findings after controller corrections. Task 2's reviewer could not rerun nested Node process tests under its read-only sandbox (`EPERM`); controller/implementer test results remain the execution evidence.

Controller review removed a single-argument SDK credential placeholder that could auto-select the wrong account, strengthened export checks against inverted/additional conditions and disabled safety nodes, and tightened malformed/contradictory SMTP result handling. The remote batch binds SMTP by the intended credential ID; sanitized Git artifacts require explicit rebinding.

SDK validation accepted the intended 22-node graph and per-node validation accepted all ten new nodes. The first update API call rejected `setNodeSettings.maxTries=1` before mutation because that API requires at least 2, despite the SDK validator accepting 1. The corrected SMTP node explicitly disables retries and omits the unused maxTries setting; this retains one send attempt. The existing Slack node settings are preserved.

## Pinned transport and routing checks

Executions **32–35** ran the actual Code/IF logic. Manual trigger, operator input, every PostgreSQL node, Slack and SMTP were pinned for each execution: no transport or database write occurred.

| Execution | Input/result | Observed path |
| --- | --- | --- |
| 32 | Valid email, sole intended SMTP acceptance | Email branch → Record accepted email send |
| 33 | Error-shaped SMTP result | Email branch → Record ambiguous email outcome |
| 34 | Recipient list instead of one mailbox | Record invalid email message; SMTP node not reached |
| 35 | Slack channel, `ok=true` | Slack branch → Record acknowledged Slack send; email branch not reached |

These validate routing and acknowledgement logic, not transport connectivity or outcome SQL. The relational tests exercise the exact SQL separately.

## One real SMTP send and replay

Immediately before sending, PostgreSQL confirmed `sonrisa_dev`, the exact Pending email intent, owned alert and reserved destination snapshot. SMTP4DEV reported port 25, no relay and zero messages containing this delivery ID. The actual inactive workflow referenced `SMTP account`, with retries disabled.

- Execution **36** used real PostgreSQL and SMTP nodes: claimed delivery `81abff1e-5269-4112-8c5d-ce21d1c4d7b0`, passed email validation, received sole-recipient acceptance and recorded Sent. PostgreSQL reported `sent_at=2026-09-14T17:59:53.109363Z`, null LastError.
- SMTP4DEV captured one message, ID `25ca14c3-1f30-48a8-b847-b516c6ebee5f`, with the expected reserved sender/recipient and delivery ID in subject/body. [Sanitized capture](2026-09-14-smtp-capture.json) contains only the owned synthetic message's selected fields; no raw MIME, credentials or unrelated mailbox messages were retained.
- Execution **37** restarted from the same explicit ID. The shared claim returned zero rows; execution stopped before message reading or either transport. The capture count remained one.
- Restored the delivery ID to empty. The existing workflow is now **Sonrisa - Deliver Notification - DEV**, still ID `5fGfgXCBLxhNO479`, 22 nodes and 23 edges. No fourth workflow, recurring activation, automatic retry, backlog drain or real Slack send occurred.

SMTP4DEV capture proves acceptance by this DEV SMTP server, not delivery to an internet inbox or human receipt. Ambiguous outcomes stay Processing for investigation, without automatic retry. Historical Unsupported email rows and prior Slack outcomes remain untouched.

## Final verification

- `node --test n8n/tests/*.test.mjs`: **26 passed, 0 failed** after exporting the actual tested graph. Includes exact source/SQL/graph parity, inert defaults, email safety, routing drift, sole-recipient acknowledgement and Slack rejection regressions.
- Guarded `dotnet test tests/Sonrisa.Web.Tests/Sonrisa.Web.Tests.csproj --no-restore --verbosity minimal`: **81 passed, 0 failed/skipped** against the verified DEV product database. Build completed as part of the command. This includes actual PostgreSQL state constraints and simultaneous email/Slack claim checks.
- Externally configured `dotnet ef migrations has-pending-model-changes --project src/Sonrisa.Web --no-build`: no pending model changes.
- `git diff --check`: passed. Final remote inspection confirmed all three workflows inactive and empty delivery selection.
- A fresh read of all three final remote definitions matched the sanitized repository artifacts exactly. Exports omit credential bindings and pins; explicit PostgreSQL/Slack/SMTP rebinding is documented. Changed Markdown local-file links resolved; changed/new files passed secret-pattern inspection and manual export review. No browser screenshot is claimed: available browser tooling could not open this environment, so the SMTP4DEV client/API and owned MIME download supplied capture evidence.
- Post-test database counts retain all baseline outcomes: email Unsupported 5; Slack Failed 1, Pending 3, Processing 2, Sent 1. The only additional persistent delivery is the one SMTP Sent record.

## Final independent review

The configured `sp_final_branch_reviewer` ran through the same read-only custom-role CLI fallback and returned **APPROVE_WITH_MINOR_NOTES**: no Critical or Important findings. Its sole Minor finding was the README opening status still describing only Slack. The controller updated that paragraph to include the SMTP extension and its evidence, then checked the final diff. No runtime changes followed the passing integration/tests. The signed follow-up commit uses `feat: add SMTP notification delivery`; completed milestone history is preserved.
