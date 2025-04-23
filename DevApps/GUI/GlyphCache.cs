using System.Windows;
using System.Windows.Media;

namespace DevApps.GUI
{
    /// <summary>
    /// Mise en cache des glyphes pour une police de caractères spécifique.
    /// Permet d'optimiser les opération de dessin de texte des caractères les plus courants
    /// </summary>
    internal static class GlyphCache
    {
        internal static Dictionary<char, ushort> CharToGlyphMap = new();
        internal static GlyphTypeface CachedGlyphTypeface;
        internal static Typeface typeface = new Typeface("Verdana");

        static GlyphCache()
        {
            if (!typeface.TryGetGlyphTypeface(out var glyphTypeface))
                throw new InvalidOperationException("Font not supported.");

            CachedGlyphTypeface = glyphTypeface;

            if (CharToGlyphMap.Count == 0)
            {
                for (char c = (char)0; c < 128; c++) // ASCII only
                {
                    if (glyphTypeface.CharacterToGlyphMap.TryGetValue(c, out ushort glyphIndex))
                    {
                        CharToGlyphMap[c] = glyphIndex;
                    }
                    else
                    {
                        CharToGlyphMap[c] = 0; // Fallback glyph
                    }
                }
            }
        }

        internal static ushort GetGlyph(char c)
        {
            if (!CharToGlyphMap.TryGetValue(c, out ushort glyph))
            {
                if (CachedGlyphTypeface.CharacterToGlyphMap.TryGetValue(c, out glyph))
                    CharToGlyphMap[c] = glyph;
                else
                    glyph = 0;

                CharToGlyphMap[c] = glyph;
            }

            return glyph;
        }
        
        internal static GlyphRun CreateGlyphRun(string text, double fontSize, Point baseLine)
        {
            ushort[] glyphIndexes = new ushort[text.Length];
            double[] advanceWidths = new double[text.Length];

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                ushort glyphIndex = GetGlyph(c);
                glyphIndexes[i] = glyphIndex;
                advanceWidths[i] = CachedGlyphTypeface.AdvanceWidths[glyphIndex] * fontSize;
            }

            return new GlyphRun(
                CachedGlyphTypeface,
                0,
                false,
                fontSize,
                96f,
                glyphIndexes,
                baseLine,
                advanceWidths,
                null, null, null, null, null, null
            );
        }

    }


}
