# Development Workflow and Conventions

## Branches

`main` is the protected, releasable branch. Create one short-lived branch per coherent change:

- `feature/<area>-<description>` for functionality
- `fix/<area>-<description>` for corrections
- `docs/<description>` for documentation-only changes

Branches are merged to `main` through a pull request after build and tests pass. Do not commit directly to `main`.

## Code conventions

- Use file-scoped namespaces and nullable reference types in C#.
- Keep controllers thin: HTTP concerns only. Put business rules in Application services and data access in Infrastructure.
- Use async APIs for I/O-bound work.
- Keep request/response DTOs separate from database-generated entities.
- Never hard-code credentials, tokens, connection strings, or patient data.
- Version SQL changes as ordered scripts under `database/migrations`; do not use EF Core migrations.
- Add or update tests with every behavior change.

## Commit guidance

Use concise imperative commit messages, for example `Add appointment slot constraint`. Keep formatting-only changes separate from behavior changes when practical.
