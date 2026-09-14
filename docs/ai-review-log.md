# AI review log

## 2026-09-14: Email export compatibility and safety guards needed narrow correction

The first Email exporter guard rejected a valid n8n-saved Email node because n8n added a node `webhookId` and omitted default `resource`/`operation` fields. The correction was limited to the known transport metadata and verified default values. The exporter strips credential references and generated metadata, treats omitted defaults as equivalent, and still rejects unsafe transport parameters, pins and routing drift. A failing regression case was added before the correction; the final Node suite passed 24/24. [Email validation](../evidence/reviews/2026-09-14-email-channel-validation.md) records the resulting graph and checks.

Controller review also hardened the generated Email helpers before remote use: synthetic subjects are marked, a single mailbox is validated more strictly, optional source URLs are safely constrained, and diagnostics avoid recipient/raw-error exposure. These were implementation corrections, not changes to the matching model or durable runtime architecture.

## 2026-09-14: Execution inspection was narrowed to sanitized observations

An initial broad execution-history inspection returned unnecessary execution metadata, including a resume-token field. The controller replaced broad response capture with selected node/field projections and retained only sanitized counts, diagnostics and controlled synthetic SMTP4DEV capture in tracked evidence. No token values were retained in tracked files; this correction concerns execution-data minimization, not a finding that SMTP credentials were exposed.

## 2026-09-14: Replace duplicated runtime infrastructure with native n8n behavior

The approved and implemented first runtime used three workflows plus PostgreSQL source-event and delivery-intent state; SMTP later reused its delivery boundary. The latest review rejected that architecture because durable queues, recovery and delivery history solved requirements absent from the product brief and duplicated n8n responsibilities. The new accepted boundary is configuration-only product persistence and one n8n runtime with native deduplication, matching, dispatch and bounded best-effort retries. Earlier approvals and validation remain historical facts, not mistakes erased from history. [ADR-012](adr/ADR-012-use-n8n-native-runtime-state.md) records the supersession; [inspection evidence](../evidence/reviews/2026-09-14-runtime-simplification-inspection.md) establishes the committed/applied baseline.

Source inspection caught two important limits before implementation: previous-execution filtering does not remove duplicates inside the current batch, and n8n 2.38.7 can throw when stored history plus incoming items exceeds its cap. Use two native operations and document bounded history. Do not replace this accepted limitation with a product ledger. USGS preferred-ID instability remains correctly identified, with alias resolution deliberately deferred.

The user corrected the proposal's terminal Slack failure: one destination exhausting retries must not stop unrelated notifications. Use a batch of one, native bounded retry, and an explicit failure-output feedback edge. Plan review also required independent destination validation, so an invalid email setting cannot suppress a valid Slack channel. These corrections require actual tests; the inspection alone does not establish runtime success.

Actual execution 44 proved five failed mock attempts followed by the next owner's notification and later items. The first mock had added diagnostic keys alongside `error`, which n8n treated as ordinary output; it was corrected to match the installed Slack node's error-only envelope before accepting the test. The production retry graph did not need another subsystem. Execution 52 then performed the single authorized real Slack send; full re-entry 53 made no second send. The reviewed forward migration removed the obsolete tables, and configuration digests were unchanged after exact test cleanup. The old workflows were archived after replacement validation. [Validation and precise mock/live boundaries](../evidence/reviews/2026-09-14-runtime-simplification-validation.md).

Export review also rejected an unnecessary dependency on remote node-array order. The actual SDK serializes the same graph in recursive order and adds non-secret Slack webhook metadata. The sanitizer now verifies the exact node set/edges independently of array order and deliberately strips that metadata only from the known Slack send node; regressions cover both accepted remote differences and rejected unsafe metadata.

Final review required the decision log's individual rows to mark the earlier durable-delivery and three-workflow choices explicitly superseded, rather than relying only on a new header. This was corrected without erasing their original decisions. Controller review also restored four useful source-only validation tests removed alongside obsolete persistence tests. Final review approved the result with 12 passing Node tests, 71 passing guarded .NET tests and actual remote export parity.


## 2026-09-14: Live repeat entry exposed n8n's empty-UPDATE result

After the single acknowledged Slack send, full re-entry correctly updated zero delivery rows and made no second send. However, n8n emitted a generic `success=true` item for the empty UPDATE result, and the next query failed because no ID existed. Relational tests alone had not exposed this native-node adaptation. The claim keeps the same atomic UPDATE/predicate/parameter inside one CTE and returns explicit columns through a final SELECT. Live execution 29 then completed with an empty claim result and no downstream transport. Final suites passed 80/80 guarded .NET and 20/20 Node tests; actual remote exports were refreshed. No new retry logic or historical-state reset was introduced. [Actual failure and correction](../evidence/reviews/2026-09-14-first-runtime-validation.md#final-runtime-acceptance).

## 2026-09-14: Native Slack errors invalidated a structured-code assumption

The initial known-rejection IF inspected only `error.code`. Live execution 23 instead exposed n8n's missing-scope rejection as a plain string, incorrectly leaving its intent Processing with an unknown-outcome diagnostic. Final review classified this as Important: conservative no-resend behavior did not make the delivery-state classification accurate. The existing IF now recognizes a bounded exact allowlist of native rejection strings and structured codes, while timeout/unknown/misleading text stays ambiguous. No broad text matching, retries or extra workflow nodes were added. A red/green regression and pinned n8n executions 24/25 verified known-rejection and timeout branches; all PostgreSQL/Slack calls were mocked in those tests. The normal claim path was restored and its actual export refreshed; all 20 Node tests passed. The two earlier Processing records remain untouched. See [review and execution evidence](../evidence/reviews/2026-09-14-first-runtime-validation.md#whole-branch-review-correction).

## 2026-09-14: Workflow export tests depended on ignored session files

The first exporter read a hardcoded ignored snapshot path, and its tests required those session artifacts. That passed locally but could not provide reproducibility from a fresh checkout. The controller rejected this dependency. The corrected CLI accepts an explicit snapshot path or stdin; tests load tracked sanitized workflows and add synthetic sensitive metadata in memory to verify its removal. The same Node suite passed 17/17 after correction. The runbook also scopes the below-threshold no-match result to the test-owned threshold-5 alerts: the pre-existing alert had legitimately matched under its own configuration. [Actual validation and limits](../evidence/reviews/2026-09-14-first-runtime-validation.md#export-reproducibility-review).

## 2026-09-14: User simplified the first runtime design

- The user approved USGS and the bounded manual slice but rejected alias-aware identity, a complex atomic SQL matcher, extra revision/attempt metadata and the manual-only dispatch framework. Preserve the useful USGS identifier discovery without treating its proposed solution as approved.
- The accepted replacement is same-source/external-ID uniqueness, n8n-owned typed evaluation, separate idempotent intent writes and completion, and one explicit-ID Slack delivery workflow. Pending replay remains recoverable, but configuration can change between evaluations; there is no event-wide atomic rule snapshot. The destination on an existing intent remains immutable.
- ADR-011 records this explicit architectural correction. No aliases, claim tokens, attempt tables or dispatch_mode column are authorized. Full automatic retry/circuit recovery and email remain deferred; unresolved delivery ambiguity must be reported honestly.
- The exact user-authored correction is [prompt 022](../prompts/022-simplify-first-runtime-workflow-design.md). This entry supersedes the earlier pre-approval recommendations below; it is not an implementation success claim.

## 2026-09-14: First runtime investigation exposed identity and backlog assumptions

- Context: pre-approval brainstorming for milestone 5; no runtime implementation has begun.
- Finding: USGS documents that the preferred event ID can change. Two successful feed reads with repeated IDs do not establish immutable identity. The proposed alias-aware database boundary must explicitly bound inputs, avoid automatic merges of ambiguous canonical records, and disclose that previously disjoint provider associations can still describe one physical occurrence.
- Independent review correction: manually evaluating the first bounded feed can leave notification intents for later delivery. Keeping today's schedule inactive alone does not prevent tomorrow's delivery workflow from draining that backlog. The revised proposal includes persisted manual-only dispatch and an explicit release/disposition gate before future automation.
- Status: provider/alias handling and the limited delivery safeguards await user approval. No new architectural decision, migration, workflow or send is claimed. The existing plan already requires atomic unique delivery intent in milestone 5; full automatic retry/circuit recovery and email remain milestone 6.
- Evidence: [read-only context reconstruction](../evidence/reviews/2026-09-14-first-runtime-context-review.md).

Use this log for material AI output that was rejected, substantially corrected, based on a false assumption, unnecessarily complex, changed after validation, or accepted only after a specific verification step. Do not log trivial edits to create activity. Entries must describe work that actually happened.

## Entry format

For each real example, record:

- Date, milestone, and the affected artifact/revision.
- Link to the relevant prompt record, when prompt recording applies.
- The original claim, output, or approach under review, without secrets.
- The actual finding and its severity or significance.
- Disposition: rejected, corrected, or accepted after verification; explain why.
- The specific check or authoritative reference used, its actual result, and an evidence link when available.
- The correction and any remaining limitation or follow-up.

Keep original evidence and subsequent corrections distinguishable. Add follow-up notes rather than silently changing a past finding. Routine check output belongs in `evidence/test-output/` or `evidence/reviews/`; reference it here only for a material example.

## 2026-09-14: Generated-output exclusions were too narrow

- Context: repository-tooling follow-up after milestone 1. This did not complete a new product milestone.
- Initial AI output: `.gitignore` excluded `coverage`, `logs`, and `artifacts` only at the repository root. The initial checks covered root outputs but missed the corresponding nested project paths.
- Finding: the final reviewer identified an Important gap. `git check-ignore --no-index -q` returned exit code 1 for `tests/Sample.Tests/coverage/coverage.cobertura.xml`, `tests/Sample.Tests/logs/test.log`, and `tests/Sample.Tests/artifacts/report.xml`, confirming that these generated paths remained trackable.
- Correction: remove the root-only restriction and add directory-specific exceptions beneath `evidence/` for deliberately retained reports. Keep secret-file exclusions active inside the evidence directories.
- Validation: an inline Python loop invoking `git check-ignore --no-index -q` passed 43 ignored-path and 32 trackable-path cases after the correction. These included nested outputs, evidence in original output directories, and private environment/credential files beneath evidence. No probe files were created. The project-owned staged whitespace check passed.
- Disposition: corrected and accepted after the focused re-review. The reviewer returned `APPROVE_WITH_MINOR_NOTES`, independently verified 11 ignored and 12 trackable paths, and reported no remaining Critical or Important findings. Its only Minor note was inherited formatting in the imported skills. No runtime or skill-behavior testing is claimed.
- Separate inherited observation: the full staged whitespace check reports nine trailing-space lines and twelve blank lines at EOF in the imported skill files. Those supplied files are preserved byte-for-byte; the full check is not represented as passing.

## 2026-09-14: First-slice proposal did not explain the extension boundary

- Context: milestone 2 design discussion following [the architecture request](../prompts/002-design-mvp-architecture.md).
- Initial AI proposal: start with earthquake events, one user-configured magnitude threshold, Slack delivery first, and email within the MVP. The explanation did not show how this domain-specific rule would fit a reusable application.
- Finding: the user accepted the initial slice but challenged the risk of hardcoding earthquake logic and requiring separate business logic for every new source. This exposed a gap in the design explanation, not a defect found in implemented code.
- Design check: reason through adding a second earthquake provider versus adding news events. Another provider for the same event type should require source normalization against the existing contract; a new event type may require a new validated payload/field contract and rule capability. Both should reuse event acceptance, deduplication, and durable delivery orchestration.
- Correction under discussion: separate provider identity from canonical event type; use a common envelope with validated type-specific data and a small rule boundary over explicitly supported typed fields/operators. Keep magnitude out of the shared processing flow. Do not replace the missing boundary with unrestricted JSONPath, arbitrary expressions, or a promise that every new domain can be added without code changes.
- Disposition: initial explanation corrected; the detailed event/rule design still requires agreement and later validation. This was a conceptual extension walkthrough; no code, prototype, or runtime tests were produced.
- Follow-up: the user accepted the clarified extension boundary. [ADR-002](adr/ADR-002-canonical-events-and-typed-alert-conditions.md) records the decision; exact integration contracts and implementation validation remain pending.

## 2026-09-14: A delivery circuit must govern the n8n transport call

- Context: milestone 2, after the user required a circuit breaker and preferred retrying uncertain email/Slack sends. The delivery preference is recorded in [the decision log](decision-log.md).
- Alternatives evaluated during design: a breaker around an application HTTP client, a breaker scoped to a single workflow execution, and unconditional retry on the n8n send node.
- Finding: the application does not make the external transport call, so its HTTP-client breaker would not observe Slack/email failures. Per-execution state would not coordinate separately scheduled retries. Send-node retries could bypass durable attempt accounting and any gate applied only when claiming work.
- Correction proposed: coordinate durable attempt eligibility and per-transport circuit state at the application API; let n8n perform one external send per authorized attempt and report the outcome. Keep Slack and email failure domains separate. [ADR-004](adr/ADR-004-durable-delivery-and-circuit-breaking.md) records this proposal and its added state/coordination cost.
- Validation performed: traced the actual planned call boundary and compared it with [Microsoft's circuit-breaker guidance](https://learn.microsoft.com/en-us/azure/architecture/patterns/circuit-breaker) and [n8n's Retry On Fail behavior](https://github.com/n8n-io/n8n-docs/blob/main/docs/integrations/builtin/handle-rate-limits.md). This is a conceptual design check; no resilience package, workflow, or prototype was implemented or tested.
- Disposition: reject the three alternatives as the authoritative delivery protection. The proposed durable coordination still requires final architecture review and later failure/concurrency tests. No claim of duplicate-free external delivery is made.
- Subsequent user correction: the application-owned coordination proposal was rejected. The circuit breaker must live in n8n workflows, and n8n must access the product database directly without application HTTP endpoints. [ADR-005](adr/ADR-005-direct-database-integration-and-workflow-owned-delivery.md) supersedes ADR-004's allocation. The earlier analysis correctly distinguished transport calls from application HTTP calls, but overreached by assigning the solution to application services. Workflow-owned durable state and database contracts must now be designed; no previous implementation or tests are claimed.

## 2026-09-14: New-event branching is insufficient for crash recovery

- Context: the user's proposed workflow sequence supplied a new-event branch followed by matching, intent creation, and external delivery.
- Design finding: stopping after event insertion can leave an event that is no longer new but has never been evaluated. Retrying only when another new event arrives can also strand pending notifications during quiet source periods.
- Correction: retain Pending/Evaluated event state; schedule pending evaluation and pending delivery independently of source ingestion. Complete matching, unique delivery insertion, and the Evaluated marker atomically in one PostgreSQL operation. Keep external sends outside the transaction.
- Validation performed: conceptual crash walkthrough before insertion, after insertion, during evaluation, and after intent creation. No executable workflow or database experiment was run.
- Disposition: incorporate the recovery boundaries into the proposed architecture; preserve the user's n8n/database ownership. The new-event check remains an ingestion optimization, not evidence that processing completed.

## 2026-09-14: Recovery review corrected circuit accounting and probe handling

- Context: the custom reviewer performed a focused conceptual recovery review. It reported no Critical findings, four Important findings, and a Minor concurrency/fairness qualification.
- Findings: evaluation needs an event lock and one consistent rule snapshot; lease expiry alone does not prove provider failure; permanent destination rejection needs a neutral Half-open outcome; every replay path must return through the durable claim gate. An unexpired lease limits logical ownership, not physical sends already in flight.
- Corrections: specify evaluation-time rule sampling and one atomic operation; do not increment a Closed circuit for unobserved lease expiry; release a neutral probe so the next eligible item can probe; include error workflows and manual reruns in the gate rule; state overlap risk and stable due-work ordering.
- Validation: these changes were inspected against the proposed state transitions and reviewer findings. The final whole-design review is still required. All findings and corrections concern design; no runtime tests or implementation results are claimed.

## 2026-09-14: Final review found stale current-decision statuses

- Context: the final reviewer inspected the staged architecture milestone.
- Finding: no Critical issues; one Important inconsistency. The decision-log table still presented superseded HTTP integration as approved and pointed delivery coordination to the rejected ADR-004 proposal. The footer did not make the individual row statuses sufficiently clear.
- Correction: preserve historical decisions, explicitly mark superseded ownership/HTTP boundaries, and point current delivery coordination to ADR-005 and the architecture. Align the local-demo row with the documented Development-only identity design.
- Validation: the full review confirmed that the four earlier recovery findings were addressed. The focused correction review returned APPROVE with no remaining Critical, Important, or Minor findings; its staged whitespace check passed.
- Disposition: corrected and accepted as a documentation/design baseline. See [the actual review and check record](../evidence/reviews/2026-09-14-architecture-review.md). This does not validate PostgreSQL transactions, workflow behavior, or delivery at runtime.

## 2026-09-14: Existing DEV infrastructure superseded local-service assumptions

- Context: milestone 3 preparation under [prompt 010](../prompts/010-create-application-skeleton.md), before any application scaffolding. The user supplied environment information identifying existing shared DEV PostgreSQL and hosted n8n.
- Prior generated design: ADR-003 required a local application/n8n/PostgreSQL topology and creation of two databases; the active architecture restricted the n8n editor to loopback and treated n8n internal storage and credentials as repository integration concerns. ADR-005 changed processing ownership but retained those persistence assumptions.
- Finding: repository inspection found a conflict with the actual development environment described by the user. Work paused without edits. The user authorized a narrow amendment and clarified that hosted n8n internal persistence is entirely external to this project's database design.
- Correction: [ADR-006](adr/ADR-006-use-existing-shared-dev-infrastructure.md) records this implementation-environment discovery. Use the local management application and existing shared DEV services; remove requirements to provision services or create/manage n8n internal storage. Preserve the product database contract, EF schema ownership, least-privilege product-access roles, and workflow-owned processing. Active validation guidance no longer implies permission to restart shared services or inspect their internal storage.
- Disposition: earlier topology superseded by explicit user decision. This corrects architecture and scope documentation, not generated application code; no duplicate services, databases, or workflows were actually created. The independent review found no Critical or Important issue and one Minor historical-status wording issue, which was clarified. See [the topology review record](../evidence/reviews/2026-09-14-dev-topology-review.md) for actual checks and runtime verification limits.

## 2026-09-14: Health cancellation alone did not bound a stalled PostgreSQL handshake

- Context: milestone 3 implementation under [the approved skeleton plan](superpowers/plans/2026-09-14-application-skeleton.md). The approved readiness design used a five-second ASP.NET Core health timeout and propagated its cancellation token to EF Core connectivity checking.
- Initial output: the PostgreSQL probe relied on health cancellation while retaining the configured provider connection timeout.
- Finding: a controlled loopback listener accepted TCP but stalled the PostgreSQL handshake. The first HTTP check exceeded the ten-second client wait, demonstrating that token propagation alone did not provide the expected response bound in this tested path. Missing, malformed, and refused connections had not exposed it.
- Correction: cap the connection timeout at five seconds on the health probe's own scoped connection, while preserving any shorter configured timeout and continuing to pass cancellation. Do not mutate the captured application configuration or use a shared DbContext. Return fixed sanitized failures and omit raw provider exceptions from logs/results.
- Validation: the final missing/malformed/refused/stalled real-process checks passed. The stalled probe returned HTTP 503 in approximately 5.15 seconds while liveness stayed healthy. Temporary test sentinels were absent from checked bodies and logs. The task reviewer approved specification compliance and task quality with no findings, including the scope/timeout correction. See [the skeleton evidence](../evidence/reviews/2026-09-14-skeleton-review.md).
- Limit: this validates controlled local failure behavior, not a successful connection through the shared DEV product credential or every network/provider failure mode.

## 2026-09-14 — Isolate Docker failure probes and validate container connectivity

The initial application-only Docker plan preserved an existing `.env` but did not explicitly isolate negative readiness tests from its contents. Independent plan review identified that Compose could load real credentials and contact shared DEV during a no-credential test. The revised plan disables default dotenv loading and substitutes disposable service environment input for those tests, without reading or replacing the user's file. The review also required positive readiness from the container when safe credentials become available: a host-process or MCP connection cannot establish container network access. These are plan corrections before implementation, not observed secret exposure or successful database checks. See [the specification](superpowers/specs/2026-09-14-application-skeleton-design.md) and [review evidence](../evidence/reviews/2026-09-14-skeleton-review.md).

## 2026-09-14 — Exclude host build output from Docker restore/publish

The first generated `.dockerignore` admitted local `bin/obj` files. The first image publish failed with `NETSDK1064` after host restore assets overwrote the container restore, and a scratch build-context export confirmed the unwanted directories. The corrected allowlist re-excludes source subtrees before admitting the exact current application inputs. A second context export contained exactly seven required source/configuration files, with no credentials, IDE files, local build output, documentation, or prompt history; the image build and isolated runtime checks then passed. The runtime image contains published output only. Future source additions must update this explicit allowlist. See [validation evidence](../evidence/reviews/2026-09-14-skeleton-review.md).

## 2026-09-14 — Successful readiness did not establish safe runtime access

The real connection passed host and container readiness. A separate read-only audit of the host session then found superuser privileges and no PostgreSQL TLS. Treating `Healthy` as final access/security acceptance would have hidden an ADR-006 boundary violation. Readiness intentionally proves connectivity only; the runtime code needs no change for this result. Final acceptance now requires a restricted application credential and verified secure transport, without the agent altering shared infrastructure. An external encrypted tunnel has not been established or ruled out by the PostgreSQL TLS result. See [the actual query and sanitized results](../evidence/reviews/2026-09-14-skeleton-review.md).

Subsequent user clarification explicitly accepted the observed administrative credential and lack of PostgreSQL TLS for current DEV usage. [ADR-007](adr/ADR-007-accept-current-dev-database-access.md) records this deployment-specific exception and removes the milestone blocker. The audit facts and production requirements remain unchanged; do not treat an accepted DEV limitation as a new failed check or as production-security validation.

## 2026-09-14: Reconcile the configuration milestone with accepted scope

- Context: milestone 4 brainstorming under [prompt 019](../prompts/019-alert-configuration-model-and-management-ui.md), before feature design or implementation.
- Finding: repository inspection exposed conflicts between the task's candidate multi-condition model, the existing demo identity selector, and the runtime-before-management sequence. The controller stopped and reported them as the task required.
- User correction: do not treat the clarification as blanket permission to supersede architecture. Retain ADR-002's one-condition MVP and reject the multiple-condition/AND expansion. Separately replace demo identity switching with one configured owner and move persisted configuration before runtime consumption.
- Disposition: [ADR-008](adr/ADR-008-use-single-configured-mvp-owner.md) narrowly supersedes the identity assumption; D-004 records the deliberate nine-milestone sequence; D-005 reaffirms the smaller condition scope. No multiple-condition model or fake identity behavior was implemented. The broader model was a user-supplied candidate, not an implemented or independently validated AI solution.
- Validation basis: inspected ADR-002/003/005, the active scope/architecture/plan, and Git history (`4fca2f1`, `b68e3ad`, `5880801`). The user explicitly resolved the conflicts. Detailed feature design still awaits approval; documentation amendments do not validate runtime behavior.

## 2026-09-14: Database trace defaults bypass existing log privacy filters

- Context: milestone 4 observability design, before package installation or implementation.
- Candidate assessed: enable automatic Npgsql traces alongside the existing provider-log suppression.
- Finding: the stable 10.0.3 provider's versioned source records SQL text and raw exception data in activities. Suppressing Npgsql/EF connection log categories does not suppress or sanitize those activities. Its official tracing guide also retains an experimental-compatibility caveat.
- Disposition: recommend request traces and sanitized correlated application failure logs first; defer database spans rather than add a custom telemetry-redaction subsystem for this MVP. This recommendation awaits feature-design approval. A stable compatible tracing package exists; it is not rejected as preview-only or incompatible.
- Evidence: [context/source review](../evidence/reviews/2026-09-14-alert-configuration-context-review.md). No telemetry was emitted or inspected at runtime in this phase, and no secret exposure is claimed.

## 2026-09-14: Configuration validation and tooling required concrete failure checks

- Context: approved milestone 4 persistence implementation.
- Findings: initial destination validation accepted trailing LF because regex end anchors permitted it, and trimming could normalize newline-only destination input. A malformed connection string raised ArgumentException instead of the intended safe unavailable result. The first design-time factory required a command-line connection; its replacement initially gave User Secrets precedence over a controller-supplied environment target.
- Corrections: strict destination anchors and pre-normalization newline rejection; bounded connection parsing with sanitized failure classification; external design-time connection discovery with environment overriding User Secrets and no connection fallback. Explicit EF Core/Relational patch references resolved an observed mixed-version assembly conflict.
- Validation: targeted red/green cases and local build passed; the controller applied the reviewed migration through an environment-supplied, independently verified target. The initial PostgreSQL-enabled suite passed 36 tests. The reviewer approved the task with one redundant tracker-clear cleanup. See [actual milestone evidence](../evidence/reviews/2026-09-14-alert-configuration-validation.md).
- Limit: these results do not establish browser behavior or observability privacy; those require their own checks.

## 2026-09-14: Browser validation exposed Razor boolean attribute semantics

- Context: milestone 4 management UI after its focused task review.
- Finding: actual rendered HTML used `value="value"` for the hidden boolean desired state, preventing Enable from binding. The existing route/form tests did not cover rendered status transitions. Separate malformed checkbox input also demonstrated that model-binding errors must be handled before calling the service.
- Correction: render explicit string `true`/`false` values; reject invalid editor/status binding and display safe reload guidance for revision conversion failures. Remove a proposed boolean-conversion helper/test that merely mirrored implementation and did not exercise Razor rendering.
- Validation: seven focused HTTP tests passed; scoped fix review approved; actual Chromium create/edit/Enable/Disable and independent HTTP ownership/antiforgery/stale-edit checks passed. A guarded PostgreSQL HTTP regression exercises the rendered status form. See [evidence](../evidence/reviews/2026-09-14-alert-configuration-validation.md) for executed checks and pending test status.

## 2026-09-14: Combined validation revealed telemetry test configuration leakage

- Context: final milestone suite with real PostgreSQL explicitly enabled.
- Finding: nine telemetry checks inherited the runtime connection and observed healthy management responses instead of their deliberately unavailable database fixture. The same host-startup timing had earlier made late in-memory test configuration ineffective. Focused tests alone did not reveal the inherited-environment case.
- Correction: isolate and restore database/owner environment settings alongside telemetry settings in the nonparallel fixture; retain real SDK/receiver assertions. The PostgreSQL HTTP fixture replaces only external settings and database options while preserving real management/EF/Razor behavior.
- Validation: an inherited invalid-connection focused run passed eleven telemetry tests; scoped review approved; the full guarded suite passed all 56 tests without skips. No production configuration behavior was changed. See [evidence](../evidence/reviews/2026-09-14-alert-configuration-validation.md).

## 2026-09-14: User correction replaced destination ownership and custom validation

- Context: before the milestone commit, the user rejected externally configured destination allowlists and requested an existing validation framework. They explicitly selected shared destinations per user rather than per-alert subscription targets.
- Correction: ADR-010 supersedes that part of the initial design. A minimal PostgreSQL users profile stores common email/Slack destinations; alerts reference its existing ownership UUID. Remove per-alert target fields and allowlists; add an actual settings page, without Identity/authentication. Use FluentValidation 12.1.1 core, whose official package declares Apache-2.0 licensing and net8.0-or-later compatibility.
- Scope: preserve the one-condition limit, current-owner scoping, telemetry and workflow ownership. Do not rewrite the applied initial migration; review a new migration that preserves unambiguous values and refuses ambiguous conversion.
- Evidence status: the earlier56-test/browser/container results validate the previous design only. Revised-model implementation and acceptance are recorded separately; no commit had been created when the correction arrived.

### Shared-destination migration preflight

The independent amendment review identified that pooling different channel subsets could expand delivery targets, a write during backfill could lose configuration, and a generated downgrade could discard profile-only settings. The specification/plan now require identical complete old target sets per owner, transaction-scoped exclusive table locks before checks/transfer, and a fail-fast unsupported downgrade. Scoped re-review approved these rules; generated migration source/SQL and actual PostgreSQL behavior remain separate validation gates. See [the amendment evidence](../evidence/reviews/2026-09-14-alert-configuration-validation.md#shared-user-destinations-amendment).

### Shared settings implementation and fixture correction

The controller rejected a first validator draft that merely wrapped custom field validation in FluentValidation Must predicates; ordinary name/length/email/Slack checks now use framework rules, retaining only necessary cross-field, finite-number and platform-mailbox/control checks. A settings success response retaining the submitted ModelState would render a stale hidden revision; POST-redirect-GET and a consecutive rendered-save test corrected that before acceptance. The first full DEV-enabled revised run exposed a test host inheriting the runtime database connection (64 passed, one failed). Replacing typed DbContext options in the isolated HTTP fixture corrected that boundary, and scoped review approved it. The final full suite passed 65/65. These corrections and the actual PostgreSQL/browser/container results are in [the amendment evidence](../evidence/reviews/2026-09-14-alert-configuration-validation.md#shared-user-destinations-amendment).

### Database whitespace contract correction

The final branch reviewer found that PostgreSQL btrim without a character set removes ordinary spaces but leaves tabs, allowing whitespace-only alert names and sole email destinations through direct writes despite the documented nonblank checks. Read-only SQL reproduced the issue; it also showed that the database locale’s POSIX whitespace class does not recognize nonbreaking space. The correction uses an explicit Unicode trim set matching .NET, a forward constraint-only migration preserving both applied migrations, and guarded direct-write regression checks. The architecture diagram’s unsupported “secure DEV connection” label was also changed to neutral wording consistent with ADR-007. The forward migration was applied after review, both regression tests changed from expected failure to passing, and the final guarded suite passed 67/67, as recorded in [evidence](../evidence/reviews/2026-09-14-alert-configuration-validation.md).

## 2026-09-14 — Real PostgreSQL checks corrected a test assumption

The first six runtime relational tests ran after reviewed migration application. Four passed; both restricted-delete tests correctly received a database rejection but incorrectly expected the generic foreign-key SQLSTATE `23503`. PostgreSQL 18 returned `23001` for the explicit RESTRICT action. The assertions were corrected to `PostgresErrorCodes.RestrictViolation`; the same real-DEV suite passed 6/6 and scoped review approved. No schema behavior was weakened. [Actual evidence](../evidence/reviews/2026-09-14-first-runtime-validation.md).

## 2026-09-14: SMTP extension simplified to a channel branch

The initial SMTP proposal added a fourth workflow, duplicating selected-ID validation, claim and event reading. The user correctly challenged that separation; the approved correction keeps the existing delivery workflow and adds mutually exclusive Slack/email transport paths behind one claim. No new recovery requirement justified another workflow.

Independent plan review then identified fixture isolation, pre-send validation outcome and credential-target verification gaps. The plan now requires an empty source queue and no unrelated matches, records invalid email preparation as a definite Failed/non-send, and verifies the intended SMTP4DEV target before the controlled send. Native SMTP result validation checks the sole intended recipient; broad error strings are deliberately not guessed into definitive rejection states. Actual checks and the read-only custom-role CLI fallback used for independent implementation review are recorded in [SMTP evidence](../evidence/reviews/2026-09-14-smtp-delivery-validation.md).

Controller review also removed an SDK credential placeholder that could select an unintended account, strengthened export checks for routing predicates and disabled safety nodes, and rejected contradictory SMTP acknowledgements. The n8n update API rejected an unused `maxTries=1` setting despite SDK acceptance; omitting it while explicitly disabling retries preserved one-attempt behavior. These corrections preceded the controlled send; see the same SMTP evidence.
