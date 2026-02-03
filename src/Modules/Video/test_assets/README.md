# Test Assets for Video Worker

## Required Test Files

To run full integration tests, place the following files in this directory:

### Video Files
- **sample_video.mp4** - A short (5-10 second) video file for testing concatenation
- **sample_video2.mp4** - Another short video for multi-asset concatenation tests
- Format: MP4, H.264 codec, 30fps recommended
- Resolution: 1920x1080 or 1280x720
- Size: Keep under 5MB for faster tests

### Audio Files
- **sample_audio.mp3** - A short (5-10 second) audio clip for composition tests
- Format: MP3, 128kbps or higher
- Size: Keep under 1MB

### Subtitle Files
- Test SRT files are generated automatically by the tests

## Creating Test Assets

### Using FFmpeg to create test videos:

```bash
# Create a 10-second test video with color bars
ffmpeg -f lavfi -i testsrc=duration=10:size=1280x720:rate=30 -pix_fmt yuv420p sample_video.mp4

# Create a 5-second test video with different pattern
ffmpeg -f lavfi -i testsrc2=duration=5:size=1280x720:rate=30 -pix_fmt yuv420p sample_video2.mp4

# Create a 10-second test audio (sine wave)
ffmpeg -f lavfi -i sine=frequency=1000:duration=10 -c:a libmp3lame sample_audio.mp3
```

### Using Online Generators:

If you don't have FFmpeg, you can:
1. Use online test video generators (search "test video generator")
2. Record a short video/audio on your device
3. Download royalty-free short clips from:
   - Pexels (videos)
   - Freesound (audio)

## Notes

- Test files are **NOT** committed to git (see .gitignore)
- Tests will skip if files are missing
- Minimal file sizes are recommended for fast test execution
- Use simple content (solid colors, patterns) to reduce file size
