# Hospital Management System

A modular-monolith hospital management application. It supports patient, doctor, and administrator workflows while keeping the database as the source of truth.

## Architecture

- Backend: ASP.NET Core 8, layered into API, Application, Domain, and Infrastructure projects.
- Frontend: React with Vite.
- Database: SQL Server, managed through versioned SQL scripts and scaffolded into EF Core in Phase 2. EF Core migrations are intentionally not used.

## Repository layout

`src/` contains backend projects; `client/` contains the React frontend; `database/` contains versioned SQL schema, seed, and helper scripts; `tests/` contains unit, integration, and API test projects; and `docs/` contains development documentation.

## Prerequisites

- .NET SDK 8.0.423 or compatible 8.0 SDK
- Node.js 22 and npm
- SQL Server (required from Phase 1)

## Run locally

```powershell
dotnet restore HospitalManagementSystem.sln
dotnet build HospitalManagementSystem.sln --no-restore
dotnet test HospitalManagementSystem.sln --no-build

Set-Location client/hospital-web
npm install
npm run dev
```

Run the API separately with `dotnet run --project src/Hospital.Api`. The health check is available at `/health`.

## Configuration and secrets

Copy `src/Hospital.Api/appsettings.Local.example.json` to `src/Hospital.Api/appsettings.Local.json` and provide local values, including a unique JWT signing key of at least 32 characters. The local file is ignored by Git. Production secrets must be supplied through environment-specific secure configuration, never committed. Outside Development, configure the exact frontend URLs in `Cors:AllowedOrigins`; the API will not start with an implicit CORS allowlist.

## Authentication

The API exposes `POST /api/auth/register`, `POST /api/auth/login`, and authenticated `GET /api/auth/me`. Registration creates an active `Patient` account and returns a short-lived JWT. Send it as `Authorization: Bearer <token>`; logout is performed client-side by deleting that token. The API returns `401` for absent, invalid, expired, or bad-credential requests and `403` for an authenticated user without the required role.

The fictional development seed accounts use `DevelopmentOnly!123`; never use this password or the seed identities outside a local development database.

## Profiles and administration

Authenticated users can read their account profile through `GET /api/profile/me`. Patients create or update their own profile with `PUT /api/profile/me`; this generates a stable medical-record number for a new patient profile. Administrators can search patients, doctors, and staff through `/api/admin/patients`, `/api/admin/doctors`, and `/api/admin/staff`, and activate or deactivate another account with `PATCH /api/admin/accounts/{userId}/status`.

## Department and doctor catalog

Use `GET /api/departments`, `GET /api/departments/{id}`, `GET /api/doctors`, `GET /api/doctors/{id}`, or `GET /api/departments/{id}/doctors` to select an active department and doctor. Only administrators may create or update departments. Inactive departments and doctors are never exposed for selection.

## Database setup

The database is initialized from repository SQL scripts, not EF Core migrations. See [database/README.md](database/README.md) for the SQL Server command and safety notes.

## Branching strategy

See [docs/DEVELOPMENT.md](docs/DEVELOPMENT.md). Work is created from `main` in short-lived `feature/`, `fix/`, or `docs/` branches and merged through reviewed pull requests.

## Current milestone

The roadmap phases through production validation are complete locally. See the handoff documentation:

- [Architecture](docs/ARCHITECTURE.md)
- [Database](docs/DATABASE.md)
- [API](docs/API.md)
- [Setup](docs/SETUP.md)
- [Deployment](docs/DEPLOYMENT.md)
- [Testing](docs/TESTING.md)
- [AI summary safeguards](docs/AI.md)
- [Troubleshooting](docs/TROUBLESHOOTING.md)

The first hosted CI run remains pending because this workspace has no Git remote or initial commit. The included workflow is ready to run when the repository is pushed.
