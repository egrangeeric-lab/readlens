using System.Collections.Generic;

namespace ReadLens.Core
{
    // Splits text into sentences for Focus mode (one sentence on screen at a time).
    public static class Sentences
    {
        static readonly HashSet<string> Abbreviations = new HashSet<string> { "mr", "mrs", "ms", "dr", "st", "mt", "vs", "etc", "e.g", "i.e", "m", "mme" };

        public static List<string> Split(string text)
        {
            var result = new List<string>();
            int start = 0;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                bool paragraphEnd = c == '\n';
                bool sentenceEnd = (c == '.' || c == '!' || c == '?') && (i + 1 == text.Length || char.IsWhiteSpace(text[i + 1]) || text[i + 1] == '"' || text[i + 1] == '”');
                if (sentenceEnd && c == '.' && IsAbbreviation(text, i)) sentenceEnd = false;
                if (!sentenceEnd && !paragraphEnd) continue;
                int end = i + 1;
                while (end < text.Length && (text[end] == '"' || text[end] == '”')) end++; // keep closing quotes
                Add(result, text.Substring(start, end - start));
                start = i = end;
                i--;
            }
            if (start < text.Length) Add(result, text.Substring(start));
            return result;
        }

        static void Add(List<string> list, string s)
        {
            s = s.Trim();
            if (s.Length > 0) list.Add(s);
        }

        static bool IsAbbreviation(string text, int dot)
        {
            int w = dot;
            while (w > 0 && (char.IsLetter(text[w - 1]) || text[w - 1] == '.')) w--;
            var word = text.Substring(w, dot - w).ToLowerInvariant();
            return Abbreviations.Contains(word) || (word.Length == 1 && char.IsUpper(text[w])); // initials: "J. K. Rowling"
        }
    }
}
