"""Animated explainers for the demo video, drawn with PIL (PNG sequences, 30 fps).
  detect/  full screen: the app finds readable text by itself and puts a button right below it
  swipe/   full screen: thumb swipe right = next page, left = previous page
  gestures/tap, gestures/swipe  small looping cards shown over the footage
Usage: python3 -I schemas.py <fonts_dir> <out_dir> [preview]   (preview = a few still frames only)"""
import os
import sys
from PIL import Image, ImageDraw, ImageFilter, ImageFont

FONTS, OUT = sys.argv[1], sys.argv[2]
PREVIEW = len(sys.argv) > 3
SS, W, H, FPS = 2, 1920, 1080, 30
INK, PANEL, CREAM, PAPER = (21, 23, 28), (34, 38, 47), (251, 245, 230), (247, 240, 225)
NAVY, ACCENT, LEMON, WHITE, MUTED = (27, 58, 138), (111, 160, 245), (242, 207, 59), (255, 255, 255), (167, 174, 187)
BLUE, RED, GREY = (31, 95, 191), (192, 57, 43), (120, 114, 104)
SKIN, SKIN2, LINE = (241, 224, 206), (222, 200, 178), (140, 112, 94)
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


def limb(d, pts, w, color):
    d.line([c for p in pts for c in p], fill=color, width=round(w), joint="curve")
    for x, y in pts:
        d.ellipse([x - w / 2, y - w / 2, x + w / 2, y + w / 2], fill=color)


HAND_W, HAND_H = 800, 760  # hand design box, in units


def hand(u, tip=300, lift=0.0, glow=0.0):
    """Right hand as its owner sees it: fingers curled, index pointing left, thumb resting on top of it.
    tip = where the thumb touches the index (220 = near the fingertip, 400 = near the knuckle), lift = thumb raised."""
    k = u * SS
    img = Image.new("RGBA", (round(HAND_W * k), round(HAND_H * k)), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)

    def p(x, y):
        return (x * k, y * k)

    ol = 5 * k
    arm, index = [p(800, 960), p(560, 440)], [p(470, 305), p(270, 290), p(186, 328), p(214, 386)]
    for grow, color in ((ol, LINE), (0, SKIN)):  # one outline around arm + palm + index
        limb(d, arm, 150 * k + 2 * grow, color)
        d.rounded_rectangle([410 * k - grow, 270 * k - grow, 640 * k + grow, 520 * k + grow], radius=95 * k + grow, fill=color)
        limb(d, index, 64 * k + 2 * grow, color)
    for y, x0, w in ((368, 262, 58), (428, 286, 54), (484, 316, 46)):  # the three other fingers, curled
        limb(d, [p(455, y), p(x0, y)], w * k + 2 * ol, LINE)
        limb(d, [p(455, y), p(x0, y)], w * k, SKIN2)
    tp = p(tip, 256 - 58 * lift)
    thumb = [p(590, 345), p(478 + 0.45 * (tip - 300), 226 - 24 * lift), tp]
    if glow > 0:
        r = 54 * k
        d.ellipse([tp[0] - r, tp[1] - r, tp[0] + r, tp[1] + r], outline=rgba(LEMON, glow), width=round(7 * k))
    limb(d, thumb, 60 * k + 2 * ol, LINE)
    limb(d, thumb, 60 * k, SKIN)
    d.ellipse([tp[0] - 20 * k, tp[1] - 24 * k, tp[0] + 16 * k, tp[1] + 6 * k], fill=(250, 238, 226))  # nail
    # fade the wrist out so the cut never shows
    fade = Image.new("L", img.size, 255)
    fd = ImageDraw.Draw(fade)
    h0 = int(img.size[1] * 0.80)
    for y in range(h0, img.size[1]):
        fd.line([0, y, img.size[0], y], fill=int(255 * (1 - (y - h0) / (img.size[1] - h0))))
    img.putalpha(Image.composite(img.getchannel("A"), Image.new("L", img.size, 0), fade))
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
        im.paste(h := hand(0.31, tip=300, lift=lift, glow=clamp(1 - lift)), px(8, 22), h)
        ripple(d, 8 + 300 * 0.31, 22 + 256 * 0.31, (t - 0.9) / 0.6, LEMON, 14, 56)
        title, lines = "Thumb tap", ("Reads the", "detected page")
    else:
        right, left = seg(t, 0.3, 0.8), seg(t, 1.7, 2.2)
        im.paste(h := hand(0.31, tip=232 + 168 * (right - left), glow=1.0 if 0 < right < 1 or 0 < left < 1 else 0.0), px(8, 22), h)
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
