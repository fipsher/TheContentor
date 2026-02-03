# TheContentor - Test Documentation

## Overview

This document describes the testing strategy, available tests, and how to run them for the TheContentor video generation system.

## Table of Contents

1. [Testing Strategy](#testing-strategy)
2. [Test Categories](#test-categories)
3. [Running Tests](#running-tests)
4. [Python Worker Tests](#python-worker-tests)
5. [Integration Testing](#integration-testing)
6. [Workflow Integrity Checks](#workflow-integrity-checks)
7. [CI/CD Integration](#cicd-integration)
8. [Test Coverage](#test-coverage)

---

## Testing Strategy

TheContentor uses a multi-layered testing approach:

- **Unit Tests**: Test individual functions and components in isolation
- **Integration Tests**: Test interactions between components (API ↔ Workers ↔ Orchestrator)
- **End-to-End Tests**: Test complete user workflows
- **Workflow Integrity Tests**: Verify message flow and state transitions

---

## Test Categories

### 1. Python Worker Unit Tests

#### Video Worker Tests (`src/Modules/Video/test_video_worker.py`)

**Test Classes:**

- **TestVideoConcatCut**: Video concatenation and cutting functionality
  - `test_concat_cut_basic`: Basic video concatenation and cutting to target duration
  - `test_concat_cut_invalid_duration`: Error handling for invalid durations

- **TestVideoCompose**: Final video composition with audio and subtitles
  - `test_compose_basic`: Composing video with audio and subtitles
  - `test_compose_missing_files`: Error handling for missing input files

- **TestCommandProcessing**: Service Bus command processing
  - `test_process_concat_cut_command`: Processing concat-cut commands
  - `test_process_compose_command`: Processing compose commands
  - `test_error_handling`: Error handling and callback generation

- **TestBlobOperations**: Blob upload/download operations
  - `test_upload_to_blob`: Uploading generated videos to blob storage

**Dependencies:**
```bash
cd src/Modules/Video
pip install -r requirements-test.txt
```

**Running Tests:**
```bash
# Run all tests
pytest test_video_worker.py -v

# Run specific test class
pytest test_video_worker.py::TestVideoConcatCut -v

# Run with coverage
pytest test_video_worker.py --cov=video_worker --cov-report=html
```

#### Subtitle Worker Tests (`src/Modules/Subtitle/test_subtitle_worker.py`)

**Test Classes:**

- **TestTimestampFormatting**: SRT timestamp formatting
  - `test_format_timestamp_basic`: Basic timestamp formatting (0s, 1s, 60s, 3600s)
  - `test_format_timestamp_with_milliseconds`: Millisecond precision
  - `test_format_timestamp_complex`: Complex timestamps (hours, minutes, seconds, ms)

- **TestSRTGeneration**: SRT subtitle file generation
  - `test_generate_srt_with_word_timing`: Word-level timing from Whisper
  - `test_generate_srt_without_word_timing`: Fallback to segment-level timing
  - `test_generate_srt_empty_segments`: Handling empty results
  - `test_generate_srt_whitespace_handling`: Trimming whitespace

- **TestSubtitleGeneration**: Whisper subtitle generation
  - `test_generate_subtitles_basic`: Basic subtitle generation flow
  - `test_generate_subtitles_error_handling`: Error handling for missing audio

- **TestCommandProcessing**: Service Bus command processing
  - `test_process_subtitle_command_success`: Successful command processing
  - `test_process_subtitle_command_error`: Error handling and callbacks
  - `test_process_subtitle_command_missing_fields`: Missing field validation

- **TestBlobOperations**: Blob operations
  - `test_upload_subtitle_file`: Uploading SRT files to blob storage

- **TestWhisperIntegration**: Real Whisper model tests (slow)
  - `test_whisper_real_audio`: Integration test with real audio (marked as slow)

- **TestSRTFileStructure**: SRT file format validation
  - `test_srt_file_format`: Validate SRT structure
  - `test_srt_multiple_segments`: Multiple subtitle entries

**Dependencies:**
```bash
cd src/Modules/Subtitle
pip install -r requirements-test.txt
```

**Running Tests:**
```bash
# Run fast tests only (no real audio required)
pytest test_subtitle_worker.py -v -m "not slow"

# Run all tests including slow Whisper integration tests
pytest test_subtitle_worker.py -v

# Run with coverage
pytest test_subtitle_worker.py --cov=subtitle_worker --cov-report=html -m "not slow"
```

### 2. C# Unit Tests (TODO)

Future tests for:
- Command handlers
- Query handlers
- Domain entities
- Orchestrator activities

---

## Running Tests

### Prerequisites

1. **Install Python Test Dependencies:**
   ```bash
   # Video Worker
   cd src/Modules/Video
   pip install -r requirements-test.txt

   # Subtitle Worker
   cd src/Modules/Subtitle
   pip install -r requirements-test.txt
   ```

2. **Create Test Assets (Optional for full integration tests):**
   - See `src/Modules/Video/test_assets/README.md`
   - See `src/Modules/Subtitle/test_assets/README.md`
   - Tests will skip if assets are missing

### Running All Python Tests

```bash
# From project root
cd src/Modules/Video && pytest test_video_worker.py -v
cd ../Subtitle && pytest test_subtitle_worker.py -v -m "not slow"
```

### Test Markers

- `@pytest.mark.slow`: Long-running tests (Whisper model, real video processing)
- `@pytest.mark.asyncio`: Async tests
- Use `-m "not slow"` to skip slow tests in development

### Generating Coverage Reports

```bash
# Video Worker
cd src/Modules/Video
pytest test_video_worker.py --cov=video_worker --cov-report=term --cov-report=html

# Subtitle Worker
cd src/Modules/Subtitle
pytest test_subtitle_worker.py --cov=subtitle_worker --cov-report=term --cov-report=html -m "not slow"

# View HTML report
open htmlcov/index.html
```

---

## Python Worker Tests

### Test Structure

Each worker has:
1. **Unit Tests**: Test individual functions with mocked dependencies
2. **Integration Tests**: Test with real libraries (MoviePy, Whisper) but mocked I/O
3. **Command Processing Tests**: Test Service Bus message handling
4. **Error Handling Tests**: Verify proper error callbacks

### Mocking Strategy

Tests use `unittest.mock` to mock:
- **Azure Service Bus**: Connection and message sending
- **Blob Storage**: Upload/download operations
- **API Calls**: HTTP requests to TheContentor API
- **File System**: For some tests to avoid requiring real files

### Test Data

Tests use:
- **Synthetic Whisper Results**: Mock transcription data
- **Minimal Test Commands**: Sample Service Bus messages
- **Generated SRT Content**: Programmatically created subtitle files

---

## Integration Testing

### Manual Integration Test Workflow

#### 1. Start Infrastructure
```bash
dotnet run --project src/Tools/TheContentor.Aspire
```

#### 2. Apply Migration
```bash
dotnet ef database update --project src/Infrastructure/TheContentor.Infrastructure --startup-project src/API/TheContentor.API
```

#### 3. Create Test Data
- Upload a video asset via UI
- Process a source post with AI
- Generate TTS for the post

#### 4. Trigger Video Generation
- Click "Generate Video" button
- Select test asset(s)
- Monitor logs in Aspire dashboard

#### 5. Verify Results
- Check video status changes: NotGenerated → InProgress → Generated
- Verify blob containers have files:
  - `generated-videos` - Cut background videos
  - `subtitles` - SRT files
  - `final-videos` - Composed videos

### Integration Test Checklist

- [ ] Video worker receives and processes concat-cut commands
- [ ] Subtitle worker receives and processes subtitle commands
- [ ] Video worker receives and processes compose commands
- [ ] Each worker sends success callbacks to events-queue
- [ ] Orchestrator processes callbacks in correct order
- [ ] Database updates with correct blob paths
- [ ] All blob files are accessible via SAS URLs
- [ ] Video status updates correctly in UI

---

## Workflow Integrity Checks

### Message Flow Verification

#### Expected Message Flow:
```
1. UI → GenerateVideoCommand
   ↓
2. API → trigger-orchestration-queue (Type: video-generation)
   ↓
3. Orchestrator → video-commands-queue (Type: concat-cut) [parallel for all parts]
   ↓
4. video-worker → events-queue (Success callback)
   ↓
5. Orchestrator → subtitle-commands-queue (Type: generate-subtitles)
   ↓
6. subtitle-worker → events-queue (Success callback)
   ↓
7. Orchestrator → video-commands-queue (Type: compose)
   ↓
8. video-worker → events-queue (Success callback)
   ↓
9. Orchestrator → UpdateProcessedPostVideoStatus
   ↓
10. Database updated, UI refreshes
```

### State Transition Verification

**VideoStatus States:**
```
NotGenerated → (User clicks Generate) → InProgress → (All parts complete) → Generated
                                                   ↘ (Any error) → Failed
```

### Integrity Checks

#### 1. Command Structure Validation

**GenerateVideoCommand:**
```json
{
  "Type": "video-generation",
  "ProcessedPostId": "guid",
  "Settings": {
    "AssetIds": ["guid1", "guid2"]
  }
}
```

**VideoCommandMessage (concat-cut):**
```json
{
  "CommandType": "concat-cut",
  "ProcessedPostId": "guid",
  "PartId": "guid",
  "OrchestrationInstanceId": "guid",
  "AssetBlobPaths": [
    {"ContainerName": "...", "AssetPath": "..."}
  ],
  "TargetDuration": "00:00:30"
}
```

**VideoEventCallback:**
```json
{
  "OrchestrationInstanceId": "guid",
  "ProcessedPostId": "guid",
  "PartId": "guid",
  "CommandType": "concat-cut",
  "BlobContainer": "generated-videos",
  "BlobPath": "video_part_xxx.mp4",
  "Success": true,
  "Duration": "00:00:30.000"
}
```

#### 2. Database Integrity

After successful video generation:
```sql
-- Check ProcessedPost
SELECT VideoStatus, VideoSettings, VideoBlobPath
FROM ProcessedPosts
WHERE Id = '<test-guid>';
-- Expected: VideoStatus = 3 (Generated)

-- Check ProcessedPostParts
SELECT Part, VideoBlobPath, SubtitleBlobPath
FROM ProcessedPostParts
WHERE ProcessedPostId = '<test-guid>';
-- Expected: All parts have VideoBlobPath and SubtitleBlobPath
```

#### 3. Blob Storage Integrity

Check containers:
```bash
# List blobs (via Aspire Storage Explorer or Azure CLI)
# generated-videos: video_part_{guid}.mp4
# subtitles: subtitles_part_{guid}.srt
# final-videos: final_video_part_{guid}.mp4
```

#### 4. Orchestrator State Integrity

Verify in orchestrator logs:
- `ExpectedCallbacks = parts.Count * 3`
- `ReceivedCallbacks` increments correctly
- No duplicate callback processing
- Proper error handling for failed callbacks

---

## CI/CD Integration

### GitHub Actions Example (TODO)

```yaml
name: Python Worker Tests

on: [push, pull_request]

jobs:
  test-python-workers:
    runs-on: ubuntu-latest

    steps:
    - uses: actions/checkout@v3

    - name: Set up Python
      uses: actions/setup-python@v4
      with:
        python-version: '3.10'

    - name: Install FFmpeg
      run: sudo apt-get install -y ffmpeg

    - name: Install Video Worker Dependencies
      run: |
        cd src/Modules/Video
        pip install -r requirements-test.txt

    - name: Run Video Worker Tests
      run: |
        cd src/Modules/Video
        pytest test_video_worker.py -v -m "not slow" --cov=video_worker

    - name: Install Subtitle Worker Dependencies
      run: |
        cd src/Modules/Subtitle
        pip install -r requirements-test.txt

    - name: Run Subtitle Worker Tests
      run: |
        cd src/Modules/Subtitle
        pytest test_subtitle_worker.py -v -m "not slow" --cov=subtitle_worker

    - name: Upload Coverage
      uses: codecov/codecov-action@v3
```

### Test Environments

- **Development**: Full tests with real assets (slow)
- **CI/CD**: Fast tests only (`-m "not slow"`)
- **Staging**: Integration tests with test data
- **Production**: Health checks and smoke tests

---

## Test Coverage

### Coverage Goals

- **Python Workers**:
  - Target: 80%+ line coverage
  - Critical paths: 95%+ coverage
  - Current: See coverage reports

- **C# Code** (TODO):
  - Target: 70%+ line coverage
  - Command/Query handlers: 90%+
  - Orchestrator activities: 85%+

### Viewing Coverage Reports

```bash
# Generate and view Python coverage
cd src/Modules/Video
pytest test_video_worker.py --cov=video_worker --cov-report=html
open htmlcov/index.html

cd ../Subtitle
pytest test_subtitle_worker.py --cov=subtitle_worker --cov-report=html -m "not slow"
open htmlcov/index.html
```

### Coverage Gaps (Known)

1. **Whisper Model Loading**: Hard to test without downloading ~1GB model
2. **Real Video Processing**: Requires actual video files
3. **Network I/O**: Mocked in most tests
4. **Durable Function Replay**: Complex to test orchestration replay logic

---

## Troubleshooting Tests

### Common Issues

**1. Import Errors**
```
ModuleNotFoundError: No module named 'video_worker'
```
**Solution**: Ensure you're in the correct directory and dependencies are installed:
```bash
cd src/Modules/Video
pip install -r requirements-test.txt
```

**2. Missing Test Assets**
```
pytest.skip: requires test video files
```
**Solution**: Either:
- Create test assets (see test_assets/README.md)
- Run with `-m "not slow"` to skip tests requiring assets

**3. FFmpeg Not Found**
```
FileNotFoundError: ffmpeg not found
```
**Solution**: Install FFmpeg:
- macOS: `brew install ffmpeg`
- Ubuntu: `sudo apt-get install ffmpeg`
- Windows: Download from https://ffmpeg.org/

**4. Whisper Model Download**
```
First run is downloading Whisper model...
```
**Solution**: This is normal on first run. The ~1GB model is cached for subsequent runs.

**5. Async Test Warnings**
```
PytestUnraisableExceptionWarning: Exception ignored
```
**Solution**: Ensure pytest-asyncio is installed and tests use `@pytest.mark.asyncio`

### Debug Mode

Run tests with verbose output:
```bash
pytest -vv -s test_video_worker.py::TestVideoConcatCut::test_concat_cut_basic
```

- `-vv`: Very verbose output
- `-s`: Show print statements
- Specify specific test to debug

---

## Future Test Enhancements

### Planned Tests

1. **C# Unit Tests**:
   - Command/Query handler tests
   - Domain entity validation tests
   - Orchestrator activity tests

2. **Load Tests**:
   - Concurrent video generation
   - Large file handling
   - Queue throughput testing

3. **Performance Tests**:
   - Video processing time benchmarks
   - Memory usage profiling
   - Disk I/O optimization

4. **Security Tests**:
   - Input validation
   - Blob access control
   - API authentication

5. **E2E Automation**:
   - Playwright/Selenium UI tests
   - Full workflow automation
   - Multi-user scenarios

### Test Metrics to Track

- Test execution time
- Coverage percentage
- Flaky test rate
- Bug detection rate
- Test maintenance cost

---

## Contributing Tests

When adding new features:

1. **Write tests first** (TDD approach)
2. **Maintain coverage** above target thresholds
3. **Mock external dependencies** for unit tests
4. **Add integration tests** for critical paths
5. **Document test purpose** in docstrings
6. **Use meaningful test names** (`test_<function>_<scenario>_<expected_result>`)

### Test Naming Convention

```python
def test_<function_name>_<scenario>_<expected_behavior>():
    """
    Test that <function> <does something> when <scenario>.

    Expected: <expected behavior>
    """
    # Arrange
    ...

    # Act
    ...

    # Assert
    ...
```

---

## Summary

✅ **Python Worker Tests**: Comprehensive unit and integration tests
✅ **Test Assets**: Documentation for creating test data
✅ **Coverage Reports**: HTML coverage reporting configured
✅ **CI/CD Ready**: Tests can run without real assets (`-m "not slow"`)
✅ **Workflow Integrity**: Message flow and state verification documented

**Next Steps**:
1. Create test assets for full integration testing
2. Add C# unit tests for API and orchestrator
3. Set up CI/CD pipeline with automated testing
4. Track coverage metrics over time

Happy Testing! 🧪✅
