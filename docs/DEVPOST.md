# ReadLens: Devpost form, field by field

Copy each block into the matching field. Fields are in the order of the form. Everything describes what is in the build today.
Lines marked **YOU** need your own answer.

Deadline: Wed Nov 18 2026, 12:00 PT / 21:00 Paris.

---

## Elevator pitch (200 characters max)

```
Hold up any printed page and read it again in your own font, spacing and colors. A hands-only mixed-reality reading aid for children with dyslexia, on Meta Quest 3.
```

## Thumbnail

`docs/devpost/thumbnail.png` (1500 × 1000, 3:2).

## About the project (Markdown, paste as is)

```markdown
## Inspiration

About **1 child in 14** has dyslexia: a 2022 meta-analysis of 58 studies found it in 7.1% of primary school children, with the same rate across writing systems. That is two children in a typical classroom.

For those children, a school page can feel like a wall: letters crowd together, b turns into d, lines slide into each other. Research shows that wider letter spacing alone cuts reading errors for dyslexic children, and every reader has their own comfortable font, size and colors. But those settings only exist on screens, for text someone has typed in. The homework sheet and the library book stay as they are.

We wanted the real page to change, without retyping anything.

## What it does

ReadLens is a mixed-reality reading aid for Meta Quest 3 and 3S.

- **It finds the text for you.** Hold up a page in passthrough. ReadLens looks about once per second, and when it sees readable text, a "Page detected" button appears right under it.
- **One thumb tap reads it.** Tap your thumb on your index finger, or touch the button. Both hands can stay on the book.
- **The page comes back in your own style.** The same text reappears in a floating window with your reading profile: three fonts (OpenDyslexic, Atkinson Hyperlegible, a classic one), size, letter, word and line spacing, page and text colors, coloring by syllables (English and French), words or lines, and b / d / p / q highlighted.
- **Pages turn like a book.** Each capture is a page. Swipe your thumb right for the next one, left for the previous one. Pages are kept for the next session.
- **Focus mode** shows one large sentence at a time.
- **Find your comfort.** The first launch asks four simple "this one or that one?" questions on the same sentence. Four taps build the profile. Everything stays editable in Settings, with a live preview and a contrast check that warns when colors get hard to read.

It also reads handwriting, and you can frame part of a page with both hands to capture only that part.

## How we built it

Unity 6 with Meta XR Core SDK, Interaction SDK and MR Utility Kit (v209), OpenXR.

- **Seeing the page:** frames from the Passthrough Camera API are read back asynchronously and sent to Google ML Kit text recognition, running on the headset through a small Java plugin. No cloud, no network permission at all.
- **Placing the button:** OCR line boxes are projected back into the room with the camera pose of that frame, so the button sits under the real text.
- **Rewriting the page:** OCR lines are reflowed into paragraphs, then rendered by a pure C# engine (syllable splitter, rich-text formatter, sentence splitter, contrast math) covered by 26 automated tests.
- **Sharp text:** the window is drawn as a compositor layer with supersampling, at a steady 90 Hz.
- **Hands only:** Interaction SDK poke and ray on every control, thumb microgestures for tap and swipe, a grab bar to move the window.

## Challenges we ran into

- Small text is hard to read in a headset. Moving the window to a compositor layer and raising the render scale made the difference.
- OCR returns loose lines. Turning them back into paragraphs, with hyphenated words rejoined, took several passes.
- Capturing while holding a book: pressing a button is impractical with full hands. Automatic detection plus a thumb tap solved it.
- The thumb swipe also triggered the default snap-turn of the interaction rig, so the room rotated. We disabled locomotion.

## Accomplishments that we're proud of

Accessibility is the product, not a setting. Setting it up needs no reading: you compare two versions and pick one. All presets pass the WCAG 4.5:1 contrast ratio. Camera frames are processed in memory on the headset and never stored or sent; only the reading profile and the captured text are saved, locally.

## What we learned

The best interaction is the one you do not have to look for. The first version had a Capture button; testers holding a page never found a free hand for it. Letting the app notice the page changed everything.

We also learned to be careful with claims. The research supports wider spacing; it does not show that any special font helps every reader. So ReadLens offers choices and lets each child decide.

## What's next

Laying the adapted text directly over the paper, reading aloud with word highlighting, more languages, and trials with families and speech therapists.

## Sources

- Yang et al., *Prevalence of Developmental Dyslexia in Primary School Children: A Systematic Review and Meta-Analysis*, Brain Sciences, 2022.
- Zorzi et al., *Extra-large letter spacing improves reading in dyslexia*, PNAS, 2012.
```

## Built with (tags, up to 25)

```
unity, c#, meta-quest, meta-xr-sdk, interaction-sdk, mr-utility-kit, passthrough-camera-api, passthrough, hand-tracking, microgestures, openxr, ml-kit, ocr, android, java, textmeshpro, accessibility, mixed-reality
```

## "Try it out" links

**YOU:** the `Competition` release channel invite URL from the Meta dashboard.

## Image gallery (3:2, with captions of 140 characters max)

| File | Caption |
| --- | --- |
| `docs/devpost/1-page-detected.jpg` | `ReadLens spots readable text by itself and puts a "Page detected" button right under the page. One thumb tap reads it.` |
| `docs/devpost/2-your-page.jpg` | `The same page, rewritten in your own reading profile: bigger letters, wider spacing, colored syllables. Pages turn like a book.` |
| `docs/devpost/3-focus.jpg` | `Focus mode shows one sentence at a time. A thumb swipe moves to the next one, so both hands can stay on the book.` |
| `docs/devpost/4-settings.jpg` | `Every reader is different: three fonts, size, spacing and colors, with a live preview and a contrast check.` |

## Demonstration video

**YOU:** the YouTube or Vimeo link, public or unlisted, no password, under 3 minutes.

## Submitter type

**YOU:** `Individual` if you enter alone, `Team of Individuals` if the person behind the idea is on the entry.

## Country of residence

**YOU.**

## Email of your developer organization / Meta VR Start membership

**YOU:** the email of the Meta account that is a Start member and owns the app on the dashboard.

## Team members (names, emails, roles)

**YOU**, only if you chose a team. Format: `Name — email — role`.

## Name of your team or organization

**YOU.** It is displayed as "ReadLens created by <name>".

## Track

`Productivity`

## Division

`New` — the project was built from scratch during the competition period. Leave the "Adapted or Significantly Updated" summary empty.

## Build path

`Unity`

## URL of your Meta VR competition release channel

**YOU:** the same invite URL as in "Try it out".

## What Meta VR capability(ies) did you use in your project?

```
- Passthrough (mixed reality): the whole app runs over the real room.
- Passthrough Camera API (MR Utility Kit PassthroughCameraAccess): camera frames and camera pose, to read printed text and to place UI under the real page.
- Hand tracking: the app is hands-only, no controller is ever needed.
- Microgestures (OVRHand): thumb tap to read the detected page, thumb swipe left / right to turn pages.
- Interaction SDK: poke and ray on every control, hand grab and ray grab to move the window.
- Compositor layers (OVROverlay underlay, supersampled) for sharp text.
- Meta XR Core SDK on OpenXR, 90 Hz, Quest 3 and Quest 3S.
```

## Describe how Hand Interactions are implemented into your experience

```
ReadLens is hands-only from the first second to the last. No controller is ever needed.

1. Thumb tap to read. ReadLens checks the passthrough camera about once per second. When it finds readable text, a "Page detected" button appears right under the real page. Tapping the thumb on the index finger (the ThumbTap microgesture) reads the page, so both hands can stay on the book.

2. Thumb swipe to turn pages. Sliding the thumb right along the index finger goes to the next page, left to the previous one (SwipeRight / SwipeLeft microgestures). In Focus mode the same swipe moves one sentence at a time. The interaction rig's snap-turn is disabled so the gesture never rotates the user.

3. Touch and pinch everywhere. Every button, color swatch, stepper and choice card supports Interaction SDK poke (touch it) and ray (pinch from a distance). Targets are large, and each press gives visual and audio feedback.

4. Two-hand frame. Making an L with each hand around a part of the page captures only that part. The pose is detected from hand joint ratios, not fixed distances, so it works with small hands.

5. Grab to move. A thin bar under the window lights up when a hand comes near. Pinch it, near or from a distance, to move the window. If the window ends up out of view, it glides back in front of the reader.

Everything is designed for a seated child holding a book: small gestures, within arm's reach, no menu to aim at.
```

## What makes you excited about the future of VR? (150 words suggested)

```
Headsets are starting to see what we see. With camera access, a headset can do more than show another world: it can help with the one in front of you.

For a child who struggles with reading, the paper does not have to change. The way they see it can. The same idea reaches far beyond dyslexia: low vision, a foreign language, a form that is hard to understand.

What excites us most is mixed reality that is quiet, private and personal. It runs on the device, asks for nothing, and helps with ordinary moments like homework. Hands-only input makes it natural enough for a child to use without being taught.
```

## Target launch date

**YOU must confirm.** Suggested answer:

```
Q2 2027, after a closed beta with families and speech therapists in early 2027.
```

---

## Not asked by the form, but keep at hand

**Testing instructions for judges** (put them in the release notes of the build or in the video description):

1. Open the invite link, install, launch. Allow camera access when asked.
2. Answer the four "Find your comfort" questions by touching the card you prefer, or press Skip.
3. Hold a printed page, with good light, about 40 cm in front of you. When "Page detected" appears, tap your thumb on your index finger or touch "Read this page".
4. Capture a second page, then slide your thumb along your index finger to turn pages. Try Focus.
5. Open Settings to change font, spacing and colors; watch the contrast line.

**Do not claim:** table or surface anchoring. The app uses MR Utility Kit for camera access only.
