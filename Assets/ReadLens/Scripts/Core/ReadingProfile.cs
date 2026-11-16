using System;

namespace ReadLens.Core
{
    public enum ColorMode { None, Syllables, Words, Lines }

    // Everything a parent or speech therapist sets up ahead of time. Saved as JSON on the headset.
    [Serializable]
    public class ReadingProfile
    {
        public string name = "Custom";
        public string language = "en";        // "en" or "fr": drives syllable rules
        public string font = "OpenDyslexic";
        public float fontSize = 36f;
        public float letterSpacing = 4f;      // TMP characterSpacing (em/100)
        public float wordSpacing = 20f;       // TMP wordSpacing (em/100)
        public float lineSpacing = 40f;       // TMP lineSpacing (em/100)
        public string background = "#FBF5E6"; // cream reduces glare
        public string textColor = "#1E1E1E";
        public ColorMode colorMode = ColorMode.Syllables;
        public string[] palette = { "#1F5FBF", "#C0392B" };
        public bool highlightConfusables = true;
        public string confusableColor = "#1E8C4E";

        public static readonly string[] Fonts = { "OpenDyslexic", "Atkinson", "Classic" };

        public ReadingProfile Clone()
        {
            var c = (ReadingProfile)MemberwiseClone();
            c.palette = (string[])palette.Clone();
            return c;
        }

        public static ReadingProfile[] Presets() => new[]
        {
            new ReadingProfile { name = "Syllables" },
            new ReadingProfile { name = "Calm", colorMode = ColorMode.Lines, palette = new[] { "#1E1E1E", "#3B5998" }, highlightConfusables = false },
            new ReadingProfile { name = "Big & airy", fontSize = 44f, letterSpacing = 8f, wordSpacing = 35f, lineSpacing = 70f, colorMode = ColorMode.Words },
        };
    }
}
