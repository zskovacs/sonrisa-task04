# SMTP delivery extension

> Historical implementation plan/design: runtime boundaries are superseded by the approved [runtime simplification](../specs/2026-09-14-runtime-simplification-design.md). Preserve this record with its original implementation and validation context.


Approved in conversation on 2026-09-14 after the user requested SMTP4DEV email and corrected the initial separate-workflow proposal: extend the existing delivery workflow with another channel. This is a bounded extension after milestone 5 (`b9cf171`), not completion of milestone 6's retry/circuit scope. [User request](../../../prompts/024-add-smtp-email-delivery.md).

## Behavior

Keep three inactive workflows. Rename the existing delivery workflow to `Sonrisa - Deliver Notification - DEV`, retaining its remote ID. One explicit UUID enters a common atomic Pending claim and immutable event/destination read, then branches by textual channel to Slack or native SMTP Send Email. The PostgreSQL claim accepts only supported Slack/email Pending records. A repeated/concurrent entry cannot claim the same record twice. Never replay a saved transport node directly.

New matching email configuration creates Pending intent, while existing Unsupported email rows remain untouched historical evidence. A forward EF migration changes only the delivery-state constraint to permit email Pending/Processing/Sent/Failed alongside legacy Unsupported. Preserve unique event/alert/channel identity, destination snapshots, source normalization, cross-owner matching and all management-plane boundaries. No new tables, dependencies, application SMTP service, automatic sends, retries, resets, circuits, backlog drain or historical rematching.

SMTP uses the existing `SMTP account` credential, separate from non-secret sender configuration and the persisted destination snapshot. Use one bare recipient, no CC/BCC, attachments, HTML or user-supplied header fields. Reject ambiguous address/list/header syntax before transport with a guarded Failed/`invalid_email_message` update and no SMTP call. Existing profile validation remains; the transport can fail closed on unsupported mailbox syntax. Plain-text subject/body contain bounded canonical event data and domain IDs, with an explicit synthetic marker.

SMTP success means the server accepted the sole intended recipient, not inbox placement or human receipt. Confirm the native result's accepted/rejected/envelope shape before recording Sent. Errors without demonstrated definitive classification remain Processing with `delivery_outcome_unknown`; no automatic retry. Slack retains its reviewed acknowledgement/rejection behavior. No exactly-once external delivery claim.

## DEV verification and source control

The existing SMTP credential is visible by metadata. The user supplied `http://192.168.0.2:5000/`; its SMTP4DEV server API responded successfully and reported port 25 with no relay configured. Use the proposed reserved test addresses `sonrisa@example.test` and `sonrisa.runtime@example.test` for one isolated synthetic notification. Verify the credential's actual non-secret host/port, not just its name, and recheck no relay before the test; never alter server/credential settings. The workflow's sender is explicit non-secret operator configuration; default delivery ID remains empty.

Validate exact runtime code/SQL, state constraints, duplicate/concurrent claims, Slack/email routing, legacy Unsupported exclusion, immutable destination, malformed recipient rejection, SMTP acceptance/unknown results, and one captured SMTP4DEV message followed by a replay producing no second message. Require an empty Pending queue and choose a synthetic magnitude below every other enabled supported threshold, checking no other alert matches before live evaluation. Use a new synthetic event and owned test alert through the same normalizer/evaluator; do not drain or reset prior milestone records. Test both branches with pinned transport where appropriate and disclose which nodes are mocked. Preserve the earlier real Slack send evidence without sending another Slack test unnecessarily.

Export the actual tested delivery and evaluation workflows using the existing reproducible exporter, preserving their remote identity and sanitizing credentials/pins/environment metadata. Update current runtime documentation, decision/review evidence and roadmap sequencing. Record that the extra workflow proposal was rejected because common selected-ID claim and channel branching are sufficient. Use a focused follow-up commit, `feat: add SMTP notification delivery`; do not amend milestone 5 or claim milestone 6 is complete.
