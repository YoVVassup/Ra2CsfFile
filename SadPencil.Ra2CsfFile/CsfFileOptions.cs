#pragma warning disable CA1805 // Do not initialize unnecessarily
using System;

namespace SadPencil.Ra2CsfFile
{
    /// <summary>
    /// This class controls the behavior of CsfFile
    /// </summary>
    public class CsfFileOptions : IEquatable<CsfFileOptions>
    {
        /// <summary>
        /// For code points 128-159 (0x80-0x9F), the original font file of RA2 mistakenly treat these characters as Windows-1252, instead of Unicode (ISO-8859-1). <br/>
        /// Enabling this option will corrects there charaters to Unicode ones when loading a .csf file.
        /// </summary>
        public bool Encoding1252ReadWorkaround { get; set; } = true;

        /// <summary>
        /// For code points 128-159 (0x80-0x9F), the original font file of RA2 mistakenly treat these characters as Windows-1252, instead of Unicode (ISO-8859-1). <br/>
        /// Enabling this option will converts there charaters from Unicode ones back to Windows-1252 when saving the .csf file. <br/>
        /// Note: it is recommended to turn this option off. In the original game.fnt file, except for Trade Mark Sign ™, other influenced characters have the correct font data in their Unicode code point. 
        /// </summary>
        public bool Encoding1252WriteWorkaround { get; set; } = false;

        /// <summary>
        /// If set, the labels will be sorted by key in ascending order (case-insensitive) when saving to any format.
        /// </summary>
        public bool OrderByKey { get; set; } = false;

        /// <summary>
        /// If true, extra data (WRTS) is treated as UTF-8 text when exporting to Excel/CSV/TXT/LLF.
        /// If false, extra data is Base64-encoded.
        /// </summary>
        public bool TreatExtraAsText { get; set; } = true;

        /// <summary>
        /// If true, apply Encoding1252 workaround to extra data when reading CSF (not recommended).
        /// Extra data is typically binary or ASCII; applying this may corrupt binary data.
        /// </summary>
        public bool ApplyEncoding1252ToExtra { get; set; } = false;

        #region IEquatable implementation

        public bool Equals(CsfFileOptions other)
        {
            if (other == null) return false;
            return this.Encoding1252ReadWorkaround == other.Encoding1252ReadWorkaround &&
                   this.Encoding1252WriteWorkaround == other.Encoding1252WriteWorkaround &&
                   this.OrderByKey == other.OrderByKey &&
                   this.TreatExtraAsText == other.TreatExtraAsText &&
                   this.ApplyEncoding1252ToExtra == other.ApplyEncoding1252ToExtra;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + Encoding1252ReadWorkaround.GetHashCode();
                hash = hash * 23 + Encoding1252WriteWorkaround.GetHashCode();
                hash = hash * 23 + OrderByKey.GetHashCode();
                hash = hash * 23 + TreatExtraAsText.GetHashCode();
                hash = hash * 23 + ApplyEncoding1252ToExtra.GetHashCode();
                return hash;
            }
        }

        public override bool Equals(object obj) => Equals(obj as CsfFileOptions);

        public static bool operator ==(CsfFileOptions left, CsfFileOptions right)
        {
            if (left is null) return right is null;
            return left.Equals(right);
        }

        public static bool operator !=(CsfFileOptions left, CsfFileOptions right) => !(left == right);

        #endregion
    }
}
#pragma warning restore CA1805 // Do not initialize unnecessarily