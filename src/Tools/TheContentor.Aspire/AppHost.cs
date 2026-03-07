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

// Local file storage (replaces Azure Blob Storage emulator)
var storageBasePath = Path.GetFullPath(Path.Combine(builder.AppHostDirectory, "..", "..", "storage"));
Directory.CreateDirectory(storageBasePath);

// Service Bus & Queues
var serviceBus = builder
    .AddAzureServiceBus("ContentorServiceBus")
    .RunAsMyEmulator(c => { c.WithLifetime(ContainerLifetime.Session); });
ConfigureServiceBus(serviceBus);

// Local LLM via Ollama — persistent container so models survive Aspire restarts
var ollama = builder.AddOllama("ollama")
    .WithAnnotation(new ContainerImageAnnotation { Image = OllamaContainerImageTags.Image, Tag = OllamaContainerImageTags.Tag, Registry = OllamaContainerImageTags.Registry }, ResourceAnnotationMutationBehavior.Replace)
    .WithDataVolume()
    .WithOpenWebUI();

ollama.AddModel("qwen2.5:7b");

// TTS Preview HTTP server (for playground / test generation)
var ttsPreview = builder.AddPythonApp("tts-preview", "../../Modules/TTS", "tts-preview.py")
    .WithHttpEndpoint(port: 8765, name: "http", env: "PORT")
    .WithEnvironment("STORAGE_BASE_PATH", storageBasePath)
    .WithEnvironment("PYTHONUNBUFFERED", "1");

var apiService = builder.AddProject<Projects.TheContentor_API>("the-contentor")
    .WithReference(postgresDb)
    .WithReference(serviceBus)
    .WithReference(ollama)
    .WithEnvironment("LocalStorage__BasePath", storageBasePath)
    .WithEnvironment("TtsPreview__Url", ttsPreview.GetEndpoint("http"))
    .WaitFor(postgresDb)
    .WaitFor(ollama);

builder
    .AddAzureFunctionsProject<Projects.TheContentor_Orchestrator>("Func-Orchestrator")
    .WithReference(serviceBus)
    .WithEnvironment("TheContentorApiUrl", apiService.GetEndpoint("http"))
    .WaitFor(serviceBus)
    .WaitFor(apiService);

// Python TTS Worker
builder.AddPythonApp("tts-worker", "../../Modules/TTS", "tts-worker.py")
    .WithReference(serviceBus)
    .WithEnvironment("STORAGE_BASE_PATH", storageBasePath)
    .WithEnvironment("PYTHONUNBUFFERED", "1")
    .WaitFor(serviceBus)
    .WaitFor(apiService);

// Python Video Worker
builder.AddPythonApp("video-worker", "../../Modules/Video", "video-worker.py")
    .WithReference(serviceBus)
    .WithEnvironment("STORAGE_BASE_PATH", storageBasePath)
    .WithEnvironment("PYTHONUNBUFFERED", "1")
    .WaitFor(serviceBus)
    .WaitFor(apiService);

// Python Subtitle Worker
builder.AddPythonApp("subtitle-worker", "../../Modules/Subtitle", "subtitle-worker.py")
    .WithReference(serviceBus)
    .WithEnvironment("STORAGE_BASE_PATH", storageBasePath)
    .WithEnvironment("PYTHONUNBUFFERED", "1")
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
                        Properties = new Dictionary<string, object>
                        {
                            { "Type", "tts" },
                        },
                    },
                });
        });
}