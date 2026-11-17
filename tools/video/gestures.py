"""Animated gesture cards for the demo video (3 s loops, RGBA PNG sequences).
Hand gestures are hard to see in a headset recording shrunk to 1080p, so each card shows one gesture big and simple.
Usage: python3 -I gestures.py <fonts_dir> <out_dir>"""
import math
import os
import sys
from PIL import Image, ImageDraw, ImageFont

FONTS, OUT = sys.argv[1], sys.argv[2]
W, H, SS, FRAMES = 440, 300, 3, 90
SKIN, SKIN_DARK, BG = (238, 222, 204), (222, 200, 178), (21, 23, 28, 215)
ACCENT, LEMON, WHITE, MUTED = (111, 160, 245), (242, 207, 59), (255, 255, 255), (201, 207, 218)
BOLD = ImageFont.truetype(os.path.join(FONTS, "AtkinsonHyperlegible-Bold.ttf"), 30 * SS)
REG = ImageFont.truetype(os.path.join(FONTS, "AtkinsonHyperlegible-Regular.ttf"), 22 * SS)


def ease(t):
    t = max(0.0, min(1.0, t))
    return t * t * (3 - 2 * t)


def seg(t, a, b):
    """0..1 progress of t inside [a, b] (fractions of the loop)."""
    return ease((t - a) / (b - a))


def S(*v):
    return [x * SS for x in v]


def card(title, caption):
    img = Image.new("RGBA", (W * SS, H * SS), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.rounded_rectangle(S(0, 0, W, H), radius=26 * SS, fill=BG)
    d.text(S(28, 214)[0:2], title, font=BOLD, fill=WHITE)
    d.text(S(28, 254)[0:2], caption, font=REG, fill=MUTED)
    return img, d


def fist(d, ox=0):
    """Loose fist seen from the side: palm block + index finger along the top."""
    d.rounded_rectangle(S(60 + ox, 92, 170 + ox, 190), radius=40 * SS, fill=SKIN)
    d.rounded_rectangle(S(80 + ox, 92, 330 + ox, 132), radius=20 * SS, fill=SKIN)
    d.line(S(150 + ox, 112, 150 + ox, 112), fill=SKIN_DARK, width=2 * SS)


def thumb(d, tip, base=(150, 165), ox=0):
    bx, by = base[0] + ox, base[1]
    d.line(S(bx, by, tip[0], tip[1]), fill=SKIN_DARK, width=34 * SS)
    d.ellipse(S(tip[0] - 17, tip[1] - 17, tip[0] + 17, tip[1] + 17), fill=SKIN_DARK)
    d.ellipse(S(bx - 17, by - 17, bx + 17, by + 17), fill=SKIN_DARK)


def ripple(d, x, y, k, color):
    if k <= 0 or k >= 1:
        return
    r = 14 + 40 * k
    a = int(255 * (1 - k))
    d.ellipse(S(x - r, y - r, x + r, y + r), outline=color + (a,), width=4 * SS)


def save(img, name, i):
    os.makedirs(os.path.join(OUT, name), exist_ok=True)
    img.resize((W, H), Image.LANCZOS).save(os.path.join(OUT, name, f"{i:03d}.png"))


for i in range(FRAMES):
    t = i / FRAMES

    # Thumb tap: thumb rises, taps the side of the index finger, ripple.
    img, d = card("Thumb tap", "Read the detected page")
    fist(d)
    lift = 1 - seg(t, 0.25, 0.42) if t < 0.5 else seg(t, 0.62, 0.85)
    thumb(d, (215, 134 - 34 * lift))
    ripple(d, 215, 118, (t - 0.42) / 0.3, LEMON)
    save(img, "tap", i)

    # Thumb swipe: thumb slides along the index finger, left to right, with a trail.
    img, d = card("Thumb swipe", "Next page or next sentence")
    fist(d)
    k = seg(t, 0.15, 0.55)
    x = 160 + 140 * k
    if 0.15 < t < 0.75:
        for j in range(1, 6):
            tx = x - j * 18 * (1 if k < 1 else 0)
            if tx > 160:
                a = int(120 * (1 - j / 6) * (1 - seg(t, 0.55, 0.75)))
                d.ellipse(S(tx - 10, 128 - 10, tx + 10, 128 + 10), fill=ACCENT + (a,))
    thumb(d, (x if t < 0.8 else 160 + 140 * (1 - seg(t, 0.8, 1.0)), 136))
    d.polygon(S(352, 102, 352, 154, 384, 128), fill=ACCENT + (int(255 * seg(t, 0.2, 0.4) * (1 - seg(t, 0.7, 0.85))),))
    save(img, "swipe", i)

    # Poke: index finger pushes a button, the button lights up.
    img, d = card("Touch", "Press any button with a finger")
    push = seg(t, 0.15, 0.4) * (1 - seg(t, 0.6, 0.85))
    pressed = 0.38 < t < 0.62
    d.rounded_rectangle(S(300, 92, 400, 152), radius=18 * SS, fill=(27, 58, 138) if pressed else (52, 84, 170))
    d.text(S(350, 122)[0:2], "OK", font=REG, fill=WHITE, anchor="mm")
    ox = 40 * push  # the hand moves forward; the fingertip stops on the button's edge
    d.rounded_rectangle(S(40 + ox, 100, 140 + ox, 190), radius=36 * SS, fill=SKIN)
    d.rounded_rectangle(S(90 + ox, 106, 256 + ox, 138), radius=16 * SS, fill=SKIN)
    ripple(d, 300, 122, (t - 0.38) / 0.3, LEMON)
    save(img, "poke", i)

    # Pinch & drag: thumb and index close on the window bar, then move it.
    img, d = card("Pinch & drag", "Move the window anywhere")
    close = seg(t, 0.1, 0.3) * (1 - seg(t, 0.8, 0.95))
    drag = seg(t, 0.35, 0.7) * (1 - seg(t, 0.85, 1.0))
    bx = 150 + 120 * drag
    held = close > 0.9
    gap = 30 * (1 - close)
    d.rounded_rectangle(S(bx - 70, 114, bx + 70, 126), radius=6 * SS, fill=(255, 255, 255, 255 if held else 150))
    d.line(S(bx, 98 - gap, bx - 56, 44 - gap / 2), fill=SKIN, width=28 * SS)          # index
    d.line(S(bx, 142 + gap, bx - 56, 178 + gap / 3), fill=SKIN_DARK, width=28 * SS)   # thumb
    d.ellipse(S(bx - 15, 84 - gap, bx + 15, 112 - gap), fill=SKIN)
    d.ellipse(S(bx - 15, 128 + gap, bx + 15, 156 + gap), fill=SKIN_DARK)
    save(img, "pinch", i)

print("ok")
