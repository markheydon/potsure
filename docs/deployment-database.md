# Database deployment

Potsure stores application data, ASP.NET Core Identity (hosted mode, including passkeys in `AspNetUserPasskeys`), and Data Protection keys in SQL Server (Azure SQL in hosted deployments; SQL container locally via Aspire).

## Migrations

EF Core migrations live in `src/Potsure.Infrastructure/Persistence/Migrations/`. Apply them **once** before serving user traffic on a new environment:

```bash
dotnet ef database update \
  --project src/Potsure.Infrastructure/Potsure.Infrastructure.csproj \
  --startup-project src/Potsure.Web/Potsure.Web.csproj
```

With Aspire running locally, use the connection string emitted for the `potsure` database resource.

## Idempotent production apply

For hosted stages, prefer a controlled migration job (single runner) rather than letting every replica migrate on startup:

1. Take a backup or confirm restore point (Azure SQL point-in-time restore).
2. Run `dotnet ef database update` from CI or a one-off deployment job against the target connection string.
3. Verify schema version (`__EFMigrationsHistory`) before shifting traffic.
4. Roll forward only; do not edit applied migration files.

Review generated SQL in staging (`dotnet ef migrations script`) before the first public environment.

## Local development

The AppHost provisions SQL with `RunAsContainer`. The web project references the `potsure` database via Aspire service discovery; migrations use the same connection as runtime when `dotnet ef` is invoked with the web startup project.

## Data Protection keys

Keys persist to the `DataProtectionKeys` table through `IDataProtectionKeyContext` on `PotsureDbContext`. Ensure migrations creating this table are applied before scaling out multiple web instances.
