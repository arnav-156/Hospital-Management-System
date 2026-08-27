# Testing

## Test pyramid

- Unit: request validation and appointment lifecycle rules.
- Integration: EF Core CRUD against SQL Server.
- API: authentication, authorization, profiles, catalog, appointments, treatment, billing, notifications, feedback, pagination, AI fallback, CORS, and response headers.
- Browser: patient registration/profile/booking, doctor acceptance/treatment/billing, and patient history/bill viewing.

## Latest local validation

| Check | Result |
|---|---|
| Unit tests | 20 passed |
| Integration tests | 1 passed |
| API tests | 15 passed |
| Web production build | Passed |
| SQL validation | Passed on default instance |
| Clean SQL initialization | Passed on separate SQLEXPRESS instance; temporary database removed |
| Browser workflow | Passed with synthetic data; temporary records removed |

Run all backend tests with `dotnet test HospitalManagementSystem.sln -m:1 -p:BuildInParallel=false -p:NuGetAudit=false`. Run `npm run build` from `client/hospital-web` for the frontend build.
