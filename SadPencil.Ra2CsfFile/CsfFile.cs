using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

[assembly: CLSCompliant(true)]
namespace SadPencil.Ra2CsfFile
{
    /// <summary>
    /// This class reads and writes the stringtable file (.csf) that is used by RA2/YR.
    /// Supports extra data (WRTS), label ordering, and case-insensitive label names.
    /// </summary>
    public class CsfFile : ICloneable, IEquatable<CsfFile>
    {
        // https://modenc.renegadeprojects.com/CSF_File_Format

        /// <summary>
        /// This option controls the behavior of CsfFile.
        /// </summary>
        public CsfFileOptions Options { get; set; } = new CsfFileOptions();

        // Internal dictionary with case-insensitive comparer; original label casing preserved.
        private readonly Dictionary<string, string> _labels = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
        /// <summary>
        /// The labels of this file. Each label has a name and a string value.
        /// </summary>
        public IDictionary<string, string> Labels => _labels;

        private readonly Dictionary<string, byte[]> _extra = new Dictionary<string, byte[]>(StringComparer.InvariantCultureIgnoreCase);
        /// <summary>
        /// Gets extra binary data (WRTS block) associated with a label, or null if none.
        /// </summary>
        public byte[] GetExtra(string labelName) => _extra.TryGetValue(labelName, out var extra) ? extra : null;
        /// <summary>
        /// Sets extra binary data for a label. Pass null to remove.
        /// </summary>
        public void SetExtra(string labelName, byte[] extra)
        {
            if (extra == null)
                _extra.Remove(labelName);
            else
                _extra[labelName] = extra;
        }
        /// <summary>
        /// Removes extra data from a label.
        /// </summary>
        public void RemoveExtra(string labelName) => _extra.Remove(labelName);
        /// <summary>
        /// Checks whether a label has an extra data block.
        /// </summary>
        public bool HasExtra(string labelName) => _extra.ContainsKey(labelName);

        private readonly List<string> _order = new List<string>();
        /// <summary>
        /// The labels in the order they appear in the CSF file (or as last set).
        /// </summary>
        public IList<string> LabelOrder => _order.AsReadOnly();

        /// <summary>
        /// The line break characters between the multiple lines in the label value.
        /// </summary>
        public static string LineBreakCharacters { get; } = "\n";

        /// <summary>
        /// The language of this file.
        /// </summary>
        public CsfLang Language { get; set; } = CsfLang.EnglishUS;

        /// <summary>
        /// The version number of the CSF format.
        /// </summary>
        public int Version { get; set; } = 3;

        /// <summary>
        /// Returns the sequence of labels in the order they should be written to output formats.
        /// Respects the OrderByKey option and the stored label order.
        /// </summary>
        /// <returns>An enumerable of label names in write order.</returns>
        public IEnumerable<string> GetLabelsInWriteOrder()
        {
            if (Options.OrderByKey)
                return Labels.Keys.OrderBy(k => k, StringComparer.InvariantCultureIgnoreCase);
            if (LabelOrder.Count == Labels.Count)
                return LabelOrder;
            return Labels.Keys;
        }

        /// <summary>
        /// Add or replace a label to the string table.
        /// </summary>
        /// <param name="labelName">Label name (ASCII, case-insensitive).</param>
        /// <param name="labelValue">Label value (UTF-16 text).</param>
        /// <returns>True if label already existed, false otherwise.</returns>
        public bool AddLabel(string labelName, string labelValue)
        {
            return AddLabel(labelName, labelValue, null);
        }

        /// <summary>
        /// Add or replace a label to the string table with optional extra binary data (WRTS block).
        /// </summary>
        /// <param name="labelName">Label name (ASCII, case-insensitive).</param>
        /// <param name="labelValue">Label value (UTF-16 text).</param>
        /// <param name="extra">Optional extra binary data (will be stored as WRTS).</param>
        /// <returns>True if label already existed, false otherwise.</returns>
        public bool AddLabel(string labelName, string labelValue, byte[] extra)
        {
            if (labelName == null) throw new ArgumentNullException(nameof(labelName));
            if (labelValue == null) throw new ArgumentNullException(nameof(labelValue));
            if (!ValidateLabelName(labelName))
                throw new ArgumentException("Invalid characters found in label name.", nameof(labelName));

            bool exists = _labels.ContainsKey(labelName);
            _labels[labelName] = labelValue;
            if (extra != null)
                _extra[labelName] = extra;
            else
                _extra.Remove(labelName);

            if (!exists)
                _order.Add(labelName);
            return exists;
        }

        /// <summary>
        /// Remove a label from the string table.
        /// </summary>
        /// <param name="labelName">Label name (case-insensitive).</param>
        /// <returns>True if the label existed and was removed.</returns>
        public bool RemoveLabel(string labelName)
        {
            bool removed = _labels.Remove(labelName);
            _extra.Remove(labelName);
            if (removed)
            {
                string keyToRemove = _order.FirstOrDefault(k => string.Equals(k, labelName, StringComparison.InvariantCultureIgnoreCase));
                if (keyToRemove != null)
                    _order.Remove(keyToRemove);
            }
            return removed;
        }

        /// <summary>
        /// Create an empty stringtable file with default options.
        /// </summary>
        public CsfFile() { }

        /// <summary>
        /// Create an empty stringtable file with given options.
        /// </summary>
        public CsfFile(CsfFileOptions options)
        {
            this.Options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Clone the CsfFile.
        /// </summary>
        public CsfFile(CsfFile csf)
        {
            if (csf == null) throw new ArgumentNullException(nameof(csf));

            this.Version = csf.Version;
            this.Language = csf.Language;
            this.Options = csf.Options;
            this._labels = new Dictionary<string, string>(csf._labels, StringComparer.InvariantCultureIgnoreCase);
            this._extra = new Dictionary<string, byte[]>(csf._extra, StringComparer.InvariantCultureIgnoreCase);
            this._order = new List<string>(csf._order);
        }

        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        public object Clone() => new CsfFile(this);

        #region IEquatable implementation

        /// <summary>Determines whether the specified object is equal to the current object.</summary>
        public bool Equals(CsfFile other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            if (this.Language != other.Language || this.Version != other.Version)
                return false;
            if (!this.Options.Equals(other.Options))
                return false;
            if (this._labels.Count != other._labels.Count)
                return false;

            foreach (var kvp in this._labels)
            {
                if (!other._labels.TryGetValue(kvp.Key, out string otherValue))
                    return false;
                if (!string.Equals(kvp.Value, otherValue, StringComparison.InvariantCulture))
                    return false;
                byte[] thisExtra = this.GetExtra(kvp.Key);
                byte[] otherExtra = other.GetExtra(kvp.Key);
                if (!ByteArraysEqual(thisExtra, otherExtra))
                    return false;
            }
            return true;
        }

        /// <summary>Determines whether the specified object is equal to the current object.</summary>
        public override bool Equals(object obj) => Equals(obj as CsfFile);

        /// <summary>Serves as the default hash function.</summary>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + Language.GetHashCode();
                hash = hash * 23 + Version.GetHashCode();
                hash = hash * 23 + Options.GetHashCode();
                foreach (var kvp in _labels.OrderBy(k => k.Key, StringComparer.InvariantCultureIgnoreCase))
                {
                    hash = hash * 23 + kvp.Key.GetHashCode();
                    hash = hash * 23 + kvp.Value.GetHashCode();
                }
                return hash;
            }
        }

        /// <summary>Equality operator.</summary>
        public static bool operator ==(CsfFile left, CsfFile right)
        {
            if (left is null) return right is null;
            return left.Equals(right);
        }

        /// <summary>Inequality operator.</summary>
        public static bool operator !=(CsfFile left, CsfFile right) => !(left == right);

        private static bool ByteArraysEqual(byte[] a, byte[] b)
        {
            if (a == b) return true;
            if (a == null || b == null) return false;
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
                if (a[i] != b[i]) return false;
            return true;
        }

        #endregion

        #region CSF File Operations

        /// <summary>
        /// Load an existing stringtable file (.csf).
        /// </summary>
        public static CsfFile LoadFromCsfFile(Stream stream) => LoadFromCsfFile(stream, new CsfFileOptions());

        private static byte[] TruncateUtf16BytesAtDoubleZero(byte[] inputBytes)
        {
            if (inputBytes == null) throw new ArgumentNullException(nameof(inputBytes));
            for (int i = 0; i < inputBytes.Length - 1; i += 2)
            {
                if (inputBytes[i] == 0x00 && inputBytes[i + 1] == 0x00)
                {
                    byte[] result = new byte[i];
                    Array.Copy(inputBytes, 0, result, 0, i);
                    return result;
                }
            }
            return inputBytes;
        }

        /// <summary>
        /// Load an existing stringtable file (.csf) with options.
        /// </summary>
        public static CsfFile LoadFromCsfFile(Stream stream, CsfFileOptions options)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (options == null) throw new ArgumentNullException(nameof(options));

            var csf = new CsfFile(options);
            long originalPosition = stream.Position;

            try
            {
                using (var br = new BinaryReader(stream, Encoding.ASCII))
                {
                    byte[] headerId = br.ReadBytes(4);
                    if (!headerId.SequenceEqual(Encoding.ASCII.GetBytes(" FSC")))
                        throw new InvalidDataException("Invalid CSF file header.");

                    csf.Version = br.ReadInt32();
                    int labelsNum = br.ReadInt32();
                    int stringsNum = br.ReadInt32();
                    _ = br.ReadInt32();
                    csf.Language = CsfLangHelper.GetCsfLang(br.ReadInt32());

                    for (int iLabel = 0; iLabel < labelsNum; iLabel++)
                    {
                        while (true)
                        {
                            byte[] labelId = br.ReadBytes(4);
                            if (labelId.SequenceEqual(Encoding.ASCII.GetBytes(" LBL")))
                                break;
                            if (labelId.Length != 4)
                                throw new InvalidDataException("Unexpected end of file.");
                        }

                        int numValues = br.ReadInt32();
                        int labelNameLength = br.ReadInt32();
                        byte[] labelName = br.ReadBytes(labelNameLength);
                        string labelNameStr;
                        try
                        {
                            labelNameStr = Encoding.ASCII.GetString(labelName);
                        }
                        catch (Exception ex)
                        {
                            throw new InvalidDataException($"Invalid label name at position {stream.Position}.", ex);
                        }

                        if (!ValidateLabelName(labelNameStr))
                            throw new InvalidDataException($"Invalid characters found in label name \"{labelNameStr}\" at position {stream.Position}.");

                        string labelValue = null;
                        byte[] extraData = null;
                        for (int iValue = 0; iValue < numValues; iValue++)
                        {
                            byte[] labelValueType = br.ReadBytes(4);
                            bool labelHasExtraValue;

                            if (labelValueType.SequenceEqual(Encoding.ASCII.GetBytes(" RTS")))
                                labelHasExtraValue = false;
                            else if (labelValueType.SequenceEqual(Encoding.ASCII.GetBytes("WRTS")))
                                labelHasExtraValue = true;
                            else
                                throw new InvalidDataException($"Invalid label value type at position {stream.Position}.");

                            int valueLength = br.ReadInt32();
                            byte[] value = br.ReadBytes(valueLength * 2);
                            value = TruncateUtf16BytesAtDoubleZero(value);
                            value = value.Select(v => (byte)(~v)).ToArray();

                            string valueStr;
                            try
                            {
                                valueStr = Encoding.Unicode.GetString(value);
                                if (options.Encoding1252ReadWorkaround)
                                    valueStr = Encoding1252Workaround.ConvertsUnicodeToEncoding1252(valueStr);
                            }
                            catch (Exception ex)
                            {
                                throw new InvalidDataException($"Invalid label value string at position {stream.Position}.", ex);
                            }

                            if (labelHasExtraValue)
                            {
                                int extLength = br.ReadInt32();
                                extraData = br.ReadBytes(extLength);
                                // Removed Encoding.GetEncoding(1252) call for .NET 4.0 compatibility.
                                // If needed, use Encoding1252Workaround manually.
                            }

                            if (iValue == 0)
                                labelValue = valueStr;
                        }

                        if (labelValue != null)
                            csf.AddLabel(labelNameStr, labelValue, extraData);
                    }
                }
            }
            catch (InvalidDataException)
            {
                stream.Position = originalPosition;
                throw;
            }
            catch (Exception ex)
            {
                stream.Position = originalPosition;
                throw new IOException("Error reading CSF file.", ex);
            }

            if (options.OrderByKey)
                csf = csf.OrderByKey();

            return csf;
        }

        /// <summary>
        /// Write a stringtable file (.csf).
        /// </summary>
        public void WriteCsfFile(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            long originalPosition = stream.Position;

            try
            {
                using (var bw = new BinaryWriter(stream, Encoding.ASCII))
                {
                    bw.Write(Encoding.ASCII.GetBytes(" FSC"));
                    bw.Write(this.Version);
                    int numLabels = this._labels.Count;
                    bw.Write(numLabels);
                    int numValues = this._labels.Count;
                    bw.Write(numValues);
                    bw.Write(0);
                    bw.Write((int)this.Language);

                    foreach (var labelName in GetLabelsInWriteOrder())
                    {
                        if (!this._labels.TryGetValue(labelName, out string labelValue))
                            continue;

                        if (this.Options.Encoding1252WriteWorkaround)
                            labelValue = Encoding1252Workaround.ConvertsEncoding1252ToUnicode(labelValue);

                        if (!ValidateLabelName(labelName))
                            throw new InvalidDataException($"Invalid characters found in label name \"{labelName}\".");

                        bw.Write(Encoding.ASCII.GetBytes(" LBL"));
                        bw.Write(1);
                        byte[] labelNameBytes = Encoding.ASCII.GetBytes(labelName);
                        bw.Write(labelNameBytes.Length);
                        bw.Write(labelNameBytes);

                        bool hasExtra = this._extra.TryGetValue(labelName, out byte[] extraData);
                        if (hasExtra)
                            bw.Write(Encoding.ASCII.GetBytes("WRTS"));
                        else
                            bw.Write(Encoding.ASCII.GetBytes(" RTS"));

                        byte[] valueBytes = Encoding.Unicode.GetBytes(labelValue);
                        valueBytes = valueBytes.Select(v => (byte)(~v)).ToArray();
                        if (valueBytes.Length % 2 != 0)
                            throw new InvalidDataException("Unexpected UTF-16 LE bytes. Odd number of bytes detected.");
                        bw.Write(valueBytes.Length / 2);
                        bw.Write(valueBytes);

                        if (hasExtra && extraData != null)
                        {
                            bw.Write(extraData.Length);
                            bw.Write(extraData);
                        }
                    }
                }
            }
            catch (InvalidDataException)
            {
                stream.Position = originalPosition;
                throw;
            }
            catch (Exception ex)
            {
                stream.Position = originalPosition;
                throw new IOException("Error writing CSF file.", ex);
            }
        }

        #endregion

        #region Sorting Helpers

        /// <summary>
        /// Returns a new CsfFile object with labels ordered by key using simple case-insensitive sort.
        /// </summary>
        public CsfFile OrderByKey()
        {
            var csf = new CsfFile(this.Options) { Version = this.Version, Language = this.Language };
            var sortedKeys = this._labels.Keys.OrderBy(k => k, StringComparer.InvariantCultureIgnoreCase).ToList();
            foreach (var key in sortedKeys)
            {
                byte[] extra = this.GetExtra(key);
                csf.AddLabel(key, this._labels[key], extra);
            }
            return csf;
        }

        #endregion

        #region LLF File Operations

        /// <summary>
        /// Load a CSF file from an LLF representation.
        /// </summary>
        public static CsfFile LoadFromLlfFile(Stream stream) => CsfFileLlfHelper.LoadFromLlfFile(stream, new CsfFileOptions());

        /// <summary>
        /// Load a CSF file from an LLF representation with options.
        /// </summary>
        public static CsfFile LoadFromLlfFile(Stream stream, CsfFileOptions options) => CsfFileLlfHelper.LoadFromLlfFile(stream, options);

        /// <summary>
        /// Save the CSF file to an LLF representation.
        /// </summary>
        public void WriteLlfFile(Stream stream, string fileName = "converted") => CsfFileLlfHelper.WriteLlfFile(this, stream, fileName);

        #endregion

        #region TXT File Operations (CSFTool format)

        /// <summary>
        /// Load a CSF file from a TXT representation in CSFTool format.
        /// </summary>
        public static CsfFile LoadFromTxtFile(Stream stream) => LoadFromTxtFile(stream, new CsfFileOptions());

        /// <summary>
        /// Load a CSF file from a TXT representation in CSFTool format with options.
        /// </summary>
        public static CsfFile LoadFromTxtFile(Stream stream, CsfFileOptions options) => CsfFileTxtHelper.LoadFromTxtFile(stream, options);

        /// <summary>
        /// Save the CSF file to a TXT representation in CSFTool format.
        /// </summary>
        public void WriteTxtFile(Stream stream) => CsfFileTxtHelper.WriteTxtFile(this, stream);

        #endregion

        #region Excel File Operations

        /// <summary>
        /// Load a CSF file from an Excel representation (.xlsx or .xls).
        /// </summary>
        public static CsfFile LoadFromExcelFile(Stream stream) => CsfFileExcelHelper.LoadFromExcelFile(stream, new CsfFileOptions());

        /// <summary>
        /// Load a CSF file from an Excel representation with options.
        /// </summary>
        public static CsfFile LoadFromExcelFile(Stream stream, CsfFileOptions options) => CsfFileExcelHelper.LoadFromExcelFile(stream, options);

        /// <summary>
        /// Save the CSF file to an Excel representation.
        /// </summary>
        /// <param name="stream">Output stream.</param>
        /// <param name="xlsx">If true, save as XLSX (Excel 2007+); if false, save as XLS (Excel 97-2003).</param>
        public void WriteExcelFile(Stream stream, bool xlsx = true) => CsfFileExcelHelper.WriteExcelFile(this, stream, xlsx);

        #endregion

        #region CSV File Operations

        /// <summary>
        /// Load a CSF file from a CSV representation.
        /// </summary>
        public static CsfFile LoadFromCsvFile(Stream stream) => LoadFromCsvFile(stream, new CsfFileOptions());

        /// <summary>
        /// Load a CSF file from a CSV representation with options.
        /// </summary>
        public static CsfFile LoadFromCsvFile(Stream stream, CsfFileOptions options) => CsfFileCsvHelper.LoadFromCsvFile(stream, ",", null, options);

        /// <summary>
        /// Load a CSF file from a CSV representation with custom delimiter and encoding.
        /// </summary>
        public static CsfFile LoadFromCsvFile(Stream stream, string delimiter, Encoding encoding, CsfFileOptions options) => CsfFileCsvHelper.LoadFromCsvFile(stream, delimiter, encoding, options);

        /// <summary>
        /// Save the CSF file to a CSV representation.
        /// </summary>
        public void WriteCsvFile(Stream stream) => CsfFileCsvHelper.WriteCsvFile(this, stream, ",", null);

        /// <summary>
        /// Save the CSF file to a CSV representation with custom delimiter and encoding.
        /// </summary>
        public void WriteCsvFile(Stream stream, string delimiter, Encoding encoding) => CsfFileCsvHelper.WriteCsvFile(this, stream, delimiter, encoding);

        #endregion

        #region Other Format Wrappers (forward compatibility)

        /// <summary>Obsolete. Please use CsfFileIniHelper.LoadFromIniFile() instead.</summary>
        [Obsolete("Please use CsfFileIniHelper.LoadFromIniFile() instead.")]
        public static CsfFile LoadFromIniFile(Stream stream) => CsfFileIniHelper.LoadFromIniFile(stream);

        /// <summary>Obsolete. Please use CsfFileIniHelper.WriteIniFile() instead.</summary>
        [Obsolete("Please use CsfFileIniHelper.WriteIniFile() instead.")]
        public void WriteIniFile(Stream stream) => CsfFileIniHelper.WriteIniFile(this, stream);

        #endregion

        #region Helper Methods

        /// <summary>
        /// Check whether the name of a label is valid. A valid label name is an ASCII string without tabs, line breaks, and invisible characters.
        /// Spaces are tolerated.
        /// </summary>
        public static bool ValidateLabelName(string labelName)
        {
            if (string.IsNullOrEmpty(labelName))
                return false;
            foreach (char c in labelName)
                if (c < 32 || c >= 127)
                    return false;
            return true;
        }

        #endregion
    }
}