var builder = DistributedApplication.CreateBuilder(args);

var aca = builder.AddAzureContainerAppEnvironment("aca");

var sql = builder.AddAzureSqlServer("sql")
    .RunAsContainer(container => container
        .WithDataVolume("potsure-sql-data")
        .WithLifetime(ContainerLifetime.Persistent));

var database = sql.AddDatabase("potsure");

#pragma warning disable ASPIREDOTNETPROJECT001
var schemaHost = builder.AddDotnetProject("schema-host", "../Potsure.MigrationsHost/Potsure.MigrationsHost.csproj")
#pragma warning restore ASPIREDOTNETPROJECT001
    .WithReference(database)
    .WaitFor(database)
    .WithHttpEndpoint(port: 5192);

#pragma warning disable ASPIREPROJECTS001
var dataProtectionMigrations = schemaHost.AddEFMigrations(
        "ef-data-protection-migrations",
        "Potsure.Data.PotsureDataProtectionDbContext")
    .WithReference(database)
    .WaitFor(database)
    .WithEnvironment("ASPNETCORE_URLS", "http://127.0.0.1:0")
    .WithMigrationsProject("../Potsure.Data/Potsure.Data.csproj")
    .RunDatabaseUpdateOnStart()
    .PublishAsMigrationScript();
#pragma warning restore ASPIREPROJECTS001

#pragma warning disable ASPIREPROJECTS001
var migrations = schemaHost.AddEFMigrations(
        "ef-migrations",
        "Potsure.Data.PotsureDbContext")
    .WithReference(database)
    .WaitFor(database)
    .WaitForCompletion(dataProtectionMigrations)
    .WithEnvironment("ASPNETCORE_URLS", "http://127.0.0.1:0")
    .WithMigrationsProject("../Potsure.Data/Potsure.Data.csproj")
    .RunDatabaseUpdateOnStart()
    .PublishAsMigrationScript();
#pragma warning restore ASPIREPROJECTS001

#pragma warning disable ASPIREDOTNETPROJECT001
var web = builder.AddDotnetProject("web", "../Potsure.Web/Potsure.Web.csproj")
#pragma warning restore ASPIREDOTNETPROJECT001
    .WithExternalHttpEndpoints()
    .WithEndpoint("http", endpoint => endpoint.Port = 5191)
    .WithHttpHealthCheck("/health")
    .WithReference(database)
    .WithEnvironment("Deployment__Mode", "Hosted")
    .WithComputeEnvironment(aca)
    .PublishAsAzureContainerApp((_, _) => { });

web.WaitForCompletion(migrations);

builder.Build().Run();
