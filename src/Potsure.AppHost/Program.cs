var builder = DistributedApplication.CreateBuilder(args);

var aca = builder.AddAzureContainerAppEnvironment("aca");

var sql = builder.AddAzureSqlServer("sql")
    .RunAsContainer();

var database = sql.AddDatabase("potsure");

var web = builder.AddProject<Projects.Potsure_Web>("web")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(database)
    .WithEnvironment("Deployment__Mode", "Hosted")
    .WithComputeEnvironment(aca)
    .PublishAsAzureContainerApp((_, _) => { });

var dataProtectionMigrations = web.AddEFMigrations(
        "ef-data-protection-migrations",
        "Potsure.Data.PotsureDataProtectionDbContext")
    .WithReference(database)
    .WaitFor(database)
    .WithMigrationsProject("../Potsure.Data/Potsure.Data.csproj")
    .RunDatabaseUpdateOnStart()
    .PublishAsMigrationScript();

var migrations = web.AddEFMigrations(
        "ef-migrations",
        "Potsure.Data.PotsureDbContext")
    .WithReference(database)
    .WaitFor(database)
    .WaitForCompletion(dataProtectionMigrations)
    .WithMigrationsProject("../Potsure.Data/Potsure.Data.csproj")
    .RunDatabaseUpdateOnStart()
    .PublishAsMigrationScript();

web.WaitForCompletion(migrations);

builder.Build().Run();
