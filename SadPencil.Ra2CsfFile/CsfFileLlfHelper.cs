using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace SadPencil.Ra2CsfFile
{
    /// <summary>
    /// Helper class for working with LLF files representing CSF string tables.
    /// </summary>
    public static class CsfFileLlfHelper
    {
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
                        continue;
                    
                    // Process metadata comments
                    if (line.StartsWith("#"))
                    {
                        // Extract version
                        var versionMatch = Regex.Match(line, @"#Version:\s*(\d+)");
                        if (versionMatch.Success)
                        {
                            csf.Version = int.Parse(versionMatch.Groups[1].Value);
                            continue;
                        }
                        
                        // Extract language
                        var langMatch = Regex.Match(line, @"#Language:\s*(\d+)");
                        if (langMatch.Success)
                        {
                            int langValue = int.Parse(langMatch.Groups[1].Value);
                            csf.Language = CsfLangHelper.GetCsfLang(langValue);
                            continue;
                        }
                        
                        // Skip other comments
                        continue;
                    }
                    
                    // Parse key-value pairs
                    int colonIndex = line.IndexOf(':');
                    if (colonIndex >= 0)
                    {
                        // Finalize previous entry if exists
                        if (currentKey != null)
                        {
                            csf.AddLabel(currentKey, currentValue.ToString());
                        }

                        currentKey = line.Substring(0, colonIndex).Trim();
                        string valuePart = line.Substring(colonIndex + 1).Trim();

                        // Check for multi-line value indicator
                        if (valuePart.StartsWith(">-"))
                        {
                            inMultiLine = true;
                            currentValue = new StringBuilder();
                        }
                        else
                        {
                            // Single-line value
                            csf.AddLabel(currentKey, valuePart);
                            currentKey = null;
                        }
                    }
                    else if (inMultiLine && line.StartsWith("  "))
                    {
                        // Continuation of multi-line value
                        currentValue.AppendLine(line.Substring(2).TrimEnd());
                    }
                    else if (inMultiLine)
                    {
                        // End of multi-line value
                        csf.AddLabel(currentKey, currentValue.ToString());
                        currentKey = null;
                        inMultiLine = false;
                    }
                }
                
                // Add last entry if exists
                if (currentKey != null)
                {
                    csf.AddLabel(currentKey, currentValue.ToString());
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
                sw.WriteLine($"#Version: {csf.Version}");
                sw.WriteLine($"#Language: {(int)csf.Language}");
                sw.WriteLine($"#csf count: {csf.Labels.Count}");
                sw.WriteLine($"#build time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n");

                // Write labels
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