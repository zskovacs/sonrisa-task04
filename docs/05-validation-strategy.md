# Validation strategy

The current runtime acceptance contract is [ADR-012](adr/ADR-012-use-n8n-native-runtime-state.md) and the [approved specification](superpowers/specs/2026-09-14-runtime-simplification-design.md). Tests for historical PostgreSQL event/delivery states do not prove native n8n deduplication or retry behavior.

## Management configuration

Retain focused .NET validation, EF model, ownership, shared profile/revision, transaction, page/antiforgery, health and OpenTelemetry tests. Run `dotnet test Sonrisa.sln`; relational tests require externally supplied `SONRISA_TEST_DATABASE` and matching `SONRISA_TEST_DATABASE_NAME`. Verify the target before any fixture write. Tests use exact generated IDs and rollback/cleanup; never reset unrelated shared data. Run the build and EF pending-model check after model/migration changes.

The forward runtime removal must change only the two obsolete runtime tables and their dependent objects. Review migration source and incremental generated SQL, check current database/history/counts/dependencies, and keep any restricted backup outside Git. After application, verify only justified configuration tables and EF history remain, and exercise actual management behavior. Historical migrations must be unchanged.

## Exact workflow logic and export

Run `node --test n8n/tests/*.test.mjs` against the exact source inserted into Code nodes. Cover canonical mapping and input bounds, malformed/non-numeric magnitude, multiple events in a batch, numeric threshold below/equal/above, disabled/malformed conditions, owner-independent configuration, safe Slack text and independent channel validation. Email/unknown channel cases must yield visible skips, not success.

Validate the SDK against actual installed node types. Inspect the retrieved remote graph: native within-input dedup followed by previous-execution dedup, parameterized SELECT only, n8n matching, batch-size-one channel loop, explicit Slack success/error feedback, no active schedule/email transport/runtime writes. Export the actual tested graph; sanitization must be reproducible and reject behavior drift, unsafe parameters, secrets and test input. Test export/import representation and compare remote/local artifacts.

## Live n8n evidence

| Case | Required observation |
| --- | --- |
| Real source | Real USGS request succeeds; IDs, UTC timestamps, numeric magnitude and optional fields match the normalizer contract. Transport stays suppressed during source/batch testing. |
| Deterministic source | Clearly marked `demo.usgs` earthquake-shaped fixtures use the same normalizer and downstream path. Future live triggers cannot select fixtures. |
| Native within-batch dedup | Repeated source/ID in one batch produces one retained item. |
| Native across-execution dedup | First occurrence passes; a separate later execution using the same workflow/node identity filters it. Test state after a downstream failure. Do not claim a shared-instance restart was tested unless it actually was. |
| Bounded history | A controlled small cap demonstrates installed-version behavior, then restore 10,000. Do not silently clear production history or claim permanent/atomic deduplication. |
| Typed match | Below threshold produces no send; equal and above reach Slack routing; malformed and disabled configuration produces no false match. Confirm multiple owners are considered. |
| Unsupported channel | Email and unknown type are visibly skipped; unrelated Slack items continue. |
| Native retry isolation | Safely mock the transport in the real engine: A fails five total attempts, then discard is visible and B continues. Pinning successful Slack output alone is insufficient retry evidence. Remove test-only instrumentation afterward. |
| Real Slack | Preflight one test alert, exact authorized channel/credential and synthetic event. Perform one controlled send through the full pipeline, inspect transport acceptance, repeat full entry and observe no second send. Never directly replay the send node as a dedup test. |
| Database unavailable | Simulate without disrupting shared services; fail visibly, documenting that already-seen events may lose their notifications. No product retry state is created. |

After retry exhaustion the notification is discarded. Ambiguous transport failures may duplicate or lose messages. Events seen before new/changed alerts are not rematched. These are intentional acceptance semantics, not missing recovery features.

## Safe transition and review

Keep old workflows inactive; archive them only after replacement acceptance. Preserve execution and Git history. Final graph remains inactive with manual live/empty-fixture defaults; no recurring activation. Record exact execution IDs and mock boundaries, sanitized outcomes, applied migration ID and commands/results in evidence. Never store raw credentials, connection values, unrelated executions or full source payload dumps.

Use per-task review and final whole-branch review. Resolve material findings before the milestone commit. Existing Slack/SMTP evidence remains historical and is not relabeled as validation of the replacement. Email transport and product admin validation belong to separately authorized later milestones.
