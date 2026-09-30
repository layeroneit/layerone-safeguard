"""Build Safeguard icons from the Layer One palette.

Run from the repo root:  python tools/make_icons.py
Needs Pillow. Outputs are committed so the app build does not need Python.

  assets/icon/app.ico            taskbar / exe / window icon (16..256)
  assets/icon/app-256.png        large mark for the welcome + main header
  assets/tray/<state>.ico        tray icon per state (16..48)
  assets/logo/layer-one.png      Layer One wordmark, trimmed to its ink
"""

from __future__ import annotations

import math
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw

ROOT = Path(__file__).resolve().parents[1]

# Keep in step with src/Safeguard.Brand/Colors.cs.
PRIMARY = (0x3A, 0x20, 0x81)
MAGENTA = (0xFF, 0x2D, 0xC4)
VIOLET = (0xC0, 0x84, 0xFC)
WHITE = (0xFF, 0xFF, 0xFF)
MUTED = (0x5C, 0x56, 0x70)
CLEAR = (0x1F, 0x8A, 0x5B)
REVIEW = (0xC4, 0x84, 0x1A)

SS = 8  # supersample factor

APP_SIZES = [16, 20, 24, 32, 40, 48, 64, 128, 256]
TRAY_SIZES = [16, 20, 24, 32, 40, 48]

# state -> (tile colour, ring on, dot colour)
TRAY_STATES = {
    "watching": (PRIMARY, True, CLEAR),
    "resting": (PRIMARY, True, None),
    "review": (PRIMARY, True, REVIEW),
    "off": (MUTED, False, None),
}


def lerp(a, b, t):
    return tuple(round(x + (y - x) * t) for x, y in zip(a, b))


def shield_points(cx, cy, w, h):
    """Classic shield: flat-ish top, straight sides, curved to a point."""
    top = cy - h / 2
    left, right = cx - w / 2, cx + w / 2
    shoulder = top + h * 0.12
    side_end = top + h * 0.52
    pts = [(cx, top), (right, shoulder), (right, side_end)]
    bottom = cy + h / 2
    ctrl_y = side_end + (bottom - side_end) * 0.55
    steps = 24
    for i in range(1, steps + 1):  # right side bows out, then meets at the tip
        t = i / steps
        x = (1 - t) ** 2 * right + 2 * (1 - t) * t * right + t ** 2 * cx
        y = (1 - t) ** 2 * side_end + 2 * (1 - t) * t * ctrl_y + t ** 2 * bottom
        pts.append((x, y))
    for i in range(steps - 1, -1, -1):
        t = i / steps
        x = (1 - t) ** 2 * left + 2 * (1 - t) * t * left + t ** 2 * cx
        y = (1 - t) ** 2 * side_end + 2 * (1 - t) * t * ctrl_y + t ** 2 * bottom
        pts.append((x, y))
    pts += [(left, shoulder)]
    return pts


def gradient_ring(size, cx, cy, r, width, dashed):
    """Magenta -> violet ring, left to right, like the Layer One lockup."""
    mask = Image.new("L", (size, size), 0)
    d = ImageDraw.Draw(mask)
    box = [cx - r, cy - r, cx + r, cy + r]
    if dashed:
        dashes = 10
        span = 360 / dashes
        for i in range(dashes):
            start = -90 + i * span
            d.arc(box, start, start + span * 0.62, fill=255, width=width)
    else:
        d.ellipse(box, outline=255, width=width)

    grad = Image.new("RGB", (size, size))
    gd = ImageDraw.Draw(grad)
    for x in range(size):
        gd.line([(x, 0), (x, size)], fill=lerp(MAGENTA, VIOLET, x / max(1, size - 1)))
    layer = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    layer.paste(grad, (0, 0), mask)
    return layer


def mark(px, tile=PRIMARY, ring=True, dot=None):
    s = px * SS
    img = Image.new("RGBA", (s, s), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)

    small = px <= 24
    radius = s * (0.18 if small else 0.22)
    d.rounded_rectangle([0, 0, s - 1, s - 1], radius=radius, fill=tile)

    c = s / 2
    if ring:
        ring_r = s * 0.36
        ring_w = max(SS, round(s * (0.085 if small else 0.06)))
        img.alpha_composite(gradient_ring(s, c, c, ring_r, ring_w, dashed=px >= 48))

    shield_w = s * (0.40 if small else 0.34)
    shield_h = s * (0.48 if small else 0.42)
    d.polygon(shield_points(c, c + s * 0.01, shield_w, shield_h), fill=WHITE)

    if dot is not None:
        dr = s * 0.19
        dx, dy = s - dr - s * 0.02, s - dr - s * 0.02
        border = max(SS, round(s * 0.05))
        d.ellipse([dx - dr - border, dy - dr - border, dx + dr + border, dy + dr + border], fill=WHITE)
        d.ellipse([dx - dr, dy - dr, dx + dr, dy + dr], fill=dot)

    return img.resize((px, px), Image.LANCZOS)


def save_ico(path: Path, sizes, **kw):
    path.parent.mkdir(parents=True, exist_ok=True)
    frames = [mark(n, **kw) for n in sizes]
    big = frames[-1]
    big.save(path, format="ICO", sizes=[(n, n) for n in sizes], append_images=frames[:-1])


def trim_wordmark():
    src = ROOT / "assets" / "logo" / "layer-one.png"
    img = Image.open(src).convert("RGBA")
    # Treat near-white and transparent as background.
    bg = Image.new("RGBA", img.size, (255, 255, 255, 255))
    flat = Image.alpha_composite(bg, img).convert("RGB")
    diff = ImageChops.difference(flat, Image.new("RGB", img.size, WHITE)).convert("L")
    box = diff.point(lambda v: 255 if v > 24 else 0).getbbox()
    if box is None:
        return
    pad = 8
    box = (max(0, box[0] - pad), max(0, box[1] - pad), min(img.width, box[2] + pad), min(img.height, box[3] + pad))
    if box != (0, 0, img.width, img.height):
        img.crop(box).save(src)


def main():
    save_ico(ROOT / "assets" / "icon" / "app.ico", APP_SIZES)
    mark(256).save(ROOT / "assets" / "icon" / "app-256.png")
    mark(96).save(ROOT / "assets" / "icon" / "app-96.png")  # email signature
    for name, (tile, ring, dot) in TRAY_STATES.items():
        save_ico(ROOT / "assets" / "tray" / f"{name}.ico", TRAY_SIZES, tile=tile, ring=ring, dot=dot)
    trim_wordmark()

    # Preview sheet for review (not shipped).
    preview = Image.new("RGBA", (40 + 280 + 4 * 60, 280), (0xF6, 0xF4, 0xF8, 255))
    preview.alpha_composite(mark(256), (12, 12))
    for i, (name, (tile, ring, dot)) in enumerate(TRAY_STATES.items()):
        for j, n in enumerate([48, 32, 16]):
            preview.alpha_composite(mark(n, tile=tile, ring=ring, dot=dot), (290 + i * 60, 20 + j * 70))
    out = ROOT / "tools" / "icon-preview.png"
    preview.save(out)
    print("wrote icons; preview:", out)


if __name__ == "__main__":
    main()
