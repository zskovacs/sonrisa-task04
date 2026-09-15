# Final documentation implementation plan

> **For agentic workers:** Execute the documentation tasks in order with focused review and a final `sp_final_branch_reviewer` review. The user's unattended editorial authorization applies; no product implementation is authorized.

**Goal:** Make the repository an accurate, navigable final record of the delivered MVP.

**Architecture:** Preserve the Razor Pages configuration application, configuration-only PostgreSQL and one inactive n8n runtime with Slack/Email. Historical designs remain historical.

**Tech stack:** Existing Markdown/Mermaid, .NET solution, Node workflow checks and read-only Rider/PostgreSQL/n8n tools; no added dependencies.

**Spec:** [Exact user task](../../../prompts/029-finalize-submission.md).

## Constraints and baseline

- Clean tracked tree at `e667447`; current `main` already contains all implementation/validation milestones. The user's accompanying clarification explicitly permits branching from main, overriding the archived task's sequential-branch restriction. `docs/finalize-submission` was created from that HEAD.
- No runtime/source/schema/configuration changes, shared infrastructure mutations, workflow execution/activation or notification sends. Preserve ignored/local files and historical prompts/migrations/commits.
- Stop for a real exposed credential, unsafe history/state, incomplete predecessor or material defect invalidating the MVP. Ordinary editorial choices are self-approved.
- One final commit: `docs: finalize runbook, AI review evidence and final retrospective`. Leave this branch checked out and unmerged.

## Task 1 — Reconstruct and verify the baseline

- [x] Read current docs, all ADRs/specs/plans/prompts, evidence, source/model/migrations/tests, workflow sources/artifact and complete Git history; use independent read-only history and implementation audits.
- [x] Confirm prior milestone completeness, current schema, inactive remote workflow, credential categories, graph/export parity and exact prompt preservation. Keep remote execution data and credentials out of evidence.
- [x] Review this plan before editorial implementation. Record actual findings in final evidence.

## Task 2 — Reviewer entry point and operations

**Files:** `README.md`, new `docs/08-runbook.md`, `n8n/README.md`.

- [x] Reduce README to overview/status, final architecture diagram, actual technology, validated quick start, demo/test flow, limitations and navigation.
- [x] Move applicable setup/health/migration/Docker/telemetry detail into a practical runbook; preserve validated commands and identify historical validation versus this session's checks.
- [x] Reconcile workflow operation/import/rebinding, fixture location, SMTP4DEV versus internet delivery, retries/history and real troubleshooting incidents. Clearly warn that a manual run can send; do not execute one.
- [x] Inspect focused diff and links; obtain task review and fix material findings.

## Task 3 — Reconcile decisions, scope and evidence

**Files:** `AGENTS.md`, `docs/01-plan.md`, `docs/02-assumptions-and-open-questions.md`, `docs/03-scope.md`, `docs/04-architecture.md`, `docs/05-validation-strategy.md`, `docs/06-alert-configuration-contract.md`, `docs/07-runtime-contract.md`, `docs/decision-log.md`, new `docs/adr/README.md` status index and affected ADR status notices, `docs/ai-review-log.md`, `docs/final-reflection.md`, `prompts/README.md`, new `evidence/README.md` and `evidence/reviews/2026-09-15-final-documentation.md`.

- [x] Close stale milestone status and separate implemented, excluded, deferred and unknown scope. Clarify final numeric condition storage and admin/runtime boundary.
- [x] Review every ADR's status and supersession; fix only status/cross-reference ambiguity, preserving historical substance and chronological decisions. No new architecture decision is planned.
- [x] Add navigation to substantial AI corrections, prominently explain the defensible but unnecessary durable runtime design and its simplification. Explain Email's narrow alert-name projection exception.
- [x] Complete the ten-part retrospective from actual history/evidence, add a small evidence index, and distinguish automated, integration, mock, real and unvalidated results.
- [x] Preserve historical prompt bytes; add only prompt 029/index. Inspect focused diff and obtain task review.

## Task 4 — Final verification and handoff

- [x] Run `dotnet tool restore`, `npm ci`, `npm run css:build`, `dotnet build Sonrisa.sln`, and the unconfigured .NET suite with explicit TRX counters. Do not enable guarded relational writes for this documentation-only milestone; cite the previous 83/83 guarded run separately.
- [x] Run `node --test n8n/tests/*.test.mjs`, build live/fixture SDK representations and validate SDK with n8n MCP without execution. Sanitize the read-only remote snapshot and compare with `n8n/workflows/process.json`.
- [x] Check project Markdown paths/anchors (excluding verbatim historical prompt bodies and bundled skill examples), focused tracked-file/secret hygiene and screenshots, historical prompt/migration/artifact preservation, and `git diff --check`. If no repository link checker exists, use a temporary checker and record its scope.
- [x] Record command outcomes and limitations without raw secrets or execution dumps; inspect full diff; obtain final whole-branch review and resolve Critical/Important findings.
- [x] Inspect exact staged diff and hygiene and prepare the requested milestone commit. The commit containing this completed plan records the final handoff; verify clean tracked tree/current branch and report its hash after committing. Do not merge or start another milestone.

## Review status

Plan review approved. Task 1 reconstruction completed without a missing predecessor or material defect. Task 2 review requested restoration of operational details and clearer workflow status/import/version/loss wording; the corrections received APPROVE. Task 3 reconciliation received APPROVE with no findings. Final checks passed within the documented no-write/no-send boundary. Whole-branch review returned APPROVE with no Critical, Important or Minor findings. Create the authorized commit and leave the branch checked out for human review; no further milestone is authorized.
