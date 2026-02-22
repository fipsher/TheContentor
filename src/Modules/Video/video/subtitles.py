import os
import re
import json

from video.config import PIL_AVAILABLE, Image, ImageDraw, ImageFont


def _load_subtitle_font(video_h):
    """Load the subtitle font, trying bundled Montserrat first, then system fallbacks."""
    font_size = max(36, int(video_h * 0.045))
    font = None

    if not PIL_AVAILABLE:
        return None, font_size

    # Bundled font (preferred), then Arial Black, Impact, system defaults
    bundled = os.path.join(os.path.dirname(__file__), "..", "fonts", "Montserrat-ExtraBold.ttf")
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


def prerender_subtitle_images(phrases, vid_w, vid_h, font, font_size, sub_dir):
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
