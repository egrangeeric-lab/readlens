using System;
using System.Text;
using ReadLens.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ColorMode = ReadLens.Core.ColorMode;

namespace ReadLens.App
{
    // Applies a reading profile to a text + its page background.
    public static class ProfileRenderer
    {
        public static void Apply(TMP_Text text, Graphic page, string raw, ReadingProfile p, Func<string, TMP_FontAsset> font)
        {
            text.font = font(p.font);
            text.fontSize = p.fontSize;
            text.characterSpacing = p.letterSpacing;
            text.wordSpacing = p.wordSpacing;
            text.lineSpacing = p.lineSpacing;
            text.color = UIKit.Hex(p.textColor);
            page.color = UIKit.Hex(p.background);
            text.text = TextFormatter.ToRichText(raw, p);
            if (p.colorMode == ColorMode.Lines && text.gameObject.activeInHierarchy) ColorLines(text, p.palette);
        }

        // Line colours depend on wrapping: lay out once, then wrap each rendered line in a colour tag.
        // Colour tags don't change glyph widths, so the wrapping stays identical.
        static void ColorLines(TMP_Text text, string[] palette)
        {
            Canvas.ForceUpdateCanvases();
            text.ForceMeshUpdate();
            var info = text.textInfo;
            var s = text.text;
            var starts = new int[info.lineCount];
            for (int l = 0; l < info.lineCount; l++)
            {
                int idx = info.characterInfo[info.lineInfo[l].firstCharacterIndex].index;
                // Step back over tags that open this character (e.g. a highlighted "b") so nesting stays valid.
                while (idx > 0 && s[idx - 1] == '>')
                {
                    int open = s.LastIndexOf('<', idx - 1);
                    if (open < 0) break;
                    idx = open;
                }
                starts[l] = idx;
            }
            var sb = new StringBuilder(s.Length + info.lineCount * 32);
            int cursor = 0;
            for (int l = 0; l < starts.Length; l++)
            {
                if (starts[l] < cursor) continue;
                sb.Append(s, cursor, starts[l] - cursor);
                if (l > 0) sb.Append("</color>");
                sb.Append("<color=").Append(palette[l % palette.Length]).Append('>');
                cursor = starts[l];
            }
            sb.Append(s, cursor, s.Length - cursor).Append("</color>");
            text.text = sb.ToString();
        }
    }
}
