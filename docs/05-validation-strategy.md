# Validation strategy and actual results

Validation has four distinct layers: local automated checks, application/database integration, deterministic n8n execution and real external integration. The [evidence index](../evidence/README.md) links each result to its actual milestone. Plans describe intended work; they do not prove behavior.

## Automated tests

From the repository root:

```bash
dotnet build Sonrisa.sln
dotnet test Sonrisa.sln
node --test n8n/tests/*.test.mjs
```

.NET checks cover validation, EF model/migration shape, owner-scoped services, rendered HTTP/antiforgery behavior, revisions/atomicity, admin projections, health and OpenTelemetry configuration/privacy. Node checks exercise the exact Code-node bodies, fixture mapping, typed matching, independent destinations, safe messages and export guards.

| Run | Actual outcome |
| --- | --- |
| Integrated milestone `e667447`, default .NET | 64 passed, 19 guarded PostgreSQL cases skipped. |
| Integrated milestone `e667447`, verified DEV configuration | 83 passed, zero failed/skipped. |
| Integrated milestone Node | 24 passed, zero failed/skipped. |
| Final documentation local rerun | Build: zero warnings/errors; .NET: 64 passed, 19 guarded skips; Node: 24 passed. [Milestone evidence](../evidence/reviews/2026-09-15-final-documentation.md). |
| Independent-review correction working tree based on `ce8b76c` | Release build: zero warnings/errors; .NET: 69 passed, 19 guarded skips; Node: 30 passed. [Correction evidence](../evidence/reviews/2026-09-15-independent-review-corrections.md). No hosted runtime update. |

Guarded relational tests require externally supplied `SONRISA_TEST_DATABASE` and matching `SONRISA_TEST_DATABASE_NAME`. They verify `current_database()` before fixture writes, require existing reviewed schema, and roll back or remove exact generated IDs. They never migrate, truncate or reset unrelated data. Admin aggregate tests use a nonparallel collection to prevent fixture races. The documentation milestone did not enable these writes.

## Application and PostgreSQL integration

[Integrated application evidence](../evidence/reviews/2026-09-14-e2e-application.md) records real host startup/restart, liveness/readiness, rendered owner-scoped management, cross-owner admin counts/rows, absent full admin destinations, and browser checks. Empty/timeout admin readers are explicitly controlled cases, not an emptied DEV database.

Live missing/malformed/blank-Host/refused database configurations exposed two management outage defects. Narrow service guards and four red/green HTTP regression cases corrected 500 responses to generic 503 pages. Liveness remained independent. Eleven observability tests exercised real SDK log/trace correlation, routing and privacy; unavailable OTLP did not prevent healthy operation.

Read-only PostgreSQL inspection confirmed only `users`, `alerts` and six EF migration records, with runtime-table removal represented by a forward migration. Full-row digests matched before/after generated fixture cleanup. Readiness establishes connectivity only, not schema compatibility, privilege isolation or transport security. The accepted DEV exception remains in ADR-007.

Earlier [skeleton](../evidence/reviews/2026-09-14-skeleton-review.md) and [configuration evidence](../evidence/reviews/2026-09-14-alert-configuration-validation.md) record Docker, migration and responsive UI checks. Those are historical observations; this documentation milestone did not rebuild Docker or repeat browser integration.

## Deterministic workflow validation

[Executions 64–77](../evidence/reviews/2026-09-14-end-to-end-validation.md#actual-n8n-runtime-scenarios) distinguish actual SQL/native nodes from mocks and pinned boundaries:

- Below/equal/above threshold, malformed magnitudes, disabled rules, multi-owner routing and both destinations.
- Within-input deduplication plus separate full-entry repeated executions with the same workflow/node identity.
- Native retry-engine transport mocks: five observed calls, approximately 20 seconds at the failing node, visible discard and continuation in both transport directions.
- Controlled history cap reduction: capacity failure before filtering; history retained and cap restored to 10,000.
- Controlled read-only SQL failure after dedup and replay showing accepted loss.
- Pinned schema-unrepresentable bad rules/destinations and an unknown future channel; these validate defensive boundaries, not supported persisted features.

Temporary instrumentation was removed, workflow defaults/inactivity restored and remote export compared. Fixture row digests also matched after cleanup. No persistent mock nodes, pins or test input remain in the authoritative artifact.

## Real external validation

| Integration | Evidence | What it establishes |
| --- | --- | --- |
| USGS | Execution 72; earlier Email execution 60 | Actual HTTP response and canonical ID/magnitude/UTC comparisons, with transport suppressed. |
| Slack | [Execution 52 and repeat 53](../evidence/reviews/2026-09-14-runtime-simplification-validation.md); re-inspected during integrated validation | Controlled synthetic API acceptance and no second full-entry send. Reuse was supported by unchanged source/transport contract, not a fresh credential-health or human-receipt claim. |
| SMTP/Email | [Email execution 62](../evidence/reviews/2026-09-14-email-channel-validation.md), integrated execution 76 and repeat 77 | Native SMTP acceptance, sole intended recipient, decoded plain-text SMTP4DEV capture and no repeated send. |

SMTP4DEV capture is not delivery through an internet provider to a recipient inbox. Native retry mocks are not real provider outages. A successful transport response is not an exactly-once guarantee.

## Workflow source control and documentation checks

The authoritative [process export](../n8n/workflows/process.json) is checked against exact source, SQL, bindings, graph, loop/error feedback, retry settings and live/empty defaults. The sanitizer removes environment credential references and rejects active state, pins, state, test input and unsafe drift. Importing a new workflow requires rebinding and creates distinct deduplication history.

Final read-only inspection found the same inactive 26-node DEV graph; sanitizing it produced a byte-identical export. Generated live and fixture SDK representations both passed n8n MCP validation without execution. Local Markdown paths/anchors, focused secret/hygiene checks and whitespace review are recorded in [final evidence](../evidence/reviews/2026-09-15-final-documentation.md). They establish documentation/artifact consistency, not fresh external delivery.

The subsequent [independent-review corrections](../evidence/reviews/2026-09-15-independent-review-corrections.md) have local test/export evidence only. The hosted workflow was not updated, so the earlier native execution and message-content checks do not validate the corrected Code bodies.

## Not validated

- External internet Email delivery or current Slack credential health/human receipt.
- Production scheduling reliability, missed-run completeness, high-scale/concurrent ingestion, shared-service restart or retention endurance.
- Permanent/global/physical-earthquake exactly-once deduplication; alias resolution is deliberately absent.
- Guaranteed delivery, durable recovery, dead letters or backlog drain.
- Production authentication/authorization, least-privilege enforcement or PostgreSQL TLS.
- Global hosted n8n OpenTelemetry or a new observability backend.

These boundaries are accepted MVP limits. Future validation should follow a concrete new requirement and preserve shared data, credentials and workflow history.
