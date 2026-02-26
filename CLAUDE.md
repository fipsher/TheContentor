# TheContentor

Automated video generation platform: scrape content → AI analysis → TTS → subtitles → video composition.

## Commands

```bash
# Build
dotnet build TheContentor.sln

# Run (starts all services via Aspire)
dotnet run --project src/Tools/TheContentor.Aspire

# Run tests
dotnet test                                                    # All C# tests
dotnet test test/Application/TheContentor.Application.Tests/   # Application tests only
cd src/Modules/Video && pytest test_video_worker.py -v         # Video worker tests
cd src/Modules/Subtitle && pytest test_subtitle_worker.py -v   # Subtitle worker tests

# EF Core migrations
dotnet ef migrations add <Name> --project src/Infrastructure/TheContentor.Infrastructure --startup-project src/API/TheContentor.API
dotnet ef database update --project src/Infrastructure/TheContentor.Infrastructure --startup-project src/API/TheContentor.API
```

## Architecture

Clean Architecture with CQRS (MediatR). .NET 10.0, Blazor frontend, PostgreSQL.

```
src/
├── Domain/          # Entities, enums, base classes
├── Application/     # Commands, queries, models (MediatR handlers)
├── Infrastructure/  # EF Core DbContext, migrations
├── API/             # Blazor UI + REST controllers (/api/ prefix)
├── Orchestrators/   # Azure Durable Functions (video pipeline)
├── Modules/         # Python workers (TTS, Video, Subtitle)
└── Tools/           # Aspire host, console app, TTS tool
```

**Orchestration flow:** `TtsOrchestrator` → `VideoOrchestrator` → `GenerateAllOrchestrator` (bulk). Triggered via `trigger-orchestration-queue`; workers send callbacks to `events-queue` which the `EventHandler` Function relays back to orchestrations. Real-time UI updates via SignalR at `/hubs/video-generation`.

## Code Conventions

- Inject `TheContentorDbContext` directly — no repository pattern
- All API routes start with `/api/`
- Entity relationships on ONE side only (avoids duplicate FKs)
- `IsActive` only via Activate/Deactivate/Toggle commands, never in Create/Update
- Use FluentValidation, AutoMapper, async/await throughout
- File-scoped namespaces, `var` over explicit types, no `dynamic`
- XML comments required (WarningsAsErrors 1591) — keep minimal and meaningful
- Application models go in `Features/<Feature>/Models/` next to Commands/Queries
- Video assets use streams (can be very large)

## Key Entities

SourcePost → ProcessedPost → ProcessedPostPart (audio/subtitle/video segments)
Asset (background videos), AnalysisCriteria, VideoProject, BlobPath

## File Storage

Local file system at `storage/` (configurable via `LocalStorage:BasePath`). Container names = subdirectories (e.g. `storage/assets/`, `storage/tts-audio/`). Served to the browser via static file middleware at `/storage/...`. Both .NET and Python workers read/write files directly — no Azure Blob Storage. Uploaded files get a UUID appended: `video.mp4` → `video-{uuid}.mp4`.

## Python Workers

Located in `src/Modules/`. Listen on Azure Service Bus, send status via callback events. Read/write files directly to local storage via `STORAGE_BASE_PATH` env var.
- **TTS**: EdgeTTS (primary), Kokoro (alternative); `tts-preview.py` is a separate HTTP server (port 8765) for on-demand previews at `/tts-playground`
- **Subtitle**: OpenAI Whisper (`base` model by default — change in `subtitle-worker.py` to trade accuracy vs. speed)
- **Video**: FFmpeg (direct subprocess) + Pillow for watermarks; code is a package at `src/Modules/Video/video/`

**Service Bus queues:** input — `tts-commands-queue`, `video-commands-queue`, `subtitle-commands-queue`; output — `events-queue` (all workers).
**Python env vars:** `STORAGE_BASE_PATH`, `ConnectionStrings__ContentorServiceBus` (Aspire sets these automatically; use `SERVICE_BUS_CONNECTION_STRING` for manual runs).

Requirements: `pip install -r requirements.txt` in each module dir. FFmpeg must be installed (`brew install ffmpeg`).

## Testing

- C#: xUnit + Moq + FluentAssertions
- Python: pytest (mark slow tests with `@pytest.mark.slow`)
- Integrity: `./verify_workflow.sh` (48 checks)

## Setup

Requires: .NET 10.0 SDK, Python 3.9+, Docker, FFmpeg.

```bash
dotnet user-secrets set "LLM:Gemini:ApiKey" "<key>" --project src/API/TheContentor.API
dotnet user-secrets set "LLM:ChatGPT:ApiKey" "<key>" --project src/API/TheContentor.API
```

## Gotchas

- **DB migrations run automatically** on API startup — no manual `ef database update` needed in dev.
- **FFmpeg hardware acceleration**: Video worker detects `h264_videotoolbox` on macOS automatically; falls back to `libx264`.
- **Whisper cold start**: Subtitle worker loads the Whisper model at process start — first boot is slow.
- **LLM providers**: Both `Gemini` and `ChatGPT` are wired up; set the key for whichever you use (or both).
