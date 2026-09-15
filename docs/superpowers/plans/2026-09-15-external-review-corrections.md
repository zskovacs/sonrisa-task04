# External review correction plan

**Goal:** Verify the two user-supplied reviews of `e667447` against current `ce8b76c`, fix demonstrated defects, and record reasoned dispositions.

**Specification:** The current user requests assessment and correction of valid review findings. This is a bounded correction pass after the completed documentation milestone, not authorization to reverse accepted product decisions.

**Constraints:** Preserve configured ownership, read-only admin, configuration-only PostgreSQL, one inactive manual n8n runtime, and best-effort delivery. No shared DEV writes, workflow execution/import/activation, notifications, schema changes, new dependencies, history rewriting, or restoration of excluded prompts. The initial handoff kept the corrections uncommitted on `docs/finalize-submission`. After review, the user authorized a correction commit, merge into `main` and push; this does not add or renumber a product milestone.

## Task 1 — Destination contract and message validation

- Confirm the application accepts values the Email preparer rejects, and add shared JSON mailbox vectors consumed by .NET and Node tests.
- Use the existing Email preparer's supported ASCII dot-atom/dotted-domain contract (254-character maximum) consistently in the application and evaluator. Retain FluentValidation; test surrounding-space normalization, control rejection, missing optional Email, and independent Slack behavior. No general RFC mailbox implementation or database migration.
- Add missing Slack timestamp validation (parseable string, rendered UTC). Use the available alert name instead of internal identifiers in user-facing Slack text. Like Email, accept the optional name only when it is a string without C0/C1 control characters, collapse whitespace, trim, bound to 120 characters and omit empty/unusable names. Escape Slack markup after cleanup and retain synthetic markers. Test missing/invalid timestamps and malicious, blank and oversized names. Keep source identifiers in diagnostics.
- Synchronize only affected Code-node bodies in the checked-in inactive workflow; pass the existing exporter and Node tests. Clearly distinguish local artifact corrections from the unchanged hosted workflow and historical live evidence.
- Relevant files: `src/Sonrisa.Web/Users/UserNotificationSettingsInputValidator.cs`, destination validator tests, shared fixture under `tests/fixtures/`, `n8n/runtime/evaluate-alerts.js`, `n8n/runtime/prepare-slack-message.js`, `n8n/tests/`, `n8n/workflows/process.json`.

## Task 2 — Safe unexpected-error diagnostics

- Reproduce the missing unexpected-exception diagnostic with an in-process test endpoint and a synthetic sensitive exception message.
- Add a small exception boundary before application request handling. Return generic 500 content and log only a fixed event, exception type and generated trace correlation; never log the exception object, message, stack, URL, query or destination. Preserve privacy filters rather than enabling raw framework exception logging.
- Suppress hosting-lifetime details in exported telemetry. Fix the malformed `-1` configuration assertion to inspect the actual configuration warning rather than unrelated startup text; retain a deliberate checkout/content-root collision in the regression.
- Cover the sanitized error with capture/OTLP tests and ensure existing known 503 paths and trace privacy still pass.
- Relevant files: `src/Sonrisa.Web/Program.cs`, `src/Sonrisa.Web/Observability/`, `tests/Sonrisa.Web.Tests/ObservabilityTests.cs`.

## Task 3 — Portable local checks and threshold rendering

- Reproduce Node builder failure from a copied path containing spaces, then replace URL `.pathname` with `fileURLToPath` in `n8n/tests/process.test.mjs`.
- Render owner alert thresholds with invariant `R` formatting, consistent with editing/admin. Inspect the exact invariant formatting expression and compile the Razor page; this one-line display correction does not justify new database fixtures or a test that merely repeats the formatting expression.
- Add a minimal GitHub Actions workflow for the documented .NET/Node/CSS checks, read-only repository permissions, and no database/transport secrets. Local command results and hosted CI execution must be reported separately.
- Relevant files: `n8n/tests/process.test.mjs`, `src/Sonrisa.Web/Pages/Alerts/Index.cshtml`, `.github/workflows/ci.yml`.

## Task 4 — Review reconciliation and verification

- Produce `evidence/reviews/2026-09-15-independent-review-corrections.md` with a row for every numbered finding in both reviews (related findings may share a row with both IDs), stating validity, disposition, evidence and rationale for deferral. Include stale retrospective/navigation claims, the actual 22 deleted prompt records, historical numbering reuse, and the unverified original DOCX requirement. Link the original plan and deleted history directly without fabricating provenance or restoring excluded records. Retain/index the qualifying current request as `prompts/030-assess-independent-reviews.md`, preserving both English review bodies verbatim and translating only the user's Hungarian framing.
- Improve README prominence of best-effort loss/capacity and add tooling/process context. Record the cost/lesson of the runtime rewrite without inventing person-hours. Retain current source/channel/admin limitations as explicit decisions.
- Defer schedule/error workflows, automatic history clearing, dedup reordering, durable state, channel schema redesign, disposable database infrastructure and broad refactoring. Explain specific trade-offs, including why date keys/history clears allow repeats and why a handled per-item error does not necessarily trigger a workflow error handler.
- Run focused red/green checks per task, inspect each task diff and obtain task review; run full unguarded .NET and Node suites, build, artifact validation, whitespace/link/hygiene checks and final `sp_final_branch_reviewer` review. Record exact counters; skipped relational tests and unexecuted hosted runtime remain explicit.

## Progress

- Baseline: clean `main` and `docs/finalize-submission` both at `ce8b76c`; switched to the existing documentation branch. Original Node process tests: 8/8 passed in the normal path.
- Plan review: initial review requested explicit safe Slack-name rules and an auditable per-finding matrix/prompt contract. Both requirements were added; scoped re-review returned APPROVE.

- Task 1 complete: shared vectors and Slack corrections, 27/27 focused .NET and 30/30 Node, local builders/export parity passed. Task review APPROVE with no Critical/Important findings. Hosted workflow unchanged.

- Task 2 complete: safe exception handling, Lifetime OTLP filter and path-independent warning regression. ObservabilityTests 14/14 passed; scoped review APPROVE after strengthening the positive export assertion.

- Task 3 complete: Node URL decoding, invariant Razor expression, pinned CI and explicit no-credential setup. Alternate-path Node suite 30/30; Release build and YAML structural assertions passed. Task review APPROVE.
- Combined verification caught one Task 1 wording regression (68 passed, 1 failed, 19 skipped). Restored the existing mailbox error message without weakening the rendered-page test; focused rebuilt Release check passed 1/1. Final combined rerun: 69 passed, zero failed, 19 guarded skips (88 total). Node 30/30, CSS/build/export/link/whitespace checks passed. Final whole-change review APPROVE, with no findings.

- Task 4 complete: every supplied numbered finding has a recorded disposition, current/historical evidence is separated, and final reviewer independently confirmed the corrected scope and checks. The initial handoff left corrections uncommitted on `docs/finalize-submission`; hosted/guarded verification is explicitly deferred. The user subsequently authorized committing, merging and pushing the reviewed corrections to `main`.
