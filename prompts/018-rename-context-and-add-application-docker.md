Date: 2026-09-14

Purpose: Rename the application context and add application-only Docker testing infrastructure.

It should now be working; Rider recognizes it. A few small changes: name the context AppDbContext instead of ProductDbContext. Also prepare the Docker infrastructure for my environment; we will test it there: Dockerfile, Compose, and environment configuration.
