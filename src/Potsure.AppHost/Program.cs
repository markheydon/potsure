var builder = DistributedApplication.CreateBuilder(args);

var aca = builder.AddAzureContainerAppEnvironment("aca");

var sql = builder.AddAzureSqlServer("sql")
    .RunAsContainer();

var database = sql.AddDatabase("potsure");

builder.AddProject<Projects.Potsure_Web>("web")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(database)
    .WaitFor(database)
    .WithComputeEnvironment(aca)
    .PublishAsAzureContainerApp((_, _) => { });

builder.Build().Run();
