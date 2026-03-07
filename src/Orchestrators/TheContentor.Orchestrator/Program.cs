using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RestSharp.Extensions.DependencyInjection;
using TheContentor.Orchestrator.Options;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.AddServiceDefaults();
builder.AddAzureServiceBusClient("ContentorServiceBus");

builder.Services.Configure<ApiOptions>(options =>
{
    options.BaseUrl = builder.Configuration["TheContentorApiUrl"] ?? string.Empty;
});

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights()
    .AddDurableTaskClient(o => { })
    .AddRestClient(r => r.Timeout = TimeSpan.FromMinutes(10));

builder.Services.ConfigureHttpClientDefaults(b => b.ConfigureHttpClient(c => c.Timeout = TimeSpan.FromMinutes(10)));

builder.Build().Run();