using System;
using System.Collections.Generic;
using System.IO;
using Meta.XR;
using ReadLens.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ColorMode = ReadLens.Core.ColorMode;

namespace ReadLens.App
{
    // The ReadLens window: Read tab (captured pages) + Settings tab (the reader's profile, live preview).
    public class ReadLensApp : MonoBehaviour
    {
        [SerializeField] RectTransform canvas;
        [SerializeField] Font openDyslexic, atkinson, atkinsonBold;
        [SerializeField] TMP_FontAsset classic;
        [SerializeField] PassthroughCameraAccess cameraAccess;
        [SerializeField] OcrService ocr;

        const string Sample = "The big dog barely picked a quiet path past the pond.";
        const string Welcome =
            "Welcome to ReadLens!\n\nHold a printed page in front of you, then press Capture page.\n\n" +
            "Your page will appear here, written the way you like to read. Open Settings to choose your font, spacing and colors.";
        const string Hint = "Hold the page about 30 cm in front of you, in good light.";

        static readonly string[] PageColors = { "#FBF5E6", "#FFFFFF", "#E8F1FB", "#EAF6EA", "#FFF3C4", "#1E1E1E" };
        static readonly string[] TextColors = { "#1E1E1E", "#3A3A3A", "#1B2A49", "#FFFFFF" };
        static readonly string[] Inks = { "#1F5FBF", "#C0392B", "#1E8C4E", "#7B3FB5", "#D35400", "#1E1E1E" };

        ReadingProfile profile;
        string profilePath;
        readonly List<string> pages = new List<string>();
        readonly Dictionary<string, TMP_FontAsset> fonts = new Dictionary<string, TMP_FontAsset>();
        GameObject readView, settingsView;
        TextMeshProUGUI readerText, previewText, status;
        Image paper, previewPaper;
        ScrollRect readerScroll;
        Action refreshSettings = () => { };
        Action<int> selectPreset = _ => { };

        void Awake()
        {
            fonts["OpenDyslexic"] = Dynamic(openDyslexic);
            fonts["Atkinson"] = UIKit.Font = Dynamic(atkinson);
            fonts["Classic"] = classic;
            UIKit.FontBold = Dynamic(atkinsonBold);
            profilePath = Path.Combine(Application.persistentDataPath, "profile.json");
            profile = LoadProfile();
            Build();
            refreshSettings();
            Render();
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

        void Render()
        {
            ProfileRenderer.Apply(readerText, paper, pages.Count == 0 ? Welcome : string.Join("\n\n", pages), profile, FontFor);
            ProfileRenderer.Apply(previewText, previewPaper, Sample, profile, FontFor);
        }

        // ---------- capture ----------

        void Capture()
        {
            if (ocr.Busy) return;
            if (cameraAccess == null || !cameraAccess.IsPlaying)
            {
                status.text = "The camera is starting. If asked, allow camera access, then try again.";
                return;
            }
            status.text = "Reading your page...";
            var size = cameraAccess.CurrentResolution;
            var pixels = cameraAccess.GetColors();
            SaveDebugCapture(pixels, size);
            ocr.Recognize(pixels, size.x, size.y, result =>
            {
                var text = result.ReflowedText;
                Debug.Log($"[ReadLens] OCR {size.x}x{size.y}: {result.blocks.Length} blocks, {text.Length} chars");
                if (text.Length == 0)
                {
                    status.text = "No text found. Hold the page a little closer, in good light, and try again.";
                    return;
                }
                pages.Add(text);
                status.text = $"Page {pages.Count} added. Scroll to read, or capture the next page.";
                Render();
                if (pages.Count == 1) readerScroll.verticalNormalizedPosition = 1;
            }, error =>
            {
                Debug.LogWarning("[ReadLens] OCR failed: " + error);
                status.text = "Sorry, the page couldn't be read. Please try again.";
            });
        }

        // Dev only: frames are saved only if a "debug-captures" flag file was pushed to the app folder over adb.
        void SaveDebugCapture(Unity.Collections.NativeArray<Color32> pixels, Vector2Int size)
        {
            var dir = Application.persistentDataPath;
            if (!File.Exists(Path.Combine(dir, "debug-captures"))) return;
            var tex = new Texture2D(size.x, size.y, TextureFormat.RGBA32, false);
            tex.SetPixelData(pixels, 0);
            File.WriteAllBytes(Path.Combine(dir, "last-capture.png"), tex.EncodeToPNG());
            Destroy(tex);
        }

        void Clear()
        {
            pages.Clear();
            status.text = Hint;
            Render();
        }

        // ---------- UI ----------

        void Build()
        {
            var shadow = UIKit.Panel(canvas, new Color(0, 0, 0, 0.28f), 56, "Shadow");
            UIKit.Stretch(shadow.rectTransform, -6, 4, -6, -14);
            var window = UIKit.Panel(canvas, UIKit.Surface, 48, "Window");
            UIKit.Stretch(window.rectTransform);

            var column = UIKit.Stretch(UIKit.Node("Column", window.transform));
            UIKit.Layout<VerticalLayoutGroup>(column, 24, new RectOffset(36, 36, 32, 36));

            var header = UIKit.Layout<HorizontalLayoutGroup>(UIKit.Node("Header", column), 24, null, TextAnchor.MiddleLeft);
            UIKit.Size(header, -1, UIKit.Touch);
            UIKit.Size(UIKit.Label(header.transform, "ReadLens", 52, UIKit.Accent, true), -1, UIKit.Touch, 1);
            var tabsHolder = UIKit.Node("Tabs", header.transform);
            UIKit.Size(tabsHolder, 480, UIKit.Touch);
            UIKit.Layout<HorizontalLayoutGroup>(tabsHolder, 0).childForceExpandWidth = true;
            var selectTab = UIKit.Choice(tabsHolder, new[] { "Read", "Settings" }, ShowTab, 34);

            var body = UIKit.Node("Body", column);
            body.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;
            BuildReadView(body);
            BuildSettingsView(body);
            selectTab(0);
            ShowTab(0);
        }

        void ShowTab(int i)
        {
            readView.SetActive(i == 0);
            settingsView.SetActive(i == 1);
            Render(); // line colours need the visible layout
        }

        void BuildReadView(Transform body)
        {
            var view = UIKit.Stretch(UIKit.Node("Read", body));
            readView = view.gameObject;
            UIKit.Layout<VerticalLayoutGroup>(view, 20);

            paper = UIKit.Panel(view, Color.white, 32, "Paper");
            paper.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;
            var content = UIKit.ScrollArea(paper.transform, out readerScroll);
            UIKit.Stretch((RectTransform)readerScroll.transform, 8, 8, 8, 8);
            UIKit.Layout<VerticalLayoutGroup>(content, 0, new RectOffset(36, 36, 32, 48));
            readerText = UIKit.Label(content, "", 36, UIKit.Ink);

            status = UIKit.Label(view, Hint, 32, UIKit.Muted);
            UIKit.Size(status, -1, 84);

            var actions = UIKit.Layout<HorizontalLayoutGroup>(UIKit.Node("Actions", view), 20);
            UIKit.Size(actions, -1, 112);
            var capture = UIKit.Button(actions.transform, "Capture page", Capture, -1, 112, 40);
            UIKit.SetSelected(capture, true); // primary action stands out
            UIKit.Button(actions.transform, "Clear", Clear, 220, 112, 34);
        }

        void BuildSettingsView(Transform body)
        {
            var view = UIKit.Stretch(UIKit.Node("Settings", body));
            settingsView = view.gameObject;
            UIKit.Layout<VerticalLayoutGroup>(view, 20);

            previewPaper = UIKit.Panel(view, Color.white, 32, "Preview");
            UIKit.Size(previewPaper, -1, 240);
            previewText = UIKit.Label(previewPaper.transform, "", 36, UIKit.Ink);
            previewText.overflowMode = TextOverflowModes.Ellipsis;
            UIKit.Stretch(previewText.rectTransform, 32, 24, 32, 24);

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
            var size = Step(list, "Text size", () => profile.fontSize, v => profile.fontSize = v, 2, 24, 72);
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
            UIKit.Button(Section(list, "Start over"), "Reset all settings", () =>
            {
                profile = presets[0].Clone();
                refreshSettings();
                Changed(true);
            });

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
            UIKit.Size(UIKit.Label(section, title, 34, UIKit.Ink, true), -1, 48);
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
    }
}
