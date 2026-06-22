using System;
using System.Text;

namespace SadPencil.Ra2CsfFile
{
    /// <summary>
    /// Shared helper for encoding/decoding extra data (WRTS) across all format helpers.
    /// ExtraValue in CSF is an ASCII string per the format specification.
    /// </summary>
    internal static class ExtraDataHelper
    {
        /// <summary>
        /// Decodes an extra data string from a text format into bytes.
        /// </summary>
        /// <param name="extraStr">The string representation (UTF-8 text).</param>
        /// <returns>Decoded bytes, or null if input is null/empty.</returns>
        public static byte[] Decode(string extraStr)
        {
            if (string.IsNullOrEmpty(extraStr))
                return null;

            return Encoding.UTF8.GetBytes(extraStr);
        }

        /// <summary>
        /// Encodes extra data bytes into a string for text format output.
        /// </summary>
        /// <param name="extra">The extra data bytes.</param>
        /// <returns>Encoded string, or null if input is null.</returns>
        public static string Encode(byte[] extra)
        {
            if (extra == null)
                return null;

            return Encoding.UTF8.GetString(extra);
        }
    }
}
