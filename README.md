# ReadLens

Mixed-reality reading aid for kids with dyslexia, built for Meta Quest 3/3S.
Look at a printed page, pinch, and read the same page rewritten with *your* reading profile
(font, spacing, syllable colours) in a panel you scroll with your hands.

Meta VR Start Developer Competition 2026 — Track **Productivity**, Division **New Experience**.
Deadline: **Nov 18, 2026, 12:00 PT (21:00 Paris)**. Code freeze: Nov 18, 12:00 Paris.

## Decisions

| Topic | Choice | Why |
| --- | --- | --- |
| Engine | Unity 6000.6.5f1, Meta XR Core + Interaction + MRUK v209 | Headset runs Horizon OS v209; official Passthrough Camera samples are Unity. |
| Camera | MRUK `PassthroughCameraAccess` | Public API, Quest 3/3S. Not supported by XR Simulator: test on device. |
| OCR | **On-device**: Google ML Kit text recognition (bundled model). Fallback: Tesseract. | Free, offline, nothing leaves the headset (kids' documents). Returns word boxes, needed for the page overlay. |
| Text rendering | TextMeshPro + OpenDyslexic (SIL OFL) | Spacing per character, word, line; per-character colour. |
| Language | Everything in English (UI, video, form). Syllable rules: EN + FR. | Competition rule. |

## Costs

All zero: Unity Personal, Meta SDKs, Developer Dashboard and release channels, ML Kit / Tesseract,
OpenDyslexic, Devpost, YouTube. A cloud OCR would only be needed if on-device OCR fails on the
headset, and would need a billing account.

## Data and access

- Camera permission `horizonos.permission.HEADSET_CAMERA` and scene permission (table detection) are asked at first launch.
- Frames are processed in memory on the headset, never stored, never sent.
- Judges get the app through the invite URL of the `Competition` release channel.

## Milestones

- [x] **M0 Foundations** — repo, Unity project, Meta packages, formatting engine + tests
- [ ] **M1 Hello headset** — XR setup, passthrough, reader panel with sample text, running on the Quest 3S
- [ ] **M2 Capture + OCR** — camera frame → on-device OCR → text in the panel
- [ ] **M3 Hands** — pinch to capture, scroll/grab panel, profile editor with live preview, saved profile
- [ ] **M4 Polish** — panel anchored on the detected table (MRUK), 3-step onboarding, sounds, reading ruler, 72 fps
- [ ] **M5 Stretch: on-page overlay** — adapted text laid over the real page at capture time (OCR word boxes + camera-to-world)
- [ ] **M6 Ship** — APK in `Competition` channel, invite URL tested, video, Devpost form

## Video (under 3:00, aim for 2:15–2:30)

| Time | Shot |
| --- | --- |
| 0:00–0:15 | A dense real page, as a kid sees it. The problem in one sentence. |
| 0:15–0:50 | Pinch → the same page reappears, airy and coloured. The wow moment. |
| 0:50–1:30 | Set a profile with hands, live preview. |
| 1:30–2:05 | Scroll, reading ruler, panel resting on the desk (or on the page). |
| 2:05–2:30 | Why it matters + what's next. |

Record in-headset (built-in recorder or Meta Quest Developer Hub cast), voice-over in English afterwards.
No identifiable child on screen. Demo pages written by us.

## Layout

- `Assets/ReadLens/Scripts/Core` — pure C#: profile, syllables, rich-text formatter (no Unity dependency)
- `Assets/ReadLens/Scripts/App` — Unity components (reader panel, …)
- `Assets/ReadLens/Tests/Editor` — EditMode tests

Run tests: Unity → Window → General → Test Runner → EditMode → Run All.

## Build and run on the Quest

```bash
U=/Applications/Unity/Hub/Editor/6000.6.5f1/Unity.app/Contents/MacOS/Unity
$U -batchmode -buildTarget Android -projectPath . -executeMethod ReadLens.EditorTools.Bootstrap.Setup   # once, configures XR/Meta/OpenXR
$U -batchmode -quit -buildTarget Android -projectPath . -executeMethod ReadLens.EditorTools.Bootstrap.Build
adb install -r -g Builds/ReadLens.apk
```

Same actions in the editor: menu **ReadLens → Setup Project / Build APK**.
If `adb devices` says `offline`: wake the headset and accept "Allow USB debugging" inside it.
