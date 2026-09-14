# Initial validation strategy

This document describes future checks. No application tests, running infrastructure, or executable n8n workflows exist in this baseline. Documentation inspection is not evidence of runtime correctness. Test tools and commands will be selected with the application stack and recorded when they actually exist.

## Acceptance method

Agree observable examples with the product owner before implementing the first slice. Use controlled inputs, fixed time where relevant, and isolated notification destinations for repeatable validation. Keep fast deterministic checks near domain logic; exercise real persistence/API contracts and selected n8n integrations at their boundaries. Include failure paths from the first implementation task.

| Area | Future check | Evidence to capture when performed |
| --- | --- | --- |
| Deterministic domain logic | Unit-test agreed matching/non-matching cases, supported boundaries, missing values, and invalid rules. Do not invent semantics in tests. | Actual focused command, revision, and output. |
| Persistence/API boundaries | Integration-test canonical validation, durable writes, ownership/access checks, and the agreed transaction and restart behavior. | Actual results with environment and relevant contracts identified. |
| Workflow validation | Inspect exported JSON, verify import/execution with the selected n8n version, check mappings and credential references, and exercise success/error branches. | Reviewed export revision and actual execution or validation notes. A parse check alone is insufficient. |
| Duplicate source events | Replay the same source event and exercise concurrent/repeated ingestion under the agreed identity/update rules. | Observed event/evaluation outcomes compared with the agreed policy. |
| Duplicate notifications | Replay evaluation and delivery work, including concurrency and restart, and verify one intended delivery per agreed identity. Separately test transport-level duplicate risks. | Actual delivery records and observed sends, with limitations identified. |
| Temporary failure and retry | Make transport fail temporarily, recover, and observe retry limits and durable outcomes. Also exercise exhaustion and success followed by a lost acknowledgement. | Failure/recovery observations and any uncertain outcome, without claiming exactly-once delivery unless established. |
| Malformed external input | Submit invalid or incomplete payloads and unsupported values at the canonical boundary. | Rejection/handling outcome and confirmation that invalid input cannot silently produce an unintended notification. |
| Unavailable source | Exercise timeout, unavailable-source, or rate-limit behavior for the chosen integration. | Observed error visibility, recovery, and subsequent ingestion behavior. |
| Configuration | Check missing/invalid settings and credential references; verify that configuration errors surface clearly without leaking secrets. | Actual validation output with sensitive data excluded. |
| Basic security | Inspect committed files, exports, fixtures, prompts, and logs for secrets; verify agreed user/admin permissions, ingress authentication where required, and safe handling of untrusted content in the UI and notifications. | Actual inspection notes and focused security checks; avoid claiming a comprehensive audit. |
| Manual end-to-end path | Configure an alert through the chosen UI, ingest a controlled matching event, deliver to authorized email and Slack test destinations, and inspect the result in the admin surface. Include a non-match, a duplicate, and a temporary delivery failure. | Actual demo steps, outcomes, and optional real screenshots, linked to the tested revision. |

When real sources and credentials are available, complement controlled fixtures with a real source-to-channel demonstration. Label a simulated or stubbed result as such. External sends must use destinations explicitly authorized for testing. Missing credentials or unavailable services are recorded blockers, not passing tests.

## Evidence and review discipline

Store genuine screenshots in `evidence/screenshots/`, actual test output in `evidence/test-output/`, and substantive review/validation notes in `evidence/reviews/`. Include date, tested revision or precise change description, command/procedure, expected and actual outcomes, and limitations. Inspect for secrets and personal data before committing; label any redaction or omission. Do not create artificial evidence to populate directories.

For each material change, inspect the diff, run the smallest applicable check, and review before acceptance. Escalate meaningful failures, update assumptions when evidence contradicts them, and record substantive AI corrections in [the AI review log](ai-review-log.md). Finish with whole-change review against agreed scope and a reflection based on actual results.

## Checks applicable to this baseline

Review documentation consistency, links, distinction between facts/proposals/open questions, n8n rationale and boundaries, deferred UI choice, scope exclusions, prompt-history rules, and absence of implementation or fabricated evidence. Verify that placeholders are minimal, existing work is preserved, and the staged diff contains only bootstrap files. Run Git whitespace checks before committing. Record only checks actually performed; do not invent build or test commands for an application that does not exist.
