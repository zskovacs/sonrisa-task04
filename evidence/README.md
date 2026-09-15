# Evidence index

Start with [end-to-end validation](reviews/2026-09-14-end-to-end-validation.md) for the completed implementation at `e667447`, then [final documentation checks](reviews/2026-09-15-final-documentation.md) for this branch. Each record states its revision and distinguishes real systems, mocks, historical reuse and untested behavior.

## Final-system evidence

| Area | Evidence and limits |
| --- | --- |
| Independent review corrections | [Assessment and checks](reviews/2026-09-15-independent-review-corrections.md): findings against `e667447` reconciled with `ce8b76c`, bounded local fixes, no shared DEV changes or sends. |
| Runtime course correction | [Baseline inspection](reviews/2026-09-14-runtime-simplification-inspection.md), [replacement validation](reviews/2026-09-14-runtime-simplification-validation.md): forward table removal, native dedup/cap limits, five-attempt isolation, real Slack acceptance 52/re-entry 53 and archival. The initial replacement was Slack-only; Email followed. |
| Email extensibility | [Validation](reviews/2026-09-14-email-channel-validation.md), [execution summaries](reviews/2026-09-14-email-channel-executions.json), [SMTP4DEV capture](reviews/2026-09-14-email-channel-smtp-capture.json): same channel boundary, narrow name-projection exception, both-direction native retry mocks, one SMTP submission/capture. |
| Product admin | [Validation](reviews/2026-09-14-operational-admin-validation.md), [desktop/mobile screenshots](reviews/operational-admin-ui/): read-only cross-owner counts, privacy and owner-scoped management regressions. |
| Integrated application | [Host/browser/telemetry checks](reviews/2026-09-14-e2e-application.md): safe outage corrections, actual admin rows, ownership, exporter independence and clean-checkout build. |
| Integrated runtime | [Full record](reviews/2026-09-14-end-to-end-validation.md), [executions 64–77](reviews/2026-09-14-e2e-executions.json), [fixtures](reviews/2026-09-14-e2e-fixtures.json), [SMTP4DEV capture](reviews/2026-09-14-e2e-smtp-capture.json): actual SQL/source, deterministic and pinned boundaries, native retry/cap failures, export restoration and exact data cleanup. |

## Earlier milestones and preserved history

| Area | Evidence and historical context |
| --- | --- |
| Bootstrap | [Initial review](reviews/2026-09-14-bootstrap-review.md): repository and process baseline. |
| Architecture | [Architecture review](reviews/2026-09-14-architecture-review.md), [DEV topology review](reviews/2026-09-14-dev-topology-review.md): design-only checks and later boundary changes; use the [ADR status index](../docs/adr/README.md). |
| Application skeleton | [Validation/review](reviews/2026-09-14-skeleton-review.md): startup, health, Docker, build-context correction and the accepted DEV privilege/TLS exception. |
| Configuration management | [Context review](reviews/2026-09-14-alert-configuration-context-review.md), [validation](reviews/2026-09-14-alert-configuration-validation.md), [final review](reviews/2026-09-14-alert-configuration-final-review.md), [UI screenshots](reviews/alert-configuration-ui/): includes initial model and explicitly separated shared-destination amendments. |
| First runtime, superseded | [Context](reviews/2026-09-14-first-runtime-context-review.md), [validation](reviews/2026-09-14-first-runtime-validation.md): database-backed runtime and separate workflows at `b9cf171`; native SQL/Slack corrections and credential incidents. These are not the current runtime architecture. |
| Original SMTP extension, superseded | [Validation](reviews/2026-09-14-smtp-delivery-validation.md), [capture](reviews/2026-09-14-smtp-capture.json): actual SMTP work at `e1dc337` against the old delivery-state boundary. Current Email evidence is above. |

## Reading the claims

- **83/83 guarded .NET and 24/24 Node** belong to the integrated milestone. The documentation rerun has **64 passed/19 guarded skips** and **24/24 Node**; no shared test writes or notification sends were repeated.
- **Slack acceptance** means the provider acknowledged the controlled send at the recorded time; current credential health and human receipt were not re-established here.
- **SMTP4DEV capture** means SMTP submission plus captured content. It does not prove external internet mailbox delivery.
- **Native retries/dedup** were exercised in the real n8n engine, with transport mocks and reduced-cap instrumentation clearly identified. They do not establish permanent/atomic dedup, real provider outages or production scheduling reliability.
- Screenshots show synthetic configuration and the UI at their recorded milestone; they are not current live-state screenshots.

The reserved empty `screenshots/` and `test-output/` directories contain only `.gitkeep`; those files are not evidence. Review findings are summarized in the [AI review log](../docs/ai-review-log.md), and conclusions in the [retrospective](../docs/final-reflection.md).
