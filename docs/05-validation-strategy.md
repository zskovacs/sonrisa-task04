# Validation strategy

These are future product checks for the architecture baseline, amended for shared DEV under [ADR-006](adr/ADR-006-use-existing-shared-dev-infrastructure.md). The skeleton uses focused real-process and CLI/IDE checks; milestone 4 adds a focused configuration/test-host suite and guarded real-PostgreSQL tests; milestone 5 adds the approved minimal USGS/Slack slice, whose live validation must be evidenced separately. See [the skeleton evidence](../evidence/reviews/2026-09-14-skeleton-review.md) for actual completed checks and pending validation. MCP connectivity alone does not validate product-database identity, application credentials, or readiness. [ADR-005](adr/ADR-005-direct-database-integration-and-workflow-owned-delivery.md) supersedes the earlier application ingestion/delivery API plan: n8n accesses the product database directly and implements retry/circuit-breaker behavior.

## Acceptance method

Use controlled events, fixed time where relevant, and isolated notification destinations. Verify the same canonical processing and delivery paths used by live ingestion. Keep configuration validation testable, and validate workflow/database contracts with the actual relational engine. Use the current workflow-owned contracts in the architecture; verify implementation-specific query/node behavior before accepting results.

## Unit and deterministic logic checks

- Application condition management: supported event type, allowed field/operator/value combinations, enabled state, ownership, and invalid destination selection.
- Matching correctness: threshold boundaries, non-matches, missing/invalid data, and unsupported conditions. The supported evaluator runs in n8n. Test the exact Code-node body with source-shaped fixtures, then execute the same path with PostgreSQL; do not create a duplicate C# matcher.
- When reusable pure workflow logic exists, test it independently with controlled inputs. Otherwise validate the matching sub-workflow through a defined input/output contract. Keep that distinction visible in evidence.
- Retry and circuit behavior: deterministic classification, due times, Closed/Open/Half-open transitions, independent Slack/email failure domains, and the recovery probe. These are workflow responsibilities, not tests of an application-owned breaker.

## PostgreSQL and management integration checks

- Apply EF migrations to the application database only. Verify compatibility of the resulting schema with n8n's explicit queries and the management/admin reads.
- Verify separate least-privilege credentials and permissions for application runtime, n8n product-data access, and migration tooling. Runtime credentials must not require schema-owner or superuser access. Do not create, inspect, or validate hosted n8n internal storage or its credentials; these are externally managed and outside this project.
- Test parameterized queries, valid condition storage, duplicate event/intent uniqueness, concurrent processing, and replay after an ambiguous database commit.
- Validate the final transaction boundary with real PostgreSQL. Do not substitute an EF in-memory provider for relational correctness or assume that multiple workflow nodes share a transaction.
- Prove recoverability if an execution stops between event acceptance, matching, and durable notification creation. Pending event evaluation must recover without another new source event. Selected-ID delivery remains manual; automatic recovery is deferred to milestone 6.
- Verify persistent notification/circuit state across workflow and process restarts in the product database, including restart while Open or Half-open.
- Test owner scoping for every management read/write with at least two owners, foreign IDs, and forged OwnerId form input. No demo role selection or authentication exists under ADR-008; ownership-aware queries must not be represented as a security boundary.

## Workflow-level validation

- Review and export definitions to Git; validate schema, inspect wiring, and exercise import/execution against the selected n8n version. JSON parsing alone is insufficient.
- Test source normalization and canonical validation with malformed, missing, unsupported, duplicate, and updated input. An unavailable source must produce an observable failure and allow subsequent recovery without stopping pending evaluation/delivery. Test first-snapshot duplicates with changed provider payload explicitly; no re-evaluation is expected under A-02.
- Test the workflow/database interface, not the superseded n8n/application HTTP API. External source/provider HTTP contracts still require authoritative documentation and focused tests.
- Verify transient database failure handling and idempotent replay where work may already have committed. Removing HTTP does not make SQL operations infallible.
- Test send retries through the workflow-owned circuit, including rate limits, temporary outage, shared credential failure, invalid destination, and unclassified errors. Avoid retry paths that skip the circuit or lose persistent attempt accounting.
- Under an open circuit, verify retained pending work and no new external send until a permitted probe. Test concurrent runs and independent channel recovery including a neutral permanent-destination probe outcome, expired probe recovery, and stale outcomes from older attempts.
- Inspect exports, pinned data, credentials, and execution logs for secrets. Record exactly which nodes were mocked; workflow test tools can still execute real side effects.

## Manual end-to-end and failure validation

Configure an alert through Razor Pages, inject a controlled canonical event through the agreed workflow entry, evaluate it, persist its delivery intent, send to authorized Slack/email test destinations, and inspect the result in the admin surface. Repeat with a non-match and a duplicate. Use a real provider path later to complement the deterministic demonstration.

In future delivery validation, simulate Slack failure in isolated product test work while email remains available. Observe independent circuit behavior, retained pending work, a recovery probe, and eventual resumed attempts. Do not restart shared DEV n8n/PostgreSQL or disrupt unrelated workflows. Shared-service crash/restart experiments require a separately authorized isolated environment; stopping and restarting the local application can be tested independently.

Simulate provider acceptance followed by a lost acknowledgement, or n8n stopping before recording success. For milestone 5, verify ambiguity remains visibly unresolved with no automatic resend. In milestone 6, verify automatic recovery/retry and report possible duplicate external messages honestly. This duplicate risk is accepted by the user; duplicated internal event/notification records remain defects. A provider acknowledgement is not proof of inbox placement or human receipt.

Inspect event/notification linkage, attempt details, failure state, and circuit/retry visibility where exposed by the final operational data contract. Admin displays must not silently take over workflow retry/circuit behavior.

External sends require explicitly authorized test destinations. Missing credentials or unavailable services are blockers for those checks, not passing tests. Label fixtures and simulated results clearly; no synthetic event should be mistaken for a real report.

## Configuration and evidence safety

Check missing/invalid settings and timing parameters without printing their values. Connection-string values must be absent from every tracked artifact, including examples, migration helpers, exports, logs, prompts, and evidence. Application and EF tooling obtain values from external configuration; n8n uses credentials. Do not claim that ignored files alone establish secret safety.

Store only actual screenshots, test output, and meaningful review notes in the existing evidence directories. Include the tested revision, procedure/command, expected and observed results, and limitations. Inspect for secrets and personal data; identify redactions. Record material corrections in [the AI review log](ai-review-log.md).

## Milestone 4 configuration validation

This section records milestone-4 configuration acceptance; it is not a prohibition on the separately approved milestone-5 runtime schema. Preserve one supported condition per alert; test create/edit/enable/disable, invalid configuration, shared profile validation and changes, atomic persistence, and ownership scoping with actual PostgreSQL where relational semantics matter. Do not create runtime event/delivery tables or evaluate events in the application. Review migration source/SQL and confirm the intended product database before shared DEV application. Use isolated test-owned state; do not reset a shared database. The amended specification uses users and alerts, with generated owner/profile/alert fixtures and exact cleanup for committed tests. Test missing-profile guidance, profile isolation, first-save races, stale settings, the alerts/users join, and rejection of ambiguous legacy migration target sets. PostgreSQL tests require both SONRISA_TEST_DATABASE and a matching SONRISA_TEST_DATABASE_NAME; no automatic server provisioning, EF InMemory or database reset is used.

Validate the proposed reproducible Tailwind build, Razor forms and accessibility, local/container asset serving, OpenTelemetry logs/request traces, no-endpoint startup, unavailable-exporter independence, and absence of secrets/sensitive form data in telemetry. Track actual outcomes in [milestone evidence](../evidence/reviews/2026-09-14-alert-configuration-validation.md); this strategy alone does not establish passing results. No local observability platform or metrics requirement is added.

## Checks for the topology amendment and skeleton milestone

For the topology amendment, inspect documentation consistency, links, supersession status, scope, and preservation of the existing n8n/application/product-database ownership. Verify that no premature application scaffolding or product implementation exists and unrelated work remains unchanged. Run focused whitespace/link checks and the required review. The topology review preceded application scaffolding; its historical evidence does not validate the subsequent skeleton. The original rejected HTTP/application-gate design must not reappear in active scope or acceptance checks.

For the current skeleton, verify the root solution and `src/` project layout in Rider, compatible application dependencies, clean build, Razor Pages response, local startup/restart, environment-aware configuration, and application liveness. Validate product PostgreSQL connectivity through safe runtime configuration when available; report database readiness separately from liveness. Check missing configuration without exposing secrets. Do not generate product entities or empty migrations to demonstrate connectivity, and do not create workflows or provision services. Validate the requested application-only Docker build, non-root execution, loopback host publishing, environment-file handling, and the same health semantics without duplicating shared services. Record only setup commands actually exercised. Later product failure/contract checks above are not skeleton acceptance requirements.

## Milestone 5 focused runtime acceptance

Use the [reviewed task plan](superpowers/plans/2026-09-14-first-runtime.md). Verify real USGS retrieval and the same normalizer with deterministic GeoJSON fixtures. Test below/equal/above numeric thresholds, disabled and unsupported conditions, malformed magnitude, no enabled candidates, source duplicates, first-snapshot preservation, repeated/partially completed evaluation, cross-owner alerts, immutable delivery destinations, and concurrent conditional claims. A zero-alert or zero-intent result must still complete the selected event.

Review the generated forward EF migration and SQL, verify `sonrisa_dev` using the application migration connection, apply only product changes, and inspect the resulting schema. Guard relational tests with the existing explicit target configuration and clean only their exact fixture IDs. Test actual n8n SQL, not only an equivalent test query. No shared-service outage is induced.

Before one real Slack send, verify a Sonrisa test alert, intended credential/channel and a single selected delivery ID. Repeat full entry to prove the claimed/sent intent does not send again. Test rejection and ambiguous outcomes using controlled mocks; do not cause duplicate external messages for testing. Email remains unsupported. n8n pinned tests do not prove PostgreSQL writes, live HTTP retrieval or Slack delivery; label them accurately. Export the actual tested graphs, inspect secrets and bindings, compare sanitized artifacts reproducibly, and verify all workflows are inactive. No completion claim substitutes for missing runtime configuration.
