# Video Generation Implementation - Summary & Test Plan

## 🎯 Implementation Overview

This document summarizes the video generation feature implementation for TheContentor, including architecture, components created, and testing procedures.

## ✅ Completed Components

### 1. Domain Layer
- ✅ **VideoStatus enum** - `src/Domain/TheContentor.Domain/Enums/VideoStatus.cs`
  - States: NotGenerated, InProgress, Generated, Failed
- ✅ **ProcessedPost entity** - Updated with video fields:
  - `VideoStatus VideoStatus`
  - `BlobPath? VideoBlobPath`
  - `string? VideoSettings`
- ✅ **ProcessedPostPart entity** - Updated with:
  - `BlobPath? VideoBlobPath`
  - `BlobPath? SubtitleBlobPath`
- ✅ **EF Core Configurations** - Updated `ProcessedPostConfiguration` and `ProcessedPostPartConfiguration`
- ✅ **Database Migration** - Created migration "Add video generation support"

### 2. Application Layer
- ✅ **GenerateVideoCommand** - `src/Application/.../Commands/GenerateVideoCommand.cs`
  - Validates TTS is completed before video generation
  - Queues video orchestration job
- ✅ **UpdateVideoStatusCommand** - Updates video status from orchestrator
- ✅ **VideoSettingsModel** - Contains selected asset IDs
- ✅ **DTOs Updated**:
  - `SourcePostDetailsDto` - Added VideoStatus, VideoBlobPath, VideoSettings
  - `ProcessedPostPartDto` - Added VideoBlobPath, SubtitleBlobPath
  - `GetSourcePostDetailsQuery` - Maps video fields and generates SAS URLs

### 3. API Layer
- ✅ **ProcessedPostController**:
  - `PUT /api/ProcessedPost/video-status` - Callback endpoint for orchestrator
  - `UpdateVideoStatusRequest` model
- ✅ **BlobController**:
  - `GET /api/Blob/download` - Download blobs for Python workers
  - `POST /api/Blob/upload` - Already existed
- ✅ **IBlobService** - Added `DownloadAsync` method
- ✅ **BlobService** - Implemented download functionality

### 4. UI Layer (Blazor)
- ✅ **VideoGenerationDialog.razor** - `src/API/.../SourcePosts/VideoGenerationDialog.razor`
  - Multi-select asset picker with search
  - Shows asset duration and tags
  - Loads active assets only
- ✅ **SourcePostDetails.razor** - Updated with:
  - "Generate Video" button
  - Video status badge
  - Button enabled only after TTS completion
  - Integrated VideoGenerationDialog

### 5. Orchestrator Layer
- ✅ **VideoOrchestrator** - `src/Orchestrators/TheContentor.Orchestrator/Function.cs`
  - Main orchestration function with callback handling
  - Sequential pipeline: concat/cut → subtitles → compose
- ✅ **Activities**:
  - `FetchVideoGenerationData` - Retrieves parts, audio, assets
  - `SendVideoConcatCommand` - Triggers video concatenation
  - `SendSubtitleGenerationCommand` - Triggers subtitle generation
  - `SendVideoComposeCommand` - Triggers final composition
  - `UpdateProcessedPostVideoStatus` - Updates database
- ✅ **EventHandler** - Enhanced to route video callbacks
- ✅ **Models** (`src/Orchestrators/.../Models/Video/`):
  - `VideoOrchestratorRequest`
  - `VideoOrchestrationState`
  - `VideoGenerationData` / `VideoPartData` / `AssetData`
  - `VideoCommandMessage`
  - `VideoEventCallback`

### 6. Python Workers

#### Video Worker - `src/Modules/Video/video-worker.py`
- ✅ Listens to: `video-commands-queue`
- ✅ Commands:
  - **concat-cut**: Concatenates assets, cuts to audio duration (MoviePy)
  - **compose**: Combines video + audio + subtitles
- ✅ Features:
  - Downloads blobs from API
  - Uploads results to blob storage
  - Sends callbacks to orchestrator

#### Subtitle Worker - `src/Modules/Subtitle/subtitle-worker.py`
- ✅ Listens to: `subtitle-commands-queue`
- ✅ Uses OpenAI Whisper for transcription
- ✅ Generates SRT files with word-level timestamps
- ✅ Word-by-word timing for highlighting effect

### 7. Infrastructure (Aspire)
- ✅ **AppHost.cs** - Updated with:
  - `video-commands-queue` - For video processing
  - `subtitle-commands-queue` - For subtitle generation
  - `video-worker` Python app registration
  - `subtitle-worker` Python app registration
  - Proper service dependencies and environment variables

### 8. Dependencies
- ✅ **Video Worker** - `requirements.txt`:
  - azure-servicebus==7.12.0
  - aiohttp==3.9.1
  - moviepy==1.0.3
- ✅ **Subtitle Worker** - `requirements.txt`:
  - azure-servicebus==7.12.0
  - aiohttp==3.9.1
  - openai-whisper==20231117

## 🏗️ Architecture Flow

```
┌─────────────────────────────────────────────────────────────────┐
│ User clicks "Generate Video" on SourcePostDetails page          │
└──────────────────────┬──────────────────────────────────────────┘
                       ↓
┌─────────────────────────────────────────────────────────────────┐
│ VideoGenerationDialog opens → User selects background assets    │
└──────────────────────┬──────────────────────────────────────────┘
                       ↓
┌─────────────────────────────────────────────────────────────────┐
│ GenerateVideoCommand → VideoStatus = InProgress                 │
│ Message sent to trigger-orchestration-queue                     │
└──────────────────────┬──────────────────────────────────────────┘
                       ↓
┌─────────────────────────────────────────────────────────────────┐
│ VideoOrchestrator starts (Durable Function)                     │
│ 1. FetchVideoGenerationData                                     │
│ 2. For each part: Send concat-cut commands (parallel)           │
└──────────────────────┬──────────────────────────────────────────┘
                       ↓
┌─────────────────────────────────────────────────────────────────┐
│ video-worker.py processes concat-cut commands                   │
│ - Concatenates selected assets                                  │
│ - Cuts to match audio duration                                  │
│ - Uploads video to blob storage                                 │
│ - Sends callback to events-queue                                │
└──────────────────────┬──────────────────────────────────────────┘
                       ↓
┌─────────────────────────────────────────────────────────────────┐
│ Orchestrator receives concat-cut callbacks                      │
│ → Triggers subtitle-generation for each part                    │
└──────────────────────┬──────────────────────────────────────────┘
                       ↓
┌─────────────────────────────────────────────────────────────────┐
│ subtitle-worker.py processes subtitle commands                  │
│ - Uses Whisper to transcribe audio                              │
│ - Generates SRT with word-level timestamps                      │
│ - Uploads subtitle file to blob storage                         │
│ - Sends callback to events-queue                                │
└──────────────────────┬──────────────────────────────────────────┘
                       ↓
┌─────────────────────────────────────────────────────────────────┐
│ Orchestrator receives subtitle callbacks                        │
│ → Triggers compose for each part                                │
└──────────────────────┬──────────────────────────────────────────┘
                       ↓
┌─────────────────────────────────────────────────────────────────┐
│ video-worker.py processes compose commands                      │
│ - Combines background video + audio + subtitles                 │
│ - Renders final video with burned-in subtitles                  │
│ - Uploads final video to blob storage                           │
│ - Sends callback to events-queue                                │
└──────────────────────┬──────────────────────────────────────────┘
                       ↓
┌─────────────────────────────────────────────────────────────────┐
│ Orchestrator receives all compose callbacks                     │
│ → UpdateProcessedPostVideoStatus                                │
│ → VideoStatus = Generated (or Failed if errors)                 │
└──────────────────────┬──────────────────────────────────────────┘
                       ↓
┌─────────────────────────────────────────────────────────────────┐
│ UI refreshes → Video status badge updates                       │
│ → User can download/view generated videos                       │
└─────────────────────────────────────────────────────────────────┘
```

## 🧪 Testing Plan

### Prerequisites
1. **Start Infrastructure**:
   ```bash
   dotnet run --project src/Tools/TheContentor.Aspire
   ```
   This starts: PostgreSQL, Azure Storage Emulator, Service Bus Emulator

2. **Apply Database Migration**:
   ```bash
   dotnet ef database update --project src/Infrastructure/TheContentor.Infrastructure --startup-project src/API/TheContentor.API
   ```

3. **Install Python Dependencies**:
   ```bash
   # Video Worker
   cd src/Modules/Video
   pip install -r requirements.txt

   # Subtitle Worker
   cd ../Subtitle
   pip install -r requirements.txt
   ```

### Test Scenario 1: Basic Video Generation

#### Setup
1. Navigate to http://localhost:5000 (or your API port)
2. Ensure you have:
   - At least one processed source post with TTS generated
   - At least one active video asset in the Asset Library

#### Steps
1. **Navigate to Source Post**:
   - Go to "Source Posts" list
   - Click on a post with `Status = Processed` and `TTS Status = Generated`

2. **Verify UI State**:
   - ✅ "Generate TTS" button should be visible
   - ✅ "Generate Video" button should be visible and **enabled**
   - ✅ Video Status badge should show "NotGenerated"

3. **Initiate Video Generation**:
   - Click "Generate Video" button
   - VideoGenerationDialog should open

4. **Select Assets**:
   - ✅ Verify dialog shows list of active video assets
   - ✅ Each asset shows filename, duration (if available), and tags
   - Select 1-2 video assets
   - ✅ Verify "N asset(s) selected" message appears
   - Click "Generate" button

5. **Monitor Progress**:
   - ✅ Dialog should close
   - ✅ Video Status badge should change to "InProgress" (yellow)
   - ✅ "Generate Video" button should be **disabled**

6. **Check Logs**:
   - **Orchestrator logs**: Should show "Triggering Video orchestration"
   - **video-worker logs**: Should process concat-cut commands
   - **subtitle-worker logs**: Should process subtitle generation
   - **video-worker logs**: Should process compose commands

7. **Verify Completion**:
   - After processing completes (may take several minutes):
   - ✅ Video Status badge should change to "Generated" (green)
   - ✅ Each part should have video available for download

### Test Scenario 2: Error Handling

#### Test 2A: TTS Not Generated
1. Navigate to a post with `TTS Status = NotGenerated`
2. ✅ "Generate Video" button should be **disabled**
3. ✅ Tooltip should indicate TTS must be generated first

#### Test 2B: No Assets Selected
1. Click "Generate Video"
2. Don't select any assets
3. ✅ "Generate" button in dialog should be **disabled**

#### Test 2C: Missing Audio
1. Delete TTS audio from blob storage (manually)
2. Try to generate video
3. ✅ VideoStatus should change to "Failed"
4. ✅ Check orchestrator logs for error messages

### Test Scenario 3: Multiple Parts

#### Setup
Create a source post that generates 3+ parts when processed

#### Steps
1. Generate TTS for all parts
2. Generate video
3. ✅ Verify each part gets its own video file
4. ✅ Verify all parts complete successfully
5. ✅ Check that concat-cut commands run in parallel
6. ✅ Verify subtitle generation happens sequentially after each concat-cut

### Test Scenario 4: Blob Operations

#### Test 4A: Upload
1. Generate a video
2. Check blob storage containers:
   - ✅ `generated-videos` - Should contain cut background videos
   - ✅ `subtitles` - Should contain .srt files
   - ✅ `final-videos` - Should contain final composed videos

#### Test 4B: Download
1. Use API directly:
   ```bash
   curl "http://localhost:5000/api/Blob/download?containerName=final-videos&blobPath=<path>" -o test.mp4
   ```
2. ✅ Video file should download successfully
3. ✅ Play video to verify audio + subtitles

### Test Scenario 5: Service Bus Messages

#### Monitor Queues
1. Check Service Bus queues (via Aspire dashboard or Service Bus Explorer):
   - ✅ `trigger-orchestration-queue` - Receives video-generation message
   - ✅ `video-commands-queue` - Receives concat-cut and compose commands
   - ✅ `subtitle-commands-queue` - Receives subtitle generation commands
   - ✅ `events-queue` - Receives callbacks from workers

2. Verify message format:
   ```json
   // trigger-orchestration-queue
   {
     "Type": "video-generation",
     "ProcessedPostId": "guid",
     "Settings": {
       "AssetIds": ["guid1", "guid2"]
     }
   }
   ```

### Test Scenario 6: Performance

#### Metrics to Monitor
1. **Video Processing Time**:
   - Concat/Cut: ~30-60 seconds per part
   - Subtitle Generation: ~10-30 seconds per part
   - Compose: ~30-60 seconds per part
   - **Total**: ~2-3 minutes per part

2. **Resource Usage**:
   - ✅ Python workers should not leak memory
   - ✅ Temp files should be cleaned up
   - ✅ Blob storage should not accumulate orphaned files

## 🐛 Known Issues & Limitations

### TODO Items
1. **Audio Duration**: Currently hardcoded to 30 seconds
   - Need to fetch actual duration from blob metadata or store in DB

2. **Subtitle Rendering**: Basic subtitle overlay
   - Need word-level highlighting effect
   - Consider custom subtitle rendering with colors/animations

3. **Error Recovery**: Limited retry logic
   - Add retry policies for failed operations
   - Implement compensation logic for partial failures

4. **Video Quality**: Using default settings
   - Make codec, bitrate, fps configurable
   - Add quality presets (low/medium/high)

5. **Progress Tracking**: Binary status (InProgress/Generated)
   - Consider percentage-based progress
   - Show which step is currently running

6. **Asset Validation**: No format/codec validation
   - Verify assets are compatible before processing
   - Provide better error messages for incompatible files

## 📝 API Endpoints Reference

### Video Generation
- **POST** `/api/ProcessedPost/{id}/generate-video`
  - Body: `{ "assetIds": ["guid1", "guid2"] }`
  - Response: 202 Accepted

### Status Update (Internal - Orchestrator)
- **PUT** `/api/ProcessedPost/video-status`
  - Body:
    ```json
    {
      "processedPostId": "guid",
      "status": 3,
      "partVideoBlobPaths": {
        "part-guid": {
          "containerName": "final-videos",
          "assetPath": "video.mp4"
        }
      }
    }
    ```

### Blob Operations
- **POST** `/api/Blob/upload` - Upload file
- **GET** `/api/Blob/download?containerName=X&blobPath=Y` - Download file

## 🔧 Troubleshooting

### Video Worker Not Processing
- Check `video-commands-queue` has messages
- Verify Python dependencies installed
- Check worker logs for errors
- Ensure API URL environment variable is set

### Subtitle Worker Not Processing
- Verify Whisper model downloaded (happens on first run)
- Check `subtitle-commands-queue` has messages
- Ensure sufficient disk space for Whisper models (~1GB)

### Orchestrator Not Triggering
- Check `trigger-orchestration-queue` received message
- Verify orchestrator function is running
- Check durable function storage (Azure Storage emulator)

### Blob Upload/Download Failures
- Ensure Azure Storage Emulator is running
- Check blob container names match
- Verify SAS token generation

## ✨ Next Steps

1. **Testing**: Run through all test scenarios
2. **Bug Fixes**: Address any issues found during testing
3. **Performance Tuning**: Optimize video processing times
4. **UI Enhancements**: Add progress indicators
5. **Documentation**: Create user guide for video generation
6. **Production Ready**: Add monitoring, alerting, logging
