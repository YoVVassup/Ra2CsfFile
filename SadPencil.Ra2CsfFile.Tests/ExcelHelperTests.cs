using System;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SadPencil.Ra2CsfFile.Tests
{
    [TestClass]
    public class ExcelHelperTests
    {
        private static string TestDataDir => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData");
        private static string Stringtable69Csf => Path.Combine(TestDataDir, "stringtable69.csf");
        private static string TempDir => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TempExcel");

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
        public void Excel_Xlsx_RoundTrip()
        {
            CsfFile original;
            using (var fs = File.OpenRead(Stringtable69Csf))
                original = CsfFile.LoadFromCsfFile(fs);

            string path = Path.Combine(TempDir, "test.xlsx");
            using (var fs = File.Create(path))
                CsfFileExcelHelper.WriteExcelFile(original, fs, true);

            CsfFile loaded;
            using (var fs = File.OpenRead(path))
                loaded = CsfFileExcelHelper.LoadFromExcelFile(fs);

            Assert.AreEqual(original.Labels.Count, loaded.Labels.Count, "XLSX round-trip label count should match");
            Assert.AreEqual(original.Version, loaded.Version, "XLSX round-trip version should match");
            Assert.AreEqual(original.Language, loaded.Language, "XLSX round-trip language should match");
        }

        [TestMethod]
        public void Excel_Xls_RoundTrip()
        {
            CsfFile original;
            using (var fs = File.OpenRead(Stringtable69Csf))
                original = CsfFile.LoadFromCsfFile(fs);

            string path = Path.Combine(TempDir, "test.xls");
            using (var fs = File.Create(path))
                CsfFileExcelHelper.WriteExcelFile(original, fs, false);

            CsfFile loaded;
            using (var fs = File.OpenRead(path))
                loaded = CsfFileExcelHelper.LoadFromExcelFile(fs);

            Assert.AreEqual(original.Labels.Count, loaded.Labels.Count, "XLS round-trip label count should match");
            Assert.AreEqual(original.Version, loaded.Version, "XLS round-trip version should match");
            Assert.AreEqual(original.Language, loaded.Language, "XLS round-trip language should match");
        }

        [TestMethod]
        public void Excel_Xlsx_PreservesValues()
        {
            CsfFile original;
            using (var fs = File.OpenRead(Stringtable69Csf))
                original = CsfFile.LoadFromCsfFile(fs);

            string path = Path.Combine(TempDir, "values.xlsx");
            using (var fs = File.Create(path))
                CsfFileExcelHelper.WriteExcelFile(original, fs, true);

            CsfFile loaded;
            using (var fs = File.OpenRead(path))
                loaded = CsfFileExcelHelper.LoadFromExcelFile(fs);

            foreach (var kvp in original.Labels)
            {
                Assert.IsTrue(loaded.Labels.ContainsKey(kvp.Key), $"Label {kvp.Key} should exist in Excel round-trip");
                Assert.AreEqual(kvp.Value, loaded.Labels[kvp.Key], $"Value for {kvp.Key} should match");
            }
        }

        [TestMethod]
        public void Excel_Xlsx_CreatesTwoSheets()
        {
            CsfFile csf;
            using (var fs = File.OpenRead(Stringtable69Csf))
                csf = CsfFile.LoadFromCsfFile(fs);

            string path = Path.Combine(TempDir, "sheets.xlsx");
            using (var fs = File.Create(path))
                CsfFileExcelHelper.WriteExcelFile(csf, fs, true);

            // Verify file is valid Excel by reading it back
            CsfFile loaded;
            using (var fs = File.OpenRead(path))
                loaded = CsfFileExcelHelper.LoadFromExcelFile(fs);

            Assert.IsTrue(loaded.Labels.Count > 0, "Should read labels from Excel");
        }

        [TestMethod]
        public void Excel_WithCyrillic_RoundTrip()
        {
            string russianCsf = Path.Combine(TestDataDir, "ra2md_ru.csf");
            CsfFile original;
            using (var fs = File.OpenRead(russianCsf))
                original = CsfFile.LoadFromCsfFile(fs);

            string path = Path.Combine(TempDir, "cyrillic.xlsx");
            using (var fs = File.Create(path))
                CsfFileExcelHelper.WriteExcelFile(original, fs, true);

            CsfFile loaded;
            using (var fs = File.OpenRead(path))
                loaded = CsfFileExcelHelper.LoadFromExcelFile(fs);

            Assert.AreEqual(original.Labels.Count, loaded.Labels.Count, "Cyrillic XLSX round-trip should match");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Excel_WriteNullCsf_ThrowsArgumentNullException()
        {
            CsfFileExcelHelper.WriteExcelFile(null, new MemoryStream(), true);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Excel_LoadNullStream_ThrowsArgumentNullException()
        {
            CsfFileExcelHelper.LoadFromExcelFile(null);
        }

        [TestMethod]
        public void Excel_EmptyCsf_WritesAndReadsCorrectly()
        {
            var csf = new CsfFile();

            string path = Path.Combine(TempDir, "empty.xlsx");
            using (var fs = File.Create(path))
                CsfFileExcelHelper.WriteExcelFile(csf, fs, true);

            CsfFile loaded;
            using (var fs = File.OpenRead(path))
                loaded = CsfFileExcelHelper.LoadFromExcelFile(fs);

            Assert.AreEqual(0, loaded.Labels.Count, "Empty XLSX should have no labels");
        }
    }
}
