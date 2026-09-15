# Final reflection

This retrospective describes the MVP implemented through `e667447`, including the course correction at `c38f2a3`. The [milestone history](01-plan.md), [AI review log](ai-review-log.md) and [evidence index](../evidence/README.md) distinguish proposals, actual implementation and validation.

## 1. Starting from ambiguity

The brief asked for configurable alerts about important world events, Slack and Email notifications, extensible channels and an admin view. It did not define “important,” source providers, matching semantics, storage, administration, delivery guarantees, identity/authentication or scale. News, markets and natural disasters were examples, not three committed integrations. Treating those gaps as questions prevented an attractive but unsupported feature list from becoming the product.

## 2. Planning approach

The scope narrowed to USGS earthquakes and one owner-configured magnitude threshold. Persisted alert management moved before runtime integration so n8n could consume real configuration. A configured owner replaced a demo role selector; shared per-user destinations replaced externally provisioned allowlists. Specifications and reviewed plans made these changes explicit before implementation. The original brief remained unchanged while the decision log recorded refinements and reversals.

## 3. Why n8n

n8n supplied integration nodes, execution inspection, native duplicate filtering, bounded retry and channel routing. That made it a useful orchestration choice for a small event-to-notification product. It also established an important constraint on the design: building a second workflow engine around n8n would reduce the benefit of choosing it. The final workflow keeps deterministic validation/matching code small and leaves transport execution and technical state with n8n.

## 4. Thin management application

Razor Pages, Tailwind, EF Core and PostgreSQL covered the actual forms, lists, validation and persistence needs. Angular could serve a larger interactive application, but a separate frontend introduced tooling and application boundaries that this MVP did not need.

Normal configuration queries are owner-scoped and revision-aware. The minimal users table holds notification settings, not login identities. Read-only admin pages show cross-owner configuration counts and alert summaries; execution failures and runtime debugging remain in n8n. Authentication was intentionally not implemented, and the configured owner does not provide security isolation.

## 5. AI-assisted engineering process

Coding agents helped reconstruct requirements, compare architecture options, draft specifications/plans, implement bounded tasks, review changes and investigate failures. The controller retained scope and acceptance decisions. Independent task and whole-branch reviews challenged generated output; local tests, PostgreSQL checks, rendered HTTP/browser behavior and native n8n executions supplied evidence.

The useful pattern was to test a concrete claim before accepting it. Examples include a stalled PostgreSQL handshake exceeding the intended health bound, Razor rendering a hidden boolean incorrectly, provider tracing bypassing log-privacy assumptions, and an exporter depending on ignored session files. The [review log](ai-review-log.md) records what changed and how each correction was checked. These are observed engineering corrections, not a claim that every agent proposal was defective.

## 6. Most important course correction

The intermediate runtime used separate ingestion, pending-event evaluation and notification-delivery workflows, with PostgreSQL source-event and delivery state. Stronger durability, idempotency and recovery concerns made that architecture technically defensible. It was implemented and validated, then extended with SMTP; those commits and migrations remain in the repository.

Review concluded that the combined design solved reliability requirements absent from the original brief, duplicated n8n responsibilities and made source/channel extension more involved. Earlier explicit preferences had also encouraged durable retry and circuit design; the history is not simply an AI mistake attributed after the fact. The user deliberately superseded those preferences for the smaller MVP.

The final principle became:

> PostgreSQL stores product configuration. n8n owns runtime processing, technical deduplication, retries, and channel dispatch.

[ADR-012](adr/ADR-012-use-n8n-native-runtime-state.md) records the trade-off. A forward migration removed the two runtime tables, the replacement workflow was validated, and the obsolete workflows were archived. Simplicity and requirement alignment were preferred over stronger recovery. This was a conscious reduction in guarantees, not a claim that durable systems are unnecessary in general.

USGS identity followed the same reasoning. Preferred identifiers can change; alias-aware physical-event reconciliation was considered and deferred. Source/external-ID deduplication is sufficient for the accepted MVP, with explicit repeat/loss limits.

## 7. Extensibility validation

Email tested the final channel boundary in practice. It added native SMTP transport, plain-text preparation/result checking and a routing branch. Source ingestion, canonical normalization, deduplication, selection and condition-matching behavior, PostgreSQL schema and ASP.NET architecture stayed unchanged.

There was one small upstream addition: loading the alert name and carrying it into message preparation. Claiming byte-identical alert-loading/evaluator code would conceal that change. [Email evidence](../evidence/reviews/2026-09-14-email-channel-validation.md) demonstrates both-channel fan-out and failure isolation without a new workflow or delivery subsystem. It validates this extension, not a promise that any future channel requires no configuration-model change.

## 8. Validation

The integrated milestone recorded 83/83 guarded .NET tests and 24/24 Node tests, real application/ownership/admin/browser checks, actual schema inspection and n8n executions 64–77. Native-engine mocks demonstrated five total attempts and continuation after either transport failed. Separate full-entry runs established duplicate filtering and loss after downstream failure; a reduced cap exposed capacity failure before filtering.

Real USGS reads were compared with normalized output. Slack provider acceptance from execution 52 was reused after checking the unchanged transport contract. Native SMTP submission and SMTP4DEV capture were verified, including message content and no second send on full re-entry. SMTP capture is not external-inbox delivery, and mocks are not real provider outages.

The final documentation check reran local build/tests and read-only schema/export comparisons without new sends or shared mutations. Its default .NET run passed 64 cases and skipped 19 guarded relational cases; it does not replace the prior 83/83 integration result. [Final evidence](../evidence/reviews/2026-09-15-final-documentation.md) records the exact boundary.

## 9. Final limitations

The MVP has no authentication or authorization; admin pages are unprotected. Shared DEV privilege/TLS exceptions remain explicit. The workflow stays inactive and manual. Native history is bounded, can stop at capacity, and is not permanent or globally atomic physical-event identity.

Notifications are best effort. Deduplication before reads/sends can lose notifications after downstream failure; exhausted retries discard an item, and ambiguous provider failures may lose or duplicate messages. There is no durable recovery, exactly-once guarantee or historical rematching. Production scheduling, internet mailbox delivery, restart/retention endurance and load capacity were not validated.

## 10. What I would do next

I would first validate a selected external Email provider with an authorized recipient. Before public or multi-user use, I would define access requirements and implement authenticated ownership and protected admin access. If unattended operation becomes a requirement, I would review native-history capacity and polling frequency against an actual usage target.

Those are separate product decisions. The completed MVP does not need a speculative reliability platform, new source catalogue or larger frontend to explain what it delivers today.
