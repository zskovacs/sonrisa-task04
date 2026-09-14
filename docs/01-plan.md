# Engineering and delivery plan

## Status and purpose

This is the first planning baseline, recorded on 2026-09-14. Only repository bootstrap belongs to the current task. No application implementation, infrastructure setup, or executable n8n workflow is authorized by this document alone.

The [brief](00-product-brief.md) establishes the product need; [scope](03-scope.md) proposes an MVP; [the question register](02-assumptions-and-open-questions.md) identifies what must be resolved before dependent work. Future phases need a bounded specification, concrete acceptance examples, and a reviewed task plan before implementation.

## Delivery approach

Use n8n for the integration-heavy work: scheduling or webhook ingestion, HTTP/RSS-style connections, notification transport, workflow execution, and integration failure handling. These are existing platform capabilities and offer more value within the time box than building a generic orchestration engine. Custom code should focus on canonical validation, domain rules, matching, and durable state. [ADR-001](adr/ADR-001-use-n8n-for-orchestration.md) records the rationale, alternatives, trade-offs, and mitigations.

Keep the custom application shape and UI stack open. Compare ASP.NET Core Razor Pages or another small server-rendered UI, native JavaScript/TypeScript with an HTTP API, and Angular against the actual user and admin interactions. Select the smallest maintainable option; Angular needs an explicit justification for its additional application and build complexity.

Work toward one narrow vertical slice: configure an alert, ingest a controlled event, evaluate it, persist a delivery decision, send a notification, and inspect its outcome. Prove the first path before broadening source or rule coverage; complete both required channels within the proposed MVP. Add focused checks with each capability rather than waiting until the final validation phase. Use deterministic fixtures for repeatability and a real external path later to verify integration behavior.

## Proposed phases and exit evidence

| Phase | Work and why it comes next | Exit condition for future acceptance |
| --- | --- | --- |
| 1. Clarify assumptions, scope, and questions | Preserve the brief; narrow the first useful scenario with the product owner. Resolve only the questions needed for the next slice. | An agreed first scenario, explicit assumptions, non-goals, and observable acceptance examples; unresolved dependencies remain visible. |
| 2. Establish architecture and boundaries | Refine the initial n8n/application boundary after the scenario is understood. Decide the minimum runtime, persistence approach, contracts, ownership, and access expectations. Evaluate UI options before choosing a skeleton that would lock them in. | Reviewed architecture and task plan, with ADRs for actual choices and integration/failure contracts sufficient for the next implementation step. |
| 3. Create the smallest executable skeleton | Add only the application and local infrastructure needed for the agreed slice. Avoid speculative services and frameworks. | Real startup/setup instructions and an actually verified minimal executable path; no unused infrastructure. |
| 4. Implement canonical ingestion and alert matching | Establish deterministic behavior before depending on a changing external source. | Valid and invalid events plus matching/non-matching examples pass focused checks; duplicate event semantics are tested against the agreed contract. |
| 5. Implement n8n source workflows | Connect the selected initial source once the application boundary is testable. | Small, reviewed exports in Git; repeatable ingestion, unavailable-source behavior, and malformed-input handling verified. |
| 6. Implement durable notification delivery | Complete the first source-to-notification slice, then cover email and Slack through the channel boundary. | Delivery decisions/outcomes survive the agreed restart scenarios; duplicate, temporary-failure, and uncertain-outcome cases are exercised without unsupported delivery guarantees. |
| 7. Add minimum alert management | Expose the proven alert behavior through the selected UI, avoiding speculative interaction features. | The agreed alert configuration/management journey works end to end with access and validation behavior verified. |
| 8. Add minimum operational admin view | Expose enough actual product and delivery state to explain the slice's outcomes. | An operator can inspect the agreed success and failure cases within the agreed authorization boundary. Exact fields/actions must be decided first. |
| 9. Validate the complete slice | Combine earlier focused checks and add cross-boundary failure/recovery experiments. | Matching, deduplication, delivery failure, configuration, security, and manual demo checks in the [validation strategy](05-validation-strategy.md) have actual results and documented limitations. |
| 10. Review and reflect | Review the final result against the original need and accepted scope. | Required final review completed; meaningful AI corrections/evidence linked; limitations, remaining questions, and future work recorded honestly. |

The phases are dependency guidance, not a commitment to a large feature inventory. A phase may require smaller focused commits. If discovery invalidates the scope, boundary, or milestone sequence, record the reason when it occurs, update the plan, and review the affected work before proceeding.

## Planned milestone commits

| Milestone | Intended major commit | Meaning |
| --- | --- | --- |
| 1 | `docs: define scope, assumptions and delivery plan` | This bootstrap baseline, including working rules, the supplied n8n decision, and initial architecture direction. |
| 2 | `docs: record architecture decisions and system design` | Future refinement after clarification: resolve enough design questions to support a reviewed executable slice. |
| 3 | `feat: add application skeleton and local infrastructure` | The minimum executable foundation selected in milestone 2. |
| 4 | `feat: implement event ingestion and alert matching` | Canonical validation and deterministic domain behavior. |
| 5 | `feat: add n8n source workflows` | Source integrations against the agreed boundary. |
| 6 | `feat: add durable notification delivery` | Persistent delivery behavior and email/Slack transport. |
| 7 | `feat: add alert management UI` | The agreed minimal user journey. |
| 8 | `feat: add operational admin view` | The agreed minimum operator visibility. |
| 9 | `test: validate matching, deduplication and delivery failures` | Cross-boundary validation and actual failure/recovery evidence. |
| 10 | `docs: add AI review evidence and final retrospective` | Consolidated evidence and reflection on work that actually happened. |

Only milestone 1 is targeted now. Including ADR-001 and a conceptual diagram in bootstrap does not complete milestone 2. Milestones 2–10 remain future work; do not create empty commits. Record completion through actual Git commits, not advance-dated claims. Prompt and review records should be kept throughout the work, not reconstructed at milestone 10.

## Bootstrap acceptance and ongoing process

For this documentation baseline, inspect the existing directory and preserve local files; create the requested documentation and reserved directories; review consistency, scope, links, and secret exposure; obtain final review; and inspect the exact staged files before the milestone 1 commit. Do not store the bootstrap/context prompt in prompt history. No runtime test or screenshot is expected because implementation has not started.

For later material work, follow [AGENTS.md](../AGENTS.md): record the final English agent prompt before execution, implement only a reviewed bounded task, validate before acceptance, review each task and the whole change, and record meaningful corrections when they occur. Keep durable evidence tied to the revision it validates. Scope growth and speculative architecture require justification, not silent implementation.
