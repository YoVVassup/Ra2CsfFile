using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SadPencil.Ra2CsfFile.Tests
{
    [TestClass]
    public class CsfFileTests
    {
        private static string TestDataDir => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData");
        private static string EnglishCsf => Path.Combine(TestDataDir, "ra2md.csf");
        private static string RussianCsf => Path.Combine(TestDataDir, "ra2md_ru.csf");

        #region CSF Read/Write

        [TestMethod]
        public void LoadFromCsfFile_English_FileReadsCorrectly()
        {
            using (var fs = File.OpenRead(EnglishCsf))
            {
                var csf = CsfFile.LoadFromCsfFile(fs);
                Assert.IsNotNull(csf);
                Assert.IsTrue(csf.Labels.Count > 0, "Should have labels");
                Assert.AreEqual(3, csf.Version, "CSF version should be 3");
            }
        }

        [TestMethod]
        public void LoadFromCsfFile_Russian_FileReadsCorrectly()
        {
            using (var fs = File.OpenRead(RussianCsf))
            {
                var csf = CsfFile.LoadFromCsfFile(fs);
                Assert.IsNotNull(csf);
                Assert.IsTrue(csf.Labels.Count > 0, "Should have labels");
                Assert.AreEqual(3, csf.Version, "CSF version should be 3");
            }
        }

        [TestMethod]
        public void WriteCsfFile_RoundTrip_PreservesLabels()
        {
            CsfFile original;
            using (var fs = File.OpenRead(EnglishCsf))
                original = CsfFile.LoadFromCsfFile(fs);

            byte[] writtenBytes;
            using (var ms = new MemoryStream())
            {
                original.WriteCsfFile(ms);
                writtenBytes = ms.ToArray();
            }

            CsfFile loaded;
            using (var ms = new MemoryStream(writtenBytes))
                loaded = CsfFile.LoadFromCsfFile(ms);

            Assert.AreEqual(original.Labels.Count, loaded.Labels.Count, "Label count should match");
            Assert.AreEqual(original.Version, loaded.Version, "Version should match");
            Assert.AreEqual(original.Language, loaded.Language, "Language should match");

            foreach (var kvp in original.Labels)
            {
                Assert.IsTrue(loaded.Labels.ContainsKey(kvp.Key), $"Label {kvp.Key} should exist");
                Assert.AreEqual(kvp.Value, loaded.Labels[kvp.Key], $"Value for {kvp.Key} should match");
            }
        }

        [TestMethod]
        public void WriteCsfFile_RoundTrip_PreservesExtraData()
        {
            CsfFile original;
            using (var fs = File.OpenRead(EnglishCsf))
                original = CsfFile.LoadFromCsfFile(fs);

            byte[] writtenBytes;
            using (var ms = new MemoryStream())
            {
                original.WriteCsfFile(ms);
                writtenBytes = ms.ToArray();
            }

            CsfFile loaded;
            using (var ms = new MemoryStream(writtenBytes))
                loaded = CsfFile.LoadFromCsfFile(ms);

            foreach (var label in original.Labels.Keys)
            {
                byte[] origExtra = original.GetExtra(label);
                byte[] loadedExtra = loaded.GetExtra(label);
                if (origExtra == null)
                    Assert.IsNull(loadedExtra, $"Extra for {label} should be null");
                else
                    Assert.IsNotNull(loadedExtra, $"Extra for {label} should not be null");
            }
        }

        #endregion

        #region INI Round-trip

        [TestMethod]
        public void IniRoundTrip_PreservesLabels()
        {
            CsfFile original;
            using (var fs = File.OpenRead(EnglishCsf))
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

            Assert.AreEqual(original.Labels.Count, loaded.Labels.Count, "INI round-trip label count should match");
            Assert.AreEqual(original.Version, loaded.Version, "INI round-trip version should match");
            Assert.AreEqual(original.Language, loaded.Language, "INI round-trip language should match");
        }

        #endregion

        #region JSON Round-trip

        [TestMethod]
        public void JsonRoundTrip_PreservesLabels()
        {
            CsfFile original;
            using (var fs = File.OpenRead(EnglishCsf))
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

            Assert.AreEqual(original.Labels.Count, loaded.Labels.Count, "JSON round-trip label count should match");
            Assert.AreEqual(original.Version, loaded.Version, "JSON round-trip version should match");
            Assert.AreEqual(original.Language, loaded.Language, "JSON round-trip language should match");
        }

        #endregion

        #region YAML Round-trip

        [TestMethod]
        public void YamlRoundTrip_PreservesLabels()
        {
            CsfFile original;
            using (var fs = File.OpenRead(EnglishCsf))
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

            Assert.AreEqual(original.Labels.Count, loaded.Labels.Count, "YAML round-trip label count should match");
            Assert.AreEqual(original.Version, loaded.Version, "YAML round-trip version should match");
            Assert.AreEqual(original.Language, loaded.Language, "YAML round-trip language should match");
        }

        #endregion

        #region CSV Round-trip

        [TestMethod]
        public void CsvRoundTrip_PreservesLabels()
        {
            CsfFile original;
            using (var fs = File.OpenRead(EnglishCsf))
                original = CsfFile.LoadFromCsfFile(fs);

            byte[] csvBytes;
            using (var ms = new MemoryStream())
            {
                CsfFileCsvHelper.WriteCsvFile(original, ms);
                csvBytes = ms.ToArray();
            }

            CsfFile loaded;
            using (var ms = new MemoryStream(csvBytes))
                loaded = CsfFileCsvHelper.LoadFromCsvFile(ms);

            Assert.AreEqual(original.Labels.Count, loaded.Labels.Count, "CSV round-trip label count should match");
            Assert.AreEqual(original.Version, loaded.Version, "CSV round-trip version should match");
            Assert.AreEqual(original.Language, loaded.Language, "CSV round-trip language should match");
        }

        #endregion

        #region TXT Round-trip

        [TestMethod]
        public void TxtRoundTrip_PreservesLabels()
        {
            CsfFile original;
            using (var fs = File.OpenRead(EnglishCsf))
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

            Assert.AreEqual(original.Labels.Count, loaded.Labels.Count, "TXT round-trip label count should match");
            Assert.AreEqual(original.Version, loaded.Version, "TXT round-trip version should match");
            Assert.AreEqual(original.Language, loaded.Language, "TXT round-trip language should match");
        }

        #endregion

        #region OrderByKey

        [TestMethod]
        public void OrderByKey_SortsLabelsAlphabetically()
        {
            CsfFile original;
            using (var fs = File.OpenRead(EnglishCsf))
                original = CsfFile.LoadFromCsfFile(fs);

            var options = new CsfFileOptions { OrderByKey = true };
            CsfFile sorted;
            using (var fs = File.OpenRead(EnglishCsf))
                sorted = CsfFile.LoadFromCsfFile(fs, options);

            // Verify all labels are present and GetLabelsInWriteOrder returns same count
            var writeOrder = sorted.GetLabelsInWriteOrder().ToList();
            Assert.AreEqual(sorted.Labels.Count, writeOrder.Count, "Write order should contain all labels");

            // Verify no duplicates
            var unique = new HashSet<string>(writeOrder, StringComparer.OrdinalIgnoreCase);
            Assert.AreEqual(writeOrder.Count, unique.Count, "Write order should have no duplicates");

            // Verify all labels are present
            foreach (var key in sorted.Labels.Keys)
                Assert.IsTrue(unique.Contains(key), $"Label {key} should be in write order");
        }

        #endregion

        #region Clone

        [TestMethod]
        public void Clone_CreatesIndependentCopy()
        {
            CsfFile original;
            using (var fs = File.OpenRead(EnglishCsf))
                original = CsfFile.LoadFromCsfFile(fs);

            var clone = (CsfFile)original.Clone();

            Assert.AreEqual(original.Labels.Count, clone.Labels.Count);
            Assert.AreEqual(original.Version, clone.Version);
            Assert.AreEqual(original.Language, clone.Language);

            // Modify clone should not affect original
            clone.AddLabel("TEST_LABEL", "test_value");
            Assert.AreEqual(original.Labels.Count, clone.Labels.Count - 1);
        }

        #endregion

        #region ValidateLabelName

        [TestMethod]
        public void ValidateLabelName_ValidNames_ReturnsTrue()
        {
            Assert.IsTrue(CsfFile.ValidateLabelName("GUI:Button"));
            Assert.IsTrue(CsfFile.ValidateLabelName("TEST_LABEL"));
            Assert.IsTrue(CsfFile.ValidateLabelName("label with spaces"));
        }

        [TestMethod]
        public void ValidateLabelName_InvalidNames_ReturnsFalse()
        {
            Assert.IsFalse(CsfFile.ValidateLabelName(""));
            Assert.IsFalse(CsfFile.ValidateLabelName(null));
            Assert.IsFalse(CsfFile.ValidateLabelName("label\twith\ttabs"));
            Assert.IsFalse(CsfFile.ValidateLabelName("label\nwith\nnewlines"));
        }

        #endregion

        #region Label Operations

        [TestMethod]
        public void AddLabel_NewLabel_ReturnsFalse()
        {
            var csf = new CsfFile();
            bool existed = csf.AddLabel("TEST", "value");
            Assert.IsFalse(existed);
            Assert.AreEqual(1, csf.Labels.Count);
        }

        [TestMethod]
        public void AddLabel_ExistingLabel_ReturnsTrue()
        {
            var csf = new CsfFile();
            csf.AddLabel("TEST", "value1");
            bool existed = csf.AddLabel("TEST", "value2");
            Assert.IsTrue(existed);
            Assert.AreEqual("value2", csf.Labels["TEST"]);
        }

        [TestMethod]
        public void RemoveLabel_ExistingLabel_ReturnsTrue()
        {
            var csf = new CsfFile();
            csf.AddLabel("TEST", "value");
            bool removed = csf.RemoveLabel("TEST");
            Assert.IsTrue(removed);
            Assert.AreEqual(0, csf.Labels.Count);
        }

        [TestMethod]
        public void RemoveLabel_NonExistingLabel_ReturnsFalse()
        {
            var csf = new CsfFile();
            bool removed = csf.RemoveLabel("NONEXISTENT");
            Assert.IsFalse(removed);
        }

        #endregion

        #region Extra Data

        [TestMethod]
        public void ExtraData_SetAndGet()
        {
            var csf = new CsfFile();
            csf.AddLabel("TEST", "value");

            byte[] extra = new byte[] { 1, 2, 3, 4, 5 };
            csf.SetExtra("TEST", extra);

            Assert.IsTrue(csf.HasExtra("TEST"));
            CollectionAssert.AreEqual(extra, csf.GetExtra("TEST"));
        }

        [TestMethod]
        public void ExtraData_RemoveExtra()
        {
            var csf = new CsfFile();
            csf.AddLabel("TEST", "value");
            csf.SetExtra("TEST", new byte[] { 1, 2, 3 });

            csf.RemoveExtra("TEST");
            Assert.IsFalse(csf.HasExtra("TEST"));
            Assert.IsNull(csf.GetExtra("TEST"));
        }

        #endregion

        #region Russian CSF

        [TestMethod]
        public void RussianCsf_RoundTrip_PreservesLabels()
        {
            CsfFile original;
            using (var fs = File.OpenRead(RussianCsf))
                original = CsfFile.LoadFromCsfFile(fs);

            byte[] writtenBytes;
            using (var ms = new MemoryStream())
            {
                original.WriteCsfFile(ms);
                writtenBytes = ms.ToArray();
            }

            CsfFile loaded;
            using (var ms = new MemoryStream(writtenBytes))
                loaded = CsfFile.LoadFromCsfFile(ms);

            Assert.AreEqual(original.Labels.Count, loaded.Labels.Count, "Russian CSF label count should match");

            foreach (var kvp in original.Labels)
            {
                Assert.IsTrue(loaded.Labels.ContainsKey(kvp.Key), $"Russian label {kvp.Key} should exist");
                Assert.AreEqual(kvp.Value, loaded.Labels[kvp.Key], $"Russian value for {kvp.Key} should match");
            }
        }

        [TestMethod]
        public void RussianCsf_IniRoundTrip_PreservesLabels()
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

            Assert.AreEqual(original.Labels.Count, loaded.Labels.Count, "Russian INI round-trip should match");
        }

        #endregion
    }
}
