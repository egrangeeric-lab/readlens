"""Original, royalty-free background music for the demo video: soft pad chords + a light plucked arpeggio.
Usage: python3 -I music.py <seconds> <out.wav>"""
import sys
import wave
import numpy as np

SECONDS, OUT = float(sys.argv[1]), sys.argv[2]
SR = 48000
BEAT = 60 / 72                # 72 bpm, calm
BAR = 4 * BEAT
# I - vi - IV - V in C major, voiced warm and low (MIDI notes)
CHORDS = [[48, 55, 60, 64, 67], [45, 52, 57, 60, 64], [41, 48, 53, 57, 60], [43, 50, 55, 59, 62]]


def hz(m):
    return 440.0 * 2 ** ((m - 69) / 12)


n = int(SECONDS * SR)
t = np.arange(n) / SR
pad = np.zeros(n)
arp = np.zeros(n)

bars = int(np.ceil(SECONDS / BAR))
for b in range(bars):
    chord = CHORDS[b % 4]
    start = int(b * BAR * SR)
    length = int(BAR * SR * 1.25)          # overlap into the next bar for a smooth change
    end = min(n, start + length)
    tt = t[: end - start]
    env = np.minimum(1, tt / 1.2) * np.exp(-np.maximum(0, tt - BAR) * 3)  # slow attack, soft release
    for m in chord:
        f = hz(m)
        # two slightly detuned sines + a quiet octave: a soft, chorus-like pad
        pad[start:end] += env * (np.sin(2 * np.pi * f * tt) + 0.7 * np.sin(2 * np.pi * f * 1.003 * tt)
                                 + 0.15 * np.sin(2 * np.pi * 2 * f * tt)) / len(chord)
    # arpeggio: eighth notes up the chord, an octave higher, plucked
    for k in range(8):
        m = chord[[1, 2, 3, 4, 3, 2, 3, 4][k]] + 12
        s = start + int(k * BEAT / 2 * SR)
        e = min(n, s + int(1.2 * SR))
        if s >= n:
            break
        tt2 = t[: e - s]
        f = hz(m)
        pluck = np.exp(-tt2 * 5) * (np.sin(2 * np.pi * f * tt2) + 0.3 * np.sin(2 * np.pi * 2 * f * tt2))
        arp[s:e] += pluck * (0.9 if k % 2 == 0 else 0.6)

mix = 0.55 * pad + 0.07 * arp  # arpeggio barely there: a voice-over sits on top
# gentle one-pole low-pass to round off the highs
alpha = 0.11
y = np.empty_like(mix)
acc = 0.0
for i in range(n):  # shortcut: python loop is slow (~5 s for 100 s of audio); fine for a one-off render
    acc += alpha * (mix[i] - acc)
    y[i] = acc
fade_in, fade_out = int(1.5 * SR), int(3.0 * SR)
y[:fade_in] *= np.linspace(0, 1, fade_in)
y[-fade_out:] *= np.linspace(1, 0, fade_out)
y = y / np.max(np.abs(y)) * 0.5
stereo = np.stack([y, np.roll(y, int(0.012 * SR))], axis=1)  # tiny delay for width
pcm = (stereo * 32767).astype(np.int16)
with wave.open(OUT, "wb") as w:
    w.setnchannels(2)
    w.setsampwidth(2)
    w.setframerate(SR)
    w.writeframes(pcm.tobytes())
print("ok")
