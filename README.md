🎬 TheContentor
===============

**TheContentor** is an automated content creation engine designed to scrape web content, analyze it for engagement potential using AI, and transform it into high-quality videos with voiceovers and synchronized subtitles.

> **Project Philosophy:** Prioritize open-source, on-premise solutions to minimize operational costs. Supports cloud LLMs (Gemini/ChatGPT) and local LLMs via Ollama for zero-cost, rate-limit-free processing.

* * *

🚀 Key Features
---------------

*   **Multi-Source Scraping:** Integrated Reddit API support with expandable module architecture.

*   **Content Processing:** Automatic content processing and analysis using Gemini API.
    
*   **Dynamic Video Synthesis:** Automatic merging of background footage, AI voiceovers, and dynamic subtitles.
    
*   **On-Premise Orchestration:** Powered by .NET Aspire and Azure Durable Functions running locally via Docker.

* * *

🛠 Tech Stack
-------------

| **Component**        | **Technology**                            |
|----------------------|-------------------------------------------|
| **Frontend/API**     | ASP.NET Core & Blazor                     |
| **Orchestration**    | Azure Durable Functions (via .NET Aspire) |
| **Database**         | PostgreSQL (EF Core Code-First)           |
| **LLM Integration**  | Gemini API, ChatGPT API, Local (Ollama)   |
| **Voice / TTS**      | Edge-TTS (Primary), Tortoise-TTS, Bark    |
| **Subtitles**        | OpenAI Whisper                            |
| **Video Processing** | MoviePy / FFmpeg                          |
| **File Storage**     | Azure Blob Storage Emulator via Aspire    |
| **Environment**      | Docker / On-Premise                       |

* * *

🔄 System Workflows
-------------------

### 1. Scraper Pipeline

1.  **Configuration:** User selects source (e.g., Reddit) and defines "Attractiveness Criteria."
    
2.  **Extraction:** System pulls data, applies source-specific deduplication, and stores raw content.
    
3.  **Analysis:** GenAI analyzes stories against criteria to rank high-potential content.
    
4.  **Review:** User interacts with a curated list of "approved" materials.
    

### 2. Video Compilation Pipeline

1.  **Settings:** User configures TTS (voice, speed), Subtitles (font, position), and Background Video.
    
2.  **Execution:** The orchestrator triggers the scraping-to-composition sequence.
    
3.  **Monitoring:** Real-time progress tracking via the Blazor dashboard.
    
4.  **Delivery:** Final video and individual assets (audio/subtitles) are available for download.
    

### 3. Library Management

*   **Criteria Library:** Manage predefined logic for content selection. Supports versioning via "snapshots" for default criteria.
    
*   **Asset Library:** CRUD operations for video assets. Includes an "Active/Inactive" toggle system instead of hard deletion to preserve pipeline history.


* * *

## ⚡ Local LLM (Ollama)

TheContentor supports local LLM inference via [Ollama](https://ollama.ai) as an alternative to cloud APIs — no rate limits, no API keys, runs fully on-device.

**Ollama starts automatically** when you run the Aspire host. The default model (`qwen2.5:14b`) is pulled on first start and cached for all subsequent runs.

### Recommended models

| Model | Size (Q4) | Speed (M2 Pro) | Best for |
|-------|-----------|----------------|----------|
| `qwen2.5:14b` ⭐ | ~9 GB | ~15 tok/s | Default — best JSON + instruction following |
| `qwen2.5:32b` | ~20 GB | ~7 tok/s | M3 Max / M4 Max — better creative quality |
| `gemma3:9b` | ~6 GB | ~20 tok/s | Faster, slightly weaker JSON adherence |
| `llama3.3:70b` | ~43 GB | ~4 tok/s | Maximum quality, requires 64 GB+ RAM |

### Usage

Select **Local (Ollama)** from the LLM Provider dropdown in the AI processing modal. Type the model name (e.g. `qwen2.5:14b`) — it defaults to the configured model but is editable per run.

### Apple Silicon note

Ollama in Docker does not use Metal GPU acceleration. For maximum speed on M-series Macs, install Ollama natively and set it to auto-start:

```bash
brew install ollama
brew services start ollama  # auto-starts at login
```

Then override the endpoint in user-secrets:
```bash
dotnet user-secrets set "LLM:Local:BaseUrl" "http://localhost:11434" --project src/API/TheContentor.API
```

* * *

📂 Project Structure
--------------------

Plaintext

    src/
    ├── Tools/
    │   └── TheContentor.Aspire          # Orchestration & local deployment
    ├── API/
    │   └── TheContentor.API             # Blazor UI & Web API
    ├── Application/
    │   └── TheContentor.Application     # Business logic & Orchestrators
    ├── Domain/
    │   └── TheContentor.Domain          # Entities & Domain objects
    ├── Infrastructure/
    │   └── TheContentor.Infrastructure  # DBContext & Data Persistence
    ├── Orchestrators/
    │   └── TheContentor.Orchestrator    # Calcualtes Asset metadata, Generate Video, etc.
    └── Modules/
        ├── Scraper/                     # Python-based scraping modules
        │   ├── Scraper.Abstract
        │   └── Scraper.Reddit           # PRAW Integration
        ├── TTS/                         # Text-to-Speech engines
        │   ├── TTS.Abstract
        │   ├── TTS.EdgeTTS
        │   └── TTS.Bark/Tortoise
        └── Subtitle/                    # Transcription & Alignment
            ├── Subtitle.Abstract
            └── Subtitle.Whisper

* * *

⚙️ Module Details
-----------------

### Scraper Modules

*   **Reddit:** Utilizes `PRAW` to fetch trending posts from specific subreddits.
    
*   **Scalability:** Abstracted to allow for future modules (Twitter/X, News RSS, etc.).
    

### Audio & Video

*   **TTS:** `Edge-TTS` provides high-quality, fast results. `Bark` and `Tortoise` are available for more expressive, high-compute local generation.
    
*   **Subtitles:** `OpenAI Whisper` ensures timestamps are perfectly aligned with generated audio.
    
*   **Composition:** `MoviePy` handles the heavy lifting of layering audio, video, and text overlays.
