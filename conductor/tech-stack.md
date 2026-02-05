# Tech Stack

## Overview
TheContentor leverages a diverse and modern technology stack to achieve its automated content creation goals, focusing on a hybrid approach of .NET for core services and Python for specialized media processing tasks.

## Core Components

| Component        | Technology                            | Description                                                                                             |
|------------------|---------------------------------------|---------------------------------------------------------------------------------------------------------|
| **Frontend/API** | ASP.NET Core & Blazor                 | Provides the user interface and the primary API endpoints for interaction with the system.              |
| **Orchestration**| Azure Durable Functions (via .NET Aspire) | Manages complex, long-running workflows and coordinates tasks across various services and workers.     |
| **Database**     | PostgreSQL (EF Core Code-First)       | Relational database for storing application data, managed through Entity Framework Core migrations.     |
| **LLM Integration**| Gemini API, ChatGPT API               | Utilized for advanced content analysis, natural language processing, and engagement potential scoring.|
| **Voice / TTS**  | Edge-TTS (Primary), Tortoise-TTS, Bark| Text-to-Speech engines for generating high-quality voiceovers. Edge-TTS is primary for efficiency, with others available for expressive needs. |
| **Subtitles**    | OpenAI Whisper                        | State-of-the-art speech-to-text model used for accurate transcription and subtitle generation.          |
| **Video Processing**| MoviePy / FFmpeg                     | Libraries and tools for video editing, concatenation, cutting, and overlaying subtitles and audio.      |
| **File Storage** | Azure Blob Storage Emulator via Aspire| Used for storing large binary objects like raw scraped content, video assets, and generated media.      |
| **Environment**  | Docker / On-Premise                   | The entire system is containerized with Docker, facilitating on-premise deployment and local development. |
