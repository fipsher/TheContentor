using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using TheContentor.API.Components;
using TheContentor.API.Hubs;
using TheContentor.Application;
using TheContentor.Infrastructure;
using Xabe.FFmpeg;

var builder = WebApplication.CreateBuilder(args);

// Configure FFmpeg path
var ffmpegPath = builder.Configuration["FFmpegPath"];
if (string.IsNullOrEmpty(ffmpegPath) && OperatingSystem.IsMacOS())
{
    if (Directory.Exists("/opt/homebrew/bin"))
    {
        ffmpegPath = "/opt/homebrew/bin";
    }
    else if (Directory.Exists("/usr/local/bin"))
    {
        ffmpegPath = "/usr/local/bin";
    }
}

if (!string.IsNullOrEmpty(ffmpegPath))
{
    FFmpeg.SetExecutablesPath(ffmpegPath);
}

// Add services to the container.
builder.Services.AddHttpClient();
builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(options =>
    {
        options.DetailedErrors = builder.Environment.IsDevelopment();
    })
    .AddHubOptions(options =>
    {
        options.MaximumReceiveMessageSize = 512 * 1024 * 1024; // 512MB
    });

builder.Services.AddApplicationServices();
builder.AddInfrastructureServices(builder.Configuration);

var app = builder.Build();

// Apply migrations on startup
await app.Services.ApplyMigrations();

// Serve local blob storage files at /storage
var storagePath = app.Configuration["LocalStorage:BasePath"] ?? "./storage";
var storageFullPath = Path.GetFullPath(storagePath);
Directory.CreateDirectory(storageFullPath);

var contentTypeProvider = new FileExtensionContentTypeProvider(
    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { ".mp4", "video/mp4" },
        { ".mp3", "audio/mpeg" },
        { ".wav", "audio/wav" },
        { ".srt", "application/x-subrip" },
        { ".vtt", "text/vtt" },
        { ".mov", "video/quicktime" },
        { ".webm", "video/webm" },
        { ".ogg", "audio/ogg" }
    });

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(storageFullPath),
    RequestPath = "/storage",
    ContentTypeProvider = contentTypeProvider,
    ServeUnknownFileTypes = false
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapControllers();
app.MapHub<VideoGenerationHub>("/hubs/video-generation");
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
