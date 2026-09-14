# Final whole-branch review — milestone 4 alert configuration

**Current recommendation: APPROVE.** The initial findings below were corrected and verified through the final test artifact and recorded database evidence; closure details are at the end of this report.

## Initial whole-branch findings

### Critical

None.

### Important

1. **The database accepts whitespace-only configuration despite the approved nonblank CHECK contract.** `ck_alerts_name` and `ck_users_destinations` use PostgreSQL `btrim(value)` without a character set, which trims spaces but leaves a tab. Consequently `name = E'\t'` satisfies the alert name check, and a users row with `email_destination = E'\t'`, `slack_destination = NULL` satisfies the destination check. The application validators reject these values, but direct database writes can create a blank displayed alert or an unusable sole destination. This contradicts the specification's database-level nonblank guarantee and matters at the documented application/workflow database boundary. Correct the constraints through a reviewed forward EF migration (both initial and shared-user migrations are already applied), update the model mapping/snapshot, and cover the boundary with a real PostgreSQL constraint test. References: `src/Sonrisa.Web/Data/AppDbContext.cs:20,46`; `src/Sonrisa.Web/Data/Migrations/20260914125733_InitialAlertConfiguration.cs`; `src/Sonrisa.Web/Data/Migrations/20260914140740_SharedUserNotificationDestinations.cs:30`.

### Minor

1. `docs/04-architecture.md:26` labels the current application-to-DEV database link a “secure DEV connection,” while ADR-007 and the validation evidence explicitly record no PostgreSQL TLS and no established tunnel. The surrounding prose acknowledges the accepted DEV exception, but the diagram should use neutral wording to avoid implying transport validation.

## Verification and residual risk

Reviewed the approved amended specification and plan, ADR-008/009/010, database contract, README, full review file inventory, application code, migrations, Razor pages, telemetry code, and relevant tests. `git diff --check` passed. The controller's final amended-model evidence reports 65/65 tests passed with 11 guarded PostgreSQL and 11 telemetry cases, plus successful CLI/Rider/CSS, browser/HTTP, migration, Docker, restart, and privacy checks; I did not rerun those checks or access shared DEV during this read-only review. The checks do not include the whitespace-only direct-SQL case above. Deferred authentication, n8n runtime workflows, delivery, production credentials/TLS, Npgsql spans, and gRPC wire validation match the approved scope and are not blockers for this milestone.

**Initial next action (completed):** Fix the Important constraint gap, verify the forward migration and PostgreSQL rejection behavior, then request focused re-review before exact staging and the milestone commit.

## Scoped re-review of the constraint correction

**Verdict: APPROVE for controlled migration application; final branch acceptance remains pending actual post-migration verification.** The revised `AppDbContext` check expressions use an explicit Unicode whitespace set matching .NET `string.Trim`, including tab, newline and nonbreaking space, so these values can no longer satisfy the required trimmed/nonblank checks. The new forward migration changes only `ck_users_destinations` and `ck_alerts_name`; the generated SQL drops and adds those constraints and inserts its history row within one transaction. It does not alter data, columns, keys, unrelated tables or either already-applied migration. Existing invalid data would make `ADD CONSTRAINT` fail and roll back, rather than be silently rewritten. The generated snapshot and migration designer contain the same expressions. The two new guarded PostgreSQL tests use a verified target, parameterized direct inserts, generated IDs and rollback for each whitespace case. `docs/04-architecture.md:26` now describes the current DEV connection without implying TLS.

No new Critical or Important finding in the scoped correction. Local build and non-PostgreSQL tests reportedly passed; the controller is running the expected pre-migration red tests. This review is source/SQL/test-safety approval only. After the reviewed migration is applied to the verified target, confirm the two regression tests pass against actual PostgreSQL, rerun the guarded suite and inspect final schema/history before closing the original Important finding.

## Final evidence closure

**Verdict: APPROVE. Critical: none. Important: none. Minor: none.** The original Important finding is closed by the reviewed third forward migration and two real PostgreSQL direct-write tests. The original Minor diagram wording is corrected. I inspected the final validation section and the final TRX: 67 executed, 67 passed, zero failed/skipped; both whitespace constraint test cases passed. The controller's recorded post-application catalog check found the explicit Unicode character set in both CHECK constraints and three migration-history rows on the verified `sonrisa_dev` target, with zero users/alerts after cleanup. The recorded final Docker smoke checks, Rider build and EF pending-model check passed. `git diff --check` remains clean. I did not independently requery shared DEV or rerun the suite. No additional branch finding emerged from the bounded correction. Exact staged/secret inspection and the milestone commit remain controller actions.
