# End-to-end MVP validation

Date: 2026-09-14

## Scope, revision and method

This is milestone 8 under [prompt 028](../../prompts/028-end-to-end-validation.md), the [specification](../../docs/superpowers/specs/2026-09-14-end-to-end-validation-design.md) and [reviewed plan](../../docs/superpowers/plans/2026-09-14-end-to-end-validation.md). The initial clean checkout was `main` at `eaa588b feat: add operational admin view`, with Email `a198778`, simplified runtime `c38f2a3`, historical SMTP `e1dc337`, first runtime `b9cf171`, management `98b000c` and skeleton `5880801` in its ancestry. The user explicitly allowed the new branch from main after that ancestry check. `test/end-to-end-validation` starts directly at `eaa588b`; no checkout of an older state or merge occurred. This evidence describes that baseline plus the focused management outage correction and regression tests in this milestone's commit.

Reconstruction covered root working rules/README, current and historical plans/specifications, all ADRs, scope/assumptions/architecture/contracts, decisions/reviews, substantive prompt history/index, relevant prior evidence, application/pages/services/ownership/telemetry, entities/migrations, workflow source/export and actual DEV database/workflow/executions. Historical queues/delivery persistence were treated as superseded, not missing functionality. The [application record](2026-09-14-e2e-application.md) contains live-host, browser, telemetry and clean-snapshot detail. [Execution summaries](2026-09-14-e2e-executions.json), [synthetic fixtures](2026-09-14-e2e-fixtures.json) and the [sanitized captured message](2026-09-14-e2e-smtp-capture.json) retain observed results without raw execution payloads or credentials.

Validation types are distinguished below: automated unit/HTTP/relational tests, real application/browser requests, read-only database inspection, actual n8n engine executions with transport mocks, pinned-boundary tests, a real USGS request, reused real Slack acceptance, and one new native SMTP submission. A successful mocked transport is never evidence of external delivery.

## Build and regression commands

Commands ran from the repository root unless specified. Secret configuration was injected privately by the existing external `/tmp/sonrisa-correction-tools/run.py` helper, which verifies `current_database() = sonrisa_dev` before launching tests. Its SQL path binds parameters. The temporary helper is an execution convenience, not a prerequisite for reproducing the documented environment-variable test path.

| Actual command/check | Result |
| --- | --- |
| Rider `get_solution_projects`, then `build_solution_start(rebuild: true)` and build-result inspection | Both solution projects recognized; rebuild session `810dab66-19fc-4d61-8e06-40d8efda70e1` succeeded with no problems. |
| `dotnet tool restore` | Local EF tool 10.0.12 restored. SDK 10.0.401 was installed; Node 24.21.0/npm 11.19.0 were used. |
| `npm ci` | 33 packages added, 34 audited, zero vulnerabilities. Existing `@parcel/watcher@2.5.1` install-script allowlist warning; no install or CSS failure. |
| `npm run css:build` | Tailwind 4.3.3 completed successfully. |
| `rtk dotnet build Sonrisa.sln`, final `rtk dotnet build Sonrisa.sln --no-restore` | Successful, zero warnings/errors. |
| `dotnet ef migrations has-pending-model-changes --project src/Sonrisa.Web --no-build` | No pending model changes. No migration applied or generated. |
| `rtk proxy dotnet test Sonrisa.sln --no-restore --logger 'console;verbosity=minimal' --logger 'trx;LogFileName=default.trx' --results-directory /tmp/sonrisa-e2e-default-tests` | 64 passed, 19 skipped, zero failed, total 83; 36 seconds. Relational test configuration intentionally absent. |
| `python3 /tmp/sonrisa-correction-tools/run.py dotnet test Sonrisa.sln --no-restore --logger 'console;verbosity=minimal' --logger 'trx;LogFileName=guarded.trx' --results-directory /tmp/sonrisa-e2e-guarded-tests` | 83 passed, zero skipped/failed, 36 seconds, against verified DEV. Console and TRX counters inspected. |
| `node --test n8n/tests/*.test.mjs` | 24 passed, zero skipped/failed; exact workflow source, transport text/results, builder, query and sanitizer guards. |
| `node n8n/export-workflow.mjs process /tmp/sonrisa-e2e-baseline-workflow.json` and the same command on `/tmp/sonrisa-e2e-final-workflow.json`, each redirected to a private sanitized file; `cmp` against `n8n/workflows/process.json` | Both comparisons byte-identical. No workflow artifact change required. |
| Disposable `git archive HEAD` snapshot at `eaa588b`: tool restore, `npm ci`, CSS build, solution build | Successful without `.env`; CSS 11,195 bytes, build zero warnings/errors. Snapshot removed; no duplicate DEV service provisioned. |

The first default test invocation through RTK's special `dotnet test` formatter hid skipped-test details and did not retain the requested TRX. Its abbreviated result was not used as proof of a complete suite; the explicit proxy invocation above replaced it. The 19 default skips comprise guarded PostgreSQL/page/admin/legacy-conversion tests, all executed successfully in the guarded run. Historical conversion tests use temporary isolated test structures; they do not apply old migrations to the current public schema.

## Management, ownership and admin

Guarded HTTP tests exercise actual Razor GET/POST forms with antiforgery tokens and real DEV persistence: create after saving a shared profile, default disabled state, consecutive settings saves, enable/disable, valid edit, stale revision rejection, blank name and malformed number rejection. The new successful-edit regression verifies the stable owner and exact `earthquake/magnitude/gte/number` contract, changed finite threshold and revision, plus unchanged persistence after invalid/stale posts. Invalid/missing destinations and unsupported numeric/configuration input are covered by existing validators, HTTP and relational constraint tests; no new rule model was added.

Normal pages resolve ownership from `ICurrentOwner`; EF predicates constrain list/read/edit/status to that owner. Guarded tests prove foreign edit/status routes remain inaccessible even with valid antiforgery, foreign rows do not appear, and forged `OwnerId`/`Input.OwnerId` posts cannot transfer ownership. The runtime SQL has no `MvpOwner:Id` filter and loads enabled configuration joined to profiles across owners; both the exact-query relational tests and runtime executions 64/66/68 prove that different owners contribute notifications.

Fresh browser checks after runtime cleanup compared all three admin pages with read-only SQL: summary **4 owners / 5 alerts / 1 enabled / 4 disabled / 3 Slack-configured owners / 3 Email-configured owners**; all four owner rows' alert/enabled totals and channel indicators; all five alert rows' owner, event type, state, condition and channels. Normal management displayed only its configured owner's one alert. Every persisted destination string was absent from admin HTML. No admin forms, browser errors or external browser requests appeared. Admin empty and timeout scenarios use controlled readers where required, explicitly not an emptied DEV database. See the application record for detailed browser limits.

Two genuine failure defects were found: refused database connections were wrapped by EF's `NpgsqlExecutionStrategy` in `InvalidOperationException`, and parseable blank-Host configuration failed later with `ArgumentNullException`. Alerts/settings returned 500 although admin already returned 503. The two management services now mirror the existing admin contract: reject blank parsed Host and recognize only an immediate database/timeout inner exception. Four HTTP regression cases failed before and passed after the correction; unrelated exceptions remain visible. No product, architecture or schema change was necessary.

## Actual PostgreSQL state and cleanup

Read-only PostgreSQL MCP inspection confirmed public `users`, `alerts`, and `__EFMigrationsHistory` only. Product configuration has owner IDs/revisions/shared non-secret destination columns, alert ownership/name/enabled/revision and one typed finite numeric condition. The owner FK uses `ON DELETE RESTRICT`; two primary-key indexes plus `ix_alerts_owner_id` and `ix_alerts_enabled_event_type` exist. EF model/migration constraints and actual columns/relationships were compared. There is no `source_events`, `notification_deliveries`, queue, retry, dead-letter, runtime-history or credential column/table.

Six migration records remain, all with EF ProductVersion 10.0.12: `20260914125733_InitialAlertConfiguration`, `20260914140740_SharedUserNotificationDestinations`, `20260914143004_EnforceUnicodeTrimmedConfiguration`, `20260914155714_FirstRuntimeSlice`, `20260914173558_EnableEmailDeliveryStates`, `20260914184146_RemoveObsoleteRuntimeState`. Historical runtime migration records are coherent with their later forward removal. No existing migration file changed.

Runtime fixtures inserted three generated profiles and four generated alerts using bound JSON parameters after target verification. The three enabled rules had threshold -100, below the preflight minimum enabled original threshold 4; the fourth was disabled. B/C were subsequently disabled for the single-recipient SMTP check. Cleanup removed only those exact four alert IDs and three owner IDs. The full guarded suite separately cleaned up its own generated records.

Before fixtures, after runtime cleanup, and again after the complete guarded suite, the same ordered full-row digest expression `md5(string_agg(row_to_json(alias)::text, ',' ORDER BY id))` returned users **`e800c423c0b169eebadc0bafecb1f6d1`** and alerts **`cec2034f3bbfa6cac6f0fed02d3dd2ba`**. Counts, enabled/channel totals and all migration records also matched the baseline. This comparison includes revisions and destination values without printing them. An intermediate checksum calculation used a different JSON aggregation and was discarded; only identical expressions were compared. Original product rows were unchanged.

## Actual n8n runtime scenarios

One unarchived authoritative workflow was found: **`aVijfnQr0kdLAJHP` / `Sonrisa - Process Alerts - DEV`**, inactive with 26 nodes. Direct inspection of historical IDs `K3A9cowlgglFqriG`, `zZYWvZSk1LDGhgJa`, `5fGfgXCBLxhNO479` returned archived/inaccessible; they do not compete with the current flow. No recurring schedule was activated.

Each scenario first compared the retrieved baseline, then atomically disconnected and disabled native transports before installing temporary Code mocks. The normalizer, dedup identities, actual parameterized SQL and matcher stayed the same except for the explicitly labeled cap/query/pinned cases. Each scenario used controller-owned try/finally restoration, an on-disk baseline/recovery description, and a fresh comparison of node IDs/types/meaningful parameters/credentials/edges/retry/settings/inactivity. The mock transport nodes copied the native five-attempt, 5,000 ms, error-output settings; node-scoped counters observed calls and were removed with instrumentation. These are actual n8n retry-engine checks, not provider outages or a separately implemented retry loop.

| Execution | Validation type and observed result |
| --- | --- |
| **64** | Deterministic full entry with actual SQL, mocked sends. Six features: below -100.1, equal -100, above -99.9, duplicate equal ID, string magnitude, null magnitude. Four normalized events, two invalid-magnitude diagnostics; within-batch dedup retained three. Only equal/above matched. Across three enabled owners, each matching event yielded Email-only, both destinations, and Slack-only: four Email plus four Slack items. Disabled alert and below-threshold event produced no notifications. Each item's destination matched its persisted owner's profile. |
| **65** | Same full-entry fixture again: previous-execution dedup outputs `[0,3]`; no configuration query or transport. |
| **66** | New ID passes dedup. Mock Email A failed exactly five native attempts, taking 20,082 ms at that node; visible `email_retries_exhausted_discarded`. Later Slack B, Email B and Slack C still processed. A later Email invocation exposed observed counts A=5/B=1. |
| **67** | Full-entry replay after failed Email: dedup `[0,1]`, no SQL/send. Loss after an event is seen is accepted. |
| **68** | New ID; mock Slack B failed five native attempts, 20,092 ms, visible `slack_retries_exhausted_discarded`; Email B and later Slack C continued. Observed counts B=5/C=1; both Email items accepted by mocks. |
| **69** | Existing native history retained, cap temporarily reduced to 1. Actual dedup node failed before SQL with the maximum-history-size error, even for a seen key. Cap restored to 10,000; history never cleared. This confirms capacity failure before filtering, not rolling eviction. |
| **70** | Unique fixture reached dedup, then a temporary read-only `SELECT 1 / 0 AS controlled_database_read_failure` failed in the actual PostgreSQL node. No matcher/send. This is controlled query failure, not a shared database outage. Query/binding restored afterward. |
| **71** | Same full entry after query failure: dedup `[0,1]`, no SQL/send. No durable recovery is claimed. |
| **72** | Real USGS HTTP request and actual downstream pipeline, mocked transports. Five source features normalized, no malformed diagnostics. IDs, numeric magnitudes and millisecond-to-UTC conversions compared to source. SQL and matching proceeded normally. |
| **73** | Pinned configuration boundary for schema-unrepresentable invalid rules: unsupported `gt`, string threshold, disabled rule, invalid Email list with valid Slack, and valid both-channel rule. Actual matcher emitted `unsupported_condition`, `invalid_threshold`, `invalid_email_destination`; disabled emitted no match, independent valid destinations produced two Slack/one Email mock items. HTTP/configuration/past-dedup pins are not real persistence/dedup evidence. |
| **74** | Pinned evaluator output containing unknown future channel then valid Slack/Email. Actual switch emitted `unsupported_channel` and continued both valid mocks; unknown channel had no send/acceptance. This is a routing fixture, not a supported persisted third channel. |
| **75** | Actual SQL/no-send preflight after disabling generated B/C alerts: exactly one Email item to persisted `e2e-a@example.test`, no Slack. |
| **76** | Fresh synthetic ID; native SMTP enabled only for this scenario. Exactly one Email accepted, no rejected recipients, matching envelope, `250 Mail accepted`; `email_accepted`. SMTP4DEV captured one new message. |
| **77** | Full-entry replay of 76 with transports suppressed: native dedup `[0,1]`, no query/send; SMTP4DEV count stayed three (two before 76, three after). |

Execution summaries retain output counts by node/port, diagnostic counts and observed attempt counters. Loop totals count notification passes as well as its final done output; they must not be read as independent extra deliveries. The roughly 20-second failing-node timings are consistent with four waits of five seconds; individual wait intervals were not separately instrumented.

Real source execution 72 used the configured public all-hour endpoint. Observed IDs/magnitudes/UTC times included `hv75035817` / 2.09 / `2026-09-14T21:05:19.320Z`, `nc75435767` / 0.57 / `2026-09-14T20:46:18.560Z`, `ci41332807` / 1.35 / `2026-09-14T20:46:12.930Z`, `tx2026sdmzpn` / 1.4 / `2026-09-14T20:39:43.915Z`, `us7000thgs` / 5.3 / `2026-09-14T20:38:59.760Z`. Source titles also matched all five normalized titles. Exact-source tests additionally cover title fallback/control handling, optional URLs, timestamp bounds and malformed numeric rejection. No physical-earthquake alias reconciliation was implemented.

The installed release was recorded as n8n 2.38.7 by the previous runtime evidence. This session verified Remove Duplicates v2 configuration and actual capacity/retry behavior; unauthenticated settings/version retrieval did not independently confirm a fresh server release number. The existing documented limit remains consistent with execution 69. Native technical history is bounded, node-scoped and not a permanent or globally atomic event ledger; recreated/lost history and changed USGS IDs can permit repeats. No shared n8n restart, concurrent ingestion or saturation at 10,000 entries was tested.

## Transport, fan-out and observability conclusions

**Slack:** No new real Slack message was necessary or sent. Actual historical execution **52** was retrieved and showed Slack `ok=true` for the authorized synthetic test channel. Retrieved execution **53** showed dedup `[0,1]` and no Slack send. Pre-52 workflow version `c24ef2f5-305c-4b18-ab21-eae5c42c41b6` has the same credential reference, Slack node/version, destination expression, text source, retry/error settings and message options as the baseline. Differences are editor-omitted defaults and key ordering, covered by the existing sanitizer. Git diff from `c38f2a3` confirms unchanged normalization/selector/Slack preparation sources. Independent review accepted reuse. This proves provider acceptance at execution 52, not fresh credential health or human receipt today. New branch/fan-out/failure checks use the mocks explicitly identified above.

**Email:** Before 76, SMTP4DEV was running on the established DEV SMTP endpoint with no relay server or automatic relay configured; SMTP greeting/QUIT returned 220/221. Native n8n SMTP credentials were unchanged and never read as secret values. The captured message's sole recipient, sender, subject and decoded body exactly matched the prepared/persisted contract. It is `text/plain`, visibly `[SYNTHETIC]`, with no attachments, HTML, internal alert/owner/execution IDs or unnecessary debug data. SMTP4DEV proves SMTP submission/capture only; it does not prove delivery to an external internet mailbox.

**Fan-out:** Native dedup runs before one shared matcher. Execution 64 produced both transport items from one matched owner/event after that shared boundary; each event was normalized/deduplicated once, and only the final routing/preparation/send branches were transport-specific. Disabled state was respected by actual SQL and pinned defensive matching. Failures on either transport did not abort unrelated items. No PostgreSQL runtime write, delivery ledger or retry state exists.

**Application telemetry/health:** Actual no-exporter and unavailable-OTLP hosts started/restarted and kept liveness/readiness healthy with DEV connected. Missing/malformed/blank-Host/refused DB configurations kept liveness 200, readiness and management/admin unavailable responses 503. The 11 existing observability tests passed, including actual request trace/log correlation, exporter routing and privacy checks. Logs are produced; request traces are emitted when configured. Npgsql/EF spans remain intentionally absent under ADR-009, and no cross-process trace propagation through PostgreSQL is claimed. No collector/backend/global n8n telemetry change was made; n8n execution history provided runtime observation.

## Security, reproducibility and limitations

Focused inspection found parameterized EF owner queries and n8n `$1::jsonb` binding, encoded Razor output and no `Html.Raw` rendering of configuration, safe bounded Slack/Email text preparation and no admin destination exposure. The workflow exporter strips credential references and rejects pins/temporary inputs/unsafe graph or retry changes. Both actual remote export comparisons passed. Appsettings contain logging/host configuration only; `.env` is ignored and untracked. Known database secret/full-connection values were compared privately against tracked and new files; a pattern scan also checked Slack tokens, private keys, access keys, long secret assignments and connection values. No hit was found across 263 tracked/new files; both private TRX outputs also contained no known database secret value. A repository Markdown path-link check passed for 262 links (verbatim prompts and bundled skill examples excluded; anchors not checked). The first broader scan correctly identified two literal `url` placeholders in bundled skill examples, not broken project links. Prompt 028 was compared with the original user task, and historical prompts were unchanged. Raw execution payloads, SMTP settings and private logs were not copied into evidence. This is a focused hygiene review, not a claim that unknown secrets or every historical Git object were exhaustively audited.

README's root solution/SDK, Tailwind toolchain, external secret configuration, local-application/shared-DEV distinction and n8n import/rebinding instructions were checked against the implementation and clean snapshot. No new workflow was imported: doing so creates separate native history; sanitizer/artifact tests and byte parity validate the export representation. No Docker rebuild/redeployment, fresh machine provisioning, external internet Email delivery, real provider outage, authentication/authorization, unattended scheduling, retention endurance or shared-service restart was claimed. Existing Docker and historical responsive-browser evidence remain historical evidence.

Accepted limitations remain unchanged: unauthenticated single-user management and unprotected read-only admin; accepted DEV credential/TLS exception; inactive/manual runtime; bounded history and provider-ID changes; best-effort notification loss after exhausted retry or downstream query failure; theoretical ambiguous-provider duplicate/loss; no durable recovery or exactly-once guarantee. These require no new product feature. Before any future unattended activation, review native-history capacity and production access separately; that is outside this milestone.

## Explicit final self-review

| Question | Answer and evidence |
| --- | --- |
| 1. Management UI correct? | Yes for accepted create/edit/settings/status/validation contracts: guarded HTTP suite and real browser checks. Outage defects corrected. |
| 2. Normal owner scoping intact? | Yes: owner-filtered reads/writes, foreign-route tests, forged-owner form tests and one-owner browser list. |
| 3. Admin intentionally cross-owner? | Yes: all four owners/five alerts and exact counts/rows, with destinations absent. |
| 4. PostgreSQL only justified state? | Yes: configuration plus six EF migration records; original full-row digests unchanged after all tests. |
| 5. One authoritative n8n workflow? | Yes: current inactive ID inspected, three historical IDs archived. |
| 6. Real USGS works? | Yes: actual request in 72; sends suppressed. |
| 7. Normalization works? | Yes: real field comparisons, malformed fixtures and exact-source tests. |
| 8. Dedup as documented? | Yes: 64/65/67/69/71/77 show within/across filtering and cap failure; no permanent/atomic/restart claim. |
| 9. Threshold matching correct? | Yes: below/equal/above in 64 and exact-source tests. |
| 10. Disabled/malformed fails safely? | Yes: real disabled configuration and pinned defensive malformed rules in 73; diagnostics, no false match. |
| 11. Slack works? | Historical real API acceptance 52 reused after contract comparison; current routing/failure mocked. No fresh provider-health claim. |
| 12. Email works? | Yes for native SMTP submission/capture in 76; no internet inbox claim. |
| 13. One event fans out to both? | Yes: 64, shared matcher before channel branches. |
| 14. Failure leaves unrelated items processing? | Yes: native-engine transport mocks in 66/68 and unsupported-channel continuation in 74. |
| 15. Retries bounded/n8n-owned? | Yes: five observed attempts, roughly 20 seconds waiting, error-output discard; no database retry state. |
| 16. Delivery limits honest? | Yes: failures/replays and ambiguity/loss/no-recovery boundaries explicitly documented. |
| 17. Exports secret-free? | Reviewed sanitizer checks/scan; baseline and final export byte parity, no credentials/pins/test input. |
| 18. Telemetry/health valid? | Yes: live-host matrix and 11 observability tests; Npgsql tracing intentionally deferred. |
| 19. Any secret leaks? | None found in inspected files, outputs and captured evidence; focused scope stated above. |
| 20. Defects fixed without expansion? | Yes: two narrow service failure guards, four outage regression cases and one successful-edit regression; no schema/runtime feature change. |
| 21. Any untested claims? | Test types and reused/skipped/untested boundaries are explicit. Default skips all ran in the guarded suite. |

Per-task review approved the successful-edit test, outage correction and completed runtime validation evidence. The initial plan review required explicit interruption-safe workflow restoration; the plan and actual scenario procedure were corrected before runtime mutation. Final whole-branch review returned **APPROVE**, with no Critical, Important or Minor findings. The reviewer independently inspected both TRX files, local links, baseline/final workflow graph/settings/parameters/credentials/inactivity and byte-identical sanitized export. The final documentation/retrospective milestone was not started.

The exact staged change contains only this milestone’s documentation/prompt/evidence and the two service guards plus their tests. Staged whitespace and secret checks were completed before the authorized `test: validate end-to-end behavior and failure scenarios` commit. The commit containing this record is the milestone revision; the branch remains `test/end-to-end-validation`, without merging.
