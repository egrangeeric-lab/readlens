using System.Collections.Generic;
using System.Text;

namespace ReadLens.Core
{
    // OCR returns the page's physical lines; the reader needs flowing paragraphs it can re-wrap.
    public static class TextReflow
    {
        public static string Join(IEnumerable<IEnumerable<string>> blocks)
        {
            var sb = new StringBuilder();
            foreach (var block in blocks)
            {
                var paragraph = new StringBuilder();
                foreach (var raw in block)
                {
                    var line = raw.Trim();
                    if (line.Length == 0) continue;
                    if (paragraph.Length > 0)
                    {
                        // "read-" + "ing" on the next line -> "reading"
                        if (paragraph[paragraph.Length - 1] == '-' && paragraph.Length > 1 && char.IsLetter(paragraph[paragraph.Length - 2]) && char.IsLower(line[0]))
                            paragraph.Length--;
                        else
                            paragraph.Append(' ');
                    }
                    paragraph.Append(line);
                }
                if (paragraph.Length == 0) continue;
                if (sb.Length > 0) sb.Append("\n\n");
                sb.Append(paragraph);
            }
            return sb.ToString();
        }
    }
}
