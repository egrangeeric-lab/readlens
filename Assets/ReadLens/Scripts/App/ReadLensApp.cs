using System;
using System.Collections.Generic;
using System.IO;
using ReadLens.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ColorMode = ReadLens.Core.ColorMode;

namespace ReadLens.App
{
    // The ReadLens window: Read tab (captured pages, Focus mode), Settings tab (profile + live preview),
    // first-run "Find your comfort", plus the "Page detected" suggestion and the frame gesture.
    public class ReadLensApp : MonoBehaviour
    {
        [SerializeField] RectTransform canvas;
        [SerializeField] RectTransform badgeCanvas;
        [SerializeField] Transform head;
        [SerializeField] Font openDyslexic, atkinson, atkinsonBold;
        [SerializeField] TMP_FontAsset classic;
        [SerializeField] OcrService ocr;
        [SerializeField] FrameGesture frameGesture;
        [SerializeField] WindowFollow follow;
        [SerializeField] ThumbGestures thumbGestures;
        [SerializeField] Icons icons;
        [SerializeField] ScanEffect scan;

        const string Sample = "The big dog barely picked a quiet path past the pond.";
        const string Welcome =
            "Welcome to ReadLens!\n\nHold a printed page in front of you. When ReadLens sees text, a button appears under it: press Read this page.\n\n" +
            "You can also frame a page on your desk with both hands, like a photographer, or press Capture page.\n\n" +
            "To turn pages, close your hand loosely and slide your thumb along the side of your index finger: right for the next page, left to go back.";
        const string Hint = "Hold a page in front of you, or frame it with both hands.";
        const int MinChars = 30;            // less than this is noise, not a page
        const float ScanInterval = 1f;      // live text detection rate
        const float DetectionLifetime = 2.5f;

        static readonly string[] PageColors = { "#FBF5E6", "#FFFFFF", "#E8F1FB", "#EAF6EA", "#FFF3C4", "#1E1E1E" };
        static readonly string[] TextColors = { "#1E1E1E", "#3A3A3A", "#1B2A49", "#FFFFFF" };
        static readonly string[] Inks = { "#1F5FBF", "#C0392B", "#13723D", "#7B3FB5", "#A84300", "#1E1E1E" };

        ReadingProfile profile;
        string profilePath;
        readonly List<string> pages = new List<string>();
        int currentPage;
        readonly Dictionary<string, TMP_FontAsset> fonts = new Dictionary<string, TMP_FontAsset>();

        GameObject readView, settingsView, comfortView, tabs, normalActions, focusActions, pageNav;
        Image paper, previewPaper, focusPaper;
        TextMeshProUGUI readerText, previewText, focusText, focusCount, status, contrastLabel, pageLabel;
        Button previousPage, nextPage;
        ScrollRect readerScroll;
        Button pinButton;
        Action refreshSettings = () => { };
        Action<int> selectPreset = _ => { }, selectTab = _ => { };
        Fade readFade, settingsFade, comfortFade, badgeFade;
        Slide pageTurn, sentenceTurn;

        bool focusOn;
        List<string> sentences = new List<string>();
        int sentence;

        OcrResult detected;
        float detectedAt = -99f, nextScan;
        bool captureRequested;
        Vector3 badgeTarget;

        // ---------- lifecycle ----------

        void Awake()
        {
            fonts["OpenDyslexic"] = Dynamic(openDyslexic);
            fonts["Atkinson"] = UIKit.Font = Dynamic(atkinson);
            fonts["Classic"] = classic;
            UIKit.FontBold = Dynamic(atkinsonBold);
            profilePath = Path.Combine(Application.persistentDataPath, "profile.json");
            bool firstRun = !File.Exists(profilePath);
            profile = LoadProfile();
            Build();
            BuildBadge();
            refreshSettings();
            frameGesture.OnFramed += OnFramed;
            frameGesture.Enabled = () => readFade.Shown && !focusOn;
            thumbGestures.OnSwipe += OnThumbSwipe;
            thumbGestures.OnTap += OnThumbTap;
            if (firstRun) StartComfort();
            else Render();
        }

        void Update()
        {
            bool reading = readFade.Shown && !focusOn;
            if (reading && ocr.CameraReady && !ocr.Busy && Time.time >= nextScan)
            {
                nextScan = Time.time + ScanInterval;
                ocr.ReadCamera(OnScan, _ => { });
            }
            bool showBadge = reading && detected != null && Time.time - detectedAt < DetectionLifetime;
            if (badgeFade.Shown != showBadge)
            {
                if (!badgeCanvas.gameObject.activeSelf) badgeCanvas.position = badgeTarget;
                badgeFade.Show(showBadge);
            }
            if (badgeCanvas.gameObject.activeSelf)
            {
                badgeCanvas.position = Vector3.Lerp(badgeCanvas.position, badgeTarget, 1f - Mathf.Exp(-8f * Time.deltaTime));
                badgeCanvas.rotation = Quaternion.LookRotation(badgeCanvas.position - head.position);
            }
        }

        TMP_FontAsset Dynamic(Font f)
        {
            var asset = TMP_FontAsset.CreateFontAsset(f);
            asset.fallbackFontAssetTable = new List<TMP_FontAsset> { classic };
            return asset;
        }

        TMP_FontAsset FontFor(string name) => fonts.TryGetValue(name, out var f) ? f : classic;

        // ---------- profile ----------

        ReadingProfile LoadProfile()
        {
            try
            {
                if (File.Exists(profilePath)) return JsonUtility.FromJson<ReadingProfile>(File.ReadAllText(profilePath));
            }
            catch (Exception e)
            {
                Debug.LogWarning("[ReadLens] Profile unreadable, using default: " + e.Message);
            }
            return ReadingProfile.Presets()[0];
        }

        // Any hand-made change turns the profile into "Custom"; picking a preset keeps its name.
        void Changed(bool fromPreset = false)
        {
            if (!fromPreset)
            {
                profile.name = "Custom";
                selectPreset(-1);
            }
            File.WriteAllText(profilePath, JsonUtility.ToJson(profile));
            Render();
        }

        // The page on screen: one captured page at a time, like turning pages in a book.
        string Document => pages.Count == 0 ? Welcome : pages[currentPage];

        void Render()
        {
            pageNav.SetActive(pages.Count > 1 && !focusOn);
            pageLabel.text = $"Page {currentPage + 1} of {pages.Count}";
            previousPage.interactable = currentPage > 0;
            nextPage.interactable = currentPage < pages.Count - 1;
            ProfileRenderer.Apply(readerText, paper, Document, profile, FontFor);
            ProfileRenderer.Apply(previewText, previewPaper, Sample, profile, FontFor);
            if (focusOn) RenderFocus();

            double worst = Contrast.Worst(profile);
            var ratio = worst.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture); // English UI on any device locale
            contrastLabel.text = worst >= Contrast.Comfortable ? $"Contrast {ratio} : 1  -  easy to read"
                : worst >= Contrast.Minimum ? $"Contrast {ratio} : 1  -  readable"
                : $"Low contrast {ratio} : 1  -  may be hard to read. Try another color.";
            contrastLabel.color = worst >= Contrast.Minimum ? UIKit.Muted : UIKit.Warning;
            contrastLabel.fontStyle = worst >= Contrast.Minimum ? FontStyles.Normal : FontStyles.Bold;
        }

        // ---------- capture ----------

        void OnScan(OcrResult r)
        {
            var text = r.ReflowedText;
            if (text.Length >= MinChars && r.Bounds(out var box))
            {
                detected = r;
                detectedAt = Time.time;
                // Badge just under the text.
                var ray = ocr.PixelToRay(r, new Vector2(box.center.x, box.yMax));
                badgeTarget = ray.GetPoint(OcrService.AssumedPageDistance) + Vector3.down * 0.05f;
            }
            if (!captureRequested) return;
            captureRequested = false;
            r.Bounds(out var region);
            AddPage(text, r, region);
        }

        void Capture()
        {
            if (detected != null && Time.time - detectedAt < 1.5f)
            {
                ReadDetected();
                return;
            }
            if (!ocr.CameraReady)
            {
                status.text = "The camera is starting. If asked, allow camera access, then try again.";
                return;
            }
            status.text = "Reading your page...";
            captureRequested = true;
            if (!ocr.Busy) ocr.ReadCamera(OnScan, OnOcrError);
        }

        void ReadDetected()
        {
            if (detected == null) return;
            detected.Bounds(out var region);
            AddPage(detected.ReflowedText, detected, region);
        }

        // Both hands framed an area: read only the lines inside it.
        void OnFramed(Vector3 a, Vector3 b)
        {
            status.text = "Reading the framed area...";
            ocr.ReadCamera(r =>
            {
                Vector2 pa = ocr.WorldToPixel(r, a), pb = ocr.WorldToPixel(r, b);
                var framed = Rect.MinMaxRect(Mathf.Min(pa.x, pb.x), Mathf.Min(pa.y, pb.y), Mathf.Max(pa.x, pb.x), Mathf.Max(pa.y, pb.y));
                AddPage(r.TextInside(framed), r, framed);
            }, OnOcrError);
        }

        void OnOcrError(string error)
        {
            captureRequested = false;
            Debug.LogWarning("[ReadLens] OCR failed: " + error);
            status.text = "Sorry, the page couldn't be read. Please try again.";
        }

        // source + pixels: the camera frame and text region the page came from, for the scan effect.
        void AddPage(string text, OcrResult source, Rect pixels)
        {
            Debug.Log($"[ReadLens] page: {text.Length} chars");
            if (text.Length < MinChars)
            {
                status.text = "No text found. Hold the page a little closer, in good light, and try again.";
                return;
            }
            pages.Add(text);
            detected = null;
            status.text = $"Page {pages.Count} added. Use Previous and Next to turn pages, or try Focus.";
            GoToPage(pages.Count - 1);
            if (pages.Count == 1) pageTurn.Play(1); // the welcome text gives way to the first page
            PlayScan(source, pixels);
        }

        // Lights up the page region in the world, then sends it into the window.
        void PlayScan(OcrResult r, Rect px)
        {
            Vector3 At(float x, float y) => ocr.PixelToRay(r, new Vector2(x, y)).GetPoint(OcrService.AssumedPageDistance);
            scan.Play(At(px.xMin, px.yMin), At(px.xMax, px.yMin), At(px.xMin, px.yMax), At(px.xMax, px.yMax), canvas.position);
        }

        void GoToPage(int index)
        {
            int previous = currentPage;
            currentPage = Mathf.Clamp(index, 0, Mathf.Max(0, pages.Count - 1));
            Render();
            readerScroll.verticalNormalizedPosition = 1; // start each page at the top
            if (currentPage != previous) pageTurn.Play(currentPage > previous ? 1 : -1);
        }

        // Thumb swipe: next/previous sentence in Focus, next/previous page otherwise.
        void OnThumbSwipe(int direction)
        {
            if (!readFade.Shown) return;
            if (focusOn)
            {
                MoveSentence(direction);
                return;
            }
            if (pages.Count < 2) return;
            GoToPage(currentPage + direction);
            status.text = $"Page {currentPage + 1} of {pages.Count}";
        }

        // Thumb tap: read the detected page without letting go of it.
        void OnThumbTap()
        {
            if (badgeFade.Shown) ReadDetected();
        }

        void Clear()
        {
            pages.Clear();
            currentPage = 0;
            status.text = Hint;
            Render();
        }

        // ---------- focus mode ----------

        void SetFocus(bool on)
        {
            focusOn = on;
            paper.gameObject.SetActive(!on);
            focusPaper.gameObject.SetActive(on);
            normalActions.SetActive(!on);
            focusActions.SetActive(on);
            if (on)
            {
                sentences = Sentences.Split(Document);
                sentence = 0;
                status.text = "One sentence at a time. Press Next, or slide your thumb to the right.";
            }
            else status.text = Hint;
            Render();
        }

        void MoveSentence(int delta)
        {
            int previous = sentence;
            sentence = Mathf.Clamp(sentence + delta, 0, Mathf.Max(0, sentences.Count - 1));
            RenderFocus();
            if (sentence != previous) sentenceTurn.Play(delta);
        }

        void RenderFocus()
        {
            var big = profile.Clone();
            big.fontSize = profile.fontSize * 1.5f;
            ProfileRenderer.Apply(focusText, focusPaper, sentences.Count == 0 ? "" : sentences[sentence], big, FontFor);
            focusCount.text = $"{sentence + 1} / {Mathf.Max(1, sentences.Count)}";
        }

        // ---------- UI ----------

        void Build()
        {
            var shadow = UIKit.Panel(canvas, new Color(0, 0, 0, 0.28f), 56, "Shadow");
            UIKit.Stretch(shadow.rectTransform, 4, 6, 4, -4);
            var window = UIKit.Panel(canvas, UIKit.Surface, 48, "Window");
            UIKit.Stretch(window.rectTransform, 10, 10, 10, 10);

            var column = UIKit.Stretch(UIKit.Node("Column", window.transform));
            UIKit.Layout<VerticalLayoutGroup>(column, 20, new RectOffset(32, 32, 28, 32));

            var header = UIKit.Layout<HorizontalLayoutGroup>(UIKit.Node("Header", column), 16, null, TextAnchor.MiddleLeft);
            UIKit.Size(header, -1, UIKit.Touch);
            UIKit.Size(UIKit.Label(header.transform, "ReadLens", 46, UIKit.Accent, true), -1, UIKit.Touch, 1);
            var tabsHolder = UIKit.Node("Tabs", header.transform);
            tabs = tabsHolder.gameObject;
            UIKit.Size(tabsHolder, 400, UIKit.Touch);
            UIKit.Layout<HorizontalLayoutGroup>(tabsHolder, 0).childForceExpandWidth = true;
            selectTab = UIKit.Choice(tabsHolder, new[] { "Read", "Settings" }, ShowTab, 32, new[] { icons.read, icons.settings });
            pinButton = UIKit.Button(header.transform, "Pin", TogglePin, 160, UIKit.Touch, 30, icons.pin);

            var body = UIKit.Node("Body", column);
            body.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;
            BuildReadView(body);
            BuildSettingsView(body);
            BuildComfortView(body);
            readFade = Fade.On(readView);
            settingsFade = Fade.On(settingsView);
            comfortFade = Fade.On(comfortView);
            selectTab(0);
            ShowTab(0);
        }

        void ShowTab(int i)
        {
            comfortFade.Show(false);
            readFade.Show(i == 0);
            settingsFade.Show(i == 1);
            Render(); // line colours need the visible layout
        }

        void TogglePin()
        {
            follow.Pinned = !follow.Pinned;
            UIKit.SetSelected(pinButton, follow.Pinned);
            pinButton.GetComponentInChildren<TextMeshProUGUI>().text = follow.Pinned ? "Pinned" : "Pin";
        }

        void BuildReadView(Transform body)
        {
            var view = UIKit.Stretch(UIKit.Node("Read", body));
            readView = view.gameObject;
            UIKit.Layout<VerticalLayoutGroup>(view, 16);

            paper = UIKit.Panel(view, Color.white, 32, "Paper");
            paper.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;
            var content = UIKit.ScrollArea(paper.transform, out readerScroll);
            UIKit.Stretch((RectTransform)readerScroll.transform, 8, 8, 8, 8);
            UIKit.Layout<VerticalLayoutGroup>(content, 0, new RectOffset(32, 32, 28, 48));
            readerText = UIKit.Label(content, "", 46, UIKit.Ink);
            pageTurn = Slide.On(content); // the scroll view only drives y, so x is free to animate

            focusPaper = UIKit.Panel(view, Color.white, 32, "Focus");
            focusPaper.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;
            focusText = UIKit.Label(focusPaper.transform, "", 60, UIKit.Ink, false, TextAlignmentOptions.Center);
            focusText.overflowMode = TextOverflowModes.Overflow;
            UIKit.Stretch(focusText.rectTransform, 40, 32, 40, 32);
            sentenceTurn = Slide.On(focusText.rectTransform);
            focusPaper.gameObject.SetActive(false);

            var nav = UIKit.Layout<HorizontalLayoutGroup>(UIKit.Node("Page Navigation", view), 16, null, TextAnchor.MiddleCenter);
            pageNav = nav.gameObject;
            UIKit.Size(nav, -1, UIKit.Touch);
            previousPage = UIKit.Button(nav.transform, "Previous", () => GoToPage(currentPage - 1), 240, UIKit.Touch, 34, icons.previous);
            pageLabel = UIKit.Label(nav.transform, "", 32, UIKit.Ink, true, TextAlignmentOptions.Center);
            UIKit.Size(pageLabel, -1, UIKit.Touch, 1);
            nextPage = UIKit.Button(nav.transform, "Next", () => GoToPage(currentPage + 1), 240, UIKit.Touch, 34, icons.next);
            pageNav.SetActive(false);

            status = UIKit.Label(view, Hint, 30, UIKit.Muted);
            UIKit.Size(status, -1, 80);

            var actions = UIKit.Layout<HorizontalLayoutGroup>(UIKit.Node("Actions", view), 16);
            normalActions = actions.gameObject;
            UIKit.Size(actions, -1, 108);
            UIKit.SetSelected(UIKit.Button(actions.transform, "Capture page", Capture, -1, 108, 38, icons.capture), true);
            UIKit.Button(actions.transform, "Focus", () => SetFocus(true), 180, 108, 34, icons.focus);
            UIKit.Button(actions.transform, "Clear", Clear, 160, 108, 34, icons.clear);

            var focus = UIKit.Layout<HorizontalLayoutGroup>(UIKit.Node("Focus Actions", view), 16, null, TextAnchor.MiddleCenter);
            focusActions = focus.gameObject;
            UIKit.Size(focus, -1, 108);
            UIKit.Button(focus.transform, "Previous", () => MoveSentence(-1), -1, 108, 36, icons.previous);
            focusCount = UIKit.Label(focus.transform, "", 32, UIKit.Ink, true, TextAlignmentOptions.Center);
            UIKit.Size(focusCount, 130, 108);
            UIKit.SetSelected(UIKit.Button(focus.transform, "Next", () => MoveSentence(1), -1, 108, 36, icons.next), true);
            UIKit.Button(focus.transform, "Exit", () => SetFocus(false), 140, 108, 32, icons.exit);
            focusActions.SetActive(false);
        }

        void BuildSettingsView(Transform body)
        {
            var view = UIKit.Stretch(UIKit.Node("Settings", body));
            settingsView = view.gameObject;
            UIKit.Layout<VerticalLayoutGroup>(view, 12);

            previewPaper = UIKit.Panel(view, Color.white, 32, "Preview");
            UIKit.Size(previewPaper, -1, 220);
            previewText = UIKit.Label(previewPaper.transform, "", 46, UIKit.Ink);
            previewText.overflowMode = TextOverflowModes.Ellipsis;
            UIKit.Stretch(previewText.rectTransform, 28, 20, 28, 20);
            contrastLabel = UIKit.Label(view, "", 28, UIKit.Muted);
            UIKit.Size(contrastLabel, -1, 44);

            var scrollHolder = UIKit.Node("Options", view);
            scrollHolder.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;
            var list = UIKit.ScrollArea(scrollHolder, out var scroll);
            UIKit.Stretch((RectTransform)scroll.transform);
            UIKit.Layout<VerticalLayoutGroup>(list, 28, new RectOffset(4, 4, 8, 48));

            var presets = ReadingProfile.Presets();
            selectPreset = UIKit.Choice(Section(list, "Start from a profile"), Array.ConvertAll(presets, x => x.name), i =>
            {
                profile = presets[i].Clone();
                refreshSettings();
                Changed(true);
            });

            var font = UIKit.Choice(Section(list, "Font"), ReadingProfile.Fonts, i => { profile.font = ReadingProfile.Fonts[i]; Changed(); });
            var size = Step(list, "Text size", () => profile.fontSize, v => profile.fontSize = v, 2, 30, 80);
            var letters = Step(list, "Space between letters", () => profile.letterSpacing, v => profile.letterSpacing = v, 2, 0, 20);
            var words = Step(list, "Space between words", () => profile.wordSpacing, v => profile.wordSpacing = v, 5, 0, 80);
            var lines = Step(list, "Space between lines", () => profile.lineSpacing, v => profile.lineSpacing = v, 10, 0, 150);
            var pageColor = UIKit.Swatches(Section(list, "Page color"), PageColors, h => { profile.background = h; Changed(); });
            var textColor = UIKit.Swatches(Section(list, "Text color"), TextColors, h => { profile.textColor = h; Changed(); });
            var modes = new[] { "None", "Syllables", "Words", "Lines" };
            var mode = UIKit.Choice(Section(list, "Color the text by"), modes, i => { profile.colorMode = (ColorMode)i; Changed(); });
            var first = UIKit.Swatches(Section(list, "First color"), Inks, h => { profile.palette[0] = h; Changed(); });
            var second = UIKit.Swatches(Section(list, "Second color"), Inks, h => { profile.palette[1] = h; Changed(); });
            var confusables = Section(list, "Highlight b, d, p and q");
            var confusableOn = UIKit.Choice(confusables, new[] { "On", "Off" }, i => { profile.highlightConfusables = i == 0; Changed(); });
            var confusableColor = UIKit.Swatches(confusables, Inks, h => { profile.confusableColor = h; Changed(); });
            var languages = new[] { "en", "fr" };
            var language = UIKit.Choice(Section(list, "Split syllables for"), new[] { "English", "French" }, i => { profile.language = languages[i]; Changed(); });
            UIKit.Choice(Section(list, "Reduce motion"), new[] { "On", "Off" }, i => Motion.Reduced = i == 0)(Motion.Reduced ? 0 : 1);
            var again = Section(list, "Not sure what suits you?");
            UIKit.Button(again, "Find my comfort again", StartComfort, -1, UIKit.Touch, 34, icons.comfort);
            UIKit.Button(again, "Reset all settings", () =>
            {
                profile = presets[0].Clone();
                refreshSettings();
                Changed(true);
            }, -1, UIKit.Touch, 34, icons.reset);

            refreshSettings = () =>
            {
                selectPreset(Array.FindIndex(presets, x => x.name == profile.name));
                font(Array.IndexOf(ReadingProfile.Fonts, profile.font));
                size(); letters(); words(); lines();
                pageColor(profile.background);
                textColor(profile.textColor);
                mode((int)profile.colorMode);
                first(profile.palette[0]);
                second(profile.palette[1]);
                confusableOn(profile.highlightConfusables ? 0 : 1);
                confusableColor(profile.confusableColor);
                language(Array.IndexOf(languages, profile.language));
            };
        }

        static Transform Section(Transform list, string title)
        {
            var section = UIKit.Node(title, list);
            UIKit.Layout<VerticalLayoutGroup>(section, 12);
            UIKit.Size(UIKit.Label(section, title, 32, UIKit.Ink, true), -1, 44);
            return section;
        }

        // Stepper bound to a profile number; returns a refresher.
        Action Step(Transform list, string title, Func<float> get, Action<float> set, float step, float min, float max)
        {
            Action<string> show = null;
            show = UIKit.Stepper(Section(list, title),
                () => { set(Mathf.Max(min, get() - step)); show(get().ToString("0")); Changed(); },
                () => { set(Mathf.Min(max, get() + step)); show(get().ToString("0")); Changed(); });
            return () => show(get().ToString("0"));
        }

        // ---------- find your comfort (first run) ----------

        struct ComfortStep
        {
            public string question;
            public Action<ReadingProfile> a, b;
        }

        static readonly ComfortStep[] ComfortSteps =
        {
            new ComfortStep { question = "Which letters are easier to read?", a = p => p.font = "OpenDyslexic", b = p => p.font = "Atkinson" },
            new ComfortStep { question = "Which spacing feels better?",
                a = p => { p.letterSpacing = 2; p.wordSpacing = 10; p.lineSpacing = 30; },
                b = p => { p.letterSpacing = 8; p.wordSpacing = 35; p.lineSpacing = 70; } },
            new ComfortStep { question = "Do colors help you read?", a = p => p.colorMode = ColorMode.Syllables, b = p => p.colorMode = ColorMode.None },
            new ComfortStep { question = "Which size is more comfortable?", a = p => p.fontSize = 44, b = p => p.fontSize = 56 },
        };

        TextMeshProUGUI comfortQuestion, comfortProgress;
        (Image card, TextMeshProUGUI text)[] comfortCards;
        int comfortStep;

        void BuildComfortView(Transform body)
        {
            var view = UIKit.Stretch(UIKit.Node("Comfort", body));
            comfortView = view.gameObject;
            UIKit.Layout<VerticalLayoutGroup>(view, 16);
            UIKit.Size(UIKit.Label(view, "Find your comfort", 42, UIKit.Accent, true), -1, 56);
            comfortQuestion = UIKit.Label(view, "", 34, UIKit.Ink, true);
            UIKit.Size(comfortQuestion, -1, 56);
            comfortCards = new (Image, TextMeshProUGUI)[2];
            for (int i = 0; i < 2; i++)
            {
                int choice = i;
                var card = UIKit.Panel(view, Color.white, 32, i == 0 ? "Option A" : "Option B");
                card.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;
                var button = card.gameObject.AddComponent<Button>();
                button.targetGraphic = card;
                button.onClick.AddListener(() => PickComfort(choice));
                var text = UIKit.Label(card.transform, "", 46, UIKit.Ink);
                text.overflowMode = TextOverflowModes.Ellipsis;
                UIKit.Stretch(text.rectTransform, 28, 20, 28, 20);
                comfortCards[i] = (card, text);
            }
            var footer = UIKit.Layout<HorizontalLayoutGroup>(UIKit.Node("Footer", view), 16, null, TextAnchor.MiddleLeft);
            UIKit.Size(footer, -1, UIKit.Touch);
            comfortProgress = UIKit.Label(footer.transform, "", 30, UIKit.Muted);
            UIKit.Size(comfortProgress, -1, UIKit.Touch, 1);
            UIKit.Button(footer.transform, "Skip", FinishComfort, 180, UIKit.Touch, 34, icons.skip);
            comfortView.SetActive(false);
        }

        void StartComfort()
        {
            if (focusOn) SetFocus(false);
            comfortStep = 0;
            readFade.Show(false);
            settingsFade.Show(false);
            tabs.SetActive(false);
            comfortFade.Show(true);
            ShowComfortStep();
        }

        void ShowComfortStep()
        {
            var step = ComfortSteps[comfortStep];
            comfortQuestion.text = step.question + "  Tap the one you prefer.";
            comfortProgress.text = $"Question {comfortStep + 1} of {ComfortSteps.Length}";
            for (int i = 0; i < 2; i++)
            {
                var variant = profile.Clone();
                (i == 0 ? step.a : step.b)(variant);
                ProfileRenderer.Apply(comfortCards[i].text, comfortCards[i].card, Sample, variant, FontFor);
            }
        }

        void PickComfort(int choice)
        {
            var step = ComfortSteps[comfortStep];
            (choice == 0 ? step.a : step.b)(profile);
            if (++comfortStep < ComfortSteps.Length)
            {
                ShowComfortStep();
                return;
            }
            FinishComfort();
            status.text = "All set! You can change anything later in Settings.";
        }

        void FinishComfort()
        {
            tabs.SetActive(true);
            refreshSettings();
            Changed();
            selectTab(0);
            ShowTab(0);
        }

        // ---------- "Page detected" suggestion ----------

        void BuildBadge()
        {
            var pill = UIKit.Panel(badgeCanvas, UIKit.Accent, 40, "Badge");
            UIKit.Stretch(pill.rectTransform);
            var row = UIKit.Layout<HorizontalLayoutGroup>(pill, 16, new RectOffset(28, 16, 16, 16), TextAnchor.MiddleLeft);
            row.childForceExpandHeight = true;
            UIKit.Size(UIKit.Label(pill.transform, "Page detected\n<size=22>or tap your thumb</size>", 30, UIKit.OnAccent, true), -1, -1, 1);
            var read = UIKit.Button(pill.transform, "Read this page", ReadDetected, 320, -1, 32, icons.scan);
            ((Image)read.targetGraphic).color = UIKit.Surface;
            badgeCanvas.gameObject.SetActive(false);
            badgeFade = Fade.On(badgeCanvas.gameObject, 0.2f, pill.transform, 0.85f);
        }
    }
}
