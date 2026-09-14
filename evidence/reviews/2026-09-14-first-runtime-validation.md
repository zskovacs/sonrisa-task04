# First runtime slice — validation record

Date: 2026-09-14. Validated on `feat/first-n8n-alert-workflow`, baseline `fd2a9e6`. This record preserves intermediate failures and their later corrections; the final acceptance results appear at the end. The user-approved [simplification](../../prompts/022-simplify-first-runtime-workflow-design.md) and [specification](../../docs/superpowers/specs/2026-09-14-first-runtime-design.md) govern scope.

## Reviewed specification and schema

The independent plan review identified that a zero-alert SQL result could stop n8n before event completion. The plan now requires one event/empty-alert envelope and a count summary for empty intent inserts; scoped re-review approved. Task1 independent review approved the two-table model/migration with no findings, with relational validation explicitly pending.

Local model tests failed before the entities existed, then passed 2/2. The worker's full local `dotnet test Sonrisa.sln --no-restore` returned 56 passed, 19 guarded skips, 0 failed. `dotnet ef migrations has-pending-model-changes --project src/Sonrisa.Web` reported no pending model changes. These results did not prove database behavior.

## Migration target and application

The current shell had no application User Secrets connection. A temporary ignored helper resolved the existing ignored Compose environment in memory and passed only the connection key to EF tooling; it did not print, persist or commit the value. Using that same connection, read-only identity inspection returned `sonrisa_dev`, role `postgres`, superuser true and PostgreSQL TLS false. These are the existing DEV application limitations accepted in ADR-007, not the desired n8n/production permission model.

Before applying, the database contained the three expected configuration migrations and only `20260914155714_FirstRuntimeSlice` pending. Controller and task reviewer inspected the generated migration; controller also inspected its incremental SQL. The forward script creates exactly `public.source_events` and `public.notification_deliveries`, their constraints/indexes and restricted FKs, and the EF history entry. It does not change users/alerts columns, unrelated objects, roles or n8n internal storage.

`dotnet ef database update 20260914155714_FirstRuntimeSlice --project src/Sonrisa.Web --no-build`, with externally resolved configuration, exited 0 and reported application of that migration. Subsequent same-connection inspection returned all four applied and no pending migrations. PostgreSQL MCP independently confirmed all new columns, both uniqueness indexes, the pending-event index and restricted foreign keys in the migration contract.

## First guarded runtime test execution

The temporary helper checked the exact database identity before launching the existing test guard with `SONRISA_TEST_DATABASE` and `SONRISA_TEST_DATABASE_NAME`. No connection value entered the command output.

`dotnet test Sonrisa.sln --no-restore --verbosity minimal --filter FullyQualifiedName~PostgresRuntimeTests` ran all six new relational cases: 4 passed, 2 failed. Both failures were test assertions expecting SQLSTATE `23503` for deletion through `ON DELETE RESTRICT`; the server returned `23001` (`restrict_violation`). Both attempted deletions were rejected. The [PostgreSQL 18 constraint documentation](https://www.postgresql.org/docs/18/sql-createtable.html) distinguishes RESTRICT from NO ACTION. The two expectations were changed to the provider constant `RestrictViolation`, with no runtime/schema changes. The same guarded command then passed 6/6 with zero skips. Independent scoped re-review approved the correction with no findings. Tests use generated IDs and rolled-back transactions; no unrelated records are reset.

The full guarded `dotnet test Sonrisa.sln --no-restore --verbosity minimal` subsequently passed **75/75**, zero failures/skips, in 36 seconds against the same verified DEV target. This includes existing management/ownership/telemetry tests and the new runtime relational tests.

## n8n target verification

A targeted local MCP configuration inspection returned only the n8n server host/path, confirming the available n8n MCP endpoint is `n8n.nasgard.io/mcp-server/http`. Authentication fields were not output. This establishes the deployment target without modifying a workflow or shared configuration.

## Actual n8n workflows and execution evidence

The three saved graphs were inspected through MCP, including names, nodes, connections, credential metadata and activation flags. All are inactive:

- [Sonrisa - Ingest Earthquakes - DEV](https://n8n.nasgard.io/workflow/K3A9cowlgglFqriG): 8 nodes.
- [Sonrisa - Evaluate Pending Events - DEV](https://n8n.nasgard.io/workflow/zZYWvZSk1LDGhgJa): 6 nodes.
- [Sonrisa - Deliver Slack Notification - DEV](https://n8n.nasgard.io/workflow/5fGfgXCBLxhNO479): 12 nodes, including explicit claim and result/error branches.

Creation automatically bound newly available `Postgres account` and `Slack account` credentials. Only names/IDs were inspected, never secret material. A temporary read-only final SELECT in ingestion verified the actual n8n connection reaches `sonrisa_dev` and both runtime tables. Its role is `postgres`, with superuser and public-schema CREATE privileges, and no PostgreSQL TLS. This is broader than the intended separate least-privilege workflow role; no grant, role or shared configuration was changed. The user instructed documenting an available broader DEV credential rather than redesigning shared permissions.

| Execution | Actual observation |
| --- | --- |
| 1 | Real USGS HTTP retrieval, 8 normalized events, no record diagnostics. Final node temporarily performed only the database identity/schema SELECT, no source insert. |
| 2 | Restored exact reviewed insert: 8 USGS events persisted Pending. Single JSON query binding worked on the actual Postgres node. |
| 3 | Repeated live polling inserted 0 additional events. |
| 4–11 | Eight real events reached Evaluated through the real configuration read, Code evaluator, intent insert and completion nodes. Execution 4 had zero intents and still completed. |
| 12 | Controlled source-shaped fixture used the same normalizer/insert, forced source demo.usgs, and inserted 3 events. |
| 13 | Repeated fixture ingestion inserted 0 additional events. |
| 14–16 | Real n8n evaluation of the three fixture events completed; database verification below distinguishes test-owned from pre-existing alert matches. |
| 17 | Pinned delivery test with unacknowledged Slack response reached the ambiguous-outcome branch. |
| 18 | Pinned delivery test with ok=true reached the recorded-success branch. |
| 19 | Live selected-ID delivery failed before the claim: n8n could not decrypt Postgres account. No Slack node executed; selected intent remained Pending. |
| 20 | Pinned malformed ingestion test ran the actual selector/normalizer: one valid demo.usgs event survived; string/null magnitudes produced invalid_magnitude diagnostics and a quarry blast produced unsupported_feature. Trigger, operator input, HTTP and PostgreSQL were pinned; no source request or database write occurred. |
| 21 | After PostgreSQL credential repair, live evaluation completed on an empty queue, with zero rows from the pending-event read and no downstream writes. |
| 22 | Live delivery claimed the originally selected intent, then Slack credential decryption failed before contacting Slack. The error branch persisted delivery_outcome_unknown while retaining Processing; no Sent result. |
| 23 | After Slack credential repair, a different explicitly selected synthetic intent was claimed. The Slack node reported missing required OAuth scopes; the conservative error branch retained Processing with delivery_outcome_unknown. No acknowledged send. |
| 24 | After correcting the classifier, a pinned reproduction of execution 23's exact native error string reached Record known Slack rejection. Only the existing IF executed; every database/Slack node and the trigger were pinned. |
| 25 | The same pinned classifier test with a timeout string reached Record ambiguous Slack outcome. No database write or send occurred. |
| 26 | After user-created Sonrisa DEV Slack became accessible and was explicitly bound, a different Pending synthetic intent reached Slack. Slack rejected it with not_in_channel; the corrected classifier and real SQL recorded Failed/slack_rejected with no SentAt. |
| 27 | After the user added the bot, intent d4ee0a32-946e-48b8-a4db-bbb0657800da sent one synthetic fixture-51 notification. Slack returned ok=true, channel C0C1QRQDTSN, app A0C1LMW5S0M and bot B0C1K5D8ZLK. Real SQL recorded Sent with a timestamp. |
| 28 | Full re-entry for that same Sent intent made no Slack call. The zero-row UPDATE claim emitted n8n's generic success=true item; the next node failed on its absent ID. This exposed a result-shape defect to correct before clean replay acceptance. |
| 29 | After the same conditional UPDATE was wrapped in a CTE with a final SELECT, full re-entry of the Sent intent succeeded. Claim output was an empty array; no event-read, preparation or Slack node ran. |

The two pinned delivery tests executed pure shaping/branch nodes but mocked every PostgreSQL/Slack node and the trigger. They did not write results or send messages. Full execution-history dumps are not committed; the execution viewer and domain IDs carry investigation context.

### Deterministic threshold and ownership results

Two isolated, generated test owners received explicit test alerts with threshold 5, plus a disabled alert. The first owner had Slack and synthetic email settings; the second had Slack. Every test used real persisted configuration. Existing alert/user settings were preserved. Runtime correctly considered the pre-existing enabled alert too; its intents are not counted as failures of the test threshold.

| External ID | Magnitude | First test owner intents | Second test owner intents | Disabled test alert | State |
| --- | --- | --- | --- | --- | --- |
| sonrisa-fixture-49 | 4.9 | 0 | 0 | 0 | Evaluated |
| sonrisa-fixture-50 | 5.0 | 1 Slack Pending, 1 email Unsupported | 1 Slack Pending | 0 | Evaluated |
| sonrisa-fixture-51 | 5.1 | 1 Slack Pending, 1 email Unsupported | 1 Slack Pending | 0 | Evaluated |

The pre-existing alert produced two additional intents per fixture event (Slack/email). No queue-drain workflow exists and none of these were sent. Source fixture timestamps and titles remain synthetic; they are not presented as real earthquakes.

The table captures intent state immediately after evaluation. Subsequent delivery attempts changed the first test owner's Slack intents for fixture-50 and fixture-51 to Processing with an unknown-outcome diagnostic, as recorded below.

### Exact workflow SQL verification

Five additional guarded tests execute the actual ten SQL files and call the exact normalizer/evaluator bodies through Node. Final corrected filter `FullyQualifiedName~PostgresWorkflowSqlTests` passed **5/5**, no failures/skips, against verified DEV. It covered concurrent source insertion (one insert winner), preserved first magnitude, typed boundary matches across two owners, disabled exclusion, empty-intent summary and completion, invalid intent JSON leaving Pending, partial committed evaluation replay with unchanged destination, one winner for concurrent Slack claims, outcome updates, and email exclusion. No external transport was invoked by these relational tests.

Task review required eliminating a direct minimum-threshold test query so even test configuration reads use the exact candidate query. The repaired no-match test derives its assertions from that envelope; the same live filter passed 5/5 and scoped re-review approved. Existing configuration is not disabled to force a zero-enabled global snapshot. The empty-alert Code case is covered locally and the query's COALESCE envelope is reviewed, but a live globally-zero-enabled snapshot is not claimed.

### Controlled Slack destination and initial blockers

The user confirmed `Slack account` as the intended DEV credential and explicitly authorized channel `C0C1QRQDTSN` for one test. The channel was also present in current Sonrisa settings; the credential's channel-list lookup returned no metadata, so no channel name was inferred. The selected test intent is `c1a44cab-dd35-4602-81bf-d4567f274305`, source event `d35bd454-e8f8-43e4-a0f6-50321d42f62f`, alert `50ea4648-c1c1-4f8c-8898-d409050d28dc` (Sonrisa DEV runtime test first_alert). Its destination snapshot matched the authorized ID and prepared text had an explicit SYNTHETIC marker.

One initial execution-tool response was unusable; read-only execution history showed only dry runs and the intent was still Pending. Re-entry through the claim gate started execution 19, which failed at credential decryption before any claim or Slack call. This is not an ambiguous provider acceptance. No automatic retry of a Processing intent occurred. The user was asked to repair the credential in n8n; secrets and global encryption configuration remain outside agent changes. Operator delivery input was restored to empty while waiting.

After the user repaired PostgreSQL, execution 21 verified database access with an empty queue. Execution 22 claimed that same Pending intent, but the Slack credential could not be decrypted before an API call. The workflow conservatively recorded `delivery_outcome_unknown` and retained Processing. The row was not reset or retried.

After the user reported Slack repaired, execution 23 selected a different existing Pending intent: `6f11ca8b-4beb-407d-8d2c-f2f90ea2587e`, source event `c84a6771-970e-44a9-ada7-28e71a7d1e18` (`demo.usgs`, `sonrisa-fixture-51`), same test alert and authorized destination. Decryption succeeded, but the native node returned `Your Slack credential is missing required Oauth Scopes`. Its error output exposed only a string, so the conservative unknown branch ran. The intent remains Processing with `delivery_outcome_unknown`, not Sent. Both retained rows require later operator investigation; neither is eligible for another automatic claim. The operator ID was restored to empty and the workflow remains inactive. The user was asked to grant the app's required posting scope and reauthorize in n8n without sharing secrets. Slack documents `chat:write` for [chat.postMessage](https://docs.slack.dev/reference/methods/chat.postMessage/).

## Export reproducibility review

The initial export helper depended on an ignored session snapshot path, so its tests would not run in a fresh checkout. The controller rejected that dependency. The corrected CLI takes an explicit input file or stdin; tests use the tracked sanitized exports and inject synthetic metadata in memory. All 17 Node tests passed after the correction. Exports intentionally remove credential bindings, pinned data and remote execution/project/version metadata, preserve the reviewed graph and exact Code/SQL, and require explicit credential rebinding on import. Independent scoped review approved with no findings and reran all 17 tests. After execution 23 and default restoration, fresh MCP workflow objects for all three workflows sanitized byte-for-byte to the tracked exports; all were inactive with live/empty ingestion and empty delivery defaults. The CLI expects the response's workflow object, not the containing MCP response.

## Acceptance checks before the final send

### Whole-branch review correction

The final reviewer found one Important defect: the known-rejection IF assumed `$json.error.code`, but actual n8n execution 23 emitted a string. This was conservative against duplicate sends but inaccurate delivery-state reporting. The corrected existing IF accepts an exact allowlist of structured rejection codes and native error strings, including the observed missing-scope message. Unknown, malformed, timeout and misleading substring errors remain unknown. The official [n8n Slack source](https://github.com/n8n-io/n8n/blob/master/packages/nodes-base/nodes/Slack/V2/GenericFunctions.ts) confirms the native mapping. No new node, table, retry or historical-state rewrite was added.

The new regression failed on the original expression and passed after correction. MCP validated the updated IF and complete delivery SDK. For executions 24/25, the inactive manual trigger was temporarily connected to that IF; all database/Slack nodes and the trigger were pinned. After those checks, the normal explicit-ID/claim path was restored, its sorted graph edges matched the original graph, and the delivery ID remained empty. A new actual remote snapshot produced the tracked delivery export. The complete Node suite then passed **20/20**. Existing Processing rows were preserved. Scoped re-review subsequently approved the correction; the external scope configuration blocker was separate from this code defect.

After adding the exact-query tests, the full guarded `dotnet test Sonrisa.sln --no-restore --verbosity minimal` passed **80/80**, zero failures/skips, in 36 seconds. The helper first confirmed `sonrisa_dev`, all four applied migrations and no pending migrations. This broader run includes the existing management/ownership/telemetry suite and both runtime relational suites; it does not substitute for a successful external Slack send.

`dotnet ef migrations has-pending-model-changes --project src/Sonrisa.Web --no-build` reported no changes since the last migration. `git diff --check` passed. A focused scan of 59 changed/untracked milestone paths found no Slack-token, private-key, connection-literal or bearer-token patterns; manual export inspection additionally verified omitted credential bindings and pin/history data. A local-link check resolved all 164 links in changed documentation (historical prompt bodies excluded). These checks do not prove the absence of every possible secret format or runtime issue. Final whole-branch review follows the final runtime checks below.

One real Slack notification is acknowledged and persisted (execution 27, below). The clean repeat-entry correction passed in execution 29. No schedule has been activated. Final review and the requested commit follow these verified acceptance results.

## User-authorized Slack app setup

The user installed the Slack CLI and explicitly requested creation of the required app/scopes and n8n credential configuration where possible. CLI version was 4.7.0; `slack auth list` showed the sole authorized workspace `nasgard`, `T0C1L4SQSJH`. A minimal manifest requests only bot `chat:write`, without events, socket mode or a hosted runtime. Local CLI hook/configuration files were kept in the ignored task directory; no package or Slack-hosted code was installed. An initial bare `cat` manifest hook rejected the CLI's appended `--source` option; a static Python file-reader hook that ignores those arguments resolved it.

`slack manifest validate --team T0C1L4SQSJH --no-color --skip-update` returned Valid. `slack app install --team T0C1L4SQSJH --environment deployed --no-color --skip-update` created and installed **Sonrisa DEV Alerts**, app `A0C1LMW5S0M`. CLI installation output was captured with secret-bearing lines suppressed; no token was printed or placed in Git/evidence. `slack api auth.test --app A0C1LMW5S0M --team T0C1L4SQSJH --no-color --skip-update` succeeded as `sonrisa_dev_alerts`, user `U0C1PDTSA4E`, bot `B0C1K5D8ZLK`, in the intended workspace. Remote `slack manifest info` confirmed only `chat:write`. A non-secret copy is [versioned](../../n8n/slack-app-manifest.json). A read-only channel-info call reported missing read scope and confirmed provided scope `chat:write`; channel-reading permission was not added just for tooling convenience.

The available n8n credential-schema API call returned 401 Unauthorized with the existing MCP authorization; browser control was unavailable. No credential was created/modified through that API. The user was asked to add this bot to `C0C1QRQDTSN` and copy its bot token directly into a new n8n credential named `Sonrisa DEV Slack`. These are the remaining external configuration steps before selecting another untouched Pending synthetic intent for the single authorized send. The previous `Slack account` credential and Processing intents were not replaced/reset. The classifier fix received an independent scoped APPROVE after 20/20 Node tests and byte-identical refreshed export comparison; whole-milestone acceptance still requires the live send and repeat-entry check.

After the user's follow-up, MCP exposed `Sonrisa DEV Slack` (`slackApi`, ID `Yg0unLofRQ9m41sT`), which was explicitly bound to the one Slack node. No other credential was changed. Execution 26 selected Pending intent `b2f0e03f-8cec-453a-9458-482d5149c149`, second test alert `429c0d61-1c52-4e95-8d6e-25d98fd85801`, fixture-50, authorized destination `C0C1QRQDTSN`. Slack returned the exact native error `Slack error response: "not_in_channel"`. The corrected known-rejection branch executed real SQL; PostgreSQL independently confirmed `failed`, `slack_rejected`, and null `sent_at`. This is actual failure-path validation, not a successful notification. The selected ID was cleared again. The user was asked specifically to add the newly created app to the channel; credential access is resolved, membership remains the blocker.

The user then confirmed adding the new app. Execution 27 selected untouched Pending intent `d4ee0a32-946e-48b8-a4db-bbb0657800da`, second test alert, canonical event `c84a6771-970e-44a9-ada7-28e71a7d1e18` (`demo.usgs`, `sonrisa-fixture-51`). Slack acknowledged one message with `ok=true`, the authorized channel and the expected new app/bot IDs; provider message timestamp was `1789405418.135849`. PostgreSQL independently returned `sent`, `sent_at=2026-09-14T17:03:38.255236Z`, and null error. This proves provider acceptance, not human receipt. Execution 28 re-entered through the full claim gate and did not execute Slack again, but a native zero-row UPDATE result caused a downstream missing-ID error. The later correction below changes that result shape without weakening the atomic claim. The operator ID was cleared, and the three exact test alert IDs were disabled through guarded parameterized updates in the verified product database. Existing user configuration and all evidence intents were preserved.

## Final runtime acceptance

The claim SQL now uses one CTE containing the unchanged conditional UPDATE and returns its explicit columns through a SELECT. It remains a single atomic claim, with the same UUID parameter, status predicate and returned IDs/destination; no matching logic or larger transaction was added. This changes the native n8n zero-row response from a generic success item to an empty result. The complete updated SDK validated, then live execution 29 re-entered the already Sent intent and finished successfully at the claim with `main: [[]]`. Slack was not invoked. The successful message was not resent or its state reset.

After restoring the empty delivery ID, fresh MCP details confirmed all three final workflows inactive. Their actual definitions were sanitized into `n8n/workflows/`; exports retain the tested Code/SQL/graph and strip credentials and temporary metadata. Final Node tests passed **20/20**. The final full guarded .NET suite after the SQL correction passed **80/80**, zero failures/skips, in 36 seconds on verified `sonrisa_dev`, with all four migrations applied and none pending. Independent PostgreSQL inspection confirmed all three exact test alerts disabled. No credentials or unrelated user configuration were changed during cleanup.

Exactly one real synthetic notification was accepted, from the expected Sonrisa app to the authorized test channel. Real USGS ingestion and evaluation, deterministic threshold/ownership behavior, database uniqueness/replay/concurrency, actual delivery failure handling and no-resend entry behavior have separate evidence above. Deliberate limits remain: changing provider IDs can represent the same physical earthquake twice; outages longer than the one-hour feed can miss events; configuration can change across Pending replays; unresolved Processing intents require investigation; automatic recovery, backlog draining, email, dashboards and n8n global OTEL configuration remain deferred. Live globally-zero-enabled configuration was not forced, and no shared service outage/restart or human receipt was claimed.

Final fixture-state inspection returned 1 Sent, 1 Failed, 2 Processing, 3 Pending and 5 Unsupported intents across the three synthetic events, including pre-existing alert matches. Only the Sent intent produced an acknowledged message. Retained Pending/Processing test evidence must be reviewed before any future automatic delivery. The final 62-path secret-pattern scan found no listed token/key/connection patterns, all 167 checked local documentation links resolved, whitespace checks passed, and all three fresh remote snapshots reproduced the tracked exports byte-for-byte.

## Final independent review

The final whole-branch reviewer returned **APPROVE**, with no remaining Critical, Important or Minor findings, after reviewing the exact 62-path staged set, both corrected failure paths, schema/SQL/Code contracts, the runbook and evidence. It independently ran all 20 Node tests and staged whitespace checks and reproduced all three sanitized exports from the final remote snapshots. It relied on the controller's recorded live n8n/PostgreSQL results, final 80/80 guarded .NET suite and secret/link checks rather than claiming to repeat them. The initially reported native Slack error-shape defect and the later empty-UPDATE integration defect are corrected and preserved as actual review evidence above. The approved commit is `feat: implement first end-to-end n8n alert workflow`; Git history records its resulting hash. No later milestone, merge or push is authorized by this completion.

The first commit attempt failed with exit 128: the configured `op-ssh-sign` helper reported `1Password: failed to fill whole buffer`, followed by `fatal: failed to write commit object`. No commit was created by that attempt. The reviewed changes remain staged; signing configuration and Git identity were not changed. A successful later retry, if performed, is recorded in Git history.
