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

## Python Workers

Located in `src/Modules/`. Listen on Azure Service Bus, send status via callback events.
- **TTS**: Edge-TTS (primary), Bark/Tortoise fallback
- **Subtitle**: OpenAI Whisper
- **Video**: MoviePy + FFmpeg composition

Requirements: `pip install -r requirements.txt` in each module dir. FFmpeg must be installed (`brew install ffmpeg`).

## Testing

- C#: xUnit + Moq + FluentAssertions
- Python: pytest (mark slow tests with `@pytest.mark.slow`)
- Integrity: `./verify_workflow.sh` (48 checks)

## Setup

Requires: .NET 10.0 SDK, Python 3.9+, Docker, FFmpeg.
Secrets: `dotnet user-secrets set "LLM:Gemini:ApiKey" "<key>" --project src/API/TheContentor.API`
