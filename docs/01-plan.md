# Engineering and delivery plan

## Status and scope

The architecture baseline was completed on 2026-09-14 at `b68e3ad`; the original repository milestone is `4fca2f1`. The current task implements milestone 3, the smallest executable application skeleton. Its DEV topology is amended by [ADR-006](adr/ADR-006-use-existing-shared-dev-infrastructure.md) following implementation-environment discovery. The root solution and single Razor Pages skeleton have been validated under the [reviewed task plan](superpowers/plans/2026-09-14-application-skeleton.md). No product model, migration, or executable product workflow exists. Host and container readiness passed; [ADR-007](adr/ADR-007-accept-current-dev-database-access.md) records the accepted current DEV access/transport exception.

Read [scope](03-scope.md), [architecture](04-architecture.md), [assumptions](02-assumptions-and-open-questions.md), and [validation](05-validation-strategy.md) together. Later implementation requires a bounded, reviewed task plan for the next milestone. This roadmap does not authorize executing all future phases.

## Delivery approach and course correction

n8n was selected because scheduled/webhook orchestration, source integrations, credentials, transport, and execution visibility are existing platform capabilities. Building that machinery would compete with the small product slice. [ADR-001](adr/ADR-001-use-n8n-for-orchestration.md) records the original rationale and its trade-offs.

During design, the user narrowed the application responsibility and rejected the proposed n8n/application HTTP integration and application-owned circuit gate. [ADR-005](adr/ADR-005-direct-database-integration-and-workflow-owned-delivery.md) is the current boundary: n8n normalizes, validates, deduplicates, matches conditions, writes durable delivery intent, sends, and manages retries/circuits through direct product-database operations. The application manages conditions/roles and presents operational data. EF Core migrations own the shared product schema.

This correction changes ownership within the planned milestones, not their sequence or commit messages. In particular, milestone 4 implements the canonical processing/evaluation workflow with deterministic input; milestone 5 adds the real source adapter. Milestone 4 needs the minimum Pending delivery record for atomic matching; milestone 6 completes transport, attempts, retries, and circuit behavior. Do not treat the presence of a Pending record as completion of durable delivery.

Use one ASP.NET Core application with Razor Pages. Native JavaScript/TypeScript and Angular were compared with the simple form/list interactions; no separate client is justified. The application runs locally and uses the product database on existing shared DEV PostgreSQL. Hosted n8n uses a separate least-privilege credential for future product workflows; its internal persistence is externally managed and outside this project. Do not provision local services or an n8n internal database. User Secrets supply local application/EF connection settings; no connection-string values belong in Git.

Prove one narrow path: configured condition → canonical earthquake event → deterministic match → durable intent → Slack → inspectable outcome. Complete email through the same channel boundary within the MVP. Keep a deterministic fixture path through the same processing boundary; a second event type is a stretch goal. Provider selection remains an integration task, not an architectural prerequisite.

## Phases and acceptance evidence

| Phase | Responsibility and deliverable | Exit condition |
| --- | --- | --- |
| 1. Clarify the product | Preserve the original brief and separate user-confirmed constraints from engineering assumptions. | Local demo, first earthquake slice, typed condition boundary, and known limitations are explicit. |
| 2. Establish architecture | Review responsibilities, shared database contracts, recovery semantics, UI choice, and validation strategy. | Design artifacts and ADRs are internally consistent, independently reviewed, and recorded in the actual milestone commit. |
| 3. Smallest executable skeleton | Select compatible supported application versions; create a root solution and minimal Razor Pages project under `src/`, EF Core/PostgreSQL infrastructure, external secret configuration, separate liveness/readiness, and the requested application-only Docker testing option against existing DEV services. No product model, empty migrations, local service provisioning, or product workflows. | Actual local application startup/restart, build, and liveness checks; product-database readiness tested when safe runtime configuration is available and otherwise explicitly pending. Verify Rider layout and document real commands only after they exist. |
| 4. Canonical ingestion and matching | Implement the supported event/condition contract, minimal seeded configuration, Pending/Evaluated processing, and atomic unique delivery-intent creation in n8n/PostgreSQL. | Controlled valid/invalid/nonmatching/duplicate events and concurrent or interrupted evaluation produce the specified records without external sends. |
| 5. n8n source integration | Select one suitable earthquake provider and add polling/normalization against the tested canonical contract. | Validate identifiers, missing values, feed window, quotas, unavailable-source handling, exports, and real ingestion. RSS/news remains out of MVP. |
| 6. Durable delivery | Implement workflow-owned due-work claims, attempts, email/Slack transport, retry backoff, independent circuits, and recovery. | First source-to-Slack slice works; email uses the same intent contract; crashes, unknown outcomes, circuit probes, and recovery have actual results. |
| 7. Alert management UI | Add Razor Pages forms/lists and the explicit local demo identity selector. Validate conditions/destinations and ownership. | Create/edit/enable/disable a user alert and observe its effect on later evaluation; existing intent snapshots remain unchanged. |
| 8. Operational admin | Add read-only views over events, deliveries/attempts, errors, next eligibility, and circuit state. | An operator can explain matched/nonmatched, pending, sent, failed, and circuit-paused work; detailed workflow inspection remains in n8n. |
| 9. End-to-end failure validation | Combine focused checks into the complete deterministic and real-integration demonstrations. | Matching, replay, transaction rollback, retries, circuit isolation/recovery, role checks, and secret/configuration checks have real evidence and limitations. |
| 10. Review and reflection | Compare actual behavior with this scope and record substantive corrections and limitations. | Final review and an honest retrospective tied to actual implementation/test evidence. |

Each phase has focused checks from its first implementation task; phase 9 is not the first time failure paths are tested. Use ordinary reviewable commits within a milestone when useful. Do not broaden the source/operator inventory until the complete path is working and validated.

## Planned milestone commits

| Milestone | Intended major commit |
| --- | --- |
| 1 | `docs: define scope, assumptions and delivery plan` |
| 2 | `docs: record architecture decisions and system design` |
| 3 | `feat: add application skeleton and local infrastructure` |
| 4 | `feat: implement event ingestion and alert matching` |
| 5 | `feat: add n8n source workflows` |
| 6 | `feat: add durable notification delivery` |
| 7 | `feat: add alert management UI` |
| 8 | `feat: add operational admin view` |
| 9 | `test: validate matching, deduplication and delivery failures` |
| 10 | `docs: add AI review evidence and final retrospective` |

Only milestone 3 belongs to the current task. Its topology amendment refines the development environment without changing the ten major commit messages or their sequence. Do not create empty commits or start later product milestones after the skeleton.

## Next implementation plan requirements

Before implementing milestone 3, specify the root `.sln` and application project location under `src/`, supported application versions, local application startup, existing DEV product-database access, separate least-privilege roles, external configuration lookup for application/EF tooling, and application restart checks. Do not manage or validate hosted n8n internal persistence, restart shared services, choose an event provider, or send live notifications to validate the skeleton. Read-only capability checks do not establish product workflow integration.

Before milestone 4, finalize the bounded canonical payload and parameterized SQL contract, immutable first-snapshot semantics, one-statement rule snapshot, unique identities, and atomic event-completion/intent writes. Verify these with PostgreSQL; an in-memory EF test is not evidence of shared-database correctness.

Before milestone 6, verify effective node timeouts/retries and transport-specific error classification. The proposed demo defaults in the architecture must be feasible for the selected nodes; revise a default explicitly if evidence requires it. Every retry/error/manual recovery route must respect workflow-owned persistent claims and circuits. Configure and validate credentials before authorized test sends.

## Acceptance and evidence discipline

Follow [AGENTS.md](../AGENTS.md). Save qualifying prompts before execution, keep their text immutable, and record meaningful AI corrections when they occur. Inspect changes, verify assumptions/contracts, and obtain required review before acceptance. External content is untrusted; no source text becomes workflow instructions, SQL, or application commands.

For milestone 3, validate the skeleton against its reviewed task plan, including build/start/restart, separate liveness/readiness, safe configuration, and Rider layout. Check documentation links, whitespace, decision status, ownership consistency, scope, and absence of product implementation. Use genuine review findings; documentation review is not a database/workflow test. Preserve unrelated existing files and inspect the exact staged diff before committing. Later evidence must identify the actual revision and observed result, not reconstructed success claims.
