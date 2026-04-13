using System;
using System.IO;
using System.Text;

namespace SadPencil.Ra2CsfFile
{
    /// <summary>
    /// Helper class for working with TXT files in CSFTool format representing CSF string tables.
    /// Supports extra data (WRTS) as a Base64-encoded string or plain text using a custom prefix.
    /// Supports metadata via lines starting with "!metadata|key|value".
    /// Supports label ordering via CsfFileOptions.OrderByKey.
    /// Format: label|value
    /// Extra data: "!extra|labelname|base64data"
    /// Metadata: "!metadata|version|3" or "!metadata|language|0"
    /// For compatibility with original CSFTool, extra data is stored as Base64 when not plain text.
    /// </summary>
    public static class CsfFileTxtHelper
    {
        private const string LabelSeparator = "|";
        private const string NewLineString = @"\n";
        private const string ExtraPrefix = "!extra|";
        private const string MetadataPrefix = "!metadata|";

        /// <summary>
        /// Loads a CSF file from a TXT representation in CSFTool format.
        /// Supports extra data lines: "!extra|labelname|base64data"
        /// Supports metadata lines: "!metadata|version|3", "!metadata|language|0"
        /// </summary>
        public static CsfFile LoadFromTxtFile(Stream stream, CsfFileOptions options = null)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            options = options ?? new CsfFileOptions();
            var csf = new CsfFile(options);
            
            // Default metadata
            csf.Version = 3;
            csf.Language = CsfLang.EnglishUS;

            using (var sr = new StreamReader(stream, Encoding.UTF8, true, 1024))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    ProcessLine(csf, line, options);
                }
            }

            if (options.OrderByKey)
                csf = csf.OrderByKey();

            return csf;
        }

        private static void ProcessLine(CsfFile csf, string line, CsfFileOptions options)
        {
            if (string.IsNullOrEmpty(line))
                return;

            // Skip comments (lines starting with ';')
            if (line.StartsWith(";") || string.IsNullOrWhiteSpace(line))
                return;

            // Handle metadata line
            if (line.StartsWith(MetadataPrefix))
            {
                string metaLine = line.Substring(MetadataPrefix.Length);
                int idx = metaLine.IndexOf(LabelSeparator);
                if (idx > 0)
                {
                    string key = metaLine.Substring(0, idx).Trim().ToLower();
                    string val = metaLine.Substring(idx + LabelSeparator.Length).Trim();
                    
                    switch (key)
                    {
                        case "version":
                            if (int.TryParse(val, out int ver))
                                csf.Version = ver;
                            break;
                        case "language":
                            if (int.TryParse(val, out int lang))
                                csf.Language = CsfLangHelper.GetCsfLang(lang);
                            break;
                    }
                }
                return;
            }

            // Handle extra data line
            if (line.StartsWith(ExtraPrefix))
            {
                string extraLine = line.Substring(ExtraPrefix.Length);
                int idx = extraLine.IndexOf(LabelSeparator);
                if (idx > 0)
                {
                    string extraLabel = extraLine.Substring(0, idx);
                    string extraDataStr = extraLine.Substring(idx + LabelSeparator.Length);
                    if (CsfFile.ValidateLabelName(extraLabel))
                    {
                        byte[] extra;
                        if (options.TreatExtraAsText)
                            extra = Encoding.UTF8.GetBytes(extraDataStr);
                        else
                            extra = Convert.FromBase64String(extraDataStr);
                        csf.SetExtra(extraLabel, extra);
                    }
                }
                return;
            }

            // Skip lines starting with * followed by separator (like CSFTool does)
            if (line.IndexOf('*') == 0 && line.Length > 1 && line[1] == '|')
                return;

            int separatorIdx = line.IndexOf(LabelSeparator);
            if (separatorIdx < 1)
                return;

            string labelName = line.Substring(0, separatorIdx);
            string labelValue = line.Substring(separatorIdx + LabelSeparator.Length);

            // Replace newline escape sequences with actual newlines
            labelValue = labelValue.Replace(NewLineString, "\n");

            // Add or replace the label in CSF (preserve existing extra if already set)
            byte[] existingExtra = csf.GetExtra(labelName);
            csf.AddLabel(labelName, labelValue, existingExtra);
        }

        /// <summary>
        /// Saves a CSF file to a TXT representation in CSFTool format.
        /// Writes metadata lines (!metadata|...), then labels, and extra data lines (!extra|...).
        /// </summary>
        public static void WriteTxtFile(CsfFile csf, Stream stream)
        {
            if (csf == null)
                throw new ArgumentNullException(nameof(csf));
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            try
            {
                using (var sw = new StreamWriter(stream, Encoding.UTF8, 1024))
                {
                    // Write metadata lines
                    sw.WriteLine($"{MetadataPrefix}version{LabelSeparator}{csf.Version}");
                    sw.WriteLine($"{MetadataPrefix}language{LabelSeparator}{(int)csf.Language}");

                    // Write labels in determined order
                    foreach (string labelName in csf.GetLabelsInWriteOrder())
                    {
                        if (!csf.Labels.TryGetValue(labelName, out string labelValue))
                            continue;

                        // Convert to uppercase for CSFTool compatibility
                        string labelUpper = labelName.ToUpperInvariant();
                        string value = labelValue ?? "";

                        // Replace newlines with escape sequences
                        value = value.Replace("\n", NewLineString);
                        value = value.Replace("\r\n", NewLineString);

                        // Write main entry
                        sw.WriteLine($"{labelUpper}{LabelSeparator}{value}");

                        // Write extra data if present
                        byte[] extra = csf.GetExtra(labelName);
                        if (extra != null)
                        {
                            string extraStr;
                            if (csf.Options.TreatExtraAsText)
                                extraStr = Encoding.UTF8.GetString(extra);
                            else
                                extraStr = Convert.ToBase64String(extra);
                            sw.WriteLine($"{ExtraPrefix}{labelUpper}{LabelSeparator}{extraStr}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new IOException("Error writing TXT file.", ex);
            }
        }
    }
}