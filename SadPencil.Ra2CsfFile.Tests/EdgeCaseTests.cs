using System;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SadPencil.Ra2CsfFile.Tests
{
    [TestClass]
    public class EdgeCaseTests
    {
        #region Empty CsfFile

        [TestMethod]
        public void EmptyCsfFile_HasZeroLabels()
        {
            var csf = new CsfFile();
            Assert.AreEqual(0, csf.Labels.Count);
        }

        [TestMethod]
        public void EmptyCsfFile_WritesAndReadsCorrectly()
        {
            var csf = new CsfFile();
            byte[] bytes;
            using (var ms = new MemoryStream())
            {
                csf.WriteCsfFile(ms);
                bytes = ms.ToArray();
            }

            CsfFile loaded;
            using (var ms = new MemoryStream(bytes))
                loaded = CsfFile.LoadFromCsfFile(ms);

            Assert.AreEqual(0, loaded.Labels.Count);
        }

        [TestMethod]
        public void EmptyCsfFile_IniRoundTrip()
        {
            var csf = new CsfFile();

            byte[] iniBytes;
            using (var ms = new MemoryStream())
            {
                CsfFileIniHelper.WriteIniFile(csf, ms);
                iniBytes = ms.ToArray();
            }

            CsfFile loaded;
            using (var ms = new MemoryStream(iniBytes))
                loaded = CsfFileIniHelper.LoadFromIniFile(ms);

            Assert.AreEqual(0, loaded.Labels.Count);
        }

        #endregion

        #region Single Label

        [TestMethod]
        public void SingleLabel_WritesAndReadsCorrectly()
        {
            var csf = new CsfFile();
            csf.AddLabel("TEST", "Hello World");

            byte[] bytes;
            using (var ms = new MemoryStream())
            {
                csf.WriteCsfFile(ms);
                bytes = ms.ToArray();
            }

            CsfFile loaded;
            using (var ms = new MemoryStream(bytes))
                loaded = CsfFile.LoadFromCsfFile(ms);

            Assert.AreEqual(1, loaded.Labels.Count);
            Assert.AreEqual("Hello World", loaded.Labels["TEST"]);
        }

        [TestMethod]
        public void SingleLabel_WithExtraData()
        {
            var csf = new CsfFile();
            byte[] extra = new byte[] { 1, 2, 3, 4, 5 };
            csf.AddLabel("TEST", "Value", extra);

            byte[] bytes;
            using (var ms = new MemoryStream())
            {
                csf.WriteCsfFile(ms);
                bytes = ms.ToArray();
            }

            CsfFile loaded;
            using (var ms = new MemoryStream(bytes))
                loaded = CsfFile.LoadFromCsfFile(ms);

            Assert.AreEqual(1, loaded.Labels.Count);
            Assert.AreEqual("Value", loaded.Labels["TEST"]);
            CollectionAssert.AreEqual(extra, loaded.GetExtra("TEST"));
        }

        #endregion

        #region Empty Values

        [TestMethod]
        public void EmptyValue_WritesAndReadsCorrectly()
        {
            var csf = new CsfFile();
            csf.AddLabel("EMPTY", "");

            byte[] bytes;
            using (var ms = new MemoryStream())
            {
                csf.WriteCsfFile(ms);
                bytes = ms.ToArray();
            }

            CsfFile loaded;
            using (var ms = new MemoryStream(bytes))
                loaded = CsfFile.LoadFromCsfFile(ms);

            Assert.AreEqual(1, loaded.Labels.Count);
            Assert.AreEqual("", loaded.Labels["EMPTY"]);
        }

        [TestMethod]
        public void NullExtraData_WritesAndReadsCorrectly()
        {
            var csf = new CsfFile();
            csf.AddLabel("NO_EXTRA", "Value");
            Assert.IsNull(csf.GetExtra("NO_EXTRA"));

            byte[] bytes;
            using (var ms = new MemoryStream())
            {
                csf.WriteCsfFile(ms);
                bytes = ms.ToArray();
            }

            CsfFile loaded;
            using (var ms = new MemoryStream(bytes))
                loaded = CsfFile.LoadFromCsfFile(ms);

            Assert.IsNull(loaded.GetExtra("NO_EXTRA"));
        }

        #endregion

        #region Multi-line Values

        [TestMethod]
        public void MultiLineValue_PreservesNewlines()
        {
            var csf = new CsfFile();
            csf.AddLabel("MULTI", "Line1\nLine2\nLine3");

            byte[] bytes;
            using (var ms = new MemoryStream())
            {
                csf.WriteCsfFile(ms);
                bytes = ms.ToArray();
            }

            CsfFile loaded;
            using (var ms = new MemoryStream(bytes))
                loaded = CsfFile.LoadFromCsfFile(ms);

            Assert.AreEqual("Line1\nLine2\nLine3", loaded.Labels["MULTI"]);
        }

        [TestMethod]
        public void MultiLineValue_IniRoundTrip()
        {
            var csf = new CsfFile();
            csf.AddLabel("MULTI", "Line1\nLine2\nLine3");

            byte[] iniBytes;
            using (var ms = new MemoryStream())
            {
                CsfFileIniHelper.WriteIniFile(csf, ms);
                iniBytes = ms.ToArray();
            }

            CsfFile loaded;
            using (var ms = new MemoryStream(iniBytes))
                loaded = CsfFileIniHelper.LoadFromIniFile(ms);

            Assert.AreEqual("Line1\nLine2\nLine3", loaded.Labels["MULTI"]);
        }

        #endregion

        #region Special Characters

        [TestMethod]
        public void SpecialCharacters_PreservesCorrectly()
        {
            var csf = new CsfFile();
            csf.AddLabel("SPECIAL", "Hello!@#$%^&*()_+-=[]{}|;':\",./<>?");

            byte[] bytes;
            using (var ms = new MemoryStream())
            {
                csf.WriteCsfFile(ms);
                bytes = ms.ToArray();
            }

            CsfFile loaded;
            using (var ms = new MemoryStream(bytes))
                loaded = CsfFile.LoadFromCsfFile(ms);

            Assert.AreEqual("Hello!@#$%^&*()_+-=[]{}|;':\",./<>?", loaded.Labels["SPECIAL"]);
        }

        [TestMethod]
        public void UnicodeCharacters_PreservesCorrectly()
        {
            var csf = new CsfFile();
            csf.AddLabel("UNICODE", "日本語テスト 한국어 test");

            byte[] bytes;
            using (var ms = new MemoryStream())
            {
                csf.WriteCsfFile(ms);
                bytes = ms.ToArray();
            }

            CsfFile loaded;
            using (var ms = new MemoryStream(bytes))
                loaded = CsfFile.LoadFromCsfFile(ms);

            Assert.AreEqual("日本語テスト 한국어 test", loaded.Labels["UNICODE"]);
        }

        #endregion

        #region Case Insensitivity

        [TestMethod]
        public void CaseInsensitive_LabelLookup()
        {
            var csf = new CsfFile();
            csf.AddLabel("TestLabel", "Value");

            Assert.AreEqual("Value", csf.Labels["testlabel"]);
            Assert.AreEqual("Value", csf.Labels["TESTLABEL"]);
            Assert.AreEqual("Value", csf.Labels["TestLabel"]);
        }

        [TestMethod]
        public void CaseInsensitive_DuplicateLabels()
        {
            var csf = new CsfFile();
            csf.AddLabel("TestLabel", "Value1");
            bool existed = csf.AddLabel("testlabel", "Value2");

            Assert.IsTrue(existed);
            Assert.AreEqual(1, csf.Labels.Count);
            Assert.AreEqual("Value2", csf.Labels["TestLabel"]);
        }

        [TestMethod]
        public void CaseInsensitive_RemoveLabel()
        {
            var csf = new CsfFile();
            csf.AddLabel("TestLabel", "Value");

            bool removed = csf.RemoveLabel("testlabel");
            Assert.IsTrue(removed);
            Assert.AreEqual(0, csf.Labels.Count);
        }

        #endregion

        #region Label Names

        [TestMethod]
        public void LabelName_WithColons()
        {
            var csf = new CsfFile();
            csf.AddLabel("GUI:Button:OK", "OK");
            Assert.AreEqual(1, csf.Labels.Count);
        }

        [TestMethod]
        public void LabelName_WithSpaces()
        {
            var csf = new CsfFile();
            csf.AddLabel("My Label", "Value");
            Assert.AreEqual(1, csf.Labels.Count);
        }

        [TestMethod]
        public void LabelName_WithNumbers()
        {
            var csf = new CsfFile();
            csf.AddLabel("Label123", "Value");
            Assert.AreEqual(1, csf.Labels.Count);
        }

        [TestMethod]
        public void LabelName_WithUnderscores()
        {
            var csf = new CsfFile();
            csf.AddLabel("My_Label_Name", "Value");
            Assert.AreEqual(1, csf.Labels.Count);
        }

        #endregion

        #region Large Labels Collection

        [TestMethod]
        public void ManyLabels_WritesAndReadsCorrectly()
        {
            var csf = new CsfFile();
            for (int i = 0; i < 1000; i++)
                csf.AddLabel($"LABEL_{i:D4}", $"Value_{i}");

            byte[] bytes;
            using (var ms = new MemoryStream())
            {
                csf.WriteCsfFile(ms);
                bytes = ms.ToArray();
            }

            CsfFile loaded;
            using (var ms = new MemoryStream(bytes))
                loaded = CsfFile.LoadFromCsfFile(ms);

            Assert.AreEqual(1000, loaded.Labels.Count);
            for (int i = 0; i < 1000; i++)
                Assert.AreEqual($"Value_{i}", loaded.Labels[$"LABEL_{i:D4}"]);
        }

        #endregion

        #region CsfFileOptions

        [TestMethod]
        public void CsfFileOptions_Equality()
        {
            var opt1 = new CsfFileOptions { OrderByKey = true, ApplyEncoding1252ToExtra = true };
            var opt2 = new CsfFileOptions { OrderByKey = true, ApplyEncoding1252ToExtra = true };
            var opt3 = new CsfFileOptions { OrderByKey = false, ApplyEncoding1252ToExtra = false };

            Assert.IsTrue(opt1.Equals(opt2));
            Assert.IsFalse(opt1.Equals(opt3));
            Assert.IsTrue(opt1 == opt2);
            Assert.IsTrue(opt1 != opt3);
        }

        [TestMethod]
        public void CsfFileOptions_NullEquality()
        {
            var opt = new CsfFileOptions();
            Assert.IsFalse(opt.Equals(null));
            Assert.IsFalse(opt == null);
            Assert.IsTrue(opt != null);
        }

        #endregion

        #region CsfFile Equality

        [TestMethod]
        public void CsfFile_Equality_SameContent()
        {
            var csf1 = new CsfFile();
            csf1.AddLabel("TEST", "Value");

            var csf2 = new CsfFile();
            csf2.AddLabel("TEST", "Value");

            Assert.IsTrue(csf1.Equals(csf2));
            Assert.IsTrue(csf1 == csf2);
        }

        [TestMethod]
        public void CsfFile_Equality_DifferentContent()
        {
            var csf1 = new CsfFile();
            csf1.AddLabel("TEST", "Value1");

            var csf2 = new CsfFile();
            csf2.AddLabel("TEST", "Value2");

            Assert.IsFalse(csf1.Equals(csf2));
            Assert.IsTrue(csf1 != csf2);
        }

        [TestMethod]
        public void CsfFile_Equality_DifferentLabels()
        {
            var csf1 = new CsfFile();
            csf1.AddLabel("TEST1", "Value");

            var csf2 = new CsfFile();
            csf2.AddLabel("TEST2", "Value");

            Assert.IsFalse(csf1.Equals(csf2));
        }

        [TestMethod]
        public void CsfFile_Equality_Null()
        {
            var csf = new CsfFile();
            Assert.IsFalse(csf.Equals(null));
            Assert.IsFalse(csf == null);
            Assert.IsTrue(csf != null);
        }

        #endregion

        #region LLF Format

        [TestMethod]
        public void LlfRoundTrip_PreservesLabels()
        {
            string TestDataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData");
            string EnglishCsf = Path.Combine(TestDataDir, "ra2md.csf");

            CsfFile original;
            using (var fs = File.OpenRead(EnglishCsf))
                original = CsfFile.LoadFromCsfFile(fs);

            byte[] llfBytes;
            using (var ms = new MemoryStream())
            {
                CsfFileLlfHelper.WriteLlfFile(original, ms);
                llfBytes = ms.ToArray();
            }

            CsfFile loaded;
            using (var ms = new MemoryStream(llfBytes))
                loaded = CsfFileLlfHelper.LoadFromLlfFile(ms);

            // LLF round-trip may lose some labels due to format limitations
            // Check that most labels are preserved (at least 99%)
            double preserved = (double)loaded.Labels.Count / original.Labels.Count;
            Assert.IsTrue(preserved >= 0.99,
                $"LLF round-trip should preserve at least 99% of labels. Got {loaded.Labels.Count}/{original.Labels.Count} ({preserved:P1})");
            Assert.AreEqual(original.Version, loaded.Version, "LLF round-trip version should match");
            Assert.AreEqual(original.Language, loaded.Language, "LLF round-trip language should match");
        }

        #endregion
    }
}
