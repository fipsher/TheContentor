var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("the-contentor-postgres", port: 5433)
    // .WithLifetime(ContainerLifetime.Persistent)
    .WithEnvironment("POSTGRES_HOST_AUTH_METHOD", "trust")
    .WithDataVolume("the-contentor-data")
    .WithContainerName("the-contentor-postgres")
    .WithEndpointProxySupport(false);

var postgresDb = postgres.AddDatabase("the-contentor-db");

// Service Bus & Queues
// var serviceBus = builder
//     .AddAzureServiceBus("ContentorServiceBus")
//     .RunAsEmulator(c => c.WithLifetime(ContainerLifetime.Session));
// serviceBus.WithAnnotation(new ProxySupportAnnotation { ProxyEnabled = false }, ResourceAnnotationMutationBehavior.Replace);

// serviceBus.AddServiceBusQueue("tdm-queue");
// serviceBus.AddServiceBusQueue("tdm-feedback-queue");
// serviceBus.AddServiceBusQueue("rules-engine-queue");
// serviceBus.AddServiceBusQueue("cdm-tdm-queue");
// serviceBus.AddServiceBusQueue("cdm-rules-engine-queue");
// serviceBus.AddServiceBusQueue("cdm-feedback-queue");
// serviceBus.AddServiceBusQueue("infrastructure-management-queue");
// serviceBus.AddServiceBusQueue("infrastructure-management-callback-queue");


builder.AddProject<Projects.TheContentor_API>("the-contentor")
    .WithReference(postgresDb)
    .WaitFor(postgresDb);

builder.Build().Run();
