using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;

namespace SadPencil.Ra2CsfFile
{
    /// <summary>
    /// Helper class for working with LLF files representing CSF string tables.
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
                // If no valid characters, create a default name
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

            // Find the first colon (separates section from key)
            int firstColonIndex = line.IndexOf(':');
            if (firstColonIndex < 0)
                return false;

            // Find the second colon (separates key from value)
            int secondColonIndex = line.IndexOf(':', firstColonIndex + 1);
            if (secondColonIndex < 0)
            {
                // If only one colon, treat entire prefix as key
                fullKey = line.Substring(0, firstColonIndex).Trim();
                value = line.Substring(firstColonIndex + 1).Trim();
            }
            else
            {
                // Extract section:key as fullKey (everything before second colon)
                fullKey = line.Substring(0, secondColonIndex).Trim();
                value = line.Substring(secondColonIndex + 1).Trim();
            }

            // Check for multi-line value indicator
            if (value.StartsWith(">-"))
            {
                isMultiLine = true;
                value = ""; // Value will be built from subsequent lines
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
            options = options ?? new CsfFileOptions();
            var csf = new CsfFile(options);
            
            // Default values if not specified in file
            csf.Version = 3;
            csf.Language = CsfLang.EnglishUS;

            using (var sr = new StreamReader(stream, Encoding.UTF8))
            {
                string line;
                string currentKey = null;
                StringBuilder currentValue = null;
                bool inMultiLine = false;

                while ((line = sr.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        if (inMultiLine && currentValue != null)
                        {
                            // Add empty line to multi-line value
                            currentValue.AppendLine();
                        }
                        continue;
                    }
                    
                    // Process metadata comments
                    if (line.StartsWith("#"))
                    {
                        // Extract version
                        var versionMatch = Regex.Match(line, @"# csf version:\s*(\d+)");
                        if (versionMatch.Success)
                        {
                            csf.Version = int.Parse(versionMatch.Groups[1].Value);
                            continue;
                        }
                        
                        // Extract language
                        var langMatch = Regex.Match(line, @"# language:\s*(\d+)");
                        if (langMatch.Success)
                        {
                            int langValue = int.Parse(langMatch.Groups[1].Value);
                            csf.Language = CsfLangHelper.GetCsfLang(langValue);
                        }
                        
                        // Skip other comments
                        continue;
                    }

                    // If we're in multi-line mode and the line is indented, it's a continuation
                    if (inMultiLine && currentValue != null)
                    {
                        if (line.StartsWith("  "))
                        {
                            // Continuation line with indentation
                            currentValue.AppendLine(line.Substring(2).TrimEnd());
                        }
                        else if (string.IsNullOrWhiteSpace(line))
                        {
                            // Empty line in multi-line value
                            currentValue.AppendLine();
                        }
                        else
                        {
                            // End of multi-line value - save current entry and start new one
                            string validCurrentKey = ConvertToValidLabelName(currentKey);
                            if (!string.IsNullOrEmpty(validCurrentKey))
                            {
                                csf.AddLabel(validCurrentKey, currentValue.ToString().Trim());
                            }
                            
                            // Parse the new line
                            inMultiLine = false;
                            currentKey = null;
                            currentValue = null;
                            
                            // Process the current line
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
                                    // Add the entry immediately for single-line values
                                    string validNewKey = ConvertToValidLabelName(currentKey);
                                    if (!string.IsNullOrEmpty(validNewKey))
                                    {
                                        csf.AddLabel(validNewKey, currentValue.ToString());
                                    }
                                    currentKey = null;
                                    currentValue = null;
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
                            csf.AddLabel(validCurrentKey, currentValue.ToString().Trim());
                        }
                        currentKey = null;
                        currentValue = null;
                        inMultiLine = false;
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
                            // Add the entry immediately for single-line values
                            string validCurrentKey = ConvertToValidLabelName(currentKey);
                            if (!string.IsNullOrEmpty(validCurrentKey))
                            {
                                csf.AddLabel(validCurrentKey, currentValue.ToString());
                            }
                            currentKey = null;
                            currentValue = null;
                        }
                    }
                }
                
                // Add last entry if exists
                if (currentKey != null && currentValue != null)
                {
                    string validCurrentKey = ConvertToValidLabelName(currentKey);
                    if (!string.IsNullOrEmpty(validCurrentKey))
                    {
                        csf.AddLabel(validCurrentKey, currentValue.ToString().Trim());
                    }
                }
            }

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
                // Write metadata header
                sw.WriteLine($"# {fileName}");
                sw.WriteLine($"# version: {csf.Version}");
                sw.WriteLine($"# language: {(int)csf.Language}");
                sw.WriteLine($"# csf count: {csf.Labels.Count}");
                sw.WriteLine($"# build time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n");

                // Write all labels using full key (including section if present)
                foreach (var label in csf.Labels)
                {
                    if (label.Value.Contains("\n"))
                    {
                        // Multi-line value with YAML-style indentation
                        sw.WriteLine($"{label.Key}: >-");
                        string[] lines = label.Value.Split('\n');
                        foreach (string line in lines)
                        {
                            sw.WriteLine($"  {line.TrimEnd()}");
                        }
                    }
                    else
                    {
                        // Single-line value
                        sw.WriteLine($"{label.Key}: {label.Value}");
                    }
                }
            }
        }
    }
}