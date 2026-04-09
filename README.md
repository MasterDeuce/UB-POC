# UB Work Instructions POC

This repository contains a proof-of-concept for generating, reviewing, and publishing work instructions from project artifacts. It is organized as a .NET solution with a Blazor web app, Azure Functions worker, and layered domain/application/infrastructure projects.

## Setup

### Prerequisites

- .NET SDK 8.0+
- SQL Server (local SQL Server container/instance or Azure SQL)
- Azure Functions Core Tools v4 (for local Functions execution)
- Azure CLI (optional but recommended for provisioning/deployment)

### Clone and restore

```bash
git clone <repo-url>
cd UB-POC
dotnet restore WorkInstructions.sln
```

### Configure local settings

1. Copy the web app sample config and fill in local values:
   - `src/Web/appsettings.Example.json` -> `src/Web/appsettings.Development.json` (or user secrets / env vars)
2. Copy the Azure Functions sample config:
   - `src/Functions/local.settings.example.json` -> `src/Functions/local.settings.json`
3. Ensure all secrets are provided via environment variables, Key Vault references, or local secret stores (do not commit real credentials).

## Architecture

The solution is split by responsibility:

- `src/Domain`: Core entities and enums that model projects, jobs, drafts, and audit concepts.
- `src/Application`: DTOs and service interfaces for orchestration, extraction, generation, audit, and integrations.
- `src/Infrastructure`: EF Core persistence models/configuration and migrations.
- `src/Web`: Blazor-based operator UI and API host concerns.
- `src/Functions`: Background/async processing host for workflow automation.
- `tests/WorkInstructions.Tests`: Basic test project and smoke tests.

### High-level workflow

1. A project/document is registered from the Web app.
2. A work instruction job is created and persisted in SQL.
3. Functions/background services execute extraction and generation steps.
4. Drafts are reviewed/edited in the Web UI.
5. Finalized documents are published to target systems (for example SharePoint/Procore).

## Run instructions

### Run web app

```bash
dotnet run --project src/Web/Web.csproj
```

Expected health endpoint: `GET /health`.

> If no `ConnectionStrings:AppDb` value is provided, the web app now automatically uses an in-memory database so it can run out-of-the-box for local UI testing.

### Run functions app

```bash
func start --csharp --port 7071 --script-root src/Functions
```

If using plain dotnet hosting for diagnostics:

```bash
dotnet run --project src/Functions/Functions.csproj
```

### Run tests

```bash
dotnet test WorkInstructions.sln
```

## Database migration steps

Migrations are located under `src/Infrastructure/Persistence/Migrations`.

### Apply existing migrations

```bash
dotnet ef database update \
  --project src/Infrastructure/Infrastructure.csproj \
  --startup-project src/Web/Web.csproj
```

### Create a new migration

```bash
dotnet ef migrations add <MigrationName> \
  --project src/Infrastructure/Infrastructure.csproj \
  --startup-project src/Web/Web.csproj \
  --output-dir Persistence/Migrations
```

### Generate idempotent SQL script (recommended for CI/CD)

```bash
dotnet ef migrations script --idempotent \
  --project src/Infrastructure/Infrastructure.csproj \
  --startup-project src/Web/Web.csproj \
  --output ./artifacts/sql/migrations.sql
```

## Local development flow

1. Pull latest changes and restore packages.
2. Start local SQL Server and verify connection string values.
3. Apply EF migrations.
4. Run Web app and Functions app in parallel.
5. Exercise end-to-end scenarios (upload/validate/generate/finalize).
6. Run `dotnet test` before committing.
7. Keep secrets outside source control and rotate any leaked credentials immediately.

## Additional documentation

- `docs/azure-resources.md`
- `docs/graph-permissions.md`
- `docs/procore-configuration.md`
- `docs/production-hardening.md`
