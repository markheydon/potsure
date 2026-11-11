using Microsoft.EntityFrameworkCore;
using Potsure.Data;

/// <summary>
/// Minimal startup host for Aspire EF migration resources (avoids Blazor web endpoint wiring).
/// </summary>
var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddSqlServerDbContext<PotsureDbContext>("potsure");
builder.AddSqlServerDbContext<PotsureDataProtectionDbContext>(
    "potsure",
    configureDbContextOptions: options =>
        options.UseSqlServer(
            sql => sql.MigrationsHistoryTable("__EFDataProtectionMigrationsHistory")));

var app = builder.Build();
app.MapGet("/", () => Results.Ok("Potsure schema apply host"));
await app.RunAsync();
