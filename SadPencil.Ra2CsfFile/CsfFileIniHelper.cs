using IniParser.Model;
using IniParser.Model.Configuration;
using IniParser.Parser;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace SadPencil.Ra2CsfFile
{
    /// <summary>
    /// Вспомогательный класс для работы с INI-файлами, представляющими таблицы строк CSF.
    /// </summary>
    public static class CsfFileIniHelper
    {
        private const String INI_TYPE_NAME = "SadPencil.Ra2CsfFile.Ini";
        private const String INI_FILE_HEADER_SECTION_NAME = "SadPencil.Ra2CsfFile.Ini";
        private const String INI_FILE_HEADER_INI_VERSION_KEY = "IniVersion";
        private const String INI_FILE_HEADER_CSF_VERSION_KEY = "CsfVersion";
        private const String INI_FILE_HEADER_CSF_LANGUAGE_KEY = "CsfLang";

        private const Int32 INI_VERSION = 2;
        private static IniParserConfiguration IniParserConfiguration { get; } = new IniParserConfiguration()
        {
            AllowDuplicateKeys = false,
            AllowDuplicateSections = false,
            AllowKeysWithoutSection = false,
            CommentRegex = new Regex("a^"), // ничего не совпадает
            CaseInsensitive = true,
            AssigmentSpacer = String.Empty,
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
        /// Загружает CSF-файл из INI-представления.
        /// </summary>
        /// <param name="stream">Поток с INI-файлом.</param>
        /// <returns>Загруженный CSF-файл.</returns>
        /// <exception cref="ArgumentNullException">Если поток равен null.</exception>
        /// <exception cref="InvalidDataException">Если формат файла неверный.</exception>
        public static CsfFile LoadFromIniFile(Stream stream) => LoadFromIniFile(stream, new CsfFileOptions());

        /// <summary>
        /// Загружает CSF-файл из INI-представления с указанными опциями.
        /// </summary>
        /// <param name="stream">Поток с INI-файлом.</param>
        /// <param name="options">Опции загрузки.</param>
        /// <returns>Загруженный CSF-файл.</returns>
        /// <exception cref="ArgumentNullException">Если поток или опции равны null.</exception>
        /// <exception cref="InvalidDataException">Если формат файла неверный.</exception>
        public static CsfFile LoadFromIniFile(Stream stream, CsfFileOptions options)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (options == null) throw new ArgumentNullException(nameof(options));

            var csf = new CsfFile(options);
            var ini = ParseIni(stream);

            if (!ini.Sections.ContainsSection(INI_FILE_HEADER_SECTION_NAME))
                throw new InvalidDataException($"Invalid {INI_TYPE_NAME} file. Missing section [{INI_FILE_HEADER_SECTION_NAME}].");

            var header = ini.Sections[INI_FILE_HEADER_SECTION_NAME];

            // Проверка версии
            if (!header.ContainsKey(INI_FILE_HEADER_INI_VERSION_KEY))
                throw new InvalidDataException($"Invalid {INI_TYPE_NAME} file. Missing key \"{INI_FILE_HEADER_INI_VERSION_KEY}\".");

            if (Convert.ToInt32(header[INI_FILE_HEADER_INI_VERSION_KEY], CultureInfo.InvariantCulture) != INI_VERSION)
                throw new InvalidDataException($"Unknown {INI_TYPE_NAME} file version. Expected {INI_VERSION}.");

            // Загрузка метаданных
            csf.Version = Convert.ToInt32(header[INI_FILE_HEADER_CSF_VERSION_KEY], CultureInfo.InvariantCulture);
            csf.Language = CsfLangHelper.GetCsfLang(Convert.ToInt32(header[INI_FILE_HEADER_CSF_LANGUAGE_KEY], CultureInfo.InvariantCulture));

            // Загрузка меток
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
                    labelName = CsfFile.LowercaseLabelName(labelName);
                    csf.AddLabel(labelName, labelValue);
                }
            }

            return csf;
        }

        private static string GetIniLabelValueKeyName(int lineIndex) => 
            "Value" + (lineIndex == 1 ? string.Empty : $"Line{lineIndex.ToString(CultureInfo.InvariantCulture)}");

        /// <summary>
        /// Сохраняет CSF-файл в INI-представление.
        /// </summary>
        /// <param name="csf">CSF-файл для сохранения.</param>
        /// <param name="stream">Поток для записи.</param>
        /// <exception cref="ArgumentNullException">Если файл или поток равны null.</exception>
        /// <exception cref="InvalidDataException">Если обнаружены недопустимые символы в именах меток.</exception>
        public static void WriteIniFile(CsfFile csf, Stream stream)
        {
            if (csf == null) throw new ArgumentNullException(nameof(csf));
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            var ini = GetIniData();

            // Запись заголовка
            ini.Sections.AddSection(INI_FILE_HEADER_SECTION_NAME);
            var header = ini.Sections[INI_FILE_HEADER_SECTION_NAME];
            header.AddKey(INI_FILE_HEADER_INI_VERSION_KEY, INI_VERSION.ToString(CultureInfo.InvariantCulture));
            header.AddKey(INI_FILE_HEADER_CSF_VERSION_KEY, csf.Version.ToString(CultureInfo.InvariantCulture));
            header.AddKey(INI_FILE_HEADER_CSF_LANGUAGE_KEY, ((int)csf.Language).ToString(CultureInfo.InvariantCulture));

            // Запись меток
            foreach (var label in csf.Labels)
            {
                string labelName = label.Key;
                string labelValue = label.Value;

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
            }

            using (var sw = new StreamWriter(stream, new UTF8Encoding(false)))
            {
                sw.Write(ini.ToString());
            }
        }
    }
}