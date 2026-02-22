import tempfile
import os

from video.messaging import send_event_callback
from video.storage import save_to_local_storage
from video.operations import concat_and_cut_video, compose_final_video


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
