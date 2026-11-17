"""ReadLens demo edit v4: chapters that slide in, animated explainers (schemas.py), crossfades, quiet music bed.
Usage: python3 -I edit.py <take.mp4> <assets_dir> <work_dir> <out_dir>
assets_dir holds intro.png, endcard.png, chapter_*.png, detect/, swipe/, gestures/<name>/ (%03d.png) and music.wav."""
import os
import subprocess
import sys

TAKE, A, WORK, OUT = sys.argv[1:5]
os.makedirs(WORK, exist_ok=True)
XF = 0.35  # crossfade seconds
ENC = ["-c:v", "libx264", "-preset", "medium", "-crf", "18", "-pix_fmt", "yuv420p", "-r", "30",
       "-c:a", "aac", "-b:a", "192k", "-ar", "48000", "-ac", "2"]
GX, GY = 1920 - 500 - 56, 1080 - 280 - 56  # gesture card, bottom right

# name, start, end (in the take), chapter, speed, slow zoom, gesture, gesture start (s into the clip)
CLIPS = [
    ("capture", 108, 118, "capture", 1.0, True, "tap", 1.0),
    ("comfort_a", 43, 50.5, "comfort", 1.5, False, None, 0),
    ("comfort_b", 54, 62, None, 1.5, False, None, 0),
    ("handwriting", 84, 90, "handwriting", 1.0, True, None, 0),
    ("focus", 90, 101, "focus", 1.0, True, "swipe", 1.5),
    ("pages", 118, 126, "pages", 1.0, False, None, 0),
    ("settings", 129, 141, "settings", 1.3, False, None, 0),
    ("contrast", 165, 174, "contrast", 1.2, True, None, 0),
    ("thumb", 191, 196, "thumb", 1.0, False, "swipe", 0.5),
    ("move", 202, 214, "move", 1.25, False, None, 0),
]


def run(args):
    subprocess.run(["ffmpeg", "-nostdin", "-v", "error", "-y"] + args, check=True)


def duration(path):
    out = subprocess.run(["ffprobe", "-v", "error", "-show_entries", "format=duration", "-of", "csv=p=0", path],
                         capture_output=True, text=True, check=True).stdout
    return float(out)


def card(name, image, seconds):
    path = os.path.join(WORK, f"{name}.mp4")
    run(["-loop", "1", "-t", str(seconds), "-i", image, "-f", "lavfi", "-t", str(seconds), "-i", "anullsrc=r=48000:cl=stereo",
         "-vf", "scale=1920:1080,fps=30", *ENC, "-shortest", path])
    return path


def scene(name):
    """Full-screen animated explainer from a PNG sequence."""
    path = os.path.join(WORK, f"{name}.mp4")
    run(["-framerate", "30", "-i", os.path.join(A, name, "%03d.png"), "-f", "lavfi", "-i", "anullsrc=r=48000:cl=stereo",
         *ENC, "-shortest", path])
    return path


def clip(name, s, e, chapter, speed, zoom, gesture, g_start):
    path = os.path.join(WORK, f"{name}.mp4")
    dur = (e - s) / speed
    inputs = ["-ss", str(s), "-to", str(e), "-i", TAKE]
    v = f"[0:v]fps=30,scale=1920:1080,setpts=PTS/{speed}"
    if zoom:  # slow push-in; upscale first so zoompan does not jitter
        v += ",scale=3840:2160,zoompan=z='min(1+0.0009*on,1.08)':x='iw/2-(iw/zoom/2)':y='ih/2-(ih/zoom/2)':d=1:s=1920x1080:fps=30"
    graph = [v + "[b0]"]
    last, idx = "b0", 1
    if chapter:
        inputs += ["-loop", "1", "-t", f"{dur:.3f}", "-i", os.path.join(A, f"chapter_{chapter}.png")]
        graph.append(f"[{idx}:v]format=rgba,fade=in:st=0.3:d=0.35:alpha=1,fade=out:st=3.4:d=0.4:alpha=1[c]")
        # the title slides in from the left as it fades in
        graph.append(f"[{last}][c]overlay=x='if(lt(t,0.3),-90,if(lt(t,0.75),-90+90*(t-0.3)/0.45,0))':y=0:eval=frame:shortest=1[b1]")
        last, idx = "b1", idx + 1
    if gesture:
        g_end = dur - 0.3
        inputs += ["-stream_loop", "-1", "-framerate", "30", "-t", f"{dur:.3f}", "-i", os.path.join(A, "gestures", gesture, "%03d.png")]
        graph.append(f"[{idx}:v]format=rgba,setpts=PTS+{g_start}/TB,fade=in:st={g_start}:d=0.3:alpha=1,"
                     f"fade=out:st={g_end - 0.3:.3f}:d=0.3:alpha=1[g]")
        graph.append(f"[{last}][g]overlay=x={GX}:y={GY}:eof_action=pass:enable='between(t,{g_start},{g_end:.3f})'[b2]")
        last = "b2"
    graph.append(f"[0:a]volume=0.3,atempo={speed}[a]")
    run(inputs + ["-filter_complex", ";".join(graph), "-map", f"[{last}]", "-map", "[a]", *ENC, "-t", f"{dur:.3f}", path])
    print(f"{name:12s} {dur:5.1f}s")
    return path


parts = [card("00_intro", os.path.join(A, "intro.png"), 6), scene("detect")]
for i, c in enumerate(CLIPS):
    if c[0] == "thumb":
        parts.append(scene("swipe"))
    parts.append(clip(f"{i + 1:02d}_{c[0]}", *c[1:]))
parts.append(card("99_end", os.path.join(A, "endcard.png"), 5.5))

# Chain everything with short crossfades (video) and matching audio crossfades.
inputs, vf, af, offset = [], [], [], 0.0
for p in parts:
    inputs += ["-i", p]
durs = [duration(p) for p in parts]
v_prev, a_prev = "0:v", "0:a"
for i in range(1, len(parts)):
    offset += durs[i - 1] - XF
    vf.append(f"[{v_prev}][{i}:v]xfade=transition=fade:duration={XF}:offset={offset:.3f}[v{i}]")
    af.append(f"[{a_prev}][{i}:a]acrossfade=d={XF}[a{i}]")
    v_prev, a_prev = f"v{i}", f"a{i}"
total = offset + durs[-1]
music = os.path.join(A, "music.wav")
mix = (f"[{len(parts)}:a]atrim=0:{total:.3f},afade=t=out:st={total - 3:.3f}:d=3,volume=0.18[m];"
       f"[{a_prev}][m]amix=inputs=2:duration=first:normalize=0[mix]")
graph = ";".join(vf + af) + ";" + mix

run(inputs + ["-i", music, "-filter_complex", graph, "-map", f"[{v_prev}]", "-map", "[mix]", *ENC,
              "-movflags", "+faststart", os.path.join(OUT, "ReadLens-edit-v4.mp4")])
run(inputs + ["-filter_complex", ";".join(vf + af), "-map", f"[{v_prev}]", "-map", f"[{a_prev}]", *ENC,
              "-movflags", "+faststart", os.path.join(OUT, "ReadLens-edit-v4-no-music.mp4")])
run(["-i", music, "-t", f"{total:.3f}", "-af", f"afade=t=out:st={total - 3:.3f}:d=3",
     os.path.join(OUT, "ReadLens-music.wav")])
print(f"total {total:.1f}s")
