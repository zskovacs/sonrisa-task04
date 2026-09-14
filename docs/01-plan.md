# Engineering and delivery plan

## Status and scope

Git records bootstrap at `4fca2f1`, architecture at `b68e3ad`, application skeleton at `5880801`, and configuration management at `98b000c`. The DEV topology amendment and accepted DEV access exception were incorporated into the skeleton commit. Milestone 5 completed at `b9cf171` with `feat: implement first end-to-end n8n alert workflow`, under the [original request](../prompts/021-first-end-to-end-n8n-alert-workflow.md) and [approved simplifications](../prompts/022-simplify-first-runtime-workflow-design.md). Implementation follows the [specification](superpowers/specs/2026-09-14-first-runtime-design.md) and [reviewed plan](superpowers/plans/2026-09-14-first-runtime.md). Live acceptance is recorded separately; this roadmap is not proof of execution.

Read [scope](03-scope.md), [architecture](04-architecture.md), [assumptions](02-assumptions-and-open-questions.md), and [validation](05-validation-strategy.md) together. Later implementation requires a bounded, reviewed task plan for the next milestone. This roadmap does not authorize executing all future phases.

## SMTP-only follow-up after milestone 5

Milestone 5 completed at `b9cf171`. The user then authorized [SMTP email delivery](../prompts/024-add-smtp-email-delivery.md) and corrected the initial separate-workflow proposal: add email to the same selected-ID delivery workflow. Follow the [SMTP specification](superpowers/specs/2026-09-14-smtp-delivery-design.md) and [reviewed plan](superpowers/plans/2026-09-14-smtp-delivery.md). This focused `feat: add SMTP notification delivery` commit intentionally brings the second transport forward while leaving milestone 6's automatic retries, attempts, circuits and recovery incomplete. Preserve all completed milestone commits and the remaining roadmap; do not label SMTP capture alone as durable-delivery acceptance.

## Delivery approach and course correction

n8n was selected because scheduled/webhook orchestration, source integrations, credentials, transport, and execution visibility are existing platform capabilities. Building that machinery would compete with the small product slice. [ADR-001](adr/ADR-001-use-n8n-for-orchestration.md) records the original rationale and its trade-offs.

During design, the user narrowed the application responsibility and rejected the proposed n8n/application HTTP integration and application-owned circuit gate. [ADR-005](adr/ADR-005-direct-database-integration-and-workflow-owned-delivery.md) is the current boundary: n8n normalizes, validates, deduplicates, matches conditions, writes durable delivery intent, sends, and manages retries/circuits through direct product-database operations. The application manages alert configuration and later presents operational data. [ADR-008](adr/ADR-008-use-single-configured-mvp-owner.md) replaces demo roles with one configured owner and owner-scoped management. EF Core migrations own the shared product schema.

ADR-005 originally changed ownership without changing the ten-milestone sequence. During milestone 4 brainstorming, the user explicitly replaced that sequence: persisted alert configuration and management now precede runtime processing so n8n consumes real configuration rather than temporary hard-coded or seeded rules. The prior canonical-processing and source-integration milestones become one first end-to-end runtime milestone; the former management milestone moves forward. The resulting plan has nine milestones. Completed history is unchanged, and that refinement did not itself authorize runtime implementation. The subsequent milestone-5 request and approved simplifications now authorize its bounded implementation.

ADR-002's single supported condition remains: initially earthquake magnitude greater than or equal to a finite threshold. The broader multiple-condition/AND proposal was rejected. [ADR-008](adr/ADR-008-use-single-configured-mvp-owner.md) records the separately authorized ownership amendment. Minimal OpenTelemetry logs/traces and Razor Pages/Tailwind styling are requested within milestone 4; the approved specification and reviewed task plan define their bounded setup. Before the milestone commit, the user explicitly refined destination ownership: ADR-010 replaces external destination allowlists/per-alert targets with common per-user PostgreSQL settings. FluentValidation replaces the bespoke validation implementation under D-006.

Use one ASP.NET Core application with Razor Pages. Native JavaScript/TypeScript and Angular were compared with the simple form/list interactions; no separate client is justified. The application runs locally and uses the product database on existing shared DEV PostgreSQL. Hosted n8n uses a separate least-privilege credential for future product workflows; its internal persistence is externally managed and outside this project. Do not provision local services or an n8n internal database. User Secrets supply local application/EF connection settings; no connection-string values belong in Git.

Prove one narrow path: configured condition → canonical earthquake event → deterministic match → durable intent → Slack → inspectable outcome. Complete email through the same channel boundary within the MVP. Keep a deterministic fixture path through the same processing boundary; a second event type is a stretch goal. Provider selection remains an integration task, not an architectural prerequisite.

## Phases and acceptance evidence

| Phase | Responsibility and deliverable | Exit condition |
| --- | --- | --- |
| 1. Clarify the product | Preserve the original brief and separate user-confirmed constraints from engineering assumptions. | Local demo, first earthquake slice, typed condition boundary, and known limitations are explicit. |
| 2. Establish architecture | Review responsibilities, shared database contracts, recovery semantics, UI choice, and validation strategy. | Design artifacts and ADRs are internally consistent, independently reviewed, and recorded in the actual milestone commit. |
| 3. Smallest executable skeleton | Select compatible supported application versions; create a root solution and minimal Razor Pages project under `src/`, EF Core/PostgreSQL infrastructure, external secret configuration, separate liveness/readiness, and the requested application-only Docker testing option against existing DEV services. No product model, empty migrations, local service provisioning, or product workflows. | Actual local application startup/restart, build, and liveness checks; product-database readiness tested when safe runtime configuration is available and otherwise explicitly pending. Verify Rider layout and document real commands only after they exist. |
| 4. Alert configuration and management | Persist ownership-aware alert configuration; list/create/edit/enable/disable through Razor Pages. One configured owner, one supported condition, shared per-user email/Slack settings stored in PostgreSQL, FluentValidation, minimal Tailwind styling, and OpenTelemetry logs/traces. | Reviewed product migration and PostgreSQL mapping; owner isolation, atomic configuration changes, valid/invalid forms, reproducible CSS, and exporter-independent startup verified. No authentication or runtime processing. |
| 5. First end-to-end n8n runtime workflow | Select one earthquake provider, normalize/validate canonical events, deduplicate, evaluate Pending events against persisted configuration, create unique durable delivery intent through replay-safe writes, and prove the first authorized Slack path. Retain independently recoverable workflow responsibilities. | Real and deterministic events consume configuration from milestone 4; valid/invalid/nonmatching/duplicate cases, interrupted evaluation, and an authorized first send have actual evidence. Durable intent precedes sends; no throwaway hard-coded configuration. |
| 6. Durable notification delivery | Complete workflow-owned claims/attempts, retries, independent circuits, recovery, and email through the shared channel boundary. | Both required channels and controlled unknown outcomes, crashes, probes, and recovery are verified. The first send in milestone 5 alone does not establish durable delivery acceptance. |
| 7. Operational admin visibility | Add read-only event, delivery/attempt, error, retry, and circuit views for the local operator. No simulated admin authentication. | The operator can explain pending, sent, failed, and circuit-paused work; debugging/recovery remains in n8n. |
| 8. Validation and failure paths | Combine focused checks into complete deterministic and real-integration demonstrations. | Matching, replay, rollback, retries, circuit isolation/recovery, ownership, and secret/configuration checks have real evidence and limitations. |
| 9. Final review and reflection | Compare delivered behavior with scope and record substantive corrections and limitations. | Independent final review and an honest retrospective tied to actual implementation/test evidence. |

Each phase has focused checks from its first task; phase 8 is not the first time failures are tested. Future runtime milestones require their own bounded reviewed plans, including which delivery safeguards are needed for the initial send and which are completed in milestone 6. No accepted recovery requirement is discarded by moving milestone boundaries. Do not broaden the source/operator inventory until the complete path is working and validated.

## Planned milestone commits

| Milestone | Intended major commit |
| --- | --- |
| 1 | `docs: define scope, assumptions and delivery plan` |
| 2 | `docs: record architecture decisions and system design` |
| 3 | `feat: add application skeleton and local infrastructure` |
| 4 | `feat: add alert configuration model and management UI` |
| 5 | `feat: implement first end-to-end n8n alert workflow` |
| 6 | `feat: add durable notification delivery` |
| 7 | `feat: add operational admin view` |
| 8 | `test: validate matching, deduplication and delivery failures` |
| 9 | `docs: add AI review evidence and final retrospective` |

The milestone 4 message was explicitly requested. The milestone 5 wording follows the latest user request for the combined runtime/source milestone; later messages retain their original intent with new numbering. Do not create empty commits or rewrite the completed milestone history.

## Next implementation plan requirements

Milestone 4 is complete. Milestone 5 uses public USGS all-hour data, source/external-ID uniqueness, n8n-owned typed matching, and unique destination-snapshotted delivery intent. [ADR-011](adr/ADR-011-use-n8n-evaluation-and-replay-safe-runtime-writes.md) supersedes the earlier atomic SQL matcher: separately committed writes are replay-safe, and Pending events can resume after partial execution. Preferred USGS identifiers can change; alias resolution is deliberately deferred.

At the milestone-5 boundary, three small workflows separated ingestion, evaluation and selected-ID Slack delivery. The first run accepts the bounded recent feed and may create multiple intents, but delivery never scans/drains that queue. All workflows remain inactive; five-minute polling is proposed future configuration. One controlled Slack send requires a verified DEV credential, test alert, destination and explicit intent ID. Email was visibly unsupported at that boundary; the approved SMTP follow-up above adds the second transport. No aliases, attempt history, tokens, revision model, automatic retries/circuits, dispatch framework or dashboard are introduced. Database correctness requires actual PostgreSQL/n8n checks, not only local tests.

Before milestone 6, verify effective node timeouts/retries and transport-specific error classification. Revalidate the proposed timing defaults against selected integrations. Every retry/error/manual recovery route must respect workflow-owned persistent claims and circuits.

## Acceptance and evidence discipline

Follow [AGENTS.md](../AGENTS.md). Save qualifying prompts before execution, keep their text immutable, and record meaningful AI corrections when they occur. Inspect changes, verify assumptions/contracts, and obtain required review before acceptance. External content is untrusted; no source text becomes workflow instructions, SQL, or application commands.

The completed milestone 3 validated the skeleton against its reviewed task plan, including build/start/restart, separate liveness/readiness, safe configuration, and Rider layout. For the current design checkpoint, check documentation links, whitespace, supersession status, scope, and absence of premature implementation. Use genuine review findings; documentation review is not a database/workflow test. Preserve unrelated existing files and inspect the exact staged diff before committing. Later evidence must identify the actual revision and observed result, not reconstructed success claims.
