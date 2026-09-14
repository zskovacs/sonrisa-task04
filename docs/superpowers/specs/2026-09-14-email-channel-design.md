# Email channel extension design

Approved on 2026-09-14 in this session, with refinements to [request 026](../../../prompts/026-add-email-channel-to-existing-n8n-workflow.md). SMTP4DEV capture is explicitly approved. Implement and commit this milestone without another generic approval; stop for missing/unusable credentials, an unexpected migration, destructive shared-infrastructure work, or architecture conflict.

## Implementation status

Implementation and controlled validation are complete. The final remote workflow is version `5880b333-24c6-4cfd-8d52-ae45f5d5f2b1`, inactive, and is the sole Sonrisa workflow returned by DEV search. [Validation evidence](../../../evidence/reviews/2026-09-14-email-channel-validation.md) records the one SMTP4DEV capture, mocked transport isolation and cleanup. Final whole-branch review approved the implementation with no findings; it is ready for the requested milestone commit.

## Architecture and scope

Extend only `Sonrisa - Process Alerts - DEV`, ID `aVijfnQr0kdLAJHP`, inactive on n8n 2.38.7. Baseline is commit `c38f2a3`; the actual 22-node remote graph matches the export. Three historical workflows are archived. PostgreSQL has only users, alerts and EF history. ADR-012 and ADR-010 remain authoritative.

Preserve ingestion, canonical normalization, both native dedup nodes and their identities/history, typed numeric matching, ownership, PostgreSQL schema and ASP.NET. Add `name` to the existing configuration SELECT projection and carry `alert_name` on notification items solely for message formatting. This is the explicit permitted exception to byte-for-byte unchanged upstream code; query selection, joins, parameterization and matching semantics remain unchanged. Do not alter Slack preparation, settings, credentials or return paths.

The existing evaluator already emits independent Slack/Email destinations from each matched owner's profile. Keep the existing batch-size-one loop and Switch. Replace the unsupported Email branch with preparation, native SMTP send, acceptance projection and invalid/exhausted diagnostics; all terminal paths return to the same loop. Keep unknown channels visibly skipped. No new workflow, sub-workflow, state store, queue, worker, retry subsystem, schema, dependency, application sender, schedule or admin UI.

## Email contract

Recipient comes from the matched owner's persisted `email_destination`. Validate a single bounded bare mailbox without controls or recipient lists before transport. Use a plain-text subject `Sonrisa alert: <event title>` and body with alert name when valid, magnitude, UTC time, source name and optional validated HTTP(S) URL. Bound/sanitize untrusted text and mark synthetic messages clearly. Do not include alert UUIDs, execution IDs or other internal debugging identifiers in user-facing text. Missing/invalid optional alert name must not change matching or suppress valid Slack.

Use native `emailSend` v2.1 (`resource=email`, `operation=send`, `emailFormat=text`, attribution off), n8n-owned `SMTP account` credential, and the known SMTP4DEV sender `sonrisa@example.test` as a non-secret transport setting. No CC/BCC, attachments, response waits, HTML, TLS-validation bypass or arbitrary sender/recipient override from event data. A real SMTP provider requires credential/sender configuration changes only.

Native retry settings: `retryOnFail=true`, `maxTries=5`, `waitBetweenTries=5000`, `onError=continueErrorOutput`. Five total attempts, then a safe `email_retries_exhausted_discarded` diagnostic and loop continuation. Invalid preparation similarly discards without sending. On SMTP success, project `email_accepted` only for exactly the intended accepted recipient, no rejected recipient and a matching envelope when present. Error-shaped or inconsistent results produce `email_outcome_unconfirmed_discarded`, never false success or a resend. Diagnostics retain safe source/event/alert identifiers but omit addresses and raw errors. Delivery remains best-effort, with possible ambiguity loss/duplicates and no recovery.

## Validation and acceptance

Local exact-source tests cover formatting/injection, missing/invalid configuration, result acceptance, both channels and ownership, unchanged matching/source behavior, and exporter graph/security guards. Use the same authoritative workflow and fixture entry for remote tests. Pin/mask transports during upstream tests; no real Slack send is needed because its implementation remains unchanged and real evidence 52/53 is retained.

Use controlled temporary transport mocks in the real n8n engine to prove Email A fails five times, is discarded, then Slack B and Email C proceed; also exercise failed Slack followed by Email. Match native error-only output shape, distinguish all mocks from real sends and remove instrumentation before final export. Test unsupported channels, missing/invalid email, both-channel fan-out, real USGS with transports suppressed, repeated event-level dedup, and actual all-owner configuration reads. Use exact test-owned configuration IDs and restore preexisting rows unchanged; no schema writes.

After no-send preflight proves exactly one eligible Email and no live Slack, make one controlled synthetic SMTP submission using the persisted recipient and intended credential. Verify SMTP4DEV capture and content; repeat the full entry and observe no second send. Capture proves SMTP transport and message capture, not external inbox delivery. Restore live/empty defaults and inactivity, preserve graph identity, and export the actual tested graph with credential references, pins and mocks removed.

Update only affected current documentation and actual evidence. State the alert-name projection exception honestly. Required final review answers the ten architecture questions in request 026. Final commit: `feat: add email notification channel to n8n workflow`. No interim implementation commits or unrelated changes.
