using System;
using System.IO;
using System.Linq;
using System.Text;

namespace SadPencil.Ra2CsfFile
{
    /// <summary>
    /// Helper class for working with TXT files in CSFTool format representing CSF string tables.
    /// </summary>
    public static class CsfFileTxtHelper
    {
        private const string LabelSeparator = "|";
        private const string NewLineString = @"\n";

        /// <summary>
        /// Loads a CSF file from a TXT representation in CSFTool format.
        /// </summary>
        public static CsfFile LoadFromTxtFile(Stream stream, CsfFileOptions options = null)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            options = options ?? new CsfFileOptions();
            var csf = new CsfFile(options);

            try
            {
                using (var sr = new StreamReader(stream, Encoding.UTF8, true, 1024))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        ProcessLine(csf, line);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidDataException("Error reading TXT file.", ex);
            }

            return csf;
        }

        private static void ProcessLine(CsfFile csf, string line)
        {
            if (string.IsNullOrEmpty(line))
                return;

            // Skip comments and empty lines
            if (line.StartsWith(";") || string.IsNullOrWhiteSpace(line))
                return;

            // Skip lines starting with * followed by separator (like CSFTool does)
            if (line.IndexOf('*') == 0 && line.Length > 1 && line[1] == '|')
                return;

            int idx = line.IndexOf(LabelSeparator);
            if (idx < 1)
                return;

            string label = line.Substring(0, idx);
            string value = line.Substring(idx + LabelSeparator.Length);

            // Replace newline escape sequences with actual newlines
            value = value.Replace(NewLineString, "\n");

            // Convert label to lowercase for consistency with Ra2CsfFile
            label = CsfFile.LowercaseLabelName(label);

            // Add or replace the label in CSF
            csf.AddLabel(label, value);
        }

        /// <summary>
        /// Saves a CSF file to a TXT representation in CSFTool format.
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
                    // Sort labels by name for consistent output (like CSFTool does)
                    var sortedLabels = csf.Labels.OrderBy(pair => pair.Key).ToList();

                    foreach (var pair in sortedLabels)
                    {
                        string label = pair.Key.ToUpperInvariant(); // CSFTool uses uppercase
                        string value = pair.Value;

                        // Replace newlines with escape sequences
                        value = value.Replace("\n", NewLineString);
                        value = value.Replace("\r\n", NewLineString); // Handle Windows line endings

                        sw.WriteLine($"{label}{LabelSeparator}{value}");
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