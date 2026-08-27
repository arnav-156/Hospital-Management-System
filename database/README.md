# Database Scripts

The database is **database first**. Versioned schema changes belong in `migrations/` and must be applied in numeric order. Seed data belongs in `seed/`; operational helper scripts belong in `scripts/`. Do not add EF Core migrations to this repository.

## Initialize a clean development database

Run the following from the repository root on a machine with SQL Server and the current ODBC `sqlcmd` client installed. Windows authentication is shown; use the appropriate authenticated connection settings for your environment.

```powershell
$sqlcmd = "$env:ProgramFiles\Microsoft SQL Server\Client SDK\ODBC\180\Tools\Binn\SQLCMD.EXE"
& $sqlcmd -S localhost -E -C -b -i database/scripts/InitializeDatabase.sql
```

`InitializeDatabase.sql` creates `HospitalManagementDb` if needed, executes schema scripts `001` through `010` in dependency order, inserts fictional development seed data, and runs `ValidateDatabase.sql`. The `-C` switch trusts the development certificate; `-b` returns a non-zero exit code if a SQL error occurs. SQL Server 2025 installs the ODBC 18 client used above; older ODBC 17 `sqlcmd` clients can fail during local encrypted connections.

The seed data deliberately contains no real patient information. Its password-hash placeholders are not credentials and must never be deployed to a shared or production environment.
