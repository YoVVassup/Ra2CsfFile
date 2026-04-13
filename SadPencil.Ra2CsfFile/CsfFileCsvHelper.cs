using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace SadPencil.Ra2CsfFile
{
    /// <summary>
    /// Helper class for working with CSV files representing CSF string tables.
    /// Supports reading and writing with three columns: Label Name, Value, Extra.
    /// Extra column can be treated as UTF-8 text or Base64 according to CsfFileOptions.TreatExtraAsText.
    /// Supports metadata lines starting with '#' and sep= line for delimiter specification.
    /// Supports label ordering via CsfFileOptions.OrderByKey.
    /// </summary>
    public static class CsfFileCsvHelper
    {
        /// <summary>
        /// Loads a CSF file from a CSV file.
        /// Supports optional sep= line as first line (Excel compatibility).
        /// Supports metadata lines starting with '#' (e.g., #version=3, #language=0).
        /// Expects header row after metadata: Label Name, Value, Extra.
        /// </summary>
        /// <param name="stream">Stream containing the CSV file.</param>
        /// <param name="delimiter">Delimiter character (default ','). If null, will try to detect from sep= line or default to ','.</param>
        /// <param name="encoding">Text encoding (default UTF-8).</param>
        /// <param name="options">Loading options. If null, default options are used.</param>
        /// <returns>Loaded CSF file.</returns>
        /// <exception cref="ArgumentNullException">If stream is null.</exception>
        /// <exception cref="InvalidDataException">If the CSV format is invalid or label names are invalid.</exception>
        public static CsfFile LoadFromCsvFile(Stream stream, string delimiter = ",", Encoding encoding = null, CsfFileOptions options = null)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            options = options ?? new CsfFileOptions();
            encoding = encoding ?? Encoding.UTF8;
            var csf = new CsfFile(options);

            using (var reader = new StreamReader(stream, encoding, true, 1024))
            {
                // Read first line to check for sep= line
                string firstLine = reader.ReadLine();
                if (firstLine == null)
                    throw new InvalidDataException("CSV file is empty.");

                string actualDelimiter = delimiter;
                if (firstLine.StartsWith("sep="))
                {
                    actualDelimiter = firstLine.Substring(4);
                    if (string.IsNullOrEmpty(actualDelimiter))
                        actualDelimiter = delimiter;
                    // Read next line as potential header or metadata
                    firstLine = reader.ReadLine();
                    if (firstLine == null)
                        throw new InvalidDataException("CSV file has sep= line but no data.");
                }

                // Parse metadata lines (starting with '#')
                while (firstLine != null && firstLine.StartsWith("#"))
                {
                    ParseMetadataLine(csf, firstLine);
                    firstLine = reader.ReadLine();
                }

                // First non-metadata line should be header
                if (firstLine == null)
                    throw new InvalidDataException("CSV file has no header row.");

                List<string> headerFields = ParseCsvLine(firstLine, actualDelimiter);
                if (headerFields.Count < 2)
                    throw new InvalidDataException("CSV header must have at least 'Label Name' and 'Value' columns.");

                // Read data rows
                string line;
                while ((line = ReadCsvLine(reader)) != null)
                {
                    List<string> fields = ParseCsvLine(line, actualDelimiter);
                    if (fields.Count < 2)
                        continue;

                    string label = fields[0];
                    string value = fields.Count > 1 ? fields[1] : "";
                    string extraStr = fields.Count > 2 ? fields[2] : null;

                    if (string.IsNullOrEmpty(label))
                        continue;

                    if (!CsfFile.ValidateLabelName(label))
                        throw new InvalidDataException($"Invalid label name '{label}' in CSV.");

                    byte[] extra = null;
                    if (!string.IsNullOrEmpty(extraStr))
                    {
                        if (options.TreatExtraAsText)
                            extra = encoding.GetBytes(extraStr);
                        else
                            extra = Convert.FromBase64String(extraStr);
                    }

                    csf.AddLabel(label, value, extra);
                }
            }

            if (options.OrderByKey)
                csf = csf.OrderByKey();

            return csf;
        }

        /// <summary>
        /// Saves a CSF file to a CSV file.
        /// Writes sep= line, metadata lines (#version, #language), header, and data.
        /// </summary>
        /// <param name="csf">CSF file to save.</param>
        /// <param name="stream">Output stream.</param>
        /// <param name="delimiter">Delimiter character (default ',').</param>
        /// <param name="encoding">Text encoding (default UTF-8).</param>
        /// <exception cref="ArgumentNullException">If csf or stream is null.</exception>
        /// <exception cref="InvalidDataException">If label names contain invalid characters.</exception>
        public static void WriteCsvFile(CsfFile csf, Stream stream, string delimiter = ",", Encoding encoding = null)
        {
            if (csf == null) throw new ArgumentNullException(nameof(csf));
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            encoding = encoding ?? Encoding.UTF8;

            using (var writer = new StreamWriter(stream, encoding, 1024))
            {
                // Write sep= line for Excel compatibility
                writer.WriteLine($"sep={delimiter}");

                // Write metadata lines
                writer.WriteLine($"#version={csf.Version}");
                writer.WriteLine($"#language={(int)csf.Language}");

                // Write header
                string escapedHeaderLabel = EscapeCsvField("Label Name", delimiter);
                string escapedHeaderValue = EscapeCsvField("Value", delimiter);
                string escapedHeaderExtra = EscapeCsvField("Extra", delimiter);
                writer.WriteLine($"{escapedHeaderLabel}{delimiter}{escapedHeaderValue}{delimiter}{escapedHeaderExtra}");

                // Write labels in determined order
                foreach (string labelName in csf.GetLabelsInWriteOrder())
                {
                    if (!csf.Labels.TryGetValue(labelName, out string labelValue))
                        continue;

                    if (!CsfFile.ValidateLabelName(labelName))
                        throw new InvalidDataException($"Invalid characters in label name \"{labelName}\".");

                    string escapedLabel = EscapeCsvField(labelName, delimiter);
                    string escapedValue = EscapeCsvField(labelValue ?? "", delimiter);

                    byte[] extra = csf.GetExtra(labelName);
                    string extraStr = "";
                    if (extra != null)
                    {
                        if (csf.Options.TreatExtraAsText)
                            extraStr = encoding.GetString(extra);
                        else
                            extraStr = Convert.ToBase64String(extra);
                    }
                    string escapedExtra = EscapeCsvField(extraStr, delimiter);

                    writer.WriteLine($"{escapedLabel}{delimiter}{escapedValue}{delimiter}{escapedExtra}");
                }
            }
        }

        #region CSV Parsing Helpers

        private static void ParseMetadataLine(CsfFile csf, string line)
        {
            if (string.IsNullOrEmpty(line) || !line.StartsWith("#"))
                return;

            string content = line.Substring(1).Trim();
            int eqIdx = content.IndexOf('=');
            if (eqIdx <= 0) return;

            string key = content.Substring(0, eqIdx).Trim().ToLower();
            string value = content.Substring(eqIdx + 1).Trim();

            switch (key)
            {
                case "version":
                    if (int.TryParse(value, out int ver))
                        csf.Version = ver;
                    break;
                case "language":
                    if (int.TryParse(value, out int lang))
                        csf.Language = CsfLangHelper.GetCsfLang(lang);
                    break;
            }
        }

        private static string ReadCsvLine(StreamReader reader)
        {
            StringBuilder line = new StringBuilder();
            bool inQuotedField = false;
            int peekChar;

            while ((peekChar = reader.Peek()) >= 0)
            {
                char c = (char)peekChar;
                if (c == '"')
                {
                    inQuotedField = !inQuotedField;
                    line.Append(c);
                    reader.Read();
                }
                else if (c == '\n' && !inQuotedField)
                {
                    reader.Read();
                    break;
                }
                else if (c == '\r')
                {
                    reader.Read();
                    if (reader.Peek() == '\n' && !inQuotedField)
                    {
                        reader.Read();
                        break;
                    }
                    else
                    {
                        line.Append('\r');
                    }
                }
                else
                {
                    line.Append(c);
                    reader.Read();
                }
            }

            return line.Length > 0 ? line.ToString() : null;
        }

        private static List<string> ParseCsvLine(string line, string delimiter)
        {
            var fields = new List<string>();
            var currentField = new StringBuilder();
            bool inQuotes = false;
            int i = 0;

            while (i < line.Length)
            {
                char c = line[i];

                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        currentField.Append('"');
                        i += 2;
                        continue;
                    }
                    inQuotes = !inQuotes;
                    i++;
                }
                else if (!inQuotes && i + delimiter.Length <= line.Length && line.Substring(i, delimiter.Length) == delimiter)
                {
                    fields.Add(currentField.ToString());
                    currentField.Clear();
                    i += delimiter.Length;
                }
                else
                {
                    currentField.Append(c);
                    i++;
                }
            }
            fields.Add(currentField.ToString());

            return fields;
        }

        private static string EscapeCsvField(string field, string delimiter)
        {
            bool needsQuotes = field.Contains(delimiter) || field.Contains("\"") || field.Contains("\n") || field.Contains("\r");
            if (!needsQuotes)
                return field;

            string escaped = field.Replace("\"", "\"\"");
            return $"\"{escaped}\"";
        }

        #endregion
    }
}