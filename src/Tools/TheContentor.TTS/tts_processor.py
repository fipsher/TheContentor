import asyncio
import json
import os
import argparse
import edge_tts

async def generate_audio(text, voice, output_path, rate="+14%", volume="+0%"):
    """Generates an audio file from text using Edge-TTS."""
    communicate = edge_tts.Communicate(text, voice, rate=rate, volume=volume)
    await communicate.save(output_path)

async def process_post(post_data, output_dir, voice="en-US-MichelleNeural", rate="+14%"):
    """Processes a ProcessedPost JSON and generates audio files for each part."""
    if not os.path.exists(output_dir):
        os.makedirs(output_dir)
        print(f"Created directory: {output_dir}")

    tasks = []
    
    # 1. Process Title
    title = post_data.get('Description')
    if title:
        title_path = os.path.join(output_dir, "description.mp3")
        print(f"Adding task: description -> {title_path}")
        tasks.append(generate_audio(title, voice, title_path, rate=rate))

    # 2. Process Parts
    parts = post_data.get('Parts', [])
    for part in parts:
        part_num = part.get('Part')
        text = part.get('ProcessedText')
        if text:
            output_file = os.path.join(output_dir, f"part_{part_num}.mp3")
            print(f"Adding task: part {part_num} -> {output_file}")
            tasks.append(generate_audio(text, voice, output_file, rate=rate))
    
    if tasks:
        print(f"Generating {len(tasks)} audio files...")
        await asyncio.gather(*tasks)
        print(f"Successfully generated audio files in {output_dir}")
    else:
        print("No content found to process.")

def main():
    parser = argparse.ArgumentParser(description="Generate audio for ProcessedPost using Edge-TTS")
    parser.add_argument("input", help="Path to ProcessedPost JSON file or JSON string")
    parser.add_argument("--output", default="output_audio", help="Output directory for audio files")
    parser.add_argument("--voice", default="en-US-MichelleNeural", help="Edge-TTS voice to use (e.g., en-US-GuyNeural, en-GB-SoniaNeural)")
    parser.add_argument("--rate", default="+0%", help="Speed of the voice (e.g., +10%%, -5%%)")
    
    args = parser.parse_args()
    
    # Try to parse input as JSON string first, then as a file path
    try:
        post_data = json.loads(args.input)
    except json.JSONDecodeError:
        if os.path.exists(args.input):
            with open(args.input, 'r') as f:
                post_data = json.load(f)
        else:
            print(f"Error: Input is not a valid JSON string and file not found: {args.input}")
            return

    asyncio.run(process_post(post_data, args.output, args.voice, args.rate))

if __name__ == "__main__":
    main()
