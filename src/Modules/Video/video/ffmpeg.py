import subprocess


def _run_ffmpeg(args: list, description: str = ""):
    """Run FFmpeg subprocess. Raises RuntimeError on non-zero exit."""
    cmd = ['ffmpeg', '-hide_banner', '-loglevel', 'error', '-y'] + args
    print(f"FFmpeg {description}")
    result = subprocess.run(cmd, capture_output=True, text=True)
    if result.returncode != 0:
        raise RuntimeError(f"FFmpeg failed ({description}):\n{result.stderr}")


def _probe_duration(file_path: str) -> float:
    """Return media duration in seconds via ffprobe."""
    result = subprocess.run(
        ['ffprobe', '-v', 'error', '-show_entries', 'format=duration',
         '-of', 'default=noprint_wrappers=1:nokey=1', file_path],
        capture_output=True, text=True, timeout=30
    )
    if result.returncode != 0:
        raise RuntimeError(f"ffprobe failed: {result.stderr}")
    return float(result.stdout.strip())
