import warnings
warnings.filterwarnings("ignore", message=".*OpenSSL.*")

import os
import subprocess

try:
    from PIL import Image, ImageDraw, ImageFont
    if not hasattr(Image, 'ANTIALIAS'):
        Image.ANTIALIAS = Image.LANCZOS
    PIL_AVAILABLE = True
except Exception:
    PIL_AVAILABLE = False
    Image = ImageDraw = ImageFont = None

# Configuration
SERVICE_BUS_CONNECTION_STRING = os.environ.get("ConnectionStrings__ContentorServiceBus") or os.environ.get("SERVICE_BUS_CONNECTION_STRING")
if not SERVICE_BUS_CONNECTION_STRING:
    raise ValueError("SERVICE_BUS_CONNECTION_STRING is not set")

STORAGE_BASE_PATH = os.environ.get("STORAGE_BASE_PATH")
if not STORAGE_BASE_PATH:
    raise ValueError("STORAGE_BASE_PATH is not set")

COMMANDS_QUEUE_NAME = "video-commands-queue"
EVENTS_QUEUE_NAME = "events-queue"


def _detect_encoder():
    """Detect best available H.264 encoder. Uses VideoToolbox on macOS when available."""
    try:
        result = subprocess.run(
            ['ffmpeg', '-hide_banner', '-encoders'],
            capture_output=True, text=True, timeout=10
        )
        if 'h264_videotoolbox' in result.stdout:
            return 'h264_videotoolbox', []
    except Exception:
        pass
    return 'libx264', ['-preset', 'fast']

FFMPEG_ENCODER, FFMPEG_ENCODER_EXTRA_FLAGS = _detect_encoder()
