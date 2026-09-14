Date: 2026-09-14

Purpose: Revise the application and n8n responsibility boundary to use direct database access and workflow-owned circuit breaking.

The circuit breaker should be implemented in the n8n workflow. The application has no responsibility for it. Is that how you understood it too?

The application only needs to save each user's conditions, along with user management and related functionality, although those are not the main scope. n8n can read these directly from the database, so I would not introduce HTTP communication between n8n and the application. Retries are unnecessary here; n8n has access to the application database.

Is that how you understood it too?
