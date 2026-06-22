using System;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SadPencil.Ra2CsfFile.Tests
{
    [TestClass]
    public class ErrorHandlingTests
    {
        private static string TestDataDir => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData");
        private static string EnglishCsf => Path.Combine(TestDataDir, "ra2md.csf");

        [TestMethod]
        [ExpectedException(typeof(ObjectDisposedException))]
        public void LoadFromCsfFile_InvalidHeader_ThrowsException()
        {
            var bytes = new byte[100];
            Array.Copy(Encoding.ASCII.GetBytes("XXXX"), bytes, 4);
            using (var ms = new MemoryStream(bytes))
                CsfFile.LoadFromCsfFile(ms);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void LoadFromCsfFile_NullStream_ThrowsArgumentNullException()
        {
            CsfFile.LoadFromCsfFile(null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void WriteCsfFile_NullStream_ThrowsArgumentNullException()
        {
            var csf = new CsfFile();
            csf.WriteCsfFile(null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void AddLabel_NullName_ThrowsArgumentNullException()
        {
            var csf = new CsfFile();
            csf.AddLabel(null, "value");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void AddLabel_NullValue_ThrowsArgumentNullException()
        {
            var csf = new CsfFile();
            csf.AddLabel("TEST", null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void AddLabel_InvalidName_ThrowsArgumentException()
        {
            var csf = new CsfFile();
            csf.AddLabel("TEST\tTAB", "value");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void SetExtra_NullLabel_ThrowsArgumentNullException()
        {
            var csf = new CsfFile();
            csf.SetExtra(null, new byte[] { 1 });
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void LoadFromIniFile_NullStream_ThrowsArgumentNullException()
        {
            CsfFileIniHelper.LoadFromIniFile(null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void WriteIniFile_NullCsf_ThrowsArgumentNullException()
        {
            CsfFileIniHelper.WriteIniFile(null, new MemoryStream());
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void LoadFromJsonFile_NullStream_ThrowsArgumentNullException()
        {
            CsfFileJsonHelper.LoadFromJsonFile(null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void WriteJsonFile_NullCsf_ThrowsArgumentNullException()
        {
            CsfFileJsonHelper.WriteJsonFile(null, new MemoryStream());
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void LoadFromYamlFile_NullStream_ThrowsArgumentNullException()
        {
            CsfFileYamlHelper.LoadFromYamlFile(null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void WriteYamlFile_NullCsf_ThrowsArgumentNullException()
        {
            CsfFileYamlHelper.WriteYamlFile(null, new MemoryStream());
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void LoadFromCsvFile_NullStream_ThrowsArgumentNullException()
        {
            CsfFileCsvHelper.LoadFromCsvFile(null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void WriteCsvFile_NullCsf_ThrowsArgumentNullException()
        {
            CsfFileCsvHelper.WriteCsvFile(null, new MemoryStream());
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void LoadFromTxtFile_NullStream_ThrowsArgumentNullException()
        {
            CsfFileTxtHelper.LoadFromTxtFile(null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void WriteTxtFile_NullCsf_ThrowsArgumentNullException()
        {
            CsfFileTxtHelper.WriteTxtFile(null, new MemoryStream());
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidDataException))]
        public void LoadFromIniFile_MissingHeader_ThrowsInvalidDataException()
        {
            var bytes = Encoding.UTF8.GetBytes("[SomeSection]\nKey=Value\n");
            using (var ms = new MemoryStream(bytes))
                CsfFileIniHelper.LoadFromIniFile(ms);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidDataException))]
        public void LoadFromJsonFile_InvalidJson_ThrowsInvalidDataException()
        {
            var bytes = Encoding.UTF8.GetBytes("not valid json");
            using (var ms = new MemoryStream(bytes))
                CsfFileJsonHelper.LoadFromJsonFile(ms);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidDataException))]
        public void LoadFromYamlFile_InvalidYaml_ThrowsInvalidDataException()
        {
            var bytes = Encoding.UTF8.GetBytes("invalid: yaml: {{{{");
            using (var ms = new MemoryStream(bytes))
                CsfFileYamlHelper.LoadFromYamlFile(ms);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidDataException))]
        public void LoadFromCsvFile_EmptyFile_ThrowsInvalidDataException()
        {
            var bytes = Encoding.UTF8.GetBytes("");
            using (var ms = new MemoryStream(bytes))
                CsfFileCsvHelper.LoadFromCsvFile(ms);
        }

        [TestMethod]
        public void LoadFromCsvFile_EmptyData_HandlesGracefully()
        {
            var bytes = Encoding.UTF8.GetBytes("Label,Value,Extra\n");
            using (var ms = new MemoryStream(bytes))
            {
                var csf = CsfFileCsvHelper.LoadFromCsvFile(ms);
                Assert.AreEqual(0, csf.Labels.Count, "Empty CSV should have no labels");
            }
        }
    }
}
