import asyncio
import os
import tempfile

from video.config import FFMPEG_ENCODER, FFMPEG_ENCODER_EXTRA_FLAGS, PIL_AVAILABLE, Image
from video.ffmpeg import _run_ffmpeg, _probe_duration
from video.storage import read_from_local_storage
from video.subtitles import _parse_subtitles, _load_subtitle_font, prerender_subtitle_images
from video.watermark import _render_watermark_pieces


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

        def _build_and_write_final(video_path, audio_path, subtitle_path, out_path):
            vid_w, vid_h = 1080, 1920  # known from concat-cut output
            audio_dur = _probe_duration(audio_path)

            phrases = _parse_subtitles(subtitle_path)
            font, font_size = _load_subtitle_font(vid_h)

            sub_dir = os.path.join(os.path.dirname(out_path), "subtitle_imgs")
            os.makedirs(sub_dir, exist_ok=True)

            segments = []
            if PIL_AVAILABLE and font is not None:
                segments = prerender_subtitle_images(phrases, vid_w, vid_h, font, font_size, sub_dir)

            wm_pieces = _render_watermark_pieces(vid_w, vid_h, font, sub_dir, audio_dur)

            if not segments:
                if wm_pieces:
                    # No subtitles but watermark: build overlay stream from PIL-rendered PNGs
                    wm_unique = list(dict.fromkeys(p for p, _ in wm_pieces))
                    wm_path_to_idx = {p: 2 + i for i, p in enumerate(wm_unique)}
                    inputs = ['-i', video_path, '-i', audio_path]
                    for p in wm_unique:
                        inputs += ['-loop', '1', '-r', '30', '-t', str(audio_dur + 1), '-i', p]
                    wm_fparts = [f"[0:v]trim=end={audio_dur:.6f},setpts=PTS-STARTPTS[base]"]
                    wm_labels = []
                    for k, (path, dur) in enumerate(wm_pieces):
                        label = f'wm{k}'
                        wm_fparts.append(
                            f"[{wm_path_to_idx[path]}:v]trim=end={max(dur, 1/30):.6f},"
                            f"setpts=PTS-STARTPTS,format=rgba[{label}]"
                        )
                        wm_labels.append(f'[{label}]')
                    n_wm = len(wm_labels)
                    wm_fparts.append(
                        f"{''.join(wm_labels)}concat=n={n_wm}:v=1:a=0,format=rgba[watermark]"
                    )
                    wm_fparts.append("[base][watermark]overlay=x=0:y=0:format=auto[vout]")
                    if FFMPEG_ENCODER == 'h264_videotoolbox':
                        enc_flags = ['-c:v', FFMPEG_ENCODER, '-q:v', '65', '-pix_fmt', 'nv12']
                    else:
                        enc_flags = (['-c:v', FFMPEG_ENCODER]
                                     + FFMPEG_ENCODER_EXTRA_FLAGS
                                     + ['-crf', '18', '-maxrate', '12M', '-bufsize', '24M',
                                        '-pix_fmt', 'yuv420p'])
                    args = (inputs
                            + ['-filter_complex', ';'.join(wm_fparts),
                               '-map', '[vout]', '-map', '1:a']
                            + enc_flags
                            + ['-c:a', 'aac', '-b:a', '192k',
                               '-shortest', '-movflags', '+faststart', out_path])
                    _run_ffmpeg(args, description="compose (no subtitles, watermark)")
                else:
                    # No subtitles, no watermark: simple copy mux
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

            # Pass 1: write a concat list and render all subtitle frames into a single
            # video track. This uses one FFmpeg input regardless of segment count,
            # avoiding the pthread_create exhaustion caused by 100+ PNG inputs.
            concat_list_path = os.path.join(sub_dir, 'subtitle_concat.txt')
            with open(concat_list_path, 'w') as cf:
                for path, dur in pieces:
                    cf.write(f"file '{path}'\n")
                    cf.write(f"duration {max(dur, 1/30):.6f}\n")
                # Trailing entry prevents the concat demuxer from dropping the last frame
                if pieces:
                    cf.write(f"file '{pieces[-1][0]}'\n")

            sub_track_path = os.path.join(sub_dir, 'subtitle_track.mov')
            _run_ffmpeg([
                '-f', 'concat', '-safe', '0', '-i', concat_list_path,
                '-vf', 'format=rgba', '-c:v', 'png',
                '-r', '30', sub_track_path,
            ], description=f"render subtitle track ({len(pieces)} pieces)")

            # Pass 2: compose — subtitle_track is a single input at index 2.
            # Watermark PNGs (typically very few) remain as direct inputs.
            wm_unique = list(dict.fromkeys(p for p, _ in wm_pieces))
            wm_path_to_idx = {p: 3 + i for i, p in enumerate(wm_unique)}
            inputs = ['-i', video_path, '-i', audio_path, '-i', sub_track_path]
            for p in wm_unique:
                inputs += ['-loop', '1', '-r', '30', '-t', str(audio_dur + 1), '-i', p]

            filter_parts = [
                f"[0:v]trim=end={audio_dur:.6f},setpts=PTS-STARTPTS[base]",
            ]
            if wm_pieces:
                wm_labels = []
                for k, (path, dur) in enumerate(wm_pieces):
                    label = f'wm{k}'
                    filter_parts.append(
                        f"[{wm_path_to_idx[path]}:v]trim=end={max(dur, 1/30):.6f},"
                        f"setpts=PTS-STARTPTS,format=rgba[{label}]"
                    )
                    wm_labels.append(f'[{label}]')
                n_wm = len(wm_labels)
                filter_parts.append(
                    f"{''.join(wm_labels)}concat=n={n_wm}:v=1:a=0,format=rgba[watermark]"
                )
                filter_parts.append("[base][2:v]overlay=x=0:y=0:format=auto[subtitled]")
                filter_parts.append("[subtitled][watermark]overlay=x=0:y=0:format=auto[vout]")
            else:
                filter_parts.append("[base][2:v]overlay=x=0:y=0:format=auto[vout]")
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
