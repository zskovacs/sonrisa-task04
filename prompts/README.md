# User task prompts

This directory contains only substantive user-authored task requests. It is not a conversation transcript or an archive of agent-to-agent messages.

| Record | User task |
| --- | --- |
| [001](001-initialize-repository.md) | Initialize the repository and establish the product/process baseline. |
| [002](002-design-mvp-architecture.md) | Define and review the MVP architecture and product design. |
| [010](010-create-application-skeleton.md) | Create the application skeleton and integrate with existing DEV infrastructure. |
| [018](018-rename-context-and-add-application-docker.md) | Rename the context and add application-only Docker testing. |
| [019](019-alert-configuration-model-and-management-ui.md) | Design and implement alert configuration, ownership-aware management, Tailwind styling, and OpenTelemetry. |
| [020](020-store-shared-user-destinations-and-use-fluentvalidation.md) | Correct destination ownership and use an existing validation framework. |
| [021](021-first-end-to-end-n8n-alert-workflow.md) | Reconstruct context, approve a design, and implement the first end-to-end n8n alert workflow. |
| [022](022-simplify-first-runtime-workflow-design.md) | Simplify and approve the first runtime design, keeping matching in n8n and delivery explicitly selected. |
| [023](023-create-sonrisa-slack-app.md) | Create the Sonrisa DEV Slack app, configure its scopes, and configure the n8n credential where possible. |
| [024](024-add-smtp-email-delivery.md) | Add SMTP email delivery alongside the existing notification workflow using SMTP4DEV. |

Record meaningful user requests that start a milestone or assign implementation/product-validation work. Exclude agent-generated plans, delegated assignments, internal review prompts and loops, brainstorming answers, clarifications, approvals, status replies, and routine housekeeping. Honor explicit requests not to record a message.

Accepted decisions belong in [ADRs](../docs/adr/) and the [decision log](../docs/decision-log.md). Actual review findings and validation results belong in the [AI review log](../docs/ai-review-log.md) and [evidence](../evidence/); internal prompts are not copied there as a replacement archive.

Follow [AGENTS.md](../AGENTS.md#2-prompt-history). Each record contains only the date, purpose, and verbatim final English user prompt. Retained records keep their original numbers and text; numbering gaps reflect the authorized removal of internal prompts and discussion replies. Use the next number after the highest retained record for the next qualifying request. Earlier versions remain in Git history; do not rewrite commits to perform this cleanup.

Archived prompts describe historical requests. Current work follows the active user request and repository rules.
