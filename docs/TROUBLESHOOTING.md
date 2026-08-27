# Troubleshooting

| Symptom | Check / fix |
|---|---|
| SQL connection reports encryption/TLS failure | Install or update Microsoft ODBC Driver 18, then use `Encrypt=True;TrustServerCertificate=True` for local development. |
| API cannot start | Verify `appsettings.Local.json` has a reachable connection string and JWT issuer, audience, and 32+ character signing key. |
| Browser says `Failed to fetch` | Start the API, confirm `/health`, and use one of the configured local origins: `localhost:5173` or `127.0.0.1:5173`. |
| CORS fails in production | Configure explicit `Cors__AllowedOrigins__*` values; non-development startup has no implicit fallback. |
| Login fails for a seed account | Reinitialize a clean local database and use only the fictional development credentials documented in README. |
| AI summary unavailable | This is safe fallback behavior. Check only the external configuration/key/network if AI is intentionally enabled. Normal history remains available. |
| Build files are locked | Stop a running `Hospital.Api` development process before building or testing. |
| CI SQL bootstrap fails | Confirm the hosted runner can pull SQL Server 2022, then inspect the ephemeral container logs from the workflow. |

Never paste a JWT, SQL password, API key, or real patient record into logs, issues, or chat.
