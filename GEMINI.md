# TheContentor Project

## Project Overview

**TheContentor** is an automated content creation engine designed to scrape web content, analyze it for engagement potential using AI, and transform it into high-quality videos with voiceovers and synchronized subtitles.

The project uses a microservices-style architecture and is composed of a .NET backend and several Python workers for specialized tasks.

**Core Technologies:**

*   **Frontend/API:** ASP.NET Core & Blazor
*   **Orchestration:** .NET Aspire, Azure Durable Functions
*   **Database:** PostgreSQL (EF Core Code-First)
*   **LLM Integration:** Gemini API, ChatGPT API
*   **Workers:** Python scripts for:
    *   **Voice / TTS:** Edge-TTS, Tortoise-TTS, Bark
    *   **Subtitles:** OpenAI Whisper
    *   **Video Processing:** MoviePy / FFmpeg
*   **File Storage:** Azure Blob Storage Emulator
*   **Messaging:** Azure Service Bus Emulator

## Building and Running the Project

The project is orchestrated using .NET Aspire. The main entry point for running the application is the `TheContentor.Aspire` project.

To run the project, you will need:
* .NET 8 SDK
* Python 3.9+
* Docker

To start all services, run the `TheContentor.Aspire` project. This will:
1.  Start a PostgreSQL container.
2.  Start an Azure Storage Emulator container for blob storage.
3.  Start an Azure Service Bus Emulator container for messaging.
4.  Build and run the `TheContentor.API` project.
5.  Build and run the `TheContentor.Orchestrator` Azure Functions project.
6.  Start the Python workers for TTS, video, and subtitles.

```bash
# Navigate to the Aspire project directory
cd src/Tools/TheContentor.Aspire

# Run the Aspire host
dotnet run
```

## Development Conventions

*   **Clean Architecture:** The project follows the principles of Clean Architecture, separating concerns into `Domain`, `Application`, `Infrastructure`, and `API` layers.
*   **CQRS:** The application logic uses the Command Query Responsibility Segregation (CQRS) pattern.
*   **Asynchronous Communication:** Services communicate asynchronously via an emulated Azure Service Bus.
*   **Documentation:** The project is well-documented in markdown files in the root directory. `README.md` provides a high-level overview, and other files like `VIDEO_GENERATION_IMPLEMENTATION.md` provide detailed information about specific features.
*   **Python Workers:** Python scripts for specialized tasks are located in the `src/Modules` directory. Each module has its own `requirements.txt`.
