# First n8n Runtime Implementation Plan

> Historical implementation plan/design: runtime boundaries are superseded by the approved [runtime simplification](../specs/2026-09-14-runtime-simplification-design.md). Preserve this record with its original implementation and validation context.


> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development to implement this plan task-by-task. Steps use checkbox syntax for tracking.

**Goal:** Prove persisted user configuration drives one real earthquake-to-Slack runtime slice.

**Architecture:** n8n normalizes/evaluates; PostgreSQL enforces idempotent source and intent identities. Separate replay-safe writes preserve Pending recovery. EF owns two new tables; manual delivery accepts one explicit ID.

**Tech Stack:** Existing .NET/EF 10, Npgsql, hosted n8n native nodes, plain JavaScript/Node test runner, PostgreSQL 18 DEV. No new package expected.

**Spec:** [Approved simplified design](../specs/2026-09-14-first-runtime-design.md).

## Global Constraints

- Exactly two new runtime tables: source_events and notification_deliveries. No aliases, attempts, tokens, revisions, circuit state, dispatch framework or database matcher.
- n8n owns typed evaluation; application remains management. One earthquake/magnitude/gte/number condition, across all owners. Shared user destinations are snapshotted into unique event/alert/channel intents.
- Three workflows only, all inactive. No automatic delivery scan, retries, email, schedules, auth or UI expansion. Exact selected Slack ID and authorized destination required before a send.
- Preserve existing work in the Rider checkout on feat/first-n8n-alert-workflow. Controller owns docs, shared DEV actions and the single final milestone commit; workers do not stage/commit, spawn agents, read secrets or mutate remote services.
- Values parameterized; EF owns DDL. Never tracked connection strings or credentials. Guard DB tests with existing explicit test target variables; clean only exact fixture IDs.

## Task 1: Runtime persistence and relational safeguards

Owner: sp_integration_debugger. Create `src/Sonrisa.Web/Runtime/SourceEvent.cs`, `NotificationDelivery.cs`, `RuntimeModelConfiguration.cs`; modify `Data/AppDbContext.cs`; generate a new `Data/Migrations/*FirstRuntimeSlice*` migration and snapshot; add `tests/Sonrisa.Web.Tests/RuntimeModelTests.cs`, `PostgresRuntimeTests.cs`.

Interfaces: DbSets SourceEvents and NotificationDeliveries, snake-case tables/columns exactly as spec. Source event data is strictly validated JSONB magnitude, mapped with existing Npgsql facilities; IDs supplied by application/test or PostgreSQL SQL gen_random_uuid. Delivery channel is stable text slack/email. Keep matching out of C#.

- [x] Read spec, existing model and PostgreSQL test guard. Write failing model/relational contract tests, including source/intent uniqueness, malformed magnitude/status constraints, restricted FKs, destination snapshot and delivery transitions. Example core invariant:

```sql
INSERT INTO public.source_events (...)
VALUES (...) ON CONFLICT (source, external_id) DO NOTHING;
-- Repeating the same ID must leave the original magnitude unchanged.
```

- [x] Implement only schema/model. Use status checks and positive JSON object/type checks that cannot pass through SQL NULL; finite float8 magnitude comparison. Exclude code/field revisions and execution history. Use a separate mapping file to keep current configuration model stable.
- [x] Generate forward migration using EF tools without applying. Verify no changes to users/alerts except runtime foreign-key references. Produce SQL script for controller inspection outside tracked artifacts if it contains operational noise.
- [x] Run focused model tests and full local suite with DB cases skipped if unavailable. Report exact red/green commands and migration paths. Controller obtains scoped review before application.

## Task 2: Workflow logic, SQL and inactive remote definitions

Owner: sp_integration_debugger for local `n8n/` source/tests; controller performs live MCP build/validation/deployment. Create `n8n/runtime/*.js` focused Code-node bodies, `n8n/sql/*.sql` fixed parameterized persistence queries, `n8n/tests/*.test.mjs`, `n8n/fixtures/*.json`, and a small reproducible SDK-code generation/export helper if needed. No dependencies; native Node test runner.

Interfaces: normalizer receives trusted `{source, feed}` and outputs `{events, diagnostics}`. Event shape matches source_events columns. Evaluator consumes `{event, alerts}` where each alert contains enabled, id, event_type, condition_field/operator/value_type/value, email_destination, slack_destination; emits `{event_id, intents, diagnostics}`. Each intent is `{alert_id, channel, destination, status, last_error}`. Database queries only persist these decisions.

- [x] Read n8n skill router/capability skills, live SDK and node schemas. Build failing tests that execute exact Code-node bodies with controlled n8n input shims, not reimplemented rules. Fixtures use USGS GeoJSON shape and distinct IDs. Test 4.9/5/5.1 against threshold5, disabled/unsupported/malformed and hostile title/destination strings.
- [x] Implement a small normalizer and evaluator. Never coerce string/null/boolean magnitudes. Unknown configs return diagnostic codes/no match and preserve valid alerts. No arbitrary eval/rule DSL or huge Code-node system.
- [x] Author fixed SQL operations: source insert from parameterized JSON array with ON CONFLICT DO NOTHING; read one pending event; joined candidate snapshot across owners that returns one `{event, alerts}` envelope even for no enabled alerts (COALESCE of JSON aggregation to empty array); delivery insert from prepared JSON array with ON CONFLICT DO NOTHING and one count-summary row; completion update; conditional explicit-ID Slack claim; success/failure writes. Essential claim:

```sql
UPDATE public.notification_deliveries
SET status = 'processing', last_error = NULL
WHERE id = $1::uuid AND channel = 'slack' AND status = 'pending'
RETURNING id, source_event_id, alert_id, destination;
```

- [x] Test zero enabled alerts separately from below-threshold alerts; preserve the selected event through the empty candidate query. Ensure empty intent arrays still yield the completion path. DB errors stop before completion. Preserve first snapshot on duplicates. Message uses immutable source event and IDs, not mutable profile/rule content. No SQL magnitude comparison or enabled-alert business match predicate beyond loading candidates.
- [x] Assemble three SDK definitions with exact live types. Common ingestion path receives real HTTP data or operator-controlled fixtures with reserved namespace. Distinct manual fixture input cannot be reached by future live schedule. Delivery UUID defaults empty; missing ID fails before DB/send. Prefer native nodes and clear notes. Credentials initially unbound; never auto-select an unrelated credential.
- [x] Run `node --test n8n/tests/*.test.mjs`; validate each complete SDK workflow via MCP; controller creates inactive definitions and inspects connections/configuration. Remote execution waits only where credentials are actually needed. Scoped review includes exact source, SQL and serialized graph.

## Task 3: Migration, DEV execution and controlled validation

Owner: controller, bounded integration fixes via custom agents. Files: validation harness/evidence, reviewed fixes, actual sanitized exports `n8n/workflows/*.json`, `n8n/README.md` runbook.

- [x] Review Task1 migration source and generated SQL. Resolve migration connection externally without output; inspect identity using that connection and confirm sonrisa_dev/public product objects. Apply reviewed migration, then inspect actual schema/history. No role/service changes.
- [x] Run guarded relational tests on exact owned fixtures. Prove duplicate inserts, repeated intent writes, partial evaluation recovery, snapshot preservation and overlapping claim/update behavior. Keep the actual n8n queries exercised, not independent equivalent SQL only.
- [x] When DB execution is ready, re-list available credentials; request only missing non-secret credential selection/access. Bind explicit intended credential IDs, inspect resolved nodes. Execute retrieval/normalization first, then actual idempotent inserts/evaluation with controlled user/alert fixtures. Preserve existing alert/user data. Stop on unexpected schema/target changes.
- [x] Verify real USGS execution, same-source duplicate and first snapshot, deterministic below/equal/above/disabled/malformed/unsupported, empty queue/no match, owner-independent processing, duplicate intent and recovery after interruption before completion. Use safe failure injection without shared outages. Record precisely which paths are live versus mocked.
- [x] Once all upstream checks pass, request missing authorized Slack credential/channel only if still absent. Persist intended settings through management where practical and verify selected intent snapshot. Inspect native transport retries; conditional pending claim precedes one real send. Verify one sent record and repeat full delivery entry produces zero calls. Exercise failure classification/ambiguous path with controlled mocks; never intentionally create duplicate external sends.
- [x] Export actual final remote definitions, remove credential bindings/pin data/environment-only metadata reproducibly, verify graph equality and secret absence. Confirm active=false for all three. Run local tests and relevant .NET checks after fixes. Record actual results; no acceptance claim while live-send validation is blocked.

## Task 4: Documentation, final review and milestone commit

Owner: controller, documentation assistance via custom implementer where useful. Files: AGENTS.md current-task wording, README, docs/01-plan.md, 02 assumptions, 03 scope, 04 architecture, 05 validation, 06 configuration contract, new runtime contract, ADR-011, decision/AI-review logs, spec/plan status and evidence/reviews.

- [x] Record user-authorized supersession: n8n evaluation plus replay-safe writes replaces SQL atomic matcher; unchanged IDs deduped, alias resolution rejected intentionally; minimal selected-ID send and unresolved ambiguity; email unsupported; inactive workflows; future backlog activation requires review without a dispatch framework.
- [x] Update current documents consistently while preserving historical ADRs/evidence. Archive exact user correction because it assigns material implementation changes. Document real commands/credentials rebinding, future five-minute polling and first-hour behavior.
- [x] Inspect full diff/export, run local tests and guarded DB/n8n checks appropriate to final code, inspect secrets and scope. Request sp_final_branch_reviewer; fix Critical/Important findings and reverify affected behavior.
- [x] Review exact staged diff, stage only milestone files and create `feat: implement first end-to-end n8n alert workflow` only after accepted validation. No amend, merge, push or next milestone. If runtime config prevents live acceptance, leave work reviewable and report the specific remaining blocker instead of pretending the milestone is done.

## Preflight and process rulings

The user's approval explicitly authorizes specification, reviewed plan and implementation without a second generic gate. Keep spec/plan with the single milestone commit, as repository convention. Work in the existing Rider checkout on a feature branch; baseline was 52 passing/14 guarded skips and clean initial Git state. Task1 schema supplies Task2 SQL; controller may prepare docs/read-only MCP metadata while workers own disjoint local implementation. Task2 and Task3 share n8n artifacts sequentially. No test/code/SQL matching duplication is permitted. All full-result claims require actual execution evidence.
