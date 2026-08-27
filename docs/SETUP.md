# Setup

## Requirements

- .NET SDK 8
- Node.js 22 and npm
- SQL Server 2022/2025 or compatible SQL Server instance
- ODBC Driver 18 `sqlcmd` client

## Configure

1. Copy `src/Hospital.Api/appsettings.Local.example.json` to `appsettings.Local.json` in the same directory.
2. Set the local SQL Server connection string and a unique JWT signing key of at least 32 characters.
3. Keep that local file uncommitted. It is ignored by `.gitignore`.
4. Initialize the database using [database/README.md](../database/README.md).

## Run

```powershell
dotnet restore HospitalManagementSystem.sln -p:NuGetAudit=false
dotnet build HospitalManagementSystem.sln --no-restore
dotnet run --project src/Hospital.Api
```

In another terminal:

```powershell
Set-Location client/hospital-web
npm ci
npm run dev
```

The web client uses `http://localhost:5173`; `http://127.0.0.1:5173` is also allowed for local development. Open the displayed Vite URL and use fictional seed accounts only.
