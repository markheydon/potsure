# Database deployment

Potsure stores application data, ASP.NET Core Identity (hosted mode, including passkeys in `AspNetUserPasskeys`), and Data Protection keys in SQL Server (Azure SQL in hosted deployments; SQL container locally via Aspire).

## Ownership

| Concern | Project |
|---------|---------|
| `DbContext`, EF entities, configurations, migrations | [`src/Potsure.Data`](../src/Potsure.Data/) |
| Repositories, services, runtime seeding | [`src/Potsure.Infrastructure`](../src/Potsure.Infrastructure/) |
| Runtime SQL registration | [`src/Potsure.Web`](../src/Potsure.Web/Program.cs) via `AddSqlServerDbContext` (client name `potsure`) |

## Migrations

EF Core migrations live under `src/Potsure.Data/Migrations/`:

- **Application + Identity:** `Migrations/PotsureDbContext/`
- **Data Protection keys:** `Migrations/PotsureDataProtectionDbContext/` (history table `__EFDataProtectionMigrationsHistory`)

### Developer commands

Set `ConnectionStrings__potsure` from the Aspire dashboard or AppHost environment output before running design-time tools. Do not commit connection strings.

**Application / Identity context:**

```bash
export ConnectionStrings__potsure='…'   # Aspire-injected value

dotnet ef migrations add <Name> \
  --project src/Potsure.Data/Potsure.Data.csproj \
  --context Potsure.Data.PotsureDbContext \
  --output-dir Migrations/PotsureDbContext \
  --namespace Potsure.Data.Migrations
```

**Data Protection context:**

```bash
dotnet ef migrations add <Name> \
  --project src/Potsure.Data/Potsure.Data.csproj \
  --context Potsure.Data.PotsureDataProtectionDbContext \
  --output-dir Migrations/PotsureDataProtectionDbContext \
  --namespace Potsure.Data.Migrations.PotsureDataProtectionDbContext
```

Apply pending migrations manually (optional; local AppHost applies automatically):

```bash
dotnet ef database update \
  --project src/Potsure.Data/Potsure.Data.csproj \
  --context Potsure.Data.PotsureDbContext

dotnet ef database update \
  --project src/Potsure.Data/Potsure.Data.csproj \
  --context Potsure.Data.PotsureDataProtectionDbContext
```

Design-time factories in `src/Potsure.Data/DesignTime/` resolve the `potsure` connection from configuration/environment only.

### Standing plan migration (`StandingPlanAndRecurringAnchors`)

Copies `BudgetPeriods.BudgetedIncomeAmount` into `BudgetProfiles.EstimatedIncomeAmount` (cadence **Monthly** when income was non-zero), drops `BudgetPeriods`, and stores recurring `AmountKind` as an integer enum. Rolling back recreates an empty `BudgetPeriods` table; restore period dates and budgeted income manually if needed.

## Local Aspire (automatic schema apply)

When you run the AppHost locally (`aspire run` or F5 on `Potsure.AppHost`):

1. SQL container becomes healthy.
2. **ef-data-protection-migrations** runs `dotnet ef database update` for `PotsureDataProtectionDbContext`, then exits.
3. **ef-migrations** runs for `PotsureDbContext`, then exits.
4. **web** starts after both complete (`WaitForCompletion`).

Inspect hidden migration resources:

```bash
aspire describe --include-hidden
```

`Potsure.Web` does **not** call `Database.MigrateAsync()` at startup. Application seeding (`BudgetProfileSeeder`) runs in the web process after schema exists, not inside the migration job.

## Hosted deployment (controlled apply)

Use a **single runner** per environment  -  not concurrent migrate on every web replica:

1. Backup or confirm restore point (Azure SQL point-in-time restore).
2. Apply schema using one of:
   - Idempotent SQL scripts from `PublishAsMigrationScript()` (under `efmigrations/` in the publish directory), **Data Protection first**, then application migrations; or
   - `dotnet ef database update` for each context from CI; or
   - A published EF migration bundle executed once per context.
3. Verify both history tables before shifting traffic.
4. Roll forward only; do not edit applied migration files.

## Data Protection keys

Keys persist through `PotsureDataProtectionDbContext` and `PersistKeysToDbContext`. Apply Data Protection migrations before scaling out multiple web instances.
