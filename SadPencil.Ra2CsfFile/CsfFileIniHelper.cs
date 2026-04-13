using IniParser.Model;
using IniParser.Model.Configuration;
using IniParser.Parser;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SadPencil.Ra2CsfFile
{
    /// <summary>
    /// Helper class for working with INI files representing CSF string tables.
    /// Supports extra data (WRTS) as a Base64-encoded string or plain text.
    /// Supports label ordering via CsfFileOptions.OrderByKey.
    /// </summary>
    public static class CsfFileIniHelper
    {
        private const string INI_TYPE_NAME = "SadPencil.Ra2CsfFile.Ini";
        private const string INI_FILE_HEADER_SECTION_NAME = "SadPencil.Ra2CsfFile.Ini";
        private const string INI_FILE_HEADER_INI_VERSION_KEY = "IniVersion";
        private const string INI_FILE_HEADER_CSF_VERSION_KEY = "CsfVersion";
        private const string INI_FILE_HEADER_CSF_LANGUAGE_KEY = "CsfLang";

        private const int INI_VERSION = 3; // Increased version due to extra support

        private static IniParserConfiguration IniParserConfiguration { get; } = new IniParserConfiguration()
        {
            AllowDuplicateKeys = false,
            AllowDuplicateSections = false,
            AllowKeysWithoutSection = false,
            CommentRegex = new Regex("a^"), // nothing matches
            CaseInsensitive = true,
            AssigmentSpacer = string.Empty,
            SectionRegex = new Regex("^(\\s*?)\\[{1}\\s*[\\p{L}\\p{P}\\p{M}_\\\"\\'\\{\\}\\#\\+\\;\\*\\%\\(\\)\\=\\?\\&\\$\\^\\<\\>\\`\\^|\\,\\:\\/\\.\\-\\w\\d\\s\\\\\\~]+\\s*\\](\\s*?)$"),
        };

        private static IniData GetIniData() => new IniData() { Configuration = IniParserConfiguration };

        private static IniDataParser GetIniDataParser() => new IniDataParser(IniParserConfiguration);

        private static IniData ParseIni(Stream stream)
        {
            var parser = GetIniDataParser();

            using (var sr = new StreamReader(stream, new UTF8Encoding(false)))
            {
                return parser.Parse(sr.ReadToEnd());
            }
        }

        /// <summary>
        /// Loads a CSF file from an INI view.
        /// </summary>
        /// <param name="stream">Stream with INI file.</param>
        /// <returns>Uploaded CSF file.</returns>
        /// <exception cref="ArgumentNullException">If the stream is null.</exception>
        /// <exception cref="InvalidDataException">If the file format is incorrect.</exception>
        public static CsfFile LoadFromIniFile(Stream stream) => LoadFromIniFile(stream, new CsfFileOptions());

        /// <summary>
        /// Loads a CSF file from an INI view with the specified options.
        /// </summary>
        /// <param name="stream">Stream with INI file.</param>
        /// <param name="options">Download options.</param>
        /// <returns>Uploaded CSF file.</returns>
        /// <exception cref="ArgumentNullException">If the stream is null.</exception>
        /// <exception cref="InvalidDataException">If the file format is incorrect.</exception>
        public static CsfFile LoadFromIniFile(Stream stream, CsfFileOptions options)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (options == null) throw new ArgumentNullException(nameof(options));

            var csf = new CsfFile(options);
            var ini = ParseIni(stream);

            if (!ini.Sections.ContainsSection(INI_FILE_HEADER_SECTION_NAME))
                throw new InvalidDataException($"Invalid {INI_TYPE_NAME} file. Missing section [{INI_FILE_HEADER_SECTION_NAME}].");

            var header = ini.Sections[INI_FILE_HEADER_SECTION_NAME];

            // Checking the version
            if (!header.ContainsKey(INI_FILE_HEADER_INI_VERSION_KEY))
                throw new InvalidDataException($"Invalid {INI_TYPE_NAME} file. Missing key \"{INI_FILE_HEADER_INI_VERSION_KEY}\".");

            if (Convert.ToInt32(header[INI_FILE_HEADER_INI_VERSION_KEY], CultureInfo.InvariantCulture) != INI_VERSION)
                throw new InvalidDataException($"Unknown {INI_TYPE_NAME} file version. Expected {INI_VERSION}.");

            // Loading metadata
            csf.Version = Convert.ToInt32(header[INI_FILE_HEADER_CSF_VERSION_KEY], CultureInfo.InvariantCulture);
            csf.Language = CsfLangHelper.GetCsfLang(Convert.ToInt32(header[INI_FILE_HEADER_CSF_LANGUAGE_KEY], CultureInfo.InvariantCulture));

            // Loading labels
            foreach (var section in ini.Sections)
            {
                if (section.SectionName == INI_FILE_HEADER_SECTION_NAME) continue;

                string labelName = section.SectionName;
                var key = section.Keys;

                if (!CsfFile.ValidateLabelName(labelName))
                    throw new InvalidDataException($"Invalid characters in label name \"{labelName}\".");

                var valueParts = new List<string>();
                for (int iLine = 1; ; iLine++)
                {
                    string keyName = GetIniLabelValueKeyName(iLine);
                    if (!key.ContainsKey(keyName)) break;
                    valueParts.Add(key[keyName]);
                }

                if (valueParts.Count > 0)
                {
                    string labelValue = string.Join(CsfFile.LineBreakCharacters, valueParts);
                    
                    // Read extra data if present
                    byte[] extra = null;
                    if (key.ContainsKey("Extra"))
                    {
                        string extraStr = key["Extra"];
                        if (!string.IsNullOrEmpty(extraStr))
                        {
                            if (options.TreatExtraAsText)
                                extra = Encoding.UTF8.GetBytes(extraStr);
                            else
                                extra = Convert.FromBase64String(extraStr);
                        }
                    }
                    
                    csf.AddLabel(labelName, labelValue, extra);
                }
            }

            if (options.OrderByKey)
                csf = csf.OrderByKey();

            return csf;
        }

        private static string GetIniLabelValueKeyName(int lineIndex) => 
            "Value" + (lineIndex == 1 ? string.Empty : $"Line{lineIndex.ToString(CultureInfo.InvariantCulture)}");

        /// <summary>
        /// Saves a CSF file to an INI representation.
        /// </summary>
        /// <param name="csf">CSF file to save.</param>
        /// <param name="stream">Stream for recording.</param>
        /// <exception cref="ArgumentNullException">If the file or stream is null.</exception>
        /// <exception cref="InvalidDataException">If invalid characters are found in label names.</exception>
        public static void WriteIniFile(CsfFile csf, Stream stream)
        {
            if (csf == null) throw new ArgumentNullException(nameof(csf));
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            var ini = GetIniData();

            // Header record
            ini.Sections.AddSection(INI_FILE_HEADER_SECTION_NAME);
            var header = ini.Sections[INI_FILE_HEADER_SECTION_NAME];
            header.AddKey(INI_FILE_HEADER_INI_VERSION_KEY, INI_VERSION.ToString(CultureInfo.InvariantCulture));
            header.AddKey(INI_FILE_HEADER_CSF_VERSION_KEY, csf.Version.ToString(CultureInfo.InvariantCulture));
            header.AddKey(INI_FILE_HEADER_CSF_LANGUAGE_KEY, ((int)csf.Language).ToString(CultureInfo.InvariantCulture));

            // Write labels in determined order
            foreach (string labelName in csf.GetLabelsInWriteOrder())
            {
                if (!csf.Labels.TryGetValue(labelName, out string labelValue))
                    continue;

                if (!CsfFile.ValidateLabelName(labelName))
                    throw new InvalidDataException($"Invalid characters in label name \"{labelName}\".");

                ini.Sections.AddSection(labelName);
                var labelSection = ini.Sections[labelName];
                string[] valueLines = labelValue.Split(CsfFile.LineBreakCharacters.ToCharArray());

                for (int i = 0; i < valueLines.Length; i++)
                {
                    string keyName = GetIniLabelValueKeyName(i + 1);
                    labelSection.AddKey(keyName, valueLines[i]);
                }

                // Write extra data if present
                byte[] extra = csf.GetExtra(labelName);
                if (extra != null)
                {
                    string extraStr;
                    if (csf.Options.TreatExtraAsText)
                        extraStr = Encoding.UTF8.GetString(extra);
                    else
                        extraStr = Convert.ToBase64String(extra);
                    labelSection.AddKey("Extra", extraStr);
                }

                // Add trimmable line warning comment if needed
                if (labelValue != TrimMultiLine(labelValue))
                {
                    labelSection.AddKey("WhiteSpaceWarning", "WARNING: This label value contains leading or trailing whitespace characters in one or more lines.");
                }
            }

            using (var sw = new StreamWriter(stream, new UTF8Encoding(false)))
            {
                sw.Write(ini.ToString());
            }
        }

        /// <summary>
        /// Trim each line's leading and trailing whitespace characters in a multi-line string.
        /// </summary>
        /// <param name="input">The input multi-line string.</param>
        /// <returns>The trimmed multi-line string.</returns>
        public static string TrimMultiLine(string input) => 
            input is null ? null : string.Join(CsfFile.LineBreakCharacters, input.Split(new[] { CsfFile.LineBreakCharacters }, StringSplitOptions.None).Select(l => l.Trim()));
    }
}