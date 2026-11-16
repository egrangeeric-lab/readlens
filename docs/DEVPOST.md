# ReadLens: Devpost submission copy

Ready to paste into the Devpost form. Everything below describes what is in the build today
(checked against `Assets/ReadLens/Scripts` and `Assets/ReadLens/Editor/Bootstrap.cs`).

- **Track:** Productivity
- **Division:** New Experience
- **Special awards we fit best:** Best Accessibility Forward Experience (main target), Best First Five Minutes, Boldest Original Concept
- **Deadline:** Wed Nov 18 2026, 12:00 PT / 21:00 Paris

---

## Submission name

ReadLens

## Tagline (max 140 characters, pick one)

1. `Hold up any printed page and read it again in your own font, spacing and colours. Mixed-reality reading help for kids with dyslexia.` (132 characters)
2. `A mixed-reality reading lens for kids with dyslexia: frame a real page with your hands, read it rewritten the way your eyes like it.` (132 characters)
3. `Any book, your way: ReadLens turns the printed page in your hands into text shaped for a dyslexic reader. On-device, hands-only.` (128 characters)

Recommended: option 1 (clearest about who it is for and what happens).

---

## Description (about 500 words, the suggested maximum)

### Inspiration

For a child with dyslexia, a school page is often the hardest object in the room: letters crowd together, b turns into d, lines slide into each other. Adapted fonts, wider spacing and coloured syllables help many of these readers, but only on a screen, and only for texts someone has typed in. The homework sheet and the library book stay as they are. We wanted the real page to change, without retyping anything.

### What it does

ReadLens is a mixed-reality reading aid for Meta Quest 3 and 3S. The child holds a printed page in passthrough. A "Page detected" badge appears under it; a tap of the thumb on the index finger, or a poke on the badge, reads it. The same text then reappears in a window, rewritten with the child's own reading profile: OpenDyslexic, Atkinson Hyperlegible or a classic font, adjustable size, letter, word and line spacing, page and text colours, colouring by syllables (English and French rules), by words or by lines, and b / d / p / q highlighted.

There are three hands-only ways to capture: the live badge, a two-hand "photographer frame" that reads only the framed area, and a Capture button. Pages stack like a book: one page at a time, turned with < > or a thumb swipe. Focus mode shows one large sentence at a time.

The first launch is "Find your comfort": four A/B questions on the same sentence (letters, spacing, colours, size). Four taps, and the profile is built. Everything stays editable in Settings, with a live preview and a WCAG contrast check that warns when a colour choice gets hard to read.

### How we built it

Unity 6 with Meta XR Core, Interaction SDK and MRUK. Frames from the Passthrough Camera API (MRUK `PassthroughCameraAccess`) are read back asynchronously and go to Google ML Kit text recognition running on the headset through a small Java plugin. OCR line boxes are projected back with the frame's camera pose, so the badge sits under the real text and the frame gesture keeps only the lines inside your hands. The text is reflowed, then rendered by a pure C# engine (syllabifier, rich-text formatter, sentence splitter, contrast maths) covered by EditMode tests. The window uses Interaction SDK poke and ray on every control and a grab handle to move it. It is rendered as a compositor layer (ISDK `CanvasRenderTexture` + `OVRCanvasMeshRenderer`, supersampled) for sharp text, at 90 Hz.

### Challenges

Keeping small text sharp in a headset; turning OCR lines and hyphenated words back into paragraphs; detecting a two-hand frame from joint ratios so it works for children's hands.

### Accessibility and privacy

Accessibility is the product, not a setting. No reading is needed to set it up: Find your comfort compares, it does not ask. Every action works with hands, near or far. Frames are processed in memory on the headset and never stored or sent; only the reading profile is saved locally.

### What's next

Laying the adapted text directly over the paper, text-to-speech with word highlighting, progress tracking shared with speech therapists, and more languages.

---

## How hand interactions are implemented (optional field)

ReadLens is hands-only end to end; no controller is ever needed.

- **Poke and ray everywhere.** Every button, swatch, stepper and A/B card is a Unity UI element on a canvas with Interaction SDK poke (touch it) and ray (pinch from afar) interactions. Touch targets are sized for poking.
- **Live page detection + thumb tap.** While the Read tab is open, ReadLens reads a passthrough camera frame about once per second on-device. When it finds enough text, a "Page detected" badge appears just under the text in the real world (OCR box projected through the frame's camera pose). The child can poke "Read this page", or keep both hands on the book and tap the thumb against the index finger (Meta microgesture `ThumbTap` via `OVRHand.GetMicrogestureType`).
- **Photographer frame.** Both hands make an L shape (index and thumb out, other fingers folded) around the text. We detect it from Interaction SDK hand joints using ratios of palm length, not centimetres, so small hands work. An outline brightens as the pose is held; after 0.8 s the area is captured and only the OCR lines whose centre falls inside the frame are kept.
- **Thumb swipe to turn pages.** Microgesture `SwipeLeft` / `SwipeRight` turns pages, or moves sentence by sentence in Focus mode. The interaction rig's snap-turn locomotion is disabled so the same gesture never rotates the user.
- **Grab to move.** A thin Quest-style bar under the window lights up when a hand comes near; grab it with a near pinch or a ray grab. If the window stays out of view for 2.5 s it glides back in front of the reader. Grabbing always wins.
- **Seated by design.** Everything happens within arm's reach of a desk or a book on your lap.

## What excites you about the future of VR?

Headsets are starting to see what we see. With camera access, a headset can do more than show another world: it can help with the one in front of you. For a child who struggles with reading, the paper does not have to change; the way they see it can. We are excited by mixed reality that is quiet, private and personal, and that helps with ordinary daily moments like homework.

## Target launch date

**Q2 2027 (placeholder, the team must confirm).** Reasoning: a closed beta with families and speech therapists in Q1 2027, then a store release once OCR accuracy on real school pages and the syllable rules have been checked with them.

## Built with (tags)

unity, c#, meta-xr-sdk, meta-interaction-sdk, mruk, passthrough-camera-api, hand-tracking, microgestures, ml-kit, android, textmeshpro, opendyslexic, atkinson-hyperlegible

## Testing instructions for judges (if the form asks)

1. Open the invite link, install, launch. Allow camera access when asked.
2. Answer the four "Find your comfort" questions by poking the card you prefer (or press Skip).
3. Hold a printed page (large font works best, good light) about 40 cm in front of you. When "Page detected" appears, poke "Read this page" or tap your thumb on your index finger.
4. Or put the page on a desk, make an L with each hand around the text and hold still for a second.
5. Capture a second page, then slide your thumb along your index finger to turn pages. Try Focus.
6. Open Settings to change font, spacing and colours; watch the contrast line.

---

## Still needs a human decision

- [ ] Team members: names, emails and roles for the form.
- [ ] Target launch date: confirm or change "Q2 2027".
- [ ] Final tagline choice.
- [ ] Video link (YouTube or Vimeo, public) and the Competition release channel invite URL.
- [ ] Screenshots / thumbnail for the gallery (captured in-headset).
- [ ] If pages-across-sessions persistence, sounds or the scan effect land before the code freeze, add one line about them under "What it does"; if not, leave the text as it is.
- [ ] Note for the team: the judging criteria mention MRUK scene understanding (anchoring to planes/surfaces). The current build uses MRUK only for camera access; the window comes back in front of the reader when lost, but is not anchored to a detected table. Do not claim table anchoring unless it ships.
