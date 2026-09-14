# AI review log

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

- Context: milestone 2, after the user required a circuit breaker and preferred retrying uncertain email/Slack sends. See [the requirement](../prompts/004-require-delivery-retries-and-circuit-breaker.md).
- Alternatives evaluated during design: a breaker around an application HTTP client, a breaker scoped to a single workflow execution, and unconditional retry on the n8n send node.
- Finding: the application does not make the external transport call, so its HTTP-client breaker would not observe Slack/email failures. Per-execution state would not coordinate separately scheduled retries. Send-node retries could bypass durable attempt accounting and any gate applied only when claiming work.
- Correction proposed: coordinate durable attempt eligibility and per-transport circuit state at the application API; let n8n perform one external send per authorized attempt and report the outcome. Keep Slack and email failure domains separate. [ADR-004](adr/ADR-004-durable-delivery-and-circuit-breaking.md) records this proposal and its added state/coordination cost.
- Validation performed: traced the actual planned call boundary and compared it with [Microsoft's circuit-breaker guidance](https://learn.microsoft.com/en-us/azure/architecture/patterns/circuit-breaker) and [n8n's Retry On Fail behavior](https://github.com/n8n-io/n8n-docs/blob/main/docs/integrations/builtin/handle-rate-limits.md). This is a conceptual design check; no resilience package, workflow, or prototype was implemented or tested.
- Disposition: reject the three alternatives as the authoritative delivery protection. The proposed durable coordination still requires final architecture review and later failure/concurrency tests. No claim of duplicate-free external delivery is made.
- Subsequent user correction: the application-owned coordination proposal was rejected. The circuit breaker must live in n8n workflows, and n8n must access the product database directly without application HTTP endpoints. [ADR-005](adr/ADR-005-direct-database-integration-and-workflow-owned-delivery.md) supersedes ADR-004's allocation. The earlier analysis correctly distinguished transport calls from application HTTP calls, but overreached by assigning the solution to application services. Workflow-owned durable state and database contracts must now be designed; no previous implementation or tests are claimed.

## 2026-09-14: New-event branching is insufficient for crash recovery

- Context: the user's [workflow sequence](../prompts/006-confirm-workflow-processing-sequence.md) supplied a new-event branch followed by matching, intent creation, and external delivery.
- Design finding: stopping after event insertion can leave an event that is no longer new but has never been evaluated. Retrying only when another new event arrives can also strand pending notifications during quiet source periods.
- Correction: retain Pending/Evaluated event state; schedule pending evaluation and pending delivery independently of source ingestion. Complete matching, unique delivery insertion, and the Evaluated marker atomically in one PostgreSQL operation. Keep external sends outside the transaction.
- Validation performed: conceptual crash walkthrough before insertion, after insertion, during evaluation, and after intent creation. No executable workflow or database experiment was run.
- Disposition: incorporate the recovery boundaries into the proposed architecture; preserve the user's n8n/database ownership. The new-event check remains an ingestion optimization, not evidence that processing completed.

## 2026-09-14: Recovery review corrected circuit accounting and probe handling

- Context: the custom reviewer received [prompt 007](../prompts/007-review-workflow-recovery-design.md) for a focused conceptual recovery review. It reported no Critical findings, four Important findings, and a Minor concurrency/fairness qualification.
- Findings: evaluation needs an event lock and one consistent rule snapshot; lease expiry alone does not prove provider failure; permanent destination rejection needs a neutral Half-open outcome; every replay path must return through the durable claim gate. An unexpired lease limits logical ownership, not physical sends already in flight.
- Corrections: specify evaluation-time rule sampling and one atomic operation; do not increment a Closed circuit for unobserved lease expiry; release a neutral probe so the next eligible item can probe; include error workflows and manual reruns in the gate rule; state overlap risk and stable due-work ordering.
- Validation: these changes were inspected against the proposed state transitions and reviewer findings. The final whole-design review is still required. All findings and corrections concern design; no runtime tests or implementation results are claimed.

## 2026-09-14: Final review found stale current-decision statuses

- Context: the final reviewer received [prompt 008](../prompts/008-review-final-mvp-architecture.md) and inspected the staged architecture milestone.
- Finding: no Critical issues; one Important inconsistency. The decision-log table still presented superseded HTTP integration as approved and pointed delivery coordination to the rejected ADR-004 proposal. The footer did not make the individual row statuses sufficiently clear.
- Correction: preserve historical decisions, explicitly mark superseded ownership/HTTP boundaries, and point current delivery coordination to ADR-005 and the architecture. Align the local-demo row with the documented Development-only identity design.
- Validation: the full review confirmed that the four earlier recovery findings were addressed. The focused follow-up in [prompt 009](../prompts/009-review-decision-log-correction.md) returned APPROVE with no remaining Critical, Important, or Minor findings; its staged whitespace check passed.
- Disposition: corrected and accepted as a documentation/design baseline. See [the actual review and check record](../evidence/reviews/2026-09-14-architecture-review.md). This does not validate PostgreSQL transactions, workflow behavior, or delivery at runtime.
