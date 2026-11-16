using System.Collections.Generic;

namespace ReadLens.Core
{
    // Rule-based syllable splitter for English and French, good enough for colour cues.
    // shortcut: heuristic, not a dictionary; swap for Liang hyphenation patterns if accuracy on test pages drops under 90%.
    public static class Syllabifier
    {
        const string Vowels = "aeiouyàâäéèêëîïôöùûüÿœæ";

        // Consonant pairs that start a syllable together (ta-ble, ma-chine, an-gry).
        static readonly HashSet<string> Onsets = new HashSet<string>
        {
            "bl", "br", "ch", "cl", "cr", "dr", "fl", "fr", "gl", "gr", "ph", "pl", "pr",
            "sh", "th", "tr", "wh", "wr", "kn", "qu", "gn", "vr",
        };

        public static List<string> Split(string word, string language = "en")
        {
            var parts = new List<string>();
            if (string.IsNullOrEmpty(word)) return parts;

            var lower = word.ToLowerInvariant();
            var isVowel = new bool[word.Length];
            for (int i = 0; i < word.Length; i++)
                isVowel[i] = Vowels.IndexOf(lower[i]) >= 0 && !(lower[i] == 'y' && i == 0);
            // "qu" and "gu" + vowel: the u is part of the consonant.
            for (int i = 1; i < word.Length - 1; i++)
                if (lower[i] == 'u' && (lower[i - 1] == 'q' || lower[i - 1] == 'g') && isVowel[i + 1]) isVowel[i] = false;
            if (language == "en") MuteEnglishEndings(lower, isVowel);

            // Vowel groups = syllable nuclei.
            var nuclei = new List<(int start, int end)>();
            for (int i = 0; i < word.Length; i++)
            {
                if (!isVowel[i]) continue;
                int s = i;
                while (i + 1 < word.Length && isVowel[i + 1]) i++;
                nuclei.Add((s, i));
            }
            if (nuclei.Count <= 1) { parts.Add(word); return parts; }

            int cut = 0;
            for (int n = 0; n < nuclei.Count - 1; n++)
            {
                int a = nuclei[n].end + 1, b = nuclei[n + 1].start; // consonant cluster [a, b)
                int split = (b - a) switch
                {
                    0 => b,
                    1 => a,
                    _ => Onsets.Contains(lower.Substring(b - 2, 2)) && !(b - 2 == a && lower[a] == lower[a + 1]) ? b - 2 : a + 1,
                };
                if (b - a == 1 && lower[a] == 'x') split = a + 1; // ex-am, tax-i
                if (split <= cut) continue;
                parts.Add(word.Substring(cut, split - cut));
                cut = split;
            }
            parts.Add(word.Substring(cut));
            return parts;
        }

        static void MuteEnglishEndings(string w, bool[] isVowel)
        {
            int n = w.Length;
            // Silent final e (make, home) but not "-le" after a consonant (ta-ble) or words like "the", "be".
            if (n > 3 && w[n - 1] == 'e' && !isVowel[n - 2] && !(w[n - 2] == 'l' && !isVowel[n - 3]))
                isVowel[n - 1] = false;
            // -es / -ed are silent unless after a sound that needs them (wished vs wanted, boxes).
            if (n > 3 && (w.EndsWith("ed") || w.EndsWith("es")) && !isVowel[n - 3])
            {
                char c = w[n - 3];
                bool voiced = w.EndsWith("ed") ? (c == 't' || c == 'd') : (c == 's' || c == 'x' || c == 'z' || w.EndsWith("ches") || w.EndsWith("shes"));
                if (!voiced) isVowel[n - 2] = false;
            }
        }
    }
}
