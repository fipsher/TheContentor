# Quick Start: Video Generation Feature

## 🚀 Getting Started in 5 Minutes

### Step 1: Start the Application
```bash
# From project root
dotnet run --project src/Tools/TheContentor.Aspire
```

This will start:
- ✅ PostgreSQL database
- ✅ Azure Storage Emulator (for blobs)
- ✅ Service Bus Emulator (for queues)
- ✅ TheContentor API
- ✅ Orchestrator (Azure Functions)
- ✅ TTS Worker (Python)
- ✅ Video Worker (Python)
- ✅ Subtitle Worker (Python)

### Step 2: Apply Database Migration

In a new terminal:
```bash
dotnet ef database update --project src/Infrastructure/TheContentor.Infrastructure --startup-project src/API/TheContentor.API
```

### Step 3: Install Python Dependencies

```bash
# Video Worker
cd src/Modules/Video
pip install -r requirements.txt

# Subtitle Worker
cd ../Subtitle
pip install -r requirements.txt

# TTS Worker (if not already done)
cd ../TTS
pip install -r requirements.txt
```

### Step 4: Upload Video Assets

1. Open the web UI (typically http://localhost:5000)
2. Navigate to **Assets** → **Upload Asset**
3. Upload 1-2 video files (MP4 recommended)
4. Set asset as **Active**

### Step 5: Process a Source Post

1. Navigate to **Source Posts**
2. Select a post and click **Process with AI**
3. Wait for processing to complete
4. Click **Generate TTS** and wait for completion

### Step 6: Generate Video! 🎬

1. On the same source post details page
2. Click **Generate Video** button
3. Select one or more video assets
4. Click **Generate**
5. Monitor the status badge:
   - 🟡 **InProgress** - Video is being generated
   - 🟢 **Generated** - Video is ready!
   - 🔴 **Failed** - Check logs for errors

### Step 7: View Results

Each part of the processed post will have:
- Background video (cut to match audio duration)
- Generated subtitles (word-level timing)
- Final composed video (background + audio + subtitles)

All files are stored in blob storage and accessible via SAS URLs.

## 📋 Prerequisites Checklist

### System Requirements
- [ ] .NET 10.0 SDK
- [ ] Python 3.9+
- [ ] Docker (for Aspire containers)
- [ ] At least 4GB RAM
- [ ] 5GB free disk space (for Whisper models)

### First-Time Setup
- [ ] Run `dotnet restore`
- [ ] Run `pip install -r requirements.txt` for each Python worker
- [ ] Ensure ports 5000, 5432, 10000-10002 are available
- [ ] Install FFmpeg (required by MoviePy): `brew install ffmpeg` (Mac) or equivalent

## 🔍 Monitoring & Debugging

### View Logs

**Aspire Dashboard** (opens automatically):
- Shows all services and their logs
- Monitor resource usage
- Check message queues

**Individual Services**:
```bash
# API Logs
# Check Aspire dashboard or run API directly

# Orchestrator Logs
# Check Azure Functions logs in Aspire dashboard

# Python Workers
# Logs appear in Aspire dashboard console
```

### Check Queue Status

In Aspire dashboard:
1. Navigate to **Service Bus** section
2. Check queue depths:
   - `trigger-orchestration-queue`
   - `video-commands-queue`
   - `subtitle-commands-queue`
   - `events-queue`

### Common Issues

**"Generate Video" button disabled**
- ✅ Ensure TTS Status = "Generated"
- ✅ Check that post has at least one part

**Video generation stuck in "InProgress"**
- ✅ Check Python worker logs for errors
- ✅ Verify blob storage is accessible
- ✅ Ensure queues have messages
- ✅ Check orchestrator is processing callbacks

**Python workers not starting**
- ✅ Install dependencies: `pip install -r requirements.txt`
- ✅ Check Python version: `python --version` (3.9+)
- ✅ Verify environment variables are set (handled by Aspire)

**FFmpeg errors in video-worker**
- ✅ Install FFmpeg: `brew install ffmpeg` (Mac)
- ✅ For Windows: Download from https://ffmpeg.org/
- ✅ For Linux: `sudo apt-get install ffmpeg`

**Whisper model download slow**
- ✅ First run downloads ~1GB Whisper model
- ✅ Be patient, subsequent runs are fast
- ✅ Check internet connection

## 📊 Expected Timeline

For a typical source post with 3 parts:

| Step | Duration | Notes |
|------|----------|-------|
| AI Processing | 30-60s | Depends on post length |
| TTS Generation | 1-2 min | ~20-30s per part |
| Video Concat/Cut | 1-2 min | ~30-45s per part |
| Subtitle Generation | 1-2 min | ~30-40s per part |
| Video Composition | 1-2 min | ~30-45s per part |
| **Total** | **5-8 min** | End-to-end |

*Note: Times vary based on content length and system performance*

## 🎯 What Gets Generated

For each part of your processed post:

1. **Background Video** (`generated-videos` container)
   - Concatenated from selected assets
   - Cut to exact audio duration
   - Format: MP4, 30fps

2. **Subtitles** (`subtitles` container)
   - Word-level timing from Whisper
   - Format: SRT (SubRip)
   - Ready for highlighting effects

3. **Final Video** (`final-videos` container)
   - Background video + TTS audio + subtitles
   - Format: MP4, H.264 + AAC
   - Ready for social media upload

## 🆘 Need Help?

### Documentation
- Full implementation details: `VIDEO_GENERATION_IMPLEMENTATION.md`
- Architecture overview: `README.md`
- Agent instructions: `AGENT_INSTRUCTIONS.md`

### Debugging Steps
1. Check Aspire dashboard for service status
2. Review logs for error messages
3. Verify database migration applied
4. Ensure all Python dependencies installed
5. Check blob storage containers exist
6. Verify Service Bus queues are created

### Testing the Flow
Run through Test Scenario 1 in `VIDEO_GENERATION_IMPLEMENTATION.md` for a complete walkthrough.

## 🎉 Success Indicators

You know everything is working when:
- ✅ All services show "Running" in Aspire dashboard
- ✅ Video Status changes from NotGenerated → InProgress → Generated
- ✅ No errors in Python worker logs
- ✅ Final video files appear in `final-videos` blob container
- ✅ Videos play correctly with audio and subtitles

Happy video generating! 🎬✨
