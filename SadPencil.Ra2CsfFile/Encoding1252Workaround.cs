using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SadPencil.Ra2CsfFile
{
    /// <summary>
    /// Provides workaround for Windows-1252 to Unicode conversion for characters 128-159.
    /// This is needed because RA2's original font treats these characters as Windows-1252 instead of Unicode.
    /// </summary>
    internal static class Encoding1252Workaround
    {
        /// <summary>Mapping from Windows-1252 characters to Unicode characters.</summary>
        public static IDictionary<char, char> Encoding1252ToUnicode { get; }

        /// <summary>Mapping from Unicode characters to Windows-1252 characters.</summary>
        public static IDictionary<char, char> UnicodeToEncoding1252 { get; }

        static Encoding1252Workaround()
        {
            var encoding1252ToUnicode = new Dictionary<char, char>
            {
                { '\u20AC', '\u0080' }, // €
                { '\u201A', '\u0082' }, // ‚
                { '\u0192', '\u0083' }, // ƒ
                { '\u201E', '\u0084' }, // „
                { '\u2026', '\u0085' }, // …
                { '\u2020', '\u0086' }, // †
                { '\u2021', '\u0087' }, // ‡
                { '\u02C6', '\u0088' }, // ˆ
                { '\u2030', '\u0089' }, // ‰
                { '\u0160', '\u008A' }, // Š
                { '\u2039', '\u008B' }, // ‹
                { '\u0152', '\u008C' }, // Œ
                { '\u017D', '\u008E' }, // Ž
                { '\u2018', '\u0091' }, // ‘
                { '\u2019', '\u0092' }, // ’
                { '\u201C', '\u0093' }, // “
                { '\u201D', '\u0094' }, // ”
                { '\u2022', '\u0095' }, // •
                { '\u2013', '\u0096' }, // –
                { '\u2014', '\u0097' }, // —
                { '\u02DC', '\u0098' }, // ˜
                { '\u2122', '\u0099' }, // ™
                { '\u0161', '\u009A' }, // š
                { '\u203A', '\u009B' }, // ›
                { '\u0153', '\u009C' }, // œ
                { '\u017E', '\u009E' }, // ž
                { '\u0178', '\u009F' }  // Ÿ
            };

            var unicodeToEncoding1252 = encoding1252ToUnicode.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);

            Encoding1252ToUnicode = encoding1252ToUnicode;
            UnicodeToEncoding1252 = unicodeToEncoding1252;
        }

        /// <summary>Converts a string from Windows-1252 encoding to Unicode (correcting the character mapping).</summary>
        /// <param name="value">The string to convert. May be null.</param>
        /// <returns>The converted string, or null if input was null.</returns>
        public static string ConvertsEncoding1252ToUnicode(string value)
        {
            if (value == null) return null;
            var result = new StringBuilder(value.Length);
            foreach (char c in value)
            {
                if (Encoding1252ToUnicode.TryGetValue(c, out char unicodeChar))
                    result.Append(unicodeChar);
                else
                    result.Append(c);
            }
            return result.ToString();
        }

        /// <summary>Converts a string from Unicode to Windows-1252 encoding (reversing the correction).</summary>
        /// <param name="value">The string to convert. May be null.</param>
        /// <returns>The converted string, or null if input was null.</returns>
        public static string ConvertsUnicodeToEncoding1252(string value)
        {
            if (value == null) return null;
            var result = new StringBuilder(value.Length);
            foreach (char c in value)
            {
                if (UnicodeToEncoding1252.TryGetValue(c, out char encoding1252Char))
                    result.Append(encoding1252Char);
                else
                    result.Append(c);
            }
            return result.ToString();
        }
    }
}