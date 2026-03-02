"""Simple HTTP server for on-demand TTS preview generation."""
import warnings
warnings.filterwarnings("ignore", message=".*OpenSSL.*")

import asyncio
import json
import os
import shutil
import tempfile
import uuid
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer

from mutagen.mp3 import MP3

STORAGE_BASE_PATH = os.environ.get("STORAGE_BASE_PATH")
if not STORAGE_BASE_PATH:
    raise ValueError("STORAGE_BASE_PATH is not set")

PORT = int(os.environ.get("PORT", 8765))
PREVIEW_CONTAINER = "tts-preview"


async def _generate_edge_tts(text: str, voice: str, rate: int, pitch: int, output_path: str) -> None:
    import edge_tts
    rate_str = f"+{rate}%" if rate >= 0 else f"{rate}%"
    pitch_str = f"+{pitch}Hz" if pitch >= 0 else f"{pitch}Hz"
    communicate = edge_tts.Communicate(text, voice, rate=rate_str, pitch=pitch_str)
    await communicate.save(output_path)


def _generate_kokoro(text: str, voice: str, rate: int, output_path: str) -> None:
    import importlib
    import subprocess
    import numpy as np
    import soundfile as sf

    kokoro = importlib.import_module("kokoro")
    KPipeline = getattr(kokoro, "KPipeline")
    import re

    speed = max(0.5, min(2.0, 1.0 + rate / 100.0))
    pipeline = KPipeline(lang_code="a")
    sample_rate = 24000

    # Insert newlines after sentence-ending punctuation so Kokoro's
    # default \n+ split produces natural inter-sentence pauses.
    processed_text = re.sub(r'(?<=[.!?])\s+', '\n', text)

    chunks = []
    for _, _, audio in pipeline(processed_text, voice=voice, speed=speed):
        if audio is not None:
            chunks.append(audio)

    if not chunks:
        raise RuntimeError("Kokoro produced no audio")
    wav_path = output_path.replace(".mp3", ".wav")
    sf.write(wav_path, np.concatenate(chunks), sample_rate)
    subprocess.run(
        ["ffmpeg", "-y", "-i", wav_path, "-q:a", "2", output_path],
        check=True,
        capture_output=True,
    )
    os.remove(wav_path)


async def generate_preview(request: dict) -> dict:
    """Generate TTS audio and save to storage/tts-preview/. Returns audioPath and durationSeconds."""
    text = request.get("text", "").strip()
    if not text:
        return {"error": "Text is required"}

    voice = request.get("voice", "en-US-GuyNeural")
    rate = int(request.get("rate", 0))
    pitch = int(request.get("pitch", 0))
    engine = request.get("engine", "EdgeTTS")

    with tempfile.TemporaryDirectory() as tmp_dir:
        audio_file = os.path.join(tmp_dir, "preview.mp3")

        if engine == "Kokoro":
            loop = asyncio.get_event_loop()
            await loop.run_in_executor(None, _generate_kokoro, text, voice, rate, audio_file)
        else:
            await _generate_edge_tts(text, voice, rate, pitch, audio_file)

        duration = MP3(audio_file).info.length

        unique_name = f"preview-{uuid.uuid4()}.mp3"
        dest_dir = os.path.join(STORAGE_BASE_PATH, PREVIEW_CONTAINER)
        os.makedirs(dest_dir, exist_ok=True)
        shutil.copy2(audio_file, os.path.join(dest_dir, unique_name))

    return {
        "audioPath": f"{PREVIEW_CONTAINER}/{unique_name}",
        "durationSeconds": duration,
    }


class TtsPreviewHandler(BaseHTTPRequestHandler):
    def do_GET(self):
        if self.path == "/health":
            self._respond(200, b"ok", "text/plain")
        else:
            self.send_error(404)

    def do_POST(self):
        if self.path != "/preview":
            self.send_error(404)
            return
        try:
            length = int(self.headers.get("Content-Length", 0))
            body = json.loads(self.rfile.read(length)) if length else {}
            result = asyncio.run(generate_preview(body))
            status = 400 if "error" in result else 200
            self._respond(status, json.dumps(result).encode(), "application/json")
        except Exception as exc:
            import traceback
            traceback.print_exc()
            self._respond(500, json.dumps({"error": str(exc)}).encode(), "application/json")

    def _respond(self, status: int, payload: bytes, content_type: str) -> None:
        self.send_response(status)
        self.send_header("Content-Type", content_type)
        self.send_header("Content-Length", str(len(payload)))
        self.end_headers()
        self.wfile.write(payload)

    def log_message(self, fmt, *args):
        print(f"[tts-preview] {self.address_string()} - {fmt % args}")


if __name__ == "__main__":
    print(f"[tts-preview] Starting on port {PORT}, storage: {STORAGE_BASE_PATH}")
    server = ThreadingHTTPServer(("0.0.0.0", PORT), TtsPreviewHandler)
    server.serve_forever()