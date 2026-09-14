# Runtime Simplification Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development. Execute one bounded task at a time, with focused verification and review before the next task. The controller owns shared DEV changes and the final commit.

**Goal:** Replace the three persistent-state DEV workflows with one manually run USGS-to-Slack workflow backed only by alert/user configuration.

**Architecture:** EF removes the two applied runtime tables in a forward migration while retaining historical migrations. n8n 2.38.7 owns canonical in-flight events, two-stage native deduplication, configuration reads, typed matching, channel routing and bounded Slack retry; email is an explicit unsupported branch until the required next runtime milestone.

**Tech Stack:** .NET/EF Core 10, PostgreSQL, n8n 2.38.7 Workflow SDK and native nodes, JavaScript/Node tests. No new dependencies.

**Spec:** [Approved runtime simplification](../specs/2026-09-14-runtime-simplification-design.md).

**Execution status:** Implementation, validation and documentation completed on 2026-09-14; final whole-branch review approved with no remaining Critical/Important findings. The checklists below preserve the reviewed execution plan. Actual results, including the corrected retry mock and deployed export verification, are in [validation evidence](../../../evidence/reviews/2026-09-14-runtime-simplification-validation.md). The milestone commit follows the final staged-diff gate.

## Global constraints and execution gates

- Baseline is `e1dc337` on `feat/first-n8n-alert-workflow`; preserve committed `FirstRuntimeSlice`, `EnableEmailDeliveryStates`, all other historical migrations, and the prior Slack/SMTP evidence.
- Current applied `source_events` and `notification_deliveries` rows are intentionally lost on the reviewed forward migration. Recheck `sonrisa_dev`, migration head and row counts immediately before applying it; any restricted backup stays outside Git. The controller alone applies it.
- Product tables after migration are `users` and `alerts` plus EF history. Keep existing inline condition and user-owned destinations; no `AlertChannel`, application delivery code, or new persistence workaround.
- Replacement is `Sonrisa - Process Alerts - DEV`, manual and inactive, with `mode=live`/empty fixture defaults. Keep three existing remote workflows inactive until the replacement passes; archive them only afterward.
- n8n 2.38.7 uses native Remove Duplicates twice: current-input `source` + `external_id`, then previous-execution key `source + ':' + external_id`, node scope, history size `10000`. The installed pre-filter count-plus-input cap can throw; no rolling-history or atomic-concurrency claim.
- Slack is the only live transport this milestone. Use `Sonrisa DEV Slack` and authorized channel `C0C1QRQDTSN` for one controlled external send. Slack settings: `retryOnFail: true`, `maxTries: 5`, `waitBetweenTries: 5000`, `onError: continueErrorOutput`. Route exhausted error and success back to Loop Over Items so item B continues after A fails. Email is a required **next** same-workflow branch milestone, unsupported in this one.
- Secrets, connection values, credential bindings, pinned data, and execution payloads must not enter Git. Do not activate polling. No interim implementation commits; after final review make the single focused commit `feat: implement first end-to-end n8n alert workflow`.

## File and responsibility map

| Boundary | Files | Responsibility |
| --- | --- | --- |
| EF configuration model | `src/Sonrisa.Web/Data/AppDbContext.cs`, `src/Sonrisa.Web/Runtime/{RuntimeModelConfiguration,SourceEvent,NotificationDelivery}.cs`, new migration and designer in `src/Sonrisa.Web/Data/Migrations/`, `AppDbContextModelSnapshot.cs` | Remove active runtime entities and applied tables with EF-owned forward history. |
| Workflow logic | Keep `n8n/runtime/{select-ingest-input,normalize-earthquakes}.js`; adapt `prepare-slack-message.js`; add `expand-normalized-events.js`, `evaluate-alerts.js` or rename it in place, and safe diagnostic code as needed | Produce per-event canonical items, fail-closed matches and channel items without DB IDs/intents. |
| Configuration query | Add `n8n/sql/select-enabled-alerts.sql`; remove obsolete runtime SQL after replacement validation | One parameterized per-event read returning the event and enabled all-owner alert array. |
| SDK/export | Add `n8n/sdk/process.sdk.js`; update `n8n/build-workflow.mjs`, `n8n/export-workflow.mjs`; replace `n8n/workflows/{ingest,evaluate,deliver}.json` with `process.json` only after remote validation | Reproducible inactive graph and exact sanitized remote export. |
| Tests | `n8n/tests/*.test.mjs`, `tests/Sonrisa.Web.Tests/{RuntimeModelTests,PostgresRuntimeTests,PostgresWorkflowSqlTests,OwnerAndModelTests}.cs` and any directly dependent test | Keep useful normalization/configuration checks; remove obsolete queue/claim/SMTP runtime checks; add behavior and schema checks that exercise changed contracts. |
| Current docs/evidence | New `docs/adr/ADR-012-*.md`; `docs/{01-plan,03-scope,04-architecture,05-validation-strategy,07-runtime-contract,decision-log,ai-review-log,02-assumptions-and-open-questions}.md`, `n8n/README.md`, root `README.md`/`AGENTS.md` where current-state claims need correction; `evidence/reviews/2026-09-14-runtime-simplification-validation.md` | Supersede conflicting current architecture while preserving chronology and actual test evidence. |

## Task 1: Forward removal of product runtime schema

**Owner:** focused .NET implementer; controller applies shared DEV migration after review.

**Interfaces:** EF model exposes `DbSet<Alert>` and `DbSet<UserNotificationSettings>` only. No workflow reads removed tables after Task 2. Keep all prior migration files byte-for-byte unchanged.

- [ ] Inspect `git status --short`, current EF migration list and `AppDbContext`/snapshot. Record baseline table counts from the existing read-only inspection; do not treat them as the pre-apply count.
- [ ] Add a focused model/migration test that expects only `alerts` and `users` in current EF entity mappings and checks a forward migration drops `notification_deliveries` before `source_events`; run it and confirm it fails against the existing model. Preserve alert/user model tests. Remove the C# runtime table, claim and delivery tests that would refer to deleted entity types or SQL, while retaining useful configuration and normalization-independent tests; do not turn the broad solution test run into a known compile failure.
- [ ] Remove runtime `DbSet`s, `ConfigureRuntimeModel()` and the three active runtime source files. Generate a new named forward migration with repository EF commands, then inspect the generated `Up`/`Down` and model snapshot. The intended `Up` operations are exactly:

```csharp
migrationBuilder.DropTable(name: "notification_deliveries", schema: "public");
migrationBuilder.DropTable(name: "source_events", schema: "public");
```

  `Down` may restore their empty historical schema via EF-generated operations; it must not claim to restore row contents. Check that `alerts`/`users` are unchanged and migration history is untouched.
- [ ] Run `dotnet test Sonrisa.sln --filter FullyQualifiedName~RuntimeModelTests` and `dotnet build Sonrisa.sln`. Generate the idempotent migration SQL with `dotnet ef migrations script --idempotent --project src/Sonrisa.Web` using externally supplied migration configuration; inspect operations and ensure no connection value is printed or tracked. Review this task diff with `sp_task_reviewer`; fix Critical/Important findings.
- [ ] **Controller gate, after Task 2 local checks:** read `current_database()`, the EF history head, and `SELECT count(*)` for each of the two runtime tables using the authorized migration connection; reconcile unexpected new rows or schema drift. Optionally take a restricted backup outside Git. Apply only the reviewed forward migration with `dotnet ef database update --project src/Sonrisa.Web`. Verify `users`, `alerts` and EF history remain, both runtime tables are absent, and the new migration is recorded. If the target or migration SQL differs, stop before applying and repair the plan/review.

## Task 2: Build and locally verify the single workflow

**Owner:** n8n implementer; controller owns remote creation, credentials and live execution in Task 3.

**Interfaces:** `n8n/sql/select-enabled-alerts.sql` takes one JSON argument, `$1::jsonb`, and returns exactly one row `{event, alerts}` per canonical event. The evaluator consumes that row and emits one item per supported destination `{event, alert_id, channel, destination}` plus safe diagnostic items routed away from transport. It never emits a send for malformed or unsupported input.

- [ ] Read `using-n8n-skills-official` and routed workflow, node, expression, Code, loop, error and credential skills. Check installed `get_node_types` with exact discriminators for Manual Trigger, HTTP Request, Remove Duplicates v2, Postgres executeQuery, Loop Over Items v3, Switch, and Slack v2.7 `message/post`. Read the live SDK reference before writing SDK code. Confirm Loop output `0=done`, `1=loop`, `batchSize=1`, `reset=false`, and retry/error-output shapes. Record any drift before implementation.
- [ ] Write focused failing Node tests for the exact normalizer/selector reuse, multiple event items emitted from one feed without truncation, per-event expansion, below/equal/above numeric thresholds, malformed event/configuration and disabled alerts, separate owner/destination expansion, malformed Slack/email destinations producing safe diagnostic skips without suppressing another valid channel, unsupported email/channel diagnostics, safe Slack text, and B continuing after A's exhausted error branch. Run `node --test n8n/tests/*.test.mjs` and confirm relevant failures. Remove Node tests that only assert obsolete event/delivery persistence or SMTP selected-ID behavior; retain useful canonical normalization and configuration tests.
- [ ] Implement one all-owner configuration query using the existing textual columns. The required join and parameter boundary are:

```sql
WITH input AS (SELECT $1::jsonb AS event)
SELECT input.event,
       COALESCE(jsonb_agg(jsonb_build_object(
           'id', a.id, 'enabled', a.enabled, 'event_type', a.event_type,
           'condition_field', a.condition_field,
           'condition_operator', a.condition_operator,
           'condition_value_type', a.condition_value_type,
           'condition_value', a.condition_value,
           'email_destination', u.email_destination,
           'slack_destination', u.slack_destination
       ) ORDER BY a.id) FILTER (WHERE a.id IS NOT NULL), '[]'::jsonb) AS alerts
FROM input
LEFT JOIN (public.alerts AS a JOIN public.users AS u ON u.id = a.owner_id)
       ON a.enabled = true AND a.event_type = input.event->>'event_type'
GROUP BY input.event;
```

  Bind the canonical event with `={{ [JSON.stringify($json)] }}` through Postgres `queryReplacement` parameters, not SQL interpolation. The Postgres node expects an expression array, even for one `$1` argument. Test the **exact query** against guarded PostgreSQL fixtures with two owners, no alerts and disabled alerts; use the repository's `SONRISA_TEST_DATABASE` plus matching `SONRISA_TEST_DATABASE_NAME` guard. Do not reset shared tables.
- [ ] Build `n8n/sdk/process.sdk.js` with a manual live/fixture selector, the same normalizer, per-event expansion, two native Remove Duplicates nodes, the query above, typed evaluator/channel expansion, Loop Over Items v3 batch 1, Switch, Slack, email/unknown diagnostics and return edges. Reuse safe escaping/mention disabling from existing Slack logic. When no configured channel exists, emit no transport item. Make each error diagnostic a bounded code and safe source/alert reference; exclude raw destination and provider payload. Keep the Code node's `runOnceForAllItems` batch semantics when expanding arrays; do **not** set `executeOnce: true`, which would drop later input envelopes. Prove this with the multi-event batch test.
- [ ] Configure the native dedup nodes to compare `source`/`external_id` within the input and `source + ':' + external_id` across executions at node scope and history `10000`. Make the export guard assert operation/fields/key/scope/history and exact graph. The cap throws on `stored_count + input_count > 10000` before filtering; represent this as a tested limitation, not rolling eviction. Keep stable remote workflow and dedup node identities after validation.
- [ ] Validate each configured destination independently before transport: Slack must be a bounded channel ID with no whitespace/control characters; email is a bounded bare mailbox with no header breaks or recipient lists and is still unsupported. Invalid destination produces a diagnostic containing safe identifiers only; it must not suppress a different valid channel. Test malformed values and cross-channel isolation.
- [ ] Configure Slack `retryOnFail=true`, `maxTries=5`, `waitBetweenTries=5000`, `onError=continueErrorOutput`; connect `output(1)` to safe failure diagnostic and then loop feedback, and `output(0)` success to loop feedback. Ensure email and unknown branches also return to feedback. Assert that loop `done` does not feed a transport. Do not wire a top-level schedule or an actual email send.
- [ ] Update `n8n/build-workflow.mjs`, `n8n/export-workflow.mjs` and tests to use `process` and reject obsolete active graphs, temporary fixture values, disabled safety nodes, unsafe Slack settings, credential material, pins, unexpected SQL/code changes or bypass edges. Retire obsolete SDK/SQL/runtime code only when references are gone; do not modify historical exported evidence. Run `node --test n8n/tests/*.test.mjs`, focused guarded PostgreSQL tests and `dotnet test Sonrisa.sln` as relevant. Review the task diff with `sp_task_reviewer`; resolve Critical/Important findings before remote work.

## Task 3: Controlled DEV integration and replacement

**Owner:** controller. Only the controller changes shared DEV DB/n8n or sends to Slack.

- [ ] Run Task 1's pre-apply target/count/history/SQL gate and reviewed forward migration. Then run guarded configuration SQL tests against the verified schema, using generated test-owned IDs and exact cleanup. Verify that the actual n8n PostgreSQL credential reads `sonrisa_dev` and only configuration tables; the observed credential privilege/TLS DEV limitations remain documented under ADR-007.
- [ ] Create/update one inactive `Sonrisa - Process Alerts - DEV` graph from the local SDK. Bind the intended PostgreSQL and `Sonrisa DEV Slack` credentials explicitly. Run `validate_workflow`, fetch `get_workflow_details`, inspect actual connections/settings/defaults and confirm no schedule or webhook. Keep old three inactive. Avoid publishing/activating any graph.
- [ ] Execute a real USGS fetch with transport disconnected or safely mocked and verify normalization, native dedup, configuration read and typed evaluation from the actual execution. Execute deterministic `demo.usgs` fixtures through the same path for threshold below/equal/above, malformed/disabled configs, cross-owner matches, within-input duplicates, later-run duplicates, email/unsupported diagnostic paths and a seen event before a newly enabled alert. Never point synthetic tests at an unrelated live destination.
- [ ] Verify the native history limit with a temporarily small test cap, restored to `10000` afterward, and document the installed-version `stored_count + input_count` throw; do not fill the production node's 10,000 entries. Verify cross-execution seen state with the actual stable node identity. Do not claim concurrent atomic deduplication.
- [ ] Use a controlled temporary mocked transport in the **live n8n execution engine**: make A fail on each attempt, allow B to proceed, and inspect five A invocations, four waits, the error output, safe diagnostic and B branch. A temporary Code mock may count attempts in node-scoped technical test state and throw, provided it is removed and the original Slack node, stable dedup node identity, and graph are restored before export. Pinned Slack output alone does not prove native retry behavior. If engine behavior cannot be observed, record the missing check and do not claim the A/B acceptance condition passed.
- [ ] Preflight one test-owned enabled alert, the authorized `C0C1QRQDTSN` destination, intended credential, USGS/fixture ID and current dedup state. Prove the full entry has exactly one matching unsent event/channel pair before enabling transport; a full manual live run can otherwise send several messages. Perform **one** real Slack send through the replacement, inspect the returned channel/app identity and execution path, then repeat full entry and verify no second send. Do not replay a downstream send node directly. The user already authorized this controlled send; no new permission step is required.
- [ ] Restore live/empty-fixture defaults and real Slack node, validate/fetch the final inactive graph again, and export the actual tested remote workflow through `node n8n/export-workflow.mjs process <snapshot-outside-Git>`. Compare with `n8n/workflows/process.json`. Check credentials, pins, execution data, destination payloads, webhook IDs and connection values are absent. Only now archive the three **inactive** obsolete remote workflows (IDs `K3A9cowlgglFqriG`, `zZYWvZSk1LDGhgJa`, `5fGfgXCBLxhNO479`); do not delete their historical commits or evidence. Record remote IDs, execution IDs, mock boundaries, real result, absence of re-send and final inactivity in evidence. Review integration outcome before Task 4.

## Task 4: Documentation, final review and commit

**Owner:** controller/documentation implementer; final review by `sp_final_branch_reviewer`.

- [ ] Add ADR-012 to supersede ADR-011's durable runtime-state boundary and conflicting durable clauses of ADR-005/D-002, while preserving n8n ownership, direct configuration reads, EF ownership, shared destinations, old approvals and historical SMTP evidence. Correct current-state claims in the mapped docs and `n8n/README.md`: PostgreSQL configuration only; one inactive manual runtime; native limited dedup; best-effort five-attempt Slack; email required in the **next** same-workflow milestone; no queue/circuit/UI. Mark old milestones as historical rather than rewriting their outcome.
- [ ] Record meaningful rejected complexity and accepted limitations in `docs/ai-review-log.md`: applied runtime rows were removed by a reviewed migration, native dedup is bounded/non-atomic, USGS alias identity is deferred, pre-send seen marking can lose a notification, and A/B retry continuation depends on the observed engine test. Add only actual observations and IDs to `evidence/reviews/2026-09-14-runtime-simplification-validation.md`; link them to the tested revision. Update the decision log and open questions without claiming email delivery or unattended polling is complete.
- [ ] Run focused Node, guarded PostgreSQL and .NET checks, then `dotnet test Sonrisa.sln`, `dotnet build Sonrisa.sln`, the export parity check, link/whitespace checks and relevant repository commands. Inspect the full diff for removed obsolete runtime tests versus retained normalization/config tests, unchanged historical migrations, intended schema and docs, safe exports and no secrets. Explain any check that could not run.
- [ ] Dispatch `sp_final_branch_reviewer` with the spec, plan, full diff and exact verification/evidence. Fix every Critical/Important finding and repeat affected checks/review. Inspect `git diff --cached` after staging only intended files, then make the single commit `feat: implement first end-to-end n8n alert workflow`. Report the commit hash, files changed, commands/results, live/mock distinction, remaining limitations and next action: implement email in this workflow branch.

## Self-review before execution

- [ ] Confirm each spec requirement maps to a task: schema/data-loss gate (1/3), single workflow and limited dedup (2/3), cross-owner typed matching (2/3), independent Slack failure continuation (2/3), one real Slack/repeat (3), historical supersession/email-next/commit (4).
- [ ] Scan for placeholders and contradictory active/durable claims. Reconcile any node-schema or remote behavior drift with the controller before dependent implementation; do not silently substitute a persistence workaround.
