using System.Text;

namespace ReadLens.Core
{
    // Turns plain OCR text into TextMeshPro rich text following a profile.
    // Spacing and font size are set on the TMP component itself; line colouring needs layout, so the view does it.
    public static class TextFormatter
    {
        const string Confusables = "bdpq";

        public static string ToRichText(string text, ReadingProfile p)
        {
            var sb = new StringBuilder(text.Length * 4);
            int wordIndex = 0, syllableIndex = 0, i = 0;
            while (i < text.Length)
            {
                if (!char.IsLetter(text[i]))
                {
                    AppendChar(sb, text[i], p);
                    i++;
                    continue;
                }
                int start = i;
                while (i < text.Length && (char.IsLetter(text[i]) || IsInnerApostrophe(text, i))) i++;
                var word = text.Substring(start, i - start);

                switch (p.colorMode)
                {
                    case ColorMode.Syllables:
                        foreach (var s in Syllabifier.Split(word, p.language))
                            AppendColored(sb, s, Pick(p, syllableIndex++), p);
                        break;
                    case ColorMode.Words:
                        AppendColored(sb, word, Pick(p, wordIndex), p);
                        break;
                    default:
                        AppendColored(sb, word, null, p);
                        break;
                }
                wordIndex++;
            }
            return sb.ToString();
        }

        static string Pick(ReadingProfile p, int index) =>
            p.palette == null || p.palette.Length == 0 ? null : p.palette[index % p.palette.Length];

        static void AppendColored(StringBuilder sb, string s, string color, ReadingProfile p)
        {
            if (color != null) sb.Append("<color=").Append(color).Append('>');
            foreach (var c in s) AppendChar(sb, c, p);
            if (color != null) sb.Append("</color>");
        }

        static void AppendChar(StringBuilder sb, char c, ReadingProfile p)
        {
            if (c == '<') { sb.Append("<noparse><</noparse>"); return; } // OCR text must never inject tags
            if (p.highlightConfusables && Confusables.IndexOf(c) >= 0)
                sb.Append("<b><color=").Append(p.confusableColor).Append('>').Append(c).Append("</color></b>");
            else
                sb.Append(c);
        }

        static bool IsInnerApostrophe(string t, int i) =>
            (t[i] == '\'' || t[i] == '’') && i > 0 && i + 1 < t.Length && char.IsLetter(t[i - 1]) && char.IsLetter(t[i + 1]);
    }
}
