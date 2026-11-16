using System;
using System.Globalization;

namespace ReadLens.Core
{
    // WCAG 2.x contrast ratio between two "#RRGGBB" colours (1:1 to 21:1).
    public static class Contrast
    {
        public const double Minimum = 4.5;   // WCAG AA for body text
        public const double Comfortable = 7; // WCAG AAA

        public static double Ratio(string a, string b)
        {
            double la = Luminance(a), lb = Luminance(b);
            return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
        }

        // Worst contrast between the page and anything drawn on it with this profile.
        public static double Worst(ReadingProfile p)
        {
            double worst = Ratio(p.textColor, p.background);
            if (p.colorMode != ColorMode.None)
                foreach (var c in p.palette) worst = Math.Min(worst, Ratio(c, p.background));
            if (p.highlightConfusables) worst = Math.Min(worst, Ratio(p.confusableColor, p.background));
            return worst;
        }

        static double Luminance(string hex)
        {
            hex = hex.TrimStart('#');
            double Channel(int i)
            {
                double c = int.Parse(hex.Substring(i, 2), NumberStyles.HexNumber) / 255.0;
                return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
            }
            return 0.2126 * Channel(0) + 0.7152 * Channel(2) + 0.0722 * Channel(4);
        }
    }
}
