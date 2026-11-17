"""Animated explainers for the demo video, drawn with PIL (PNG sequences, 30 fps).
  detect/  full screen: the app finds readable text by itself and puts a button right below it
  swipe/   full screen: thumb swipe right = next page, left = previous page
  gestures/tap, gestures/swipe  small looping cards shown over the footage
Usage: python3 -I schemas.py <fonts_dir> <out_dir> [preview]   (preview = a few still frames only)"""
import os
import sys
from PIL import Image, ImageChops, ImageDraw, ImageFilter, ImageFont

FONTS, OUT = sys.argv[1], sys.argv[2]
PREVIEW = len(sys.argv) > 3
SS, W, H, FPS = 2, 1920, 1080, 30
INK, PANEL, CREAM, PAPER = (21, 23, 28), (34, 38, 47), (251, 245, 230), (247, 240, 225)
NAVY, ACCENT, LEMON, WHITE, MUTED = (27, 58, 138), (111, 160, 245), (242, 207, 59), (255, 255, 255), (167, 174, 187)
BLUE, RED, GREY = (31, 95, 191), (192, 57, 43), (120, 114, 104)
_fonts = {}


def F(bold, size):
    key = (bold, round(size * SS))
    if key not in _fonts:
        name = "AtkinsonHyperlegible-Bold.ttf" if bold else "AtkinsonHyperlegible-Regular.ttf"
        _fonts[key] = ImageFont.truetype(os.path.join(FONTS, name), key[1])
    return _fonts[key]


def clamp(t):
    return max(0.0, min(1.0, t))


def seg(t, a, b):
    """Smooth 0..1 progress of t inside [a, b] (seconds)."""
    k = clamp((t - a) / (b - a))
    return k * k * (3 - 2 * k)


def back(k):
    """Ease-out-back: overshoots a little, for things that pop in."""
    k = clamp(k) - 1
    return 1 + 2.7 * k * k * k + 1.7 * k * k


def px(*v):
    return [round(x * SS) for x in v]


def rgba(c, a=1.0):
    return tuple(c) + (int(255 * clamp(a)),)


def text(d, x, y, s, size, color, bold=True, anchor="la", a=1.0):
    if a < 0.02:  # PIL draws text with alpha 0 as opaque
        return
    d.text(px(x, y), s, font=F(bold, size), fill=rgba(color, a), anchor=anchor)


def caps(d, x, y, s, size, color, a=1.0):
    f = F(True, size)
    cx = x * SS
    for ch in s:
        d.text((cx, y * SS), ch, font=f, fill=rgba(color, a))
        cx += d.textlength(ch, font=f) + 0.16 * size * SS


def syll(d, x, y, line, size, a=1.0):
    """'Dan·iel fold·ed' with syllables in alternating colors, like the app's Syllables mode."""
    f = F(True, size)
    cx, i = x * SS, 0
    for word in line.split(" "):
        for s in word.split("·"):
            d.text((cx, y * SS), s, font=f, fill=rgba((BLUE, RED)[i % 2], a))
            cx += d.textlength(s, font=f) + 0.05 * size * SS
            i += 1
        cx += 0.42 * size * SS


def put(base, layer, a=1.0, at=(0, 0)):
    if a <= 0:
        return
    mask = layer.getchannel("A")
    if a < 1:
        mask = mask.point(lambda v: int(v * a))
    base.paste(layer, px(*at), mask)


def panel(box, radius, fill, blur=26, off=16):
    """Full-canvas RGBA layer: a rounded panel with a soft drop shadow."""
    sh = Image.new("L", (W * SS, H * SS), 0)
    ImageDraw.Draw(sh).rounded_rectangle(px(box[0], box[1] + off, box[2], box[3] + off), radius=radius * SS, fill=130)
    layer = Image.new("RGBA", (W * SS, H * SS), (0, 0, 0, 0))
    layer.putalpha(sh.filter(ImageFilter.GaussianBlur(blur * SS)))
    d = ImageDraw.Draw(layer)
    d.rounded_rectangle(px(*box), radius=radius * SS, fill=rgba(fill))
    return layer, d


def bezier(start, *cubics, n=24):
    """Sample a chain of cubic Bezier segments (each: c1, c2, end) into a polyline."""
    pts, p0 = [start], start
    for c1, c2, p3 in cubics:
        for i in range(1, n + 1):
            t = i / n
            pts.append(tuple((1 - t) ** 3 * a + 3 * (1 - t) ** 2 * t * b + 3 * (1 - t) * t * t * c + t ** 3 * d
                             for a, b, c, d in zip(p0, c1, c2, p3)))
        p0 = p3
    return pts


HAND_W, HAND_H = 800, 760  # hand layer, in units (the drawing itself is designed on a 400x300 grid, doubled)
FIST = bezier((400, 232), ((352, 214), (312, 190), (292, 160)), ((282, 128), (256, 104), (220, 99)),
              ((180, 93), (122, 95), (96, 103)), ((70, 111), (58, 134), (72, 151)), ((61, 161), (60, 181), (75, 191)),
              ((68, 202), (71, 219), (87, 227)), ((85, 239), (94, 252), (111, 256)), ((150, 266), (200, 263), (232, 251)),
              ((272, 262), (322, 292), (350, 322)))
CREASES = [bezier((72, 151), ((110, 154), (150, 152), (178, 146))), bezier((75, 191), ((110, 194), (148, 192), (174, 186))),
           bezier((87, 227), ((115, 230), (145, 228), (166, 222)))]


def hand(u, tip=300, lift=0.0, glow=0.0, fill=INK):
    """Line-art hand as its owner sees it: one cream outline for the curled fist, the thumb in yellow lying on the index.
    tip = where the thumb touches the index (232 = near the fingertip, 400 = near the knuckle), lift = thumb raised."""
    k = 2 * u * SS
    img = Image.new("RGBA", (round(HAND_W * u * SS), round(HAND_H * u * SS)), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)

    def path(pts):
        return [(x * k, (y + 37) * k) for x, y in pts]

    def stroke(pts, color, w):
        pts = path(pts)
        d.line(pts, fill=color, width=round(w * k), joint="curve")
        for x, y in (pts[0], pts[-1]):
            d.ellipse([x - w * k / 2, y - w * k / 2, x + w * k / 2, y + w * k / 2], fill=color)

    d.polygon(path(FIST + [(400, 322)]), fill=fill)
    stroke(FIST, CREAM, 5)
    for c in CREASES:
        stroke(c, (148, 145, 137), 4)
    x, y, r = tip / 2 + 3, 91 - 30 * lift, 17
    if glow > 0:
        gx, gy = path([(x, y)])[0]
        d.ellipse([gx - 30 * k, gy - 30 * k, gx + 30 * k, gy + 30 * k], fill=rgba(LEMON, 0.35 * glow))
    thumb = bezier((286, 142), ((262, 104), (x + 46, y - r - 7), (x, y - r)),
                   ((x - r * 1.35, y - r), (x - r * 1.35, y + r), (x, y + r)), ((x + 36, y + r + 2), (238, 130), (270, 176)))
    d.polygon(path(thumb), fill=fill)
    stroke(thumb, LEMON, 5)
    # fade the wrist out (it leaves through the right and bottom edges) so the cut never shows
    w, h = img.size
    ramp = lambda a, b: (lambda v: max(0, min(255, int(255 * (b - v / 255) / (b - a)))))  # 255 before a, 0 after b
    fade = ImageChops.multiply(Image.linear_gradient("L").rotate(90).resize((w, h)).point(ramp(0.70, 0.92)),
                               Image.linear_gradient("L").resize((w, h)).point(ramp(0.74, 0.90)))
    img.putalpha(Image.composite(img.getchannel("A"), Image.new("L", (w, h), 0), fade))
    return img


def arrow(d, x0, x1, y, color, a=1.0, w=10):
    """Horizontal arrow from x0 to x1 (either direction)."""
    s = 1 if x1 > x0 else -1
    d.line(px(x0, y, x1 - s * 14, y), fill=rgba(color, a), width=w * SS)
    d.polygon(px(x1, y, x1 - s * 30, y - 22, x1 - s * 30, y + 22), fill=rgba(color, a))


def ripple(d, x, y, k, color, r0=20, r1=90):
    if 0 < k < 1:
        r = r0 + (r1 - r0) * k
        d.ellipse(px(x - r, y - r, x + r, y + r), outline=rgba(color, 1 - k), width=5 * SS)


def background(kicker, title):
    im = Image.new("RGB", (W * SS, H * SS), INK)
    d = ImageDraw.Draw(im, "RGBA")
    caps(d, 110, 92, kicker, 26, ACCENT)
    text(d, 110, 132, title, 68, CREAM)
    return im


def finish(im):
    return im.resize((W, H), Image.LANCZOS)


# ---------------------------------------------------------------- 1. automatic detection
BODY = ("Daniel folded a paper boat and placed it in the puddle by the playground. A breeze pushed the boat past a "
        "pebble, under a bridge of branches, and round a bend. Daniel jumped and splashed beside it, laughing. "
        "The boat bobbed and drifted, brave and bright, until it bumped into a duck.")
PAGE_BOX, ZONE = (170, 262, 690, 830), (196, 296, 664, 598)
BADGE = (430, 690)  # centre of the "Page detected" badge: right below the text zone
D_STEPS = [("Hold up any page", "A book, a worksheet, a screen"),
           ("ReadLens finds the text", "It looks by itself, every second"),
           ("A button appears right below", "No menu, nothing to aim at"),
           ("Tap your thumb", "The page opens in your own style")]
D_LEN = 7.6


def detect_layers():
    page, d = panel(PAGE_BOX, 10, PAPER)
    text(d, 212, 312, "The Paper Boat", 27, (40, 36, 30))
    f, y, line = F(False, 18.5), 362, ""
    for wd in BODY.split():
        trial = (line + " " + wd).strip()
        if d.textlength(trial, font=f) > 436 * SS:
            text(d, 212, y, line, 18.5, GREY, bold=False)
            line, y = wd, y + 27
        else:
            line = trial
    text(d, 212, y, line, 18.5, GREY, bold=False)
    win, d = panel((150, 262, 710, 770), 38, CREAM)
    for i, s in enumerate(("Dan·iel fold·ed", "a pa·per boat", "and placed it", "in the pud·dle")):
        syll(d, 200, 316 + i * 104, s, 54)
    d.rounded_rectangle(px(360, 800, 500, 811), radius=6 * SS, fill=rgba(WHITE, 0.85))
    return page, win


def badge(d, s, a, pressed):
    cx, cy = BADGE

    def box(x0, y0, x1, y1):
        return px(cx + x0 * s, cy + y0 * s, cx + x1 * s, cy + y1 * s)

    d.rounded_rectangle(box(-225, -52, 225, 52), radius=36 * s * SS, fill=rgba(NAVY, a))
    text(d, cx - 196 * s, cy - 14 * s, "Page detected", 25 * s, WHITE, anchor="lm", a=a)
    text(d, cx - 196 * s, cy + 17 * s, "or tap your thumb", 18 * s, (214, 224, 246), anchor="lm", a=a)
    d.rounded_rectangle(box(8, -36, 208, 36), radius=24 * s * SS, fill=rgba(WHITE if pressed else (207, 222, 250), a))
    text(d, cx + 108 * s, cy, "Read this page", 21 * s, (20, 30, 60), anchor="mm", a=a)


def steps(d, items, step, x=1050, y0=326, gap=132):
    for i, (title, sub) in enumerate(items):
        n, y = i + 1, y0 + i * gap
        a = 1.0 if n == step else 0.5 if n < step else 0.22
        if n == step:
            d.ellipse(px(x, y, x + 56, y + 56), fill=rgba(ACCENT))
        else:
            d.ellipse(px(x, y, x + 56, y + 56), outline=rgba(MUTED, a), width=3 * SS)
        text(d, x + 28, y + 28, str(n), 28, INK if n == step else MUTED, anchor="mm", a=1 if n == step else a)
        text(d, x + 84, y - 6, title, 42, WHITE, a=a)
        text(d, x + 84, y + 48, sub, 27, MUTED, bold=False, a=a)


def detect(t, bg, page, win):
    im = bg.copy()
    d = ImageDraw.Draw(im, "RGBA")
    steps(d, D_STEPS, 1 + (t >= 1.2) + (t >= 3.0) + (t >= 4.5))
    appear, morph = seg(t, 0.1, 0.7), seg(t, 5.4, 6.2)
    put(im, page, appear * (1 - morph), at=(0, 30 * (1 - appear)))
    put(im, win, morph)
    if 1.2 < t < 2.4:  # scan sweep down the page
        k = seg(t, 1.2, 2.3)
        y = PAGE_BOX[1] + (PAGE_BOX[3] - PAGE_BOX[1]) * k
        a = min(1, (1 - k) * 6, k * 6)
        for j in range(60):
            d.rectangle(px(PAGE_BOX[0], y - j - 1, PAGE_BOX[2], y - j), fill=rgba(ACCENT, 0.32 * a * (1 - j / 60)))
        d.rectangle(px(PAGE_BOX[0], y, PAGE_BOX[2], y + 3), fill=rgba(ACCENT, a))
    z = seg(t, 1.9, 2.5)
    za = z * (1 - morph)
    if za > 0:  # corner brackets snap onto the text zone
        g = 46 * (1 - z)
        x0, y0, x1, y1 = ZONE[0] - g, ZONE[1] - g, ZONE[2] + g, ZONE[3] + g
        d.rounded_rectangle(px(x0, y0, x1, y1), radius=12 * SS, fill=rgba(LEMON, 0.16 * za))
        for cx, cy, sx, sy in ((x0, y0, 1, 1), (x1, y0, -1, 1), (x0, y1, 1, -1), (x1, y1, -1, -1)):
            d.line(px(cx + sx * 44, cy, cx, cy, cx, cy + sy * 44), fill=rgba((214, 150, 0), za), width=7 * SS, joint="curve")
        text(d, ZONE[0], ZONE[1] - 12 - g, "TEXT FOUND", 17, (150, 100, 0), anchor="ls", a=seg(t, 2.3, 2.6) * (1 - morph))
    b = clamp((t - 3.0) / 0.45)
    if b > 0 and morph < 1:
        badge(d, 0.6 + 0.4 * back(b), min(1, b * 3) * (1 - morph), 4.95 < t < 5.25)
    ha = seg(t, 4.4, 4.75) * (1 - seg(t, 5.7, 6.1))
    if ha > 0:  # the thumb tap that presses the button
        lift = 1 - seg(t, 4.8, 4.95) + seg(t, 5.2, 5.45)
        put(im, hand(0.36, tip=300, lift=lift, glow=1 - lift if t > 4.8 else 0), ha, at=(712, 596))
        ripple(d, BADGE[0] + 108, BADGE[1], (t - 4.95) / 0.5, WHITE, 30, 120)
    return finish(im)


# ---------------------------------------------------------------- 2. thumb swipe
WIN = (1010, 292, 1750, 700)
S_LEN = 7.6
PAGES = [("Dan·iel fold·ed", "a pa·per boat", "and placed it"), ("A breeze pushed", "the boat past", "a peb·ble")]


def swipe_layers():
    win, d = panel(WIN, 38, CREAM)
    d.rounded_rectangle(px(1310, 730, 1450, 741), radius=6 * SS, fill=rgba(WHITE, 0.85))
    w, h = WIN[2] - WIN[0], WIN[3] - WIN[1]
    pages = []
    for n, lines in enumerate(PAGES):
        im = Image.new("RGBA", (w * SS, h * SS), (0, 0, 0, 0))
        d = ImageDraw.Draw(im)
        for i, s in enumerate(lines):
            syll(d, 56, 42 + i * 92, s, 56)
        text(d, w / 2, h - 44, f"Page {n + 1} of {len(PAGES)}", 26, GREY, anchor="mm")
        pages.append(im)
    clip = Image.new("L", (w * SS, h * SS), 0)
    ImageDraw.Draw(clip).rounded_rectangle([0, 0, w * SS, h * SS], radius=38 * SS, fill=255)
    return win, pages, clip


def chip(d, x, right, title, sub, on):
    d.rounded_rectangle(px(x, 800, x + 360, 912), radius=28 * SS, fill=rgba(NAVY if on > 0.5 else PANEL))
    c = WHITE if on > 0.5 else MUTED
    arrow(d, x + (34 if right else 100), x + (100 if right else 34), 856, LEMON if on > 0.5 else MUTED, w=8)
    text(d, x + 128, 822, title, 32, c)
    text(d, x + 128, 864, sub, 25, c, bold=False, a=0.9)


def swipe(t, bg, win, pages, clip):
    im = bg.copy()
    d = ImageDraw.Draw(im, "RGBA")
    right, left = seg(t, 1.2, 1.8), seg(t, 4.0, 4.6)
    tip = 232 + 168 * (right - left)                      # thumb slides toward the knuckle, then back
    lift = 0.35 * (seg(t, 2.1, 2.4) * (1 - seg(t, 3.5, 3.9)) + seg(t, 4.9, 5.2))  # relax between swipes
    moving = (0 < right < 1) or (0 < left < 1)
    put(im, hand(0.95, tip=tip, lift=lift, glow=1.0 if moving else 0.0), at=(110, 358))
    ar, al = seg(t, 1.2, 1.5) * (1 - seg(t, 2.4, 2.9)), seg(t, 4.0, 4.3) * (1 - seg(t, 5.2, 5.7))
    if ar > 0:
        arrow(d, 330, 330 + 170 * max(0.25, right), 478, LEMON, ar)
        text(d, 330, 418, "Swipe right", 34, LEMON, a=ar)
    if al > 0:
        arrow(d, 500, 500 - 170 * max(0.25, left), 478, LEMON, al)
        text(d, 500, 418, "Swipe left", 34, LEMON, anchor="ra", a=al)
    put(im, win)
    slide = seg(t, 1.4, 2.0) - seg(t, 4.2, 4.8)           # 0 = page 1, 1 = page 2
    w = WIN[2] - WIN[0]
    inner = Image.new("RGBA", clip.size, (0, 0, 0, 0))
    inner.paste(pages[0], (round(-slide * w * SS), 0))
    inner.paste(pages[1], (round((1 - slide) * w * SS), 0))
    im.paste(inner, px(WIN[0], WIN[1]), Image.composite(inner.getchannel("A"), Image.new("L", clip.size, 0), clip))
    chip(d, 1010, True, "Swipe right", "Next page", 1 if 1.2 <= t < 3.6 else 0)
    chip(d, 1390, False, "Swipe left", "Previous page", 1 if 4.0 <= t < 6.4 else 0)
    text(d, 1380, 968, "In Focus mode, the same swipe moves one sentence at a time.", 27, MUTED, bold=False,
         anchor="mm", a=seg(t, 5.6, 6.2))
    return finish(im)


# ---------------------------------------------------------------- 3. small looping cards over the footage
CW, CH, C_LEN = 500, 280, 3.0


def gesture_card(name, t):
    im = Image.new("RGB", (CW * SS, CH * SS), (24, 27, 33))
    d = ImageDraw.Draw(im, "RGBA")
    if name == "tap":
        lift = 1 - seg(t, 0.7, 0.9) + seg(t, 1.3, 1.6)
        im.paste(h := hand(0.31, tip=300, lift=lift, glow=clamp(1 - lift), fill=(24, 27, 33)), px(8, 22), h)
        ripple(d, 8 + 300 * 0.31, 22 + 256 * 0.31, (t - 0.9) / 0.6, LEMON, 14, 56)
        title, lines = "Thumb tap", ("Reads the", "detected page")
    else:
        right, left = seg(t, 0.3, 0.8), seg(t, 1.7, 2.2)
        im.paste(h := hand(0.31, tip=232 + 168 * (right - left), glow=1.0 if 0 < right < 1 or 0 < left < 1 else 0.0, fill=(24, 27, 33)), px(8, 22), h)
        ar, al = seg(t, 0.3, 0.5) * (1 - seg(t, 1.1, 1.4)), seg(t, 1.7, 1.9) * (1 - seg(t, 2.5, 2.8))
        if ar > 0:
            arrow(d, 78, 78 + 60 * max(0.4, right), 62, LEMON, ar, w=6)
        if al > 0:
            arrow(d, 138, 138 - 60 * max(0.4, left), 62, LEMON, al, w=6)
        title, lines = "Thumb swipe", ("Right: next", "Left: back")
    text(d, 280, 84, title, 32, WHITE)
    for i, s in enumerate(lines):
        text(d, 280, 134 + i * 32, s, 24, MUTED, bold=False)
    im = im.resize((CW, CH), Image.LANCZOS).convert("RGBA")
    mask = Image.new("L", (CW * SS, CH * SS), 0)
    ImageDraw.Draw(mask).rounded_rectangle([0, 0, CW * SS, CH * SS], radius=30 * SS, fill=235)
    im.putalpha(mask.resize((CW, CH), Image.LANCZOS))
    return im


# ---------------------------------------------------------------- 4. end card: the wall of text gives way to the slogan
E_LEN = 6.5
SLOGAN = ("Ev·ery page", "can be", "read·a·ble.")


def end_layers():
    wall = Image.new("RGBA", (W * SS, H * SS), (0, 0, 0, 0))
    d = ImageDraw.Draw(wall)
    f, y, line = F(False, 31), 60, ""
    for wd in (BODY + " ").split() * 12:
        trial = (line + " " + wd).strip()
        if d.textlength(trial, font=f) > 1660 * SS:
            text(d, 130, y, line, 31, (142, 136, 124), bold=False)
            line, y = wd, y + 37
            if y > 1010:
                break
        else:
            line = trial
    logo = Image.open(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "docs", "brand", "readlens-wordmark-light.png"))
    return wall, logo.resize((round(118 * SS * logo.width / logo.height), 118 * SS), Image.LANCZOS)


def end(t, wall, logo):
    im = Image.new("RGB", (W * SS, H * SS), CREAM)
    d = ImageDraw.Draw(im, "RGBA")
    k = seg(t, 0.9, 2.3)                      # a scan line wipes the cramped page from top to bottom
    y = round(H * k)
    put(im, wall, 0.07)
    if y < H:
        low = wall.crop((0, y * SS, W * SS, H * SS))
        im.paste(low, (0, y * SS), low)
    if 0 < k < 1:
        for j in range(70):
            d.rectangle(px(0, y - j - 1, W, y - j), fill=rgba(BLUE, 0.22 * (1 - j / 70)))
        d.rectangle(px(0, y, W, y + 4), fill=rgba(BLUE))
    for i, line in enumerate(SLOGAN):         # the slogan, set the way the app sets a page
        a = seg(t, 1.5 + i * 0.35, 2.1 + i * 0.35)
        if a > 0:
            syll(d, 130, 150 + i * 205 + 40 * (1 - a), line, 172, a)
    a = seg(t, 3.2, 3.9)
    if a > 0:
        put(im, logo, a, at=(126, 880))
        text(d, 1790, 940, "Meta Quest 3 · 3S     On-device     No photo stored", 30, (90, 86, 78), bold=False, anchor="rm", a=a)
    return finish(im)


def render(name, seconds, frame):
    folder = os.path.join(OUT, name)
    os.makedirs(folder, exist_ok=True)
    if PREVIEW:
        for i in range(8):
            frame(seconds * (i + 0.5) / 8).save(os.path.join(OUT, f"preview_{name.replace('/', '_')}_{i}.png"))
        return
    for i in range(round(seconds * FPS)):
        frame(i / FPS).save(os.path.join(folder, f"{i:03d}.png"), compress_level=1)
    print(name, "ok")


bg = background("AUTOMATIC DETECTION", "It finds the text for you.")
page, win = detect_layers()
render("detect", D_LEN, lambda t: detect(t, bg, page, win))
bg2 = background("HANDS ONLY  ·  NO CONTROLLER", "Turn pages with your thumb.")
win2, pages, clip = swipe_layers()
render("swipe", S_LEN, lambda t: swipe(t, bg2, win2, pages, clip))
for g in ("tap", "swipe"):
    render(f"gestures/{g}", C_LEN, lambda t, g=g: gesture_card(g, t))
wall, logo = end_layers()
render("end", E_LEN, lambda t: end(t, wall, logo))
