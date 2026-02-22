import os

from video.config import PIL_AVAILABLE, Image, ImageDraw, ImageFont


def _render_watermark_pieces(vid_w, vid_h, font, wm_dir, total_dur, interval=10.0):
    """Pre-render 'contentor.stories' watermark as 4-corner PNGs and build a timeline.

    Cycles through corners every ``interval`` seconds (default 10):
    top-left → top-right → bottom-right → bottom-left.

    Returns a list of (png_path, duration_seconds) tuples, or [] if PIL is unavailable.
    """
    if not PIL_AVAILABLE:
        return []

    text = "contentor.stories"
    wm_font_size = max(22, int(vid_h * 0.018))

    # Load a font for the watermark (prefer bundled, fall back to system fonts)
    bundled = os.path.join(os.path.dirname(__file__), "..", "fonts", "Montserrat-ExtraBold.ttf")
    wm_font = None
    for fp in [bundled,
               "/System/Library/Fonts/Supplemental/Arial Black.ttf",
               "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf",
               "/usr/share/fonts/truetype/freefont/FreeSansBold.ttf"]:
        if os.path.exists(fp):
            try:
                wm_font = ImageFont.truetype(fp, wm_font_size)
                break
            except Exception:
                continue
    if wm_font is None:
        try:
            wm_font = ImageFont.load_default()
        except Exception:
            return []

    # Measure text dimensions
    tmp = Image.new('RGBA', (1, 1))
    draw = ImageDraw.Draw(tmp)
    try:
        bbox = draw.textbbox((0, 0), text, font=wm_font)
        tw, th = bbox[2] - bbox[0], bbox[3] - bbox[1]
    except AttributeError:
        tw, th = draw.textsize(text, font=wm_font)

    margin, shadow = 20, 2
    corners = [
        (margin, margin),                          # top-left
        (vid_w - tw - margin, margin),             # top-right
        (vid_w - tw - margin, vid_h - th - margin),# bottom-right
        (margin, vid_h - th - margin),             # bottom-left
    ]

    corner_paths = []
    for i, (cx, cy) in enumerate(corners):
        canvas = Image.new('RGBA', (vid_w, vid_h), (0, 0, 0, 0))
        d = ImageDraw.Draw(canvas)
        d.text((cx + shadow, cy + shadow), text, font=wm_font, fill=(0, 0, 0, 180))
        d.text((cx, cy), text, font=wm_font, fill=(255, 255, 255, 190))
        path = os.path.join(wm_dir, f"wm_corner_{i}.png")
        canvas.save(path, 'PNG')
        corner_paths.append(path)

    # Build timeline cycling through 4 corners
    pieces, t, idx = [], 0.0, 0
    while t < total_dur - 0.005:
        dur = min(interval, total_dur - t)
        pieces.append((corner_paths[idx % 4], dur))
        t += interval
        idx += 1
    return pieces
