# End-to-end Validation Implementation Plan

> **For agentic workers:** Use superpowers:subagent-driven-development or superpowers:executing-plans task-by-task. The controller owns DEV mutations, evidence and the single milestone commit.

**Goal:** Verify the completed MVP against its accepted behavior and failure semantics.

**Architecture:** Keep owner-scoped Razor management, read-only cross-owner admin, configuration-only PostgreSQL and one inactive n8n runtime. Add focused regression tests only; fix demonstrated defects minimally.

**Tech Stack:** Existing .NET 10/xUnit/EF/Npgsql, Node tests, n8n 2.38.7, shared DEV PostgreSQL and SMTP4DEV.

**Spec:** [Validation design](../specs/2026-09-14-end-to-end-validation-design.md), with the complete acceptance matrix in [prompt 028](../../../prompts/028-end-to-end-validation.md).

## Global constraints

- Base `eaa588b`; branch `test/end-to-end-validation`; one final commit `test: validate end-to-end behavior and failure scenarios`.
- Preserve all existing configuration rows, migration history, workflow/dedup identities, credentials and native history. No new features, dependencies, services, queues, schemas, authentication or scheduling.
- Test writes use verified `sonrisa_dev` and generated owner/alert IDs with finally cleanup. Runtime tests are serialized with relational tests so fixture configuration cannot leak into sends or aggregate assertions.
- Every temporary workflow change needs a fresh baseline comparison, restoration and retrieved-graph verification. No new top-level workflow. No automatic history reset.
- Each controlled scenario runs under a controller-owned try/finally restoration procedure: save the retrieved baseline and explicit recovery operations outside Git before updates; restore original parameters/edges/transport enablement and remove test nodes on success or exception; retrieve and compare node identities, meaningful graph/settings, inactivity and live/empty defaults. A failed/unproven restoration blocks subsequent scenarios and commit. Keep recovery files available if the tool session itself is interrupted; inspect and restore them before resuming.
- Zero fresh Slack sends when prior success is still applicable; at most one fresh Email capture after a no-send preflight. No unsafe shared failure injection.

## Task 1 — Application and reproducibility validation

**Files:** `tests/Sonrisa.Web.Tests/PostgresPageTests.cs`, focused health tests if needed; existing app source only for a demonstrated defect. Controller owns evidence.

- [x] Run a clean build and existing baseline tests: `dotnet tool restore`, `npm ci`, `npm run css:build`, `dotnet build Sonrisa.sln`, `dotnet test Sonrisa.sln`, `node --test n8n/tests/*.test.mjs`. Capture build warnings and explicit PostgreSQL skips, then run the guarded full relational suite using externally supplied `SONRISA_TEST_DATABASE` and expected name.
- [x] Extend the existing guarded HTTP pattern to create a generated owner/profile/alert, GET the real edit form, POST valid name/threshold/revision with forged `OwnerId`, and verify persisted codes/value/owner/revision. Submit a stale revision and assert conflict plus unchanged persisted data. Submit malformed threshold/blank name and assert errors plus unchanged data. Finally remove only generated rows. Existing admin tests already prove cross-owner counts/privacy and foreign-route rejection; do not duplicate them.
- [x] Run focused changed tests, inspect diff and obtain task review. A failure must be classified before a production fix; no feature expansion.
- [x] Inspect actual schema columns, constraints, relationships, indexes and six migrations; run `dotnet ef migrations has-pending-model-changes --project src/Sonrisa.Web --no-build`. No migration application.
- [x] Start the application on an owned loopback port using external DEV configuration; verify liveness/readiness, management and admin pages. Restart cleanly. Check missing/malformed/refused DB and unavailable/no OTLP using isolated hosts or existing HTTP tests. Use existing capture tests for trace/log correlation and privacy. Npgsql spans remain intentionally absent.
- [x] Verify clean checkout build/CSS path using a disposable tracked-file snapshot outside the working tree, with no secrets or duplicate services. Rider project discovery/build are separate evidence. Inspect browser pages when available; record tooling limits honestly.

## Task 2 — Controlled runtime validation

**Files:** existing `n8n/` source/artifact only if genuine drift/defect is found; sanitized new evidence under `evidence/reviews/`. Temporary instrumentation stays outside committed runtime.

- [x] Enumerate Sonrisa workflows and assert exactly one unarchived primary, inactive; inspect each historical ID and record archived/inactive status. Retrieve historical Slack execution 52/53 and compare the current Slack source, parameters, credential binding, retries and routing contract with its tested snapshot; record that comparison and why no new send is needed. Compare sanitized export against baseline using `node n8n/export-workflow.mjs process /tmp/sonrisa-e2e-baseline-workflow.json`.
- [x] Preflight the installed test/manual execution mechanism. Assume pins do not protect a node unless observed; disconnect native transports before upstream tests. Reuse established native-retry mocks in the existing graph with five attempts/5000 ms/error-output feedback; preserve native transports for restoration.
- [x] Create generated test profiles for Email-only, Slack-only and both, plus enabled/disabled alerts with thresholds below unrelated enabled configuration. Use unique `demo.usgs` fixture IDs through the existing selector/normalizer. Assert below/equal/above, malformed magnitude, disabled alerts, distinct owners, within-batch duplicate and both channel expansion. Use pinned configuration only for schema-unrepresentable malformed/unsupported conditions and label it.
- [x] Repeat full entry with the same IDs: prior keys must be removed and no query/send invoked. A new ID must pass. Fail a transport after dedup and replay: retained seen state may suppress the event; record the accepted loss semantics.
- [x] Prove Email A fails exactly five native attempts (about 20 seconds total waits), then Slack B and Email C continue; prove inverse Slack failure. Use mock results/counters for observation only and record actual execution IDs/timing. Exercise unsupported channel diagnostic followed by valid items and safely simulated DB-read failure. No product runtime state is written.
- [x] Fetch real USGS through the same pipeline with transports suppressed. Compare source IDs, millisecond-to-UTC conversion, titles, finite magnitudes and canonical fields. Confirm installed native-history configuration/version behavior using existing cap evidence plus current version/source inspection; do not stress or reset shared history.
- [x] Preflight exactly one generated Email notification through actual SQL with mocks. Restore native SMTP only, use a fresh synthetic ID, submit once and compare the captured SMTP4DEV plain-text message/recipient with the prepared item. Repeat full entry with a transport safeguard and assert no new capture. Do not claim internet mailbox delivery.
- [x] Remove exact fixture rows; restore native transports, live/empty operator defaults, graph/settings and inactivity. Fetch and sanitize final remote graph, compare with tested artifact, rerun affected Node tests if runtime changed, and obtain task review. Check original row digests and schema remain unchanged.

## Task 3 — Hygiene, evidence, review and commit

**Files:** `AGENTS.md`, `README.md`, `docs/01-plan.md`, `docs/05-validation-strategy.md`, `docs/ai-review-log.md`, current spec/plan, prompt index, and new sanitized evidence. Change other documentation only to correct actual validation findings.

- [x] Inspect tracked files/workflow/evidence/logs for credential patterns and exact known secret values privately; print only counts/paths, never values. Check parameterized SQL, encoded Razor output, plain-text Email safety, destination-free admin and telemetry.
- [x] Record commands/test counts and skips, read-only database inspection, execution IDs and mock boundaries, real USGS/SMTP capture, reused Slack evidence, cleanup, reproducibility, actual defects/corrections and known limitations. Answer all 21 final review questions with evidence references and untested boundaries. Do not invent findings or complete `docs/final-reflection.md`.
- [x] Update milestone status and exact requested commit wording, link evidence, verify prompt body remains verbatim. Check local Markdown links and full diff; run final applicable suites after any code fixes, avoiding redundant unchanged checks.
- [x] Obtain `sp_final_branch_reviewer`, resolve Critical/Important findings, inspect exact staged diff and run `git diff --cached --check`. Commit only milestone files, verify hash/status/current branch, leave `test/end-to-end-validation` checked out without merge or next milestone.

## Completion record

All three tasks completed. Scoped reviews and the final whole-branch review returned APPROVE with no remaining findings. Build passed with zero warnings/errors; the guarded .NET suite passed 83/83 with zero skips and Node passed 24/24. The default .NET run passed 64 with 19 documented relational skips. Final graph export and original database rows remained unchanged after cleanup. [Evidence](../../../evidence/reviews/2026-09-14-end-to-end-validation.md) distinguishes real, mocked and reused checks. The milestone commit is the commit containing this completed plan; no merge or retrospective work follows.
