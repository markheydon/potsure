var builder = DistributedApplication.CreateBuilder(args);

var aca = builder.AddAzureContainerAppEnvironment("aca");

var storage = builder.AddAzureStorage("storage")
    .RunAsEmulator(azurite => azurite
        .WithDataVolume("potsure-userdata")
        .WithLifetime(ContainerLifetime.Persistent));

var userdata = storage.AddBlobContainer("userdata", blobContainerName: "potsure-userdata");

var web = builder.AddDotnetProject("web", "../Potsure.Web/Potsure.Web.csproj")
    .WithExternalHttpEndpoints()
    .WithEndpoint("http", endpoint => endpoint.Port = 5191)
    .WithHttpHealthCheck("/health")
    .WithReference(userdata)
    .WaitFor(userdata)
    .WithEnvironment("Deployment__Mode", "Hosted")
    .WithComputeEnvironment(aca)
    .PublishAsAzureContainerApp((_, _) => { });

builder.Build().Run();
