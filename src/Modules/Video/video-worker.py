import warnings
warnings.filterwarnings("ignore", message=".*OpenSSL.*")

import asyncio
import json
import os
import shutil
import subprocess
import tempfile
import uuid
import re
from azure.servicebus.aio import ServiceBusClient, AutoLockRenewer
from azure.servicebus.exceptions import MessageLockLostError
try:
    from PIL import Image, ImageDraw, ImageFont
    if not hasattr(Image, 'ANTIALIAS'):
        Image.ANTIALIAS = Image.LANCZOS
    PIL_AVAILABLE = True
except Exception:
    PIL_AVAILABLE = False

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


def save_to_local_storage(file_path, container_name):
    """Save file to local storage, return (containerName, assetPath)"""
    name, ext = os.path.splitext(os.path.basename(file_path))
    unique_name = f"{name}-{uuid.uuid4()}{ext}"
    container_dir = os.path.join(STORAGE_BASE_PATH, container_name)
    os.makedirs(container_dir, exist_ok=True)
    shutil.copy2(file_path, os.path.join(container_dir, unique_name))
    return container_name, unique_name

def read_from_local_storage(container_name, asset_path, output_path):
    """Copy file from local storage to output_path"""
    src = os.path.join(STORAGE_BASE_PATH, container_name, asset_path)
    if not os.path.exists(src):
        raise FileNotFoundError(f"Blob not found: {container_name}/{asset_path}")
    shutil.copy2(src, output_path)


def _load_subtitle_font(video_h):
    """Load the subtitle font, trying bundled Montserrat first, then system fallbacks."""
    font_size = max(36, int(video_h * 0.045))
    font = None

    if not PIL_AVAILABLE:
        return None, font_size

    # Bundled font (preferred), then Arial Black, Impact, system defaults
    bundled = os.path.join(os.path.dirname(__file__), "fonts", "Montserrat-ExtraBold.ttf")
    font_candidates = [
        bundled,
        # Arial Black
        "/System/Library/Fonts/Supplemental/Arial Black.ttf",
        "/Library/Fonts/Arial Black.ttf",
        "/usr/share/fonts/truetype/msttcorefonts/Arial_Black.ttf",
        # Impact
        "/System/Library/Fonts/Supplemental/Impact.ttf",
        "/Library/Fonts/Impact.ttf",
        "/usr/share/fonts/truetype/msttcorefonts/Impact.ttf",
        # Generic bold fallbacks
        "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf",
        "/usr/share/fonts/truetype/freefont/FreeSansBold.ttf",
        "/System/Library/Fonts/Supplemental/Arial Bold.ttf",
        "/Library/Fonts/Arial Bold.ttf",
    ]
    for fp in font_candidates:
        if os.path.exists(fp):
            try:
                font = ImageFont.truetype(fp, font_size)
                break
            except Exception:
                continue
    if font is None:
        try:
            font = ImageFont.load_default()
        except Exception:
            font = None

    return font, font_size


def _srt_time_to_seconds(time_str):
    """Convert SRT timestamp (HH:MM:SS,mmm) to seconds."""
    m = re.match(r'(\d+):(\d+):(\d+),(\d+)', time_str)
    if not m:
        return 0
    h, mi, s, ms = map(int, m.groups())
    return h * 3600 + mi * 60 + s + ms / 1000.0


def _parse_subtitles(subtitle_path):
    """Parse subtitle file (JSON phrase format or legacy SRT)."""
    with open(subtitle_path, 'r', encoding='utf-8') as f:
        content = f.read().strip()

    # Try JSON first (new phrase-grouped format)
    if content.startswith('['):
        try:
            return json.loads(content)
        except json.JSONDecodeError:
            pass

    # Legacy SRT fallback: convert to phrase format
    content = content.replace('\r\n', '\n')
    pattern = re.compile(
        r'(\d+)\n(\d{2}:\d{2}:\d{2},\d{3}) --> (\d{2}:\d{2}:\d{2},\d{3})\n(.*?)(?=\n\d+\n|\Z)',
        re.DOTALL,
    )
    phrases = []
    for m in pattern.finditer(content):
        _, start_str, end_str, text = m.groups()
        start = _srt_time_to_seconds(start_str)
        end = _srt_time_to_seconds(end_str)
        word = text.strip()
        if word:
            phrases.append({
                'phrase': word,
                'start': start,
                'end': end,
                'words': [{'word': word, 'start': start, 'end': end}],
            })
    return phrases


def _render_phrase_image(phrase_data, active_word_idx, video_w, video_h, font, font_size):  # noqa: C901
    """Render a phrase image with the active word highlighted in gold.

    Args:
        phrase_data: dict with 'phrase' and 'words' list
        active_word_idx: index of the currently-spoken word (-1 for none)
        video_w: video width in pixels
        video_h: video height in pixels
        font: PIL ImageFont
        font_size: base font size in pixels

    Returns:
        numpy array (RGBA) or None if rendering fails
    """
    import numpy as np

    if not PIL_AVAILABLE or font is None:
        return None

    words = phrase_data.get('words', [])
    if not words:
        return None

    max_width = int(video_w * 0.80)
    stroke_w = 5
    shadow_offset = (3, 3)
    shadow_color = (0, 0, 0, 153)
    inactive_color = (255, 255, 255, 255)
    active_color = (255, 215, 0, 255)
    stroke_color = (0, 0, 0, 255)

    # Active word gets a slightly larger font for scale pop
    active_font_size = int(font_size * 1.10)
    active_font = None
    if active_word_idx >= 0:
        try:
            active_font = font.font_variant(size=active_font_size)
        except Exception:
            active_font = font

    # Uppercase all words
    display_words = [w['word'].upper() for w in words]

    # Measure dummy draw surface
    dummy_img = Image.new("RGBA", (max_width * 2, 10), (0, 0, 0, 0))
    draw = ImageDraw.Draw(dummy_img)

    # Word-wrap into lines, tracking which word index each token belongs to
    # Each line is a list of (word_text, word_index) tuples
    lines = []
    current_line = []
    for wi, word_text in enumerate(display_words):
        w_font = active_font if (wi == active_word_idx and active_font) else font
        # Measure current line + this word
        test_text = ' '.join(t for t, _ in current_line) + (' ' if current_line else '') + word_text
        bbox = draw.textbbox((0, 0), test_text, font=w_font, stroke_width=stroke_w)
        line_w = bbox[2] - bbox[0]
        if line_w > max_width and current_line:
            lines.append(current_line)
            current_line = [(word_text, wi)]
        else:
            current_line.append((word_text, wi))
    if current_line:
        lines.append(current_line)

    # Compute line dimensions
    line_spacing = int(font_size * 0.35)
    line_metrics = []  # (line_width, line_height, [(word_text, word_idx, word_font, word_bbox)])
    total_height = 0
    max_line_width = 0

    for line in lines:
        line_word_data = []
        line_h = 0
        line_w = 0
        for i, (word_text, wi) in enumerate(line):
            w_font = active_font if (wi == active_word_idx and active_font) else font
            bbox = draw.textbbox((0, 0), word_text, font=w_font, stroke_width=stroke_w)
            w = bbox[2] - bbox[0]
            h = bbox[3] - bbox[1]
            line_word_data.append((word_text, wi, w_font, bbox))
            line_w += w
            line_h = max(line_h, h)
        # Add spaces between words
        if len(line) > 1:
            space_bbox = draw.textbbox((0, 0), ' ', font=font, stroke_width=stroke_w)
            space_w = space_bbox[2] - space_bbox[0]
            line_w += space_w * (len(line) - 1)

        line_metrics.append((line_w, line_h, line_word_data))
        total_height += line_h
        max_line_width = max(max_line_width, line_w)

    total_height += line_spacing * max(0, len(lines) - 1)

    # Add padding around the text for shadow/stroke overflow
    pad = stroke_w + abs(shadow_offset[0]) + 4
    img_w = max_line_width + 2 * pad
    img_h = total_height + 2 * pad

    img = Image.new("RGBA", (img_w, img_h), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    # Measure space width
    space_bbox = draw.textbbox((0, 0), ' ', font=font, stroke_width=stroke_w)
    space_w = space_bbox[2] - space_bbox[0]

    # Draw each line centered
    y = pad
    for line_w, line_h, line_word_data in line_metrics:
        x = (img_w - line_w) // 2
        for word_text, wi, w_font, bbox in line_word_data:
            w = bbox[2] - bbox[0]
            x_offset = -bbox[0]
            y_offset = -bbox[1]

            is_active = (wi == active_word_idx)
            fill = active_color if is_active else inactive_color

            # Drop shadow
            draw.text(
                (x + x_offset + shadow_offset[0], y + y_offset + shadow_offset[1]),
                word_text, font=w_font, fill=shadow_color,
                stroke_width=stroke_w, stroke_fill=shadow_color,
            )
            # Main text with stroke
            draw.text(
                (x + x_offset, y + y_offset),
                word_text, font=w_font, fill=fill,
                stroke_width=stroke_w, stroke_fill=stroke_color,
            )

            x += w + space_w

        y += line_h + line_spacing

    return np.array(img)


async def send_event_callback(sender, callback_data):
    """Send callback event to orchestrator"""
    from azure.servicebus import ServiceBusMessage

    message = ServiceBusMessage(json.dumps(callback_data))
    message.content_type = "application/json"
    if message.application_properties is None:
        message.application_properties = {}
    message.application_properties["Type"] = f"video-{callback_data.get('CommandType', 'unknown')}"
    await sender.send_messages(message)

async def concat_and_cut_video(asset_blob_paths, target_duration, output_path, video_offset=0):
    """Concatenate video assets and cut to target duration starting at video_offset using FFmpeg directly."""
    with tempfile.TemporaryDirectory() as temp_dir:
        asset_files = []
        for i, blob_path in enumerate(asset_blob_paths):
            asset_file = os.path.join(temp_dir, f"asset_{i}.mp4")
            read_from_local_storage(blob_path['ContainerName'], blob_path['AssetPath'], asset_file)
            asset_files.append(asset_file)

        target_seconds = target_duration.total_seconds() if hasattr(target_duration, 'total_seconds') else float(target_duration)
        offset_seconds = video_offset.total_seconds() if hasattr(video_offset, 'total_seconds') else float(video_offset)

        def _build_and_write_video(files, max_seconds, offset, out_path):
            # Probe total available duration
            total_dur = sum(_probe_duration(f) for f in files)
            end = min(offset + max_seconds, total_dur)
            start = min(offset, total_dur)
            if end - start < 0.1:
                print(f"Warning: not enough footage after offset {offset:.1f}s (total: {total_dur:.1f}s)")

            n = len(files)
            inputs = []
            for f in files:
                inputs += ['-i', f]

            # Per-clip: crop to 9:16 aspect ratio (handles both wider and taller sources),
            # resize to 1080x1920, set fps=30, normalize pixel format
            filter_parts = []
            for i in range(n):
                filter_parts.append(
                    f"[{i}:v]"
                    f"crop='if(gt(iw/ih\\,9/16)\\,ih*9/16\\,iw)':'if(gt(iw/ih\\,9/16)\\,ih\\,iw*16/9)',"
                    f"scale=1080:1920:flags=lanczos,format=yuv420p,fps=30"
                    f"[v{i}]"
                )

            concat_inputs = ''.join(f'[v{i}]' for i in range(n))
            filter_parts.append(f"{concat_inputs}concat=n={n}:v=1:a=0[vcat]")
            filter_parts.append(
                f"[vcat]trim=start={start:.6f}:end={end:.6f},setpts=PTS-STARTPTS[vout]"
            )
            filter_complex = ';'.join(filter_parts)

            if FFMPEG_ENCODER == 'h264_videotoolbox':
                encode_flags = ['-c:v', FFMPEG_ENCODER, '-q:v', '65', '-pix_fmt', 'nv12']
            else:
                encode_flags = (['-c:v', FFMPEG_ENCODER]
                                + FFMPEG_ENCODER_EXTRA_FLAGS
                                + ['-crf', '18', '-maxrate', '12M', '-bufsize', '24M',
                                   '-pix_fmt', 'yuv420p'])

            args = (inputs
                    + ['-filter_complex', filter_complex, '-map', '[vout]']
                    + encode_flags
                    + ['-an', '-vsync', 'cfr', '-movflags', '+faststart',
                       '-threads', '0', out_path])
            _run_ffmpeg(args, description=f"concat-cut {n} clips, offset={offset:.1f}s")
            return _probe_duration(out_path)

        return await asyncio.to_thread(_build_and_write_video, asset_files, target_seconds, offset_seconds, output_path)

async def compose_final_video(video_blob_path, audio_blob_path, subtitle_blob_path, output_path):
    """Compose final video with audio and subtitles using FFmpeg directly."""
    with tempfile.TemporaryDirectory() as temp_dir:
        video_file = os.path.join(temp_dir, "video.mp4")
        audio_file = os.path.join(temp_dir, "audio.mp3")
        subtitle_file = os.path.join(temp_dir, "subtitles.json")

        read_from_local_storage(video_blob_path['ContainerName'], video_blob_path['AssetPath'], video_file)
        read_from_local_storage(audio_blob_path['ContainerName'], audio_blob_path['AssetPath'], audio_file)
        read_from_local_storage(subtitle_blob_path['ContainerName'], subtitle_blob_path['AssetPath'], subtitle_file)

        def _prerender_subtitle_images(phrases, vid_w, vid_h, font, font_size, sub_dir):
            """Pre-render subtitle images to PNG files. Returns list of segment dicts."""
            sub_y = int(vid_h * 0.40)
            segments = []
            img_cache = {}

            def _get_or_render(pi, phrase, active_wi):
                key = (pi, active_wi)
                if key in img_cache:
                    return img_cache[key]
                img_arr = _render_phrase_image(phrase, active_wi, vid_w, vid_h, font, font_size)
                if img_arr is None:
                    return None
                # Composite onto full-size canvas so FFmpeg just does x=0:y=0 overlay
                small_img = Image.fromarray(img_arr)
                canvas = Image.new('RGBA', (vid_w, vid_h), (0, 0, 0, 0))
                x_pos = (vid_w - small_img.width) // 2
                canvas.paste(small_img, (x_pos, sub_y), small_img)
                path = os.path.join(sub_dir, f"sub_{pi}_{active_wi + 2}.png")
                canvas.save(path, 'PNG')
                img_cache[key] = path
                return path

            for pi, phrase in enumerate(phrases):
                words = phrase.get('words', [])
                phrase_start = phrase['start']
                phrase_end = phrase['end']
                if not words:
                    continue

                # Before first word
                if words[0]['start'] > phrase_start + 0.01:
                    p = _get_or_render(pi, phrase, -1)
                    if p:
                        segments.append({'path': p, 'start': phrase_start,
                                         'end': words[0]['start'], 'y': sub_y})

                for wi, w in enumerate(words):
                    p = _get_or_render(pi, phrase, wi)
                    if p:
                        segments.append({'path': p, 'start': w['start'],
                                         'end': w['end'], 'y': sub_y})
                    # Gap to next word
                    if wi < len(words) - 1:
                        gap_s = w['end']
                        gap_e = words[wi + 1]['start']
                        if gap_e - gap_s > 0.01:
                            p = _get_or_render(pi, phrase, -1)
                            if p:
                                segments.append({'path': p, 'start': gap_s,
                                                 'end': gap_e, 'y': sub_y})

                # After last word
                if phrase_end > words[-1]['end'] + 0.01:
                    p = _get_or_render(pi, phrase, -1)
                    if p:
                        segments.append({'path': p, 'start': words[-1]['end'],
                                         'end': phrase_end, 'y': sub_y})

            return segments

        def _build_and_write_final(video_path, audio_path, subtitle_path, out_path):
            vid_w, vid_h = 1080, 1920  # known from concat-cut output
            audio_dur = _probe_duration(audio_path)

            phrases = _parse_subtitles(subtitle_path)
            font, font_size = _load_subtitle_font(vid_h)

            sub_dir = os.path.join(os.path.dirname(out_path), "subtitle_imgs")
            os.makedirs(sub_dir, exist_ok=True)

            segments = []
            if PIL_AVAILABLE and font is not None:
                segments = _prerender_subtitle_images(phrases, vid_w, vid_h, font, font_size, sub_dir)

            if not segments:
                # No subtitles: just mux video + audio
                args = [
                    '-i', video_path, '-i', audio_path,
                    '-map', '0:v', '-map', '1:a',
                    '-c:v', 'copy', '-c:a', 'aac', '-b:a', '192k',
                    '-shortest', '-movflags', '+faststart', out_path
                ]
                _run_ffmpeg(args, description="compose (no subtitles)")
                return

            # Build subtitle timeline: gap/segment pieces concatenated → single overlay
            # This replaces N chained overlays (O(N)/frame) with 1 overlay (O(1)/frame).
            segs_sorted = sorted(segments, key=lambda s: s['start'])

            # Blank full-size transparent PNG for silent gaps between subtitles
            blank_path = os.path.join(sub_dir, 'blank.png')
            Image.new('RGBA', (vid_w, vid_h), (0, 0, 0, 0)).save(blank_path, 'PNG')

            # Deduplicate PNG inputs: blank first (idx=2), then subtitle states
            unique_paths = [blank_path]
            path_to_idx = {blank_path: 2}
            for seg in segs_sorted:
                p = seg['path']
                if p not in path_to_idx:
                    path_to_idx[p] = 2 + len(unique_paths)
                    unique_paths.append(p)

            inputs = ['-i', video_path, '-i', audio_path]
            for p in unique_paths:
                inputs += ['-loop', '1', '-r', '30', '-t', str(audio_dur + 1), '-i', p]

            # Build ordered piece list: (png_path, duration_seconds)
            pieces = []
            prev_end = 0.0
            for seg in segs_sorted:
                if seg['start'] > prev_end + 0.005:
                    pieces.append((blank_path, seg['start'] - prev_end))
                pieces.append((seg['path'], seg['end'] - seg['start']))
                prev_end = seg['end']
            if audio_dur > prev_end + 0.005:
                pieces.append((blank_path, audio_dur - prev_end))

            # Each piece: trim the looped PNG to its duration, normalise timestamps + format
            filter_parts = []
            filter_parts.append(
                f"[0:v]trim=end={audio_dur:.6f},setpts=PTS-STARTPTS[base]"
            )
            piece_labels = []
            for k, (path, dur) in enumerate(pieces):
                label = f'pc{k}'
                in_idx = path_to_idx[path]
                # max() guards against sub-frame durations that would produce 0 frames
                filter_parts.append(
                    f"[{in_idx}:v]trim=end={max(dur, 1/30):.6f},"
                    f"setpts=PTS-STARTPTS,format=rgba[{label}]"
                )
                piece_labels.append(f'[{label}]')

            # Concat all pieces into one subtitle stream, then single overlay
            n_pieces = len(piece_labels)
            filter_parts.append(
                f"{''.join(piece_labels)}concat=n={n_pieces}:v=1:a=0,format=rgba[subtitles]"
            )
            filter_parts.append("[base][subtitles]overlay=x=0:y=0:format=auto[vout]")
            filter_complex = ';'.join(filter_parts)

            if FFMPEG_ENCODER == 'h264_videotoolbox':
                encode_flags = ['-c:v', FFMPEG_ENCODER, '-q:v', '65', '-pix_fmt', 'nv12']
            else:
                encode_flags = (['-c:v', FFMPEG_ENCODER]
                                + FFMPEG_ENCODER_EXTRA_FLAGS
                                + ['-crf', '18', '-maxrate', '12M', '-bufsize', '24M',
                                   '-pix_fmt', 'yuv420p'])

            args = (inputs
                    + ['-filter_complex', filter_complex,
                       '-map', '[vout]', '-map', '1:a']
                    + encode_flags
                    + ['-c:a', 'aac', '-b:a', '192k',
                       '-shortest', '-movflags', '+faststart',
                       '-threads', '0', out_path])
            _run_ffmpeg(args, description=f"compose with {len(segments)} subtitle segments")

        await asyncio.to_thread(_build_and_write_final, video_file, audio_file, subtitle_file, output_path)

async def process_video_command(command, events_sender):
    """Process a single video command"""
    try:
        command_type = command.get('CommandType')
        print(f"Processing video command: {command_type} for Part {command.get('PartId')}")

        orchestration_instance_id = command.get("OrchestrationInstanceId")
        processed_post_id = command.get("ProcessedPostId")
        part_id = command.get("PartId")

        if not orchestration_instance_id:
            print("Missing orchestration instance ID")
            return

        try:
            if command_type == "concat-cut":
                # Concatenate and cut video
                asset_blob_paths = command.get("AssetBlobPaths", [])
                target_duration_str = command.get("TargetDuration")
                video_offset_str = command.get("VideoOffset")

                # Parse duration (format: HH:MM:SS or total seconds)
                def _parse_timespan(val):
                    if val is None:
                        return 0.0
                    if isinstance(val, str):
                        parts = val.split(':')
                        if len(parts) == 3:
                            h, m, s = map(float, parts)
                            return h * 3600 + m * 60 + s
                        return float(val)
                    return float(val)

                target_duration = _parse_timespan(target_duration_str)
                video_offset = _parse_timespan(video_offset_str)

                print(f"concat-cut: target_duration={target_duration:.1f}s, video_offset={video_offset:.1f}s")

                with tempfile.TemporaryDirectory() as temp_dir:
                    output_file = os.path.join(temp_dir, f"video_part_{part_id}.mp4")
                    duration = await concat_and_cut_video(asset_blob_paths, target_duration, output_file, video_offset)

                    # Save to local storage
                    container, blob_path = save_to_local_storage(output_file, "generated-videos")

                    # Send success callback
                    callback = {
                        "OrchestrationInstanceId": orchestration_instance_id,
                        "ProcessedPostId": processed_post_id,
                        "PartId": part_id,
                        "CommandType": command_type,
                        "BlobContainer": container,
                        "BlobPath": blob_path,
                        "Duration": f"{int(duration // 3600):02d}:{int((duration % 3600) // 60):02d}:{duration % 60:06.3f}",
                        "Success": True
                    }
                    await send_event_callback(events_sender, callback)
                    print(f"Successfully processed {command_type} for Part {part_id}")

            elif command_type == "compose":
                # Compose final video with audio and subtitles
                video_blob_path = command.get("VideoBlobPath")
                audio_blob_path = command.get("AudioBlobPath")
                subtitle_blob_path = command.get("SubtitleBlobPath")

                with tempfile.TemporaryDirectory() as temp_dir:
                    output_file = os.path.join(temp_dir, f"final_video_part_{part_id}.mp4")
                    await compose_final_video(video_blob_path, audio_blob_path, subtitle_blob_path, output_file)

                    # Save to local storage
                    container, blob_path = save_to_local_storage(output_file, "final-videos")

                    # Send success callback
                    callback = {
                        "OrchestrationInstanceId": orchestration_instance_id,
                        "ProcessedPostId": processed_post_id,
                        "PartId": part_id,
                        "CommandType": command_type,
                        "BlobContainer": container,
                        "BlobPath": blob_path,
                        "Success": True
                    }
                    await send_event_callback(events_sender, callback)
                    print(f"Successfully processed {command_type} for Part {part_id}")

        except Exception as processing_error:
            error_msg = str(processing_error)
            callback = {
                "OrchestrationInstanceId": orchestration_instance_id,
                "ProcessedPostId": processed_post_id,
                "PartId": part_id,
                "CommandType": command_type,
                "BlobContainer": None,
                "BlobPath": None,
                "Success": False,
                "ErrorMessage": error_msg
            }
            await send_event_callback(events_sender, callback)
            print(f"Failed to process video: {processing_error}")

    except Exception as e:
        print(f"Error processing video command: {e}")
        import traceback
        traceback.print_exc()

async def main():
    print(f"Connecting to Service Bus...")
    print(f"Commands Queue: {COMMANDS_QUEUE_NAME}")
    print(f"Events Queue: {EVENTS_QUEUE_NAME}")
    print(f"Storage Base Path: {STORAGE_BASE_PATH}")

    client = ServiceBusClient.from_connection_string(SERVICE_BUS_CONNECTION_STRING)
    async with client:
        receiver = client.get_queue_receiver(queue_name=COMMANDS_QUEUE_NAME)
        events_sender = client.get_queue_sender(queue_name=EVENTS_QUEUE_NAME)

        async with receiver, events_sender, AutoLockRenewer(max_lock_renewal_duration=60 * 30) as auto_lock_renewer:
            print(f"Listening on queue: {COMMANDS_QUEUE_NAME}")
            async for msg in receiver:
                auto_lock_renewer.register(receiver, msg, max_lock_renewal_duration=60 * 30)
                try:
                    body_bytes = b"".join(msg.body)
                    body_str = body_bytes.decode('utf-8')
                    print(f"Received message: {body_str[:100]}...")
                    command = json.loads(body_str)
                    await process_video_command(command, events_sender)
                    await receiver.complete_message(msg)
                except Exception as e:
                    print(f"Error handling message: {type(e).__name__}: {e}")
                    import traceback
                    traceback.print_exc()
                    try:
                        await receiver.dead_letter_message(
                            msg,
                            reason="ProcessingError",
                            error_description=str(e),
                        )
                    except MessageLockLostError:
                        print("Message lock lost before dead-letter; skipping settlement.")

if __name__ == "__main__":
    asyncio.run(main())
