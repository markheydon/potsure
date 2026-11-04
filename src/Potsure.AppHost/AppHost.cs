var builder = DistributedApplication.CreateBuilder(args);

var aca = builder.AddAzureContainerAppEnvironment("aca");

var sql = builder.AddAzureSqlServer("sql")
    .RunAsContainer();

var database = sql.AddDatabase("potsure");

var web = builder.AddProject<Projects.Potsure_Web>("web")
    .WithExternalHttpEndpoints()
    .WithEndpoint("http", endpoint => endpoint.Port = 5191)
    .WithHttpHealthCheck("/health")
    .WithReference(database)
    .WithEnvironment("Deployment__Mode", "Hosted")
    .WithComputeEnvironment(aca)
    .PublishAsAzureContainerApp((_, _) => { });

#pragma warning disable ASPIREPROJECTS001
var dataProtectionMigrations = web.AddEFMigrations(
#pragma warning restore ASPIREPROJECTS001
        "ef-data-protection-migrations",
        "Potsure.Data.PotsureDataProtectionDbContext")
    .WithReference(database)
    .WaitFor(database)
    .WithMigrationsProject("../Potsure.Data/Potsure.Data.csproj")
    .RunDatabaseUpdateOnStart()
    .PublishAsMigrationScript();

#pragma warning disable ASPIREPROJECTS001
var migrations = web.AddEFMigrations(
#pragma warning restore ASPIREPROJECTS001
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
