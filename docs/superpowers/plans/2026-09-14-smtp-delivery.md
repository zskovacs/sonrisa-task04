# SMTP Delivery Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development. Execute bounded tasks and review each before continuing.

**Goal:** Add SMTP4DEV email to the existing manually selected delivery workflow.

**Architecture:** PostgreSQL owns durable intent; n8n claims one supported intent and branches to Slack or SMTP. EF owns the small forward constraint migration. Three workflows remain inactive.

**Tech Stack:** Existing .NET/EF Core 10, PostgreSQL, n8n native SMTP Send Email 2.1, JavaScript and Node tests. No new packages.

**Spec:** [Approved SMTP extension](../specs/2026-09-14-smtp-delivery-design.md).

## Global Constraints

- Baseline `b9cf171`; preserve existing work and prior signed commits.
- No automatic send/retry/drain/reset, new tables, SMTP application service or extra workflow.
- Preserve historical Unsupported rows and destination snapshots; all SQL values parameterized.
- Secrets stay in existing n8n credentials; no connection values, secrets or execution dumps in Git.
- Controller alone changes shared DEV schema/workflows after generated migration/SQL review and target confirmation.

## Task 1: Email state and shared claim contract

Files: `src/Sonrisa.Web/Runtime/RuntimeModelConfiguration.cs`, new EF migration/Designer and snapshot under `Data/Migrations/`, `n8n/runtime/evaluate-alerts.js`, generic `n8n/sql/claim-delivery.sql`, existing `select-claimed-delivery.sql`, new `record-email-sent.sql` / `record-email-unknown.sql` / `record-email-invalid.sql`, Node evaluator tests and PostgreSQL runtime/SQL tests.

Consumes existing canonical events and one-condition joined alert envelope. Produces Pending email intent and a shared claim row including `channel`, with zero rows on ineligible/replayed IDs. Keep legacy `claim-slack-delivery.sql` until Task 2 removes/replaces its references. Slack outcome SQL retains its channel guard.

- [x] Add focused failing tests: new email intent Pending/null error, email legal Pending→Processing→Sent and unknown state, legacy Unsupported preserved/unclaimable, Slack unchanged, concurrent email claim has one winner, channel included in immutable read.
- [x] Change constraint by permitting the existing active-state clauses for both supported channels, preserving the old email Unsupported clause. Add a forward migration with no data changes. Down must fail safely if live email states prevent restoring the former constraint; no silent data conversion.
- [x] Implement shared claim as a final SELECT over a conditional UPDATE CTE:

```sql
WITH claimed AS (
  UPDATE public.notification_deliveries
  SET status = 'processing', last_error = NULL
  WHERE id = $1::uuid AND channel IN ('slack', 'email') AND status = 'pending'
  RETURNING id, source_event_id, alert_id, channel, destination
)
SELECT id, source_event_id, alert_id, channel, destination FROM claimed;
```

- [x] Use guarded email outcome updates (`id=$1::uuid AND channel='email' AND status='processing'`): accepted→Sent/now/null; uncertainty retains Processing and writes only `delivery_outcome_unknown`. Read immutable event includes channel for the claimed row. A pre-transport validation rejection writes Failed/`invalid_email_message` with the same id/email/processing guard; this is a definite non-send.
- [x] Run focused Node and .NET checks. Generate/review SQL; controller verifies `sonrisa_dev`, applies intended constraint-only migration and runs guarded PostgreSQL tests. Review Task 1 before Task 2.

## Task 2: Extend the existing delivery graph

Files: `n8n/sdk/deliver.sdk.js`, `n8n/build-workflow.mjs`, `n8n/export-workflow.mjs`, focused new SMTP preparation/result validation code and Node tests, updated SQL tests as required. Export files are controller-owned after remote validation.

Consumes Task 1 shared claim/read and outcome queries. Produces the same deliver workflow ID, renamed, with common claim then explicit channel routing. SMTP branch emits safe acknowledgement checks against exactly one snapshot recipient; unknown results cannot become Sent.

- [x] Add failing tests for email message safety (single bare mailbox, reject CRLF/list/group syntax), synthetic marker/domain IDs and SMTP accepted/rejected/malformed result cases; preserve Slack tests.
- [x] Use the discovered native `n8n-nodes-base.emailSend` 2.1, `resource: 'email'`, `operation: 'send'`, `emailFormat: 'text'`, attribution false, no CC/BCC/attachments, explicit `retryOnFail: false` (omit the unused SMTP maxTries setting; the update API rejects 1), error output wired to ambiguous email outcome. Sender defaults to the non-secret DEV test sender; the explicit ID defaults empty.
- [x] Email preparation returns `{id, valid: false}` for unsupported sender/recipient/message shape without raw destination/error text. An explicit validity branch writes `failed`/`invalid_email_message` using the claimed read ID and never enters SMTP. Valid output includes the checked single destination, sender, subject and text. Test this path through the actual preparation logic.
- [x] Route by the read row's textual channel, with no unsupported-channel fallthrough into a transport. Keep Slack node's tested configuration and credential. Reuse existing claim/read logic; do not introduce a generic dispatch framework.
- [x] Validate SMTP success only when accepted contains exactly the requested single destination and rejected is empty, with a consistent envelope where supplied; otherwise record ambiguity. Use named-node references where branching changes item context.
- [x] Extend exact build/export parity checks for the new branch, send settings, sender, channel predicates and query bindings. No hand-built exports presented as tested remote state.
- [x] Run `node --test n8n/tests/*.test.mjs`, relevant .NET tests/build and task review. Controller validates SDK/native schemas, updates only the existing Sonrisa evaluation/delivery graphs, binds `SMTP account`, inspects actual graph and validates pinned routing/outcome cases.

## Task 3: Controlled integration and handoff

Files: tested `n8n/workflows/evaluate.json` / `deliver.json`, `n8n/README.md`, current docs/ADR status amendments, decision and review logs, `evidence/reviews/2026-09-14-smtp-delivery-validation.md`, prompt index and task status.

- [x] Reconfirm DEV DB, inactive Sonrisa graph, SMTP credential metadata and its actual non-secret host/port (user confirmation if MCP cannot inspect credential fields), SMTP4DEV no-relay status, exact test owner/alert/destination and new synthetic intent. Create only isolated owned test records; never reset prior intents.
- [x] Before fixture evaluation require the Pending event queue to be empty. Choose a finite synthetic magnitude and test threshold below every other currently enabled supported threshold, verifying through the same evaluator that no other alert matches; abort the live test if queue/configuration preflight changes. Do not process older Pending work.
- [x] Feed a new provider-shaped synthetic event through existing normalization/evaluation; inspect Pending email intent and snapshot. Validate one explicit send and SMTP4DEV capture by delivery ID. Repeat from entry and verify no second capture/no transport execution. Disable owned temporary test alert afterward.
- [x] Inspect failed executions without dumping credentials/destinations; validate SMTP unknown path with pinned data and report mock boundaries honestly. Retest Slack branch with pinned transport, not another external Slack send.
- [x] Restore empty delivery ID, export actual final workflows, check exact parity, secrets/pins absence and all three inactive. Run relevant tests, migration consistency, diff/links checks and final custom branch review.
- [x] Update evidence with actual results, amend current milestone sequencing to an SMTP-only follow-up before remaining milestone 6 reliability work, record rejected fourth-workflow proposal and conservative unknown classification. Review exact staged diff. Commit handoff: `feat: add SMTP notification delivery` using existing signing settings, after final checks; Git history records the resulting commit.
