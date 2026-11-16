"""ReadLens brand assets (logo B "La page qui respire", app palette) + video overlays, drawn with PIL.
Usage: python3 -I make_brand.py <fonts_dir> <brand_out_dir> <video_assets_out_dir>"""
import os
import sys
from PIL import Image, ImageDraw, ImageFont

FONTS, BRAND, VID = sys.argv[1], sys.argv[2], sys.argv[3]
os.makedirs(BRAND, exist_ok=True)
os.makedirs(VID, exist_ok=True)

NAVY, INK, CREAM = "#1B3A8A", "#15171C", "#FBF5E6"
BLUE, RED, GREEN, GREY = "#1F5FBF", "#C0392B", "#13723D", "#9A9488"
BOLD = os.path.join(FONTS, "AtkinsonHyperlegible-Bold.ttf")
REG = os.path.join(FONTS, "AtkinsonHyperlegible-Regular.ttf")
SS = 4  # supersampling for smooth edges


def font(path, size):
    return ImageFont.truetype(path, size * SS)


def finish(img, w, h):
    return img.resize((w, h), Image.LANCZOS)


def mark(d, ox, oy, u, page_fill, page_line, lens_ring, lines=GREY):
    """Logo mark in a 64x72 unit box: a grey, tight page; under the lens, airy coloured syllables."""
    def p(x, y):
        return (ox + x * u, oy + y * u)

    def rr(x, y, w, h, r, fill):
        d.rounded_rectangle([p(x, y), p(x + w, y + h)], radius=r * u, fill=fill)

    # page with folded corner
    d.polygon([p(2, 2), p(44, 2), p(58, 16), p(58, 70), p(2, 70)], fill=page_fill)
    d.line([p(2, 2), p(44, 2), p(58, 16), p(58, 70), p(2, 70), p(2, 2)], fill=page_line, width=int(3 * u), joint="curve")
    d.line([p(44, 2), p(44, 16), p(58, 16)], fill=page_line, width=int(3 * u), joint="curve")
    for y, w in ((22, 22), (30, 24), (38, 20), (46, 23), (54, 18)):
        rr(9, y, w, 3.5, 1.75, lines)
    # lens
    cx, cy, r = 45, 46, 21
    d.ellipse([p(cx - r, cy - r), p(cx + r, cy + r)], fill="#FFFFFF" if page_fill != INK else CREAM, outline=lens_ring, width=int(5 * u))
    rr(32, 37, 11, 6, 3, BLUE)
    rr(46, 37, 10, 6, 3, RED)
    rr(32, 49, 15, 6, 3, GREEN)


# 1) App icon 1024x1024: cream square, mark centred.
S = 1024 * SS
img = Image.new("RGBA", (S, S), CREAM)
d = ImageDraw.Draw(img)
u = S / 96
mark(d, (S - 60 * u) / 2, (S - 72 * u) / 2, u, "#FFFFFF", INK, NAVY)
finish(img, 1024, 1024).save(os.path.join(BRAND, "readlens-icon-1024.png"))

# 2) Wordmarks (transparent): mark + "ReadLens", light and dark versions.
for name, page, line, ring, text in (("light", "#FFFFFF", INK, NAVY, INK), ("dark", "#2A2E36", CREAM, "#8FA8F0", CREAM)):
    W, H = 1400 * SS, 320 * SS
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    u = H / 80
    mark(d, 8 * u, 4 * u, u, page, line, ring)
    d.text((80 * u, H / 2), "ReadLens", font=font(BOLD, 190), fill=text, anchor="lm")
    finish(img, 1400, 320).save(os.path.join(BRAND, f"readlens-wordmark-{name}.png"))

# 3) Video end card 1920x1080.
W, H = 1920 * SS, 1080 * SS
img = Image.new("RGB", (W, H), INK)
d = ImageDraw.Draw(img)
u = 5.2 * SS
mx = W / 2 - (60 * u + 40 * SS + d.textlength("ReadLens", font=font(BOLD, 150))) / 2
mark(d, mx, H * 0.30, u, "#2A2E36", CREAM, "#8FA8F0")
d.text((mx + 60 * u + 40 * SS, H * 0.30 + 36 * u), "ReadLens", font=font(BOLD, 150), fill=CREAM, anchor="lm")
d.text((W / 2, H * 0.70), "Every page can be readable.", font=font(BOLD, 64), fill="#E8E3D6", anchor="mm")
d.text((W / 2, H * 0.79), "Meta Quest 3 · 3S   ·   On-device   ·   No photo stored", font=font(REG, 34), fill="#A7AEBB", anchor="mm")
finish(img, 1920, 1080).save(os.path.join(VID, "endcard.png"))

# 4) Intro card 1920x1080: a tight grey page, then the line that frames the problem.
img = Image.new("RGB", (W, H), CREAM)
d = ImageDraw.Draw(img)
dense = ("Daniel folded a paper boat and placed it in the puddle by the playground. A breeze pushed the boat past a "
         "pebble, under a bridge of branches, and round a bend. Daniel jumped and splashed beside it, laughing. Quick, "
         "before it disappears! shouted his big sister Bella. The boat bobbed and drifted, brave and bright, until it "
         "bumped into a duck. The puzzled duck quacked, then paddled away.")
f = font(REG, 40)
words, line, y = (dense + " " + dense).split(), "", 70 * SS
for wd in words:
    trial = (line + " " + wd).strip()
    if d.textlength(trial, font=f) > 1640 * SS:
        d.text((140 * SS, y), line, font=f, fill="#8E887C")
        line, y = wd, y + 44 * SS
        if y > 1000 * SS: break
    else:
        line = trial
d.text((140 * SS, y), line, font=f, fill="#8E887C")
d.rounded_rectangle([(260 * SS, 640 * SS), (1660 * SS, 900 * SS)], radius=28 * SS, fill=INK)
d.text((W / 2, 725 * SS), "For a child with dyslexia,", font=font(BOLD, 64), fill=CREAM, anchor="mm")
d.text((W / 2, 815 * SS), "a page like this can feel like a wall.", font=font(BOLD, 64), fill=CREAM, anchor="mm")
finish(img, 1920, 1080).save(os.path.join(VID, "intro.png"))

# 5) Chapter overlays (transparent 1920x1080): soft dark corner + number line + title, bottom left.
CHAPTERS = {
    "capture": ("01 · CAPTURE", ["Hold up a page.", "Tap your thumb."]),
    "comfort": ("02 · FIND YOUR COMFORT", ["Four simple choices"]),
    "handwriting": ("03 · ANY PRINTED OR WRITTEN TEXT", ["Even handwriting"]),
    "focus": ("04 · FOCUS", ["One sentence at a time"]),
    "pages": ("05 · PAGES", ["Turn them like a book"]),
    "settings": ("06 · YOUR WAY", ["Fonts, spacing, colors"]),
    "contrast": ("07 · CONTRAST CHECK", ["Warns when colors", "get hard to read"]),
    "thumb": ("08 · THUMB SWIPE", ["Next page, one finger"]),
    "move": ("09 · ANYWHERE", ["Grab the bar,", "move the window"]),
}
w, h = 1920, 1080
shade = Image.new("L", (w, h))
px = shade.load()
for yy in range(h):
    vy = max(0.0, min(1.0, (yy - 520) / 480))
    for xx in range(w):
        vx = max(0.0, min(1.0, 1 - xx / 1500))
        px[xx, yy] = int(200 * vy * vx)
for key, (num, title) in CHAPTERS.items():
    big = Image.new("RGBA", (w * SS, h * SS), (0, 0, 0, 0))
    d = ImageDraw.Draw(big)
    fn, ft = font(BOLD, 28), font(BOLD, 60)
    x, y = 110 * SS, (970 - 72 * len(title)) * SS
    for ch in num:  # tracked caps
        d.text((x, y - 56 * SS), ch, font=fn, fill=(255, 255, 255, 215))
        x += d.textlength(ch, font=fn) + 4 * SS
    for i, t in enumerate(title):
        d.text((110 * SS, y + i * 72 * SS), t, font=ft, fill=(255, 255, 255, 255))
    out = Image.new("RGBA", (w, h), (21, 23, 28, 0))
    out.putalpha(shade)
    out.alpha_composite(finish(big, w, h))
    out.save(os.path.join(VID, f"chapter_{key}.png"))

print("ok")
