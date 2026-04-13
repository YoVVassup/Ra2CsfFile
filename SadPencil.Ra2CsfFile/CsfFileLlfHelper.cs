using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;

namespace SadPencil.Ra2CsfFile
{
    /// <summary>
    /// Helper class for working with LLF files representing CSF string tables.
    /// Supports extra data (WRTS) as a Base64-encoded string or plain text using comment lines.
    /// Supports metadata via # version: and # language: comments.
    /// Supports label ordering via CsfFileOptions.OrderByKey.
    /// </summary>
    public static class CsfFileLlfHelper
    {
        /// <summary>
        /// Converts label name to valid CSF format (lowercase ASCII, no special characters)
        /// </summary>
        private static string ConvertToValidLabelName(string labelName)
        {
            if (string.IsNullOrEmpty(labelName))
                return labelName;

            // Convert to lowercase
            labelName = labelName.ToLowerInvariant();

            // Remove or replace invalid characters - only allow letters, numbers, underscore, hyphen, colon
            var validChars = labelName
                .Where(c => (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '_' || c == '-' || c == ':')
                .ToArray();

            if (validChars.Length == 0)
            {
                return "invalid_label";
            }

            return new string(validChars);
        }

        /// <summary>
        /// Parses a line to extract full key (including section) and value
        /// </summary>
        private static bool TryParseLine(string line, out string fullKey, out string value, out bool isMultiLine)
        {
            fullKey = null;
            value = null;
            isMultiLine = false;

            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                return false;

            int firstColonIndex = line.IndexOf(':');
            if (firstColonIndex < 0)
                return false;

            int secondColonIndex = line.IndexOf(':', firstColonIndex + 1);
            if (secondColonIndex < 0)
            {
                fullKey = line.Substring(0, firstColonIndex).Trim();
                value = line.Substring(firstColonIndex + 1).Trim();
            }
            else
            {
                fullKey = line.Substring(0, secondColonIndex).Trim();
                value = line.Substring(secondColonIndex + 1).Trim();
            }

            if (value.StartsWith(">-"))
            {
                isMultiLine = true;
                value = "";
            }

            return true;
        }

        /// <summary>
        /// Loads a CSF file from an LLF representation.
        /// </summary>
        /// <param name="stream">Stream with LLF file.</param>
        /// <param name="options">Loading options. If null, default options will be used.</param>
        /// <returns>Loaded CSF file.</returns>
        /// <exception cref="ArgumentNullException">If the stream is null.</exception>
        /// <exception cref="InvalidDataException">If the file format is incorrect.</exception>
        public static CsfFile LoadFromLlfFile(Stream stream, CsfFileOptions options = null)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            options = options ?? new CsfFileOptions();
            var csf = new CsfFile(options);
            
            csf.Version = 3;
            csf.Language = CsfLang.EnglishUS;

            using (var sr = new StreamReader(stream, Encoding.UTF8))
            {
                string line;
                string currentKey = null;
                StringBuilder currentValue = null;
                bool inMultiLine = false;
                string currentExtra = null;

                while ((line = sr.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        if (inMultiLine && currentValue != null)
                        {
                            currentValue.AppendLine();
                        }
                        continue;
                    }
                    
                    // Process metadata comments
                    if (line.StartsWith("#"))
                    {
                        var versionMatch = Regex.Match(line, @"# csf version:\s*(\d+)");
                        if (versionMatch.Success)
                        {
                            csf.Version = int.Parse(versionMatch.Groups[1].Value);
                            continue;
                        }
                        
                        var langMatch = Regex.Match(line, @"# language:\s*(\d+)");
                        if (langMatch.Success)
                        {
                            int langValue = int.Parse(langMatch.Groups[1].Value);
                            csf.Language = CsfLangHelper.GetCsfLang(langValue);
                            continue;
                        }

                        var extraMatch = Regex.Match(line, @"# extra:\s*(.+)$");
                        if (extraMatch.Success)
                        {
                            currentExtra = extraMatch.Groups[1].Value;
                        }
                        
                        continue;
                    }

                    // If we're in multi-line mode and the line is indented, it's a continuation
                    if (inMultiLine && currentValue != null)
                    {
                        if (line.StartsWith("  "))
                        {
                            currentValue.AppendLine(line.Substring(2).TrimEnd());
                        }
                        else if (string.IsNullOrWhiteSpace(line))
                        {
                            currentValue.AppendLine();
                        }
                        else
                        {
                            // End of multi-line value - save current entry
                            string validCurrentKey = ConvertToValidLabelName(currentKey);
                            if (!string.IsNullOrEmpty(validCurrentKey))
                            {
                                byte[] extra = null;
                                if (!string.IsNullOrEmpty(currentExtra))
                                {
                                    if (options.TreatExtraAsText)
                                        extra = Encoding.UTF8.GetBytes(currentExtra);
                                    else
                                        extra = Convert.FromBase64String(currentExtra);
                                }
                                csf.AddLabel(validCurrentKey, currentValue.ToString().Trim(), extra);
                            }
                            
                            inMultiLine = false;
                            currentKey = null;
                            currentValue = null;
                            currentExtra = null;
                            
                            if (TryParseLine(line, out string newKey, out string newValue, out bool newIsMultiLine))
                            {
                                currentKey = newKey;
                                if (newIsMultiLine)
                                {
                                    inMultiLine = true;
                                    currentValue = new StringBuilder();
                                }
                                else
                                {
                                    currentValue = new StringBuilder(newValue);
                                    string validNewKey = ConvertToValidLabelName(currentKey);
                                    if (!string.IsNullOrEmpty(validNewKey))
                                    {
                                        byte[] extra = null;
                                        if (!string.IsNullOrEmpty(currentExtra))
                                        {
                                            if (options.TreatExtraAsText)
                                                extra = Encoding.UTF8.GetBytes(currentExtra);
                                            else
                                                extra = Convert.FromBase64String(currentExtra);
                                        }
                                        csf.AddLabel(validNewKey, currentValue.ToString(), extra);
                                    }
                                    currentKey = null;
                                    currentValue = null;
                                    currentExtra = null;
                                }
                            }
                        }
                        continue;
                    }

                    // Finalize previous entry if exists
                    if (currentKey != null && currentValue != null)
                    {
                        string validCurrentKey = ConvertToValidLabelName(currentKey);
                        if (!string.IsNullOrEmpty(validCurrentKey))
                        {
                            byte[] extra = null;
                            if (!string.IsNullOrEmpty(currentExtra))
                            {
                                if (options.TreatExtraAsText)
                                    extra = Encoding.UTF8.GetBytes(currentExtra);
                                else
                                    extra = Convert.FromBase64String(currentExtra);
                            }
                            csf.AddLabel(validCurrentKey, currentValue.ToString().Trim(), extra);
                        }
                        currentKey = null;
                        currentValue = null;
                        inMultiLine = false;
                        currentExtra = null;
                    }

                    // Parse new line
                    if (TryParseLine(line, out string fullKey, out string value, out bool isMultiLine))
                    {
                        currentKey = fullKey;
                        
                        if (isMultiLine)
                        {
                            inMultiLine = true;
                            currentValue = new StringBuilder();
                        }
                        else
                        {
                            currentValue = new StringBuilder(value);
                            string validCurrentKey = ConvertToValidLabelName(currentKey);
                            if (!string.IsNullOrEmpty(validCurrentKey))
                            {
                                byte[] extra = null;
                                if (!string.IsNullOrEmpty(currentExtra))
                                {
                                    if (options.TreatExtraAsText)
                                        extra = Encoding.UTF8.GetBytes(currentExtra);
                                    else
                                        extra = Convert.FromBase64String(currentExtra);
                                }
                                csf.AddLabel(validCurrentKey, currentValue.ToString(), extra);
                            }
                            currentKey = null;
                            currentValue = null;
                            currentExtra = null;
                        }
                    }
                }
                
                // Add last entry if exists
                if (currentKey != null && currentValue != null)
                {
                    string validCurrentKey = ConvertToValidLabelName(currentKey);
                    if (!string.IsNullOrEmpty(validCurrentKey))
                    {
                        byte[] extra = null;
                        if (!string.IsNullOrEmpty(currentExtra))
                        {
                            if (options.TreatExtraAsText)
                                extra = Encoding.UTF8.GetBytes(currentExtra);
                            else
                                extra = Convert.FromBase64String(currentExtra);
                        }
                        csf.AddLabel(validCurrentKey, currentValue.ToString().Trim(), extra);
                    }
                }
            }

            if (options.OrderByKey)
                csf = csf.OrderByKey();

            return csf;
        }

        /// <summary>
        /// Saves a CSF file to an LLF representation.
        /// </summary>
        /// <param name="csf">CSF file to save.</param>
        /// <param name="stream">Stream for writing.</param>
        /// <param name="fileName">File name to include in the header metadata.</param>
        /// <exception cref="ArgumentNullException">If the CSF file or stream is null.</exception>
        public static void WriteLlfFile(CsfFile csf, Stream stream, string fileName = "converted")
        {
            if (csf == null) throw new ArgumentNullException(nameof(csf));
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            
            using (var sw = new StreamWriter(stream, Encoding.UTF8))
            {
                sw.WriteLine($"# {fileName}");
                sw.WriteLine($"# version: {csf.Version}");
                sw.WriteLine($"# language: {(int)csf.Language}");
                sw.WriteLine($"# csf count: {csf.Labels.Count}");
                sw.WriteLine($"# build time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n");

                // Write labels in determined order
                foreach (string labelName in csf.GetLabelsInWriteOrder())
                {
                    if (!csf.Labels.TryGetValue(labelName, out string labelValue))
                        continue;

                    byte[] extra = csf.GetExtra(labelName);
                    if (extra != null)
                    {
                        string extraStr;
                        if (csf.Options.TreatExtraAsText)
                            extraStr = Encoding.UTF8.GetString(extra);
                        else
                            extraStr = Convert.ToBase64String(extra);
                        sw.WriteLine($"# extra: {extraStr}");
                    }

                    if (labelValue.Contains("\n"))
                    {
                        sw.WriteLine($"{labelName}: >-");
                        string[] lines = labelValue.Split('\n');
                        foreach (string line in lines)
                        {
                            sw.WriteLine($"  {line.TrimEnd()}");
                        }
                    }
                    else
                    {
                        sw.WriteLine($"{labelName}: {labelValue}");
                    }
                }
            }
        }
    }
}