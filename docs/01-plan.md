# Engineering and delivery plan

## Status and history

Implementation and end-to-end validation completed at `e667447`; final documentation completed at `ce8b76c`. The subsequent [independent-review correction pass](superpowers/plans/2026-09-15-external-review-corrections.md) fixes bounded defects without changing the nine-milestone history. See the [runbook](08-runbook.md), [evidence index](../evidence/README.md) and [retrospective](final-reflection.md).

Git records bootstrap at `4fca2f1`, architecture at `b68e3ad`, skeleton at `5880801`, configuration management at `98b000c`, first runtime at `b9cf171` and the subsequent SMTP extension at `e1dc337`. These implementations and their evidence remain historical facts.

The approved runtime simplification is complete: [ADR-012](adr/ADR-012-use-n8n-native-runtime-state.md) supersedes PostgreSQL runtime queues/delivery persistence and durable recovery requirements. Milestone 6 extends that same workflow with Email under the [approved specification](superpowers/specs/2026-09-14-email-channel-design.md) and [reviewed plan](superpowers/plans/2026-09-14-email-channel.md). Implementation, validation and final whole-branch review completed in commit `a198778`.

The nine-milestone ordering still puts real persisted configuration before the first runtime. This correction deliberately changes milestone 6 from durable-delivery infrastructure to the **required email transport branch** in the same n8n pipeline. Circuits, durable retries, attempt histories and recovery workers are rejected MVP scope, not hidden prerequisites deferred to that milestone. The historical SMTP extension can contribute transport-specific code without restoring its state architecture.

## Current delivery approach

The local Razor Pages application manages owner-scoped alert configuration. PostgreSQL stores users/alerts and shared non-secret destinations; EF owns schema. n8n fetches USGS, normalizes, deduplicates with native history, queries enabled configuration across owners, evaluates the single typed magnitude condition and dispatches channels. One inactive manual workflow proves Slack and Email with bounded retry and continuation after individual failure. The sole upstream exception for Email is an alert-name projection/pass-through for message content; alert selection and matching behavior remain unchanged. No product runtime queue or application processing API is needed.

Read [scope](03-scope.md), [architecture](04-architecture.md), [assumptions](02-assumptions-and-open-questions.md) and [validation](05-validation-strategy.md) together. The roadmap authorizes no additional milestone by itself.

## Phases and acceptance

| Phase | Deliverable | Exit condition |
| --- | --- | --- |
| 1. Clarify product | Original brief, confirmed scope and assumptions. | Requirements distinguished from architecture choices. |
| 2. Architecture | Recorded responsibilities and ADRs. | Reviewed decisions, with later supersessions explicit. |
| 3. Skeleton | Local ASP.NET/EF infrastructure against shared DEV services. | Actual startup/build/readiness and application-only Docker evidence. |
| 4. Configuration management | Owner-scoped alerts, inline typed condition, shared profile destinations, FluentValidation, Razor/Tailwind and OpenTelemetry. | Actual schema, UI, ownership and atomicity validation. |
| 5. First runtime, including current correction | One n8n USGS-to-Slack flow; native limited dedup; all-owner typed matching; five-attempt per-notification retry with continuation; forward removal of rejected runtime tables. | Real source, deterministic cases, actual dedup/retry isolation, one controlled Slack send, safe export and obsolete workflow archival; inactive at completion. |
| 6. Required email channel | Add Email dispatch to the same pipeline; reuse useful SMTP transport logic selectively. | Implemented and validated: both transports use the same best-effort model without queues or a separate delivery architecture. SMTP4DEV capture proves SMTP submission/capture only. Final whole-branch review approved the change. |
| 7. Product admin view | Implemented read-only `/admin` summary, `/admin/users` configuration and `/admin/alerts` cross-owner lists. | Validated counts, ownership boundaries, privacy, empty/failure states and management regressions; no schema or n8n change. Final whole-branch review approved. |
| 8. Integrated validation | Complete deterministic and real-source/channel failure demonstrations. | Matching, native duplicate limits, retry isolation, ownership and secret checks have genuine evidence. |
| 9. Final reflection | Compare implementation to brief and review corrections. | Honest retrospective and final review tied to actual evidence. |

Milestone 8 completed at `e667447` with two narrow management outage corrections and an approved final review; [integrated evidence](../evidence/reviews/2026-09-14-end-to-end-validation.md) records the checks and cleanup. Milestone 9 follows [prompt 029](../prompts/029-finalize-submission.md) and the [reviewed documentation plan](superpowers/plans/2026-09-15-finalize-submission.md). The user allowed branching from current main, which already pointed at `e667447`; the documentation branch is `docs/finalize-submission`. No schedule is activated, and no further milestone is authorized.

## Planned milestone commits

| Milestone | Intended major commit |
| --- | --- |
| 1 | `docs: define scope, assumptions and delivery plan` |
| 2 | `docs: record architecture decisions and system design` |
| 3 | `feat: add application skeleton and local infrastructure` |
| 4 | `feat: add alert configuration model and management UI` |
| 5 | `feat: implement first end-to-end n8n alert workflow` |
| 6 | `feat: add email notification channel to n8n workflow` |
| 7 | `feat: add operational admin view` |
| 8 | `test: validate end-to-end behavior and failure scenarios` |
| 9 | `docs: finalize runbook, AI review evidence and final retrospective` |

The milestone-5 correction used the same user-requested message in a forward commit. Milestone 6 uses `feat: add email notification channel to n8n workflow`, reflecting the required channel extension and rejection of durable infrastructure. Milestone 8 uses the explicitly requested end-to-end validation wording to include application, admin, telemetry and failure coverage; this changes no milestone ordering. Milestone 9 uses the final user-requested wording to include the runbook; only the title changes. Preserve completed history; do not create empty future commits.

## Actual milestone revisions

| Milestone | Completed revision |
| --- | --- |
| 1 — Brief, scope and plan | `4fca2f1` |
| 2 — Architecture | `b68e3ad` |
| 3 — Application skeleton | `5880801` |
| 4 — Configuration management | `98b000c` |
| 5 — Runtime | `b9cf171`, deliberately simplified by forward commit `c38f2a3` |
| Intermediate SMTP extension | `e1dc337`; its runtime-state boundary was later superseded |
| 6 — Email in the final pipeline | `a198778` |
| 7 — Product admin | `eaa588b` |
| 8 — End-to-end validation | `e667447` |
| 9 — Final documentation | `ce8b76c`; [final documentation evidence](../evidence/reviews/2026-09-15-final-documentation.md) |

Milestones 1–8 were committed on 2026-09-14; this final documentation task was requested on 2026-09-15. Tooling/prompt housekeeping commits remain in Git and are not represented as additional product milestones.

## Evidence discipline

Follow [AGENTS.md](../AGENTS.md): exact substantive user prompts, actual review findings and test outcomes, scoped fixtures, no secret values, reviewed migrations and exports, per-task and final review, and exact staged-diff inspection. Documentation is not runtime proof. Earlier first-runtime/SMTP evidence describes the earlier architecture and remains preserved.
