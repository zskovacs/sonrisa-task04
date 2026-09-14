# Email Channel Implementation Plan

> **For agentic workers:** Use superpowers:subagent-driven-development; execute bounded tasks with focused review. The controller owns remote mutations, sends and the final commit.

**Goal:** Add Email to the existing inactive runtime and prove transport extensibility.

**Architecture:** Existing one-item loop and channel Switch gain a native SMTP branch. Only upstream exception is alert-name projection/pass-through; no matching, source, deduplication, schema or ASP.NET behavioral change.

**Tech Stack:** Existing n8n 2.38.7, Send Email v2.1, JavaScript/Node tests, configuration-only PostgreSQL. No dependencies.

**Spec:** [Approved Email design](../specs/2026-09-14-email-channel-design.md).

## Global constraints

- Baseline `c38f2a3`; workflow `aVijfnQr0kdLAJHP`; preserve node/workflow identities and inactive state.
- SMTP4DEV is approved; one real Email, zero real Slack sends. Native five-total-attempt retry with 5000 ms waits and separate error output.
- No migrations, ASP.NET changes, runtime persistence, new workflows, schedules or packages.
- Preserve all preexisting user configuration; temporary test-owned rows only, exact cleanup.
- Source/control artifacts omit credentials, pinned data, raw execution data and addresses in diagnostics. One final milestone commit only.

## Task 1: Implement local Email branch and guards

**Files:** `n8n/sql/select-enabled-alerts.sql`, `n8n/runtime/evaluate-alerts.js`, new `n8n/runtime/prepare-email-message.js` and `record-email-result.js`, `n8n/sdk/process.sdk.js`, `n8n/build-workflow.mjs`, `n8n/export-workflow.mjs`, `n8n/tests/process.test.mjs`, `n8n/tests/process-export.test.mjs`, new Email-focused tests. Controller updates `n8n/workflows/process.json` only after remote validation.

**Interfaces:** SQL adds `'name', a.name` beside alert ID; evaluator notification shape adds `alert_name: alert.name`. Preparation consumes `{event, alert_id, alert_name, channel, destination}`, emits `{destination, subject, text}` with paired item lineage. Result projection consumes SMTP result plus `$('Process each notification').item.json`, emits safe `{channel:'diagnostic',code,source,external_id,alert_id}`. Invalid preparation throws a fixed code into its explicit error output. No raw error/address reaches diagnostic output.

- [x] Write/run failing tests: user-facing alert name, control/header injection, invalid single mailbox/recipient lists, optional source URL, synthetic marker, no UUIDs in message, strict accepted/rejected/envelope results, both owner channels, empty/malformed channels.
- [x] Add only the name projection/pass-through. Preserve the evaluator's comparisons and all other source code. New preparation/result helpers follow the current source-insertion pattern for exact-source testing and bounded validation; no duplicated matching.
- [x] Replace `Record unsupported email diagnostic` with `Prepare safe Email text` → `Send Email notification` → `Record Email result`; preparation error → `Record invalid Email diagnostic`; SMTP error → `Record exhausted Email diagnostic`. Return all three terminal diagnostics to `Process each notification`. Keep existing Switch/Slack nodes unchanged.
- [x] Send parameters: `{resource:'email',operation:'send',fromEmail:'sonrisa@example.test',toEmail:'={{ $json.destination }}',subject:'={{ $json.subject }}',emailFormat:'text',text:'={{ $json.text }}',options:{appendAttribution:false}}`. Bind credentials remotely only. Apply retry/error settings from the spec.
- [x] Extend builder placeholders and exporter exact node/edge/code/SQL/parameter guards for Email. Reject hardcoded recipient, unsafe options, retry bypass, active state, pins, temporary fixture input, SMTP/Slack wiring drift. Test the new graph against a local candidate constructed in test code, clearly not a remote export. Defer the checked-in artifact parity assertion until Task 2 replaces the actual export; all Task 1 applicable tests must pass. The current existing exporter tests already construct their own synthetic snapshots.
- [x] Run `node --test n8n/tests/*.test.mjs`; validate emitted SDK through live MCP; inspect diff and have `sp_task_reviewer` review before remote implementation. Fix Important/Critical findings.

## Task 2: Controlled remote integration and receipt

**Owner:** Controller. Uses Task 1 SDK/code and the existing authoritative workflow, not a new workflow. Temporary instrumentation is removed before export.

- [x] Fetch fresh remote version, credentials metadata and product schema/count/digests; compare baseline identity and Slack nodes. Read-only SMTP4DEV metadata and SMTP greeting verify intended endpoint without sending.
- [x] Atomically update only name projection/pass-through and Email branch. Explicitly bind SMTP credential; validate and fetch actual graph. No publish.
- [x] Use credential-free temporary mocks with native retries in this workflow. Return exactly `{error:'controlled_transport_failure'}` for designated item A on every invocation, irrespective of counters; B/C return known acceptance shapes. Count mock invocations using the already proven manual-execution instrumentation from milestone-5 execution 44: `$getWorkflowStaticData('node')` is in-memory within this manual run and is not saved as workflow static data for manual executions. Counts observe calls only; they never decide retries or drive orchestration. Return a count summary on the later successful mock item and clear those properties immediately; remove the mock afterward. No custom retry loop or persistent state exists. Verify this lifecycle against installed engine source and runtime output. A stateless mock alone cannot prove invocation count because native retries are grouped into one node execution record.
- [x] Run fixtures through the same normalizer/dedup/SQL/evaluator path; use temporary configuration rows across two owners and separate pinned malformed config/unknown-channel tests. Test missing Email, both destinations, thresholds, repeated key filtering and real USGS without live sends. Record node counts/outcomes, not raw payloads.
- [x] Prepare one test-owned email-only profile/alert and a unique low-magnitude synthetic event below unrelated thresholds. Prove exact one eligible recipient with transport mocked; use a fresh event ID for the real run. Recheck recipient/credential/sender and SMTP4DEV capture count.
- [x] Restore native Email, keep Slack safely suppressed for controlled validation, execute full entry once, inspect intended acceptance and SMTP4DEV captured MIME/content. Repeat entry, verify dedup prevents another SMTP invocation/capture. Never retry a downstream send manually.
- [x] Restore all native transport nodes, remove mocks/pins, reset operator to live/empty, retain inactive state, clean exact test rows and verify original configuration digests. Export actual graph via sanitizer; compare repository and remote parity including unchanged Slack/source/dedup identities. Run Node suite again.

## Task 3: Documentation, final review and commit

**Files:** affected root `README.md`, `AGENTS.md`, `docs/{01-plan,02-assumptions-and-open-questions,03-scope,04-architecture,05-validation-strategy,07-runtime-contract,decision-log,ai-review-log}.md`, `n8n/README.md`, new `evidence/reviews/2026-09-14-email-channel-validation.md`, this plan and spec. Preserve historical evidence/ADRs and prompt 026 exactly.

- [x] Document second transport, configuration ownership, native retries/discard, inactive workflow, SMTP4DEV-only receipt proof, meaningful actual corrections and alert-name-only upstream exception. No new ADR; retain rejected durable architecture history. Update milestone status/message to the user's exact requested commit.
- [x] Evidence answers all ten final architecture questions: one primary workflow; name projection only upstream; no schema/application/runtime persistence; same pipeline; failure isolation; native retries; future Teams branch feasible with destination configuration extension; extensibility supported with explicit exception.
- [x] Run exact Node suite, affected guarded SQL query tests and .NET suite as available, SDK validation, export parity, secret-sensitive artifact inspection, Markdown links and `git diff --check`. Report skips honestly. No broad repeat without new changes.
- [x] `sp_final_branch_reviewer` approved the full milestone diff/spec/plan/evidence with no findings. Applicable checks and the exact staged-diff review passed. Prepare the requested commit `feat: add email notification channel to n8n workflow`.

## Plan review and execution status

Preflight review identified the need to separate local candidate graph tests from the final remote-export assertion and clarify mock instrumentation lifetime. Both are explicit above. Task 1/2 share source/graph contracts; Task 2 owns the actual export and receipt evidence. Task 3 only documents verified Task 2 outcomes. All required tests, failure paths and prohibited boundaries map to Tasks 1–3. Fresh editor-saved default omission/layout is preserved; canonicalize only verified equivalent default representations in export guards.

Tasks 1–3 implementation, documentation, verification and review are complete. The final reviewer approved the change with no findings. The staged milestone is ready for the user-requested commit; Git history records its resulting hash.
