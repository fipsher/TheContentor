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
    .RunAsEmulator();

var blobs = storage.AddBlobs("blobs");

// Service Bus & Queues
var serviceBus = builder
    .AddAzureServiceBus("ContentorServiceBus")
    .RunAsMyEmulator(c => { c.WithLifetime(ContainerLifetime.Session); });
ConfigureServiceBus(serviceBus);

builder
    .AddAzureFunctionsProject<Projects.TheContentor_Orchestrator>("Func-Orchestrator")
    .WithReference(serviceBus)
    .WaitFor(serviceBus);

builder.AddProject<Projects.TheContentor_API>("the-contentor")
    .WithReference(postgresDb)
    .WithReference(blobs)
    .WaitFor(postgresDb);

builder.Build().Run();

void ConfigureServiceBus(IResourceBuilder<AzureServiceBusResource> resourceBuilder)
{
    serviceBus.WithAnnotation(new ProxySupportAnnotation { ProxyEnabled = false },
        ResourceAnnotationMutationBehavior.Replace);
    
    resourceBuilder.AddServiceBusQueue("trigger-orchestration");

    var commandsTopic = serviceBus.AddServiceBusTopic("commands-topic");
    resourceBuilder.AddServiceBusQueue("asset-metadata-commands-queue");
    resourceBuilder.AddServiceBusQueue("scrapper-commands-queue");
    resourceBuilder.AddServiceBusQueue("video-generation-commands-queue");

    resourceBuilder.AddServiceBusQueue("events-queue");

    ConfigureCommandsSubscriptions(commandsTopic);
}

void ConfigureEventSubscriptions(IResourceBuilder<AzureServiceBusTopicResource> eventsTopic)
{
    eventsTopic.AddServiceBusSubscription("asset-metadata-events-subscription")
        .WithProperties(subscription =>
        {
            subscription.ForwardTo = "asset-metadata-events-queue";
            subscription.MaxDeliveryCount = 5;
            subscription.Rules.Add(
                new AzureServiceBusRule("asset-metadata-events-subscription-filter")
                {
                    FilterType = AzureServiceBusFilterType.CorrelationFilter,
                    CorrelationFilter = new AzureServiceBusCorrelationFilter
                    {
                        Properties = new Dictionary<string, object>()
                        {
                            { "Type", "asset-metadata" },
                        },
                    },
                });
        });

    eventsTopic.AddServiceBusSubscription("scrapper-events-subscription")
        .WithProperties(subscription =>
        {
            subscription.ForwardTo = "scrapper-events-queue";
            subscription.MaxDeliveryCount = 5;
            subscription.Rules.Add(
                new AzureServiceBusRule("scrapper-events-subscription-filter")
                {
                    FilterType = AzureServiceBusFilterType.CorrelationFilter,
                    CorrelationFilter = new AzureServiceBusCorrelationFilter
                    {
                        Properties = new Dictionary<string, object>()
                        {
                            { "Type", "scrapper" },
                        },
                    },
                });
        });

    eventsTopic.AddServiceBusSubscription("video-generation-events-subscription")
        .WithProperties(subscription =>
        {
            subscription.ForwardTo = "video-generation-events-queue";
            subscription.MaxDeliveryCount = 5;
            subscription.Rules.Add(
                new AzureServiceBusRule("video-generation-events-subscription-filter")
                {
                    FilterType = AzureServiceBusFilterType.CorrelationFilter,
                    CorrelationFilter = new AzureServiceBusCorrelationFilter
                    {
                        Properties = new Dictionary<string, object>()
                        {
                            { "Type", "video-generation" },
                        },
                    },
                });
        });
}

void ConfigureCommandsSubscriptions(IResourceBuilder<AzureServiceBusTopicResource> commandsTopic)
{
    commandsTopic.AddServiceBusSubscription("asset-metadata-commands-subscription")
        .WithProperties(subscription =>
        {
            subscription.ForwardTo = "asset-metadata-commands-queue";
            subscription.MaxDeliveryCount = 5;
            subscription.Rules.Add(
                new AzureServiceBusRule("asset-metadata-commands-subscription-filter")
                {
                    FilterType = AzureServiceBusFilterType.CorrelationFilter,
                    CorrelationFilter = new AzureServiceBusCorrelationFilter
                    {
                        Properties = new Dictionary<string, object>()
                        {
                            { "Type", "asset-metadata" },
                        },
                    },
                });
        });

    commandsTopic.AddServiceBusSubscription("scrapper-commands-subscription")
        .WithProperties(subscription =>
        {
            subscription.ForwardTo = "scrapper-commands-queue";
            subscription.MaxDeliveryCount = 5;
            subscription.Rules.Add(
                new AzureServiceBusRule("scrapper-commands-subscription-filter")
                {
                    FilterType = AzureServiceBusFilterType.CorrelationFilter,
                    CorrelationFilter = new AzureServiceBusCorrelationFilter
                    {
                        Properties = new Dictionary<string, object>()
                        {
                            { "Type", "scrapper" },
                        },
                    },
                });
        });

    commandsTopic.AddServiceBusSubscription("video-generation-commands-subscription")
        .WithProperties(subscription =>
        {
            subscription.ForwardTo = "video-generation-commands-queue";
            subscription.MaxDeliveryCount = 5;
            subscription.Rules.Add(
                new AzureServiceBusRule("video-generation-commands-subscription-filter")
                {
                    FilterType = AzureServiceBusFilterType.CorrelationFilter,
                    CorrelationFilter = new AzureServiceBusCorrelationFilter
                    {
                        Properties = new Dictionary<string, object>()
                        {
                            { "Type", "video-generation" },
                        },
                    },
                });
        });
}