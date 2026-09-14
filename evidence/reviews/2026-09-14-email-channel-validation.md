# Email channel validation — 2026-09-14

## Scope and reviewed context

Baseline: `c38f2a3`; milestone branch: `feat/email-notification-channel`. Reviewed root instructions/README, product and architecture documents, all ADRs, milestone specifications/plans, prompt history, prior review evidence, current application model/migrations, Git history/status, workflow sources/exports, and live DEV workflow/configuration/execution history. Historical SMTP work at `e1dc337` supplied transport-only knowledge; none of its rejected delivery architecture was restored.

The authoritative workflow remains **Sonrisa - Process Alerts - DEV**, ID `aVijfnQr0kdLAJHP`, on n8n **2.38.7**. It has 26 nodes and remains inactive. Final tested/restored graph version: `5880b333-24c6-4cfd-8d52-ae45f5d5f2b1`. The three old workflows remain archived/inaccessible and were not changed. No additional top-level runtime workflow was created.

## Implementation and boundary comparison

Email uses the existing one-item notification loop and existing Switch's Email output. The old unsupported-Email diagnostic was replaced by plain-text preparation, native Send Email v2.1, result projection, and invalid/exhausted diagnostics. Every terminal path returns to the existing loop.

The 19 retained nodes outside the SQL/evaluator/removed Email placeholder compare identically with the fresh remote baseline, allowing only the restored explicit `disabled:false` flag on Slack. Ingestion, normalization, deduplication node IDs/settings, query joins/filtering/binding, typed comparison, owner behavior, channel fan-out, Slack parameters/credential/retries and workflow settings are unchanged. The source files for ingestion, normalization and Slack have no diff.

**Explicit small upstream exception:** SQL projects `a.name` and the evaluator passes it through as `alert_name`. This changes the query projection/notification envelope for user-friendly Email content; it does not change loading selection or matching. The literal claim that all alert-loading/evaluator bytes stayed unchanged would be inaccurate.

Email's recipient expression reads the matched owner's configured destination. The existing n8n SMTP credential is bound remotely; no credential values or references are exported. Sender is non-secret transport configuration. Replacing SMTP4DEV with a provider requires SMTP credential/sender configuration changes, not pipeline redesign.

## Native retry and safety contract

Both transport nodes use `retryOnFail=true`, `maxTries=5`, `waitBetweenTries=5000`, `onError=continueErrorOutput`. A one-item loop makes the native retry unit one notification. The Email node returns an error-only item when continue-on-error is enabled; the installed engine's native retry/error-output behavior was verified before mocking it. Five attempts are grouped into one node execution record.

Temporary credential-free Code mocks returned exactly `{error:'controlled_transport_failure'}` for the designated failed item on every attempt. The error decision did not depend on counters. Manual-only node static data counted calls for observation; a subsequent successful mock item reported the counts and immediately cleared them. Installed lifecycle source excludes manual runs from static-data persistence. Mocks were removed before export. There is no product retry counter, retry loop, queue, ledger or recovery process.

Relevant implementation references: [Send Email implementation](https://github.com/n8n-io/n8n/blob/n8n%402.38.7/packages/nodes-base/nodes/EmailSend/v2/send.operation.ts), [native workflow execution](https://github.com/n8n-io/n8n/blob/n8n%402.38.7/packages/core/src/execution-engine/workflow-execute.ts), [execution lifecycle](https://github.com/n8n-io/n8n/blob/n8n%402.38.7/packages/cli/src/execution-lifecycle/execution-lifecycle-hooks.ts). Live execution and SMTP capture validate the installed behavior, beyond source inspection.

Plain-text preparation accepts one mailbox only, rejects header/control injection and invalid event envelopes, bounds display values, and validates optional HTTP(S) links. The message contains no internal alert/execution IDs. Synthetic subject/body are clearly marked. Result confirmation requires one exact accepted recipient, no rejected recipients, and a matching envelope when returned. Unconfirmed outcomes are visibly discarded. Diagnostic nodes omit recipient addresses, raw provider errors and full source payloads.

## Execution evidence

[Sanitized execution observations](2026-09-14-email-channel-executions.json) record actual execution IDs and counts. Except execution 62, all transport results below were mocked; no new real Slack message was sent.

| Execution | Validation | Observed result |
| --- | --- | --- |
| 54 | Full deterministic fixture → native dedup → actual PostgreSQL → evaluator → transports; two owners | Five records: one invalid numeric string rejected; four normalized, duplicate reduced to three. Below threshold did not match; equal/above each produced A Email, B Slack, B Email. All six destinations matched their owner. Four Email and two Slack mock acceptances. Disabled alert excluded. |
| 55 | Repeat full fixture | Three previous keys removed, zero configuration queries or transport attempts. |
| 56 | Email A failure → Slack B → Email C | A invoked exactly five times over 20,113 ms, then discarded. Slack B and Email C each invoked once and accepted by mocks; loop completed. |
| 57 | Inverse Slack A failure → Email A → Slack B → Email B | Slack A invoked exactly five times over 20,073 ms, then discarded. Both Emails and subsequent Slack each succeeded through mocks. |
| 58 | Pinned SQL envelope with no Email, absent destinations, invalid list, invalid mailbox, later valid Email | Slack-only attempt, visible evaluator/preparation rejections, then one valid Email attempt. No accidental Email send. Trigger/HTTP/SQL were pinned; actual evaluator, preparation, loop/router and mocks ran. |
| 59 | Unsupported future channel | Explicitly pinned evaluator output routed unsupported item to diagnostic, then processed one Slack and one Email. Trigger, HTTP, previous-dedup output and SQL were also pinned; this validates router isolation, not ingestion/matching. |
| 60 | Real USGS all-hour fetch through common pipeline | Eleven canonical unique earthquakes; eleven actual configuration-query items; 46 expanded notifications, 23 Slack and 23 Email mock acceptances. Real transports disconnected. |
| 61 | Exact one-recipient preflight through actual PostgreSQL | One test-owned Email notification, correct recipient/prepared message, no Slack attempt; transport mocked. |
| 62 | One native SMTP submission | One accepted recipient, zero rejected, matching envelope, `250 Mail accepted`, `email_accepted`. One new SMTP4DEV capture, exact prepared content. |
| 63 | Repeat real-send event through full entry with transports mocked as safeguard | One key removed by native previous-execution dedup; no SQL or transport invocation. Capture count stayed two (one historical plus one milestone message). |

Temporary test-owned rows: two profiles and three alerts; threshold -100 isolates controlled synthetic sends from unrelated enabled alerts (minimum threshold 4). One profile initially Email-only and the other both channels. The inverse test temporarily gave the first test profile a mock Slack destination. Before the real send, it was Email-only again and the second test alert was disabled. No existing user's configuration was modified.

## SMTP4DEV receipt

The intended existing SMTP credential and historical sender were reused. Before submission, read-only SMTP4DEV inspection confirmed a running capture server/no relay, and a TCP SMTP greeting plus QUIT confirmed connectivity without sending. The no-send preflight verified the actual query produced exactly the intended test recipient. A new event ID was used for the single real send because the preflight event had already crossed deduplication.

Execution **62**, capture **58e212d4-02a8-4926-96a2-bbfec1e49321**: [sanitized capture](2026-09-14-email-channel-smtp-capture.json). Captured MIME was decoded and compared with n8n's prepared subject/body and persisted test recipient. It is plain text, has no HTML or attachments, and contains the alert name, magnitude, UTC time, readable source and validated URL. The capture file contains only controlled reserved test addresses and the synthetic message, not raw transport headers.

**This proves successful SMTP submission and SMTP4DEV message capture. It does not prove external-provider or external-recipient-inbox delivery.**

Existing successful real Slack evidence from [the runtime correction](2026-09-14-runtime-simplification-validation.md) remains applicable: Slack source, credential, transport settings and routing semantics were unchanged. Fresh deterministic/live-source/mixed-channel/failure regression used mocks to avoid unnecessary Slack sends.

## Cleanup and verification

- Temporary mocks removed; native transports restored; operator input reset to live/empty; no schedule added; final graph inactive.
- Exact cleanup removed three alerts and two users. PostgreSQL MCP confirmed original four users/five alerts and six historical migrations. Original complete-row digests match: users `2e23b5cfc4a96bcae91710d30b324e8b`, alerts `6d31ee11ef3b6832589a67945e68c587`.
- No schema/migration, ASP.NET source, package or product runtime-persistence change.
- Guarded `dotnet test Sonrisa.sln --no-restore`: **71 passed, zero failed/skipped** using external ignored configuration. Includes current SQL contract against DEV.
- Generated SDK validated by live n8n MCP: **26 nodes, valid**. New Email nodes individually validated.
- `node --test n8n/tests/*.test.mjs`: **24 passed, zero failed/skipped**, including actual export/source/SQL guards. Generated SDK revalidated after exporter corrections: **26 nodes, valid**.
- Actual remote export passed the sanitizer and byte-for-byte comparison with the tracked artifact. Recursive sensitive-field inspection found no credentials, pins, static data, webhook IDs, passwords, access tokens or execution resume tokens. Workflow source includes only the non-secret sender address.
- Focused Task 1, Task 2 and documentation reviews: **APPROVE**, no Critical/Important findings. Task 2 reviewer independently reran all 24 Node tests and remote-snapshot/export parity; relied on the controller's guarded relational run for database verification. Final whole-branch review: **APPROVE**, no Critical, Important or Minor findings. The final reviewer independently verified the live inactive 26-node graph, credential bindings, baseline node preservation, execution metadata for 56/57/62/63, exact sanitized-export parity and all 24 Node tests.
- The exact staged file set contains only the 29 milestone files listed below; staged diff inspection and `git diff --cached --check` passed. All 16 changed Markdown documents have valid local link targets. The requested milestone commit is ready.

## Final architecture review

1. **Top-level authoritative runtime workflows:** one.
2. **Changes before routing:** only the explicitly approved alert-name SQL projection and envelope pass-through. No selection, evaluation, ownership, ingestion or deduplication behavior change.
3. **Database migration:** none.
4. **ASP.NET runtime code:** none.
5. **Runtime persistence added:** none. Existing n8n native bounded deduplication and execution history remain.
6. **Shared event/matching pipeline:** yes; execution 54 proves match once followed by both destinations.
7. **Failure isolation:** yes; executions 56/57 prove both directions and continuation to another owner's notifications.
8. **Retry ownership:** n8n, five total native attempts and bounded delay; exhaustion discards only the failed notification.
9. **Future Teams branch:** reasonable at this same routing boundary. Teams destination configuration would need a separate product decision because the current profile explicitly has Email/Slack fields; no generic channel registry is claimed.
10. **Extensibility claim:** supported, with the small documented name-projection exception. One added transport branch required no new workflow, runtime table, queue, worker, retry subsystem or duplicated matching logic.

Best-effort limitations remain: ambiguous provider failures can lose or duplicate notifications; native dedup history is bounded and purging/capacity can permit reprocessing; an exhausted notification is not durably recovered. External SMTP provider behavior remains untested. No unattended activation or operational admin UI is included.

## Milestone files

The milestone changes the following 29 files; no application source, migration, package, environment or IDE file is included.

- `AGENTS.md`
- `README.md`
- `docs/01-plan.md`
- `docs/02-assumptions-and-open-questions.md`
- `docs/03-scope.md`
- `docs/04-architecture.md`
- `docs/05-validation-strategy.md`
- `docs/07-runtime-contract.md`
- `docs/ai-review-log.md`
- `docs/decision-log.md`
- `docs/superpowers/plans/2026-09-14-email-channel.md`
- `docs/superpowers/specs/2026-09-14-email-channel-design.md`
- `evidence/reviews/2026-09-14-email-channel-executions.json`
- `evidence/reviews/2026-09-14-email-channel-smtp-capture.json`
- `evidence/reviews/2026-09-14-email-channel-validation.md`
- `n8n/README.md`
- `n8n/build-workflow.mjs`
- `n8n/export-workflow.mjs`
- `n8n/runtime/evaluate-alerts.js`
- `n8n/runtime/prepare-email-message.js`
- `n8n/runtime/record-email-result.js`
- `n8n/sdk/process.sdk.js`
- `n8n/sql/select-enabled-alerts.sql`
- `n8n/tests/email.test.mjs`
- `n8n/tests/process-export.test.mjs`
- `n8n/tests/process.test.mjs`
- `n8n/workflows/process.json`
- `prompts/026-add-email-channel-to-existing-n8n-workflow.md`
- `prompts/README.md`
