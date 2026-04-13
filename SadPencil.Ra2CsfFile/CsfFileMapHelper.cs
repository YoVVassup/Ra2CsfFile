using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using IniParser.Model;
using IniParser.Model.Configuration;
using IniParser.Parser;

namespace SadPencil.Ra2CsfFile
{
    /// <summary>
    /// Helper class for extracting label names from RA2/YR map files (.map, .mpr, .yrm).
    /// Uses IniParser for robust parsing with permissive configuration.
    /// </summary>
    public static class CsfFileMapHelper
    {
        private static IniParserConfiguration GetMapIniParserConfiguration()
        {
            return new IniParserConfiguration
            {
                AllowDuplicateKeys = true,
                AllowDuplicateSections = true,
                AllowKeysWithoutSection = true,
                CaseInsensitive = true,
                AssigmentSpacer = string.Empty,
                CommentRegex = new System.Text.RegularExpressions.Regex(";.*"),
                SectionRegex = new System.Text.RegularExpressions.Regex(@"^\[([^]]+)\]")
            };
        }

        private static IniDataParser GetMapIniDataParser() => new IniDataParser(GetMapIniParserConfiguration());

        private static IniData ParseMapIni(Stream stream)
        {
            var parser = GetMapIniDataParser();
            using (var sr = new StreamReader(stream, Encoding.ASCII))
            {
                return parser.Parse(sr.ReadToEnd());
            }
        }

        /// <summary>
        /// Scans a folder recursively for map files and extracts all label names used in them.
        /// </summary>
        /// <param name="mapFolder">Path to folder containing map files.</param>
        /// <returns>A hash set of label names found in the maps.</returns>
        /// <exception cref="DirectoryNotFoundException">Thrown if mapFolder does not exist.</exception>
        /// <exception cref="IOException">Thrown if a map file cannot be read.</exception>
        public static HashSet<string> ExtractLabelsFromMapFolder(string mapFolder)
        {
            if (!Directory.Exists(mapFolder))
                throw new DirectoryNotFoundException($"Map folder not found: {mapFolder}");

            var mapFiles = Directory.EnumerateFiles(mapFolder, "*.*", SearchOption.AllDirectories)
                .Where(f => f.EndsWith(".map", StringComparison.InvariantCultureIgnoreCase) ||
                            f.EndsWith(".mpr", StringComparison.InvariantCultureIgnoreCase) ||
                            f.EndsWith(".yrm", StringComparison.InvariantCultureIgnoreCase))
                .ToList();

            var allLabels = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
            foreach (var mapFile in mapFiles)
            {
                try
                {
                    using (var fs = File.OpenRead(mapFile))
                    {
                        var ini = ParseMapIni(fs);
                        ExtractLabelsFromIni(ini, allLabels);
                    }
                }
                catch (Exception ex)
                {
                    throw new IOException($"Failed to parse map file {mapFile}: {ex.Message}", ex);
                }
            }
            return allLabels;
        }

        private static void ExtractLabelsFromIni(IniData ini, HashSet<string> labels)
        {
            // 1. Scan all sections for UIName keys
            foreach (var section in ini.Sections)
            {
                if (section.Keys.ContainsKey("UIName"))
                {
                    string value = section.Keys["UIName"];
                    if (!string.IsNullOrEmpty(value) && CsfFile.ValidateLabelName(value))
                        labels.Add(value);
                }
            }

            // 2. Scan [Actions] section for action type 11/103 with parameter 4
            if (ini.Sections.ContainsSection("Actions"))
            {
                var actionsSection = ini.Sections["Actions"];
                foreach (var key in actionsSection)
                {
                    string[] parts = key.Value.Split(',');
                    if (parts.Length >= 3 && int.TryParse(parts[0], out int actionCount) && actionCount > 0)
                    {
                        for (int idx = 1; idx < actionCount * 8 && idx + 2 < parts.Length; idx += 8)
                        {
                            if (!int.TryParse(parts[idx], out int actionType)) continue;
                            if (actionType != 11 && actionType != 103) continue;
                            if (!int.TryParse(parts[idx + 1], out int param1)) continue;
                            if (param1 != 4) continue;
                            string label = parts[idx + 2];
                            if (!string.IsNullOrEmpty(label) && CsfFile.ValidateLabelName(label))
                                labels.Add(label);
                        }
                    }
                }
            }

            // 3. Scan [Ranking] section for specific keys
            if (ini.Sections.ContainsSection("Ranking"))
            {
                var rankingSection = ini.Sections["Ranking"];
                string[] rankingKeys = { "OverParTitle", "UnderParTitle", "OverParMessage", "UnderParMessage" };
                foreach (var keyName in rankingKeys)
                {
                    if (rankingSection.ContainsKey(keyName))
                    {
                        string value = rankingSection[keyName];
                        if (!string.IsNullOrEmpty(value) && CsfFile.ValidateLabelName(value))
                            labels.Add(value);
                    }
                }
            }
        }

        /// <summary>
        /// Checks which labels from map files are missing in a given CSF file.
        /// </summary>
        /// <param name="csf">The CSF file to check against.</param>
        /// <param name="mapFolder">Folder containing map files.</param>
        /// <returns>Sorted list of missing label names.</returns>
        public static List<string> FindMissingLabels(CsfFile csf, string mapFolder)
        {
            if (csf == null) throw new ArgumentNullException(nameof(csf));
            var mapLabels = ExtractLabelsFromMapFolder(mapFolder);
            var missing = mapLabels.Where(l => !csf.Labels.ContainsKey(l)).ToList();
            missing.Sort(StringComparer.InvariantCultureIgnoreCase);
            return missing;
        }
    }
}