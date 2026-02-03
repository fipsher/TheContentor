# Test Assets for Subtitle Worker

## Required Test Files

To run full integration tests with real audio transcription, place the following files in this directory:

### Audio Files
- **sample_audio.mp3** - A short (5-10 second) audio clip with clear speech
- Content: Simple spoken sentence (e.g., "This is a test of the subtitle generation system")
- Format: MP3, 128kbps or higher
- Language: English (default for Whisper model)
- Size: Keep under 1MB

### Example Audio Content

Good test phrases:
- "Hello world, this is a subtitle generation test"
- "The quick brown fox jumps over the lazy dog"
- "Testing one, two, three, four, five"

## Creating Test Audio

### Using FFmpeg with Text-to-Speech (macOS):

```bash
# macOS: Use 'say' command to generate speech
say "This is a test of the subtitle generation system" -o temp.aiff
ffmpeg -i temp.aiff -c:a libmp3lame sample_audio.mp3
rm temp.aiff
```

### Using FFmpeg with Tone (Fallback):

```bash
# Generate a 10-second tone (not useful for transcription, but for basic tests)
ffmpeg -f lavfi -i sine=frequency=1000:duration=10 -c:a libmp3lame sample_audio_tone.mp3
```

### Recording Your Own:

1. Use your device's voice recorder
2. Record a clear, short sentence
3. Export as MP3
4. Keep file under 1MB

### Online Resources:

- Use text-to-speech websites to generate audio
- Download royalty-free speech samples from Freesound
- Use samples from Whisper's test suite

## Notes

- Test files are **NOT** committed to git (see .gitignore)
- Tests will skip Whisper transcription if audio files are missing
- Unit tests for SRT formatting work without audio files
- For CI/CD, consider storing test assets in a separate repository or cloud storage
- Whisper model (~1GB) is downloaded on first run

## Running Tests Without Audio Files

Most tests will run without actual audio files by using mocked responses. Only integration tests marked with `@pytest.mark.slow` require real audio files.

```bash
# Run fast tests only (no real audio required)
pytest -m "not slow"

# Run all tests including slow integration tests
pytest
```
