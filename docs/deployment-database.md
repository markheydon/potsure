# Database deployment

Potsure stores application data, ASP.NET Core Identity (hosted mode, including passkeys in `AspNetUserPasskeys`), and Data Protection keys in SQL Server (Azure SQL in hosted deployments; SQL container locally via Aspire).

## Migrations

EF Core migrations live in `src/Potsure.Infrastructure/Persistence/Migrations/`.

### Developer commands

Create a new migration or apply pending migrations manually:

```bash
dotnet ef migrations add <Name> \
  --project src/Potsure.Infrastructure/Potsure.Infrastructure.csproj \
  --startup-project src/Potsure.Web/Potsure.Web.csproj

dotnet ef database update \
  --project src/Potsure.Infrastructure/Potsure.Infrastructure.csproj \
  --startup-project src/Potsure.Web/Potsure.Web.csproj
```

The design-time factory in `PotsureDbContextFactory` may use a local connection string from configuration; runtime uses Aspire `AddSqlServerDbContext` with the `potsure` database reference name.

## Local Aspire (automatic schema apply)

When you run the AppHost locally (`aspire run` or F5 on `Potsure.AppHost`), schema is applied **before** the web app starts:

1. SQL container becomes healthy.
2. The **ef-migrations** resource runs `dotnet ef database update` once and exits (`RunDatabaseUpdateOnStart`).
3. The web project waits for that migration resource to complete (`WaitForCompletion`).

The migration resource is hidden in the Aspire dashboard by default. Inspect it with:

```bash
aspire describe --include-hidden
```

`Potsure.Web` does **not** call `Database.MigrateAsync()` at startup. Application data seeding (for example default budget categories via `BudgetProfileSeeder`) runs in the web process after schema exists, not inside the migration job.

## Hosted deployment (controlled apply)

For hosted stages, use a **single runner** — not `RunDatabaseUpdateOnStart` on publish and not concurrent migrate on every web replica:

1. Take a backup or confirm restore point (Azure SQL point-in-time restore).
2. Apply schema using one of:
   - The idempotent SQL script generated at publish time (`PublishAsMigrationScript()` on the AppHost migration resource — output under `efmigrations/` in the publish directory), reviewed in staging first; or
   - `dotnet ef database update` from CI or a one-off deployment job against the target connection string; or
   - A published EF migration bundle executed once with the target connection string.
3. Verify schema version (`__EFMigrationsHistory`) before shifting traffic.
4. Roll forward only; do not edit applied migration files.

Review generated SQL in staging (`dotnet ef migrations script`) before the first public environment.

## Idempotent production apply

Prefer idempotent scripts (`IF NOT EXISTS` checks) when using `PublishAsMigrationScript()` so a partially applied database can be retried safely.

## Data Protection keys

Keys persist to the `DataProtectionKeys` table through `IDataProtectionKeyContext` on `PotsureDbContext`. Ensure migrations creating this table are applied before scaling out multiple web instances.
