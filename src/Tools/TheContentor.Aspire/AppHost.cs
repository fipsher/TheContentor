using Aspire.Hosting.Azure;
using TheContentor.Aspire;

var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("the-contentor-postgres", port: 5433)
    // .WithLifetime(ContainerLifetime.Persistent)
    .WithEnvironment("POSTGRES_HOST_AUTH_METHOD", "trust")
    .WithDataVolume("the-contentor-data")
    .WithContainerName("the-contentor-postgres")
    .WithEndpointProxySupport(false);

var postgresDb = postgres.AddDatabase("the-contentor-db");

var storage = builder.AddAzureStorage("storage")
    .RunAsEmulator(cfg => cfg.WithDataVolume("the-contentor-storage"));

var blobs = storage.AddBlobs("blobs");

// Service Bus & Queues
var serviceBus = builder
    .AddAzureServiceBus("ContentorServiceBus")
    .RunAsMyEmulator(c => { c.WithLifetime(ContainerLifetime.Session); });
ConfigureServiceBus(serviceBus);

var apiService = builder.AddProject<Projects.TheContentor_API>("the-contentor")
    .WithReference(postgresDb)
    .WithReference(blobs)
    .WithReference(serviceBus)
    .WaitFor(postgresDb);

builder
    .AddAzureFunctionsProject<Projects.TheContentor_Orchestrator>("Func-Orchestrator")
    .WithReference(serviceBus)
    .WithEnvironment("TheContentorApiUrl", apiService.GetEndpoint("http"))
    .WaitFor(serviceBus)
    .WaitFor(apiService);

// Python TTS Worker
builder.AddPythonApp("tts-worker", "../../Modules/TTS", "tts-worker.py")
    .WithReference(serviceBus)
    .WithReference(blobs)
    .WithReference(apiService)
    .WithEnvironment("TheContentorApiUrl", apiService.GetEndpoint("http"))
    .WaitFor(serviceBus)
    .WaitFor(apiService);

// Python Video Worker
builder.AddPythonApp("video-worker", "../../Modules/Video", "video-worker.py")
    .WithReference(serviceBus)
    .WithReference(blobs)
    .WithReference(apiService)
    .WithEnvironment("TheContentorApiUrl", apiService.GetEndpoint("http"))
    .WaitFor(serviceBus)
    .WaitFor(apiService);

// Python Subtitle Worker
builder.AddPythonApp("subtitle-worker", "../../Modules/Subtitle", "subtitle-worker.py")
    .WithReference(serviceBus)
    .WithReference(blobs)
    .WithReference(apiService)
    .WithEnvironment("TheContentorApiUrl", apiService.GetEndpoint("http"))
    .WaitFor(serviceBus)
    .WaitFor(apiService);

builder.Build().Run();

void ConfigureServiceBus(IResourceBuilder<AzureServiceBusResource> resourceBuilder)
{
    serviceBus.WithAnnotation(new ProxySupportAnnotation { ProxyEnabled = false },
        ResourceAnnotationMutationBehavior.Replace);

    resourceBuilder.AddServiceBusQueue("trigger-orchestration-queue");

    var commandsTopic = serviceBus.AddServiceBusTopic("commands-topic");
    resourceBuilder.AddServiceBusQueue("tts-commands-queue");
    resourceBuilder.AddServiceBusQueue("video-commands-queue");
    resourceBuilder.AddServiceBusQueue("subtitle-commands-queue");

    resourceBuilder.AddServiceBusQueue("events-queue");

    ConfigureCommandsSubscriptions(commandsTopic);
}

void ConfigureCommandsSubscriptions(IResourceBuilder<AzureServiceBusTopicResource> commandsTopic)
{
    commandsTopic.AddServiceBusSubscription("tts-commands-subscription")
        .WithProperties(subscription =>
        {
            subscription.ForwardTo = "tts-commands-queue";
            subscription.MaxDeliveryCount = 5;
            subscription.Rules.Add(
                new AzureServiceBusRule("tts-commands-subscription-filter")
                {
                    FilterType = AzureServiceBusFilterType.CorrelationFilter,
                    CorrelationFilter = new AzureServiceBusCorrelationFilter
                    {
                        Properties = new Dictionary<string, object>()
                        {
                            { "Type", "tts" },
                        },
                    },
                });
        });
}