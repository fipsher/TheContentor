using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http;
using RestSharp.Extensions.DependencyInjection;
using TheContentor.Infrastructure.Interfaces;
using TheContentor.Infrastructure.Mappings;
using TheContentor.Infrastructure.Options;
using TheContentor.Infrastructure.Scrappers.Reddit;
using TheContentor.Infrastructure.Scrappers.Reddit.Models;
using TheContentor.Infrastructure.Scrappers.Shared;
using TheContentor.Infrastructure.Services;
using YoutubeDLSharp;
using YoutubeExplode;

namespace TheContentor.Infrastructure;

public static class DependencyInjection
{
    public static IHostApplicationBuilder AddInfrastructureServices(
        this IHostApplicationBuilder builder,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("the-contentor-db");

        builder.Services.AddDbContext<TheContentorDbContext>(options =>
            options.UseNpgsql(connectionString));

        builder.Services.Configure<LlmOptions>(configuration.GetSection(LlmOptions.SectionName));

        builder.Services.Configure<LocalStorageOptions>(configuration.GetSection(LocalStorageOptions.SectionName));
        builder.AddAzureServiceBusClient("ContentorServiceBus");

        builder.AddOllamaApiClient("ollama").AddChatClient();
        builder.Services.PostConfigure<HttpClientFactoryOptions>("httpClientKey", options =>
        {
            options.HttpClientActions.Add(client =>
            {
                // This will run after the library's configuration
                client.Timeout = TimeSpan.FromMinutes(10);
            });
        });

        builder.Services.AddScoped<IBlobService, LocalFileSystemBlobService>();
        builder.Services.AddScoped<IPostProcessor, PostProcessor>();
        builder.Services.AddScoped<YoutubeClient>(_ => new YoutubeClient());
        builder.Services.AddScoped<YoutubeDL>(_ => new YoutubeDL
        {
            YoutubeDLPath = "/opt/homebrew/bin/yt-dlp",
            FFmpegPath = "/opt/homebrew/bin/ffmpeg",
            OutputFolder = AppDomain.CurrentDomain.BaseDirectory,
        });
        builder.Services.AddScoped<IYouTubeService, YouTubeService>();

        builder.Services.AddRestClient();
        builder.Services.AddScoped<ISourceScraper<RedditPost, RedditScrapperRequest>, RedditScrapper>();

        builder.Services.AddAutoMapper(cfg => cfg.AddProfile<RedditMappingProfile>());

        return builder;
    }

    public static async Task ApplyMigrations(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TheContentorDbContext>();
        await context.Database.MigrateAsync();
    }
}