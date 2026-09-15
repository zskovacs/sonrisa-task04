Date: 2026-09-15

Purpose: Assess two independent reviews and correct verified defects and applicable recommendations.

An independent agent prepared two reviews. Review these and make corrections where there is an error or a good idea/recommendation:

# Code review: zskovacs/sonrisa-task04

**Reviewed:** `main` at `e667447` (13 commits, all authored 2026-09-14 between 10:25 and 23:49 +0200).
**Specification:** *Task 04, Feature Design & Build from a Vague Brief* (the attached docx).
**Review date:** 2026-09-15.

The spec is explicit that the process is the deliverable and the code is evidence. This review therefore covers both: how the repository holds up against the spec's evaluation criteria, and the technical quality of what was built.

---

## 1. Verdict

This is a strong submission on the axis the spec says it cares about most: visible, specific, verifiable judgment over AI output. The AI review log records real defects that were caught and fixed, the architecture was deliberately simplified after it over-grew, and history was preserved rather than rewritten. The code itself is careful about security and data integrity.

Four things hold it back:

1. **The prompt history was deliberately pruned.** The spec asks for *all* prompts. Commit `9de6b95` deleted 23 records, including user-authored decisions, and the remaining numbering has gaps.
2. **The retrospective was never written.** `docs/final-reflection.md` is a placeholder. The spec's central question, "how you arrived at it", is answered only implicitly across ~110k words of other documents.
3. **The runtime has a built-in expiry date.** The n8n duplicate-history cap of 10,000 will be hit roughly 47 days after activation at today's USGS volume, after which the workflow fails on every run and delivers nothing.
4. **The system is demonstrated, not running.** The workflow is inactive and manual. No user has ever received a notification from the poll loop the product is supposed to provide.

Scorecard against the spec's "What to submit" list:

| Spec requirement | Status | Notes |
| --- | --- | --- |
| Plan of attack, defined before building | Met | `docs/01-plan.md` at `4fca2f1` has 10 milestones with exit criteria. The file was later rewritten in place; the original is only in git history. |
| Commits for each major milestone, meaningful messages | Met | 9 planned milestone commits, conventional style. Two commits share an identical message. |
| Prompt history, all prompts | **Partially met** | Only 14 "substantive user task requests" kept. Agent dispatch prompts, approvals, and several user decisions removed. See §3.1. |
| Plans, decision logs, drafts, intermediate outputs, screenshots | Met, hard to navigate | 12 ADRs, decision log, AI review log, 8 specs, 8 plans, 21 evidence records, 14 screenshots. Volume and hedging boilerplate hurt readability. See §3.4. |
| Deliverables as defined by own plan | **Partially met** | 8 of 9 milestones delivered. Milestone 9 (retrospective) not started. |
| Visible checks, rejections, course corrections | **Strong** | `docs/ai-review-log.md` is the best artifact in the repo. See §3.2. |

---

## 2. What I verified and how

| Check | Result |
| --- | --- |
| Full read of application source, migrations, Razor pages, n8n Code-node sources, SQL, SDK builder, exporter, both test suites, all docs/ADRs/evidence, and the retained prompts | Done |
| `node --test n8n/tests/*.test.mjs` on Node 22.6 | **23 of 24 pass.** The one failure is a portability bug in the test, not in the workflow. See finding M8. |
| `dotnet build` / `dotnet test` | **Not run.** This machine has .NET SDK 8.0.303 only; the project targets `net10.0`. The claimed 83/83 guarded and 64/19 unguarded results are unverified here. |
| n8n executions 64–77, DEV PostgreSQL state, Slack execution 52, SMTP4DEV capture | **Not verifiable** from outside. Assessed for internal consistency only. |
| Live USGS `all_day` feed volume | Measured 2026-09-15: 211 features in 24 h, 210 typed earthquake, 16 with magnitude ≥ 4.5. |
| Git history: commit dates, authorship, the prompt-deletion commit, original plan | Verified. |
| Secret scan of tracked files | No credentials found. Internal hostname `n8n.nasgard.io` and one Slack channel ID are present in docs (not secrets). |

---

## 3. Process review against the spec

### 3.1 Prompt history is incomplete by design

`AGENTS.md §2` and `prompts/README.md` define a policy of keeping only "substantive user-authored task requests" and explicitly excluding "agent-authored prompts, subagent assignments, internal planning messages, internal review requests … approvals, status replies, or decision confirmations."

The spec says: *"your prompt history — all prompts used during the process."*

What was removed in `9de6b95 docs: keep only user-authored task prompts` (472 lines):

- User-authored decisions that shaped the architecture, e.g. `006-confirm-workflow-processing-sequence.md` (the user's own sketch of the pipeline), `011-amend-dev-topology.md` (the shared-DEV decision that became ADR-006), `013-approve-skeleton-design-and-execution.md` (188 lines of user constraints on solution structure and health semantics).
- Controller-to-subagent dispatch prompts, e.g. `015-implement-skeleton-runtime.md`, which is exactly the kind of "how you directed the AI" artifact the spec asks to see.
- Numbering now jumps 002 → 010 → 018 → 019, which signals the gaps but does not explain them.

The 14 retained prompts are also unusual: five of them run 700–1,365 lines and read as full specifications with headings like "Phase 1 — Reconstruct the complete current context" and "Milestone self-review". Nothing records whether these were typed by the author, drafted with AI and edited, or generated wholesale. Either is acceptable, but the spec is evaluating direction of the AI, so provenance matters.

Everything deleted is recoverable with `git show 9de6b95^:prompts/<file>`, but an evaluator will not go looking.

**Recommendation:** restore the deleted records (or add them under `prompts/archive/`), keep the index, and add one line per prompt on authorship (hand-written / AI-drafted / agent-generated).

### 3.2 AI-output validation is the standout strength

`docs/ai-review-log.md` contains about 25 entries. Unlike most such logs, the entries name a concrete claim, the check that disproved it, and the correction. Examples I could confirm against code:

- PostgreSQL `btrim(name)` without a character set trims spaces but not tabs, so whitespace-only names passed the "nonblank" constraint. Fixed by the explicit Unicode trim set now in `AppDbContext.cs:11` and migration `20260914143004`.
- EF's `NpgsqlExecutionStrategy` wraps a refused connection in `InvalidOperationException`; the two management services returned 500 while admin returned 503. Fixed with the `InvalidOperationException { InnerException: DbException or TimeoutException }` pattern now duplicated in three files.
- Razor rendered a hidden boolean as `value="value"`, so Enable could never bind. Caught by real browser testing, not by the existing HTTP tests.
- n8n emitted a synthetic `success=true` item for an empty `UPDATE`, which the relational tests could not expose. Caught in live execution 29.
- The installed n8n 2.38.7 Remove Duplicates node throws at the history cap instead of evicting. Verified empirically in execution 69 with the cap temporarily set to 1.
- The first workflow exporter read an ignored session file, so its tests passed locally but were not reproducible. Rejected and rewritten.
- A transport mock that added diagnostic keys next to `error` was treated by n8n as ordinary output; the retry-exhaustion test was not accepted until the mock matched the real node's error-only envelope.

This is what the spec asks for and it is done well.

### 3.3 The course correction is real, and its cost is not reflected on

The runtime was built twice. `b9cf171` and `e1dc337` (about 6,500 lines added) implemented a three-workflow design with `source_events` and `notification_deliveries` tables, claim/lease semantics, and delivery lifecycle states. `c38f2a3` then removed it (3,417 deletions) in favour of one n8n workflow with native dedup and retry, per ADR-012.

The reasoning in ADR-012 and prompt 025 is sound: the earlier design solved reliability requirements the brief never stated. The history was kept rather than squashed, which is the right call.

But the log does not ask the uncomfortable question. The user's original sketch in the now-deleted prompt 006 was already the simple pipeline. The complexity came from the AI's milestone-2 design (ADR-004) plus the user's own D-002 request for retries and a circuit breaker. Roughly a third of the day went into building and then removing that layer. The place to reflect on that is `docs/final-reflection.md`, and it is empty.

### 3.4 Documentation volume works against the evaluator

Approximate word counts: docs and ADRs 28k, evidence 24k, prompts 36k, superpowers specs and plans 21k. That is on the order of 220 printed pages for a one-day task.

Specific friction:

- The current architecture is only reachable by following supersession chains: ADR-001 → ADR-005 → ADR-011 → ADR-012, each amended with header notes pointing forward. There is no single "current state" page shorter than the README.
- Almost every paragraph carries defensive qualifiers ("this does not prove…", "no claim is made that…"). Once per document is honest; once per paragraph is noise.
- `docs/01-plan.md` was rewritten in place at each milestone, so the plan-of-attack the spec wants to see is at `4fca2f1`, not at HEAD.

**Recommendation:** a two-page "Start here" narrative (brief → scope decision → architecture in one diagram → what runs → what was rejected and why → what is not done) and an immutable copy of the original plan with a dated changelog.

### 3.5 Deliverables versus the brief

| Brief item | Delivered |
| --- | --- |
| Users set up alerts for important world events | One event type (USGS earthquakes), one condition (magnitude ≥ threshold), one alert form. Breaking news and markets explicitly deferred. Reasonable narrowing, well documented. |
| Email and Slack | Both implemented in the n8n workflow. Slack proven by one real send (execution 52); Email proven by SMTP4DEV capture only. |
| Flexible enough to add channels later | Adequate at the workflow routing layer, weak at the data model. See M4. |
| Admin view | Three read-only configuration pages. No delivery or outcome visibility. See M5. |
| Working implementation | Manually demonstrable end to end. The workflow is inactive; there is no schedule; there is no authentication. |

---

## 4. Technical findings

Severity: **High** = will cause loss of the product's core function or blocks the spec's evaluation; **Medium** = real defect or design gap worth fixing before anyone relies on this; **Low** = hygiene, consistency, or small bugs.

### High

**H1. The duplicate-history cap turns into a total outage roughly 47 days after activation.**
`n8n/sdk/process.sdk.js:20` configures Remove Duplicates v2 with `historySize: 10000`, node scope. The repo's own execution 69 confirms that on n8n 2.38.7 the node throws *before filtering* once stored keys plus incoming items exceed the cap. The workflow keys on every normalized earthquake regardless of magnitude. The live `all_day` feed carried 211 features on 2026-09-15, so the cap fills in about 47 days of polling. From then on every execution fails at the dedup node, nothing is evaluated, and nothing is sent, until an operator manually clears history.
`docs/07-runtime-contract.md` and the runbook document the cap and say "review capacity before unattended activation", but no mitigation exists in the design. Cheap options: dedup only events at or above the lowest enabled threshold (16 per day at ≥ 4.5 gives about 600 days), key on `date:source:external_id` with a periodic clear, or schedule the node's clear-history operation.

**H2. A database blip or transport exhaustion permanently loses the notification, and a cheap improvement was not considered.**
The previous-execution dedup node runs before the configuration query and the sends (`process.sdk.js:86`). Executions 70/71 in the evidence show exactly this: a failed query, then a replay that filters the event and never evaluates it. ADR-012 accepts this as "best effort". For a product whose only job is "notify me when something important happens", losing the M 7 notification because PostgreSQL restarted is the worst possible failure, and it is silent to the user. Moving the previous-execution dedup *after* the configuration query would make DB failures retry on the next poll with no new persistence. Transport failure would still lose, but the exposure halves.

**H3. Prompt history pruned.** See §3.1.

**H4. Retrospective not delivered.** See §3.3. `docs/final-reflection.md:3` says "No retrospective conclusions are recorded yet."

### Medium

**M1. Unhandled exceptions are invisible in every log sink.**
`src/Sonrisa.Web/Observability/ObservabilityExtensions.cs:21` sets `Microsoft.AspNetCore` to `LogLevel.None` (and `Program.cs:18-19` does the same for EF and Npgsql). `Program.cs` registers no `UseExceptionHandler` or `UseStatusCodePages`. A category filter without a provider alias applies to console and OTLP alike. The result: any unexpected 500, including the pre-fix `ArgumentNullException` path found in milestone 8, returns a blank page and writes nothing anywhere. The privacy goal (raw URLs in request-start logs) can be met by filtering `Microsoft.AspNetCore.Hosting.Diagnostics` and routing categories only, and by adding a sanitized exception handler that logs the exception type and trace id.

**M2. Three different email validators disagree, and the failure mode is silent per-owner loss.**
- App: `UserNotificationSettingsInputValidator.cs:15-20` uses FluentValidation `EmailAddress()` (single-`@` check in v12) plus `MailAddress.TryCreate`. Accepts `user@localhost`.
- DB: `ck_users_destinations` checks length and trim only.
- Evaluator: `n8n/runtime/evaluate-alerts.js:8` requires a dotted domain and allows a leading-dot local part.
- Preparer: `n8n/runtime/prepare-email-message.js:2` requires dot-atom local part and rejects leading/trailing dots.

A destination the settings page accepts can be discarded at runtime as `invalid_email_destination` or `invalid_email_notification_discarded`. Because destinations are per owner, every email notification for that owner disappears, visible only as an n8n diagnostic. `docs/06-alert-configuration-contract.md` says "Management validates configuration; n8n validates and evaluates actual events" as if they agree. Fix: one shared rule set with the same test vectors on both sides, and the application at least as strict as runtime.

**M3. Sequential retry inside the loop makes an outage self-amplifying.**
`Send Slack notification` and `Send Email notification` use `maxTries: 5, waitBetweenTries: 5000` inside a batch-size-1 loop. Execution 66 measured 20,082 ms for one exhausted item. With N matching notifications during a Slack outage an execution takes about 20 s × N. The proposed schedule is 5 minutes. n8n runs overlapping executions of one workflow by default, and the docs concede previous-execution dedup is not atomic across concurrent runs, so an outage of the transport produces duplicate notifications on the other transport. Mitigations: the workflow-level "prevent concurrent executions" setting, an execution timeout, or short-circuiting a transport after the first exhaustion within a run.

**M4. Channel extensibility is thinner than the brief's requirement.**
Adding a third channel today means: a migration adding a column to `users`, settings UI and validator changes, `evaluate-alerts.js` changes, a new Switch rule, a preparation Code node, a send node, three diagnostic Set nodes, and updates to the exporter's hard-coded node/edge allowlists in `n8n/export-workflow.mjs:6-46` plus the mirrored lists in `process-export.test.mjs`. ADR-012 already admits Teams "needs a separate product-configuration decision". The original schema had a generic `alert_channels(alert_id, channel_type, destination)` table. When the user moved destinations to the user in prompt 020, the design collapsed to fixed columns instead of a `user_channels` table, which would have kept the generic shape at no extra cost.

**M5. The admin view cannot answer "did the alert fire?"**
`/admin`, `/admin/users`, `/admin/alerts` show configuration counts and rows only. The original plan's milestone 8 said "expose enough actual product and delivery state to explain the slice's outcomes." After ADR-012 removed all runtime state from PostgreSQL, the only place to see outcomes is the n8n editor, which the brief's admin would not normally have. This is a defensible MVP trade-off, but the narrowing is recorded as a consequence of ADR-012 rather than as a product decision with alternatives (for example a small n8n-executions read via API, which AGENTS.md forbids).

**M6. No authentication anywhere, admin included.**
Documented in README, ADR-008, and the scope doc, and the app binds to loopback with `AllowedHosts` restricted. Acceptable for this task. It must stay at the top of any "before anyone else uses this" list because the owner UUID shown on every admin row is the only identity the system has.

**M7. Slack and Email show the user different things.**
`prepare-slack-message.js:9` puts the internal alert UUID and the provider event ID in the Slack text and omits the alert name; `prepare-email-message.js:22` does the opposite, by explicit design ("do not expose internal IDs"). `alert_name` is already on the item. A user with both channels sees two different vocabularies for the same event.

**M8. Test portability and reproducibility gaps.**
- `n8n/tests/process.test.mjs:64` passes `new URL(...).pathname` to `execFileSync`. On any checkout path containing a space the path stays percent-encoded and the test fails (23/24 on this machine). Use `fileURLToPath`.
- The guarded .NET suite was run through an untracked helper, `/tmp/sonrisa-correction-tools/run.py`, referenced in four evidence files. The documented environment-variable path exists, but what was actually executed is not in the repo.
- There is no CI. Nothing runs the 64 unguarded .NET tests or the Node suite on push.

### Low

**L1. Culture-sensitive number rendering.** `Pages/Alerts/Index.cshtml:34` renders `@alert.Threshold` with the current culture, while `Edit.cshtml.cs:16` and `Admin/Alerts.cshtml.cs:38` use invariant `"R"`. On a `hu-HU` or `de-DE` host the list shows `5,5` while the form demands `5.5`.

**L2. Triplicated failure guards.** `HasConnection`, `IsDatabaseFailure`, and `LogUnavailable` are copied in `AlertManagementService.cs:152-172`, `UserNotificationSettingsService.cs:83-103`, and `Pages/Admin/AdminPageModel.cs:16-40`. The milestone-8 bug (services lacking a guard admin had) is precisely the drift this invites.

**L3. Inconsistent failure UX.** `Edit.cshtml.cs:13` returns a bare `StatusCode(503)`; `Index.cshtml.cs:25` returns plain-text 409/503 for the status handler. Other paths render the friendly page.

**L4. Hardcoded sender.** `sonrisa@example.test` is fixed in `process.sdk.js:56` and *enforced* by the exporter. Any real SMTP provider will reject a `.test` sender. It should come from an n8n variable or credential.

**L5. Duplicate commit message.** `b9cf171` and `c38f2a3` are both "feat: implement first end-to-end n8n alert workflow". The docs explain this as intentional; `git log` readers will not know.

**L6. Repository noise.** 11,307 lines of vendored third-party n8n skills under `.agents/` (commit `bd40ac8`) next to a `skills-lock.json` that already pins them by hash. `.gitkeep` files remain in `evidence/screenshots/` and `evidence/test-output/` while real screenshots live under `evidence/reviews/`. No `LICENSE`. Three migrations (`FirstRuntimeSlice`, `EnableEmailDeliveryStates`, `RemoveObsoleteRuntimeState`) net to zero and replay a create/drop on every fresh database; keeping them is correct for the shared DEV history, but worth a comment in the README.

**L7. One-hour feed with no cursor.** Any outage longer than 60 minutes silently drops events. The `all_day` feed is the same endpoint family and would give a 24-hour buffer for free; dedup already handles the overlap.

**L8. Missing guard.** `prepare-slack-message.js:5` validates `external_id`, `title`, and magnitude but not `occurred_at`, so `String(undefined)` can reach the message.

**L9. Internal infrastructure in a public repo.** The hosted n8n hostname and a Slack channel ID appear in tracked docs and a prompt. Not secrets, but unnecessary disclosure.

---

## 5. What is genuinely good

- **Security posture for an MVP:** parameterized EF queries, `$1::jsonb` binding in the workflow, Razor encoding with no `Html.Raw`, Slack markdown/mentions/unfurls disabled with escaping, plain-text email with control characters stripped from subject and body, no secrets tracked, provider diagnostics kept out of logs, health-check timeout capped at 5 s on a scoped connection.
- **Data integrity:** database check constraints mirror application validation including a locale-independent Unicode trim set; optimistic concurrency with GUID revisions on both tables; unique-violation on the first profile save mapped to a conflict; `ON DELETE RESTRICT` on the owner FK; ownership scoping tested with forged `OwnerId` form fields and foreign-route probes with valid antiforgery tokens.
- **Workflow as code:** Code-node bodies live in `n8n/runtime/*.js` and are tested directly; the SDK builder assembles them; the exporter re-derives the exact graph, strips credential references, and rejects pins, `executeOnce`, unsafe retry settings, SQL drift, and fixture input; the checked-in export must round-trip through the sanitizer unchanged.
- **Honest evidence:** every evidence record separates mocked, pinned, replayed, and real observations, states what was *not* tested (no restart, no 10,000-entry saturation, no external inbox), and records exact cleanup with row digests.
- **Decisiveness:** the ADR-012 simplification removed real complexity, and the migrations, commits, and evidence of the removed design were kept.

---

## 6. Recommended next steps, in order

1. Write `docs/final-reflection.md`. Include the cost of the runtime rewrite and what would be done differently.
2. Restore the deleted prompt records and note authorship per prompt.
3. Fix H1: either filter before dedup by the minimum enabled threshold, or add a scheduled history clear. Fix H2 by moving previous-execution dedup after the configuration query.
4. Fix M1: replace the blanket `Microsoft.AspNetCore` filter with targeted category filters and add a sanitized exception handler.
5. Fix M2: one email rule set with shared test vectors across the validator and the two Code nodes.
6. Add CI running `dotnet test` (unguarded) and `node --test`, and fix the `fileURLToPath` bug so the Node suite is portable.
7. Add a two-page "Start here" document and freeze the original plan.
8. Consider `user_channels` before a third channel arrives, and decide what the admin view should show about outcomes.

Other agent:

# Code review: `zskovacs/sonrisa-task04` (Sonrisa alerts)

| | |
|---|---|
| Repository | https://github.com/zskovacs/sonrisa-task04 |
| Reviewed commit | `e667447` — `test: validate end-to-end behavior and failure scenarios` (branch `test/end-to-end-validation`; `origin/main` points at the same commit) |
| Brief | `task-04-feature-design-and-build.docx`: alerts for "important world events", email + Slack, room for more channels, an admin view. The evaluators state they grade the process (plans, prompts, decisions, course corrections, validation of AI output), not the code alone. |
| Review date | 2026-09-15 |
| Method | Read every tracked source, test, workflow, doc, ADR, prompt and evidence file; built with .NET SDK 10.0.401; ran the .NET and Node test suites; scanned the full git history for secrets; cross-checked claims in the evidence against code and history. |

## 1. Summary

**Verdict: strong process discipline, honest documentation and competent code, but the product is a manually operated demo rather than a working alerting system, and the prompt history as submitted does not meet the brief's "all prompts used" requirement.**

What exists: a small ASP.NET Core 10 Razor Pages application (alert CRUD for one configured owner, a notification-settings page, three read-only admin pages) on PostgreSQL via EF Core, plus one n8n workflow that fetches the USGS one-hour earthquake feed, normalises it, deduplicates, joins enabled alerts across owners, evaluates `magnitude >= threshold` and routes each match to Slack and/or SMTP email with native retries. Code quality is good, secrets hygiene is verified clean, and the ADR / decision-log / AI-review-log trail is unusually disciplined.

Three things to know before anything else:

1. **The workflow has no schedule trigger and is kept inactive by design.** Nothing notifies anyone unless an operator presses "Execute" in n8n. Across the whole history exactly two real Slack messages and one local SMTP4DEV capture were sent, all from synthetic fixtures; every real-USGS run had the transports mocked or disconnected.
2. **Twenty-two prompt records were deleted** in commit `9de6b95` (including user decisions later reversed by retained prompts), the retained prompts are the finalised English versions sent to the agent, no transcripts exist, and the final reflection is an empty template. The deleted records are recoverable from git history, but the submission as presented is incomplete against the brief.
3. **Locally the .NET suite is 63 passed / 1 failed / 19 skipped**, not the documented 64 / 0 / 19. One telemetry test is environment-dependent: its sentinel `-1` matches any checkout path containing `-1` (mine contained a date).

## 2. Architecture at a glance

| Component | What it does | Key files |
|---|---|---|
| Razor Pages app | `/alerts` list/create/edit/enable, `/settings/notifications`, `/admin`, `/admin/users`, `/admin/alerts`; one configured owner, no authentication | `src/Sonrisa.Web/Pages/**`, `Alerts/AlertManagementService.cs`, `Users/UserNotificationSettingsService.cs` |
| Persistence | `users(id, email_destination, slack_destination, revision)` and `alerts(id, owner_id, name, enabled, revision, one inline typed condition)`; check constraints; optimistic concurrency via `revision`; six migrations including a forward drop of two abandoned runtime tables | `Data/AppDbContext.cs`, `Data/Migrations/` |
| n8n workflow `Sonrisa - Process Alerts - DEV` | manual trigger → USGS fetch or operator fixture → normalise (Code) → split → Remove Duplicates ×2 → Postgres SELECT (one per event) → evaluate (Code) → Loop Over Items (batch 1) → Switch → Slack / Email / diagnostic → back to the loop | `n8n/workflows/process.json`, `n8n/runtime/*.js`, `n8n/sql/select-enabled-alerts.sql`, `n8n/sdk/process.sdk.js` |
| Workflow tooling | build script that inlines Code bodies into an SDK file; export sanitiser that strips credentials and rejects graph drift; Node tests for Code bodies and the exporter | `n8n/build-workflow.mjs`, `n8n/export-workflow.mjs`, `n8n/tests/` |
| Observability | OpenTelemetry logs and request traces, optional OTLP export, aggressive tag stripping | `Observability/ObservabilityExtensions.cs` |
| Ops | application-only Dockerfile and Compose; shared DEV PostgreSQL and hosted n8n are external | `Dockerfile`, `compose.yaml` |

## 3. Compliance with the brief

| Brief requirement | Status | Notes |
|---|---|---|
| Users can set up alerts | Done, narrowly | One configured owner (ADR-008, no authentication). One condition type: earthquake magnitude ≥ threshold. Naming, enable/disable and stale-edit conflicts work and are tested. |
| Get notified "when something important happens" | **Not demonstrated unattended** | Workflow is manual and inactive; 5-minute polling is "future configuration". Real sends in history: Slack executions 27 and 52 (synthetic fixtures), SMTP4DEV capture execution 76. The live-USGS run (execution 60) records `realSends: 0`. |
| Email | Implemented | Native n8n SMTP node, plain text, bounded content. Validated against SMTP4DEV only, never an external inbox (stated honestly). |
| Slack | Implemented | Native Slack node, escaped text, mentions/markdown/unfurls disabled. Two real messages to one test channel. |
| "Flexible enough to add more channels later" | **Weak** | Channels are two fixed columns and hard-coded branches in at least ten places (M2). The first design had a generic `alert_channels(channel_type, destination)` table; it was replaced by fixed `users` columns and the architecture doc now says "Do not introduce a separate channel table for theoretical extensibility". |
| Admin view | Done, read-only | Counts, channel presence, cross-owner alert list. Unauthenticated. No delivery or run visibility (delegated to n8n's own UI). |
| Breaking news, markets, disasters | One source | USGS earthquakes only; other sources are "extension examples". The typed-condition direction (ADR-002) is sound, but the DB check constraint, the UI and the evaluator pin the single condition (M3). |

## 4. Findings

Severity: **High** undermines the brief's core promise or its submission requirements. **Medium** is a real defect or design weakness to fix before further work. **Low** is polish.

### High

#### H1. The alerting pipeline never runs on its own
- **Where:** `n8n/workflows/process.json` line 4 (`"active": false`) and line 9 (`n8n-nodes-base.manualTrigger`, the only trigger); `n8n/README.md` ("Keep it inactive … do not activate unattended polling").
- **What:** There is no Schedule Trigger. The product's only job, noticing an event and telling the user, requires a human to execute the workflow. Evidence confirms it: `evidence/reviews/2026-09-14-email-channel-executions.json` execution 60 (live USGS) has `realSends: 0`; the only real deliveries ever are Slack executions 27 and 52 and the SMTP4DEV capture in `2026-09-14-e2e-smtp-capture.json` (execution 76), all from synthetic fixtures with magnitude −100.
- **Why it matters:** The brief asks for a working implementation of "get notified when something important happens". The docs are honest that scheduling is deferred, but the consequence is that end-to-end behaviour was never observed once with real data and real transport.
- **Suggestion:** Add a Schedule Trigger (five minutes) wired only to the live path (the fixture path must stay unreachable from the schedule, as the README already requires), add an n8n Error Trigger workflow that pages an operator channel, run it for an hour against real USGS with a low threshold, and record that execution as evidence.

#### H2. Silent notification loss is the default failure mode
- **Where:** `Deduplicate previous executions` (`process.json` lines 186–202) runs before `Load owner alert configuration` and before both transports; `n8n/README.md` section "Native history and retry limits"; `docs/07-runtime-contract.md` section "Transport dispatch and failure isolation".
- **What:** n8n records an event as seen the moment the Remove Duplicates node runs. If the PostgreSQL read fails, if five SMTP attempts fail, or if the 10,000-entry history cap throws (documented behaviour of n8n 2.38.7), the affected notifications are gone. Only an execution-data diagnostic remains and nobody is told.
- **Why it matters:** This reversed the user's own earlier requirement. Deleted prompt 004 (`git show 9de6b95^:prompts/004-require-delivery-retries-and-circuit-breaker.md`) says: "I would rather have an important notification sent five times than not sent at all." Prompt 025 later judged the durable design over-engineered. Best-effort delivery can be a legitimate MVP choice, but the decision is buried in ADR-012 and decision D-008, while the README's opening paragraph presents it as a feature ("exhaustion is visible and later notifications continue").
- **Suggestion:** Keep the simple pipeline but (a) add an Error Trigger workflow so exhausted or failed items page an operator; (b) consider marking events as seen only after fan-out succeeds (Remove Duplicates after the loop, or a small `processed_events` key written on success); (c) state the loss semantics in the README's first paragraph.

#### H3. Prompt history and process artifacts are incomplete against the brief
- **Where:** commit `9de6b95` "docs: keep only user-authored task prompts"; `prompts/README.md`; `AGENTS.md` §1–2; `.gitignore` (`.superpowers`, commit `fd2a9e6`); `docs/final-reflection.md`; `evidence/test-output/` and `evidence/screenshots/` (only `.gitkeep`).
- **What:**
  - Twenty-two prompt files were deleted. Seven are genuine user decisions: 003 (persistence and secrets), 004 (retries plus circuit breaker), 005 ("The circuit breaker should be implemented in the n8n workflow … I would not introduce HTTP communication between n8n and the application"), 006 (workflow sequence), 011 (DEV topology), 013 (a 188-line skeleton approval) and 025 ("No, these are not problems. Production will use a normal database user"). The other fifteen are agent-to-agent delegation and review prompts. All are recoverable with `git show 9de6b95^:prompts/<file>`, but the submission's own index hides them, and numbers 019–026 were then reused for unrelated later prompts.
  - After the cleanup no subagent prompt was recorded at all, and several user decisions survive only as paraphrase in evidence ("The user approved the specification in chat").
  - Retained prompts are the finalised English prompt sent to the agent (AGENTS.md §1), not necessarily what was typed; no conversation transcript or AI response is stored anywhere.
  - Milestone 9 (final reflection) was never done; `docs/final-reflection.md` is a three-line placeholder.
  - TRX test output is cited as reviewed evidence but not committed; `evidence/test-output/` is empty.
  - The README never states which AI tooling was used. The evidence reveals OpenAI Codex CLI, a customised `superpowers` skill set (`sp_task_reviewer`, `sp_final_branch_reviewer`), `rtk`, and n8n / PostgreSQL / Rider MCP servers.
- **Why it matters:** The brief says the process artifacts "are the submission" and asks for "all prompts used". The deleted records contain exactly the course corrections the evaluators care about; the 004-versus-025 reversal is the most interesting decision in the whole project.
- **Suggestion:** Restore the deleted records (for example under `prompts/archive/`) with their original numbers, add a short human-written final reflection, commit the two TRX files, and add a "Tooling and process" section to the README.

### Medium

#### M1. One test fails deterministically on checkout paths containing `-1`
- **Where:** `tests/Sonrisa.Web.Tests/ObservabilityTests.cs:134-160`, assertion at line 159.
- **What:** `Malformed_trace_configuration_disables_only_trace_export("OTEL_EXPORTER_OTLP_TRACES_TIMEOUT", "-1")` asserts that no captured log contains the invalid value. The `Microsoft.Hosting.Lifetime` startup entry "Content root path: …" contains the checkout path, so any path with `-1` in it fails the test. Reproduced 3/3 in isolation and 2/2 in full runs. The documented "64 passed" is therefore machine-specific.
- **Also:** those startup lines (content root, environment) are exported through OTLP, which is inconsistent with the stated goal of keeping host details out of telemetry.
- **Suggestion:** use a distinctive invalid sentinel (for example `-424242`) or assert only on the specific warning entry; consider excluding `Microsoft.Hosting.Lifetime` from the OTLP log pipeline.

#### M2. Channel extensibility is hard-coded in at least ten places
- **Where** (what adding a hypothetical Teams channel would touch): `src/Sonrisa.Web/Data/AppDbContext.cs:49` (`ck_users_destinations`) plus a migration; `Users/UserNotificationSettingsInputValidator.cs`; `Pages/Settings/Notifications.cshtml`; `Pages/Admin/Alerts.cshtml.cs:39-45` (`Channels` switch); `Pages/Admin/Index.cshtml.cs` (per-channel counts); `n8n/sql/select-enabled-alerts.sql`; `n8n/runtime/evaluate-alerts.js:30-39` (one inline validator per channel); `n8n/sdk/process.sdk.js` (Switch rule plus four new nodes); `n8n/export-workflow.mjs` (expected node and edge lists); `n8n/tests/process-export.test.mjs:7-86` (the same lists duplicated).
- **What:** The brief explicitly asks for flexibility to add channels. The code treats a channel as two nullable columns and two copy-pasted branches. The docs acknowledge it: "a Teams destination needs a separate product-configuration decision" (`docs/04-architecture.md`).
- **Suggestion:** a `user_destinations(owner_id, channel, destination)` table (or a JSONB map) with a channel-to-validator registry in C#; in n8n, a channel-to-sub-workflow routing table so a new channel is one sub-workflow plus one registry row. Derive the exporter's expected graph from the SDK source instead of hand-maintaining it twice.

#### M3. Event-type extensibility is also on paper only
- **Where:** `src/Sonrisa.Web/Data/AppDbContext.cs:24` (`ck_alerts_codes` pins `earthquake / magnitude / gte / number`); `Alerts/Alert.cs` defaults; `Pages/Alerts/_AlertEditor.cshtml` (no type, field or operator inputs); `n8n/runtime/evaluate-alerts.js:20`; `n8n/runtime/normalize-earthquakes.js` (USGS only).
- **What:** ADR-002's typed-condition idea is right, but a second source (news, markets) requires a migration, UI work, evaluator and normaliser changes; only `gte` exists. Fine for an MVP, but it should be described as such rather than as an "accepted extension boundary".

#### M4. Diagnostics blind spots: whole log categories suppressed, no exception handler
- **Where:** `src/Sonrisa.Web/Program.cs:18-19` (`Microsoft.EntityFrameworkCore`, `Npgsql` set to `None`); `Observability/ObservabilityExtensions.cs:21-22` (`Microsoft.AspNetCore`, `OpenTelemetry` set to `None`); no `UseExceptionHandler` or `UseStatusCodePages` in `Program.cs`; `Pages/Alerts/Create.cshtml.cs:20` and `Pages/Alerts/Edit.cshtml.cs:34` return a bare `StatusCode(500)`.
- **What:** Kestrel's "An unhandled exception was thrown by the application" and OTLP exporter failures are never logged, so an operator cannot see crashes or a dead telemetry backend. Privacy was the motivation, but `LogLevel.None` on a whole namespace is broader than needed.
- **Suggestion:** filter `Microsoft.AspNetCore.Hosting.Diagnostics` and `Microsoft.AspNetCore.Routing` at `Warning`, keep `Error` for the rest, and add a minimal exception handler that logs a sanitised entry with the trace id.

#### M5. Validation logic is triplicated and drifts
- **Where:** Email is validated in `Users/UserNotificationSettingsInputValidator.cs:36-43` (`MailAddress`, rejects a leading `.` and `..`), in `n8n/runtime/evaluate-alerts.js:8` (regex that accepts a leading `.`) and in `n8n/runtime/prepare-email-message.js:2` (a stricter regex). Slack is validated in the validator (line 25), the DB constraint (`AppDbContext.cs:49`), `evaluate-alerts.js:32` and `prepare-slack-message.js:5`. The database-availability helpers `HasConnection`, `IsDatabaseFailure` and `LogUnavailable` are copied into `Alerts/AlertManagementService.cs:152-172`, `Users/UserNotificationSettingsService.cs:83-103` and `Pages/Admin/AdminPageModel.cs:16-47`.
- **Why it matters:** a destination can be accepted by the application, pass the evaluator and be rejected by the email preparer, visible only as a diagnostic inside n8n. Three copies of the availability helper will drift.
- **Suggestion:** one `DatabaseAvailability` helper in C#; one destination contract shared by the C# validator and the JS bodies, tested against the same fixture list.

#### M6. Test coverage gaps around the parts that were hardest to get right
- `src/Sonrisa.Web/Health/PostgresHealthCheck.cs:29-33` (the stalled-handshake timeout cap, a documented bug fix) has no automated test; it was validated manually.
- No test renders `/alerts` with data outside the database-gated suite, which is how L1 slipped through.
- The re-validation branch in `AlertManagementService.SetEnabledAsync` (`Alerts/AlertManagementService.cs:109-115`) is untested.
- `n8n/tests/process-export.test.mjs` re-declares the expected graph (lines 7–86); a wrong edit applied to both files passes.
- Nineteen of the 83 .NET tests need the shared DEV database and nothing in the repo lets a reviewer run them. A disposable PostgreSQL (Testcontainers or a Compose profile) would make the guarded suite reproducible.

#### M7. One configuration query per event and sequential retries
- **Where:** `n8n/sdk/process.sdk.js:21` (`queryBatching: 'independently'`); `n8n/sql/select-enabled-alerts.sql`; Slack and Email nodes with `maxTries: 5`, `waitBetweenTries: 5000`; loop `batchSize: 1`.
- **What:** N events produce N aggregate SELECTs, each returning every enabled alert, and each event may see a different configuration snapshot. A transport outage costs about 20 seconds per notification, serially. Acceptable for tens of earthquakes per hour; not for a news or market feed.
- **Suggestion:** one SELECT before the split (or pass the whole batch as one JSON parameter); consider a parallel branch per channel.

#### M8. Documentation volume hides the system
- **Where:** README (about 180 lines), `docs/00–07`, twelve ADRs, `decision-log.md`, `ai-review-log.md`, eight spec/plan pairs, sixteen evidence files, roughly 10,000 lines of prompts, and 65 vendored third-party skill files (`.agents/skills`, about 700 KB, committed in `bd40ac8`).
- **What:** The same limitations are restated in near-identical wording in the README, docs 02, 03, 04 and 07, `n8n/README.md` and ADR-012. It takes a long time to discover that the runtime is one manual workflow and the application is three pages.
- **Suggestion:** a one-page "what it does / how to run / what is not done" at the top of the README; move the vendored skills out of the tree (`skills-lock.json` already pins them).

### Low

#### L1. Culture-dependent threshold rendering
`src/Sonrisa.Web/Pages/Alerts/Index.cshtml:34` renders `@alert.Threshold` (a `double`) with the current culture, while parsing (`Alerts/AlertInputValidator.cs:19`), the edit form (`Pages/Alerts/Edit.cshtml.cs:16`) and the admin row (`Pages/Admin/Alerts.cshtml.cs:38`) use the invariant culture. On a hu-HU or de-DE host the list shows `6,25` while the editor demands `6.25`. The admin row got an explicit fr-FR test (`tests/Sonrisa.Web.Tests/AdminPagesTests.cs:233-249`); the user-facing list did not. Fix: format with `CultureInfo.InvariantCulture`, or set `<InvariantGlobalization>true</InvariantGlobalization>` in the project.

#### L2. Inconsistent notification content between channels
`n8n/runtime/prepare-slack-message.js:9` includes the internal alert UUID and the raw external id and omits the alert name and source URL; `prepare-email-message.js:22` does the opposite. The runtime contract says not to expose internal ids in user-facing text; the Slack message does.

#### L3. Fragile paired-item access in the email result node
`n8n/runtime/record-email-result.js:3` uses `$('Process each notification').item` inside a run-once-for-all-items Code node. It is correct only because the loop batch size is 1 (which the exporter enforces). `prepare-slack-message.js:5` also skips validating `occurred_at`; `String(undefined)` would print "undefined".

#### L4. Hard-coded sender pinned in three places
`fromEmail: 'sonrisa@example.test'` lives in `n8n/sdk/process.sdk.js:56` and `n8n/workflows/process.json:591`, and `n8n/export-workflow.mjs:154` rejects any other value. A real deployment must edit all three.

#### L5. Repository hygiene
- Four placeholder directories (`infra/`, `docs/diagrams/`, `evidence/screenshots/`, `evidence/test-output/`) never received content.
- `n8n/fixtures/smtp-email.json` is a leftover of the removed three-workflow architecture; the `n8n/run-code.mjs:5` allow-list omits the two email Code bodies.
- `.dockerignore` is an allow-list that must be hand-maintained per new source folder.
- Two commits share the message `feat: implement first end-to-end n8n alert workflow` (`b9cf171`, `c38f2a3`). Deliberate and documented, but `git log` readers will stumble.
- `HasConnection()` logs a warning on every request while the connection string is missing (`Alerts/AlertManagementService.cs:164`).
- Environment identifiers in a public repository: the DEV n8n hostname, a LAN IP (`192.168.0.2`, SMTP spec and evidence), Slack workspace/app/channel ids. Not secrets, but unnecessary.

#### L6. The CSS build is not part of the .NET build
`dotnet run` on a clean checkout serves an unstyled site until `npm run css:build` (the README documents it). A small MSBuild `Exec` target would remove the trap; the Dockerfile already does the right thing.

## 5. Process review (what the brief actually evaluates)

**Commits.** Thirteen commits, all on 2026-09-14 between 10:25 and 23:49 (+02:00), conventional messages, one per planned milestone. Milestone 5 appears twice by design (first runtime `b9cf171`, then the rewrite `c38f2a3`), with the SMTP extension `e1dc337` in between. Only `.env.example` was ever committed; a pattern scan of the full history found no tokens, connection strings or keys.

**Planning.** `docs/01-plan.md` defines nine milestones with exit criteria before any code. The plan was resequenced once (configuration before runtime, decision D-004) with the reason recorded. Eight of nine milestones are done; the retrospective is not.

**Course corrections** (the strongest part of the submission):
- Prompt 020 rejected destinations in `appsettings` and custom validation ("This is completely the wrong direction; we did not agree on this"), leading to ADR-010, a new migration with conversion guards, and FluentValidation.
- Prompt 022 approved "the overall direction, but NOT the design exactly as proposed": it rejected alias tables, an atomic SQL matcher and attempt tables.
- Prompt 025 removed the implemented three-workflow durable-delivery runtime ("Those requirements were introduced by us, not by the product"; "Do not rewrite history to make it appear that the over-engineered architecture never existed"), leading to ADR-012, a forward migration dropping two tables, and archived workflows.
- The AI's fourth-workflow proposal for SMTP was rejected in favour of a branch.
- Deleted prompts 005 and 011 moved n8n to direct database access and to the existing DEV infrastructure.

**AI-output validation.** `docs/ai-review-log.md` records concrete, specific catches with evidence links: PostgreSQL returning `23001` where the AI assumed `23503`; Razor rendering `value="value"` for a boolean; `btrim` not stripping tabs or non-breaking spaces; `NpgsqlExecutionStrategy` wrapping `TimeoutException`; n8n's Slack scope error arriving as a string instead of `error.code`; n8n's empty-UPDATE result shape; the SDK accepting `maxTries=1` that the API rejected; `.dockerignore` leaking `bin/obj`; the `rtk` formatter hiding skipped tests; a "Healthy" readiness masking a superuser, no-TLS credential. These are real.

**Caveats.** The evidence and review logs are written by the agent (a "controller" voice) and the "independent reviewers" are AI subagents. Human judgement is visible mainly in the short prompts (018, 020, 023, 024) and in the deleted decision records. The long prompts (600–1,400 lines each) are uniform, exhaustive checklists that read as AI-drafted; the brief permits that, but it blurs where the candidate's own judgement starts. The user's decision D-002 (retry until delivered, add a circuit breaker) was reversed by the same user in prompt 025; the decision log records it, but the README does not surface the consequence (H2).

## 6. What is good

- Ownership scoping is enforced in every management query, with tests that post forged `OwnerId` fields and foreign ids with valid antiforgery tokens.
- Optimistic concurrency with an opaque `revision` token on both entities, including the first-save race (unique violation mapped to a conflict), all tested against a real database.
- Database constraints mirror application validation, including a Unicode trim set that matches .NET `Trim()` regardless of locale. This was found by review, fixed with a constraint-only forward migration, and regression-tested with direct SQL writes.
- The migration that converted per-alert channels into shared destinations locks both tables, refuses ambiguous data, and its conversion SQL is unit-tested against temporary tables.
- n8n Code bodies live in versioned files, are inlined by a build script, and are tested with `node:test` outside n8n. The export sanitiser strips credential references and rejects graph, SQL, retry-setting and pinned-data drift. The fixture path cannot be spoofed by provider data.
- Untrusted content is handled carefully: Slack escaping with markdown, mentions and unfurls off; email control-character stripping and single-mailbox enforcement; bounded titles and URLs; parameterised JSONB binding.
- Failure handling in the application is consistent: missing, malformed, refused or stalled database yields a generic 503 with no provider details, verified for management and admin pages.
- Telemetry is privacy-first (route template, method, status only), export is optional, and it is tested against a real OTLP receiver.
- Every limitation I found in the code is already written down somewhere in the docs. The honesty is exemplary; the discoverability is not (M8).

## 7. What I verified

| Check | Result |
|---|---|
| Clone, compare branches | `test/end-to-end-validation` = `origin/main` = `e667447` |
| `node --test n8n/tests/*.test.mjs` (Node 22.6) | 24 passed, 0 failed |
| `dotnet build Sonrisa.sln -c Release` (SDK 10.0.401, installed locally for this review) | succeeded, 0 warnings, 0 errors |
| `dotnet test Sonrisa.sln` without `SONRISA_TEST_DATABASE` | 63 passed, 1 failed (M1), 19 skipped; the failure reproduced 3/3 in isolation and 2/2 in full runs |
| Test-count reconciliation | 29 `[Fact]` + 35 `[InlineData]` + 19 `[PostgresFact]` = 83, matching the documented totals |
| Secrets scan over `git log --all -p` | no hits; only `.env.example` ever committed |
| Exported workflow | 26 nodes, `active: false`, no `credentials` or `pinData`, retry settings as documented, one `manualTrigger`, no schedule |
| Screenshots | 14 PNGs exist and match the described pages |
| Deleted prompt records | recovered and read from `9de6b95^` to confirm the quotations above |

Not verified (no access): the 19 database-gated tests, the n8n executions cited in evidence (ids 1–77), the Docker image build (daemon unavailable), live Slack and SMTP behaviour.
