using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

[assembly: CLSCompliant(true)]
namespace SadPencil.Ra2CsfFile
{
    /// <summary>
    /// This class reads and writes the stringtable file (.csf) that is used by RA2/YR.
    /// </summary>
    public class CsfFile : ICloneable
    {
        // https://modenc.renegadeprojects.com/CSF_File_Format

        /// <summary>
        /// This option controls the behavior of CsfFile.
        /// </summary>
        public CsfFileOptions Options { get; set; } = new CsfFileOptions();

        private readonly Dictionary<String, String> _labels = new Dictionary<String, String>();
        /// <summary>
        /// The labels of this file. Each label has a name, and a string, which are the dictionary keys and values.        
        /// </summary>
        public IDictionary<String, String> Labels => this._labels;

        /// <summary>
        /// The line break characters between the multiple lines in the label value.
        /// </summary>
        public static String LineBreakCharacters { get; } = "\n";

        /// <summary>
        /// The language of this file.
        /// </summary>
        public CsfLang Language { get; set; } = CsfLang.EnglishUS;
        
        /// <summary>
        /// The version number of the CSF format.
        /// </summary>
        public Int32 Version { get; set; } = 3;

        /// <summary>
        /// Add or replace a label to the string table.
        /// </summary>
        /// <param name="labelName">The label name. Must be lowercase.</param>
        /// <param name="labelValue">The label value.</param>
        /// <returns>True if an existing element is found and replaced.</returns>
        /// <exception cref="ArgumentException">If label name is invalid.</exception>
        /// <exception cref="ArgumentNullException">If label name or value is null.</exception>
        public Boolean AddLabel(String labelName, String labelValue)
        {
            if (labelName == null) throw new ArgumentNullException(nameof(labelName));
            if (labelValue == null) throw new ArgumentNullException(nameof(labelValue));
            if (!ValidateLabelName(labelName))
                throw new ArgumentException("Invalid characters found in label name.", nameof(labelName));
            if (labelName != LowercaseLabelName(labelName))
                throw new ArgumentException("Label name should be in lower case.", nameof(labelName));

            if (this.Labels.ContainsKey(labelName))
            {
                this._labels[labelName] = labelValue;
                return true;
            }
            else
            {
                this._labels.Add(labelName, labelValue);
                return false;
            }
        }

        /// <summary>
        /// Remove a label from the string table.
        /// </summary>
        /// <param name="labelName">The label name.</param>
        /// <returns>True if the element is found and removed.</returns>
        public Boolean RemoveLabel(String labelName) => this._labels.Remove(labelName);

        /// <summary>
        /// Create an empty stringtable file with default options.
        /// </summary>
        public CsfFile() { }

        /// <summary>
        /// Create an empty stringtable file with given options.
        /// </summary>
        /// <param name="options">The CsfFileOptions.</param>
        /// <exception cref="ArgumentNullException">If options is null.</exception>
        public CsfFile(CsfFileOptions options)
        {
            this.Options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Clone the CsfFile.
        /// </summary>
        /// <param name="csf">The CsfFile object.</param>
        /// <exception cref="ArgumentNullException">If csf is null.</exception>
        public CsfFile(CsfFile csf)
        {
            if (csf == null) throw new ArgumentNullException(nameof(csf));

            this.Version = csf.Version;
            this.Language = csf.Language;
            this.Options = csf.Options;
            this._labels = new Dictionary<String, String>(csf._labels);
        }

        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>A new object that is a copy of this instance.</returns>
        public Object Clone() => new CsfFile(this);

        /// <summary>
        /// Load an existing stringtable file (.csf).
        /// </summary>
        /// <param name="stream">The file stream of a stringtable file (.csf).</param>
        /// <returns>The loaded CsfFile.</returns>
        /// <exception cref="ArgumentNullException">If stream is null.</exception>
        /// <exception cref="InvalidDataException">If file format is invalid.</exception>
        /// <exception cref="IOException">If I/O error occurs.</exception>
        public static CsfFile LoadFromCsfFile(Stream stream) => LoadFromCsfFile(stream, new CsfFileOptions());

        /// <summary>
        /// Load an existing stringtable file (.csf) with options.
        /// </summary>
        /// <param name="stream">The file stream of a stringtable file (.csf).</param>
        /// <param name="options">The CsfFileOptions.</param>
        /// <returns>The loaded CsfFile.</returns>
        /// <exception cref="ArgumentNullException">If stream or options are null.</exception>
        /// <exception cref="InvalidDataException">If file format is invalid.</exception>
        /// <exception cref="IOException">If I/O error occurs.</exception>
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
                    // read headers
                    Byte[] headerId = br.ReadBytes(4);
                    if (!headerId.SequenceEqual(Encoding.ASCII.GetBytes(" FSC")))
                        throw new InvalidDataException("Invalid CSF file header.");

                    csf.Version = br.ReadInt32();

                    Int32 labelsNum = br.ReadInt32();
                    Int32 stringsNum = br.ReadInt32();
                    _ = br.ReadInt32(); // unused
                    csf.Language = CsfLangHelper.GetCsfLang(br.ReadInt32());

                    // read labels
                    for (Int32 iLabel = 0; iLabel < labelsNum; iLabel++)
                    {
                        // read label names
                        while (true)
                        {
                            Byte[] labelId = br.ReadBytes(4);
                            if (labelId.SequenceEqual(Encoding.ASCII.GetBytes(" LBL")))
                                break;
                            if (labelId.Length != 4)
                                throw new InvalidDataException("Unexpected end of file.");
                        }

                        Int32 numValues = br.ReadInt32();
                        Int32 labelNameLength = br.ReadInt32();
                        Byte[] labelName = br.ReadBytes(labelNameLength);
                        String labelNameStr;
                        try
                        {
                            labelNameStr = Encoding.ASCII.GetString(labelName);
                            labelNameStr = LowercaseLabelName(labelNameStr);
                        }
                        catch (Exception ex)
                        {
                            throw new InvalidDataException($"Invalid label name at position {stream.Position}.", ex);
                        }

                        if (!ValidateLabelName(labelNameStr))
                            throw new InvalidDataException($"Invalid characters found in label name \"{labelNameStr}\" at position {stream.Position}.");

                        // read values
                        String labelValue = null;
                        for (Int32 iValue = 0; iValue < numValues; iValue++)
                        {
                            Byte[] labelValueType = br.ReadBytes(4);
                            Boolean labelHasExtraValue;

                            if (labelValueType.SequenceEqual(Encoding.ASCII.GetBytes(" RTS")))
                            {
                                labelHasExtraValue = false;
                            }
                            else if (labelValueType.SequenceEqual(Encoding.ASCII.GetBytes("WRTS")))
                            {
                                labelHasExtraValue = true;
                            }
                            else
                            {
                                throw new InvalidDataException($"Invalid label value type at position {stream.Position}.");
                            }

                            Int32 valueLength = br.ReadInt32();
                            Byte[] value = br.ReadBytes(valueLength * 2);
                            value = value.Select(v => (Byte)(~v)).ToArray();
                            String valueStr;
                            try
                            {
                                valueStr = Encoding.Unicode.GetString(value);
                                if (options.Encoding1252ReadWorkaround)
                                {
                                    valueStr = Encoding1252Workaround.ConvertsUnicodeToEncoding1252(valueStr);
                                }
                            }
                            catch (Exception ex)
                            {
                                throw new InvalidDataException($"Invalid label value string at position {stream.Position}.", ex);
                            }

                            if (labelHasExtraValue)
                            {
                                Int32 extLength = br.ReadInt32();
                                _ = br.ReadBytes(extLength);
                            }

                            if (iValue == 0)
                            {
                                labelValue = valueStr;
                            }
                        }

                        if (labelValue != null)
                        {
                            _ = csf.AddLabel(labelNameStr, labelValue);
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
                throw new IOException("Error reading CSF file.", ex);
            }
            
            return csf;
        }

        /// <summary>
        /// Write a stringtable file (.csf).
        /// </summary>
        /// <param name="stream">The file stream of a new stringtable file (.csf).</param>
        /// <exception cref="ArgumentNullException">If stream is null.</exception>
        /// <exception cref="InvalidDataException">If label data is invalid.</exception>
        /// <exception cref="IOException">If I/O error occurs.</exception>
        public void WriteCsfFile(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            long originalPosition = stream.Position;
            
            try
            {
                using (var bw = new BinaryWriter(stream, Encoding.ASCII))
                {
                    // write header
                    bw.Write(Encoding.ASCII.GetBytes(" FSC"));
                    bw.Write(this.Version);
                    Int32 numLabels = this.Labels.Count;
                    bw.Write(numLabels);
                    Int32 numValues = this.Labels.Count;
                    bw.Write(numValues);
                    bw.Write(0); // unused
                    bw.Write((Int32)this.Language);

                    // write labels
                    foreach (var labelNameValues in this.Labels)
                    {
                        String labelName = labelNameValues.Key;
                        String labelValue = labelNameValues.Value;

                        if (this.Options.Encoding1252WriteWorkaround)
                        {
                            labelValue = Encoding1252Workaround.ConvertsEncoding1252ToUnicode(labelValue);
                        }

                        if (!ValidateLabelName(labelName))
                            throw new InvalidDataException($"Invalid characters found in label name \"{labelName}\".");

                        bw.Write(Encoding.ASCII.GetBytes(" LBL"));
                        bw.Write(1);
                        Byte[] labelNameBytes = Encoding.ASCII.GetBytes(labelName);
                        bw.Write(labelNameBytes.Length);
                        bw.Write(labelNameBytes);

                        // write values 
                        bw.Write(Encoding.ASCII.GetBytes(" RTS"));
                        Byte[] valueBytes = Encoding.Unicode.GetBytes(labelValue);
                        valueBytes = valueBytes.Select(v => (Byte)(~v)).ToArray();
                        if (valueBytes.Length % 2 != 0)
                            throw new InvalidDataException("Unexpected UTF-16 LE bytes. Odd number of bytes detected.");
                        bw.Write(valueBytes.Length / 2);
                        bw.Write(valueBytes);
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

        /// <summary>
        /// Check whether the name of a label is valid. A valid label name is an ASCII string without control characters.
        /// </summary>
        /// <param name="labelName">The name of a label to be checked.</param>
        /// <returns>Whether the name is valid or not.</returns>
        public static Boolean ValidateLabelName(String labelName) =>
            !string.IsNullOrEmpty(labelName) && !labelName.ToCharArray().Any(c => (c < 32 || c >= 127));

        /// <summary>
        /// Converts label name to lowercase for case-insensitive comparison.
        /// </summary>
        /// <param name="labelName">The label name to be converted.</param>
        /// <returns>Lowercase label name.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase")]
        public static String LowercaseLabelName(String labelName) => labelName?.ToLowerInvariant();
    }
}