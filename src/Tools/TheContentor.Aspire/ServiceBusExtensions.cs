using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Aspire.Hosting.Azure;
using Microsoft.Extensions.DependencyInjection;

namespace TheContentor.Aspire;

internal static class ServiceBusExtensions
{
    private const UnixFileMode FileMode644 = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead |
                                             UnixFileMode.OtherRead;

    private const string EmulatorHealthEndpointName = "emulatorhealth";
    private const string EmulatorConfigJsonPath = "/ServiceBus_Emulator/ConfigFiles/Config.json";

    public static IResourceBuilder<AzureServiceBusResource> RunAsMyEmulator(
        this IResourceBuilder<AzureServiceBusResource> builder,
        Action<IResourceBuilder<AzureServiceBusEmulatorResource>>? configureContainer = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (builder.Resource.IsEmulator)
        {
            throw new InvalidOperationException(
                "The Azure Service Bus resource is already configured to run as an emulator.");
        }

        if (builder.ApplicationBuilder.ExecutionContext.IsPublishMode)
        {
            return builder;
        }

        // Add emulator container

        // The password must be at least 8 characters long and contain characters from three of the following four sets: Uppercase letters, Lowercase letters, Base 10 digits, and Symbols
        var passwordParameter = ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(
            builder.ApplicationBuilder, $"{builder.Resource.Name}-sql-pwd", minLower: 1, minUpper: 1, minNumeric: 1);

        builder
            .WithEndpoint(name: "emulator", targetPort: 5672)
            .WithHttpEndpoint(name: EmulatorHealthEndpointName, targetPort: 5300)
            .WithAnnotation(new ContainerImageAnnotation
            {
                Registry = ServiceBusEmulatorContainerImageTags.Registry,
                Image = ServiceBusEmulatorContainerImageTags.Image,
                Tag = ServiceBusEmulatorContainerImageTags.Tag,
            })
            .WithUrlForEndpoint(EmulatorHealthEndpointName, u => u.DisplayLocation = UrlDisplayLocation.DetailsOnly);

        var sqlResource = builder.ApplicationBuilder
            .AddContainer(
                $"{builder.Resource.Name}-sqledge",
                image: ServiceBusEmulatorContainerImageTags.AzureSqlEdgeImage,
                tag: ServiceBusEmulatorContainerImageTags.AzureSqlEdgeTag)
            .WithImageRegistry(ServiceBusEmulatorContainerImageTags.AzureSqlEdgeRegistry)
            .WithEndpoint(targetPort: 1433, name: "tcp")
            .WithEnvironment("ACCEPT_EULA", "Y")
            .WithEnvironment(context =>
            {
                context.EnvironmentVariables["MSSQL_SA_PASSWORD"] = passwordParameter;
            })
            .WithParentRelationship(builder);

        builder.WithAnnotation(new EnvironmentCallbackAnnotation(context =>
        {
            // ReSharper disable once AccessToModifiedClosure
            var sqlEndpoint = sqlResource.Resource.GetEndpoint("tcp");

            context.EnvironmentVariables.Add("ACCEPT_EULA", "Y");
            context.EnvironmentVariables.Add("SQL_SERVER", $"{sqlEndpoint.Resource.Name}:{sqlEndpoint.TargetPort}");
            context.EnvironmentVariables.Add("MSSQL_SA_PASSWORD", passwordParameter);
        }));

        var lifetime = ContainerLifetime.Session;

        if (configureContainer != null)
        {
            var surrogate = new AzureServiceBusEmulatorResource(builder.Resource);
            var surrogateBuilder = builder.ApplicationBuilder.CreateResourceBuilder(surrogate);
            configureContainer(surrogateBuilder);

            if (surrogate.TryGetLastAnnotation<ContainerLifetimeAnnotation>(out var lifetimeAnnotation))
            {
                lifetime = lifetimeAnnotation.Lifetime;
            }
        }

        sqlResource = sqlResource.WithLifetime(lifetime);

        // RunAsEmulator() can be followed by custom model configuration so we need to delay the creation of the Config.json file
        // until all resources are about to be prepared and annotations can't be updated anymore.
        builder.ApplicationBuilder.Eventing.Subscribe<BeforeStartEvent>(async (@event, ct) =>
        {
            // Create JSON configuration file
            var hasCustomConfigJson = builder.Resource.Annotations.OfType<ContainerMountAnnotation>()
                .Any(v => v.Target == EmulatorConfigJsonPath);

            if (hasCustomConfigJson)
            {
                await Task.CompletedTask;
                return;
            }

            // Create Config.json file content and its alterations in a temporary file
            var tempConfigFile = WriteEmulatorConfigJson(builder.Resource);

            try
            {
                // Apply ConfigJsonAnnotation modifications
                var configJsonAnnotations = builder.Resource.Annotations
                    .Where(a => a.GetType().Name == "ConfigJsonAnnotation").ToList();

                if (configJsonAnnotations.Any())
                {
                    await using var readStream = new FileStream(tempConfigFile, FileMode.Open, FileAccess.Read);
                    var jsonObject = await JsonNode.ParseAsync(readStream, cancellationToken: ct);
                    readStream.Close();

                    if (jsonObject == null)
                    {
                        throw new InvalidOperationException("The configuration file mount could not be parsed.");
                    }

                    foreach (var annotation in configJsonAnnotations)
                    {
                        var configureProperty = annotation.GetType().GetProperty("Configure")!;
                        if (configureProperty.GetValue(annotation) is Action<JsonNode> property)
                        {
                            property.Invoke(jsonObject);
                        }
                    }

                    await using var writeStream = new FileStream(tempConfigFile, FileMode.Open, FileAccess.Write);
                    await using var writer = new Utf8JsonWriter(writeStream, new JsonWriterOptions { Indented = true });
                    jsonObject.WriteTo(writer);
                }

                var aspireStore = @event.Services.GetRequiredService<IAspireStore>();

                // Deterministic file path for the configuration file based on its content
                var configJsonPath =
                    aspireStore.GetFileNameWithContent($"{builder.Resource.Name}-Config.json", tempConfigFile);

                // The docker container runs as a non-root user, so we need to grant other user's read/write permission
                if (!OperatingSystem.IsWindows())
                {
                    File.SetUnixFileMode(configJsonPath, FileMode644);
                }

                builder.WithAnnotation(new ContainerMountAnnotation(
                    configJsonPath,
                    EmulatorConfigJsonPath,
                    ContainerMountType.BindMount,
                    isReadOnly: true));
            }
            finally
            {
                try
                {
                    File.Delete(tempConfigFile);
                }
                catch
                {
                    // ignored
                }
            }

            await Task.CompletedTask;
        });

        builder.WithHttpHealthCheck(endpointName: EmulatorHealthEndpointName, path: "/health");

        return builder;
    }

    private static string WriteEmulatorConfigJson(AzureServiceBusResource emulatorResource)
    {
        // This temporary file is not used by the container, it will be copied and then deleted
        var filePath = Path.GetTempFileName();

        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Write);
        using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });

        writer.WriteStartObject();
        writer.WriteStartObject("UserConfig");
        writer.WriteStartArray("Namespaces");
        writer.WriteStartObject();
        writer.WriteString("Name", emulatorResource.Name);
        writer.WriteStartArray("Queues");

        var queuesProperty = emulatorResource.GetType()
            .GetProperty("Queues", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var queues = (List<AzureServiceBusQueueResource>)queuesProperty.GetValue(emulatorResource)!;
        foreach (var queue in queues)
        {
            writer.WriteStartObject();
            InvokeWriteJsonObjectProperties(queue, writer);

            // queue.WriteJsonObjectProperties(writer);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();

        writer.WriteStartArray("Topics");
        var topicsProperty = emulatorResource.GetType()
            .GetProperty("Topics", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var topics = (List<AzureServiceBusTopicResource>)topicsProperty.GetValue(emulatorResource)!;
        foreach (var topic in topics)
        {
            writer.WriteStartObject();
            InvokeWriteJsonObjectProperties(topic, writer);
            writer.WriteStartArray("Subscriptions");

            var subscriptionsProperty =
                topic.GetType().GetProperty("Subscriptions", BindingFlags.NonPublic | BindingFlags.Instance)!;
            var subscriptions = (List<AzureServiceBusSubscriptionResource>)subscriptionsProperty.GetValue(topic)!;
            foreach (var subscription in subscriptions)
            {
                writer.WriteStartObject();
                InvokeWriteJsonObjectProperties(subscription, writer);
                writer.WriteStartArray("Rules");

                foreach (var rule in subscription.Rules)
                {
                    writer.WriteStartObject();
                    InvokeWriteJsonObjectProperties(rule, writer);
                    writer.WriteEndObject();
                }

                writer.WriteEndArray();

                writer.WriteEndObject();
            }

            writer.WriteEndArray();

            writer.WriteEndObject();
        }

        writer.WriteEndArray();

        writer.WriteEndObject();
        writer.WriteEndArray();
        writer.WriteStartObject("Logging");
        writer.WriteString("Type", "File");
        writer.WriteEndObject();

        writer.WriteEndObject();
        writer.WriteEndObject();

        return filePath;
    }

    private static void InvokeWriteJsonObjectProperties(object topic, Utf8JsonWriter writer)
    {
        var writeJsonObjectProperties = topic.GetType()
            .GetMethod("WriteJsonObjectProperties", BindingFlags.NonPublic | BindingFlags.Instance)!;
        writeJsonObjectProperties.Invoke(topic, [writer]);
    }
}
