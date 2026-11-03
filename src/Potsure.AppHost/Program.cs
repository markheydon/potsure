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

var migrations = web.AddEFMigrations(
        "ef-migrations",
        "Potsure.Infrastructure.Persistence.PotsureDbContext")
    .WithReference(database)
    .WaitFor(database)
    .WithMigrationsProject<Projects.Potsure_Infrastructure>()
    .RunDatabaseUpdateOnStart()
    .PublishAsMigrationScript();

web.WaitForCompletion(migrations);

builder.Build().Run();
