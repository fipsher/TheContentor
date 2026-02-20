import warnings
warnings.filterwarnings("ignore", message=".*OpenSSL.*")

import asyncio
import json
import os
import shutil
import tempfile
import uuid
from azure.servicebus.aio import ServiceBusClient, AutoLockRenewer
from azure.servicebus.exceptions import MessageLockLostError
import re
from moviepy.editor import VideoFileClip, concatenate_videoclips, AudioFileClip, CompositeVideoClip, TextClip, ColorClip, ImageClip
import numpy as np
try:
    from PIL import Image, ImageDraw, ImageFont
    # Pillow 10+ removed ANTIALIAS; MoviePy's resize still references it
    if not hasattr(Image, 'ANTIALIAS'):
        Image.ANTIALIAS = Image.LANCZOS
    PIL_AVAILABLE = True
except Exception:
    PIL_AVAILABLE = False
from moviepy.video.fx.crop import crop

# Configuration
SERVICE_BUS_CONNECTION_STRING = os.environ.get("ConnectionStrings__ContentorServiceBus") or os.environ.get("SERVICE_BUS_CONNECTION_STRING")
if not SERVICE_BUS_CONNECTION_STRING:
    raise ValueError("SERVICE_BUS_CONNECTION_STRING is not set")

STORAGE_BASE_PATH = os.environ.get("STORAGE_BASE_PATH")
if not STORAGE_BASE_PATH:
    raise ValueError("STORAGE_BASE_PATH is not set")

COMMANDS_QUEUE_NAME = "video-commands-queue"
EVENTS_QUEUE_NAME = "events-queue"

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
    """Concatenate video assets and cut to target duration starting at video_offset"""
    with tempfile.TemporaryDirectory() as temp_dir:
        # Download all assets from local storage
        asset_files = []
        for i, blob_path in enumerate(asset_blob_paths):
            asset_file = os.path.join(temp_dir, f"asset_{i}.mp4")
            read_from_local_storage(blob_path['ContainerName'], blob_path['AssetPath'], asset_file)
            asset_files.append(asset_file)

        target_seconds = target_duration.total_seconds() if hasattr(target_duration, 'total_seconds') else target_duration
        offset_seconds = video_offset.total_seconds() if hasattr(video_offset, 'total_seconds') else video_offset

        def _build_and_write_video(files, max_seconds, offset, out_path):
            TARGET_FPS = 30
            TARGET_W, TARGET_H = 1080, 1920  # 9:16 portrait for social media
            clips = []
            normalized = []
            concatenated = None
            final_clip = None
            try:
                clips = [VideoFileClip(f) for f in files]

                # Center-crop each clip to 9:16 aspect ratio, then resize to 1080x1920
                target_aspect = TARGET_W / TARGET_H  # 0.5625
                for c in clips:
                    src_w, src_h = c.size
                    src_aspect = src_w / src_h

                    if src_aspect > target_aspect:
                        # Source is wider than 9:16 — crop sides
                        new_w = int(src_h * target_aspect)
                        x_center = src_w / 2
                        cropped = crop(c, x_center=x_center, width=new_w, height=src_h)
                    else:
                        # Source is taller than 9:16 — crop top/bottom
                        new_h = int(src_w / target_aspect)
                        y_center = src_h / 2
                        cropped = crop(c, y_center=y_center, width=src_w, height=new_h)

                    resized = cropped.resize((TARGET_W, TARGET_H)).set_fps(TARGET_FPS)
                    normalized.append(resized)

                concatenated = concatenate_videoclips(normalized, method="chain")

                # Slice from offset to offset + target duration (no looping)
                end = min(offset + max_seconds, concatenated.duration)
                start = min(offset, concatenated.duration)
                if end - start < 0.1:
                    print(f"Warning: not enough footage after offset {offset:.1f}s (total: {concatenated.duration:.1f}s)")

                final_clip = concatenated.subclip(start, end)

                final_clip.write_videofile(
                    out_path, codec='libx264', audio=False, fps=TARGET_FPS, preset='medium',
                    bitrate='8000k', ffmpeg_params=['-crf', '18', '-pix_fmt', 'yuv420p']
                )
                return final_clip.duration
            finally:
                if final_clip is not None:
                    final_clip.close()
                if concatenated is not None:
                    concatenated.close()
                for clip in normalized:
                    clip.close()
                for clip in clips:
                    clip.close()

        return await asyncio.to_thread(_build_and_write_video, asset_files, target_seconds, offset_seconds, output_path)

async def compose_final_video(video_blob_path, audio_blob_path, subtitle_blob_path, output_path):
    """Compose final video with audio and subtitles"""
    with tempfile.TemporaryDirectory() as temp_dir:
        # Read files from local storage
        video_file = os.path.join(temp_dir, "video.mp4")
        audio_file = os.path.join(temp_dir, "audio.mp3")
        subtitle_file = os.path.join(temp_dir, "subtitles.json")

        read_from_local_storage(video_blob_path['ContainerName'], video_blob_path['AssetPath'], video_file)
        read_from_local_storage(audio_blob_path['ContainerName'], audio_blob_path['AssetPath'], audio_file)
        read_from_local_storage(subtitle_blob_path['ContainerName'], subtitle_blob_path['AssetPath'], subtitle_file)

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

        def _srt_time_to_seconds(time_str):
            """Convert SRT timestamp (HH:MM:SS,mmm) to seconds."""
            m = re.match(r'(\d+):(\d+):(\d+),(\d+)', time_str)
            if not m:
                return 0
            h, mi, s, ms = map(int, m.groups())
            return h * 3600 + mi * 60 + s + ms / 1000.0

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

        def _build_and_write_final(video_path, audio_path, subtitle_path, out_path):
            video_clip = None
            audio_clip = None
            adjusted_video = None
            final_clip = None
            subtitle_clips = []
            try:
                video_clip = VideoFileClip(video_path)
                audio_clip = AudioFileClip(audio_path)

                # Trim video to match audio duration (no looping — concat-cut already sized it)
                audio_dur = audio_clip.duration
                video_dur = video_clip.duration
                if video_dur > audio_dur + 0.1:
                    adjusted_video = video_clip.subclip(0, audio_dur)
                else:
                    adjusted_video = video_clip

                vid_w = adjusted_video.w
                vid_h = adjusted_video.h
                sub_y = int(vid_h * 0.40)

                phrases = _parse_subtitles(subtitle_path)
                font, font_size = _load_subtitle_font(vid_h)

                for phrase in phrases:
                    phrase_start = phrase['start']
                    phrase_end = phrase['end']
                    words = phrase.get('words', [])

                    if not words:
                        continue

                    made = False

                    # Preferred: PIL-based rendering with word highlighting
                    if PIL_AVAILABLE and font is not None:
                        try:
                            # Pre-render one image per word-highlight state
                            word_images = {}
                            for wi in range(len(words)):
                                img = _render_phrase_image(phrase, wi, vid_w, vid_h, font, font_size)
                                if img is not None:
                                    word_images[wi] = img

                            if word_images:
                                for wi, word_info in enumerate(words):
                                    w_start = word_info['start']
                                    w_end = word_info['end']
                                    img_arr = word_images.get(wi)
                                    if img_arr is not None:
                                        clip = (ImageClip(img_arr)
                                                .set_start(w_start)
                                                .set_end(w_end)
                                                .set_position(('center', sub_y)))
                                        subtitle_clips.append(clip)

                                # Fill gaps between words with no-highlight image
                                no_highlight = _render_phrase_image(phrase, -1, vid_w, vid_h, font, font_size)
                                if no_highlight is not None:
                                    # Before first word
                                    if words[0]['start'] > phrase_start + 0.01:
                                        clip = (ImageClip(no_highlight)
                                                .set_start(phrase_start)
                                                .set_end(words[0]['start'])
                                                .set_position(('center', sub_y)))
                                        subtitle_clips.append(clip)
                                    # Between words
                                    for wi in range(len(words) - 1):
                                        gap_start = words[wi]['end']
                                        gap_end = words[wi + 1]['start']
                                        if gap_end - gap_start > 0.01:
                                            clip = (ImageClip(no_highlight)
                                                    .set_start(gap_start)
                                                    .set_end(gap_end)
                                                    .set_position(('center', sub_y)))
                                            subtitle_clips.append(clip)
                                    # After last word
                                    if phrase_end > words[-1]['end'] + 0.01:
                                        clip = (ImageClip(no_highlight)
                                                .set_start(words[-1]['end'])
                                                .set_end(phrase_end)
                                                .set_position(('center', sub_y)))
                                        subtitle_clips.append(clip)

                                made = True
                        except Exception as e:
                            print(f"Warning: PIL subtitle rendering failed for '{phrase.get('phrase', '')}': {e}")

                    # Fallback: MoviePy TextClip (may require ImageMagick)
                    if not made:
                        try:
                            text = phrase.get('phrase', '')
                            txt_clip = TextClip(
                                text.upper(),
                                fontsize=max(36, int(vid_h * 0.045)),
                                color='white',
                                stroke_color='black',
                                stroke_width=5,
                                method='caption',
                                size=(int(vid_w * 0.80), None)
                            ).set_start(phrase_start).set_end(phrase_end).set_position(('center', sub_y))
                            subtitle_clips.append(txt_clip)
                            made = True
                        except Exception as e:
                            print(f"Warning: Failed to create TextClip for subtitle '{phrase.get('phrase', '')}': {e}")

                    if not made:
                        print(f"Warning: Skipped subtitle due to rendering issues: '{phrase.get('phrase', '')}'")

                if subtitle_clips:
                    final_clip = CompositeVideoClip([adjusted_video] + subtitle_clips).set_audio(audio_clip)
                else:
                    final_clip = adjusted_video.set_audio(audio_clip)

                final_clip.write_videofile(
                    out_path,
                    codec='libx264',
                    audio_codec='aac',
                    fps=30,
                    preset='medium',
                    bitrate='8000k',
                    ffmpeg_params=['-crf', '18', '-pix_fmt', 'yuv420p']
                )
            finally:
                if final_clip is not None:
                    final_clip.close()
                if adjusted_video is not None and adjusted_video is not video_clip:
                    adjusted_video.close()
                if video_clip is not None:
                    video_clip.close()
                if audio_clip is not None:
                    audio_clip.close()
                for sc in subtitle_clips:
                    sc.close()

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
