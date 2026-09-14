Date: 2026-09-14

Purpose: Confirm the UI and runtime architecture, database separation, application persistence tooling, and secret handling.

The application and n8n would have separate databases in PostgreSQL, correct? On the application side, use EF Core and migrations. Make sure connection strings are never stored in the repository. We can use User Secrets, for example, or an ignored .env file for Docker.

The proposed UI and runtime direction is acceptable.
