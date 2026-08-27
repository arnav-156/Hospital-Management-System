# Deployment

The project has no deployment target configured. A student-friendly production layout is a static web host for `client/hospital-web/dist`, an ASP.NET Core application host for the API, and managed SQL Server.

## Production configuration

- Supply `ConnectionStrings__HospitalManagementDb`, `Jwt__Issuer`, `Jwt__Audience`, and `Jwt__SigningKey` through the host secret store/environment configuration.
- Supply `OpenAi__ApiKey` or `OPENAI_API_KEY` only when the optional AI feature is enabled.
- Set `Cors__AllowedOrigins__0`, `Cors__AllowedOrigins__1`, and so on to the exact deployed web URLs. The API refuses non-development startup without at least one valid configured origin.
- Terminate TLS at the host/proxy and run the API outside the Development environment so HSTS and HTTPS redirection are enabled.
- Initialize schema only on a reviewed clean target; back up production before schema changes.

## CI

`.github/workflows/ci.yml` creates an ephemeral SQL Server container, initializes it, formats/builds/tests the solution, builds the web client, and uploads API and web artifacts. The SQL password is generated at runtime and is not stored in the repository.

## Known limitations

- No cloud host, domain, TLS certificate, or managed SQL Server has been provisioned for this local project.
- The first hosted CI run is pending because the workspace has no initial commit or Git remote; the workflow is included and locally validated where possible.
- AI summaries depend on optional external provider configuration and may be unavailable; the application deliberately falls back to normal history.
- The UI is a focused MVP and uses identifier-based appointment/treatment/billing forms rather than a calendar or payment gateway.
