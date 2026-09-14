# End-to-end validation

Date: 2026-09-14

## Authority and reconstructed baseline

[Prompt 028](../../../prompts/028-end-to-end-validation.md) authorizes unattended validation, minimal genuine defect corrections, evidence, review and one commit. The follow-up explicitly permits branching from current `main` when it contains the completed milestones. `test/end-to-end-validation` starts at `eaa588b`, following Email `a198778`, simplified runtime `c38f2a3`, historical SMTP `e1dc337` and first runtime `b9cf171`. The initial working tree was clean. Preserve history and leave the new branch checked out without merging.

Read-only reconstruction covered current documents, ADR supersessions, specifications/plans, prompt history, evidence, application/tests and workflow sources. Rider recognizes both projects. PostgreSQL MCP identifies `sonrisa_dev`, only users/alerts/EF history, six migrations, four owners and five alerts (one enabled). Fresh private row digests are the cleanup baseline; historical digests are not assumed current. The sole searchable current Sonrisa workflow is inactive `aVijfnQr0kdLAJHP`, 26 nodes, version `5880b333-24c6-4cfd-8d52-ae45f5d5f2b1`.

## Validation design

Validate the existing MVP, without new features. Combine the complete .NET/Node suites, guarded PostgreSQL integration tests, real loopback application checks, read-only schema inspection, exact remote-export comparison, and controlled executions of the existing n8n pipeline. Add focused successful HTTP edit/revision/ownership coverage where existing tests leave a gap. Tests must fail on a wrong result; no test-count target or duplicated matcher.

Normal management remains configured-owner scoped; admin deliberately reads across owners. Product data remains configuration only. Runtime matching, bounded native history, retries and transport remain in one inactive n8n workflow. Application Npgsql spans are deliberately deferred under ADR-009; validate their absence and request-level correlation rather than add instrumentation.

For workflow tests, preserve workflow/dedup node identities and existing history. Suppress real transports for fixture, live-source and failure tests. Use the established temporary transport mock with native retry settings and error-only failure output; counters observe attempts only. Remove instrumentation afterward. Reuse trusted real Slack evidence if its credential/source/configuration contract remains unchanged. Make at most one fresh synthetic SMTP4DEV submission after proving precisely one test-owned Email recipient is eligible; capture proves SMTP submission only. Never replay a send node to test deduplication.

Use generated fixture owners/alerts only, verify the target before writes, and guarantee exact-ID cleanup. Do not disable or modify existing owners/alerts, stop shared services, clear native duplicate history, apply migrations, publish a schedule, or add top-level workflows. Simulate source/configuration/transport failures in the existing inactive workflow or test host. Unsupported persisted condition combinations are constrained by the real schema; use clearly labeled pinned query envelopes for malformed configuration.

## Acceptance and evidence

The plan maps all 22 requested validation areas to application, runtime and hygiene checks. Record actual commands, counts/skips, execution IDs, mock boundaries, real source/capture observations, export parity, cleanup and limitations. Answer all 21 final self-review questions explicitly in evidence. A missing required credential, material previous-milestone defect, destructive infrastructure/migration need or architecture conflict stops dependent work. Routine test choices are self-approved by the user request.

Known limits remain: no authentication, bounded/non-atomic dedup history and capacity failures, changing USGS IDs, best-effort loss/duplicates, no durable recovery, inactive/manual DEV workflow, accepted DEV privilege/TLS exception and no external mailbox proof. Do not rewrite the final reflection.
