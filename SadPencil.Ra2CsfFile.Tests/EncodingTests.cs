using System;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SadPencil.Ra2CsfFile.Tests
{
    [TestClass]
    public class EncodingTests
    {
        private static string TestDataDir => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData");
        private static string RussianCsf => Path.Combine(TestDataDir, "ra2md_ru.csf");

        #region CSV Encoding

        [TestMethod]
        public void Csv_Windows1251_CanBeRead()
        {
            CsfFile original;
            using (var fs = File.OpenRead(RussianCsf))
                original = CsfFile.LoadFromCsfFile(fs);

            var encoding = Encoding.GetEncoding(1251);
            byte[] csvBytes;
            using (var ms = new MemoryStream())
            {
                CsfFileCsvHelper.WriteCsvFile(original, ms, ",", encoding);
                csvBytes = ms.ToArray();
            }

            CsfFile loaded;
            using (var ms = new MemoryStream(csvBytes))
                loaded = CsfFileCsvHelper.LoadFromCsvFile(ms, ",", encoding);

            Assert.AreEqual(original.Labels.Count, loaded.Labels.Count,
                "Windows-1251 CSV should preserve label count");
        }

        [TestMethod]
        public void Csv_Utf8_WithBom_CanBeRead()
        {
            CsfFile original;
            using (var fs = File.OpenRead(RussianCsf))
                original = CsfFile.LoadFromCsfFile(fs);

            var encoding = new UTF8Encoding(true);
            byte[] csvBytes;
            using (var ms = new MemoryStream())
            {
                CsfFileCsvHelper.WriteCsvFile(original, ms, ",", encoding);
                csvBytes = ms.ToArray();
            }

            // Verify BOM exists
            Assert.AreEqual(0xEF, csvBytes[0], "First byte of UTF-8 BOM");
            Assert.AreEqual(0xBB, csvBytes[1], "Second byte of UTF-8 BOM");
            Assert.AreEqual(0xBF, csvBytes[2], "Third byte of UTF-8 BOM");

            CsfFile loaded;
            using (var ms = new MemoryStream(csvBytes))
                loaded = CsfFileCsvHelper.LoadFromCsvFile(ms, ",", new UTF8Encoding(true));

            Assert.AreEqual(original.Labels.Count, loaded.Labels.Count,
                "UTF-8 BOM CSV should preserve label count");
        }

        [TestMethod]
        public void Csv_RoundTrip_WithDifferentDelimiters()
        {
            string TestDataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData");
            string EnglishCsf = Path.Combine(TestDataDir, "ra2md.csf");

            CsfFile original;
            using (var fs = File.OpenRead(EnglishCsf))
                original = CsfFile.LoadFromCsfFile(fs);

            // Test with comma
            byte[] commaBytes;
            using (var ms = new MemoryStream())
            {
                CsfFileCsvHelper.WriteCsvFile(original, ms, ",");
                commaBytes = ms.ToArray();
            }
            CsfFile loadedComma;
            using (var ms = new MemoryStream(commaBytes))
                loadedComma = CsfFileCsvHelper.LoadFromCsvFile(ms, ",");
            Assert.AreEqual(original.Labels.Count, loadedComma.Labels.Count, "Comma delimiter should work");

            // Test with semicolon
            byte[] semicolonBytes;
            using (var ms = new MemoryStream())
            {
                CsfFileCsvHelper.WriteCsvFile(original, ms, ";");
                semicolonBytes = ms.ToArray();
            }
            CsfFile loadedSemicolon;
            using (var ms = new MemoryStream(semicolonBytes))
                loadedSemicolon = CsfFileCsvHelper.LoadFromCsvFile(ms, ";");
            Assert.AreEqual(original.Labels.Count, loadedSemicolon.Labels.Count, "Semicolon delimiter should work");

            // Test with tab
            byte[] tabBytes;
            using (var ms = new MemoryStream())
            {
                CsfFileCsvHelper.WriteCsvFile(original, ms, "\t");
                tabBytes = ms.ToArray();
            }
            CsfFile loadedTab;
            using (var ms = new MemoryStream(tabBytes))
                loadedTab = CsfFileCsvHelper.LoadFromCsvFile(ms, "\t");
            Assert.AreEqual(original.Labels.Count, loadedTab.Labels.Count, "Tab delimiter should work");
        }

        #endregion

        #region INI Encoding

        [TestMethod]
        public void Ini_RoundTrip_WithCyrillic()
        {
            CsfFile original;
            using (var fs = File.OpenRead(RussianCsf))
                original = CsfFile.LoadFromCsfFile(fs);

            byte[] iniBytes;
            using (var ms = new MemoryStream())
            {
                CsfFileIniHelper.WriteIniFile(original, ms);
                iniBytes = ms.ToArray();
            }

            CsfFile loaded;
            using (var ms = new MemoryStream(iniBytes))
                loaded = CsfFileIniHelper.LoadFromIniFile(ms);

            Assert.AreEqual(original.Labels.Count, loaded.Labels.Count,
                "INI round-trip with Cyrillic should preserve label count");
        }

        #endregion

        #region JSON Encoding

        [TestMethod]
        public void Json_RoundTrip_WithCyrillic()
        {
            CsfFile original;
            using (var fs = File.OpenRead(RussianCsf))
                original = CsfFile.LoadFromCsfFile(fs);

            byte[] jsonBytes;
            using (var ms = new MemoryStream())
            {
                CsfFileJsonHelper.WriteJsonFile(original, ms);
                jsonBytes = ms.ToArray();
            }

            CsfFile loaded;
            using (var ms = new MemoryStream(jsonBytes))
                loaded = CsfFileJsonHelper.LoadFromJsonFile(ms);

            Assert.AreEqual(original.Labels.Count, loaded.Labels.Count,
                "JSON round-trip with Cyrillic should preserve label count");
        }

        #endregion

        #region YAML Encoding

        [TestMethod]
        public void Yaml_RoundTrip_WithCyrillic()
        {
            CsfFile original;
            using (var fs = File.OpenRead(RussianCsf))
                original = CsfFile.LoadFromCsfFile(fs);

            byte[] yamlBytes;
            using (var ms = new MemoryStream())
            {
                CsfFileYamlHelper.WriteYamlFile(original, ms);
                yamlBytes = ms.ToArray();
            }

            CsfFile loaded;
            using (var ms = new MemoryStream(yamlBytes))
                loaded = CsfFileYamlHelper.LoadFromYamlFile(ms);

            Assert.AreEqual(original.Labels.Count, loaded.Labels.Count,
                "YAML round-trip with Cyrillic should preserve label count");
        }

        #endregion

        #region TXT Encoding

        [TestMethod]
        public void Txt_RoundTrip_WithCyrillic()
        {
            CsfFile original;
            using (var fs = File.OpenRead(RussianCsf))
                original = CsfFile.LoadFromCsfFile(fs);

            byte[] txtBytes;
            using (var ms = new MemoryStream())
            {
                CsfFileTxtHelper.WriteTxtFile(original, ms);
                txtBytes = ms.ToArray();
            }

            CsfFile loaded;
            using (var ms = new MemoryStream(txtBytes))
                loaded = CsfFileTxtHelper.LoadFromTxtFile(ms);

            Assert.AreEqual(original.Labels.Count, loaded.Labels.Count,
                "TXT round-trip with Cyrillic should preserve label count");
        }

        #endregion

        #region All Formats Full Round-Trip

        [TestMethod]
        public void FullRoundTrip_Csf_Ini_Json_Yaml_Csv_Txt_BackToCsf()
        {
            string TestDataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData");
            string EnglishCsf = Path.Combine(TestDataDir, "ra2md.csf");

            CsfFile original;
            using (var fs = File.OpenRead(EnglishCsf))
                original = CsfFile.LoadFromCsfFile(fs);

            // CSF -> INI
            byte[] iniBytes;
            using (var ms = new MemoryStream())
            { CsfFileIniHelper.WriteIniFile(original, ms); iniBytes = ms.ToArray(); }

            // INI -> JSON
            CsfFile fromIni;
            using (var ms = new MemoryStream(iniBytes))
                fromIni = CsfFileIniHelper.LoadFromIniFile(ms);
            byte[] jsonBytes;
            using (var ms = new MemoryStream())
            { CsfFileJsonHelper.WriteJsonFile(fromIni, ms); jsonBytes = ms.ToArray(); }

            // JSON -> YAML
            CsfFile fromJson;
            using (var ms = new MemoryStream(jsonBytes))
                fromJson = CsfFileJsonHelper.LoadFromJsonFile(ms);
            byte[] yamlBytes;
            using (var ms = new MemoryStream())
            { CsfFileYamlHelper.WriteYamlFile(fromJson, ms); yamlBytes = ms.ToArray(); }

            // YAML -> CSV
            CsfFile fromYaml;
            using (var ms = new MemoryStream(yamlBytes))
                fromYaml = CsfFileYamlHelper.LoadFromYamlFile(ms);
            byte[] csvBytes;
            using (var ms = new MemoryStream())
            { CsfFileCsvHelper.WriteCsvFile(fromYaml, ms); csvBytes = ms.ToArray(); }

            // CSV -> TXT
            CsfFile fromCsv;
            using (var ms = new MemoryStream(csvBytes))
                fromCsv = CsfFileCsvHelper.LoadFromCsvFile(ms);
            byte[] txtBytes;
            using (var ms = new MemoryStream())
            { CsfFileTxtHelper.WriteTxtFile(fromCsv, ms); txtBytes = ms.ToArray(); }

            // TXT -> CSF
            CsfFile fromTxt;
            using (var ms = new MemoryStream(txtBytes))
                fromTxt = CsfFileTxtHelper.LoadFromTxtFile(ms);

            Assert.AreEqual(original.Labels.Count, fromTxt.Labels.Count,
                "Full round-trip should preserve label count");
        }

        #endregion
    }
}
