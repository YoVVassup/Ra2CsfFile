using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SadPencil.Ra2CsfFile.Tests
{
    [TestClass]
    public class FormatConversionTests
    {
        private static string TestDataDir => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData");
        private static string EnglishCsf => Path.Combine(TestDataDir, "ra2md.csf");
        private static string TempDir => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Temp");

        [ClassInitialize]
        public static void Setup(TestContext context)
        {
            if (!Directory.Exists(TempDir))
                Directory.CreateDirectory(TempDir);
        }

        [ClassCleanup]
        public static void Cleanup()
        {
            if (Directory.Exists(TempDir))
                Directory.Delete(TempDir, true);
        }

        [TestMethod]
        public void ConvertCsfToIni_ProducesValidOutput()
        {
            CsfFile csf;
            using (var fs = File.OpenRead(EnglishCsf))
                csf = CsfFile.LoadFromCsfFile(fs);

            string iniPath = Path.Combine(TempDir, "test.ini");
            using (var fs = File.Create(iniPath))
                CsfFileIniHelper.WriteIniFile(csf, fs);

            string content = File.ReadAllText(iniPath);
            Assert.IsTrue(content.Contains("[SadPencil.Ra2CsfFile.Ini]"), "Should contain INI header");
            Assert.IsTrue(content.Contains("IniVersion=3"), "Should contain INI version");
        }

        [TestMethod]
        public void ConvertCsfToJson_ProducesValidOutput()
        {
            CsfFile csf;
            using (var fs = File.OpenRead(EnglishCsf))
                csf = CsfFile.LoadFromCsfFile(fs);

            string jsonPath = Path.Combine(TempDir, "test.json");
            using (var fs = File.Create(jsonPath))
                CsfFileJsonHelper.WriteJsonFile(csf, fs);

            string content = File.ReadAllText(jsonPath);
            Assert.IsTrue(content.Contains("\"labels\""), "Should contain labels key");
            Assert.IsTrue(content.Contains("\"version\""), "Should contain version key");
        }

        [TestMethod]
        public void ConvertCsfToYaml_ProducesValidOutput()
        {
            CsfFile csf;
            using (var fs = File.OpenRead(EnglishCsf))
                csf = CsfFile.LoadFromCsfFile(fs);

            string yamlPath = Path.Combine(TempDir, "test.yaml");
            using (var fs = File.Create(yamlPath))
                CsfFileYamlHelper.WriteYamlFile(csf, fs);

            string content = File.ReadAllText(yamlPath);
            Assert.IsTrue(content.Contains("labels:"), "Should contain labels key");
            Assert.IsTrue(content.Contains("version_yaml:"), "Should contain YAML version");
        }

        [TestMethod]
        public void ConvertCsfToTxt_ProducesValidOutput()
        {
            CsfFile csf;
            using (var fs = File.OpenRead(EnglishCsf))
                csf = CsfFile.LoadFromCsfFile(fs);

            string txtPath = Path.Combine(TempDir, "test.txt");
            using (var fs = File.Create(txtPath))
                CsfFileTxtHelper.WriteTxtFile(csf, fs);

            string content = File.ReadAllText(txtPath);
            Assert.IsTrue(content.Contains("!metadata|version|3"), "Should contain version metadata");
            Assert.IsTrue(content.Contains("!metadata|language|"), "Should contain language metadata");
        }

        [TestMethod]
        public void ConvertCsfToCsv_ProducesValidOutput()
        {
            CsfFile csf;
            using (var fs = File.OpenRead(EnglishCsf))
                csf = CsfFile.LoadFromCsfFile(fs);

            string csvPath = Path.Combine(TempDir, "test.csv");
            using (var fs = File.Create(csvPath))
                CsfFileCsvHelper.WriteCsvFile(csf, fs);

            string content = File.ReadAllText(csvPath);
            Assert.IsTrue(content.Contains("sep=,"), "Should contain separator");
            Assert.IsTrue(content.Contains("#version=3"), "Should contain version metadata");
            Assert.IsTrue(content.Contains("Label Name,Value,Extra"), "Should contain header");
        }

        [TestMethod]
        public void CrossFormat_ConvertIniToJson_PreservesLabels()
        {
            CsfFile original;
            using (var fs = File.OpenRead(EnglishCsf))
                original = CsfFile.LoadFromCsfFile(fs);

            string iniPath = Path.Combine(TempDir, "cross.ini");
            using (var fs = File.Create(iniPath))
                CsfFileIniHelper.WriteIniFile(original, fs);

            CsfFile fromIni;
            using (var fs = File.OpenRead(iniPath))
                fromIni = CsfFileIniHelper.LoadFromIniFile(fs);

            string jsonPath = Path.Combine(TempDir, "cross.json");
            using (var fs = File.Create(jsonPath))
                CsfFileJsonHelper.WriteJsonFile(fromIni, fs);

            CsfFile fromJson;
            using (var fs = File.OpenRead(jsonPath))
                fromJson = CsfFileJsonHelper.LoadFromJsonFile(fs);

            Assert.AreEqual(original.Labels.Count, fromJson.Labels.Count,
                "Cross-format conversion should preserve label count");
        }

        [TestMethod]
        public void CrossFormat_ConvertCsvToYaml_PreservesLabels()
        {
            CsfFile original;
            using (var fs = File.OpenRead(EnglishCsf))
                original = CsfFile.LoadFromCsfFile(fs);

            string csvPath = Path.Combine(TempDir, "cross.csv");
            using (var fs = File.Create(csvPath))
                CsfFileCsvHelper.WriteCsvFile(original, fs);

            CsfFile fromCsv;
            using (var fs = File.OpenRead(csvPath))
                fromCsv = CsfFileCsvHelper.LoadFromCsvFile(fs);

            string yamlPath = Path.Combine(TempDir, "cross.yaml");
            using (var fs = File.Create(yamlPath))
                CsfFileYamlHelper.WriteYamlFile(fromCsv, fs);

            CsfFile fromYaml;
            using (var fs = File.OpenRead(yamlPath))
                fromYaml = CsfFileYamlHelper.LoadFromYamlFile(fs);

            Assert.AreEqual(original.Labels.Count, fromYaml.Labels.Count,
                "Cross-format conversion should preserve label count");
        }

        [TestMethod]
        public void CsvDelimiter_Semicolon_Works()
        {
            CsfFile original;
            using (var fs = File.OpenRead(EnglishCsf))
                original = CsfFile.LoadFromCsfFile(fs);

            string csvPath = Path.Combine(TempDir, "semicolon.csv");
            using (var fs = File.Create(csvPath))
                CsfFileCsvHelper.WriteCsvFile(original, fs, ";");

            CsfFile loaded;
            using (var fs = File.OpenRead(csvPath))
                loaded = CsfFileCsvHelper.LoadFromCsvFile(fs, ";");

            Assert.AreEqual(original.Labels.Count, loaded.Labels.Count,
                "Semicolon delimiter should work");
        }
    }
}
